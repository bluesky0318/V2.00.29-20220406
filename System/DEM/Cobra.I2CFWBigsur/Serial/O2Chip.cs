using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cobra.Common;

namespace Cobra.I2CFWBigsur
{
    public class O2Chip
    {
        public virtual UInt32 Erase(ref TASKMessage msg) { return LibErrorCode.IDS_ERR_SUCCESSFUL; }
        public virtual UInt32 Download(ref TASKMessage msg) { return LibErrorCode.IDS_ERR_SUCCESSFUL; }
        public virtual UInt32 Upload(ref TASKMessage msg) { return LibErrorCode.IDS_ERR_SUCCESSFUL; }
    }
}
