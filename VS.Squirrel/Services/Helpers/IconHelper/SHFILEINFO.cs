using System;
using System.Runtime.InteropServices;

namespace AutoSquirrel
{
    public static partial class IconHelper
    {
        /// <summary>
        /// SH FILE INFO
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct SHFILEINFO
        {
            /// <summary>
            /// The h icon
            /// </summary>
            public IntPtr hIcon;

            /// <summary>
            /// The i icon
            /// </summary>
            public int iIcon;

            /// <summary>
            /// The dw attributes
            /// </summary>
            public uint dwAttributes;

            /// <summary>
            /// The sz display name
            /// </summary>
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;

            /// <summary>
            /// The sz type name
            /// </summary>
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }
    }
}
