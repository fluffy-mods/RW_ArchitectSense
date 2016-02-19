﻿using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CommunityCoreLibrary;
using Verse;
using RimWorld;
using UnityEngine;

namespace ArchitectSense
{
    public class Bootstrap : SpecialInjector
    {

        // copypasta from Designator_Build
        private static readonly Vector2 TerrainTextureCroppedSize = new Vector2(64f, 64f);

        private bool isForDef( Designator_Build des, BuildableDef def )
        {
            // we might get nulls from special designators being cast to des_build
            // in which case, reflection fails WITHOUT THROWING AN ERROR!
            if ( des == null )
                return false;
            return ( Designator_SubCategoryItem.entDefFieldInfo.GetValue( des ) as BuildableDef)?.defName == def.defName;
        }

        public override void Inject()
        {
            Log.Message( "ArchitectSense :: Creating subcategories" );

            if ( Designator_SubCategoryItem.entDefFieldInfo == null )
            {
                Log.Error( "ArchitectSense :: Fetching entDef field info failed! Stopping!" );
                return;
            }

            foreach ( DesignationSubCategoryDef def in DefDatabase<DesignationSubCategoryDef>.AllDefsListForReading )
            {
                if( def.debug ) 
                    Log.Message( "ArchitectSense :: Creating subcategory " + def.LabelCap + " in category " + def.designationCategory );

                // cop out if main cat not found
                DesignationCategoryDef mainCategoryDef =
                    DefDatabase<DesignationCategoryDef>.GetNamedSilentFail( def.designationCategory );
                if ( mainCategoryDef == null )
                {
                    Log.Warning( "ArchitectSense :: Category " + def.designationCategory + " not found! Skipping." );
                    continue;
                }
                
                // set up sub category
                List<Designator_Build> designators = new List<Designator_Build>();
                int subCategoryIndex = - 1;

                // start adding designators to it
                foreach ( string defName in def.defNames )
                {
                    BuildableDef bdef = DefDatabase<ThingDef>.GetNamedSilentFail( defName );
                    
                    if( bdef == null )
                    {
                        bdef = DefDatabase<TerrainDef>.GetNamedSilentFail( defName );
                    }

                    // do some common error checking
                    // buildable def exists
                    if( bdef == null )
                    {
                        if( def.debug )
                            Log.Warning( "ArchitectSense :: ThingDef " + defName + " not found! Skipping." );
                        continue;
                    }

                    // main designation categories match
                    if( bdef.designationCategory != def.designationCategory )
                    {
                        if( def.debug )
                            Log.Warning( "ArchitectSense :: ThingDef " + defName + " main designationCategory doesn't match subcategory's designationCategory! Skipping." );
                        continue;
                    }

                    // fetch the designator from the main category, by checking if the designators entitiyDef (entDef, protected) is the same as our current def.
                    Designator_Build bdefDesignator = mainCategoryDef.resolvedDesignators.FirstOrDefault(des => isForDef( des as Designator_Build, bdef )) as Designator_Build;
                    if (def.debug && bdefDesignator == null)
                        Log.Warning( "No designator found with matching entity def! Skipping." );

                    // if not null, add designator to the subcategory, and remove from main category
                    if( bdefDesignator != null )
                    {
                        // first, set the insert position of the subcategory to the first designator found
                        if ( subCategoryIndex < 0 )
                            subCategoryIndex = mainCategoryDef.resolvedDesignators.IndexOf( bdefDesignator );

                        designators.Add( new Designator_SubCategoryItem( bdefDesignator ) );
                        mainCategoryDef.resolvedDesignators.Remove( bdefDesignator );

                        if( def.debug )
                            Log.Message( "ArchitectSense :: ThingDef " + defName + " passed checks and was added to subcategory." );
                    }
                    // done with this designator
                }

                // check if any designators were added to subdesignator
                if( !designators.NullOrEmpty() )
                {
                    // create subcategory
                    Designator_SubCategory subCategory = new Designator_SubCategory();
                    subCategory.SubDesignators = designators;
                    subCategory.defaultLabel = def.label;
                    subCategory.defaultDesc = def.description;

                    // set the icon
                    if ( def.graphicData != null )
                    {
                        // use graphic in subcategory def
                        subCategory.icon = def.graphicData.Graphic.MatSingle.mainTexture as Texture2D;
                        subCategory.iconProportions = def.graphicData.drawSize;
                        subCategory.iconProportions = new Vector2( 1f, 1f );
                    }
                    else
                    {
                        // use graphic in first designator
                        BuildableDef entDef = Designator_SubCategoryItem.entDefFieldInfo.GetValue( subCategory.SubDesignators.First() ) as BuildableDef;

                        if ( entDef == null && def.debug )
                        {
                            Log.Warning( "ArchitectSense :: Failed to get def for icon automatically." );
                        }
                        else
                        {
                            subCategory.icon = entDef.uiIcon;
                            ThingDef thingDef = entDef as ThingDef;
                            if( thingDef != null )
                            {
                                subCategory.iconProportions = thingDef.graphicData.drawSize;
                                subCategory.iconDrawScale = GenUI.IconDrawScale( thingDef );
                            }
                            else
                            {
                                subCategory.iconProportions = new Vector2( 1f, 1f );
                                subCategory.iconDrawScale = 1f;
                            }
                            if( entDef is TerrainDef )
                                subCategory.iconTexCoords = new Rect( 0.0f, 0.0f,
                                                                      TerrainTextureCroppedSize.x /
                                                                      subCategory.icon.width,
                                                                      TerrainTextureCroppedSize.y /
                                                                      subCategory.icon.height );
                        }
                    }
                    
                    // insert at location where first designator used to be.
                    mainCategoryDef.resolvedDesignators.Insert(subCategoryIndex, subCategory);


                    if( def.debug)
                        Log.Message( "ArchitectSense :: Subcategory " + subCategory.LabelCap + " created." );
                } else if ( def.debug )
                {
                    Log.Warning( "ArchitectSense :: Subcategory " + def.LabelCap + " did not have any (resolved) contents! Skipping." );
                }
            }
        }
    }
}
