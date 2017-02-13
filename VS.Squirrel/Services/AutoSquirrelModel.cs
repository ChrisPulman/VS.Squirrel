namespace AutoSquirrel
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net.Cache;
    using System.Runtime.Serialization;
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using FluentValidation;
    using FluentValidation.Results;
    using System.Reactive.Linq;

    /// <summary>
    /// Auto Squirrel Model
    /// </summary>
    /// <seealso cref="AutoSquirrel.PropertyChangedBaseValidable"/>
    [DataContract]
    public class AutoSquirrelModel : PropertyChangedBaseValidable
    {
        [DataMember]
        internal List<WebConnectionBase> CachedConnection = new List<WebConnectionBase>();

        private ICommand _addDirectoryCmd;
        private string _appId;
        private string _authors;
        private List<string> _availableUploadLocation;
        private string _description;
        private ICommand _editConnectionCmd;
        private string _iconFilepath;
        private string _mainExePath;
        private string _nupkgOutputPath;
        private ObservableCollection<ItemLink> _packageFiles = new ObservableCollection<ItemLink>();
        private ICommand _refreshVersionNumber;
        private ICommand _removeAllItemsCmd;
        private ICommand _removeItemCmd;
        private WebConnectionBase _selectedConnection;
        private string _selectedConnectionString;
        private SingleFileUpload _selectedUploadItem;
        private ICommand _selectIconCmd;
        private bool _setVersionManually;
        private string _squirrelOutputPath;
        private string _title;
        private ObservableCollection<SingleFileUpload> _uploadQueue = new ObservableCollection<SingleFileUpload>();
        private string _version;
        private string newFolderName = "NEW FOLDER";
        private ItemLink selectedItem = new ItemLink();

        /// <summary>
        /// Initializes a new instance of the <see cref="AutoSquirrelModel"/> class.
        /// </summary>
        public AutoSquirrelModel() => this.PackageFiles = new ObservableCollection<ItemLink>();

        /// <summary>
        /// Gets the add directory command.
        /// </summary>
        /// <value>The add directory command.</value>
        public ICommand AddDirectoryCmd => this._addDirectoryCmd ??
       (this._addDirectoryCmd = new DelegateCommand(this.AddDirectory));

        /// <summary>
        /// Gets or sets the application identifier.
        /// </summary>
        /// <value>The application identifier.</value>
        [DataMember]
        public string AppId
        {
            get => this._appId;

            set
            {
                this._appId = value;
                NotifyOfPropertyChange(() => this.AppId);
            }
        }

        /// <summary>
        /// Gets or sets the authors.
        /// </summary>
        /// <value>The authors.</value>
        [DataMember]
        public string Authors
        {
            get => this._authors;

            set
            {
                this._authors = value;
                NotifyOfPropertyChange(() => this.Authors);
            }
        }

        /// <summary>
        /// Gets the available upload location.
        /// </summary>
        /// <value>The available upload location.</value>
        public List<string> AvailableUploadLocation =>
            this._availableUploadLocation ?? (this._availableUploadLocation = new List<string>()
                    {
                        "Amazon S3",
                        "File System",
                    });

        /// <summary>
        /// Gets the current file path.
        /// </summary>
        /// <value>The current file path.</value>
        [DataMember]
        public string CurrentFilePath { get; internal set; }

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        /// <value>The description.</value>
        [DataMember]
        public string Description
        {
            get => this._description;

            set
            {
                this._description = value;
                NotifyOfPropertyChange(() => this.Description);
            }
        }

        /// <summary>
        /// Gets the edit connection command.
        /// </summary>
        /// <value>The edit connection command.</value>
        public ICommand EditConnectionCmd => this._editConnectionCmd ??
       (this._editConnectionCmd = new DelegateCommand(this.EditCurrentConnection));

        /// <summary>
        /// Gets or sets the icon filepath.
        /// </summary>
        /// <value>The icon filepath.</value>
        [DataMember]
        public string IconFilepath
        {
            get => this._iconFilepath;

            set
            {
                this._iconFilepath = value;
                NotifyOfPropertyChange(() => this.IconFilepath);
                NotifyOfPropertyChange(() => this.IconSource);
            }
        }

        /// <summary>
        /// Gets the icon source.
        /// </summary>
        /// <value>The icon source.</value>
        public ImageSource IconSource
        {
            get
            {
                try
                {
                    return GetImageFromFilepath(this.IconFilepath);
                }
                catch
                {
                    //TODO -  default icon
                    return null;
                }
            }
        }

        /// <summary>
        /// Gets or sets the main executable path.
        /// </summary>
        /// <value>The main executable path.</value>
        [DataMember]
        public string MainExePath
        {
            get => this._mainExePath;

            set
            {
                this._mainExePath = value;
                NotifyOfPropertyChange(() => this.MainExePath);
            }
        }

        /// <summary>
        /// Gets or sets the nupkg output path.
        /// </summary>
        /// <value>The nupkg output path.</value>
        [DataMember]
        public string NupkgOutputPath
        {
            get => this._nupkgOutputPath;

            set
            {
                this._nupkgOutputPath = value;
                NotifyOfPropertyChange(() => this.NupkgOutputPath);
            }
        }

        /// <summary>
        /// Gets or sets the package files.
        /// </summary>
        /// <value>The package files.</value>
        [DataMember]
        public ObservableCollection<ItemLink> PackageFiles
        {
            get => this._packageFiles;

            set
            {
                this._packageFiles = value;
                NotifyOfPropertyChange(() => this.PackageFiles);
            }
        }

        /// <summary>
        /// Gets the refresh version number.
        /// </summary>
        /// <value>The refresh version number.</value>
        public ICommand RefreshVersionNumber => this._refreshVersionNumber ??
               (this._refreshVersionNumber = new DelegateCommand(this.RefreshPackageVersion));

        /// <summary>
        /// Gets the remove all items command.
        /// </summary>
        /// <value>The remove all items command.</value>
        public ICommand RemoveAllItemsCmd => this._removeAllItemsCmd ??
               (this._removeAllItemsCmd = new DelegateCommand(this.RemoveAllItems));

        /// <summary>
        /// Gets the remove item command.
        /// </summary>
        /// <value>The remove item command.</value>
        public ICommand RemoveItemCmd => this._removeItemCmd ??
       (this._removeItemCmd = new DelegateCommand(this.RemoveItem));

        /// <summary>
        /// Gets or sets the selected connection.
        /// </summary>
        /// <value>The selected connection.</value>
        [DataMember]
        public WebConnectionBase SelectedConnection
        {
            get => this._selectedConnection;

            set
            {
                this._selectedConnection = value;
                NotifyOfPropertyChange(() => this.SelectedConnection);
            }
        }

        /// <summary>
        /// Gets or sets the selected connection string.
        /// </summary>
        /// <value>The selected connection string.</value>
        [DataMember]
        public string SelectedConnectionString
        {
            get => this._selectedConnectionString;

            set
            {
                if (this._selectedConnectionString == value)
                {
                    return;
                }

                UpdateSelectedConnection(value);
                this._selectedConnectionString = value;
                NotifyOfPropertyChange(() => this.SelectedConnectionString);
            }
        }

        /// <summary>
        /// Gets the selected item.
        /// </summary>
        /// <value>The selected item.</value>
        public ItemLink SelectedItem
        {
            get => this.selectedItem;

            set
            {
                this.selectedItem = value;
                NotifyOfPropertyChange(() => this.SelectedItem);
            }
        }

        /// <summary>
        /// Gets or sets the selected link.
        /// </summary>
        /// <value>The selected link.</value>
        public IList<ItemLink> SelectedLink { get; set; } = new List<ItemLink>();

        /// <summary>
        /// Gets or sets the selected upload item.
        /// </summary>
        /// <value>The selected upload item.</value>
        public SingleFileUpload SelectedUploadItem
        {
            get => this._selectedUploadItem;

            set
            {
                this._selectedUploadItem = value;
                NotifyOfPropertyChange(() => this.SelectedUploadItem);
            }
        }

        /// <summary>
        /// Gets the select icon command.
        /// </summary>
        /// <value>The select icon command.</value>
        public ICommand SelectIconCmd => this._selectIconCmd ??
       (this._selectIconCmd = new DelegateCommand(this.SelectIcon));

        /// <summary>
        /// Gets or sets a value indicating whether [set version manually].
        /// </summary>
        /// <value><c>true</c> if [set version manually]; otherwise, <c>false</c>.</value>
        [DataMember]
        public bool SetVersionManually
        {
            get => this._setVersionManually;

            set
            {
                this._setVersionManually = value;
                NotifyOfPropertyChange(() => this.SetVersionManually);
            }
        }

        /// <summary>
        /// Gets or sets the squirrel output path.
        /// </summary>
        /// <value>The squirrel output path.</value>
        [DataMember]
        public string SquirrelOutputPath
        {
            get => this._squirrelOutputPath;

            set
            {
                this._squirrelOutputPath = value;
                NotifyOfPropertyChange(() => this.SquirrelOutputPath);
            }
        }

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        /// <value>The title.</value>
        [DataMember]
        public string Title
        {
            get => this._title;

            set
            {
                this._title = value;
                NotifyOfPropertyChange(() => this.Title);
            }
        }

        /// <summary>
        /// Gets or sets the upload queue.
        /// </summary>
        /// <value>The upload queue.</value>
        public ObservableCollection<SingleFileUpload> UploadQueue
        {
            get => this._uploadQueue;

            set
            {
                this._uploadQueue = value;
                NotifyOfPropertyChange(() => this.UploadQueue);
            }
        }

        /// <summary>
        /// Gets or sets the version.
        /// </summary>
        /// <value>The version.</value>
        [DataMember]
        public string Version
        {
            get => this._version;

            set
            {
                string[] test = value.Split('.');
                if (test.Length <= 3)
                {
                    this._version = value;
                    NotifyOfPropertyChange(() => this.Version);
                }
                else
                {
                    if (MessageBox.Show("Please use a Semantic Versioning 2.0.0 standard for the version number i.e. Major.Minor.Build http://semver.org/", "Invalid Version", MessageBoxButton.OK) == MessageBoxResult.OK)
                    {
                        RefreshPackageVersion(null);
                    }
                }
            }
        }

        /// <summary>
        /// Adds the directory.
        /// </summary>
        /// <param name="parm">The parm.</param>
        public void AddDirectory(object parm)
        {
            if (this.SelectedLink.Count != 1)
            {
                return;
            }

            ItemLink selectedLink = this.SelectedLink[0];
            if (selectedLink != null)
            {
                var validFolderName = GetValidName(this.newFolderName, selectedLink.Children);

                selectedLink.Children.Add(new ItemLink { OutputFilename = validFolderName, IsDirectory = true });
            }
            else
            {
                var validFolderName = GetValidName(this.newFolderName, this.PackageFiles);

                this.PackageFiles.Add(new ItemLink { OutputFilename = validFolderName, IsDirectory = true });
            }
        }

        /// <summary>
        /// Edits the current connection.
        /// </summary>
        /// <param name="parm">The parm.</param>
        public void EditCurrentConnection(object parm)
        {
            if (this.SelectedConnection == null)
            {
                return;
            }

            var vw = parm as WebConnectionEdit;
            vw.DataContext = this.SelectedConnection;
            vw.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Read the main exe version and set it as package version
        /// </summary>
        /// <param name="parm">The parm.</param>
        public void RefreshPackageVersion(object parm)
        {
            if (!File.Exists(this.MainExePath))
            {
                return;
            }

            if (this.SetVersionManually)
            {
                return;
            }

            var versInfo = FileVersionInfo.GetVersionInfo(this.MainExePath);

            this.Version = $"{versInfo.ProductMajorPart}.{versInfo.ProductMinorPart}.{versInfo.ProductBuildPart}";
        }

        /// <summary>
        /// Removes all items.
        /// </summary>
        /// <param name="parm">The parm.</param>
        public void RemoveAllItems(object parm)
        {
            if (this.SelectedLink?.Count == 0)
            {
                return;
            }

            RemoveAllFromTreeview(this.SelectedLink[0]);
        }

        /// <summary>
        /// Removes the item.
        /// </summary>
        /// <param name="parm">The parm.</param>
        public void RemoveItem(object parm)
        {
            if (this.SelectedLink?.Count == 0)
            {
                return;
            }

            foreach (ItemLink link in this.SelectedLink)
            {
                RemoveFromTreeview(link);
            }
        }

        /// <summary>
        /// Selects the icon.
        /// </summary>
        /// <param name="parm">The parm.</param>
        public void SelectIcon(object parm)
        {
            var ofd = new System.Windows.Forms.OpenFileDialog
            {
                AddExtension = true,
                DefaultExt = ".ico",
                Filter = "ICON | *.ico"
            };

            System.Windows.Forms.DialogResult o = ofd.ShowDialog();

            if (o != System.Windows.Forms.DialogResult.OK || !File.Exists(ofd.FileName))
            {
                return;
            }

            this.IconFilepath = ofd.FileName;
        }

        /// <summary>
        /// Selects the nupkg directory.
        /// </summary>
        public void SelectNupkgDirectory()
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();

            if (Directory.Exists(this.NupkgOutputPath))
            {
                dialog.SelectedPath = this.NupkgOutputPath;
            }

            System.Windows.Forms.DialogResult result = dialog.ShowDialog();

            if (result != System.Windows.Forms.DialogResult.OK)
            {
                return;
            }

            this.NupkgOutputPath = dialog.SelectedPath;
        }

        /// <summary>
        /// Selects the output directory.
        /// </summary>
        public void SelectOutputDirectory()
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();

            if (Directory.Exists(this.SquirrelOutputPath))
            {
                dialog.SelectedPath = this.SquirrelOutputPath;
            }

            System.Windows.Forms.DialogResult result = dialog.ShowDialog();

            if (result != System.Windows.Forms.DialogResult.OK)
            {
                return;
            }

            this.SquirrelOutputPath = dialog.SelectedPath;
        }

        /// <summary>
        /// Sets the selected item.
        /// </summary>
        /// <param name="item">The item.</param>
        public void SetSelectedItem(IList<ItemLink> item)
        {
            this.SelectedLink = item;
            this.SelectedItem = this.SelectedLink.FirstOrDefault() ?? new ItemLink();
        }

        /// <summary>
        /// Validates this instance.
        /// </summary>
        /// <returns></returns>
        public override ValidationResult Validate()
        {
            ValidationResult commonValid = new Validator().Validate(this);
            if (!commonValid.IsValid)
            {
                return commonValid;
            }

            return base.Validate();
        }

        internal static BitmapImage GetImageFromFilepath(string path)
        {
            if (!File.Exists(path))
            {
                return null;
            }

            if (File.Exists(path))
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.None;
                bitmap.UriCachePolicy = new RequestCachePolicy(RequestCacheLevel.BypassCache);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                bitmap.UriSource = new Uri(path, UriKind.RelativeOrAbsolute);
                bitmap.EndInit();

                return bitmap;
            }

            return new BitmapImage();
        }

        internal static ObservableCollection<ItemLink> OrderFileList(ObservableCollection<ItemLink> packageFiles)
        {
            foreach (ItemLink node in packageFiles)
            {
                node.Children = OrderFileList(node.Children);
            }

            return new ObservableCollection<ItemLink>(packageFiles.OrderByDescending(n => n.IsDirectory).ThenBy(n => n.Filename));
        }

        internal void AddFile(string filePath, ItemLink targetItem)
        {
            var isDir = false;
            FileAttributes fa = File.GetAttributes(filePath);
            if ((fa & FileAttributes.Directory) != 0)
            {
                isDir = true;
            }

            RemoveItemBySourceFilepath(filePath);

            var node = new ItemLink() { SourceFilepath = filePath, IsDirectory = isDir };

            ItemLink parent = targetItem;
            if (targetItem == null)
            {
                //Porto su root
                this._packageFiles.Add(node);
            }
            else
            {
                if (!targetItem.IsDirectory)
                {
                    parent = targetItem.GetParent(this.PackageFiles);
                }

                if (parent != null)
                {
                    //Insert into treeview root
                    parent.Children.Add(node);
                }
                else
                {
                    //Insert into treeview root
                    this._packageFiles.Add(node);
                }
            }

            if (isDir)
            {
                var dir = new DirectoryInfo(filePath);

                FileInfo[] files = dir.GetFiles("*.*", SearchOption.TopDirectoryOnly);
                DirectoryInfo[] subDirectory = dir.GetDirectories("*.*", SearchOption.TopDirectoryOnly);

                foreach (FileInfo f in files)
                {
                    AddFile(f.FullName, node);
                }

                foreach (DirectoryInfo f in subDirectory)
                {
                    AddFile(f.FullName, node);
                }
            }
            else
            {
                // I keep the exe filepath, i'll read the version from this file.
                var ext = Path.GetExtension(filePath).ToLower();

                if (ext == ".exe")
                {
                    ItemLink nodeParent = node.GetParent(this.PackageFiles);
                    if (nodeParent == null)
                    {
                        var fileName = Path.GetFileNameWithoutExtension(filePath);
                        this.AppId = fileName;
                        this.Title = fileName;
                        this.MainExePath = filePath;
                        var versInfo = FileVersionInfo.GetVersionInfo(this.MainExePath);
                        this.Description = versInfo.Comments;
                        this.Authors = versInfo.CompanyName;

                        RefreshPackageVersion(null);
                    }
                }
            }
        }

        /// <summary>
        /// 29/01/2015
        /// 1) Create update files list
        /// 2) Create queue upload list. Iterating file list foreach connection ( i can have multiple
        /// cloud storage )
        /// 3) Start async upload.
        /// </summary>
        /// <param name="mode">The mode.</param>
        /// <exception cref="Exception"></exception>
        internal void BeginUpdatedFiles(int mode)
        {
            var releasesPath = this.SquirrelOutputPath;

            if (!Directory.Exists(releasesPath))
            {
                throw new Exception("Releases directory " + releasesPath + "not found !");
            }

            if (this.SelectedConnection == null)
            {
                throw new Exception("No selected upload location !");
            }

            var fileToUpdate = new List<string>()
            {
                "RELEASES",
                $"{AppId}-{Version}-delta.nupkg",
            };

            if (mode == 0)
            {
                fileToUpdate.Add($"{AppId}-{Version}-full.nupkg");
                fileToUpdate.Add("Setup.exe");
            }

            var updatedFiles = new List<FileInfo>();

            foreach (var fp in fileToUpdate)
            {
                var ffp = releasesPath + fp;
                if (!File.Exists(ffp))
                {
                    continue;
                }

                updatedFiles.Add(new FileInfo(ffp));
            }

            this.UploadQueue = this.UploadQueue ?? new ObservableCollection<SingleFileUpload>();

            this.UploadQueue.Clear();

            foreach (WebConnectionBase connection in new List<WebConnectionBase>() { SelectedConnection })
            {
                foreach (FileInfo file in updatedFiles)
                {
                    this.UploadQueue.Add(new SingleFileUpload()
                    {
                        Filename = Path.GetFileName(file.FullName),
                        ConnectionName = connection.ConnectionName,
                        FileSize = BytesToString(file.Length),
                        Connection = connection,
                        FullPath = file.FullName,
                    });
                }
            }

            if (!CheckInternetConnection.IsConnectedToInternet())
            {
                throw new Exception("Internet Connection not available");
            }

            ProcessNextUploadFile();
        }

        private static string BytesToString(long byteCount)
        {
            string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" }; //Longs run out around EB
            if (byteCount == 0)
            {
                return "0" + suf[0];
            }

            var bytes = Math.Abs(byteCount);
            var place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            var num = Math.Round(bytes / Math.Pow(1024, place), 1);
            return (Math.Sign(byteCount) * num).ToString() + suf[place];
        }

        private static string GetValidName(string newFolderName, ObservableCollection<ItemLink> children)
        {
            var folderName = newFolderName;

            ItemLink ex = children.FirstOrDefault(i => i.Filename == folderName);
            var index = 0;
            while (ex != null)
            {
                index++;
                folderName = newFolderName + " (" + index + ")";

                ex = children.FirstOrDefault(i => i.Filename == folderName);
            }

            return folderName;
        }

        private static void SearchNodeByFilepath(string filepath, ObservableCollection<ItemLink> root, List<ItemLink> rslt)
        {
            foreach (ItemLink node in root)
            {
#pragma warning disable RCS1155 // Use StringComparison when comparing strings.
                if (node.SourceFilepath != null && filepath.ToLower() == node.SourceFilepath.ToLower())
#pragma warning restore RCS1155 // Use StringComparison when comparing strings.
                {
                    rslt.Add(node);
                }

                SearchNodeByFilepath(filepath, node.Children, rslt);
            }
        }

        private void Current_OnUploadCompleted(object sender, UploadCompleteEventArgs e)
        {
            SingleFileUpload i = e.FileUploaded;

            i.OnUploadCompleted -= this.Current_OnUploadCompleted;

            Trace.WriteLine("Upload Complete " + i.Filename);

            //if (i != null && UploadQueue.Contains(i))
            //    UploadQueue.Remove(i);

            ProcessNextUploadFile();
        }

        private void MoveItem(ItemLink draggedItem, ItemLink targetItem)
        {
            // Remove from current location
            RemoveFromTreeview(draggedItem);

            // Add to target position
            ItemLink parent = targetItem;
            if (targetItem == null)
            {
                //Porto su root
                this._packageFiles.Add(draggedItem);
            }
            else
            {
                if (!targetItem.IsDirectory)
                {
                    parent = targetItem.GetParent(this.PackageFiles);
                }

                if (parent != null)
                {
                    //Insert into treeview root
                    parent.Children.Add(draggedItem);
                }
                else
                {
                    //Insert into treeview root
                    this._packageFiles.Add(draggedItem);
                }
            }

            NotifyOfPropertyChange(() => this.PackageFiles);
        }

        private void ProcessNextUploadFile()
        {
            try
            {
                SingleFileUpload current = this.UploadQueue.FirstOrDefault(u => u.UploadStatus == FileUploadStatus.Queued);

                if (current == null)
                {
                    return;
                }

                current.OnUploadCompleted += this.Current_OnUploadCompleted;

                current.StartUpload();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void RemoveAllFromTreeview(ItemLink item)
        {
            ItemLink parent = item.GetParent(this.PackageFiles);

            // Element is in the treeview root.
            if (parent == null)
            {
                this._packageFiles.Clear();
                NotifyOfPropertyChange(() => this.PackageFiles);
            }
            else
            {
                //Remove it from children list
                parent.Children.Clear();
            }
            this.MainExePath = string.Empty;
            RefreshPackageVersion(null);
        }

        private void RemoveFromTreeview(ItemLink item)
        {
            ItemLink parent = item.GetParent(this.PackageFiles);

#pragma warning disable RCS1155 // Use StringComparison when comparing strings.
            if (this.MainExePath != null && item.SourceFilepath != null && this.MainExePath.ToLower() == item.SourceFilepath.ToLower())
#pragma warning restore RCS1155 // Use StringComparison when comparing strings.
            {
                this.MainExePath = string.Empty;
                RefreshPackageVersion(null);
            }

            // Element is in the treeview root.
            if (parent == null)
            {
                if (this._packageFiles.Contains(item))
                {
                    this._packageFiles.Remove(item);
                }
            }
            else
            {
                //Remove it from children list
                parent.Children.Remove(item);
            }
        }

        private void RemoveItemBySourceFilepath(string filepath)
        {
            var list = new List<ItemLink>();

            SearchNodeByFilepath(filepath, this.PackageFiles, list);

            foreach (ItemLink node in list)
            {
                RemoveFromTreeview(node);
            }
        }

        /// <summary>
        /// I keep in memory created WebConnectionBase, so if the user switch accidentally the
        /// connection string , he don't lose inserted parameter
        /// </summary>
        /// <param name="connectionType">Type of the connection.</param>
        private void UpdateSelectedConnection(string connectionType)
        {
            if (string.IsNullOrWhiteSpace(connectionType))
            {
                return;
            }

            this.CachedConnection = this.CachedConnection ?? new List<WebConnectionBase>();

            WebConnectionBase con = null;
            switch (connectionType)
            {
                case "Amazon S3":
                    con = this.CachedConnection.Find(c => c is AmazonS3Connection) ?? new AmazonS3Connection();
                    break;

                case "File System":
                    con = this.CachedConnection.Find(c => c is FileSystemConnection) ?? new FileSystemConnection();
                    break;
            }

            if (con != null && !this.CachedConnection.Contains(con))
            {
                this.CachedConnection.Add(con);
            }

            this.SelectedConnection = con;
        }

        private class Validator : AbstractValidator<AutoSquirrelModel>
        {
            public Validator()
            {
                RuleFor(c => c.AppId).NotEmpty();
                RuleFor(c => c.Title).NotEmpty();
                RuleFor(c => c.Description).NotEmpty();
                RuleFor(c => c.Version).NotEmpty();
                RuleFor(c => c.PackageFiles).NotEmpty();
                RuleFor(c => c.Authors).NotEmpty();
                RuleFor(c => c.SelectedConnectionString).NotEmpty();
            }
        }
    }
}