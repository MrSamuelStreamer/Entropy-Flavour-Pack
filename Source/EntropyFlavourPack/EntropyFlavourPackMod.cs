using UnityEngine;
using Verse;

namespace EntropyFlavourPack
{

    public class EntropyFlavourPackMod : Mod
    {
        public static Settings settings;

        public EntropyFlavourPackMod(ModContentPack content) : base(content)
        {
            // initialize settings
            settings = GetSettings<Settings>();

        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            base.DoSettingsWindowContents(inRect);
            settings.DoWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return "EntropyFlavourPack_SettingsCategory".Translate();
        }
    }
}

