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

namespace SharpQuake.Framework.Definitions.Bsp
{
    public static class ModelDef
    { 
        // modelgen.h
        public const int ALIAS_VERSION = 6;

        public const int IDPOLYHEADER = ( 'O' << 24 ) + ( 'P' << 16 ) + ( 'D' << 8 ) + 'I'; // little-endian "IDPO"

        // spritegn.h
        public const int SPRITE_VERSION = 1;

        public const int IDSPRITEHEADER = ( 'P' << 24 ) + ( 'S' << 16 ) + ( 'D' << 8 ) + 'I'; // little-endian "IDSP"

        public const int VERTEXSIZE = 7;
        public const int MAX_SKINS = 32;
        public const int MAXALIASVERTS = 1024; //1024
        public const int MAXALIASFRAMES = 256;
        public const int MAXALIASTRIS = 2048;
        public const int MAX_MOD_KNOWN = 512;

        public const int MAX_LBM_HEIGHT = 480;

        public const int ANIM_CYCLE = 2;

        public static float ALIAS_BASE_SIZE_RATIO = 1.0f / 11.0f;
    }
}
