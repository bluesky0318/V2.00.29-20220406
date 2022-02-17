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
    internal class TrimDEMBehaviorManage : DEMBehaviorManageBase
    {

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
                case ElementDefine.COMMAND.TRIM_SLOPE_TRIMMING:

                    Preparation();

                    for (ushort i = 0; i < demparameterlist.parameterlist.Count; i++)
                    {
                        param = demparameterlist.parameterlist[i];
                        param.sphydata = String.Empty;
                    }

                    for (ushort code = 0; code < 16; code++)
                    {
                        WriteSlopeCode(code);
                        Thread.Sleep(500);
                        ret = ReadAvrage(ref msg);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        for (ushort i = 0; i < demparameterlist.parameterlist.Count; i++)
                        {
                            param = demparameterlist.parameterlist[i];
                            param.sphydata += param.phydata.ToString() + ",";
                        }
                    }
                    break;
                case ElementDefine.COMMAND.TRIM_OFFSET_TRIMMING:

                    Preparation();

                    for (ushort i = 0; i < demparameterlist.parameterlist.Count; i++)
                    {
                        param = demparameterlist.parameterlist[i];
                        param.sphydata = String.Empty;
                    }

                    for (ushort code = 0; code < 16; code++)
                    {
                        WriteOffsetCode(code);
                        Thread.Sleep(500);

                        ret = ReadAvrage(ref msg);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL) return ret;

                        for (ushort i = 0; i < demparameterlist.parameterlist.Count; i++)
                        {
                            param = demparameterlist.parameterlist[i];
                            param.sphydata += param.phydata.ToString() + ",";
                        }
                    }
                    break;
            }
            return ret;
        }

        private void WriteSlopeCode(ushort code)
        {
            WriteWord(0x28, (ushort)((code << 12) | (code << 4)));
            WriteWord(0x29, (ushort)(code << 4));
        }

        private void WriteOffsetCode(ushort code)
        {
            ushort buf = 0;
            ReadWord(0x28, ref buf);
            WriteWord(0x28, (ushort)(buf | (code << 8) | code));
            ReadWord(0x29, ref buf);
            WriteWord(0x29, (ushort)(buf | code));
        }

        private UInt32 Preparation()
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            //ret = WriteWord(0x20, 0x54);
            //if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
            //    return ret;
            //ret = WriteWord(0x20, 0x53);
            //if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
            //    return ret;
            //ret = WriteWord(0x20, 0x54);
            //if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
            //    return ret;
            return ret;
        }
        private uint ReadAvrage(ref TASKMessage msg)
        {
            uint errorcode = 0;
            List<double[]> llt = new List<double[]>();
            List<double> avr = new List<double>();
            foreach (Parameter param in msg.task_parameterlist.parameterlist)
            {
                llt.Add(new double[5]);
                avr.Add(0);
            }
            for (int i = 0; i < 5; i++)
            {
                errorcode = Read(ref msg);
                if (errorcode != LibErrorCode.IDS_ERR_SUCCESSFUL)
                {
                    return errorcode;
                }
                errorcode = ConvertHexToPhysical(ref msg);
                if (errorcode != LibErrorCode.IDS_ERR_SUCCESSFUL)
                {
                    return errorcode;
                }
                for (int j = 0; j < msg.task_parameterlist.parameterlist.Count; j++)
                {
                    llt[j][i] = msg.task_parameterlist.parameterlist[j].phydata;
                    avr[j] += llt[j][i];
                }
                Thread.Sleep(100);
            }

            for (int j = 0; j < msg.task_parameterlist.parameterlist.Count; j++)
            {
                //llt[j][i] = msg.task_parameterlist.parameterlist[j].phydata;
                avr[j] /= 5;
                int minIndex = 0;
                double err = 999;
                for (int i = 0; i < 5; i++)
                {
                    if (err > Math.Abs(llt[j][i] - avr[j]))
                    {
                        err = Math.Abs(llt[j][i] - avr[j]);
                        minIndex = i;
                    }
                }
                msg.task_parameterlist.parameterlist[j].phydata = llt[j][minIndex];
            }
            return errorcode;
        }
        #endregion
    }
}