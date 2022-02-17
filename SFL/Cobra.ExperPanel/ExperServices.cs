﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using System.Windows;
using System.Reflection;
using Cobra.Common;


namespace Cobra.ExperPanel
{
    [Export(typeof(IServices))]
    public class Services : IServices
    {
        public UIElement Insert(object pParent, string name)
        {
            return new ExperControl(pParent, name);
        }

        public void Destroy()
        {

        }
    }
}
