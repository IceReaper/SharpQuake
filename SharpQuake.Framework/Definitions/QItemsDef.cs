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
    public static class QItemsDef
    {
        // stock defines

        public static int IT_SHOTGUN = 1;
        public static int IT_SUPER_SHOTGUN = 2;
        public static int IT_NAILGUN = 4;
        public static int IT_SUPER_NAILGUN = 8;
        public static int IT_GRENADE_LAUNCHER = 16;
        public static int IT_ROCKET_LAUNCHER = 32;
        public static int IT_LIGHTNING = 64;
        public static int IT_SUPER_LIGHTNING = 128;
        public static int IT_SHELLS = 256;
        public static int IT_NAILS = 512;
        public static int IT_ROCKETS = 1024;
        public static int IT_CELLS = 2048;
        public static int IT_AXE = 4096;
        public static int IT_ARMOR1 = 8192;
        public static int IT_ARMOR2 = 16384;
        public static int IT_ARMOR3 = 32768;
        public static int IT_SUPERHEALTH = 65536;
        public static int IT_KEY1 = 131072;
        public static int IT_KEY2 = 262144;
        public static int IT_INVISIBILITY = 524288;
        public static int IT_INVULNERABILITY = 1048576;
        public static int IT_SUIT = 2097152;
        public static int IT_QUAD = 4194304;
        public static int IT_SIGIL1 = 1 << 28;
        public static int IT_SIGIL2 = 1 << 29;
        public static int IT_SIGIL3 = 1 << 30;
        public static int IT_SIGIL4 = 1 << 31;

        //===========================================
        //rogue changed and added defines

        public static int RIT_SHELLS = 128;
        public static int RIT_NAILS = 256;
        public static int RIT_ROCKETS = 512;
        public static int RIT_CELLS = 1024;
        public static int RIT_AXE = 2048;
        public static int RIT_LAVA_NAILGUN = 4096;
        public static int RIT_LAVA_SUPER_NAILGUN = 8192;
        public static int RIT_MULTI_GRENADE = 16384;
        public static int RIT_MULTI_ROCKET = 32768;
        public static int RIT_PLASMA_GUN = 65536;
        public static int RIT_ARMOR1 = 8388608;
        public static int RIT_ARMOR2 = 16777216;
        public static int RIT_ARMOR3 = 33554432;
        public static int RIT_LAVA_NAILS = 67108864;
        public static int RIT_PLASMA_AMMO = 134217728;
        public static int RIT_MULTI_ROCKETS = 268435456;
        public static int RIT_SHIELD = 536870912;
        public static int RIT_ANTIGRAV = 1073741824;
        public static int RIT_SUPERHEALTH = -2147483648;// 2147483648;

        //MED 01/04/97 added hipnotic defines
        //===========================================
        //hipnotic added defines
        public static int HIT_PROXIMITY_GUN_BIT = 16;

        public static int HIT_MJOLNIR_BIT = 7;
        public static int HIT_LASER_CANNON_BIT = 23;
        public static int HIT_PROXIMITY_GUN = 1 << QItemsDef.HIT_PROXIMITY_GUN_BIT;
        public static int HIT_MJOLNIR = 1 << QItemsDef.HIT_MJOLNIR_BIT;
        public static int HIT_LASER_CANNON = 1 << QItemsDef.HIT_LASER_CANNON_BIT;
        public static int HIT_WETSUIT = 1 << ( 23 + 2 );
        public static int HIT_EMPATHY_SHIELDS = 1 << ( 23 + 3 );
        //===========================================
    }
}
