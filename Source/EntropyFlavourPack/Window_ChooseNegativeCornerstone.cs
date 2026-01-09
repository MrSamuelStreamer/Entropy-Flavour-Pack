using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using VFETribals;

namespace EntropyFlavourPack
{
    public class Window_ChooseNegativeCornerstone : Window
    {
        private readonly List<CornerstoneDef> choices;
        private readonly Action<CornerstoneDef> onChosen;
        private Vector2 scrollPos;
        private float scrollHeight = 100000f;

        private const float WindowWidth = 700f;
        private const float WindowHeight = 700f;
        private const float ChoiceHeight = 50f;
        private const float ChoiceMargin = 15f;

        public override Vector2 InitialSize => new Vector2(WindowWidth, WindowHeight);

        public Window_ChooseNegativeCornerstone(List<CornerstoneDef> choices, Action<CornerstoneDef> onChosen)
        {
            this.choices = choices;
            this.onChosen = onChosen;
            this.forcePause = true;
            this.absorbInputAroundWindow = true;
            this.closeOnClickedOutside = false;
            this.closeOnAccept = false;
            this.closeOnCancel = false;
            this.doCloseButton = false;
            this.doCloseX = false;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Vector2 pos = new Vector2(15f, 0f);

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(pos.x, pos.y, inRect.width - pos.x, 32f), "EntropyFlavourPack_TechRegressionTitle".Translate());
            pos.y += 40f;

            Color originalColor = GUI.color;
            string factionName = Faction.OfPlayer.Name;
            float nameWidth = Text.CalcSize(factionName).x;
            Rect iconRect = new Rect(inRect.width / 2f - (40f + nameWidth + 15f) / 2f, pos.y + 1f, 40f, 40f);
            GUI.color = Faction.OfPlayer.Color;
            GUI.DrawTexture(iconRect, Faction.OfPlayer.def.FactionIcon);
            GUI.color = originalColor;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(new Rect(iconRect.xMax + 15f, iconRect.y, nameWidth, iconRect.height), factionName);
            Text.Anchor = TextAnchor.UpperLeft;
            pos.y += 50f;

            Text.Font = GameFont.Small;
            GUI.color = Color.gray;
            Widgets.Label(new Rect(pos.x, pos.y, inRect.width - 30f, 40f), "EntropyFlavourPack_TechRegressionDesc".Translate());
            GUI.color = Color.white;
            pos.y += 50f;

            Widgets.Label(new Rect(pos.x, pos.y, 200f, 24f), "EntropyFlavourPack_NegativeCornerstones".Translate());
            pos.y += 24f;

            Rect viewRect = new Rect(pos.x, pos.y, inRect.width - 16f - 15f, scrollHeight);
            Rect outRect = new Rect(pos.x, pos.y, inRect.width - 15f, 445f);
            scrollHeight = 0f;

            Widgets.BeginScrollView(outRect, ref scrollPos, viewRect);
            Vector2 listPos = pos;
            CornerstoneDef chosenDef = null;

            foreach (CornerstoneDef cornerstone in choices)
            {
                bool alreadyHas = GameComponent_Tribals.Instance?.cornerstones.Contains(cornerstone) ?? false;
                Rect choiceRect = new Rect(listPos.x, listPos.y, viewRect.width, ChoiceHeight);

                Widgets.DrawMenuSection(choiceRect);
                if (alreadyHas)
                {
                    Widgets.DrawHighlightSelected(choiceRect);
                }

                Text.Font = GameFont.Small;
                Rect labelRect = new Rect(listPos.x + 5f, listPos.y + 3f, viewRect.width - 210f, ChoiceHeight - 24f);
                GUI.color = new Color(0.9f, 0.5f, 0.5f);
                Widgets.Label(labelRect, cornerstone.label.CapitalizeFirst());
                GUI.color = Color.white;

                Text.Font = GameFont.Tiny;
                Widgets.Label(new Rect(labelRect.x, labelRect.yMax, labelRect.width, 24f), cornerstone.description);

                Text.Font = GameFont.Small;
                if (!alreadyHas && Mouse.IsOver(choiceRect))
                {
                    Rect buttonRect = new Rect(choiceRect.width - 200f, choiceRect.y + 10f, 200f, choiceRect.height - 20f);
                    if (Widgets.ButtonText(buttonRect, "EntropyFlavourPack_AcceptPenalty".Translate()))
                    {
                        chosenDef = cornerstone;
                    }
                }

                listPos.y += ChoiceHeight + ChoiceMargin;
                scrollHeight += ChoiceHeight + ChoiceMargin;
            }

            if (chosenDef != null)
            {
                SoundDefOf.Click.PlayOneShotOnCamera();
                onChosen?.Invoke(chosenDef);
                Close();
            }

            Widgets.EndScrollView();

            Text.Font = GameFont.Small;
            Rect footerRect = new Rect(0f, outRect.yMax + 10f, inRect.width, 40f);
            GUI.color = Color.gray;
            Widgets.Label(footerRect, "EntropyFlavourPack_MustChoosePenalty".Translate());
            GUI.color = Color.white;
        }
    }
}
