//------------------------------------------------------------------------------
// <copyright file="SquirrelPackagerControl.xaml.cs" company="AIC Solutions Ltd">
//     Copyright (c) AIC Solutions Ltd.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace AutoSquirrel
{
    using System;
    using System.Collections.Generic;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Media;

    /// <summary>
    /// Interaction logic for SquirrelPackagerControl.
    /// </summary>
    public partial class SquirrelPackagerControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SquirrelPackagerControl"/> class.
        /// </summary>
        public SquirrelPackagerControl()
        {
            this.DataContext = new ShellViewModel();
            this.InitializeComponent();

            this.PackageTreeview.PreviewMouseRightButtonDown += this.OnPreviewMouseRightButtonDown;
            this.PackageTreeview.SelectedItemChanged += this.PackageTreeview_SelectedItemChanged;
            this.PublishPackageComplete.Click += this.PublishPackageComplete_Click;
            this.PublishPackageOnlyUpdate.Click += this.PublishPackageOnlyUpdate_Click;
        }

        private static TreeView VisualUpwardSearch(DependencyObject source)
        {
            while (source != null && !(source is TreeView))
            {
                source = VisualTreeHelper.GetParent(source);
            }

            return source as TreeView;
        }

        private void OnPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            TreeView treeViewItem = VisualUpwardSearch(e.OriginalSource as DependencyObject);

            if (treeViewItem == null) { return; }

            treeViewItem.Focus();
        }

        private void PackageTreeview_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            IList<ItemLink> items = new List<ItemLink>
            {
                (sender as TreeView).SelectedItem as ItemLink
            };
            ((ShellViewModel)this.DataContext).Model.SetSelectedItem(items);
        }

        private void PublishPackageComplete_Click(object sender, RoutedEventArgs e) => ((ShellViewModel)this.DataContext).PublishPackageComplete();

        private void PublishPackageOnlyUpdate_Click(object sender, RoutedEventArgs e) => ((ShellViewModel)this.DataContext).PublishPackageOnlyUpdate();
    }
}