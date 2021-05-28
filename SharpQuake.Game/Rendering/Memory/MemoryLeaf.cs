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
    using World;

    public class MemoryLeaf : MemoryNodeBase
    {
        // leaf specific
        /// <summary>
        /// loadmodel->visdata
        /// Use in pair with visofs!
        /// </summary>
        public byte[] compressed_vis; // byte*
        public int visofs; // added by Uze
        public EFrag efrags;

        /// <summary>
        /// loadmodel->marksurfaces
        /// </summary>
        public MemorySurface[] marksurfaces;
        public int firstmarksurface; // msurface_t	**firstmarksurface;
        public int nummarksurfaces;
        //public int key;			// BSP sequence number for leaf's contents
        public byte[] ambient_sound_level; // [NUM_AMBIENTS];

        public MemoryLeaf( )
        {
            this.ambient_sound_level = new byte[AmbientDef.NUM_AMBIENTS];
        }
    } //mleaf_t;

}
