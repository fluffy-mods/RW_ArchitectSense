// ArchitectSense/FloatMenuOption_SubCategory.cs
//
// Copyright Karel Kroeze, 2016.
//
// Created 2016-02-15 23:59

using RimWorld;
using System;
using UnityEngine;
using Verse;

namespace ArchitectSense
{
    internal class FloatMenuOption_SubCategory : FloatMenuOption
    {
        #region Fields

        public Texture2D backgroundTexture = ContentFinder<Texture2D>.Get( "UI/Widgets/DesButBG" );
        public Designator_Build gizmo;
        public Color mouseOverColor = new Color( 1f, 0.92f, 0.6f );

        #endregion Fields

        #region Constructors

        public FloatMenuOption_SubCategory( Designator_SubCategoryItem designator,
                                            Action action,
                                            MenuOptionPriority priority = MenuOptionPriority.Default,
                                            Action mouseoverGuiAction = null,
                                            Thing revalidateClickTarget = null )
            : base( designator.LabelCap, action, priority, mouseoverGuiAction, revalidateClickTarget )
        {
            this.gizmo = designator;
        }

        #endregion Constructors

        #region Properties

        // TODO: implement allowed logic.
        public bool Allowed => true;

        #endregion Properties

        #region Methods

        public override bool DoGUI( Rect rect, bool pawnOrder )
        {
            // considering we're trying to recreate the gizmo look and feel, we might as well steal the Gizmo.OnGUI
            GizmoResult x = gizmo.GizmoOnGUI( rect.position );
            if ( x.State == GizmoState.Interacted )
                gizmo.ProcessInput( x.InteractEvent );

            // return clicks.
            return Widgets.ButtonInvisible( rect );
        }

        #endregion Methods
    }
}
