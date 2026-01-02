using RimWorld;
using Verse;

namespace EntropyFlavourPack
{
    [DefOf]
    public static class EntropyFlavourPackDefOf
    {
        [MayRequire("chaoticenrico.researchpapers")]
        public static ThingDef ResearchPaper;

        static EntropyFlavourPackDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(EntropyFlavourPackDefOf));
        }
    }
}

