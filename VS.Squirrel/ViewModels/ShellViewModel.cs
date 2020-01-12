namespace AutoSquirrel
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Windows;
    using System.Windows.Input;
    using Caliburn.Micro;
    using NuGet;
    using AutoSquirrel.Services.Helpers;
    using System.Reactive.Linq;
    using System.Xml;
    using System.Reactive.Concurrency;

    /// <summary>
    /// Shell View Model
    /// </summary>
    /// <seealso cref="Caliburn.Micro.ViewAware"/>
    public class ShellViewModel : ViewAware
    {
        internal BackgroundWorker ActiveBackgroungWorker;
        private ICommand _abortPackageCreationCmd;
        private bool _abortPackageFlag;
        private string _currentPackageCreationStage;
        private bool _isBusy;
        private bool _isSaved;
        private AutoSquirrelModel _model;
        private int _publishMode;
        private Visibility _toolVisibility = Visibility.Visible;
        private Process exeProcess;

        /// <summary>
        /// Initializes a new instance of the <see cref="ShellViewModel"/> class.
        /// </summary>
        public ShellViewModel()
        {
            var lastproject = string.Empty;
            var errorMessageShown = false;
            AutoSquirrelPackage.DesignTimeEnviroment.Events.BuildEvents.OnBuildDone += (Scope, Action) => {
                try {
                    if (VSHelper.Options != null
                    && VSHelper.SelectedProject.Value != null
                    && VSHelper.Options.ShowUI
                    && (VSHelper.Options.UseDebug || VSHelper.Options.UseRelease)) {
                        errorMessageShown = false;
                        lastproject = string.Empty;
                        Model = new AutoSquirrelModel();
                        VSHelper.SetProjectFiles(VSHelper.SelectedProject.Value);
                    }
                } catch {
                }
            };

            VSHelper.ProjectFiles.Where(x => x.Value != null && x.Key != null)
                .Throttle(TimeSpan.FromMilliseconds(500))
                .Select(x => new { Files = x.Value, Project = x.Key })
                .ObserveOn(DispatcherScheduler.Current)
                .Subscribe(x => {
                    try {
                        var IsSquirrelProject = x.Files.Any(s => s.Contains("Squirrel.dll"));
                        if (!IsSquirrelProject) {
                            // Project is not using Squirrel
                            VSHelper.ProjectIsValid.Value = false;
                            return;
                        }

                        if (x.Project.FileName == lastproject && Model.IsValid) {
                            // Nothing has changed just show the data
                            VSHelper.ProjectIsValid.Value = true;
                            return;
                        }

                        Model = new AutoSquirrelModel();

                        ItemLink targetItem = null;
                        foreach (var filePath in x.Files) {
                            // exclude unwanted files
                            if (!filePath.Contains(".pdb") && !filePath.Contains(".nupkg") && !filePath.Contains(".vshost.")) {
                                Model.AddFile(filePath, targetItem);
                            }
                        }

                        if (string.IsNullOrWhiteSpace(Model.MainExePath)) {
                            // Project does not contain an exe at the root level
                            Model = new AutoSquirrelModel();
                            VSHelper.ProjectIsValid.Value = false;
                            return;
                        }
                        lastproject = x.Project.FileName;

                        Model.PackageFiles = AutoSquirrelModel.OrderFileList(Model.PackageFiles);
                        ProjectFilePath = Path.GetDirectoryName(x.Project.FileName);

                        var xmldoc = new XmlDocument();
                        xmldoc.Load(x.Project.FileName);
                        var mgr = new XmlNamespaceManager(xmldoc.NameTable);
                        mgr.AddNamespace("x", "http://schemas.microsoft.com/developer/msbuild/2003");
                        const string msbuild = "//x:";
                        ////// ApplicationIcon
                        foreach (XmlNode item in xmldoc.SelectNodes(msbuild + "ApplicationIcon", mgr)) {
                            Model.IconFilepath = Path.Combine(ProjectFilePath, item.InnerText);
                        }

                        // try to retrieve existing settings
                        var file = Path.Combine(ProjectFilePath, $"{Model.AppId}.asproj");
                        if (File.Exists(file)) {
                            AutoSquirrelModel m = FileUtility.Deserialize<AutoSquirrelModel>(file);
                            if (m != null) {
                                Model.IconFilepath = m.IconFilepath;
                                if (!string.IsNullOrWhiteSpace(m?.SelectedConnectionString)) {
                                    Model.SelectedConnectionString = m.SelectedConnectionString;
                                    Model.SelectedConnection = m.SelectedConnection;
                                    FilePath = m.CurrentFilePath;
                                    if (Model.SelectedConnection is FileSystemConnection fscon) {
                                        if (string.IsNullOrWhiteSpace(fscon.FileSystemPath)) {
                                            fscon.FileSystemPath = Path.Combine(FilePath, $"{Model.AppId}_files\\Releases");
                                        }
                                    } else if (Model.SelectedConnection is AmazonS3Connection s3con) {
                                        if (string.IsNullOrWhiteSpace(s3con.FileSystemPath)) {
                                            s3con.FileSystemPath = Path.Combine(FilePath, $"{Model.AppId}_files\\Releases");
                                        }
                                    }
                                }
                            }
                        }

                        // If not able to read settings default to FileSystem settings
                        if (string.IsNullOrWhiteSpace(Model.SelectedConnectionString)) {
                            FilePath = ProjectFilePath;
                            Model.SelectedConnectionString = "File System";
                            if (!string.IsNullOrWhiteSpace(ProjectFilePath) && !string.IsNullOrWhiteSpace(Model.AppId) && Model.SelectedConnection is FileSystemConnection con) {
                                con.FileSystemPath = Path.Combine(FilePath, $"{Model.AppId}_files\\Releases");
                            }
                        }
                        if (string.IsNullOrWhiteSpace(FilePath) && Model.SelectedConnection is FileSystemConnection fscon2) {
                            FilePath = ProjectFilePath;
                            fscon2.FileSystemPath = Path.Combine(FilePath, $"{Model.AppId}_files\\Releases");
                        }

                        if (string.IsNullOrWhiteSpace(FilePath) && Model.SelectedConnection is AmazonS3Connection s3con2) {
                            FilePath = ProjectFilePath;
                            s3con2.FileSystemPath = Path.Combine(FilePath, $"{Model.AppId}_files\\Releases");
                        }
                        Save();
                        if (Model.IsValid) {
                            VSHelper.ProjectIsValid.Value = true;
                        } else {
                            if (!errorMessageShown) {
                                MessageBox.Show(Model.Error, "Please correct the following issues with your project", MessageBoxButton.OK, MessageBoxImage.Error);
                                errorMessageShown = true;
                            }
                        }
                    } catch (Exception) {
                        VSHelper.ProjectIsValid.Value = false;
                    }
                });
        }

        /// <summary>
        /// Gets the abort package creation command.
        /// </summary>
        /// <value>The abort package creation command.</value>
        public ICommand AbortPackageCreationCmd => _abortPackageCreationCmd ??
       (_abortPackageCreationCmd = new DelegateCommand(AbortPackageCreation));

        /// <summary>
        /// Gets or sets the current package creation stage.
        /// </summary>
        /// <value>The current package creation stage.</value>
        public string CurrentPackageCreationStage
        {
            get => _currentPackageCreationStage;

            set
            {
                _currentPackageCreationStage = value;
                NotifyOfPropertyChange(() => CurrentPackageCreationStage);
            }
        }

        /// <summary>
        /// Gets or sets the file path.
        /// </summary>
        /// <value>The file path.</value>
        public string FilePath
        {
            get => Model.CurrentFilePath;

            set
            {
                // Check that Filepath has no spaces
                if (value.Contains(" ")) {
                    SelectFilePath(value);
                    return;
                }
                Model.CurrentFilePath = value;
                NotifyOfPropertyChange(() => FilePath);
                var fp = "New Project*";
                if (!string.IsNullOrWhiteSpace(FilePath)) {
                    fp = Path.GetFileNameWithoutExtension(FilePath);
                }
                VSHelper.Caption.Value = $"Squirrel Packager {PathFolderHelper.GetProgramVersion()} - {fp}";
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is busy.
        /// </summary>
        /// <value><c>true</c> if this instance is busy; otherwise, <c>false</c>.</value>
        public bool IsBusy
        {
            get => _isBusy;

            set
            {
                _isBusy = value;
                NotifyOfPropertyChange(() => IsBusy);
            }
        }

        /// <summary>
        /// Gets or sets the model.
        /// </summary>
        /// <value>The model.</value>
        public AutoSquirrelModel Model
        {
            get => _model;

            set
            {
                _model = value;
                NotifyOfPropertyChange(() => Model);
            }
        }

        /// <summary>
        /// Gets the project file path.
        /// </summary>
        /// <value>The project file path.</value>
        public string ProjectFilePath { get; private set; }

        /// <summary>
        /// Gets or sets the tool visibility.
        /// </summary>
        /// <value>The tool visibility.</value>
        public Visibility ToolVisibility
        {
            get => _toolVisibility;

            set
            {
                _toolVisibility = value;
                NotifyOfPropertyChange(() => ToolVisibility);
            }
        }

        /// <summary>
        /// Gets the window title.
        /// </summary>
        /// <value>The window title.</value>
        public string WindowTitle
        {
            get
            {
                var fp = "New Project*";
                if (!string.IsNullOrWhiteSpace(FilePath)) {
                    fp = Path.GetFileNameWithoutExtension(FilePath);
                }
                VSHelper.Caption.Value = $"Squirrel Packager {PathFolderHelper.GetProgramVersion()} - {fp}";
                return VSHelper.Caption.Value;
            }
        }

        /// <summary>
        /// Gets the window title.
        /// </summary>
        /// <value>The window title.</value>
        /// <summary>
        /// Aborts the package creation.
        /// </summary>
        /// <param name="parm">The parm.</param>
        public void AbortPackageCreation(object parm)
        {
            if (ActiveBackgroungWorker != null) {
                ActiveBackgroungWorker.CancelAsync();

                if (exeProcess != null) {
                    exeProcess.Kill();
                }
            }

            _abortPackageFlag = true;
        }

        /// <summary>
        /// Publishes the package.
        /// </summary>
        /// <exception cref="Exception">
        /// Package Details are invalid or incomplete ! or Selected connection details are not valid !
        /// </exception>
        public void PublishPackage()
        {
            try {
                if (ActiveBackgroungWorker?.IsBusy == true) {
                    Trace.TraceError("You shouldn't be here !");
                    return;
                }

                Model.UploadQueue.Clear();
                Model.RefreshPackageVersion(null);

                Trace.WriteLine("START PUBLISHING ! : " + Model.Title);

                // 1) Check validity
                if (!Model.IsValid) {
                    throw new Exception("Package Details are invalid or incomplete !");
                }

                if (Model.SelectedConnection == null || !Model.SelectedConnection.IsValid) {
                    throw new Exception("Selected connection details are not valid !");
                }

                Trace.WriteLine("DATA VALIDATE - OK ! ");

                Save();

                // I proceed only if i created the project .asproj file and directory I need existing
                // directory to create the packages.

                if (!_isSaved) {
                    return;
                }

                IsBusy = true;

                ActiveBackgroungWorker = new BackgroundWorker() { WorkerReportsProgress = true, WorkerSupportsCancellation = true };

                ActiveBackgroungWorker.DoWork += ActiveBackgroungWorker_DoWork;
                ActiveBackgroungWorker.RunWorkerCompleted += PackageCreationCompleted;
                ActiveBackgroungWorker.ProgressChanged += ActiveBackgroungWorker_ProgressChanged;

                ActiveBackgroungWorker.RunWorkerAsync(this);
            } catch (Exception ex) {
                MessageBox.Show(ex.ToString(), "Error on publishing", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 1) Check field validity
        /// 2) Create Nuget package
        /// 3) Squirrel relasify
        /// 4) Publish to amazon the updated file ( to get the update file , search the timedate &gt;
        ///    of building time ) /// - Possibly in async way..
        /// - Must be callable from command line, so i can optionally start this process from at the
        ///   end of visual studio release build
        /// </summary>
        public void PublishPackageComplete()
        {
            _publishMode = 0;
            PublishPackage();
        }

        /// <summary>
        /// Publishes the package only update.
        /// </summary>
        public void PublishPackageOnlyUpdate()
        {
            _publishMode = 1;
            PublishPackage();
        }

        /// <summary>
        /// Selects the file path.
        /// </summary>
        /// <param name="currentPath">The current path.</param>
        public void SelectFilePath(string currentPath)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();

            if (Directory.Exists(currentPath)) {
                dialog.SelectedPath = currentPath;
                dialog.Description = "Please select a new File Path that does not contain Spaces.\r A new folder will be created here containing the Squirrel build for this project";
                dialog.ShowNewFolderButton = true;
            }

            var result = dialog.ShowDialog();

            if (result != System.Windows.Forms.DialogResult.OK) {
                return;
            }

            FilePath = dialog.SelectedPath;
        }

        internal string CreateNugetPackage(AutoSquirrelModel model)
        {
            var metadata = new ManifestMetadata()
            {
                Id = model.AppId,
                Authors = model.Authors,
                Version = model.Version,
                Description = model.Description,
                Title = model.Title,
            };

            var builder = new PackageBuilder();
            builder.Populate(metadata);

            //As Squirrel convention i put everything in lib/net45 folder

            const string directoryBase = "/lib/net45";

            var files = new List<ManifestFile>();

            foreach (ItemLink node in model.PackageFiles) {
                AddFileToPackage(directoryBase, node, files);
            }

            builder.PopulateFiles("", files.ToArray());

            var nugetPath = model.NupkgOutputPath + model.AppId + "." + model.Version + ".nupkg";

            using (var stream = File.Open(nugetPath, FileMode.OpenOrCreate)) {
                builder.Save(stream);
            }

            return nugetPath;
        }

        internal void Save()
        {
            if (string.IsNullOrWhiteSpace(FilePath)) {
                throw new Exception("File Path is invalid");
            }

            Model.NupkgOutputPath = FilePath + Path.DirectorySeparatorChar + Model.AppId + "_files" + PathFolderHelper.PackageDirectory;
            Model.SquirrelOutputPath = FilePath + Path.DirectorySeparatorChar + Model.AppId + "_files" + PathFolderHelper.ReleasesDirectory;

            if (!Directory.Exists(Model.NupkgOutputPath)) {
                Directory.CreateDirectory(Model.NupkgOutputPath);
            }

            if (!Directory.Exists(Model.SquirrelOutputPath)) {
                Directory.CreateDirectory(Model.SquirrelOutputPath);
            }

            FileUtility.SerializeToFile(Path.Combine(ProjectFilePath, $"{Model.AppId}.asproj"), Model);

            Trace.WriteLine("FILE SAVED ! : " + ProjectFilePath);

            _isSaved = true;

            NotifyOfPropertyChange(() => WindowTitle);
        }

        private static void AddFileToPackage(string directoryBase, ItemLink node, List<ManifestFile> files)
        {
            // Don't add manifest if is directory

            if (node.IsDirectory) {
                directoryBase += "/" + node.Filename;

                foreach (var subNode in node.Children) {
                    AddFileToPackage(directoryBase, subNode, files);
                }
            } else {
                var manifest = new ManifestFile()
                {
                    Source = node.SourceFilepath
                };

                manifest.Target = directoryBase + "/" + node.Filename;

                files.Add(manifest);
            }
        }

        private void ActiveBackgroungWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            try {
                ActiveBackgroungWorker.ReportProgress(20, "NUGET PACKAGE CREATING");

                // Create Nuget Package from package treeview.
                var nugetPackagePath = CreateNugetPackage(Model);
                Trace.WriteLine("CREATED NUGET PACKAGE to : " + Model.NupkgOutputPath);

                if (ActiveBackgroungWorker.CancellationPending) {
                    return;
                }

                ActiveBackgroungWorker.ReportProgress(40, "SQUIRREL PACKAGE CREATING");

                // Releasify
                SquirrelReleasify(nugetPackagePath, Model.SquirrelOutputPath);
                Trace.WriteLine("CREATED SQUIRREL PACKAGE to : " + Model.SquirrelOutputPath);
            } catch (Exception ex) {
                e.Result = ex;
            }
        }

        private void ActiveBackgroungWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            //todo : Update busy indicator information.
            var message = e.UserState as string;
            if (message == null) {
                return;
            }

            CurrentPackageCreationStage = message;
        }

        /// <summary>
        /// Called on package created. Start the upload.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void PackageCreationCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            IsBusy = false;

            CurrentPackageCreationStage = string.Empty;

            ActiveBackgroungWorker.Dispose();

            ActiveBackgroungWorker = null;

            if (_abortPackageFlag) {
                if (Model.UploadQueue != null) {
                    Model.UploadQueue.Clear();
                }

                _abortPackageFlag = false;

                return;
            }

            if (e.Result is Exception ex) {
                MessageBox.Show(ex.Message, "Package creation error", MessageBoxButton.OK, MessageBoxImage.Error);

                //todo : Manage generated error
                return;
            }

            if (e.Cancelled) {
                return;
            }

            // Start uploading generated files.
            await Model.BeginUpdatedFilesAsync(_publishMode);
        }

        private void SquirrelReleasify(string nugetPackagePath, string squirrelOutputPath)
        {
            /*
            https://github.com/Squirrel/Squirrel.Windows/blob/c86d3d0f19418d9f31d244f9c1d96d25a9c0dfb6/src/Update/Program.cs
                    "Options:",
                    { "h|?|help", "Display Help and exit", _ => {} },
                    { "r=|releaseDir=", "Path to a release directory to use with releasify", v => releaseDir = v},
                    { "p=|packagesDir=", "Path to the NuGet Packages directory for C# apps", v => packagesDir = v},
                    { "bootstrapperExe=", "Path to the Setup.exe to use as a template", v => bootstrapperExe = v},
                    { "g=|loadingGif=", "Path to an animated GIF to be displayed during installation", v => backgroundGif = v},
                    { "i=|icon", "Path to an ICO file that will be used for icon shortcuts", v => icon = v},
                    { "setupIcon=", "Path to an ICO file that will be used for the Setup executable's icon", v => setupIcon = v},
                    { "n=|signWithParams=", "Sign the installer via SignTool.exe with the parameters given", v => signingParameters = v},
                    { "s|silent", "Silent install", _ => silentInstall = true},
                    { "b=|baseUrl=", "Provides a base URL to prefix the RELEASES file packages with", v => baseUrl = v, true},
                    { "a=|process-start-args=", "Arguments that will be used when starting executable", v => processStartArgs = v, true},
                    { "l=|shortcut-locations=", "Comma-separated string of shortcut locations, e.g. 'Desktop,StartMenu'", v => shortcutArgs = v},
                    { "no-msi", "Don't generate an MSI package", v => noMsi = true},
            */
            var cmd = $@" -releasify {nugetPackagePath} -releaseDir {squirrelOutputPath} -l 'Desktop'";

            if (File.Exists(Model.IconFilepath)) {
                cmd += $@" -i {Model.IconFilepath}";
                cmd += $@" -setupIcon {Model.IconFilepath}";
            }

            var squirrel = Path.Combine(Path.GetDirectoryName(typeof(ShellViewModel).Assembly.Location), @"tools\Squirrel-Windows.exe");
            if (File.Exists(squirrel)) {
                var startInfo = new ProcessStartInfo()
                {
                    WindowStyle = ProcessWindowStyle.Minimized,
                    FileName = squirrel,

                    Arguments = cmd,
                    UseShellExecute = false
                };

                using (exeProcess = Process.Start(startInfo)) {
                    try {
                        exeProcess.WaitForExit();
                    } catch (Exception) {
                        throw;
                    }
                }
            } else {
                MessageBox.Show(squirrel, "Error finding Squirrel", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
