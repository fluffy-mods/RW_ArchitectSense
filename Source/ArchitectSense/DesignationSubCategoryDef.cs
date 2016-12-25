// Karel Kroeze
// DesignationSubCategoryDef.cs
// 2016-12-21

using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace ArchitectSense
{
    public class DesignationSubCategoryDef : Def
    {
        public bool debug = false;
        public List<string> defNames = new List<string>();
        public DesignationCategoryDef designationCategory;
        public GraphicData graphicData = null;
        public int order = 0;
        public bool preview = true;
    }
}
