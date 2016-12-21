// Karel Kroeze
// Designator_SubCategory.cs
// 2016-12-21

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;

namespace ArchitectSense
{
    [StaticConstructorOnStartup] // not actually needed - but suppresses warning.
    internal class Designator_SubCategory : Designator
    {
        #region Fields

        public static Vector2 SubCategoryIndicatorSize = new Vector2( 16f, 16f );
        public static Texture2D SubCategoryIndicatorTexture =
            ContentFinder<Texture2D>.Get( "UI/Icons/SubcategoryIndicator" );
        public List<Designator_SubCategoryItem> SubDesignators = new List<Designator_SubCategoryItem>();
        
        #endregion Fields

        #region Properties

        public List<Designator_SubCategoryItem> ValidSubDesignators
        {
            get { return SubDesignators.Where( designator => designator.Visible ).ToList(); }
        }

        public override bool Visible
        {
            get { return ValidSubDesignators.Count > 0; }
        }

        public override Color IconDrawColor => SubDesignators.First().IconDrawColor;

        #endregion Properties

        #region Methods

        public override AcceptanceReport CanDesignateCell( IntVec3 loc ) { return false; }

        public override GizmoResult GizmoOnGUI( Vector2 topLeft )
        {
            GizmoResult val = base.GizmoOnGUI( topLeft );
            if ( ValidSubDesignators.Count == 1 )
                return val;

            var subCategoryIndicatorRect = new Rect( topLeft.x + Width - 20f, topLeft.y + 4f, SubCategoryIndicatorSize.x,
                                                     SubCategoryIndicatorSize.y );
            GUI.DrawTexture( subCategoryIndicatorRect, SubCategoryIndicatorTexture );
            return val;
        }

        public override bool GroupsWith( Gizmo other ) { return false; }

        public override void ProcessInput( Event ev )
        {
            // if only one option, immediately skip to that option's processinput
            if ( ValidSubDesignators.Count() == 1 )
            {
                ValidSubDesignators.First().ProcessInput( ev );
                return;
            }

            var options = new List<FloatMenuOption_SubCategory>();
            foreach ( Designator_Build designator in ValidSubDesignators )
            {
                options.Add( new FloatMenuOption_SubCategory( designator.LabelCap,
                                                              delegate { designator.ProcessInput( ev ); }, designator ) );
            }

            Find.WindowStack.Add( new FloatMenu_SubCategory( options, null, new Vector2( 75, 75 ) ) );
        }

        #endregion Methods
    }
}
