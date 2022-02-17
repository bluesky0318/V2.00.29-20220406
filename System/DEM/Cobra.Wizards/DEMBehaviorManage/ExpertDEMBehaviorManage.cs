using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.ComponentModel;
using Cobra.Communication;
using Cobra.Common;
using System.IO;

namespace Cobra.SD77060
{
    internal class ExpertDEMBehaviorManage:DEMBehaviorManageBase
    {

        #region 基础服务功能设计
        public override UInt32 Command(ref TASKMessage msg)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            switch ((ElementDefine.COMMAND)msg.sub_task)
            {
                case ElementDefine.COMMAND.EXPERT_ENTER_SERIAL_MODE:
                    ret = WriteWord(0x06, 0xADDA);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    break;
                case ElementDefine.COMMAND.EXPERT_ENTER_PARALLEL_MODE:
                    ret = WriteWord(0x06, 0xDAAD);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    break;
                case ElementDefine.COMMAND.EXPERT_ENTER_SHIP_MODE:
                    ret = WriteWord(0x06, 0xABBA);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    break;
                case ElementDefine.COMMAND.EXPERT_ENTER_SLEEP_MODE:
                    ret = WriteWord(0x06, 0xACCA);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    break;
                case ElementDefine.COMMAND.EXPERT_EXIT_SLEEP_MODE:
                    ret = WriteWord(0x06, 0xCAAC);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    break;
                case ElementDefine.COMMAND.EXPERT_ENTER_TEST_MODE:
                    ret = WriteWord(0x20, 0xACBD);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    break;
                case ElementDefine.COMMAND.EXPERT_EXIT_TEST_MODE:
                    ret = WriteWord(0x20, 0xBDAC);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    break;
                case ElementDefine.COMMAND.EXPERT_ENTER_FET_CTRL:
                    ret = WriteWord(0x20, 0xFCCF);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    break;
                case ElementDefine.COMMAND.EXPERT_EXIT_FET_CTRL:
                    ret = WriteWord(0x20, 0xCFFC);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    break;
                case ElementDefine.COMMAND.EXPERT_ENTER_TRIM_MODE:
                    ret = WriteWord(0x20, 0x54);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    ret = WriteWord(0x20, 0x53);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    ret = WriteWord(0x20, 0x54);
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    break;
            }
            return ret;
        }
        #endregion
    }
}