using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace EntropyFlavourPack
{
    public class ResearchPaperDecayComponent : GameComponent
    {
        private const int MinDaysBetweenDecay = 10;
        private const int MaxDaysBetweenDecay = 15;
        private const int TicksPerDay = 60000;
        
        private int ticksUntilNextDecay;
        private int nextDecayInterval;

        public ResearchPaperDecayComponent(Game game)
        {
            this.nextDecayInterval = GetRandomDecayInterval();
            this.ticksUntilNextDecay = this.nextDecayInterval;
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
            List<string> messageKeys = new List<string>
            {
                "EntropyFlavourPack_DecayMessage_Damp",
                "EntropyFlavourPack_DecayMessage_Insects",
                "EntropyFlavourPack_DecayMessage_Fire",
                "EntropyFlavourPack_DecayMessage_Fade",
                "EntropyFlavourPack_DecayMessage_Crumble",
                "EntropyFlavourPack_DecayMessage_Spill",
                "EntropyFlavourPack_DecayMessage_Rodents"
            };
            
            string randomKey = messageKeys.RandomElement();
            return randomKey.Translate(paperLabel).ToString();
        }

        private TechLevel GetPaperTechLevel(Thing paper)
        {
            var compsProperty = typeof(ThingWithComps).GetProperty("AllComps", 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            
            if (compsProperty != null && paper is ThingWithComps thingWithComps)
            {
                var comps = compsProperty.GetValue(thingWithComps) as List<ThingComp>;
                if (comps != null)
                {
                    foreach (var comp in comps)
                    {
                        if (comp.GetType().Name == "CompResearchPaper")
                        {
                            var projectsField = comp.GetType().GetField("projects");
                            if (projectsField != null)
                            {
                                var projects = projectsField.GetValue(comp) as List<ResearchProjectDef>;
                                if (projects != null && projects.Count > 0)
                                {
                                    TechLevel highestTech = TechLevel.Undefined;
                                    foreach (var project in projects)
                                    {
                                        if (project != null && project.techLevel > highestTech)
                                        {
                                            highestTech = project.techLevel;
                                        }
                                    }
                                    return highestTech;
                                }
                            }
                            break;
                        }
                    }
                }
            }
            
            return TechLevel.Undefined;
        }

        private void TryDecayResearchPaper()
        {
            List<Thing> allPapers = new List<Thing>();
            
            foreach (Map map in Find.Maps)
            {
                if (map == null || !map.IsPlayerHome)
                    continue;

                var papers = map.listerThings.AllThings
                    .Where(t => t.def.defName == "ResearchPaper" && !t.Destroyed)
                    .ToList();
                
                allPapers.AddRange(papers);
            }

            if (allPapers.Count == 0)
            {
                return;
            }

            Dictionary<TechLevel, List<Thing>> papersByTechLevel = new Dictionary<TechLevel, List<Thing>>();
            
            foreach (var paper in allPapers)
            {
                TechLevel techLevel = GetPaperTechLevel(paper);
                
                if (!papersByTechLevel.ContainsKey(techLevel))
                {
                    papersByTechLevel[techLevel] = new List<Thing>();
                }
                papersByTechLevel[techLevel].Add(paper);
            }

            TechLevel highestTechLevel = TechLevel.Undefined;
            foreach (var techLevel in papersByTechLevel.Keys)
            {
                if (techLevel > highestTechLevel)
                {
                    highestTechLevel = techLevel;
                }
            }

            List<Thing> highestTechPapers = papersByTechLevel[highestTechLevel];
            
            if (highestTechPapers.Count == 0)
            {
                return;
            }

            Thing paperToDestroy = highestTechPapers.RandomElement();
            
            if (paperToDestroy != null)
            {
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
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref ticksUntilNextDecay, "ticksUntilNextDecay", GetRandomDecayInterval());
            Scribe_Values.Look(ref nextDecayInterval, "nextDecayInterval", GetRandomDecayInterval());
        }
    }
}
