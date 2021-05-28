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
    using Definitions;
    using System;
    using System.Numerics;
    using System.Runtime.InteropServices;

    //=================================================================
    // QuakeC compiler generated data from progdefs.q1
    //=================================================================

    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public class GlobalVariables
    {
        private PadInt28 pad; //int pad[28];
        public int self;
        public int other;
        public int world;
        public float time;
        public float frametime;
        public float force_retouch;
        public int mapname;
        public float deathmatch;
        public float coop;
        public float teamplay;
        public float serverflags;
        public float total_secrets;
        public float total_monsters;
        public float found_secrets;
        public float killed_monsters;
        public float parm1;
        public float parm2;
        public float parm3;
        public float parm4;
        public float parm5;
        public float parm6;
        public float parm7;
        public float parm8;
        public float parm9;
        public float parm10;
        public float parm11;
        public float parm12;
        public float parm13;
        public float parm14;
        public float parm15;
        public float parm16;
        public Vector3 v_forward;
        public Vector3 v_up;
        public Vector3 v_right;
        public float trace_allsolid;
        public float trace_startsolid;
        public float trace_fraction;
        public Vector3 trace_endpos;
        public Vector3 trace_plane_normal;
        public float trace_plane_dist;
        public int trace_ent;
        public float trace_inopen;
        public float trace_inwater;
        public int msg_entity;
        public int main;
        public int StartFrame;
        public int PlayerPreThink;
        public int PlayerPostThink;
        public int ClientKill;
        public int ClientConnect;
        public int PutClientInServer;
        public int ClientDisconnect;
        public int SetNewParms;
        public int SetChangeParms;

        public static int SizeInBytes = Marshal.SizeOf( typeof( GlobalVariables ) );

        public void SetParams( float[] src )
        {
            if ( src.Length < ServerDef.NUM_SPAWN_PARMS )
                throw new ArgumentException( string.Format( "There must be {0} parameters!", ServerDef.NUM_SPAWN_PARMS ) );

            this.parm1 = src[0];
            this.parm2 = src[1];
            this.parm3 = src[2];
            this.parm4 = src[3];
            this.parm5 = src[4];
            this.parm6 = src[5];
            this.parm7 = src[6];
            this.parm8 = src[7];
            this.parm9 = src[8];
            this.parm10 = src[9];
            this.parm11 = src[10];
            this.parm12 = src[11];
            this.parm13 = src[12];
            this.parm14 = src[13];
            this.parm15 = src[14];
            this.parm16 = src[15];
        }
    } // globalvars_t;
}
