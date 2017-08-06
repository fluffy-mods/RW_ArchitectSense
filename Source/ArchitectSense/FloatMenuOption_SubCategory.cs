// Karel Kroeze
// FloatMenuOption_SubCategory.cs
// 2016-12-21

using System;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace ArchitectSense
{
    internal class FloatMenuOption_SubCategory : FloatMenuOption
    {
        static readonly PropertyInfo Designator_Build_DoToolTip_PropertyInfo =
            typeof(Designator_Build).GetProperty("DoTooltip",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

        public Texture2D backgroundTexture = ContentFinder<Texture2D>.Get("UI/Widgets/DesButBG");
        public Designator_Build gizmo;
        public Color mouseOverColor = new Color(1f, 0.92f, 0.6f);


        public FloatMenuOption_SubCategory(Designator_SubCategoryItem designator,
            Action action,
            MenuOptionPriority priority = MenuOptionPriority.Default,
            Action mouseoverGuiAction = null,
            Thing revalidateClickTarget = null)
            : base(designator.LabelCap, action, priority, mouseoverGuiAction, revalidateClickTarget)
        {
            gizmo = designator;
        }

        // TODO: implement allowed logic.
        public bool Allowed => true;

        public bool DoGUI_BG(Rect rect)
        {
            bool mouseIsOver = false;
            if (Mouse.IsOver(rect))
            {
                mouseIsOver = true;
                GUI.color = GenUI.MouseoverColor;
            }
            var badTex = gizmo.icon;
            if (badTex == null)
            {
                badTex = BaseContent.BadTex;
            }
            GUI.DrawTexture(rect, Command.BGTex);

            MouseoverSounds.DoRegion(rect, SoundDefOf.MouseoverCommand);

            GUI.color = gizmo.IconDrawColor;
            Widgets.DrawTextureFitted(new Rect(rect), badTex, gizmo.iconDrawScale * 0.85f, gizmo.iconProportions,
                gizmo.iconTexCoords);
            GUI.color = Color.white;

            return mouseIsOver;
        }

        public bool DoGUI_Label(Rect rect)
        {
            bool commandIsTriggered = false;
            KeyCode keyCode = (gizmo.hotKey != null) ? gizmo.hotKey.MainKey : KeyCode.None;
            if (keyCode != KeyCode.None && !GizmoGridDrawer.drawnHotKeys.Contains(keyCode))
            {
                Rect rect2 = new Rect(rect.x + 5f, rect.y + 5f, rect.width - 10f, 18f);
                Widgets.Label(rect2, keyCode.ToStringReadable());
                GizmoGridDrawer.drawnHotKeys.Add(keyCode);
                if (gizmo.hotKey.KeyDownEvent)
                {
                    commandIsTriggered = true;
                    Event.current.Use();
                }
            }
            if (Widgets.ButtonInvisible(rect, false))
            {
                commandIsTriggered = true;
            }

            string labelCap = gizmo.LabelCap;
            if (!labelCap.NullOrEmpty())
            {
                float num = Text.CalcHeight(labelCap, rect.width);
                Rect rect3 = new Rect(rect.x, rect.yMax - num + 12f, rect.width, num);
                GUI.DrawTexture(rect3, TexUI.GrayTextBG);
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperCenter;
                Widgets.Label(rect3, labelCap);
                Text.Anchor = TextAnchor.UpperLeft;
            }

            GUI.color = Color.white;
            if ((bool) Designator_Build_DoToolTip_PropertyInfo.GetValue(gizmo, null))
            {
                TipSignal tip = gizmo.Desc;
                if (gizmo.disabled && !gizmo.disabledReason.NullOrEmpty())
                {
                    string text = tip.text;
                    tip.text = string.Concat(new string[]
                    {
                        text,
                        "\n\n",
                        "DisabledCommand".Translate(),
                        ": ",
                        gizmo.disabledReason
                    });
                }
                TooltipHandler.TipRegion(rect, tip);
            }
            if (!gizmo.HighlightTag.NullOrEmpty() && (Find.WindowStack.FloatMenu == null ||
                                                      !Find.WindowStack.FloatMenu.windowRect.Overlaps(rect)))
            {
                UIHighlighter.HighlightOpportunity(rect, gizmo.HighlightTag);
            }
            return commandIsTriggered;
        }

        public GizmoResult DoGUI_Logic(bool mouseIsOver, bool commandIsTriggered)
        {
            if (commandIsTriggered)
            {
                if (gizmo.disabled)
                {
                    if (!gizmo.disabledReason.NullOrEmpty())
                    {
                        Messages.Message(gizmo.disabledReason, MessageSound.RejectInput);
                    }
                    return new GizmoResult(GizmoState.Mouseover, null);
                }
                if (!TutorSystem.AllowAction(gizmo.TutorTagSelect))
                {
                    return new GizmoResult(GizmoState.Mouseover, null);
                }
                TutorSystem.Notify_Event(gizmo.TutorTagSelect);
                return new GizmoResult(GizmoState.Interacted, Event.current);
            }

            if (mouseIsOver)
            {
                return new GizmoResult(GizmoState.Mouseover, null);
            }

            return new GizmoResult(GizmoState.Clear, null);
        }
    }
}