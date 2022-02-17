using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;

namespace Cobra.ProjectPanel
{
    public abstract class SubUserControl : UserControl
    {
        public abstract void OpenFile(ProjFile pf);
        public abstract void SaveFile(ProjFile pf);
        public abstract void CloseFile();
    }
}
