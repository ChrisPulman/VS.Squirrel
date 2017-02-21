using AutoSquirrel.Services.Helpers;
using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;

namespace AutoSquirrel
{
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [ComVisible(true)]
    [Guid("68C87C7B-0212-4256-BB6D-6A6BB847A3A7")]
    public class OptionsPage : UIElementDialogPage
    {
        private OptionsControl child;

        protected override UIElement Child => this.child ?? (this.child = new OptionsControl { UseDebug = false, UseRelease = true, ShowUI = true });

        protected override void OnActivate(CancelEventArgs e)
        {
            base.OnActivate(e);
            if (!File.Exists(VSHelper.OptionsFile))
            {
                FileUtility.SerializeToFile(VSHelper.OptionsFile, new VSSqirrelOptions { UseDebug = false, UseRelease = true, ShowUI = true });
            }

            LoadSettings();
        }

        protected override void OnApply(PageApplyEventArgs args)
        {
            if (args.ApplyBehavior == ApplyKind.Apply)
            {
                SaveSettings();
            }

            base.OnApply(args);
        }

        private void LoadSettings()
        {
            VSHelper.Options = FileUtility.Deserialize<VSSqirrelOptions>(VSHelper.OptionsFile);
            this.child.UseDebug = VSHelper.Options.UseDebug;
            this.child.UseRelease = VSHelper.Options.UseRelease;
            this.child.ShowUI = VSHelper.Options.ShowUI;
        }

        private void SaveSettings()
        {
            VSHelper.Options.UseDebug = this.child.UseDebug;
            VSHelper.Options.UseRelease = this.child.UseRelease;
            VSHelper.Options.ShowUI = this.child.ShowUI;
            FileUtility.SerializeToFile(VSHelper.OptionsFile, VSHelper.Options);
            VSHelper.ProjectIsValid.Value = VSHelper.Options.ShowUI && (VSHelper.Options.UseDebug || VSHelper.Options.UseRelease);
        }
    }
}