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

namespace SharpQuake.Framework.Definitions
{
    public class DrawDef
    {
        public const int MAX_GLTEXTURES = 1024;
        public const int MAX_CACHED_PICS = 128;

        //
        //  scrap allocation
        //
        //  Allocate all the little status bar obejcts into a single texture
        //  to crutch up stupid hardware / drivers
        //
        public const int MAX_SCRAPS = 2;
        public const int BLOCK_WIDTH = 256;
        public const int BLOCK_HEIGHT = 256;
    }
}
