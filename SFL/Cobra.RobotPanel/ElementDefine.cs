using System;
using System.Collections.Generic;
using System.Deployment.Internal;
using System.Linq;
using System.Text;
using Cobra.Common;

namespace Cobra.RobotPanel
{
    /// <summary>
    /// 数据结构定义
    ///     XX       XX        XX         XX
    /// --------  -------   --------   -------
    ///    保留   参数类型  寄存器地址   起始位
    /// </summary>
    public class ElementDefine
    {
        public const string prefix = "0x";
        internal enum Robot_Result : int
        {
            Result_Fresh = -1,
            Result_Pass = 0,
            Result_Fail = 1
        }
    }
}
