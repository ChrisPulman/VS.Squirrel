namespace AutoSquirrel
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Media;

    /// <summary>
    /// Interaction logic for SquirrelPackagerControl.
    /// </summary>
    public partial class SquirrelPackagerControl : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SquirrelPackagerControl"/> class.
        /// </summary>
        public SquirrelPackagerControl()
        {
            DataContext = new ShellViewModel();
            InitializeComponent();

            PackageTreeview.PreviewMouseRightButtonDown += OnPreviewMouseRightButtonDown;
            PackageTreeview.SelectedItemChanged += PackageTreeview_SelectedItemChanged;
            PublishPackageComplete.Click += PublishPackageComplete_Click;
            PublishPackageOnlyUpdate.Click += PublishPackageOnlyUpdate_Click;
            WebConnection.IsVisibleChanged += WebConnection_IsVisibleChanged;
        }

        private static TreeView VisualUpwardSearch(DependencyObject source)
        {
            while (source != null && !(source is TreeView)) {
                source = VisualTreeHelper.GetParent(source);
            }

            return source as TreeView;
        }

        private void OnPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var treeViewItem = VisualUpwardSearch(e.OriginalSource as DependencyObject);

            if (treeViewItem == null) { return; }

            treeViewItem.Focus();
        }

        private void PackageTreeview_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            IList<ItemLink> items = new List<ItemLink>
            {
                (sender as TreeView).SelectedItem as ItemLink
            };
            ((ShellViewModel)DataContext).Model.SetSelectedItem(items);
        }

        private void PublishPackageComplete_Click(object sender, RoutedEventArgs e) => ((ShellViewModel)DataContext).PublishPackageComplete();

        private void PublishPackageOnlyUpdate_Click(object sender, RoutedEventArgs e) => ((ShellViewModel)DataContext).PublishPackageOnlyUpdate();

        private void WebConnection_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!(bool)e.NewValue) {
                ((ShellViewModel)DataContext).Save();
            }
        }
    }
}
