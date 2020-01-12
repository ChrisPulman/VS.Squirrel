using System.Drawing;
using System.Runtime.InteropServices;

namespace AutoSquirrel
{
    public static partial class IconHelper
    {
        /// <summary>
        /// Provides static methods to read system icons for both folders and files.
        /// </summary>
        /// <example>
        /// <code>
        /// IconReader.GetFileIcon("c:\\general.xls");
        /// </code>
        /// </example>
        private static class IconReader
        {
            /// <summary>
            /// Options to specify the size of icons to return.
            /// </summary>
            public enum IconSize
            {
                /// <summary>
                /// Specify large icon - 32 pixels by 32 pixels.
                /// </summary>
                Large = 0,

                /// <summary>
                /// Specify small icon - 16 pixels by 16 pixels.
                /// </summary>
                Small = 1
            }

            /// <summary>
            /// Returns an icon for a given file - indicated by the name parameter.
            /// </summary>
            /// <param name="name">Pathname for file.</param>
            /// <param name="size">Large or small</param>
            /// <param name="linkOverlay">Whether to include the link icon</param>
            /// <returns>System.Drawing.Icon</returns>
            public static Icon GetFileIcon(string name, IconSize size, bool linkOverlay)
            {
                var shfi = new Shell32.Shfileinfo();
                var flags = Shell32.ShgfiIcon | Shell32.ShgfiUsefileattributes;
                if (linkOverlay)
                {
                    flags += Shell32.ShgfiLinkoverlay;
                }
                /* Check the size specified for return. */
                if (IconSize.Small == size)
                {
                    flags += Shell32.ShgfiSmallicon;
                }
                else
                {
                    flags += Shell32.ShgfiLargeicon;
                }

                Shell32.SHGetFileInfo(name,
                    Shell32.FileAttributeNormal,
                    ref shfi,
                    (uint)Marshal.SizeOf(shfi),
                    flags);

                // Copy (clone) the returned icon to a new object, thus allowing us to clean-up properly
                var icon = (Icon)Icon.FromHandle(shfi.hIcon).Clone();
                User32.DestroyIcon(shfi.hIcon);     // Cleanup
                return icon;
            }
        }
    }
}
