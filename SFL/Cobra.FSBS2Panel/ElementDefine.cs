using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cobra.FSBS2Panel
{
    public class ElementDefine
    {
        public const UInt32 CommandMask = 0x0000FF00;
        public const String NoKeyDefined = "NoSuchKey";
        public enum SBS_PARAM_SUBTYPE : ushort
        {
            PARAM_DYNAMIC = 0,
            PARAM_STATIC,
            PARAM_EVENT,
            PARAM_WR,
            PARAM_EVENT_BIT = 4,
            PARAM_VOL,
            PARAM_CUR,
            PARAM_TEMP,
            PARAM_EVENT_WR = 9,
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
