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



// client.h

namespace SharpQuake.Networking.Client
{
    using Engine.Host;
    using Framework.Definitions;
    using Framework.Engine;
    using Framework.IO.Sound;
    using Framework.Networking;
    using Framework.Networking.Client;
    using Framework.Rendering;
    using Game.Data.Models;
    using Game.World;
    using Rendering;
    using System;
    using System.Numerics;

    public struct lightstyle_t
    {
        //public int length;
        public string map; // [MAX_STYLESTRING];
    }

    public enum cactive_t
    {
        ca_dedicated, 		// a dedicated server with no ability to start a client
        ca_disconnected, 	// full screen console with no connection
        ca_connected		// valid netcon, talking to a server
    }

    

    //
    // cl_input
    //
    internal struct kbutton_t
    {
        public bool IsDown => (this.state & 1 ) != 0;

        public int down0, down1;        // key nums holding it down
        public int state;			// low bit is down state
    }

    public partial class client
    {
        public client_static_t cls => this._Static;

        public client_state_t cl => this._State;

        public Entity[] Entities => this._Entities;

        /// <summary>
        /// cl_entities[cl.viewentity]
        /// Player model (visible when out of body)
        /// </summary>
        public Entity ViewEntity => this._Entities[this._State.viewentity];

        /// <summary>
        /// cl.viewent
        /// Weapon model (only visible from inside body)
        /// </summary>
        public Entity ViewEnt => this._State.viewent;

        public float ForwardSpeed => this.Host.Cvars.ForwardSpeed.Get<float>( );

        public bool LookSpring => this.Host.Cvars.LookSpring.Get<bool>( );

        public bool LookStrafe => this.Host.Cvars.LookStrafe.Get<bool>( );

        public dlight_t[] DLights => this._DLights;

        public lightstyle_t[] LightStyle => this._LightStyle;

        public Entity[] VisEdicts => this._VisEdicts;

        public float Sensitivity => this.Host.Cvars.Sensitivity.Get<float>( );

        public float MSide => this.Host.Cvars.MSide.Get<float>( );

        public float MYaw => this.Host.Cvars.MYaw.Get<float>( );

        public float MPitch => this.Host.Cvars.MPitch.Get<float>( );

        public float MForward => this.Host.Cvars.MForward.Get<float>( );

        public string Name => this.Host.Cvars.Name.Get<string>();

        public float Color => this.Host.Cvars.Color.Get<float>( );

        public int NumVisEdicts;

        private client_static_t _Static;
        private client_state_t _State;

        public client( Host host )
        {
            this.Host = host;
            this._Static = new();
            this._State = new();
        }

        private EFrag[] _EFrags = new EFrag[ClientDef.MAX_EFRAGS]; // cl_efrags
        private Entity[] _Entities = new Entity[QDef.MAX_EDICTS]; // cl_entities
        private Entity[] _StaticEntities = new Entity[ClientDef.MAX_STATIC_ENTITIES]; // cl_static_entities
        private lightstyle_t[] _LightStyle = new lightstyle_t[QDef.MAX_LIGHTSTYLES]; // cl_lightstyle
        private dlight_t[] _DLights = new dlight_t[ClientDef.MAX_DLIGHTS]; // cl_dlights

		// cl_numvisedicts
		private Entity[] _VisEdicts = new Entity[ClientDef.MAX_VISEDICTS]; // cl_visedicts[MAX_VISEDICTS]
    }

    // lightstyle_t;

    internal static class ColorShift
    {
        public const int CSHIFT_CONTENTS = 0;
        public const int CSHIFT_DAMAGE = 1;
        public const int CSHIFT_BONUS = 2;
        public const int CSHIFT_POWERUP = 3;
        public const int NUM_CSHIFTS = 4;
    }

    public class scoreboard_t
    {
        public string name; //[MAX_SCOREBOARDNAME];

        //public float entertime;
        public int frags;

        public int colors;			// two 4 bit fields
        public byte[] translations; // [VID_GRADES*256];

