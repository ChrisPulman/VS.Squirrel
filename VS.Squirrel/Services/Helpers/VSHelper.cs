using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Shell.Interop;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio;
using System.Runtime.InteropServices;
using System.IO;
using System.Xml;
using Reactive.Bindings;
using EnvDTE80;
using System.Diagnostics;
using System.Linq;

namespace AutoSquirrel.Services.Helpers
{
    public static class VSHelper
    {
        private static DTE2 _dte = SquirrelPackagerPackage._dte;
        private static string lsolDir;
        private static string lsolFile;
        private static string lsolUserOpts;
        private static string solDir;
        private static string solFile;
        private static string solUserOpts;

        public static ReactiveProperty<string> BuildPath { get; } = new ReactiveProperty<string>();

        public static ReactiveProperty<IEnumerable<string>> ProjectFiles { get; } = new ReactiveProperty<IEnumerable<string>>();

        public static ReactiveProperty<Project> SelectedProject { get; } = new ReactiveProperty<Project>();

        public static ProjectItem AddFileToProject(this Project project, FileInfo file, string itemType = null)

        {
            if (project.IsKind(ProjectTypes.ASPNET_5, ProjectTypes.SSDT))
            {
                return _dte.Solution.FindProjectItem(file.FullName);
            }

            var root = project.GetRootFolder();

            if (string.IsNullOrEmpty(root) || !file.FullName.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            ProjectItem item = project.ProjectItems.AddFromFile(file.FullName);

            item.SetItemType(itemType);

            return item;
        }

        public static string CleanNameSpace(string ns, bool stripPeriods = true)
        {
            if (stripPeriods)
            {
                ns = ns.Replace(".", "");
            }

            ns = ns.Replace(" ", "")
                     .Replace("-", "")
                     .Replace("\\", ".");

            return ns;
        }

        public static Project GetActiveProject()
        {
            try
            {
                if (_dte.ActiveSolutionProjects is Array activeSolutionProjects && activeSolutionProjects.Length > 0)
                {
                    return activeSolutionProjects.GetValue(0) as Project;
                }

                Document doc = _dte.ActiveDocument;

                if (doc != null && !string.IsNullOrEmpty(doc.FullName))

                {
                    ProjectItem item = _dte.Solution?.FindProjectItem(doc.FullName);

                    if (item != null)
                    {
                        return item.ContainingProject;
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Error getting the active project" + ex);
            }

            return null;
        }

        public static Project GetDTEProject(IVsHierarchy hierarchy)
        {
            if (hierarchy == null)
            {
                return null;
            }

            hierarchy.GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ExtObject, out object obj);
            return obj as Project;
        }

        public static IEnumerable<Project> GetProjects(IVsSolution solution)
        {
            foreach (IVsHierarchy hier in GetProjectsInSolution(solution))
            {
                Project project = GetDTEProject(hier);
                if (project != null)
                {
                    yield return project;
                }
            }
        }

        public static IEnumerable<IVsHierarchy> GetProjectsInSolution(IVsSolution solution) => GetProjectsInSolution(solution, __VSENUMPROJFLAGS.EPF_LOADEDINSOLUTION);

        public static IEnumerable<IVsHierarchy> GetProjectsInSolution(IVsSolution solution, __VSENUMPROJFLAGS flags)
        {
            if (solution == null)
            {
                yield break;
            }

            Guid guid = Guid.Empty;
            solution.GetProjectEnum((uint)flags, ref guid, out var enumHierarchies);
            if (enumHierarchies == null)
            {
                yield break;
            }

            IVsHierarchy[] hierarchy = new IVsHierarchy[1];
            while (enumHierarchies.Next(1, hierarchy, out uint fetched) == VSConstants.S_OK && fetched == 1)
            {
                if (hierarchy.Length > 0 && hierarchy[0] != null)
                {
                    yield return hierarchy[0];
                }
            }
        }

        public static string GetRootFolder(this Project project)
        {
            if (project == null)
            {
                return null;
            }

            if (project.IsKind(ProjectKinds.vsProjectKindSolutionFolder))
            {
                return Path.GetDirectoryName(_dte.Solution.FullName);
            }

            if (string.IsNullOrEmpty(project.FullName))
            {
                return null;
            }

            string fullPath;

            try
            {
                fullPath = project.Properties.Item("FullPath").Value as string;
            }
            catch (ArgumentException)

            {
                try
                {
                    // MFC projects don't have FullPath, and there seems to be no way to query existence
                    fullPath = project.Properties.Item("ProjectDirectory").Value as string;
                }
                catch (ArgumentException)

                {
                    // Installer projects have a ProjectPath.
                    fullPath = project.Properties.Item("ProjectPath").Value as string;
                }
            }

            if (string.IsNullOrEmpty(fullPath))
            {
                return File.Exists(project.FullName) ? Path.GetDirectoryName(project.FullName) : null;
            }

            if (Directory.Exists(fullPath))
            {
                return fullPath;
            }

            if (File.Exists(fullPath))
            {
                return Path.GetDirectoryName(fullPath);
            }

            return null;
        }

        public static string GetRootNamespace(this Project project)
        {
            if (project == null)
            {
                return null;
            }

            var ns = project.Name ?? string.Empty;

            try
            {
                Property prop = project.Properties.Item("RootNamespace");

                if (prop?.Value != null && !string.IsNullOrEmpty(prop.Value.ToString()))
                {
                    ns = prop.Value.ToString();
                }
            }
            catch { /* Project doesn't have a root namespace */ }

            return CleanNameSpace(ns, stripPeriods: false);
        }

        public static object GetSelectedItem()
        {
            object selectedObject = null;

            var monitorSelection = (IVsMonitorSelection)Package.GetGlobalService(typeof(SVsShellMonitorSelection));

            try
            {
                monitorSelection.GetCurrentSelection(out IntPtr hierarchyPointer, out var itemId, out IVsMultiItemSelect multiItemSelect, out IntPtr selectionContainerPointer);

                if (Marshal.GetTypedObjectForIUnknown(hierarchyPointer, typeof(IVsHierarchy)) is IVsHierarchy selectedHierarchy)

                {
                    ErrorHandler.ThrowOnFailure(selectedHierarchy.GetProperty(itemId, (int)__VSHPROPID.VSHPROPID_ExtObject, out selectedObject));
                }

                Marshal.Release(hierarchyPointer);

                Marshal.Release(selectionContainerPointer);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);
            }

            return selectedObject;
        }

        public static void GetSolutionProjects()
        {
            // get current solution
            var solution = (IVsSolution)Package.GetGlobalService(typeof(IVsSolution));
            solution.GetSolutionInfo(out solDir, out solFile, out solUserOpts);
            if (solDir == lsolDir && solFile == lsolFile && solUserOpts == lsolUserOpts)
            {
                return;
            }

            lsolDir = solDir;
            lsolFile = solFile;
            lsolUserOpts = solUserOpts;
            foreach (Project project in GetProjects(solution))
            {
                var directoryName = Path.GetDirectoryName(project.FileName);
                var fileName = Path.GetFileName(project.FileName);

                if (directoryName == null || fileName == null)
                {
                    return;
                }

                var directory = new DirectoryInfo(directoryName);
                var fileInfo = new FileInfo(fileName);

                var xmldoc = new XmlDocument();
                xmldoc.Load(project.FileName);

                var mgr = new XmlNamespaceManager(xmldoc.NameTable);
                mgr.AddNamespace("x", "http://schemas.microsoft.com/developer/msbuild/2003");

                foreach (XmlNode item in xmldoc.SelectNodes("//x:OutputPath", mgr))
                {
                    var test = item.InnerText;
                }
            }
        }

        public static bool IsKind(this Project project, params string[] kindGuids)
        {
            foreach (var guid in kindGuids)

            {
                if (project.Kind.Equals(guid, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public static void SetCurrentProject(IVsHierarchy pHierNew, uint itemidNew)
        {
            try
            {
                // try to get the active project first
                Project activeProject = GetActiveProject();
                if (activeProject != null)
                {
                    VSHelper.SetProjectFiles(activeProject);
                    VSHelper.SelectedProject.Value = activeProject;
                    return;
                }
            }
            catch
            {
            }

            if (pHierNew != null)
            {
                try
                {
                    ErrorHandler.ThrowOnFailure(pHierNew.GetProperty(itemidNew, (int)__VSHPROPID.VSHPROPID_ExtObject, out var selectedObject));
                    if (selectedObject is Project project)
                    {
                        VSHelper.SetProjectFiles(project);
                        VSHelper.SelectedProject.Value = project;
                    }
                }
                catch
                {
                }
            }
        }

        public static void SetItemType(this ProjectItem item, string itemType)

        {
            try

            {
                if (item == null || item.ContainingProject == null)
                {
                    return;
                }

                if (string.IsNullOrEmpty(itemType)
                                        || item.ContainingProject.IsKind(ProjectTypes.WEBSITE_PROJECT)
                                                            || item.ContainingProject.IsKind(ProjectTypes.UNIVERSAL_APP))
                {
                    return;
                }

                item.Properties.Item("ItemType").Value = itemType;
            }
            catch (Exception ex)

            {
                Trace.WriteLine(ex);
            }
        }

        public static void SetProjectFiles(Project project)
        {
            if (string.IsNullOrWhiteSpace(project.FileName))
            {
                return;
            }

            var directoryName = Path.GetDirectoryName(project.FileName);
            var fileName = Path.GetFileName(project.FileName);

            if (directoryName == null || fileName == null)
            {
                return;
            }

            //do work
            var xmldoc = new XmlDocument();
            xmldoc.Load(project.FileName);

            var mgr = new XmlNamespaceManager(xmldoc.NameTable);
            mgr.AddNamespace("x", "http://schemas.microsoft.com/developer/msbuild/2003");
            const string msbuild = "//x:";

            ConfigurationManager man = project.ConfigurationManager;

            ////// Configuration
            var config = man.ActiveConfiguration.ConfigurationName;

            ////// Platform
            var platform = man.ActiveConfiguration.PlatformName.Replace(" ", "");

            ////// PropertyGroup Condition=" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' "
            foreach (XmlNode item in xmldoc.SelectNodes(msbuild + "PropertyGroup", mgr))
            {
                if (item.HasChildNodes)
                {
                    foreach (XmlNode test in item.ChildNodes)
                    {
                        if (test.Name == "OutputPath")
                        {
                            if (item.Attributes.GetNamedItem("Condition").Value.Contains($"{config}|{platform}"))
                            {
                                VSHelper.BuildPath.Value = Path.Combine(directoryName, test.InnerText);
                            }
                        }
                    }
                }
            }

            if (VSHelper.BuildPath.Value != string.Empty)
            {
                VSHelper.ProjectFiles.Value = Directory.EnumerateFileSystemEntries(VSHelper.BuildPath.Value);
            }
        }

        private static IEnumerable<Project> GetChildProjects(Project parent)
        {
            try
            {
                if (!parent.IsKind(ProjectKinds.vsProjectKindSolutionFolder) && parent.Collection == null)  // Unloaded
                {
                    return Enumerable.Empty<Project>();
                }

                if (!string.IsNullOrEmpty(parent.FullName))
                {
                    return new[] { parent };
                }
            }
            catch (COMException)
            {
                return Enumerable.Empty<Project>();
            }

            return parent.ProjectItems
                                    .Cast<ProjectItem>()
                                        .Where(p => p.SubProject != null)
                                        .SelectMany(p => GetChildProjects(p.SubProject));
        }

        public static class ProjectTypes
        {
            public const string ASPNET_5 = "{8BB2217D-0F2D-49D1-97BC-3654ED321F3B}";
            public const string DOTNET_Core = "{9A19103F-16F7-4668-BE54-9A1E7A4F7556}";
            public const string NODE_JS = "{9092AA53-FB77-4645-B42D-1CCCA6BD08BD}";
            public const string SSDT = "{00d1a9c2-b5f0-4af3-8072-f6c62b433612}";
            public const string UNIVERSAL_APP = "{262852C6-CD72-467D-83FE-5EEB1973A190}";
            public const string WEBSITE_PROJECT = "{E24C65DC-7377-472B-9ABA-BC803B73C61A}";
        }
    }
}