using System.Collections.Generic;
using System.Linq;
using EntropyFlavourPack.Defs;
using LudeonTK;
using ResearchPapers;
using RimWorld;
using Verse;
using VFETribals;

namespace EntropyFlavourPack
{
    public class ResearchPaperDecayComponent : GameComponent
    {
        public List<StringChance> translationKeyCollection;

        private const int MinDaysBetweenDecay = 10;
        private const int MaxDaysBetweenDecay = 15;
        private const int TicksPerDay = 60000;

        private int ticksUntilNextDecay;
        private int nextDecayInterval;
        private TechLevel lastHighestPaperTechLevel = TechLevel.Undefined;
        private int pendingNegativeCornerstoneChoices;

        public ResearchPaperDecayComponent(Game game)
        {
            nextDecayInterval = GetRandomDecayInterval();
            ticksUntilNextDecay = nextDecayInterval;
            translationKeyCollection = DefDatabase<TranslationKeyCollectionDef>.GetNamed("EntropyFlavourPack_ResearchPaperDecayMessages").translationKeys;
        }

        private int GetRandomDecayInterval()
        {
            return Rand.RangeInclusive(MinDaysBetweenDecay, MaxDaysBetweenDecay) * TicksPerDay;
        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();
            lastHighestPaperTechLevel = GetHighestPaperTechLevel();
        }

        public override void GameComponentTick()
        {
            base.GameComponentTick();

            if (ticksUntilNextDecay > 0)
            {
                ticksUntilNextDecay--;
            }

            if (ticksUntilNextDecay <= 0)
            {
                TryDecayResearchPaper();
                nextDecayInterval = GetRandomDecayInterval();
                ticksUntilNextDecay = nextDecayInterval;
            }

            if (pendingNegativeCornerstoneChoices > 0 && !Find.WindowStack.IsOpen<Window_ChooseNegativeCornerstone>())
                TryShowNegativeCornerstoneWindow();
        }

        private string GetRandomDecayMessage(string paperLabel, Pawn nearbyPawn)
        {
            string randomKey = translationKeyCollection.RandomElementByWeight(el=>el.weight).key;
            return randomKey.Translate(paperLabel, nearbyPawn.Named("PAWN")).ToString();
        }

        private TechLevel GetPaperTechLevel(Thing paper)
        {
            if (paper.TryGetComp<CompResearchPaper>() is not { } paperComp || paperComp.projects.NullOrEmpty())
                return TechLevel.Undefined;

            TechLevel highestTech = TechLevel.Undefined;
            foreach (ResearchProjectDef project in paperComp.projects)
            {
                if (project != null && project.techLevel > highestTech)
                {
                    highestTech = project.techLevel;
                }
            }

            return highestTech;
        }

        private void TryDecayResearchPaper()
        {
            if (EntropyFlavourPackDefOf.ResearchPaper == null)
                return;
            List<Thing> allPapers = [];

            foreach (Map map in Find.Maps)
            {
                if (map is not { IsPlayerHome: true })
                    continue;

                List<Thing> papers = map.listerThings.ThingsOfDef(EntropyFlavourPackDefOf.ResearchPaper).Where(t => !t.Destroyed).ToList();

                allPapers.AddRange(papers);
            }

            if (allPapers.Count == 0)
                return;

            Dictionary<TechLevel, List<Thing>> papersByTechLevel = new();

            foreach (Thing paper in allPapers)
            {
                TechLevel techLevel = GetPaperTechLevel(paper);

                if (!papersByTechLevel.ContainsKey(techLevel))
                {
                    papersByTechLevel[techLevel] = [];
                }

                papersByTechLevel[techLevel].Add(paper);
            }

            TechLevel highestTechLevel = TechLevel.Undefined;
            foreach (TechLevel techLevel in papersByTechLevel.Keys)
            {
                if (techLevel > highestTechLevel)
                {
                    highestTechLevel = techLevel;
                }
            }

            List<Thing> highestTechPapers = papersByTechLevel[highestTechLevel];

            if (highestTechPapers.Count == 0)
                return;

            Thing paperToDestroy = highestTechPapers.RandomElement();
            if (paperToDestroy == null)
                return;

            Pawn nearbyPawn = paperToDestroy.Map.mapPawns.FreeColonistsSpawned.OrderBy(p => p.Position.DistanceTo(paperToDestroy.Position)).FirstOrDefault() ?? paperToDestroy.Map.mapPawns.FreeColonistsSpawned.RandomElement();

            string paperLabel = paperToDestroy.Label;
            Map paperMap = paperToDestroy.Map;
            TechLevel destroyedPaperTechLevel = GetPaperTechLevel(paperToDestroy);

            paperToDestroy.Destroy(DestroyMode.Vanish);

            string letterLabel = "EntropyFlavourPack_DecayLetterLabel".Translate();
            string letterText = GetRandomDecayMessage(paperLabel, nearbyPawn);

            Find.LetterStack.ReceiveLetter(letterLabel, letterText, LetterDefOf.NegativeEvent, new LookTargets(paperToDestroy.Position, paperMap));

            CheckForTechRegression(destroyedPaperTechLevel);
        }

