using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cobra.UFPSBSPanel
{
    public class ElementDefine
    {
        public const UInt16 ShowMax = 120;
        public const UInt32 CommandMask = 0x0000FF00;
        public enum SBS_PARAM_SUBTYPE : ushort
        {
            PARAM_DYNAMIC = 0,
            PARAM_STATIC,
            PARAM_EVENT
        }

        public enum SBS_PARAM_SHOWMODE : ushort
        {
            PARAM_DEFAULT = 0,
            PARAM_FIXED,
            PARAM_DYNAMIC,
        }

        internal enum SBS_PARAM_FORMAT : ushort
        {
            PARAM_DEFAULT = 0,
            PARAM_SIGN,
            PARAM_BYTE,
            PARAM_WORD,
            PARAM_TEMP,
            PARAM_DATA = 8,
            PARAM_STRING = 9,
        }
    }

}
