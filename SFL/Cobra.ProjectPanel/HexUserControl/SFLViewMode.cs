using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.IO;
using Cobra.EM;
using Cobra.Common;

namespace Cobra.ProjectPanel.Hex
{
    public class SFLViewMode
    {
        internal byte[] m_MTP_Memory = new byte[0x100000];
        //父对象保存
        private HexUserControl m_control_parent;
        public HexUserControl control_parent
        {
            get { return m_control_parent; }
            set { m_control_parent = value; }
        }

        private string m_SFLname;
        public string sflname
        {
            get { return m_SFLname; }
            set { m_SFLname = value; }
        }

        private ObservableCollection<SFLModel> m_SFL_ParameterList = new ObservableCollection<SFLModel>();
        public ObservableCollection<SFLModel> sfl_parameterlist
        {
            get { return m_SFL_ParameterList; }
            set { m_SFL_ParameterList = value; }
        }

        public SFLViewMode(object pParent, object parent)
        {
            #region 相关初始化
            control_parent = (HexUserControl)parent;
            if (control_parent == null) return;

            sflname = control_parent.parent.sflname;
            if (String.IsNullOrEmpty(sflname)) return;

            Init();
            #endregion
        }

        public void Init()
        {
            sfl_parameterlist.Clear();
            LoadFile();

            for (uint i = control_parent.projFile.startAddress; i < control_parent.projFile.size / 16; i++)
            {
                SFLModel fwlist = new SFLModel();
                fwlist.address = (uint)i * 16;
                for (int j = 0; j < 16; j++)
                {
                    fwlist.m_data[j] = control_parent.projFile.data[fwlist.address + j];
                }
                sfl_parameterlist.Add(fwlist);
            }
        }


