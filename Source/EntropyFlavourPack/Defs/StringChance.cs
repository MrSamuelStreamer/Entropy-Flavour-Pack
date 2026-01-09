using Verse;

namespace EntropyFlavourPack.Defs;

public class StringChance: IExposable
{
    public string key;
    public float weight;

    public void ExposeData()
    {
        Scribe_Values.Look(ref key, "key");
        Scribe_Values.Look(ref weight, "weight", 1f);
    }
}
