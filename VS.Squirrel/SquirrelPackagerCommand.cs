//------------------------------------------------------------------------------
// <copyright file="SquirrelPackagerCommand.cs" company="AIC Solutions Ltd">
//     Copyright (c) AIC Solutions Ltd.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using AutoSquirrel.Services.Helpers;

namespace AutoSquirrel
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class SquirrelPackagerCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("a2e139eb-841b-414f-bcf2-b8745b7b7e4e");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly Package package;

        /// <summary>
        /// Initializes a new instance of the <see cref="SquirrelPackagerCommand"/> class. Adds our
        /// command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <exception cref="ArgumentNullException"></exception>
        private SquirrelPackagerCommand(Package package)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));

            if (this.ServiceProvider.GetService(typeof(IMenuCommandService)) is OleMenuCommandService commandService)
            {
                var menuCommandID = new CommandID(CommandSet, CommandId);
                var menuItem = new MenuCommand(this.ShowToolWindow, menuCommandID);
                commandService.AddCommand(menuItem);
            }

            IVsWindowFrame windowFrame = GetWindowFrame();

            VSHelper.ProjectIsValid.Subscribe(ok =>
            {
                if (ok)
                {
                    Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
                }
                else
                {
                    Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.CloseFrame((uint)__FRAMECLOSE.FRAMECLOSE_SaveIfDirty));
                }
            });
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static SquirrelPackagerCommand Instance { get; private set; }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IServiceProvider ServiceProvider => this.package;

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static void Initialize(Package package) => Instance = new SquirrelPackagerCommand(package);

        public IVsWindowFrame GetWindowFrame()
        {
            // Get the instance number 0 of this tool window. This window is single instance so this
            // instance is actually the only one. The last flag is set to true so that if the tool
            // window does not exists it will be created.
            ToolWindowPane window = this.package.FindToolWindow(typeof(SquirrelPackager), 0, true);
            if ((window == null) || (window.Frame == null))
            {
                throw new NotSupportedException("Cannot create tool window");
            }

            return (IVsWindowFrame)window.Frame;
        }

        /// <summary>
        /// Shows the tool window when the menu item is clicked.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        /// <exception cref="NotSupportedException"></exception>
        private void ShowToolWindow(object sender, EventArgs e) => Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(GetWindowFrame().Show());
    }
}