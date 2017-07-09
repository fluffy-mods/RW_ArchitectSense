#if DEBUG
//#define DEBUG_HIDE_DEFS
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using UnityEngine;
using UnityEngine.Assertions.Must;
using Verse;

namespace ArchitectSense
{
    public static class DesignatorUtility
    {
        private static Dictionary<BuildableDef, Designator_Build> _designators = new Dictionary<BuildableDef, Designator_Build>();

        public static void MergeDesignationCategories(DesignationCategoryDef target, DesignationCategoryDef source)
        {
            Log.Warning(String.Format("ArchitectSense :: Merging {0} with {1}...", source.defName, target.defName));

            // get both lists of resolved designators 
            var sourceDesignators = source.AllResolvedDesignators;
            var targetDesignators = target.AllResolvedDesignators;

            // merge designators that did not yet exist into the target category
            foreach (Designator sourceDesignator in sourceDesignators)
                if (!targetDesignators.Contains(sourceDesignator))
                    targetDesignators.Add(sourceDesignator);

            // todo; why doesn't this work?
            //// clean up source category
            //// check we got the field info
            //if ( _desPanelCacheFieldinfo == null )
            //    throw new Exception( "Could not get MainTabWindow_Architect.desPanelsCached FieldInfo" );

            //// check if we're actually getting anything
            //List<ArchitectCategoryTab> tabs = _desPanelCacheFieldinfo.GetValue( MainTabDefOf.Architect.Window ) as List<ArchitectCategoryTab>;
            //if ( tabs == null )
            //    throw new Exception( "Could not get list of cached designation panels " );

            //// find our source tab and remove it
            //var sourceTab = tabs.Find( tab => tab.def == source );
            //if ( sourceTab == null )
            //    throw new Exception( "Could not find cached panel for category" );
            //tabs.Remove( sourceTab );

            //// if it was selected, unselect it (this should really never happen, but whatever)
            //if ( ( MainTabDefOf.Architect.Window as MainTabWindow_Architect )?.selectedDesPanel == sourceTab )
            //    ( MainTabDefOf.Architect.Window as MainTabWindow_Architect ).selectedDesPanel = null;

            //// assign the new cached list back
            //_desPanelCacheFieldinfo.SetValue( MainTabDefOf.Architect.Window, tabs );


            // the subtle solution didn't seem to work, so let's get nuclear
            (DefDatabase<DesignationCategoryDef>.AllDefs as List<DesignationCategoryDef>)?.Remove(source);
            typeof(MainTabWindow_Architect).GetMethod("CacheDesPanels", (BindingFlags)60)
                                             .Invoke(MainButtonDefOf.Architect.TabWindow, null);
        }

        public static void HideDesignator(Designator_Build des, DesignationCategoryDef cat = null)
        {
            if ( des == null )
                throw new ArgumentNullException( nameof( des ) );

            // get the entity def
            BuildableDef def = des.PlacingDef;
            // check for null
            if (def == null)
                throw new Exception(String.Format("Tried to hide designator with NULL entDef ({0}). Such designators should not exist.", des.Label));

            // if category wasn't explicitly set, assume it is the same as the def's
            if (cat == null)
                cat = def.designationCategory;
            // if still null, there's nothing to hide.
            if (cat == null)
                throw new Exception("Tried to hide designator from NULL category. That makes little sense.");

            // make sure the designator is cached so we can still get it later
            if (!_designators.ContainsKey(def))
                _designators.Add(def, des);

            // get the categories designators
            var resolved = cat.AllResolvedDesignators;

            // remove our designator if it was in there, throw a warning if it was not
            if (resolved.Contains(des))
                resolved.Remove(des);
            else
                Log.Warning(String.Format("Tried to remove designator {0} from category {1}, but it was not included in the categories' resolved designators.", des.Label, cat.label));
        }

        internal static bool isForDef(Designator_Build des, BuildableDef def)
        {
            // we might get nulls from special designators being cast to des_build
            // in which case, reflection fails WITHOUT THROWING AN ERROR!
            if (des == null)
                return false;

            return des.PlacingDef.defName == def.defName;
        }


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
        public static void AddSubCategory(DesignationCategoryDef categoryDef, DesignationSubCategoryDef subcategoryDef,
                                           List<TerrainDef> terrainDefs, int position = -1)
        {
            AddSubCategory(categoryDef, subcategoryDef, terrainDefs.Select(def => def as BuildableDef).ToList(),
                            position);
        }