        private TechLevel GetHighestPaperTechLevel()
        {
            if (EntropyFlavourPackDefOf.ResearchPaper == null)
                return TechLevel.Undefined;

            TechLevel highest = TechLevel.Undefined;

            foreach (Map map in Find.Maps)
            {
                if (map is not { IsPlayerHome: true })
                    continue;

                foreach (Thing paper in map.listerThings.ThingsOfDef(EntropyFlavourPackDefOf.ResearchPaper).Where(t => !t.Destroyed))
                {
                    TechLevel paperTech = GetPaperTechLevel(paper);
                    if (paperTech > highest)
                    {
                        highest = paperTech;
                    }
                }
            }

            return highest;
        }

        private void CheckForTechRegression(TechLevel destroyedPaperTechLevel)
        {
            TechLevel currentHighest = GetHighestPaperTechLevel();

            if (destroyedPaperTechLevel >= lastHighestPaperTechLevel && currentHighest < lastHighestPaperTechLevel)
            {
                int levelsLost = (int)lastHighestPaperTechLevel - (int)currentHighest;
                if (levelsLost > 0)
                {
                    pendingNegativeCornerstoneChoices += levelsLost;
                    TryShowNegativeCornerstoneWindow();
                }
            }

            lastHighestPaperTechLevel = currentHighest;
        }

        private void TryShowNegativeCornerstoneWindow()
        {
            if (pendingNegativeCornerstoneChoices <= 0)
                return;

            List<CornerstoneDef> negativeCornerstones = GetNegativeCornerstones();
            if (negativeCornerstones.Count == 0)
            {
                pendingNegativeCornerstoneChoices = 0;
                return;
            }

            Find.WindowStack.Add(new Window_ChooseNegativeCornerstone(negativeCornerstones, OnCornerstoneChosen));
        }

        private void OnCornerstoneChosen(CornerstoneDef chosen)
        {
            pendingNegativeCornerstoneChoices--;

            if (GameComponent_Tribals.Instance != null)
            {
                GameComponent_Tribals.Instance.cornerstones.Add(chosen);

                if (!GameComponent_Tribals.Instance.ethosLocked)
                    GameComponent_Tribals.Instance.ethos = GameComponent_Tribals.Instance.GetNewEthos();
            }

            string letterLabel = "EntropyFlavourPack_TechRegressionLetterLabel".Translate(chosen.label);
            string letterText = "EntropyFlavourPack_TechRegressionLetterText".Translate(chosen.label, chosen.description);
            Find.LetterStack.ReceiveLetter(letterLabel, letterText, LetterDefOf.NegativeEvent);

            if (pendingNegativeCornerstoneChoices > 0)
                TryShowNegativeCornerstoneWindow();
        }

        private List<CornerstoneDef> GetNegativeCornerstones()
        {
            return DefDatabase<CornerstoneDef>
                .AllDefsListForReading.Where(c => c.defName.Contains("Negative") || c.defName.StartsWith("EFP_Neg"))
                .Where(c => !(GameComponent_Tribals.Instance?.cornerstones.Contains(c) ?? false))
                .ToList();
        }

        [DebugAction("Entropy Flavour Pack", "Trigger Research Paper Decay")]
        public static void TriggerResearchPaperDecayNextTick()
        {
            ResearchPaperDecayComponent component = Current.Game.GetComponent<ResearchPaperDecayComponent>();
            if (component != null)
            {
                component.ticksUntilNextDecay = 0;
            }
        }

        [DebugAction("Entropy Flavour Pack", "Trigger Tech Regression Event")]
        public static void TriggerTechRegressionEvent()
        {
            ResearchPaperDecayComponent component = Current.Game.GetComponent<ResearchPaperDecayComponent>();
            if (component != null)
            {
                component.pendingNegativeCornerstoneChoices++;
                component.TryShowNegativeCornerstoneWindow();
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref ticksUntilNextDecay, "ticksUntilNextDecay", GetRandomDecayInterval());
            Scribe_Values.Look(ref nextDecayInterval, "nextDecayInterval", GetRandomDecayInterval());
            Scribe_Values.Look(ref lastHighestPaperTechLevel, "lastHighestPaperTechLevel", TechLevel.Undefined);
            Scribe_Values.Look(ref pendingNegativeCornerstoneChoices, "pendingNegativeCornerstoneChoices", 0);
        }
    }
}