        public scoreboard_t()
        {
            this.translations = new byte[Vid.VID_GRADES * 256];
        }
    } // scoreboard_t;

    public class cshift_t
    {
        public int[] destcolor; // [3];
        public int percent;		// 0-256

        public void Clear()
        {
            this.destcolor[0] = 0;
            this.destcolor[1] = 0;
            this.destcolor[2] = 0;
            this.percent = 0;
        }

        public cshift_t()
        {
            this.destcolor = new int[3];
        }

        public cshift_t( int[] destColor, int percent )
        {
            if( destColor.Length != 3 )
                throw new ArgumentException( "destColor must have length of 3 elements!" );

            this.destcolor = destColor;
            this.percent = percent;
        }
    } // cshift_t;


    internal class beam_t
    {
        public int entity;
        public ModelData model;
        public float endtime;
        public Vector3 start, end;

        public void Clear()
        {
            this.entity = 0;
            this.model = null;
            this.endtime = 0;
            this.start = Vector3.Zero;
            this.end = Vector3.Zero;
        }
    } // beam_t;

    // cactive_t;

    //
    // the client_static_t structure is persistant through an arbitrary number
    // of server connections
    //
    public class client_static_t
    {
        public cactive_t state;

        // personalization data sent to server
        public string mapstring; // [MAX_QPATH];

        public string spawnparms;//[MAX_MAPSTRING];	// to restart a level

        // demo loop control
        public int demonum;		// -1 = don't play demos

        public string[] demos; // [MAX_DEMOS][MAX_DEMONAME];		// when not playing

        // demo recording info must be here, because record is started before
        // entering a map (and clearing client_state_t)
        public bool demorecording;

        public bool demoplayback;
        public bool timedemo;
        public int forcetrack;			// -1 = use normal cd track
        public IDisposable demofile; // DisposableWrapper<BinaryReader|BinaryWriter> // FILE*
        public int td_lastframe;		// to meter out one message a frame
        public int td_startframe;		// host_framecount at start
        public float td_starttime;		// realtime at second frame of timedemo

        // connection information
        public int signon;			// 0 to SIGNONS

        public qsocket_t netcon; // qsocket_t	*netcon;
        public MessageWriter message; // sizebuf_t	message;		// writing buffer to send to server

        public client_static_t()
        {
            this.demos = new string[ClientDef.MAX_DEMOS];
            this.message = new( 1024 ); // like in Client_Init()
        }
    } // client_static_t;

    //
    // the client_state_t structure is wiped completely at every
    // server signon
    //
    public class client_state_t
    {
        public int movemessages;	// since connecting to this server

        // throw out the first couple, so the player
        // doesn't accidentally do something the
        // first frame
        public usercmd_t cmd;			// last command sent to the server

        // information for local display
        public int[] stats; //[MAX_CL_STATS];	// health, etc

        public int items;			// inventory bit flags
        public float[] item_gettime; //[32];	// cl.time of aquiring item, for blinking
        public float faceanimtime;	// use anim frame if cl.time < this

        public cshift_t[] cshifts; //[NUM_CSHIFTS];	// color shifts for damage, powerups
        public cshift_t[] prev_cshifts; //[NUM_CSHIFTS];	// and content types

        // the client maintains its own idea of view angles, which are
        // sent to the server each frame.  The server sets punchangle when
        // the view is temporarliy offset, and an angle reset commands at the start
        // of each level and after teleporting.
        public Vector3[] mviewangles; //[2];	// during demo playback viewangles is lerped

        // between these
        public Vector3 viewangles;

        public Vector3[] mvelocity; //[2];	// update by server, used for lean+bob

        // (0 is newest)
        public Vector3 velocity;		// lerped between mvelocity[0] and [1]

        public Vector3 punchangle;		// temporary offset

        // pitch drifting vars
        public float idealpitch;

        public float pitchvel;
        public bool nodrift;
        public float driftmove;
        public double laststop;

        public float viewheight;
        public float crouch;			// local amount for smoothing stepups

        public bool paused;			// send over by server
        public bool onground;
        public bool inwater;

