// Karel Kroeze
// Controller.cs
// 2016-12-21

using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using static ArchitectSense.DesignatorUtility;

namespace ArchitectSense
{

    public class Controller : Mod
    {
        #region Constructors

        public Controller( ModContentPack content ) : base ( content )
        {
            LongEventHandler.QueueLongEvent(Initialize, "ArchitectSense.Initialize", false, null);
        }

        public void Initialize()
        {
            CreateSubCategories();
            MergeCategories();
            RemoveCategories();
        }

        private static void MergeCategories()
        {
            Logger.Debug("Merging categories...");
            foreach (var merger in DefDatabase<MergeCategories>.AllDefsListForReading)
                merger.Apply();
        }

        private static void RemoveCategories()
        {
            Logger.Debug("Removing categories...");
            foreach (var obsoletion in DefDatabase<RemoveCategory>.AllDefsListForReading)
                obsoletion.Apply();
        }

        private static void CreateSubCategories()
        {
            Logger.Debug("Creating subcategories");
            foreach (DesignationSubCategoryDef category in DefDatabase<DesignationSubCategoryDef>.AllDefsListForReading
            )
            {
                if (category.debug)
                    Logger.Message("Creating subcategory {0} in category {1}", category.LabelCap,
                        category.designationCategory);

                // cop out if main cat not found
                if (category.designationCategory == null)
                {
                    Logger.Warning("Category {0} not found! Skipping.", category.designationCategory);
                    continue;
                }

                // set up sub category
                var designators = new List<Designator_Build>();

                // keep track of best position for the subcategory - it will replace the first subitem in the original category.
                int firstDesignatorIndex = -1;

                // get list of current designators in the category
                List<Designator> resolvedDesignators = category.designationCategory.AllResolvedDesignators;

                // start adding designators to the subcategory
                if (category.defNames != null)
                    foreach (string defName in category.defNames)
                    {
                        BuildableDef bdef = DefDatabase<ThingDef>.GetNamedSilentFail(defName) ??
                                            (BuildableDef) DefDatabase<TerrainDef>.GetNamedSilentFail(defName);

                        // do some common error checking
                        // buildable def exists
                        if (bdef == null)
                        {
                            if (category.debug)
                                Logger.Warning("ThingDef {0} not found! Skipping.", defName);
                            continue;
                        }

                        // find the designator for this buildabledef
                        DesignationCategoryDef designatorCategory;
                        var bdefDesignator = FindDesignator(bdef, out designatorCategory);
                        if (category.debug && bdefDesignator == null)
                            Log.Warning("No designator found with matching entity def! Skipping.");

                        // if not null, add designator to the subcategory, and remove from main category
                        if (bdefDesignator != null)
                        {
                            // if taken designator was in the same category as the new subcategory, find index and update FirstDesignatorIndex
                            if (designatorCategory == category.designationCategory)
                            {
                                int index = resolvedDesignators.IndexOf(bdefDesignator);
                                if (firstDesignatorIndex < 0 || index < firstDesignatorIndex)
                                    firstDesignatorIndex = index;
                            }

                            designators.Add(bdefDesignator);
                            HideDesignator(bdefDesignator);

                            if (category.debug)
                                Logger.Message("ThingDef {0} passed checks and was added to subcategory.", defName);
                        }
                        // done with this designator
                    }

                // check if any designators were added to subdesignator
                if (!designators.NullOrEmpty())
                {
                    // create subcategory
                    var subCategory = new Designator_SubCategory(category, designators);

                    // insert to replace first designator removed, or just add at the end if taken from different categories
                    if (firstDesignatorIndex >= 0)
                        resolvedDesignators.Insert(firstDesignatorIndex, subCategory);
                    else
                        resolvedDesignators.Add(subCategory);

                    if (category.debug)
                        Logger.Message("Subcategory {0} created.", subCategory.LabelCap);
                }
                else if (category.debug)
                {
                    Logger.Warning("Subcategory {0} did not have any (resolved) contents! Skipping.", category.LabelCap);
                }
            }
        }

        #endregion Constructors

        #region Properties
        
        private static Logger _logger;
        public static Logger Logger
        {
            get
            {
                if ( _logger == null )
                {
                    _logger = new Logger( "ArchitectSense" );
                }
                return _logger;
            }
        } 

        #endregion Properties

    }
}
