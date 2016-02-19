using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;

namespace ArchitectSense
{
    class Designator_SubCategory : Designator
    {
        public List<Designator_Build> SubDesignators = new List<Designator_Build>();
        public static Texture2D SubCategoryIndicatorTexture = ContentFinder<Texture2D>.Get( "UI/Icons/SubcategoryIndicator" );
        public static Vector2 SubCategoryIndicatorSize = new Vector2( 16f, 16f );
        
        public override AcceptanceReport CanDesignateCell( IntVec3 loc )
        {
            return false;
        }

        public override bool GroupsWith( Gizmo other )
        {
            return false;
        }

        public bool useDefaultIcon;

        private PropertyInfo _iconDrawColorPropertyInfo = typeof (Designator_Build).GetProperty( "IconDrawColor",
                                                                                                 BindingFlags.NonPublic |
                                                                                                 BindingFlags.Instance );

        protected override Color IconDrawColor
        {
            get
            {
                if ( useDefaultIcon )
                    return Color.white;
                return (Color)_iconDrawColorPropertyInfo.GetValue( SubDesignators.First(), null );
            }
        }

        public override GizmoResult GizmoOnGUI( Vector2 topLeft )
        {
            GizmoResult val = base.GizmoOnGUI( topLeft );
            Rect subCategoryIndicatorRect = new Rect( topLeft.x + this.Width - 20f, topLeft.y + 4f, SubCategoryIndicatorSize.x, SubCategoryIndicatorSize.y );
            GUI.DrawTexture( subCategoryIndicatorRect, SubCategoryIndicatorTexture );
            return val;
        }

        public override void ProcessInput( Event ev )
        {
            List<FloatMenuOption_SubCategory> options = new List<FloatMenuOption_SubCategory>();
            foreach ( Designator_Build designator in SubDesignators.Where( designator => designator.Visible ) )
            {
                options.Add( new FloatMenuOption_SubCategory( designator.LabelCap, delegate
                {
                    designator.ProcessInput( ev );
                }, designator ) );
            }
            Find.WindowStack.Add( new FloatMenu_SubCategory( options, null, new Vector2( 75, 75) ) );
        }
    }
}
