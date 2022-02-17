using Cobra.Common;
using System;
using System.ComponentModel.Composition;
using System.Windows;

namespace Cobra.RobotPanel
{
    [Export(typeof(IServices))]
    [Serializable]
    public class Services : IServices
    {
        public UIElement Insert(object pParent, string name)
        {
            return new MainControl(pParent, name);
        }
    }
}
