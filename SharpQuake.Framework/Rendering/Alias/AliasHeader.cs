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

namespace SharpQuake.Framework.Rendering.Alias
{
    using Definitions.Bsp;
    using System.Numerics;
    using System.Runtime.InteropServices;

    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public class aliashdr_t
    {
        public int ident;
        public int version;
        public Vector3 scale;
        public Vector3 scale_origin;
        public float boundingradius;
        public Vector3 eyeposition;
        public int numskins;
        public int skinwidth;
        public int skinheight;
        public int numverts;
        public int numtris;
        public int numframes;
        public SyncType synctype;
        public int flags;
        public float size;

        public int numposes;
        public int poseverts;
        /// <summary>
        /// Changed from int offset from this header to posedata to
        /// trivertx_t array
        /// </summary>
        public trivertx_t[] posedata;	// numposes*poseverts trivert_t
        /// <summary>
        /// Changed from int offset from this header to commands data
        /// to commands array
        /// </summary>
        public int[] commands;	// gl command list with embedded s/t
        [MarshalAs( UnmanagedType.ByValArray, SizeConst = ModelDef.MAX_SKINS * 4 )]
        public int[,] gl_texturenum; // int gl_texturenum[MAX_SKINS][4];
        /// <summary>
        /// Changed from integers (offsets from this header start) to objects to hold pointers to arrays of byte
        /// </summary>
        [MarshalAs( UnmanagedType.ByValArray, SizeConst = ModelDef.MAX_SKINS )]
        public object[] texels; // int texels[MAX_SKINS];	// only for player skins
        public maliasframedesc_t[] frames; // maliasframedesc_t	frames[1];	// variable sized

        public static int SizeInBytes = Marshal.SizeOf( typeof( aliashdr_t ) );

        public aliashdr_t( )
        {
            this.gl_texturenum = new int[ModelDef.MAX_SKINS, 4];//[];
            this.texels = new object[ModelDef.MAX_SKINS];
        }
    } // aliashdr_t;
}
