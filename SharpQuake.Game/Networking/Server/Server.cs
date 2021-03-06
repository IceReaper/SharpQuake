﻿/// <copyright>
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

namespace SharpQuake.Game.Networking.Server
{
    using Data.Models;
    using Framework;
    using Framework.Definitions;
    using Framework.Engine;
    using System;

    public enum server_state_t
    {
        Loading,
        Active
    }

    //=============================================================================

    // server_state_t;

    public class server_t
    {
        public bool active;             // false if only a net client
        public bool paused;
        public bool loadgame;           // handle connections specially
        public double time;
        public int lastcheck;           // used by PF_checkclient
        public double lastchecktime;
        public string name;// char		name[64];			// map name
        public string modelname;// char		modelname[64];		// maps/<name>.bsp, for model_precache[0]
        public BrushModelData worldmodel;
        public string[] model_precache; //[MAX_MODELS];	// NULL terminated
        public ModelData[] models; //[MAX_MODELS];
        public string[] sound_precache; //[MAX_SOUNDS];	// NULL terminated
        public string[] lightstyles; // [MAX_LIGHTSTYLES];
        public int num_edicts;
        public int max_edicts;
        public MemoryEdict[] edicts;        // can NOT be array indexed, because

        // edict_t is variable sized, but can
        // be used to reference the world ent
        public server_state_t state;			// some actions are only valid during load

        public MessageWriter datagram;
        public MessageWriter reliable_datagram; // copied to all clients at end of frame
        public MessageWriter signon;

        public void Clear( )
        {
            this.active = false;
            this.paused = false;
            this.loadgame = false;
            this.time = 0;
            this.lastcheck = 0;
            this.lastchecktime = 0;
            this.name = null;
            this.modelname = null;
            this.worldmodel = null;
            Array.Clear(this.model_precache, 0, this.model_precache.Length );
            Array.Clear(this.models, 0, this.models.Length );
            Array.Clear(this.sound_precache, 0, this.sound_precache.Length );
            Array.Clear(this.lightstyles, 0, this.lightstyles.Length );
            this.num_edicts = 0;
            this.max_edicts = 0;
            this.edicts = null;
            this.state = 0;
            this.datagram.Clear( );
            this.reliable_datagram.Clear( );
            this.signon.Clear( );
            GC.Collect( );
        }

        public server_t( )
        {
            this.model_precache = new string[QDef.MAX_MODELS];
            this.models = new ModelData[QDef.MAX_MODELS];
            this.sound_precache = new string[QDef.MAX_SOUNDS];
            this.lightstyles = new string[QDef.MAX_LIGHTSTYLES];
            this.datagram = new( QDef.MAX_DATAGRAM );
            this.reliable_datagram = new( QDef.MAX_DATAGRAM );
            this.signon = new( 8192 );
        }
    }// server_t;
}
