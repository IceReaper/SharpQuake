/// <copyright>
///
/// SharpQuakeEvolved changes by optimus-code, 2019
/// 
/// Based on SharpQuake (Quake Rewritten in C# by Yury Kiselev, 2010.)
///
/// Copyright (C) 1996-1997 Id Software, Inc.
///
/// This program is free software; you can redistribute it and/or
/// modify it under the terms of the GNU General Public License
/// as published by the Free Software Foundation; either version 2
/// of the License, or (at your option) any later version.
///
/// This program is distributed in the hope that it will be useful,
/// but WITHOUT ANY WARRANTY; without even the implied warranty of
/// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
///
/// See the GNU General Public License for more details.
///
/// You should have received a copy of the GNU General Public License
/// along with this program; if not, write to the Free Software
/// Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
/// </copyright>

namespace SharpQuake.Framework.IO.BSP
{
    using Definitions.Bsp;
    using System.Runtime.InteropServices;

    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public struct BspFace
    {
        public short planenum;
        public short side;

        public int firstedge;		// we must support > 64k edges
        public short numedges;
        public short texinfo;

        // lighting info
        [MarshalAs( UnmanagedType.ByValArray, SizeConst = BspDef.MAXLIGHTMAPS )]
        public byte[] styles; //[MAXLIGHTMAPS];

        public int lightofs;		// start of [numstyles*surfsize] samples

        public static int SizeInBytes = Marshal.SizeOf( typeof( BspFace ) );
    } // dface_t
}
