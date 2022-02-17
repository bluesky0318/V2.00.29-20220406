using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.ComponentModel;
using System.Collections.ObjectModel;
using Cobra.Common;
using Cobra.EM;

namespace Cobra.RobotPanel
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

        private ParamContainer m_DM_ParameterList = new ParamContainer();
        public ParamContainer dm_parameterlist
        {
            get { return m_DM_ParameterList; }
            set { m_DM_ParameterList = value; }
        }

        private ObservableCollection<Model> m_Robot_Commands = new ObservableCollection<Model>();
        public ObservableCollection<Model> robot_commands
        {
            get { return m_Robot_Commands; }
            set
            {
                m_Robot_Commands = value;
            }
        }

        private List<Model> m_Execute_Commands = new List<Model>();
        public List<Model> execute_Commands
        {
            get { return m_Execute_Commands; }
            set { m_Execute_Commands = value; }
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

            robot_commands.Clear();
            execute_Commands.Clear();
            #endregion
        }
    }
}
