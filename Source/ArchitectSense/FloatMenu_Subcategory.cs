// Karel Kroeze
// FloatMenu_Subcategory.cs
// 2016-12-21

using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace ArchitectSense
{
    internal class FloatMenu_SubCategory : FloatMenu
    {
        #region Constructors

        /// <summary>
        /// Constructor for a floatmenu with configurable sizes, icons and background textures.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="title"></param>
        /// <param name="optionSize"></param>
        /// <param name="iconSize"></param>
        /// <param name="closeOnSelection"></param>
        public FloatMenu_SubCategory( List<FloatMenuOption_SubCategory> options,
                                      string title,
                                      Vector2 optionSize,
                                      bool closeOnSelection = false )
            : base( options.Select( opt => opt as FloatMenuOption ).ToList(), title )
        {
            _options = options;
            _optionSize = optionSize;
            _closeOnSelection = closeOnSelection;
            preventCameraMotion = false;

            // need to redo column and size calculations because base isn't aware of our configurable sizes.
            // copypasta from base..ctor

            // set number of columns so the total height fits within game screen
            numColumns = 0;
            do
            {
                ++numColumns;
            } while ( TotalHeight > Screen.height * .9 );

            windowRect.size = InitialSize;

            // first off, move float so it goes up from the mouse click instead of down
            windowRect.y -= windowRect.height;

            // tweak rect position to fit within window
            // note: we're assuming up, then right placement of buttons now.
            if ( windowRect.xMax > (double) Screen.width )
                windowRect.x = Screen.width - windowRect.width;
            if ( windowRect.yMin < 0f )
                windowRect.y -= windowRect.yMin;
            if ( windowRect.yMax > (double) Screen.height )
                windowRect.y = Screen.height - windowRect.height;
        }

        #endregion Constructors

        #region Methods

        struct OptionValues
        {
            public Rect DrawArea;
            public bool mouseIsOver;
            public bool commandIsTriggered;
        }

        public override void DoWindowContents( Rect canvas )
        {
            // define our own implementation, mostly copy-pasta with a few edits for option sizes
            // actual drawing is handled in OptionOnGUI.
            UpdateBaseColor();
            GUI.color = baseColor;
            Vector2 listRoot = ListRoot;
            Text.Font = GameFont.Small;
            var row = 0;
            var col = 0;

            if ( _options.NullOrEmpty() )
                return;

            Text.Font = GameFont.Tiny;

            var sortedOptions = _options.OrderByDescending (op => op.Priority);

            var optionValuesArray = new OptionValues[ _options.Count ];

            for (int i = 0; i < _options.Count; i++) {
                var option = sortedOptions.ElementAt(i);

                float posX = listRoot.x + col * (_optionSize.x + _margin);
                float posY = listRoot.y + row * (_optionSize.y + _margin);

                optionValuesArray [i].DrawArea = new Rect (posX, posY, option.gizmo.Width, 75f);

                GUI.color = baseColor;

                optionValuesArray [i].mouseIsOver = option.DoGUI_BG (optionValuesArray [i].DrawArea);

                row++;
                if (row >= ColumnMaxOptionCount) {
                    row = 0;
                    col++;
                }
            }

            for (int i = 0; i < _options.Count; i++) {
                var option = sortedOptions.ElementAt (i);

                GUI.color = baseColor;

                optionValuesArray [i].commandIsTriggered = option.DoGUI_Label (optionValuesArray [i].DrawArea);
            }

            for (int i = 0; i < _options.Count; i++) {
                var option = sortedOptions.ElementAt (i);

                var logic = option.DoGUI_Logic (optionValuesArray [i].mouseIsOver, optionValuesArray [i].commandIsTriggered);
                if (logic.State == GizmoState.Interacted) {
                    option.gizmo.ProcessInput (logic.InteractEvent);
                }

                if (_closeOnSelection && Widgets.ButtonInvisible (optionValuesArray [i].DrawArea)) {
                    Find.WindowStack.TryRemove (this, true);
                }
            }

            GUI.color = Color.white;
            Text.Font = GameFont.Small;
        }

        #endregion Methods

        #region Fields

        private const float _margin = 5f;
        private bool _closeOnSelection;
        private List<FloatMenuOption_SubCategory> _options;
        private Vector2 _optionSize;

        // copypasta from base (privates, ugh)
        private Color baseColor;

        private int numColumns;

        #endregion Fields

        #region copypasta from Verse.FloatMenu

        public override Vector2 InitialSize
        {
            get
            {
                if ( _options.NullOrEmpty() )
                    return new Vector2( 0.0f, 0.0f );

                return new Vector2( TotalWidth, TotalHeight );
            }
        }

        private float ColumnMaxOptionCount
        {
            get
            {
                if ( options.Count % numColumns == 0 )
                    return options.Count / numColumns;

                return options.Count / numColumns + 1;
            }
        }

        private Vector2 ListRoot
        {
            get { return new Vector2( 4f, 0.0f ); }
        }

        private Rect OverRect
        {
            get { return new Rect( ListRoot.x, ListRoot.y, TotalWidth, TotalHeight ); }
        }

        private float TotalHeight
        {
            get
            {
                // the base constructor miscounts the number of options per column, overestimating it by one.
                return ColumnMaxOptionCount * ( _optionSize.y + _margin );
            }
        }

        private float TotalWidth
        {
            get { return numColumns * ( _optionSize.x + _margin ); }
        }

        private void UpdateBaseColor()
        {
            baseColor = Color.white;
            if ( !vanishIfMouseDistant )
                return;

            Rect r = OverRect.ContractedBy( -12f );
            if ( r.Contains( Event.current.mousePosition ) )
                return;

            float distanceFromRect = GenUI.DistFromRect( r, Event.current.mousePosition );
            baseColor = new Color( 1f, 1f, 1f, (float) ( 1.0 - distanceFromRect / 200.0 ) );
            if ( distanceFromRect <= 200.0 )
                return;

            Close( false );
            Cancel();
        }

        #endregion copypasta from Verse.FloatMenu
    }
}
