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

namespace SharpQuake.Framework.Engine
{
    using System.Numerics;
    using System.Runtime.InteropServices;

    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public struct EntVars
    {
        public float modelindex;
        public Vector3 absmin;
        public Vector3 absmax;
        public float ltime;
        public float movetype;
        public float solid;
        public Vector3 origin;
        public Vector3 oldorigin;
        public Vector3 velocity;
        public Vector3 angles;
        public Vector3 avelocity;
        public Vector3 punchangle;
        public int classname;
        public int model;
        public float frame;
        public float skin;
        public float effects;
        public Vector3 mins;
        public Vector3 maxs;
        public Vector3 size;
        public int touch;
        public int use;
        public int think;
        public int blocked;
        public float nextthink;
        public int groundentity;
        public float health;
        public float frags;
        public float weapon;
        public int weaponmodel;
        public float weaponframe;
        public float currentammo;
        public float ammo_shells;
        public float ammo_nails;
        public float ammo_rockets;
        public float ammo_cells;
        public float items;
        public float takedamage;
        public int chain;
        public float deadflag;
        public Vector3 view_ofs;
        public float button0;
        public float button1;
        public float button2;
        public float impulse;
        public float fixangle;
        public Vector3 v_angle;
        public float idealpitch;
        public int netname;
        public int enemy;
        public float flags;
        public float colormap;
        public float team;
        public float max_health;
        public float teleport_time;
        public float armortype;
        public float armorvalue;
        public float waterlevel;
        public float watertype;
        public float ideal_yaw;
        public float yaw_speed;
        public int aiment;
        public int goalentity;
        public float spawnflags;
        public int target;
        public int targetname;
        public float dmg_take;
        public float dmg_save;
        public int dmg_inflictor;
        public int owner;
        public Vector3 movedir;
        public int message;
        public float sounds;
        public int noise;
        public int noise1;
        public int noise2;
        public int noise3;

        public static int SizeInBytes = Marshal.SizeOf( typeof( EntVars ) );
    } // entvars_t
}
