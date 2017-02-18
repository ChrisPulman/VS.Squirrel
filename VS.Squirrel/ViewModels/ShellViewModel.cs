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
            SquirrelPackagerPackage._dte.Events.BuildEvents.OnBuildDone += (Scope, Action) =>
            {
                try
                {
                    if (VSHelper.SelectedProject.Value != null)
                    {
                        errorMessageShown = false;
                        lastproject = string.Empty;
                        this.Model = new AutoSquirrelModel();
                        VSHelper.SetProjectFiles(VSHelper.SelectedProject.Value);
                    }
                }
                catch
                {
                }
            };

            VSHelper.ProjectFiles.Where(x => x.Value != null && x.Key != null)
                .Select(x => new { Files = x.Value, Project = x.Key })
                .Throttle(TimeSpan.FromMilliseconds(500))
                .ObserveOn(DispatcherScheduler.Current)
                .Subscribe(x =>
                {
                    try
                    {
                        var IsSquirrelProject = x.Files.Any(s => s.Contains("Squirrel.dll"));
                        if (!IsSquirrelProject)
                        {
                            // Project is not using Squirrel
                            VSHelper.ProjectIsValid.Value = false;
                            return;
                        }

                        if (x.Project.FileName == lastproject && this.Model.IsValid)
                        {
                            // Nothing has changed just show the data
                            VSHelper.ProjectIsValid.Value = true;
                            return;
                        }

                        this.Model = new AutoSquirrelModel();

                        ItemLink targetItem = null;
                        foreach (var filePath in x.Files)
                        {
                            // exclude unwanted files
                            if (!filePath.Contains(".pdb") && !filePath.Contains(".nupkg") && !filePath.Contains(".vshost."))
                            {
                                this.Model.AddFile(filePath, targetItem);
                            }
                        }

                        if (string.IsNullOrWhiteSpace(this.Model.MainExePath))
                        {
                            // Project does not contain an exe at the root level
                            this.Model = new AutoSquirrelModel();
                            VSHelper.ProjectIsValid.Value = false;
                            return;
                        }
                        lastproject = x.Project.FileName;

                        this.Model.PackageFiles = AutoSquirrelModel.OrderFileList(this.Model.PackageFiles);
                        this.ProjectFilePath = Path.GetDirectoryName(x.Project.FileName);

                        var xmldoc = new XmlDocument();
                        xmldoc.Load(x.Project.FileName);
                        var mgr = new XmlNamespaceManager(xmldoc.NameTable);
                        mgr.AddNamespace("x", "http://schemas.microsoft.com/developer/msbuild/2003");
                        const string msbuild = "//x:";
                        ////// ApplicationIcon
                        foreach (XmlNode item in xmldoc.SelectNodes(msbuild + "ApplicationIcon", mgr))
                        {
                            this.Model.IconFilepath = Path.Combine(this.ProjectFilePath, item.InnerText);
                        }

                        // try to retrieve existing settings
                        var file = Path.Combine(this.ProjectFilePath, $"{this.Model.AppId}.asproj");
                        if (File.Exists(file))
                        {
                            AutoSquirrelModel m = FileUtility.Deserialize<AutoSquirrelModel>(file);
                            if (!string.IsNullOrWhiteSpace(m?.SelectedConnectionString))
                            {
                                this.Model.SelectedConnectionString = m.SelectedConnectionString;
                                this.Model.SelectedConnection = m.SelectedConnection;
                                this.FilePath = m.CurrentFilePath;
                                if (this.Model.SelectedConnection is FileSystemConnection fscon)
                                {
                                    if (string.IsNullOrWhiteSpace(fscon.FileSystemPath))
                                    {
                                        fscon.FileSystemPath = Path.Combine(this.FilePath, $"{this.Model.AppId}_files\\Releases");
                                    }
                                }
                                else if (this.Model.SelectedConnection is AmazonS3Connection s3con)
                                {
                                    if (string.IsNullOrWhiteSpace(s3con.FileSystemPath))
                                    {
                                        s3con.FileSystemPath = Path.Combine(this.FilePath, $"{this.Model.AppId}_files\\Releases");
                                    }
                                }
                            }
                        }

                        // If not able to read settings default to FileSystem settings
                        if (string.IsNullOrWhiteSpace(this.Model.SelectedConnectionString))
                        {
                            this.FilePath = this.ProjectFilePath;
                            this.Model.SelectedConnectionString = "File System";
                            if (!string.IsNullOrWhiteSpace(this.ProjectFilePath) && !string.IsNullOrWhiteSpace(this.Model.AppId) && this.Model.SelectedConnection is FileSystemConnection con)
                            {
                                con.FileSystemPath = Path.Combine(this.FilePath, $"{this.Model.AppId}_files\\Releases");
                            }
                        }
                        if (string.IsNullOrWhiteSpace(this.FilePath) && this.Model.SelectedConnection is FileSystemConnection fscon2)
                        {
                            this.FilePath = this.ProjectFilePath;
                            fscon2.FileSystemPath = Path.Combine(this.FilePath, $"{this.Model.AppId}_files\\Releases");
                        }

                        if (string.IsNullOrWhiteSpace(this.FilePath) && this.Model.SelectedConnection is AmazonS3Connection s3con2)
                        {
                            this.FilePath = this.ProjectFilePath;
                            s3con2.FileSystemPath = Path.Combine(this.FilePath, $"{this.Model.AppId}_files\\Releases");
                        }
                        this.Save();
                        if (this.Model.IsValid)
                        {
                            VSHelper.ProjectIsValid.Value = true;
                        }
                        else
                        {
                            if (!errorMessageShown)
                            {
                                MessageBox.Show(this.Model.Error, "Please correct the following issues with your project", MessageBoxButton.OK, MessageBoxImage.Error);
                                errorMessageShown = true;
                            }
                        }
                    }
                    catch (Exception)
                    {
                        VSHelper.ProjectIsValid.Value = false;
                    }
                });
        }

        /// <summary>
        /// Gets the abort package creation command.
        /// </summary>
        /// <value>The abort package creation command.</value>
        public ICommand AbortPackageCreationCmd => this._abortPackageCreationCmd ??
       (this._abortPackageCreationCmd = new DelegateCommand(this.AbortPackageCreation));

        /// <summary>
        /// Gets or sets the current package creation stage.
        /// </summary>
        /// <value>The current package creation stage.</value>
        public string CurrentPackageCreationStage
        {
            get => this._currentPackageCreationStage;

            set
            {
                this._currentPackageCreationStage = value;
                NotifyOfPropertyChange(() => this.CurrentPackageCreationStage);
            }
        }

        /// <summary>
        /// Gets or sets the file path.
        /// </summary>
        /// <value>The file path.</value>
        public string FilePath
        {
            get => this.Model.CurrentFilePath;

            set
            {
                // Check that Filepath has no spaces
                if (value.Contains(" "))
                {
                    SelectFilePath(value);
                    return;
                }
                this.Model.CurrentFilePath = value;
                NotifyOfPropertyChange(() => this.FilePath);
                var fp = "New Project*";
                if (!string.IsNullOrWhiteSpace(this.FilePath))
                {
                    fp = Path.GetFileNameWithoutExtension(this.FilePath);
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
            get => this._isBusy;

            set
            {
                this._isBusy = value;
                NotifyOfPropertyChange(() => this.IsBusy);
            }
        }

        /// <summary>
        /// Gets or sets the model.
        /// </summary>
        /// <value>The model.</value>
        public AutoSquirrelModel Model
        {
            get => this._model;

            set
            {
                this._model = value;
                NotifyOfPropertyChange(() => this.Model);
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
            get => this._toolVisibility;

            set
            {
                this._toolVisibility = value;
                NotifyOfPropertyChange(() => this.ToolVisibility);
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
                if (!string.IsNullOrWhiteSpace(this.FilePath))
                {
                    fp = Path.GetFileNameWithoutExtension(this.FilePath);
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
            if (this.ActiveBackgroungWorker != null)
            {
                this.ActiveBackgroungWorker.CancelAsync();

                if (this.exeProcess != null)
                {
                    this.exeProcess.Kill();
                }
            }

            this._abortPackageFlag = true;
        }

        /// <summary>
        /// Publishes the package.
        /// </summary>
        /// <exception cref="Exception">
        /// Package Details are invalid or incomplete ! or Selected connection details are not valid !
        /// </exception>
        public void PublishPackage()
        {
            try
            {
                if (this.ActiveBackgroungWorker?.IsBusy == true)
                {
                    Trace.TraceError("You shouldn't be here !");
                    return;
                }

                this.Model.UploadQueue.Clear();
                this.Model.RefreshPackageVersion(null);

                Trace.WriteLine("START PUBLISHING ! : " + this.Model.Title);

                // 1) Check validity
                if (!this.Model.IsValid)
                {
                    throw new Exception("Package Details are invalid or incomplete !");
                }

                if (this.Model.SelectedConnection == null || !this.Model.SelectedConnection.IsValid)
                {
                    throw new Exception("Selected connection details are not valid !");
                }

                Trace.WriteLine("DATA VALIDATE - OK ! ");

                Save();

                // I proceed only if i created the project .asproj file and directory I need existing
                // directory to create the packages.

                if (!this._isSaved)
                {
                    return;
                }

                this.IsBusy = true;

                this.ActiveBackgroungWorker = new BackgroundWorker() { WorkerReportsProgress = true, WorkerSupportsCancellation = true };

                this.ActiveBackgroungWorker.DoWork += this.ActiveBackgroungWorker_DoWork;
                this.ActiveBackgroungWorker.RunWorkerCompleted += this.PackageCreationCompleted;
                this.ActiveBackgroungWorker.ProgressChanged += this.ActiveBackgroungWorker_ProgressChanged;

                this.ActiveBackgroungWorker.RunWorkerAsync(this);
            }
            catch (Exception ex)
            {
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
            this._publishMode = 0;
            PublishPackage();
        }

        /// <summary>
        /// Publishes the package only update.
        /// </summary>
        public void PublishPackageOnlyUpdate()
        {
            this._publishMode = 1;
            PublishPackage();
        }

        /// <summary>
        /// Selects the file path.
        /// </summary>
        /// <param name="currentPath">The current path.</param>
        public void SelectFilePath(string currentPath)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();

            if (Directory.Exists(currentPath))
            {
                dialog.SelectedPath = currentPath;
                dialog.Description = "Please select a new File Path that does not contain Spaces.\r A new folder will be created here containing the Squirrel build for this project";
                dialog.ShowNewFolderButton = true;
            }

            System.Windows.Forms.DialogResult result = dialog.ShowDialog();

            if (result != System.Windows.Forms.DialogResult.OK)
            {
                return;
            }

            this.FilePath = dialog.SelectedPath;
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

            foreach (ItemLink node in model.PackageFiles)
            {
                AddFileToPackage(directoryBase, node, files);
            }

            builder.PopulateFiles("", files.ToArray());

            var nugetPath = model.NupkgOutputPath + model.AppId + "." + model.Version + ".nupkg";

            using (FileStream stream = File.Open(nugetPath, FileMode.OpenOrCreate))
            {
                builder.Save(stream);
            }

            return nugetPath;
        }

        internal void Save()
        {
            if (string.IsNullOrWhiteSpace(this.FilePath))
            {
                throw new Exception("File Path is invalid");
            }

            this.Model.NupkgOutputPath = this.FilePath + Path.DirectorySeparatorChar + this.Model.AppId + "_files" + PathFolderHelper.PackageDirectory;
            this.Model.SquirrelOutputPath = this.FilePath + Path.DirectorySeparatorChar + this.Model.AppId + "_files" + PathFolderHelper.ReleasesDirectory;

            if (!Directory.Exists(this.Model.NupkgOutputPath))
            {
                Directory.CreateDirectory(this.Model.NupkgOutputPath);
            }

            if (!Directory.Exists(this.Model.SquirrelOutputPath))
            {
                Directory.CreateDirectory(this.Model.SquirrelOutputPath);
            }

            FileUtility.SerializeToFile(Path.Combine(this.ProjectFilePath, $"{this.Model.AppId}.asproj"), this.Model);

            Trace.WriteLine("FILE SAVED ! : " + this.ProjectFilePath);

            this._isSaved = true;

            NotifyOfPropertyChange(() => this.WindowTitle);
        }

        private static void AddFileToPackage(string directoryBase, ItemLink node, List<ManifestFile> files)
        {
            // Don't add manifest if is directory

            if (node.IsDirectory)
            {
                directoryBase += "/" + node.Filename;

                foreach (ItemLink subNode in node.Children)
                {
                    AddFileToPackage(directoryBase, subNode, files);
                }
            }
            else
            {
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
            try
            {
                this.ActiveBackgroungWorker.ReportProgress(20, "NUGET PACKAGE CREATING");

                // Create Nuget Package from package treeview.
                var nugetPackagePath = CreateNugetPackage(this.Model);
                Trace.WriteLine("CREATED NUGET PACKAGE to : " + this.Model.NupkgOutputPath);

                if (this.ActiveBackgroungWorker.CancellationPending)
                {
                    return;
                }

                this.ActiveBackgroungWorker.ReportProgress(40, "SQUIRREL PACKAGE CREATING");

                // Releasify
                SquirrelReleasify(nugetPackagePath, this.Model.SquirrelOutputPath);
                Trace.WriteLine("CREATED SQUIRREL PACKAGE to : " + this.Model.SquirrelOutputPath);
            }
            catch (Exception ex)
            {
                e.Result = ex;
            }
        }

        private void ActiveBackgroungWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            //todo : Update busy indicator information.
            var message = e.UserState as string;
            if (message == null)
            {
                return;
            }

            this.CurrentPackageCreationStage = message;
        }

        /// <summary>
        /// Called on package created. Start the upload.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PackageCreationCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            this.IsBusy = false;

            this.CurrentPackageCreationStage = string.Empty;

            this.ActiveBackgroungWorker.Dispose();

            this.ActiveBackgroungWorker = null;

            if (this._abortPackageFlag)
            {
                if (this.Model.UploadQueue != null)
                {
                    this.Model.UploadQueue.Clear();
                }

                this._abortPackageFlag = false;

                return;
            }

            if (e.Result is Exception ex)
            {
                MessageBox.Show(ex.Message, "Package creation error", MessageBoxButton.OK, MessageBoxImage.Error);

                //todo : Manage generated error
                return;
            }

            if (e.Cancelled)
            {
                return;
            }

            // Start uploading generated files.
            this.Model.BeginUpdatedFiles(this._publishMode);
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

            if (File.Exists(this.Model.IconFilepath))
            {
                cmd += $@" -i {this.Model.IconFilepath}";
                cmd += $@" -setupIcon {this.Model.IconFilepath}";
            }

            var squirrel = Path.Combine(Path.GetDirectoryName(typeof(ShellViewModel).Assembly.Location), @"tools\Squirrel-Windows.exe");
            if (File.Exists(squirrel))
            {
                var startInfo = new ProcessStartInfo()
                {
                    WindowStyle = ProcessWindowStyle.Normal,
                    FileName = squirrel,

                    Arguments = cmd,
                    UseShellExecute = false
                };

                using (this.exeProcess = Process.Start(startInfo))
                {
                    try
                    {
                        this.exeProcess.WaitForExit();
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                }
            }
            else
            {
                MessageBox.Show(squirrel, "Error finding Squirrel", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}