        public UInt32 LoadFile2()
        {
            int pos;
            //double dval = 0.0;
            string line, tmp;
            //UInt32 selfid;
            UInt32 ret = LibErrorCode.IDS_ERR_SUCCESSFUL;

            Byte length = 0, type = 0, checksum = 0, btmp = 0;
            UInt16 uaddress = 0;
            Byte[] databuffer;//, firmwarebuffer;
            databuffer = new Byte[32];
            if (!File.Exists(control_parent.projFile.fullName))
                return LibErrorCode.IDS_ERR_SECTION_PROJECT_CONTENT_FILE_NOTEXIST_FIRMWARE; //should return error

            //Array.Clear(control_parent.projFile.data, 0, control_parent.projFile.data.Length);
            for (int i = 0; i < control_parent.projFile.data.Length; i++)
                control_parent.projFile.data[i] = 0xFF;
            try
            {
                // Create an instance of StreamReader to read from a file.
                // The using statement also closes the StreamReader.
                using (StreamReader sr = new StreamReader(@control_parent.projFile.fullName))
                {
                    // Read and display lines from the file until the end of 
                    // the file is reached.
                    while ((line = sr.ReadLine()) != null)
                    {
                        checksum = 0;

                        // First char should be ":"
                        pos = line.IndexOf(':');
                        if (pos == -1) continue;
                        //remove it
                        line = line.Remove(0, 1);
                        // then, next 2 char are length
                        tmp = line.Substring(0, 2);
                        //length = Convert.ToUInt32(tmp, 16);
                        length = Convert.ToByte(tmp, 16);
                        checksum += length;
                        line = line.Remove(0, 2);
                        // Tehn, next 4 char are address offset
                        tmp = line.Substring(0, 4);
                        uaddress = Convert.ToUInt16(tmp, 16);

                        checksum += (Byte)uaddress;
                        checksum += (Byte)(uaddress >> 8);

                        line = line.Remove(0, 4);
                        // then, next 1 char are type "00" means data, "01" means end of file
                        tmp = line.Substring(0, 2);
                        type = Convert.ToByte(tmp, 16);
                        checksum += type;
                        line = line.Remove(0, 2);
                        if (type != 0)
                            continue;
                        // The data according to length. up to 16 (dec)
                        // line in here should be have only data with last check sum.
                        for (int i = 0; i < length; i++)
                        {
                            tmp = line.Substring(0, 2);
                            btmp = Convert.ToByte(tmp, 16);
                            checksum += btmp;
                            databuffer[i] = btmp;
                            line = line.Remove(0, 2);
                        }
                        // the last 1 char is checksum.
                        tmp = line.Substring(0, 2);
                        btmp = Convert.ToByte(tmp, 16);
                        checksum += btmp;
                        // Do checksum calculation for hex file in each line.
                        //  byte checksum = 0;
                        if (checksum == 0)
                        {
                            for (btmp = 0; btmp < length; btmp++)
                            {
                                //databuffer = new Byte[32];
                                control_parent.projFile.data[uaddress + btmp] = databuffer[btmp];
                            }
                        }
                        else
                        {
                            return LibErrorCode.IDS_ERR_SECTION_PROJECT_CONTENT_FILE_FIRMWARE_CHECKSUM_ERROR;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                // Let the user know what went wrong.
                Console.WriteLine("The file could not be read:");
                Console.WriteLine(e.Message);
            }
            return ret;

        }

        public UInt32 LoadFile()
        {
            int pos;
            string line, tmp;
            bool bType0 = false;
            UInt16 address = 0;
            Byte length = 0, type = 0, btmp = 0;
            Byte[] databuffer = new Byte[256];
            List<MemoryControl> bufList = new List<MemoryControl>();
            UInt32 startAddress = 0, endAddress = 0, total_len = 0, slAddr = 0;
            UInt32 extaddress = 0, lineaddress = 0, uaddress = 0, MaxAddress = 0;
            MemoryControl m_Buffer_Control = null;
            try
            {
                using (StreamReader sr = new StreamReader(@control_parent.projFile.fullName))
                {
                    while ((line = sr.ReadLine()) != null)
                    {
                        pos = line.IndexOf(':');        // First char should be ":"
                        if (pos == -1) continue;
                        line = line.Remove(0, 1);       //remove it
                        tmp = line.Substring(0, 2);     // then, next 2 char are length
                        length = Convert.ToByte(tmp, 16);
                        line = line.Remove(0, 2);
                        tmp = line.Substring(0, 4);
                        lineaddress = Convert.ToUInt16(tmp, 16);

                        line = line.Remove(0, 4);
                        tmp = line.Substring(0, 2); // then, next 1 char are type "00" means data, "01" means end of file
                        type = Convert.ToByte(tmp, 16);
                        line = line.Remove(0, 2);
                        switch (type)
                        {
                            case 00:
                                total_len += length;
                                break;
                            case 01: //EOF
                                continue;
                            case 02:
                                bType0 = false;
                                tmp = line.Substring(0, 4);
                                address = Convert.ToUInt16(tmp, 16);
                                extaddress = (UInt32)address << 4;
                                if (bufList.Count != 0)
                                    m_Buffer_Control.totalSize = total_len;
                                total_len = 0;
                                continue;
                            case 03:
                                continue;
                            case 04:
                                bType0 = false;
                                tmp = line.Substring(0, 4);
                                address = Convert.ToUInt16(tmp, 16);
                                extaddress = (UInt32)address << 16;
                                if (bufList.Count != 0)
                                    m_Buffer_Control.totalSize = total_len;
                                total_len = 0;
                                continue;
                            case 05:
                                continue;
                        }
                        endAddress = MaxAddress;
                        uaddress = (UInt32)(extaddress + lineaddress);
                        if (!bType0)
                        {
                            bType0 = true;
                            startAddress = uaddress;
                            if (bufList.Count != 0)
                            {
                                m_Buffer_Control.endAddress = endAddress;
                                m_Buffer_Control.Update();
                                Array.Copy(m_MTP_Memory, 0, m_Buffer_Control.buffer, 0, m_Buffer_Control.totalSize);
                            }
                            m_Buffer_Control = new MemoryControl();
                            m_Buffer_Control.startAddress = startAddress;
                            endAddress = MaxAddress = 0;
                            bufList.Add(m_Buffer_Control);
                            Array.Clear(m_MTP_Memory, 0, m_MTP_Memory.Length);
                        }
                        for (int i = 0; i < length; i++)
                        {
                            tmp = line.Substring(0, 2);
                            btmp = Convert.ToByte(tmp, 16);
                            databuffer[i] = btmp;
                            line = line.Remove(0, 2);
                        }
                        tmp = line.Substring(0, 2);
                        btmp = Convert.ToByte(tmp, 16);
                        Array.Copy(databuffer, 0, m_MTP_Memory, (uaddress - startAddress), length);
                        if (uaddress > MaxAddress) MaxAddress = uaddress;
                    }
                }
                m_Buffer_Control.endAddress = MaxAddress;
                m_Buffer_Control.totalSize = total_len;
                m_Buffer_Control.Update();
                Array.Copy(m_MTP_Memory, 0, m_Buffer_Control.buffer, 0, m_Buffer_Control.totalSize);

                slAddr = m_Buffer_Control.startAddress;
                foreach (MemoryControl mc in bufList)
                {
                    if (slAddr >= mc.startAddress)
                        slAddr = mc.startAddress;
                }
                foreach (MemoryControl mc in bufList)
                {
                    Array.Copy(mc.buffer, 0, control_parent.projFile.data, (mc.startAddress - slAddr), mc.totalSize);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message.ToString());
            }
            return LibErrorCode.IDS_ERR_SUCCESSFUL;
        }


        private void reverseArrayBySize(ref byte[] ary, int subSize)
        {
            byte[] bval = new byte[subSize];
            List<byte> splitted = new List<byte>();//This list will contain all the splitted arrays.

            for (int i = 0; i < 64 * 1024; i = i + subSize)
            {
                Array.Copy(control_parent.projFile.data, i, bval, 0, subSize);
                Array.Reverse(bval);
                for (int j = 0; j < subSize; j++)
                    splitted.Add(bval[j]);
            }
            ary = splitted.ToArray();
        }
    }
}
