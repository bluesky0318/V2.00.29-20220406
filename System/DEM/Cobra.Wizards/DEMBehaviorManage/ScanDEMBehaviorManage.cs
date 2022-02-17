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
    internal class ScanDEMBehaviorManage : DEMBehaviorManageBase
    {
        private static bool switcher = false;
        #region 基础服务功能设计
        public override UInt32 Command(ref TASKMessage msg)
        {
            Parameter param = null;
            ParamContainer demparameterlist = null;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            demparameterlist = msg.task_parameterlist;
            if (demparameterlist == null) return ret;
            switch ((ElementDefine.COMMAND)msg.sub_task)
            {
                case ElementDefine.COMMAND.OPTIONS:
                    var options = SharedAPI.DeserializeStringToDictionary<string, string>(msg.sub_task_json);
                    switch (options["Work Mode"])
                    {
                        case "Normal":
                            ret = Read(ref msg);
                            break;
                        case "Switch":
                            ret = SwitchRead(ref msg);
                            break;
                    }
                    if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        return ret;
                    break;
            }
            return ret;
        }

        private uint SwitchRead(ref TASKMessage msg)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            if (switcher == false)
            {
                ret = WriteWord(0x06, 0xADDA);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                    return ret;
                switcher = true;
            }
            else
            {
                ret = WriteWord(0x06, 0xDAAD);
                if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                    return ret;
                switcher = false;
            }
            Thread.Sleep(150);
            ret = Read(ref msg);
            return ret;
        }
        #endregion
    }
}