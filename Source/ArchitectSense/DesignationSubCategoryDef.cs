using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace ArchitectSense
{
    class DesignationSubCategoryDef : Def
    {
        public List<string>     defNames                = new List<string>();
        public int              order                   = 0;
        public string           designationCategory     = string.Empty;
        public GraphicData      graphicData;
        public bool             debug;
    }
}
