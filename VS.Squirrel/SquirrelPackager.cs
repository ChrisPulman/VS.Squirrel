using System;
using System.Runtime.InteropServices;
using AutoSquirrel.Services.Helpers;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace AutoSquirrel
{
    /// <summary>
    /// This class implements the tool window exposed by this package and hosts a user control.
    /// </summary>
    /// <remarks>
    /// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane,
    /// usually implemented by the package implementer.
    /// <para>
    /// This class derives from the ToolWindowPane class provided from the MPF in order to use its
    /// implementation of the IVsUIElementPane interface.
    /// </para>
    /// </remarks>
    [Guid("e37d35fa-ad30-4275-aae3-0d9699f3eb54")]
    public class SquirrelPackager : ToolWindowPane, IVsSelectionEvents, IVsUpdateSolutionEvents
    {
        private uint buildCookie;
        private uint monCookie;

        /// <summary>
        /// Initializes a new instance of the <see cref="SquirrelPackager"/> class.
        /// </summary>
        public SquirrelPackager() : base(null)
        {
            VSHelper.Caption.Subscribe(caption => Caption = caption);

            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
            // the object returned by the Content property.
            Content = new SquirrelPackagerControl();
        }

        public int OnActiveProjectCfgChange(IVsHierarchy pIVsHierarchy)
        {
            Update(pIVsHierarchy);
            return VSConstants.S_OK;
        }

        public int OnCmdUIContextChanged(uint dwCmdUICookie, int fActive) => VSConstants.S_OK;

        public int OnElementValueChanged(uint elementid, object varValueOld, object varValueNew) => VSConstants.S_OK;

        public int OnSelectionChanged(IVsHierarchy pHierOld, uint itemidOld, IVsMultiItemSelect pMISOld, ISelectionContainer pSCOld, IVsHierarchy pHierNew, uint itemidNew, IVsMultiItemSelect pMISNew, ISelectionContainer pSCNew)
        {
            Update(pHierNew, itemidNew);
            return VSConstants.S_OK;
        }

        public int UpdateSolution_Begin(ref int pfCancelUpdate) => VSConstants.S_OK;

        public int UpdateSolution_Cancel() => VSConstants.S_OK;

        public int UpdateSolution_Done(int fSucceeded, int fModified, int fCancelCommand) => VSConstants.S_OK;

        public int UpdateSolution_StartUpdate(ref int pfCancelUpdate) => VSConstants.S_OK;

        protected override void Dispose(bool disposing)
        {
            VSHelper.ProjectIsValid.Value = false;
            var monitorSelection = (IVsMonitorSelection)this.GetService(typeof(SVsShellMonitorSelection));
            monitorSelection.UnadviseSelectionEvents(this.monCookie);
            var monitorBuild = (IVsSolutionBuildManager)this.GetService(typeof(SVsSolutionBuildManager));
            monitorBuild.UnadviseUpdateSolutionEvents(this.buildCookie);
            base.Dispose(disposing);
        }

        protected override void Initialize()
        {
            var monitorSelection = (IVsMonitorSelection)this.GetService(typeof(SVsShellMonitorSelection));
            monitorSelection.AdviseSelectionEvents(this, out this.monCookie);
            var monitorBuild = (IVsSolutionBuildManager)this.GetService(typeof(SVsSolutionBuildManager));
            monitorBuild.AdviseUpdateSolutionEvents(this, out this.buildCookie);
        }

        private void Update(IVsHierarchy pIVsHierarchy, uint itemidNew) =>
            System.Threading.Tasks.Task.Run(() => {
                if (VSHelper.Options != null && VSHelper.Options.ShowUI && (VSHelper.Options.UseDebug || VSHelper.Options.UseRelease)) {
                    VSHelper.SetCurrentProject(pIVsHierarchy, itemidNew);
                } else {
                    VSHelper.ProjectIsValid.Value = false;
                }
            }).ConfigureAwait(false);

        private void Update(IVsHierarchy pIVsHierarchy) =>
            System.Threading.Tasks.Task.Run(() => {
                if (VSHelper.Options != null && VSHelper.Options.ShowUI && (VSHelper.Options.UseDebug || VSHelper.Options.UseRelease)) {
                    var pro = VSHelper.GetDTEProject(pIVsHierarchy);
                    if (pro != null) {
                        VSHelper.SetProjectFiles(pro);
                        return;
                    }
                }
                VSHelper.ProjectIsValid.Value = false;
            }).ConfigureAwait(false);
    }
}
