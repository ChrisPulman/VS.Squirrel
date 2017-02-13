using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AutoSquirrel
{
    /// <summary>
    /// Icon Helper
    /// </summary>
    public static partial class IconHelper
    {
        /// <summary>
        /// The file attribute directory
        /// </summary>
        public const uint FILE_ATTRIBUTE_DIRECTORY = 0x00000010;

        /// <summary>
        /// The shgfi icon
        /// </summary>
        public const uint SHGFI_ICON = 0x000000100;

        /// <summary>
        /// The shgfi largeicon
        /// </summary>
        public const uint SHGFI_LARGEICON = 0x000000000;

        /// <summary>
        /// The shgfi openicon
        /// </summary>
        public const uint SHGFI_OPENICON = 0x000000002;

        /// <summary>
        /// The shgfi smallicon
        /// </summary>
        public const uint SHGFI_SMALLICON = 0x000000001;

        /// <summary>
        /// The shgfi usefileattributes
        /// </summary>
        public const uint SHGFI_USEFILEATTRIBUTES = 0x000000010;

        private static readonly Dictionary<string, ImageSource> _largeIconCache = new Dictionary<string, ImageSource>();

        private static readonly Dictionary<string, ImageSource> _smallIconCache = new Dictionary<string, ImageSource>();

        /// <summary>
        /// Get an icon for a given filename
        /// </summary>
        /// <param name="fileName">any filename</param>
        /// <param name="large">16x16 or 32x32 icon</param>
        /// <returns>null if path is null, otherwise - an icon</returns>
        public static ImageSource FindIconForFilename(string fileName, bool large)
        {
            var extension = Path.GetExtension(fileName);
            if (extension == null)
            {
                return null;
            }

            Dictionary<string, ImageSource> cache = large ? _largeIconCache : _smallIconCache;
            if (cache.TryGetValue(extension, out var icon))
            {
                return icon;
            }

            icon = IconReader.GetFileIcon(fileName, large ? IconReader.IconSize.Large : IconReader.IconSize.Small, false).ToImageSource();
            cache.Add(extension, icon);
            return icon;
        }

        /// <summary>
        /// Gets the folder icon.
        /// </summary>
        /// <param name="size">The size.</param>
        /// <param name="folderType">Type of the folder.</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static Icon GetFolderIcon(IconSize size, FolderType folderType)
        {
            // Need to add size check, although errors generated at present!
            var flags = SHGFI_ICON | SHGFI_USEFILEATTRIBUTES;

            if (FolderType.Open == folderType)
            {
                flags += SHGFI_OPENICON;
            }
            if (IconSize.Small == size)
            {
                flags += SHGFI_SMALLICON;
            }
            else
            {
                flags += SHGFI_LARGEICON;
            }

            // Get the folder icon
            var shfi = new SHFILEINFO();

            IntPtr res = SHGetFileInfo(Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                FILE_ATTRIBUTE_DIRECTORY,
                out shfi,
                (uint)Marshal.SizeOf(shfi),
                flags);

            if (res == IntPtr.Zero)
            {
                throw Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());
            }

            // Load the icon from an HICON handle
            Icon.FromHandle(shfi.hIcon);

            // Now clone the icon, so that it can be successfully stored in an ImageList
            var icon = (Icon)Icon.FromHandle(shfi.hIcon).Clone();

            DestroyIcon(shfi.hIcon);        // Cleanup

            return icon;
        }

        /// <summary>
        /// Shes the get file information.
        /// </summary>
        /// <param name="pszPath">The PSZ path.</param>
        /// <param name="dwFileAttributes">The dw file attributes.</param>
        /// <param name="psfi">The psfi.</param>
        /// <param name="cbFileInfo">The cb file information.</param>
        /// <param name="uFlags">The u flags.</param>
        /// <returns></returns>
        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, out SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

        /// <summary>
        /// To the image source.
        /// </summary>
        /// <param name="icon">The icon.</param>
        /// <returns></returns>
        public static ImageSource ToImageSource(this Icon icon)
        {
            if (icon == null)
            {
                return null;
            }

            ImageSource imageSource = Imaging.CreateBitmapSourceFromHIcon(
                icon.Handle,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());

            return imageSource;
        }

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DestroyIcon(IntPtr hIcon);
    }
}