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
        /// <summary>
        /// Creates a subcategory based on subcategoryDef in categoryDef at position, containing elements terrainDefs.
        /// Position defaults to adding on the right. 
        /// 
        /// Note that if designators for the terrains in terrainDefs already exist, they will NOT be removed - this method
        /// creates NEW designators, and is primarily meant for mods that programatically generate defs.
        /// </summary>
        /// <param name="categoryDef"></param>
        /// <param name="subcategoryDef"></param>
        /// <param name="terrainDefs"></param>
        /// <param name="position"></param>
        public static void AddSubCategory( DesignationCategoryDef categoryDef, DesignationSubCategoryDef subcategoryDef,
                                           List<TerrainDef> terrainDefs, int position = -1 )
        {
            AddSubCategory( categoryDef, subcategoryDef, terrainDefs.Select( def => def as BuildableDef ).ToList(),
                            position );
        }

        /// <summary>
        /// Creates a subcategory based on subcategoryDef in categoryDef at position, containing elements thingDefs.
        /// Position defaults to adding on the right. 
        /// 
        /// Note that if designators for the terrains in thingDefs already exist, they will NOT be removed - this method
        /// creates NEW designators, and is primarily meant for mods that programatically generate defs.
        /// </summary>
        /// <param name="categoryDef"></param>
        /// <param name="subcategoryDef"></param>
        /// <param name="thingDefs"></param>
        /// <param name="position"></param>
        public static void AddSubCategory( DesignationCategoryDef categoryDef, DesignationSubCategoryDef subcategoryDef,
                                           List<ThingDef> thingDefs, int position = -1 )
        {
            AddSubCategory( categoryDef, subcategoryDef, thingDefs.Select( def => def as BuildableDef ).ToList(),
                            position );
        }

        /// <summary>
        /// Creates a subcategory based on subcategoryDef in categoryDef at position, containing elements buildableDefs.
        /// Position defaults to adding on the right. 
        /// 
        /// Note that if designators for the terrains in buildableDefs already exist, they will NOT be removed - this method
        /// creates NEW designators, and is primarily meant for mods that programatically generate defs.
        /// </summary>
        /// <param name="categoryDef"></param>
        /// <param name="subcategoryDef"></param>
        /// <param name="buildableDefs"></param>
        /// <param name="position"></param>
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
            if (category == null)
                throw new ArgumentNullException( nameof( category ) );

            var res = _resolvedDesignatorsFieldInfo.GetValue( category ) as List<Designator>;
            if (res == null)
                throw new Exception( "Resolved designators for " + category.defName + " is NULL!" );

            return res;
        }

        public static Dictionary<BuildableDef, Designator_Build> _designators = new Dictionary<BuildableDef, Designator_Build>();

        public static void HideDesignator( Designator_Build des, DesignationCategoryDef cat = null )
        {
            // get the entity def
            // first check our field info
            if ( Designator_SubCategoryItem.entDefFieldInfo == null )
                throw new Exception( "Could not get Designator_Build.entDef FieldInfo." );
            // get the def
            BuildableDef def = Designator_SubCategoryItem.entDefFieldInfo.GetValue( des ) as BuildableDef;
            // check for null
            if ( def == null )
                throw new Exception( $"Tried to hide designator with NULL entDef ({des.Label}). Such designators should not exist." );

            // if category wasn't explicitly set, assume it is the same as the def's
            if ( cat == null )
                cat = def.designationCategory;
            // if still null, there's nothing to hide.
            if ( cat == null )
                throw new Exception( $"Tried to hide designator from NULL category. That makes little sense." );

            // make sure the designator is cached so we can still get it later
            if (!_designators.ContainsKey( def ))
                _designators.Add( def, des );

            // get the categories designators
            var resolved = GetResolvedDesignators( cat );

            // remove our designator if it was in there, throw a warning if it was not
            if ( resolved.Contains( des ) )
                resolved.Remove( des );
            else 
                Log.Warning( $"Tried to remove designator {des.Label} from category {cat.label}, but it was not included in the categories' resolved designators."  );
        }

        public static Designator_Build GetDesignator( BuildableDef def )
        {
            Designator_Build result;
            
            // try get from cache
            if ( _designators.TryGetValue( def, out result ) )
                return result;

            // find the designator
            DesignationCategoryDef dump;
            result = FindDesignator( def, out dump );

            // did we get anything? If not, create it.
            if ( result == null )
                result = new Designator_Build( def );

            // cache it
            _designators.Add( def, result );

            return result;
        }

        private static Designator_Build FindDesignator( BuildableDef def, out DesignationCategoryDef cat_out )
        {
            // cycle through all categories to try and find our designator
            foreach ( DesignationCategoryDef cat in DefDatabase<DesignationCategoryDef>.AllDefsListForReading )
            {
                // check vanilla designators
                foreach ( Designator_Build des in GetResolvedDesignators( cat ).OfType<Designator_Build>() )
                {
                    if ( isForDef( des, def ) )
                    {
                        cat_out = cat;
                        return des;
                    }
                }

                // check our designation subcategories
                foreach (
                    Designator_SubCategory subcat in GetResolvedDesignators( cat ).OfType<Designator_SubCategory>() )
                {
                    foreach ( Designator_SubCategoryItem subdes in subcat.SubDesignators )
                    {
                        if ( isForDef( subdes, def ) )
                        {
                            cat_out = cat;
                            return subdes;
                        }
                    }
                }
            }

            cat_out = null;
            return null;
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
                int firstDesignatorIndex = -1;

                // get list of current designators in the category
                List<Designator> resolvedDesignators = GetResolvedDesignators( category.designationCategory );

                // start adding designators to the subcategory
                if ( category.defNames != null )
                    foreach ( string defName in category.defNames )
                    {
                        BuildableDef bdef = DefDatabase<ThingDef>.GetNamedSilentFail( defName ) ??
                                            (BuildableDef) DefDatabase<TerrainDef>.GetNamedSilentFail( defName );

                        // do some common error checking
                        // buildable def exists
                        if ( bdef == null )
                        {
                            if ( category.debug )
                                Logger.Warning( "ThingDef {0} not found! Skipping.", defName );
                            continue;
                        }
                        
                        // find the designator for this buildabledef
                        DesignationCategoryDef designatorCategory;
                        var bdefDesignator = FindDesignator( bdef, out designatorCategory );
                        if ( category.debug && bdefDesignator == null )
                            Log.Warning( "No designator found with matching entity def! Skipping." );

                        // if not null, add designator to the subcategory, and remove from main category
                        if ( bdefDesignator != null )
                        {
                            // if taken designator was in the same category as the new subcategory, find index and update FirstDesignatorIndex
                            if ( designatorCategory == category.designationCategory )
                            {
                                int index = resolvedDesignators.IndexOf( bdefDesignator );
                                if ( firstDesignatorIndex < 0 || index < firstDesignatorIndex )
                                    firstDesignatorIndex = index;
                            }

                            designators.Add( bdefDesignator );
                            HideDesignator( bdefDesignator );

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

                    // insert to replace first designator removed, or just add at the end if taken from different categories
                    if (firstDesignatorIndex >= 0)
                        resolvedDesignators.Insert( firstDesignatorIndex, subCategory );
                    else
                        resolvedDesignators.Add( subCategory );

                    if ( category.debug )
                        Logger.Message( "Subcategory {0} created.", subCategory.LabelCap );
                }
                else if ( category.debug )
                {
                    Logger.Warning( "Subcategory {0} did not have any (resolved) contents! Skipping.", category.LabelCap );
                }
            }
        }

        private static bool isForDef( Designator_Build des, BuildableDef def )
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
