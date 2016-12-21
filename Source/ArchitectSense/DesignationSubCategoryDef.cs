using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace ArchitectSense
{
    internal class DesignationSubCategoryDef : Def
    {
        #region Fields

        public bool             debug                   = false;
        public List<string>     defNames                = new List<string>();
        public DesignationCategoryDef  designationCategory;
        public GraphicData      graphicData             = null;
        public int              order                   = 0;

        #endregion Fields
    }
}