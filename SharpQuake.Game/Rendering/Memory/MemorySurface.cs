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

namespace SharpQuake.Game.Rendering.Memory
{
    using Framework;
    using Framework.Definitions.Bsp;
    using Framework.Mathematics;
    using Framework.Rendering;

    public class MemorySurface
    {
        public int visframe;		// should be drawn when node is crossed

        public Plane plane;
        public int flags;

        public int firstedge;	// look up in model->surfedges[], negative numbers
        public int numedges;	// are backwards edges

        public short[] texturemins; //[2];
        public short[] extents; //[2];

        public int light_s, light_t;	// gl lightmap coordinates

        public GLPoly polys;			// multiple if warped
        public MemorySurface texturechain;

        public MemoryTextureInfo texinfo;

        // lighting info
        public int dlightframe;
        public int dlightbits;

        public int lightmaptexturenum;
        public byte[] styles; //[MAXLIGHTMAPS];
        public int[] cached_light; //[MAXLIGHTMAPS];	// values currently used in lightmap
        public bool cached_dlight;				// true if dynamic light in cache
        /// <summary>
        /// Former "samples" field. Use in pair with sampleofs field!!!
        /// </summary>
        public byte[] sample_base;		// [numstyles*surfsize]
        public int sampleofs; // added by Uze. In original Quake samples = loadmodel->lightdata + offset;
        // now samples = loadmodel->lightdata;

        public MemorySurface( )
        {
            this.texturemins = new short[2];
            this.extents = new short[2];
            this.styles = new byte[BspDef.MAXLIGHTMAPS];
            this.cached_light = new int[BspDef.MAXLIGHTMAPS];
            // samples is allocated when needed
        }
    } //msurface_t;
}
