using UnityEngine;
using Verse;

namespace EntropyFlavourPack;

public class Settings : ModSettings
{
    public IntRange DaysBetweenDecay = new(10, 15);
    public IntRange TechToDestroyAtATime = new(1, 1);

    public void DoWindowContents(Rect wrect)
    {
        Listing_Standard options = new();
        options.Begin(wrect);

        options.Label("EntropyFlavourPack_DaysBetweenDecay".Translate());
        options.IntRange(ref DaysBetweenDecay, 1, 30);

        options.Label("EntropyFlavourPack_TechToDestroyAtATime".Translate());
        options.IntRange(ref TechToDestroyAtATime, 1, 30);
        options.End();
    }


    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref DaysBetweenDecay, "DaysBetweenDecay", new IntRange(10, 15));
        Scribe_Values.Look(ref TechToDestroyAtATime, "TechToDestroyAtATime", new IntRange(1, 1));
    }
}
