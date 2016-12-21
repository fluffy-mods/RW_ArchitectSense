using System;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HugsLib;
using UnityEngine;
using Verse;

namespace ArchitectSense
{
    public class Controller : ModBase
    {
        #region Fields

        // copypasta from Designator_Build
        private static readonly Vector2 TerrainTextureCroppedSize = new Vector2(64f, 64f);

        #endregion Fields

        #region Methods

        public override void Initialize()
        {
            Logger.Message( "Creating subcategories" );

            if ( Designator_SubCategoryItem.entDefFieldInfo == null )
            {
                Logger.Error( "Fetching entDef field info failed! Stopping!" );
                return;
            }

            foreach ( DesignationSubCategoryDef category in DefDatabase<DesignationSubCategoryDef>.AllDefsListForReading )
            {
                if ( category.debug )
                    Logger.Message( "Creating subcategory {0} in category {1}", category.LabelCap, category.designationCategory );

                // cop out if main cat not found
                if ( category.designationCategory == null )
                {
                    Logger.Warning( "Category {0} not found! Skipping.", category.designationCategory );
                    continue;
                }

                // set up sub category
                List<Designator_SubCategoryItem> designators = new List<Designator_SubCategoryItem>();

                // keep track of best position for the subcategory - it will replace the first subitem in the original category.
                int FirstDesignatorIndex = -1;

                // get list of current designators in the category
                List<Designator> resolvedDesignators = GetresolvedDesignators( category.designationCategory );

                // start adding designators to it
                foreach ( string defName in category.defNames )
                {
                    BuildableDef bdef = DefDatabase<ThingDef>.GetNamedSilentFail( defName );

                    if ( bdef == null )
                    {
                        bdef = DefDatabase<TerrainDef>.GetNamedSilentFail( defName );
                    }

                    // do some common error checking
                    // buildable def exists
                    if ( bdef == null )
                    {
                        if ( category.debug )
                            Logger.Warning( "ThingDef {0} not found! Skipping.", defName );
                        continue;
                    }

                    // main designation categories match
                    if ( bdef.designationCategory != category.designationCategory )
                    {
                        if ( category.debug )
                            Logger.Warning( "ThingDef {0} main designationCategory doesn't match subcategory's designationCategory! Skipping.", defName );
                        continue;
                    }

                    // fetch the designator from the main category, by checking if the designators entitiyDef (entDef, protected) is the same as our current def.
                    Designator_Build bdefDesignator = resolvedDesignators.FirstOrDefault( des => isForDef( des as Designator_Build, bdef ) ) as Designator_Build;
                    if ( category.debug && bdefDesignator == null )
                        Log.Warning( "No designator found with matching entity def! Skipping." );

                    // if not null, add designator to the subcategory, and remove from main category
                    if ( bdefDesignator != null )
                    {
                        // find index, and update FirstDesignatorIndex
                        int index = resolvedDesignators.IndexOf( bdefDesignator );
                        if ( FirstDesignatorIndex < 0 || index < FirstDesignatorIndex )
                            FirstDesignatorIndex = index;

                        designators.Add( new Designator_SubCategoryItem( bdefDesignator ) );
                        resolvedDesignators.Remove( bdefDesignator );

                        if ( category.debug )
                            Logger.Message( "ThingDef {0} passed checks and was added to subcategory.", defName );
                    }
                    // done with this designator
                }

                // check if any designators were added to subdesignator
                if ( !designators.NullOrEmpty() )
                {
                    // create subcategory
                    Designator_SubCategory subCategory = new Designator_SubCategory();
                    subCategory.SubDesignators = designators;
                    subCategory.defaultLabel = category.label;
                    subCategory.defaultDesc = category.description;

                    // set the icon
                    if ( category.graphicData != null )
                    {
                        // use graphic in subcategory def
                        subCategory.icon = category.graphicData.Graphic.MatSingle.mainTexture as Texture2D;
                        subCategory.iconProportions = category.graphicData.drawSize;
                        subCategory.iconProportions = new Vector2( 1f, 1f );
                    }
                    else
                    {
                        // use graphic in first designator
                        BuildableDef entDef = Designator_SubCategoryItem.entDefFieldInfo.GetValue( subCategory.SubDesignators.First() ) as BuildableDef;

                        if ( entDef == null && category.debug )
                        {
                            Logger.Warning( "Failed to get def for icon automatically." );
                        }
                        else
                        {
                            subCategory.icon = entDef.uiIcon;
                            ThingDef thingDef = entDef as ThingDef;
                            if ( thingDef != null )
                            {
                                subCategory.iconProportions = thingDef.graphicData.drawSize;
                                subCategory.iconDrawScale = GenUI.IconDrawScale( thingDef );
                            }
                            else
                            {
                                subCategory.iconProportions = new Vector2( 1f, 1f );
                                subCategory.iconDrawScale = 1f;
                            }
                            if ( entDef is TerrainDef )
                                subCategory.iconTexCoords = new Rect( 0.0f, 0.0f,
                                                                      TerrainTextureCroppedSize.x /
                                                                      subCategory.icon.width,
                                                                      TerrainTextureCroppedSize.y /
                                                                      subCategory.icon.height );
                        }
                    }

                    // insert to replace first designator removed
                    // Log.Message( string.Join( ", ", resolvedDesignators.Select( d => d.LabelCap ).ToArray() ) );
                    resolvedDesignators.Insert( FirstDesignatorIndex, subCategory );

                    if ( category.debug )
                        Logger.Message( "Subcategory {0} created.", subCategory.LabelCap );
                }
                else if ( category.debug )
                {
                    Logger.Warning( "Subcategory {0} did not have any (resolved) contents! Skipping.", category.LabelCap );
                }
            }
        }

        private bool isForDef( Designator_Build des, BuildableDef def )
        {
            // we might get nulls from special designators being cast to des_build
            // in which case, reflection fails WITHOUT THROWING AN ERROR!
            if ( des == null )
                return false;
            return ( Designator_SubCategoryItem.entDefFieldInfo.GetValue( des ) as BuildableDef )?.defName == def.defName;
        }

        private static FieldInfo _resolvedDesignatorsFieldInfo =
            typeof( DesignationCategoryDef ).GetField( "resolvedDesignators",
                                                       BindingFlags.NonPublic | BindingFlags.Instance );

        public List<Designator> GetresolvedDesignators( DesignationCategoryDef category )
        {
            if (_resolvedDesignatorsFieldInfo == null )
                throw new Exception( "resolvedDesignatorsFieldInfo not found!" );
                
            return _resolvedDesignatorsFieldInfo.GetValue( category ) as List<Designator>;
        }

        #endregion Methods

        public override string ModIdentifier => "ArchtectSense";
    }
}