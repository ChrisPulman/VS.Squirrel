using System.ComponentModel;
using System.IO;
using System.Runtime.Serialization;
using AutoSquirrel.Services.Helpers;
using Microsoft.VisualStudio.Shell;

namespace AutoSquirrel
{
    /// <summary>
    /// VS Sqirrel Options
    /// </summary>
    /// <seealso cref="AutoSquirrel.IVSSqirrelOptions"/>
    [DataContract]
    public class VSSqirrelOptions : DialogPage, IVSSqirrelOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether [show UI].
        /// </summary>
        /// <value><c>true</c> if [show UI]; otherwise, <c>false</c>.</value>
        [DataMember] public bool ShowUI { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [use debug].
        /// </summary>
        /// <value><c>true</c> if [use debug]; otherwise, <c>false</c>.</value>
        [DataMember] public bool UseDebug { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [use release].
        /// </summary>
        /// <value><c>true</c> if [use release]; otherwise, <c>false</c>.</value>
        [DataMember] public bool UseRelease { get; set; }

        protected override void OnActivate(CancelEventArgs e)
        {
            if (!File.Exists(VSHelper.OptionsFile)) {
                FileUtility.SerializeToFile(VSHelper.OptionsFile, new VSSqirrelOptions { UseDebug = false, UseRelease = true, ShowUI = true });
            }

            LoadSettings();
            base.OnActivate(e);
        }

        protected override void OnApply(PageApplyEventArgs args)
        {
            if (args.ApplyBehavior == ApplyKind.Apply) {
                SaveSettings();
            }

            base.OnApply(args);
        }

        private void LoadSettings()
        {
            VSHelper.Options = FileUtility.Deserialize<VSSqirrelOptions>(VSHelper.OptionsFile);
            UseDebug = VSHelper.Options.UseDebug;
            UseRelease = VSHelper.Options.UseRelease;
            ShowUI = VSHelper.Options.ShowUI;
        }

        private void SaveSettings()
        {
            VSHelper.Options.UseDebug = UseDebug;
            VSHelper.Options.UseRelease = UseRelease;
            VSHelper.Options.ShowUI = ShowUI;
            FileUtility.SerializeToFile(VSHelper.OptionsFile, VSHelper.Options);
            VSHelper.ProjectIsValid.Value = VSHelper.Options.ShowUI && (VSHelper.Options.UseDebug || VSHelper.Options.UseRelease);
        }
    }
}
