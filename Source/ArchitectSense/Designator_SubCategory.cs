// Karel Kroeze
// Designator_SubCategory.cs
// 2016-12-21

#if DEBUG
#define DEBUG_ICON
#endif 

using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace ArchitectSense
{
    [StaticConstructorOnStartup] // not actually needed - but suppresses warning.
    public class Designator_SubCategory : Designator
    {
        public static Vector2 SubCategoryIndicatorSize = new Vector2(16f, 16f);

        public static Texture2D SubCategoryIndicatorTexture = ContentFinder<Texture2D>.Get("UI/Icons/SubcategoryIndicator");

        private static readonly Vector2 TerrainTextureCroppedSize = new Vector2(64f, 64f);

        public readonly DesignationSubCategoryDef def;

        private Designator_SubCategoryItem _selected;

        public List<Designator_SubCategoryItem> SubDesignators = new List<Designator_SubCategoryItem>();

        public Designator_SubCategory(DesignationSubCategoryDef categoryDef, List<Designator_Build> designators)
        {
            def = categoryDef;
            SubDesignators = designators.Select(d => new Designator_SubCategoryItem(d, this)).ToList();
            defaultLabel = categoryDef.label;
            defaultDesc = categoryDef.description;
            SetIcon();
        }

        public Designator_SubCategoryItem SelectedItem
        {
            get
            {
                if (_selected == null)
                    _selected = ValidSubDesignators.First();

                return _selected;
            }
            set
            {
                _selected = value;
                SetIcon();
            }
        }

        public override Color IconDrawColor
        {
            get
            {
                if (def.graphicData != null)
                    return def.graphicData.color;

                return SelectedItem.IconDrawColor;
            }
        }

        public List<Designator_SubCategoryItem> ValidSubDesignators
        {
            // currently we're initializing in the defsLoaded HugsLib callback,
            // but this is not ideal as calls to designator.Visible require the
            // researchManager, which is unavailable until game start.
            // The fallout is that the initial icon for categories, if not set expl
            // icitly, will always be the first defs icon - regardless of wether or
            // not that item is available.
            get
            {
                return Current.Game?.researchManager != null
                           ? SubDesignators.Where(designator => designator.Visible).ToList()
                           : SubDesignators;
            }
        }

        public override bool Visible
        {
            get { return ValidSubDesignators.Count > 0; }
        }

        private void SetIcon()
        {
            if (def.graphicData != null)
            {
#if DEBUG_ICON
                Controller.Logger.Message( "using subcatdef icon" );
#endif
                // use graphic in subcategory def
                icon = def.graphicData.Graphic.MatSingle.mainTexture as Texture2D;
                iconProportions = def.graphicData.drawSize;
            }
            else
                SetDesignatorIcon();
        }

        private void SetDesignatorIcon()
        {
            // use graphic in first designator
            if (SelectedItem.PlacingDef == null && def.debug)
            {
                Controller.Logger.Warning("Failed to get def for icon automatically.");
            }
            else
            {
#if DEBUG_ICON
                Controller.Logger.Message($"using {SelectedItem.PlacingDef.defName} uiIcon");
#endif
                icon = SelectedItem.PlacingDef.uiIcon;
                var thingDef = SelectedItem.PlacingDef as ThingDef;
                if (thingDef != null)
                {
                    iconProportions = thingDef.graphicData.drawSize;
                    iconDrawScale = GenUI.IconDrawScale(thingDef);
                }
                else
                {
                    iconProportions = new Vector2(1f, 1f);
                    iconDrawScale = 1f;
                }
                if (SelectedItem.PlacingDef is TerrainDef)
                    iconTexCoords = new Rect(0.0f, 0.0f,
                                              TerrainTextureCroppedSize.x / icon.width,
                                              TerrainTextureCroppedSize.y / icon.height );
            }
        }

        public override AcceptanceReport CanDesignateCell(IntVec3 loc) { return false; }

        public override void DrawPanelReadout(ref float curY, float width)
        {
            if (SelectedItem != null)
                SelectedItem.DrawPanelReadout(ref curY, width);
        }

        public override GizmoResult GizmoOnGUI(Vector2 topLeft)
        {
            GizmoResult val = base.GizmoOnGUI(topLeft);
            if (ValidSubDesignators.Count == 1)
                return val;

            var subCategoryIndicatorRect = new Rect(topLeft.x + Width - 20f, topLeft.y + 4f, SubCategoryIndicatorSize.x,
                                                     SubCategoryIndicatorSize.y);
            GUI.DrawTexture(subCategoryIndicatorRect, SubCategoryIndicatorTexture);
            return val;
        }

        public override bool GroupsWith(Gizmo other) { return false; }

        public override void ProcessInput(Event ev)
        {
            // if only one option, immediately skip to that option's processinput, and stop further processing - for all intents and purposes it will act like a normal designator.
            if (ValidSubDesignators.Count() == 1)
            {
                ValidSubDesignators.First().ProcessInput(ev);
                return;
            }

            // otherwise, mimick the normal stuff selection by re-selecting the last 'stuff' (designator), but also showing OUR floatmenu.
            // Since our floatmenu will be called right after a possible stuff selection floatmenu, we should check if there's a stuff float menu - and close it.
            Find.WindowStack.FloatMenu?.Close(false);
            SelectedItem.ProcessInput(ev);
            ShowOptions();
        }

        private void ShowOptions()
        {
            if (def.preview)
            {
                var options = new List<FloatMenuOption_SubCategory>();
                foreach (Designator_SubCategoryItem designator in ValidSubDesignators)
                {
                    // action is handled by FloatMenuOption_SubCategory
                    // note that normally setting action to null would cause the option to be disabled, but
                    // this behaviour is defined in the OnGui method, which we're overriding anyway.
                    options.Add(new FloatMenuOption_SubCategory(designator, null));
                }

                Find.WindowStack.Add(new FloatMenu_SubCategory(options, null, new Vector2(75, 75)));
            }
            else
            {
                // if we don't have to draw preview images, we can re-use the default floatmenu
                var options = new List<FloatMenuOption>();
                foreach (Designator_SubCategoryItem designator in ValidSubDesignators)
                {
                    // TODO: Check if subdesignator is allowed (also check if this check is even needed, as !Visible is already filtered out)
                    options.Add(new FloatMenuOption(designator.LabelCap,
                                                      delegate { Find.DesignatorManager.Select(designator); }));
                }

                Find.WindowStack.Add(new FloatMenu(options, null));
            }
        }
    }
}