        public int intermission;	// don't change view angle, full screen, etc
        public int completed_time;	// latched at intermission start

        public double[] mtime; //[2];		// the timestamp of last two messages
        public double time;			// clients view of time, should be between

        // servertime and oldservertime to generate
        // a lerp point for other data
        public double oldtime;		// previous cl.time, time-oldtime is used

        // to decay light values and smooth step ups

        public float last_received_message;	// (realtime) for net trouble icon

        //
        // information that is static for the entire time connected to a server
        //
        public ModelData[] model_precache; // [MAX_MODELS];

        public SoundEffect_t[] sound_precache; // [MAX_SOUNDS];

        public string levelname; // char[40];	// for display on solo scoreboard
        public int viewentity;		// cl_entitites[cl.viewentity] = player
        public int maxclients;
        public int gametype;

        // refresh related state
        public BrushModelData worldmodel;	// cl_entitites[0].model

        public EFrag free_efrags; // first free efrag in list
        public int num_entities;	// held in cl_entities array
        public int num_statics;	// held in cl_staticentities array
        public Entity viewent;			// the gun model

        public int cdtrack, looptrack;	// cd audio

        // frag scoreboard
        public scoreboard_t[] scores;		// [cl.maxclients]

        public bool HasItems( int item )
        {
            return (this.items & item ) == item;
        }

        public void Clear()
        {
            this.movemessages = 0;
            this.cmd.Clear();
            Array.Clear(this.stats, 0, this.stats.Length );
            this.items = 0;
            Array.Clear(this.item_gettime, 0, this.item_gettime.Length );
            this.faceanimtime = 0;

            foreach( var cs in this.cshifts )
                cs.Clear();
            foreach( var cs in this.prev_cshifts )
                cs.Clear();

            this.mviewangles[0] = Vector3.Zero;
            this.mviewangles[1] = Vector3.Zero;
            this.viewangles = Vector3.Zero;
            this.mvelocity[0] = Vector3.Zero;
            this.mvelocity[1] = Vector3.Zero;
            this.velocity = Vector3.Zero;
            this.punchangle = Vector3.Zero;

            this.idealpitch = 0;
            this.pitchvel = 0;
            this.nodrift = false;
            this.driftmove = 0;
            this.laststop = 0;

            this.viewheight = 0;
            this.crouch = 0;

            this.paused = false;
            this.onground = false;
            this.inwater = false;

            this.intermission = 0;
            this.completed_time = 0;

            this.mtime[0] = 0;
            this.mtime[1] = 0;
            this.time = 0;
            this.oldtime = 0;
            this.last_received_message = 0;

            Array.Clear(this.model_precache, 0, this.model_precache.Length );
            Array.Clear(this.sound_precache, 0, this.sound_precache.Length );

            this.levelname = null;
            this.viewentity = 0;
            this.maxclients = 0;
            this.gametype = 0;

            this.worldmodel = null;
            this.free_efrags = null;
            this.num_entities = 0;
            this.num_statics = 0;
            this.viewent.Clear();

            this.cdtrack = 0;
            this.looptrack = 0;

            this.scores = null;
        }

        public client_state_t()
        {
            this.stats = new int[QStatsDef.MAX_CL_STATS];
            this.item_gettime = new float[32]; // ???????????

            this.cshifts = new cshift_t[ColorShift.NUM_CSHIFTS];
            for( var i = 0; i < ColorShift.NUM_CSHIFTS; i++ )
                this.cshifts[i] = new();

            this.prev_cshifts = new cshift_t[ColorShift.NUM_CSHIFTS];
            for( var i = 0; i < ColorShift.NUM_CSHIFTS; i++ )
                this.prev_cshifts[i] = new();

            this.mviewangles = new Vector3[2]; //??????
            this.mvelocity = new Vector3[2];
            this.mtime = new double[2];
            this.model_precache = new ModelData[QDef.MAX_MODELS];
            this.sound_precache = new SoundEffect_t[QDef.MAX_SOUNDS];
            this.viewent = new();
        }
    } //client_state_t;

    // usercmd_t;

    // kbutton_t;
}