        /// <summary>
        /// Creates a subcategory based on subcategoryDef in categoryDef at position, containing elements thingDefs.
        /// Position defaults to adding on the right. 
        /// 
        /// Note that if designators for the things in thingDefs already exist, they will NOT be removed - this method
        /// creates NEW designators, and is primarily meant for mods that programatically generate defs.
        /// </summary>
        /// <param name="categoryDef"></param>
        /// <param name="subcategoryDef"></param>
        /// <param name="thingDefs"></param>
        /// <param name="position"></param>
        public static void AddSubCategory(DesignationCategoryDef categoryDef, DesignationSubCategoryDef subcategoryDef,
                                           List<ThingDef> thingDefs, int position = -1)
        {
            AddSubCategory(categoryDef, subcategoryDef, thingDefs.Select(def => def as BuildableDef).ToList(),
                            position);
        }

        /// <summary>
        /// Creates a subcategory based on subcategoryDef in categoryDef at position, containing elements buildableDefs.
        /// Position defaults to adding on the right. 
        /// 
        /// Note that if designators for the buildables in buildableDefs already exist, they will NOT be removed - this method
        /// creates NEW designators, and is primarily meant for mods that programatically generate defs.
        /// </summary>
        /// <param name="categoryDef"></param>
        /// <param name="subcategoryDef"></param>
        /// <param name="buildableDefs"></param>
        /// <param name="position"></param>
        public static void AddSubCategory(DesignationCategoryDef categoryDef, DesignationSubCategoryDef subcategoryDef,
                                           List<BuildableDef> buildableDefs, int position = -1)
        {
            // cop out on null
            if (categoryDef == null)
                throw new ArgumentNullException(nameof(categoryDef));

            // get designation category's resolved designators
            List<Designator> resolvedDesignators = categoryDef.AllResolvedDesignators;

            // check position argument
            if (position > resolvedDesignators.Count)
                throw new ArgumentOutOfRangeException(nameof(position));

            // hide existing designators 
            foreach ( BuildableDef def in buildableDefs )
                HideDesignator( def );

            // create subcategory
            var subcategory = new Designator_SubCategory(subcategoryDef,
                                                          buildableDefs.Select(bd => new Designator_Build(bd))
                                                                       .ToList());

            // if no position is specified, add it at the end
            if (position < 0)
                resolvedDesignators.Add(subcategory);
            else
                resolvedDesignators.Insert(position, subcategory);
        }
        
        public static Designator_Build GetDesignator(BuildableDef def)
        {
            Designator_Build result;

            // try get from cache
            if (_designators.TryGetValue(def, out result))
                return result;

            // find the designator
            DesignationCategoryDef dump;
            result = FindDesignator(def, out dump);

            // did we get anything? If not, create it.
            if (result == null)
                result = new Designator_Build(def);

            // cache it
            _designators.Add(def, result);

            return result;
        }

        public static void HideDesignator(BuildableDef def)
        {
            DesignationCategoryDef cat;
            var des = FindDesignator(def, out cat);
            if (cat != null && des != null)
                HideDesignator(des, cat);
        }

        internal static Designator_Build FindDesignator(BuildableDef def, out DesignationCategoryDef cat_out)
        {
#if DEBUG_HIDE_DEFS
            Controller.Logger.Message($"Trying to find {def.defName}");
#endif
            // cycle through all categories to try and find our designator
            foreach (DesignationCategoryDef cat in DefDatabase<DesignationCategoryDef>.AllDefsListForReading)
            {
#if DEBUG_HIDE_DEFS
                Controller.Logger.Message($"Checking {cat.defName}, {cat.AllResolvedDesignators.Count} designators found.");
#endif
                // check vanilla designators
                foreach (Designator_Build des in cat.AllResolvedDesignators.OfType<Designator_Build>())
                {
#if DEBUG_HIDE_DEFS
                    Controller.Logger.Message($"Checking {des.Label}, for {des.PlacingDef.defName}");
#endif
                    if (isForDef(des, def))
                    {
                        cat_out = cat;
                        return des;
                    }
                }

                // check our designation subcategories
                foreach (
                    Designator_SubCategory subcat in cat.AllResolvedDesignators.OfType<Designator_SubCategory>())
                {
                    foreach (Designator_SubCategoryItem subdes in subcat.SubDesignators)
                    {
                        if (isForDef(subdes, def))
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
    }
}
