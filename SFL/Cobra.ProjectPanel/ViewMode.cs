using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using Cobra.Common;
using Cobra.EM;

namespace Cobra.ProjectPanel
{
    public class ViewMode
    {
        //父对象保存
        private MainControl m_control_parent;
        public MainControl control_parent
        {
            get { return m_control_parent; }
            set { m_control_parent = value; }
        }

        private Device m_device_parent;
        public Device device_parent
        {
            get { return m_device_parent; }
            set { m_device_parent = value; }
        }

        private string m_SFLname;
        public string sflname
        {
            get { return m_SFLname; }
            set { m_SFLname = value; }
        }

        public Proj m_empty_prj = new Proj("Project");
        public Proj m_load_prj = null;

        private ParamContainer m_Prj_ParamContainer = new ParamContainer();
        public ParamContainer prj_paramContainer
        {
            get { return m_Prj_ParamContainer; }
            set { m_Prj_ParamContainer = value; }
        }

        private AsyncObservableCollection<Parameter> m_parameter_list = new AsyncObservableCollection<Parameter>();
        public AsyncObservableCollection<Parameter> parameterlist
        {
            get { return m_parameter_list; }
            set { m_parameter_list = value; }
        }
        public ViewMode(object pParent, object parent)
        {
            #region 相关初始化
            device_parent = (Device)pParent;
            if (device_parent == null) return;

            control_parent = (MainControl)parent;
            if (control_parent == null) return;

            sflname = control_parent.sflname;
            if (String.IsNullOrEmpty(sflname)) return;

            Init();
            #endregion

            prj_paramContainer = device_parent.GetParamLists(sflname);
            foreach (Parameter param in prj_paramContainer.parameterlist)
            {
                if (param == null) continue;
                InitSFLParameter(param);
            }
            m_load_prj = m_empty_prj.DeepCopy();
        }

        public void Init()
        {
            m_empty_prj.bReady = false;
            m_empty_prj.projFiles.Clear();
        }

        private void InitSFLParameter(Parameter param)
        {
            UInt16 udata = 0;
            ProjFile model = new ProjFile();
            model.bExist = false;
            model.folderPath = string.Empty;
            model.info = string.Empty;

            foreach (DictionaryEntry de in param.sfllist[sflname].nodetable)
            {
                switch (de.Key.ToString())
                {
                    case "Index":
                        if (!UInt16.TryParse(de.Value.ToString(), out udata))
                            model.index = 0;
                        else
                            model.index = udata;
                        break;
                    case "Format":
                        if (!UInt16.TryParse(de.Value.ToString(), out udata))
                            model.type = 0;
                        else                            
                            model.type = udata;
                        switch ((FILE_TYPE)model.type)
                        {
                            case FILE_TYPE.FILE_HEX:
                                model.name = "Hex File";
                                break;
                            case FILE_TYPE.FILE_PARAM:
                                model.name = "Parameter File";
                                break;
                            case FILE_TYPE.FILE_THERMAL_TABLE:
                                model.name = "Thermal File";
                                break;
                            case FILE_TYPE.FILE_OCV_TABLE:
                                model.name = "OCV File";
                                break;
                            case FILE_TYPE.FILE_RC_TABLE:
                                model.name = "RC File";
                                break;
                            case FILE_TYPE.FILE_SELF_DISCH_TABLE:
                                model.name = "Self-Discharge File";
                                break;
                            case FILE_TYPE.FILE_FD_TABLE:
                                model.name = "Fault Detection File";
                                break;
                            case FILE_TYPE.FILE_FGLITE_TABLE:
                                model.name = "FGLITE File";
                                break;
                        }
                        break;
                    case "Folder":
                        model.folder = de.Value.ToString();
                        break;
                    case "StartAddress":
                        model.startAddress = Convert.ToUInt32(de.Value.ToString(), 16);
                        break;
                    case "xAxisPointsAddress":
                        model.xAxisPointsAddress = Convert.ToUInt32(de.Value.ToString(), 16);
                        break;
                    case "yAxisPointsAddress":
                        model.yAxisPointsAddress = Convert.ToUInt32(de.Value.ToString(), 16);
                        break;
                    case "zAxisPointsAddress":
                        model.zAxisPointsAddress = Convert.ToUInt32(de.Value.ToString(), 16);
                        break;
                    case "wAxisPointsAddress":
                        model.wAxisPointsAddress = Convert.ToUInt32(de.Value.ToString(), 16);
                        break;
                    case "Size":
                        model.size = Convert.ToUInt32(de.Value.ToString(), 16);
                        break;
                    default:
                        break;
                }
                model.toolTip = string.Format("Add a {0} into the project", model.name);
            }
            m_empty_prj.projFiles.Add(model);
        }

        public void UpdatePrjParameters()
        {
            prj_paramContainer.parameterlist.Clear();
            foreach (Parameter param in parameterlist)
                prj_paramContainer.parameterlist.Add(param);
        }

        public UInt32 WriteDevice()
        {
            foreach (ProjFile fl in m_load_prj.projFiles)
            {
                if (fl == null) continue;
                switch ((FILE_TYPE)fl.type)
                {
                    case FILE_TYPE.FILE_OCV_TABLE:
                    case FILE_TYPE.FILE_RC_TABLE:
                    case FILE_TYPE.FILE_SELF_DISCH_TABLE:
                    case FILE_TYPE.FILE_THERMAL_TABLE:
                    case FILE_TYPE.FILE_FD_TABLE:
                        Array.Copy(fl.tableHeader.NumOfPoints, 0, m_load_prj.GetFileByType(FILE_TYPE.FILE_HEX).data, fl.xAxisPointsAddress, fl.tableHeader.NumOfAxis);
                        Array.Copy(fl.data, 0, m_load_prj.GetFileByType(FILE_TYPE.FILE_HEX).data, fl.startAddress, fl.size);
                        break;
                    case FILE_TYPE.FILE_PARAM:
                        (fl.userCtrl as Param.ParamUserControl).viewmode.WriteDevice();
                        break;
                    case FILE_TYPE.FILE_FGLITE_TABLE:
                        (fl.userCtrl as Table.FGTableUserControl).WriteDevice();
                        break;
                }
            }          
            return LibErrorCode.IDS_ERR_SUCCESSFUL;
        }
    }
}
