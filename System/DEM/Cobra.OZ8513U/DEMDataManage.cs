using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Cobra.Common;

namespace Cobra.OZ8513U
{
    internal class DEMDataManage
    {
        #region 定义参数subtype枚举类型
        //父对象保存
        private DEMDeviceManage m_parent;
        public DEMDeviceManage parent
        {
            get { return m_parent; }
            set { m_parent = value; }
        }

        private UInt32[] uval4 = new UInt32[4];
        private byte[] bval15 = new byte[15];
        #endregion

        /// <summary>
        /// 硬件模式下相关参数数据初始化
        /// </summary>
        public void Init(object pParent)
        {
            parent = (DEMDeviceManage)pParent;
        }

        /// <summary>
        /// 更新参数ItemList
        /// </summary>
        /// <param name="p"></param>
        /// <param name="relatedparameters"></param>
        /// <returns></returns>
        public void UpdateEpParamItemList(Parameter p)
        {
            return;
        }

        /// <summary>
        /// 转换参数值类型从物理值到16进制值
        /// </summary>
        /// <param name="p"></param>
        /// <param name="relatedparameters"></param>
        public void Physical2Hex(ref Parameter p)
        {
            UInt32 udata = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            if (p == null) return;
            switch ((ElementDefine.COBRA_PARAM_SUBTYPE)p.subtype)
            {
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_STRING:
                    {
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_PD_VER:
                    {
                        break;
                    }
                default:
                    {
                        udata = (UInt32)((double)(p.phydata * p.regref) / (double)p.phyref);
                        ret = WriteToRegImg(p, udata);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                            WriteToRegImgError(p, ret);
                        break;
                    }
            }
        }

        /// <summary>
        /// 转换参数值类型从物理值到16进制值
        /// </summary>
        /// <param name="p"></param>
        /// <param name="relatedparameters"></param>
        public void Hex2Physical(ref Parameter p)
        {
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            if (p == null) return;
            switch ((ElementDefine.COBRA_PARAM_SUBTYPE)p.subtype)
            {
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_STRING:
                    {
                        ret = ReadFromRegImg(p, ref uval4);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        for (int i = 0; i < p.reglist.Count; i++)
                        {
                            bval15[i * 4] = (byte)(uval4[i] & 0x00FF);
                            bval15[i * 4 + 1] = (byte)((uval4[i] & 0x0000FF00) >> 8);
                            bval15[i * 4 + 2] = (byte)((uval4[i] & 0x00FF0000) >> 16);
                            if ((i * 4 + 3) == 15) break;
                            bval15[i * 4 + 3] = (byte)((uval4[i] & 0xFF000000) >> 24);
                        }
                        p.sphydata = SharedFormula.HexToASCII(bval15);
                        break;
                    }
                case ElementDefine.COBRA_PARAM_SUBTYPE.PARAM_PD_VER:
                    {
                        ret = ReadFromRegImg(p, ref uval4);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        if ((uval4[0] > p.dbPhyMax) | (uval4[0] < p.dbPhyMin))
                            p.phydata = 0;
                        else
                            p.phydata = uval4[0];
                        break;
                    }
                default:
                    {
                        ret = ReadFromRegImg(p, ref uval4);
                        if (ret != LibErrorCode.IDS_ERR_SUCCESSFUL)
                        {
                            p.phydata = ElementDefine.PARAM_PHYSICAL_ERROR;
                            break;
                        }
                        p.phydata = (double)((double)(uval4[0] * p.phyref) / (double)p.regref);
                    }
                    break;
            }
        }

        /// <summary>
        /// 从数据buffer中读数据
        /// </summary>
        /// <param name="pval"></param>
        /// <returns></returns>
        private UInt32 ReadFromRegImg(Parameter p, ref UInt32[] pval)
        {
            byte index = 0;
            byte type = 0x00;
            UInt32 lo = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            type = (byte)((p.guid & ElementDefine.CommandMask) >> 16);//获取DataType
            foreach (Reg reg in p.reglist.Values)  //获取Command,每个Command对应4bytes数据
            {
                ret |= ReadRegFromImg(type,reg, p.guid, ref lo);
                lo <<= (32 - reg.bitsnumber - reg.startbit); //align with left
                pval[index] = lo >> (32 - reg.bitsnumber);
                index++;
            }
            return ret;
        }

        /// <summary>
        /// 写数据到buffer中
        /// </summary>
        /// <param name="wVal"></param>
        /// <returns></returns>
        public UInt32 WriteToRegImg(Parameter p, UInt32 pval)
        {
            byte type = 0x00;
            UInt32 data = 0;
            UInt32 lomask = 0;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            type = (byte)((p.guid & ElementDefine.CommandMask) >> 16);//获取DataType
            foreach (Reg reg in p.reglist.Values)  //获取Command,每个Command对应4bytes数据
            {
                ret = ReadRegFromImg(type,reg, p.guid, ref data);
                if (reg.bitsnumber == 0x20) //系统为32位系统，只能做这种特殊处理
                    lomask = 0xFFFFFFFF;
                else
                {
                    lomask = (UInt32)((1 << reg.bitsnumber) - 1);
                    lomask <<= reg.startbit;
                }
                data &= (UInt32)(~lomask);
                data |= (UInt32)(pval << reg.startbit);
                WriteRegToImg(type, reg, p.guid, data);
            }
            return ret;
        }

        private void WriteToRegImgError(Parameter p, UInt32 err)
        {
        }

        #region YFLASH数据缓存操作
        private UInt32 ReadRegFromImg(byte type,Reg reg, UInt32 guid, ref UInt32 pval)
        {
            OZ8513U_REG org = null;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            switch (guid & ElementDefine.ElementMask)
            {
                case ElementDefine.OperationElement:
                    {
                        org = parent.FindRegOnImgReg(type, reg);
                        if (org == null) break;
                    
                        pval = org.val;
                        ret = org.err;
                        break;
                    }
                default:
                    break;
            }
            return ret;
        }

        private void WriteRegToImg(byte type,Reg reg, UInt32 guid, UInt32 value)
        {
            OZ8513U_REG org = null;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;
            switch (guid & ElementDefine.ElementMask)
            {
                case ElementDefine.OperationElement:
                    {
                        org = parent.FindRegOnImgReg(type, reg);
                        if (org == null) break;

                        org.val = value;
                        org.err = ret;
                        break;
                    }
                default:
                    break;
            }
        }
        #endregion   
    }
}
