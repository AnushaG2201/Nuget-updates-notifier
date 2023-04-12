
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;

namespace MonitorNugetUpdates
{
    public class NotificationService
    {
        private readonly IServiceProvider _serviceProvider;

        public NotificationService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }



        public void ShowMessage(string title, List<String> packagesList, List<String>latestVersionList)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var statusBar = (IVsStatusbar)_serviceProvider.GetService(typeof(SVsStatusbar));
            if (statusBar == null)
            {
                return;
            }

            statusBar.SetText(title);
            
            CustomDialog.ShowDialog(title, packagesList, latestVersionList);
            var progressBar = statusBar;
            object pvIcon = null;
            if (progressBar != null)
            {
                progressBar.Animation(1, ref pvIcon);
            }           
        }

    }
}

