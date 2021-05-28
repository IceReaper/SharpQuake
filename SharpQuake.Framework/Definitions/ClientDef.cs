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
    public static class ClientDef
    {
        public const int SIGNONS = 4;	// signon messages to receive before connected
        public const int MAX_DLIGHTS = 32;
        public const int MAX_BEAMS = 24;
        public const int MAX_EFRAGS = 640;
        public const int MAX_MAPSTRING = 2048;
        public const int MAX_DEMOS = 8;
        public const int MAX_DEMONAME = 16;
        public const int MAX_VISEDICTS = 256;
        public const int MAX_TEMP_ENTITIES = 64;	// lightning bolts, etc
        public const int MAX_STATIC_ENTITIES = 128;          // torches, etc
    }
}
