﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake.Framework.IO.Sprite
{
    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public struct dspriteframe_t
    {
        [MarshalAs( UnmanagedType.ByValArray, SizeConst = 2 )]
        public Int32[] origin; // [2];
        public Int32 width;
        public Int32 height;

        public static Int32 SizeInBytes = Marshal.SizeOf( typeof( dspriteframe_t ) );
    } // dspriteframe_t;
}
