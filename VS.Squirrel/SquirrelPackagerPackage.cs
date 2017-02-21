﻿//------------------------------------------------------------------------------
// <copyright file="SquirrelPackagerPackage.cs" company="AIC Solutions Ltd">
//     Copyright (c) AIC Solutions Ltd.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Win32;
using EnvDTE80;
using EnvDTE;
using AutoSquirrel.Services.Helpers;
using System.Threading;
using System.Threading.Tasks;

namespace AutoSquirrel
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio is to
    /// implement the IVsPackage interface and register itself with the shell. This package uses the
    /// helper classes defined inside the Managed Package Framework (MPF) to do it: it derives from
    /// the Package class that provides the implementation of the IVsPackage interface and uses the
    /// registration attributes defined in the framework to register itself and its components with
    /// the shell. These attributes tell the pkgdef creation utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset
    /// Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("#110", "#112", System.AssemblyVersionInformation.Version, IconResourceID = 400)] // Info on this package for Help/About
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideToolWindow(typeof(SquirrelPackager), Style = VsDockStyle.Tabbed, Window = EnvDTE.Constants.vsViewKindDesigner)]
    [ProvideOptionPage(typeof(OptionsPage), "VS.Squirrel", "General", 0, 0, supportsAutomation: true)]
    [Guid(SquirrelPackagerPackage.PackageGuidString)]
    [ProvideAutoLoad(UIContextGuids80.SolutionExists)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    public sealed class SquirrelPackagerPackage : AsyncPackage
    {
        /// <summary>
        /// SquirrelPackagerPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "0f423afa-e287-4ebc-b319-12bc988977f0";

        public static DTE2 DesignTimeEnviroment;

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so
        /// this is the place where you can put all the initialization code that rely on services
        /// provided by VisualStudio.
        /// </summary>
        protected override async System.Threading.Tasks.Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            DesignTimeEnviroment = await GetServiceAsync(typeof(DTE)) as DTE2;
            SquirrelPackagerCommand.Initialize(this);
            base.Initialize();
            VSHelper.ProjectIsValid.Value = false;
        }
    }
}