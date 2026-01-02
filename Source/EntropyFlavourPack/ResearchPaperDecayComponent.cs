using System.Collections.Generic;
using System.Linq;
using EntropyFlavourPack.Defs;
using LudeonTK;
using ResearchPapers;
using RimWorld;
using Verse;

namespace EntropyFlavourPack
{
    public class ResearchPaperDecayComponent : GameComponent
    {
        public List<string> translationKeyCollection;

        private const int MinDaysBetweenDecay = 10;
        private const int MaxDaysBetweenDecay = 15;
        private const int TicksPerDay = 60000;

        private int ticksUntilNextDecay;
        private int nextDecayInterval;

        public ResearchPaperDecayComponent(Game game)
        {
            this.nextDecayInterval = GetRandomDecayInterval();
            this.ticksUntilNextDecay = this.nextDecayInterval;
            this.translationKeyCollection = DefDatabase<TranslationKeyCollectionDef>.GetNamed("EntropyFlavourPack_ResearchPaperDecayMessages").translationKeys;
        }

        private int GetRandomDecayInterval()
        {
            return Rand.RangeInclusive(MinDaysBetweenDecay, MaxDaysBetweenDecay) * TicksPerDay;
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
        }

        private string GetRandomDecayMessage(string paperLabel)
        {
            string randomKey = translationKeyCollection.RandomElement();
            return randomKey.Translate(paperLabel).ToString();
        }

        private TechLevel GetPaperTechLevel(Thing paper)
        {
            if (paper.TryGetComp<CompResearchPaper>() is not { } paperComp || paperComp.projects.NullOrEmpty()) return TechLevel.Undefined;

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
            if (EntropyFlavourPackDefOf.ResearchPaper == null) return;
            List<Thing> allPapers = [];

            foreach (Map map in Find.Maps)
            {
                if (map is not { IsPlayerHome: true })
                    continue;

                List<Thing> papers = map.listerThings.ThingsOfDef(EntropyFlavourPackDefOf.ResearchPaper)
                    .Where(t => !t.Destroyed)
                    .ToList();

                allPapers.AddRange(papers);
            }

            if (allPapers.Count == 0) return;

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

            if (highestTechPapers.Count == 0) return;

            Thing paperToDestroy = highestTechPapers.RandomElement();
            if (paperToDestroy == null) return;

            string paperLabel = paperToDestroy.Label;
            Map paperMap = paperToDestroy.Map;

            paperToDestroy.Destroy(DestroyMode.Vanish);

            string letterLabel = "EntropyFlavourPack_DecayLetterLabel".Translate();
            string letterText = GetRandomDecayMessage(paperLabel);

            Find.LetterStack.ReceiveLetter(
                letterLabel,
                letterText,
                LetterDefOf.NegativeEvent,
                new LookTargets(paperToDestroy.Position, paperMap)
            );
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

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref ticksUntilNextDecay, "ticksUntilNextDecay", GetRandomDecayInterval());
            Scribe_Values.Look(ref nextDecayInterval, "nextDecayInterval", GetRandomDecayInterval());
        }
    }
}
