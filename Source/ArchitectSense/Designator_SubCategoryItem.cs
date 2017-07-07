// Karel Kroeze
// Designator_SubCategoryItem.cs
// 2016-12-21

#if DEBUG
#define DEBUG_COSTLIST
#endif

using System;
using System.Linq;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace ArchitectSense
{
    public class Designator_SubCategoryItem : Designator_Build
    {
        private Designator_SubCategory subCategory;

        // default constructor from ThingDef, forwarded to base.
        public Designator_SubCategoryItem( ThingDef entDef, Designator_SubCategory subCategory) : base(entDef)
        {
            this.subCategory = subCategory;
        }

        // constructor from Designator_Build, links to constructor from ThingDef.
        public Designator_SubCategoryItem( Designator_Build designator, Designator_SubCategory subCategory )
            : base( designator.PlacingDef )
        {
            this.subCategory = subCategory;
        }

        public override bool Visible
        {
            get
            {
                if (DebugSettings.godMode)
                    return true;

                return PrerequisitesSatisfied && MaterialsAvailable;
            }
        }

        public bool PrerequisitesSatisfied => base.Visible;

        public bool MaterialsAvailable
        {
            get
            {
                if (DebugSettings.godMode)
                    return true;

                if (subCategory.def.emulateStuff)
                {
                    // note that for emulating stuff, we're assuming the item doesn't _actually_ have a stuff.
                    foreach ( ThingCountClass tc in entDef.costList )
                        if (Map.listerThings.ThingsOfDef(tc.thingDef).Count == 0)
                            return false;
                }

                return true;
            }
        }

        public override GizmoResult GizmoOnGUI(Vector2 topLeft)
        {
            // start GUI.color is the transparency set by our floatmenu parent
            // store it, so we can apply it to all subsequent colours
            Color transparency = GUI.color;

            // below is 99% copypasta from Designator_Build, with minor naming changes and taking account of transparency.
            var buttonRect = new Rect(topLeft.x, topLeft.y, Width, 75f);
            var mouseover = false;
            if (Mouse.IsOver(buttonRect))
            {
                mouseover = true;
                GUI.color = GenUI.MouseoverColor * transparency;
            }
            Texture2D tex = icon;
            if (tex == null)
                tex = BaseContent.BadTex;
            GUI.DrawTexture(buttonRect, BGTex);
            MouseoverSounds.DoRegion(buttonRect, SoundDefOf.MouseoverCommand);
            GUI.color = IconDrawColor * transparency;
            Widgets.DrawTextureFitted(new Rect(buttonRect), tex, iconDrawScale * 0.85f, iconProportions,
                                       iconTexCoords);
            GUI.color = Color.white * transparency;
            var clicked = false;
            KeyCode keyCode = hotKey != null ? hotKey.MainKey : KeyCode.None;
            if (keyCode != KeyCode.None && !GizmoGridDrawer.drawnHotKeys.Contains(keyCode))
            {
                Widgets.Label(new Rect(buttonRect.x + 5f, buttonRect.y + 5f, 16f, 18f), keyCode.ToString());
                GizmoGridDrawer.drawnHotKeys.Add(keyCode);
                if (hotKey.KeyDownEvent)
                {
                    clicked = true;
                    Event.current.Use();
                }
            }
            if (Widgets.ButtonInvisible(buttonRect))
                clicked = true;
            string labelCap = LabelCap;
            if (!labelCap.NullOrEmpty())
            {
                float height = Text.CalcHeight(labelCap, buttonRect.width) - 2f;
                var rect2 = new Rect(buttonRect.x, (float)(buttonRect.yMax - (double)height + 12.0),
                                      buttonRect.width, height);
                GUI.DrawTexture(rect2, TexUI.GrayTextBG);
                GUI.color = Color.white * transparency;
                Text.Anchor = TextAnchor.UpperCenter;
                Widgets.Label(rect2, labelCap);
                Text.Anchor = TextAnchor.UpperLeft;
            }
            GUI.color = Color.white;
            if (DoTooltip)
            {
                TipSignal tip = Desc;
                if (disabled && !disabledReason.NullOrEmpty())
                {
                    TipSignal local = tip;
                    local.text += "\n\nDISABLED: " + disabledReason;
                }
                TooltipHandler.TipRegion(buttonRect, tip);
            }
            // TODO: Reimplement tutor.
            //if( !tutorHighlightTag.NullOrEmpty() )
            //    TutorUIHighlighter.HighlightOpportunity( tutorHighlightTag, buttonRect );

            if (clicked)
            {
                if (!disabled)
                    return new GizmoResult(GizmoState.Interacted, Event.current);

                if (!disabledReason.NullOrEmpty())
                    Messages.Message(disabledReason, MessageSound.RejectInput);
                return new GizmoResult(GizmoState.Mouseover, null);
            }

            if (mouseover)
                return new GizmoResult(GizmoState.Mouseover, null);

            return new GizmoResult(GizmoState.Clear, null);
        }

        private FieldInfo stuffDefFieldInfo = typeof( Designator_Build ).GetField( "stuffDef",
                                                                                   BindingFlags.Instance |
                                                                                   BindingFlags.NonPublic );
        public ThingDef StuffDef
        {
            get
            {
                if ( stuffDefFieldInfo == null )
                    throw new NullReferenceException( "stuffDef field info NULL " );

                return stuffDefFieldInfo.GetValue( this ) as ThingDef;
            }
        }

        public override void Selected()
        {
            base.Selected();
            subCategory.SelectedItem = this;

#if DEBUG_COSTLIST
            // var costs = PlacingDef.CostListAdjusted(StuffDef).Select(c => c.thingDef.defName + ": " + c.count).ToArray();
            var costs = entDef.costList.Select(c => c.thingDef.defName + ": " + c.count).ToArray();
            Log.Message( Label + ": \n" + String.Join( "\n", costs ) );
#endif
        }
    }
}
