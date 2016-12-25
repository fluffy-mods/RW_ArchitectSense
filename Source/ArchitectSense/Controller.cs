// Karel Kroeze
// Controller.cs
// 2016-12-21

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HugsLib;
using HugsLib.Utils;
using RimWorld;
using UnityEngine;
using Verse;

namespace ArchitectSense
{
    public class Controller : ModBase
    {
        #region Fields

        private static Controller _instance;

        private static FieldInfo _resolvedDesignatorsFieldInfo =
            typeof( DesignationCategoryDef ).GetField( "resolvedDesignators",
                                                       BindingFlags.NonPublic | BindingFlags.Instance );

        #endregion Fields

        #region Constructors

        public Controller() { _instance = this; }

        #endregion Constructors

        #region Properties

        public static Controller Get => _instance;
        public static ModLogger GetLogger => _instance?.Logger;
        public override string ModIdentifier => "ArchitectSense";

        #endregion Properties

        #region Methods
        public static void AddSubCategory( DesignationCategoryDef categoryDef, DesignationSubCategoryDef subcategoryDef,
                                           List<TerrainDef> terrainDefs, int position = -1 )
        {
            AddSubCategory( categoryDef, subcategoryDef, terrainDefs.Select( def => def as BuildableDef ).ToList(),
                            position );
        }

        public static void AddSubCategory( DesignationCategoryDef categoryDef, DesignationSubCategoryDef subcategoryDef,
                                           List<ThingDef> thingDefs, int position = -1 )
        {
            AddSubCategory( categoryDef, subcategoryDef, thingDefs.Select( def => def as BuildableDef ).ToList(),
                            position );
        }

        public static void AddSubCategory( DesignationCategoryDef categoryDef, DesignationSubCategoryDef subcategoryDef,
                                           List<BuildableDef> buildableDefs, int position = -1 )
        {
            // cop out on null
            if ( categoryDef == null )
                throw new ArgumentNullException( nameof( categoryDef ) );

            // get designation category's resolved designators
            List<Designator> resolvedDesignators = GetResolvedDesignators( categoryDef );

            // check position argument
            if ( position > resolvedDesignators.Count )
                throw new ArgumentOutOfRangeException( nameof( position ) );

            // create subcategory
            var subcategory = new Designator_SubCategory( subcategoryDef,
                                                          buildableDefs.Select( bd => new Designator_Build( bd ) )
                                                                       .ToList() );

            // if no position is specified, add it at the end
            if ( position < 0 )
                resolvedDesignators.Add( subcategory );
            else
                resolvedDesignators.Insert( position, subcategory );
        }

        public static List<Designator> GetResolvedDesignators( DesignationCategoryDef category )
        {
            if ( _resolvedDesignatorsFieldInfo == null )
                throw new Exception( "resolvedDesignatorsFieldInfo not found!" );

            return _resolvedDesignatorsFieldInfo.GetValue( category ) as List<Designator>;
        }

        public override void DefsLoaded()
        {
            Logger.Message( "Creating subcategories" );

            if ( Designator_SubCategoryItem.entDefFieldInfo == null )
            {
                Logger.Error( "Fetching entDef field info failed! Stopping!" );
                return;
            }

            foreach ( DesignationSubCategoryDef category in DefDatabase<DesignationSubCategoryDef>.AllDefsListForReading
                )
            {
                if ( category.debug )
                    Logger.Message( "Creating subcategory {0} in category {1}", category.LabelCap,
                                    category.designationCategory );

                // cop out if main cat not found
                if ( category.designationCategory == null )
                {
                    Logger.Warning( "Category {0} not found! Skipping.", category.designationCategory );
                    continue;
                }

                // set up sub category
                var designators = new List<Designator_Build>();

                // keep track of best position for the subcategory - it will replace the first subitem in the original category.
                int FirstDesignatorIndex = -1;

                // get list of current designators in the category
                List<Designator> resolvedDesignators = GetResolvedDesignators( category.designationCategory );

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
                            Logger.Warning(
                                           "ThingDef {0} main designationCategory doesn't match subcategory's designationCategory! Skipping.",
                                           defName );
                        continue;
                    }

                    // fetch the designator from the main category, by checking if the designators entitiyDef (entDef, protected) is the same as our current def.
                    var bdefDesignator =
                        resolvedDesignators.FirstOrDefault( des => isForDef( des as Designator_Build, bdef ) ) as
                        Designator_Build;
                    if ( category.debug && bdefDesignator == null )
                        Log.Warning( "No designator found with matching entity def! Skipping." );

                    // if not null, add designator to the subcategory, and remove from main category
                    if ( bdefDesignator != null )
                    {
                        // find index, and update FirstDesignatorIndex
                        int index = resolvedDesignators.IndexOf( bdefDesignator );
                        if ( FirstDesignatorIndex < 0 || index < FirstDesignatorIndex )
                            FirstDesignatorIndex = index;

                        designators.Add( bdefDesignator );
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
                    var subCategory = new Designator_SubCategory( category, designators );

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

            return ( Designator_SubCategoryItem.entDefFieldInfo.GetValue( des ) as BuildableDef )?.defName ==
                   def.defName;
        }

        #endregion Methods
    }
}
