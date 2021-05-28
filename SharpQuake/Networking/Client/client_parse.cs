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



// cl_parse.c

namespace SharpQuake.Networking.Client
{
    using Desktop;
    using Framework.Definitions;
    using Framework.Engine;
    using Framework.IO;
    using Framework.Mathematics;
    using Framework.Rendering;
    using Game.Data.Models;
    using Game.World;
    using Rendering;
    using Sound;
    using System;

    partial class client
    {
        private const string ConsoleBar = "\n\n\u001D\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001F\n\n";

        private static string[] _SvcStrings = new string[]
        {
            "svc_bad",
            "svc_nop",
            "svc_disconnect",
            "svc_updatestat",
            "svc_version",		// [long] server version
	        "svc_setview",		// [short] entity number
	        "svc_sound",			// <see code>
	        "svc_time",			// [float] server time
	        "svc_print",			// [string] null terminated string
	        "svc_stufftext",		// [string] stuffed into client's console buffer
						        // the string should be \n terminated
	        "svc_setangle",		// [vec3] set the view angle to this absolute value

	        "svc_serverinfo",		// [long] version
						        // [string] signon string
						        // [string]..[0]model cache [string]...[0]sounds cache
						        // [string]..[0]item cache
	        "svc_lightstyle",		// [byte] [string]
	        "svc_updatename",		// [byte] [string]
	        "svc_updatefrags",	// [byte] [short]
	        "svc_clientdata",		// <shortbits + data>
	        "svc_stopsound",		// <see code>
	        "svc_updatecolors",	// [byte] [byte]
	        "svc_particle",		// [vec3] <variable>
	        "svc_damage",			// [byte] impact [byte] blood [vec3] from

	        "svc_spawnstatic",
            "OBSOLETE svc_spawnbinary",
            "svc_spawnbaseline",

            "svc_temp_entity",		// <variable>
	        "svc_setpause",
            "svc_signonnum",
            "svc_centerprint",
            "svc_killedmonster",
            "svc_foundsecret",
            "svc_spawnstaticsound",
            "svc_intermission",
            "svc_finale",			// [string] music [string] text
	        "svc_cdtrack",			// [byte] track [byte] looptrack
	        "svc_sellscreen",
            "svc_cutscene"
        };

        private int[] _BitCounts = new int[16]; // bitcounts
        private object _MsgState; // used by KeepaliveMessage function
        private float _LastMsg; // static float lastmsg from CL_KeepaliveMessage

        /// <summary>
        /// CL_ParseServerMessage
        /// </summary>
        private void ParseServerMessage()
        {
            //
            // if recording demos, copy the message out
            //
            if(this.Host.Cvars.ShowNet.Get<int>( ) == 1 )
                this.Host.Console.Print( "{0} ", this.Host.Network.Message.Length );
            else if(this.Host.Cvars.ShowNet.Get<int>( ) == 2 )
                this.Host.Console.Print( "------------------\n" );

            this.cl.onground = false;	// unless the server says otherwise

            //
            // parse the message
            //
            this.Host.Network.Reader.Reset();
            int i;
            while( true )
            {
                if(this.Host.Network.Reader.IsBadRead )
                    this.Host.Error( "CL_ParseServerMessage: Bad server message" );

                var cmd = this.Host.Network.Reader.ReadByte();
                if( cmd == -1 )
                {
                    this.ShowNet( "END OF MESSAGE" );
                    return;	// end of message
                }

                // if the high bit of the command byte is set, it is a fast update
                if( ( cmd & 128 ) != 0 )
                {
                    this.ShowNet( "fast update" );
                    this.ParseUpdate( cmd & 127 );
                    continue;
                }

                this.ShowNet( client._SvcStrings[cmd] );

                // other commands
                switch( cmd )
                {
                    default:
                        this.Host.Error( "CL_ParseServerMessage: Illegible server message\n" );
                        break;

                    case ProtocolDef.svc_nop:
                        break;

                    case ProtocolDef.svc_time:
                        this.cl.mtime[1] = this.cl.mtime[0];
                        this.cl.mtime[0] = this.Host.Network.Reader.ReadFloat();
                        break;

                    case ProtocolDef.svc_clientdata:
                        i = this.Host.Network.Reader.ReadShort();
                        this.ParseClientData( i );
                        break;

                    case ProtocolDef.svc_version:
                        i = this.Host.Network.Reader.ReadLong();
                        if( i != ProtocolDef.PROTOCOL_VERSION )
                            this.Host.Error( "CL_ParseServerMessage: Server is protocol {0} instead of {1}\n", i, ProtocolDef.PROTOCOL_VERSION );
                        break;

                    case ProtocolDef.svc_disconnect:
                        this.Host.EndGame( "Server disconnected\n" );
                        break;

                    case ProtocolDef.svc_print:
                        this.Host.Console.Print(this.Host.Network.Reader.ReadString() );
                        break;

                    case ProtocolDef.svc_centerprint:
                        this.Host.Screen.CenterPrint(this.Host.Network.Reader.ReadString() );
                        break;

                    case ProtocolDef.svc_stufftext:
                        this.Host.Commands.Buffer.Append(this.Host.Network.Reader.ReadString() );
                        break;

                    case ProtocolDef.svc_damage:
                        this.Host.View.ParseDamage();
                        break;

                    case ProtocolDef.svc_serverinfo:
                        this.ParseServerInfo();
                        this.Host.Screen.vid.recalc_refdef = true;	// leave intermission full screen
                        break;

                    case ProtocolDef.svc_setangle:
                        this.cl.viewangles.X = this.Host.Network.Reader.ReadAngle();
                        this.cl.viewangles.Y = this.Host.Network.Reader.ReadAngle();
                        this.cl.viewangles.Z = this.Host.Network.Reader.ReadAngle();
                        break;

                    case ProtocolDef.svc_setview:
                        this.cl.viewentity = this.Host.Network.Reader.ReadShort();
                        break;

                    case ProtocolDef.svc_lightstyle:
                        i = this.Host.Network.Reader.ReadByte();
                        if( i >= QDef.MAX_LIGHTSTYLES )
                            Utilities.Error( "svc_lightstyle > MAX_LIGHTSTYLES" );

                        this._LightStyle[i].map = this.Host.Network.Reader.ReadString();
                        break;

                    case ProtocolDef.svc_sound:
                        this.ParseStartSoundPacket();
                        break;

                    case ProtocolDef.svc_stopsound:
                        i = this.Host.Network.Reader.ReadShort();
                        this.Host.Sound.StopSound( i >> 3, i & 7 );
                        break;

                    case ProtocolDef.svc_updatename:
                        this.Host.Hud.Changed();
                        i = this.Host.Network.Reader.ReadByte();
                        if( i >= this.cl.maxclients )
                            this.Host.Error( "CL_ParseServerMessage: svc_updatename > MAX_SCOREBOARD" );

                        this.cl.scores[i].name = this.Host.Network.Reader.ReadString();
                        break;

                    case ProtocolDef.svc_updatefrags:
                        this.Host.Hud.Changed();
                        i = this.Host.Network.Reader.ReadByte();
                        if( i >= this.cl.maxclients )
                            this.Host.Error( "CL_ParseServerMessage: svc_updatefrags > MAX_SCOREBOARD" );

                        this.cl.scores[i].frags = this.Host.Network.Reader.ReadShort();
                        break;

                    case ProtocolDef.svc_updatecolors:
                        this.Host.Hud.Changed();
                        i = this.Host.Network.Reader.ReadByte();
                        if( i >= this.cl.maxclients )
                            this.Host.Error( "CL_ParseServerMessage: svc_updatecolors > MAX_SCOREBOARD" );

                        this.cl.scores[i].colors = this.Host.Network.Reader.ReadByte();
                        this.NewTranslation( i );
                        break;

                    case ProtocolDef.svc_particle:
                        this.Host.RenderContext.Particles.ParseParticleEffect(this.Host.Client.cl.time, this.Host.Network.Reader );
                        break;

                    case ProtocolDef.svc_spawnbaseline:
                        i = this.Host.Network.Reader.ReadShort();
                        // must use CL_EntityNum() to force cl.num_entities up
                        this.ParseBaseline(this.EntityNum( i ) );
                        break;

                    case ProtocolDef.svc_spawnstatic:
                        this.ParseStatic();
                        break;

                    case ProtocolDef.svc_temp_entity:
                        this.ParseTempEntity();
                        break;

                    case ProtocolDef.svc_setpause:
                    {
                        this.cl.paused = this.Host.Network.Reader.ReadByte() != 0;

                        if(this.cl.paused )
                            this.Host.CDAudio.Pause();
                        else
                            this.Host.CDAudio.Resume();
                    }
                    break;

                    case ProtocolDef.svc_signonnum:
                        i = this.Host.Network.Reader.ReadByte();
                        if( i <= this.cls.signon )
                            this.Host.Error( "Received signon {0} when at {1}", i, this.cls.signon );

                        this.cls.signon = i;
                        this.SignonReply();
                        break;

                    case ProtocolDef.svc_killedmonster:
                        this.cl.stats[QStatsDef.STAT_MONSTERS]++;
                        break;

                    case ProtocolDef.svc_foundsecret:
                        this.cl.stats[QStatsDef.STAT_SECRETS]++;
                        break;

                    case ProtocolDef.svc_updatestat:
                        i = this.Host.Network.Reader.ReadByte();
                        if( i < 0 || i >= QStatsDef.MAX_CL_STATS )
                            Utilities.Error( "svc_updatestat: {0} is invalid", i );

                        this.cl.stats[i] = this.Host.Network.Reader.ReadLong();
                        break;

                    case ProtocolDef.svc_spawnstaticsound:
                        this.ParseStaticSound();
                        break;

                    case ProtocolDef.svc_cdtrack:
                        this.cl.cdtrack = this.Host.Network.Reader.ReadByte();
                        this.cl.looptrack = this.Host.Network.Reader.ReadByte();
                        if( (this.cls.demoplayback || this.cls.demorecording ) && this.cls.forcetrack != -1 )
                            this.Host.CDAudio.Play( ( byte )this.cls.forcetrack, true );
                        else
                            this.Host.CDAudio.Play( ( byte )this.cl.cdtrack, true );
                        break;

                    case ProtocolDef.svc_intermission:
                        this.cl.intermission = 1;
                        this.cl.completed_time = ( int )this.cl.time;
                        this.Host.Screen.vid.recalc_refdef = true;	// go to full screen
                        break;

                    case ProtocolDef.svc_finale:
                        this.cl.intermission = 2;
                        this.cl.completed_time = ( int )this.cl.time;
                        this.Host.Screen.vid.recalc_refdef = true;	// go to full screen
                        this.Host.Screen.CenterPrint(this.Host.Network.Reader.ReadString() );
                        break;

                    case ProtocolDef.svc_cutscene:
                        this.cl.intermission = 3;
                        this.cl.completed_time = ( int )this.cl.time;
                        this.Host.Screen.vid.recalc_refdef = true;	// go to full screen
                        this.Host.Screen.CenterPrint(this.Host.Network.Reader.ReadString() );
                        break;

                    case ProtocolDef.svc_sellscreen:
                        this.Host.Commands.ExecuteString( "help", CommandSource.Command );
                        break;
                }
            }
        }

        private void ShowNet( string s )
        {
            if(this.Host.Cvars.ShowNet.Get<int>( ) == 2 )
                this.Host.Console.Print( "{0,3}:{1}\n", this.Host.Network.Reader.Position - 1, s );
        }

        /// <summary>
        /// CL_ParseUpdate
        ///
        /// Parse an entity update message from the server
        /// If an entities model or origin changes from frame to frame, it must be
        /// relinked.  Other attributes can change without relinking.
        /// </summary>
        private void ParseUpdate( int bits )
        {
            int i;

            if(this.cls.signon == ClientDef.SIGNONS - 1 )
            {
                // first update is the final signon stage
                this.cls.signon = ClientDef.SIGNONS;
                this.SignonReply();
            }

            if( ( bits & ProtocolDef.U_MOREBITS ) != 0 )
            {
                i = this.Host.Network.Reader.ReadByte();
                bits |= i << 8;
            }

            int num;

            if( ( bits & ProtocolDef.U_LONGENTITY ) != 0 )
                num = this.Host.Network.Reader.ReadShort();
            else
                num = this.Host.Network.Reader.ReadByte();

            var ent = this.EntityNum( num );
            for( i = 0; i < 16; i++ )
            {
                if( ( bits & ( 1 << i ) ) != 0 )
                    this._BitCounts[i]++;
            }

            var forcelink = false;
            if( ent.msgtime != this.cl.mtime[1] )
                forcelink = true;	// no previous frame to lerp from

            ent.msgtime = this.cl.mtime[0];
            int modnum;
            if( ( bits & ProtocolDef.U_MODEL ) != 0 )
            {
                modnum = this.Host.Network.Reader.ReadByte();
                if( modnum >= QDef.MAX_MODELS )
                    this.Host.Error( "CL_ParseModel: bad modnum" );
            }
            else
                modnum = ent.baseline.modelindex;

            var model = this.cl.model_precache[modnum];
            if( model != ent.model )
            {
                ent.model = model;
                // automatic animation (torches, etc) can be either all together
                // or randomized
                if( model != null )
                {
                    if( model.SyncType == SyncType.ST_RAND )
                        ent.syncbase = ( float ) ( MathLib.Random() & 0x7fff ) / 0x7fff;
                    else
                        ent.syncbase = 0;
                }
                else
                    forcelink = true;	// hack to make null model players work

                if( num > 0 && num <= this.cl.maxclients )
                    this.Host.RenderContext.TranslatePlayerSkin( num - 1 );
            }

            if( ( bits & ProtocolDef.U_FRAME ) != 0 )
                ent.frame = this.Host.Network.Reader.ReadByte();
            else
                ent.frame = ent.baseline.frame;

            if( ( bits & ProtocolDef.U_COLORMAP ) != 0 )
                i = this.Host.Network.Reader.ReadByte();
            else
                i = ent.baseline.colormap;
            if( i == 0 )
                ent.colormap = this.Host.Screen.vid.colormap;
            else
            {
                if( i > this.cl.maxclients )
                    Utilities.Error( "i >= cl.maxclients" );
                ent.colormap = this.cl.scores[i - 1].translations;
            }

            int skin;
            if( ( bits & ProtocolDef.U_SKIN ) != 0 )
                skin = this.Host.Network.Reader.ReadByte();
            else
                skin = ent.baseline.skin;
            if( skin != ent.skinnum )
            {
                ent.skinnum = skin;
                if( num > 0 && num <= this.cl.maxclients )
                    this.Host.RenderContext.TranslatePlayerSkin( num - 1 );
            }

            if( ( bits & ProtocolDef.U_EFFECTS ) != 0 )
                ent.effects = this.Host.Network.Reader.ReadByte();
            else
                ent.effects = ent.baseline.effects;

            // shift the known values for interpolation
            ent.msg_origins[1] = ent.msg_origins[0];
            ent.msg_angles[1] = ent.msg_angles[0];

            if( ( bits & ProtocolDef.U_ORIGIN1 ) != 0 )
                ent.msg_origins[0].X = this.Host.Network.Reader.ReadCoord();
            else
                ent.msg_origins[0].X = ent.baseline.origin.X;
            if( ( bits & ProtocolDef.U_ANGLE1 ) != 0 )
                ent.msg_angles[0].X = this.Host.Network.Reader.ReadAngle();
            else
                ent.msg_angles[0].X = ent.baseline.angles.X;

            if( ( bits & ProtocolDef.U_ORIGIN2 ) != 0 )
                ent.msg_origins[0].Y = this.Host.Network.Reader.ReadCoord();
            else
                ent.msg_origins[0].Y = ent.baseline.origin.Y;
            if( ( bits & ProtocolDef.U_ANGLE2 ) != 0 )
                ent.msg_angles[0].Y = this.Host.Network.Reader.ReadAngle();
            else
                ent.msg_angles[0].Y = ent.baseline.angles.Y;

            if( ( bits & ProtocolDef.U_ORIGIN3 ) != 0 )
                ent.msg_origins[0].Z = this.Host.Network.Reader.ReadCoord();
            else
                ent.msg_origins[0].Z = ent.baseline.origin.Z;
            if( ( bits & ProtocolDef.U_ANGLE3 ) != 0 )
                ent.msg_angles[0].Z = this.Host.Network.Reader.ReadAngle();
            else
                ent.msg_angles[0].Z = ent.baseline.angles.Z;

            if( ( bits & ProtocolDef.U_NOLERP ) != 0 )
                ent.forcelink = true;

            if( forcelink )
            {	// didn't have an update last message
                ent.msg_origins[1] = ent.msg_origins[0];
                ent.origin = ent.msg_origins[0];
                ent.msg_angles[1] = ent.msg_angles[0];
                ent.angles = ent.msg_angles[0];
                ent.forcelink = true;
            }
        }

        /// <summary>
        /// CL_ParseClientdata
        /// Server information pertaining to this client only
        /// </summary>
        private void ParseClientData( int bits )
        {
            if( ( bits & ProtocolDef.SU_VIEWHEIGHT ) != 0 )
                this.cl.viewheight = this.Host.Network.Reader.ReadChar();
            else
                this.cl.viewheight = ProtocolDef.DEFAULT_VIEWHEIGHT;

            if( ( bits & ProtocolDef.SU_IDEALPITCH ) != 0 )
                this.cl.idealpitch = this.Host.Network.Reader.ReadChar();
            else
                this.cl.idealpitch = 0;

            this.cl.mvelocity[1] = this.cl.mvelocity[0];
            for( var i = 0; i < 3; i++ )
            {
                if( ( bits & ( ProtocolDef.SU_PUNCH1 << i ) ) != 0 )
                    MathLib.SetComp( ref this.cl.punchangle, i, this.Host.Network.Reader.ReadChar() );
                else
                    MathLib.SetComp( ref this.cl.punchangle, i, 0 );
                if( ( bits & ( ProtocolDef.SU_VELOCITY1 << i ) ) != 0 )
                    MathLib.SetComp( ref this.cl.mvelocity[0], i, this.Host.Network.Reader.ReadChar() * 16 );
                else
                    MathLib.SetComp( ref this.cl.mvelocity[0], i, 0 );
            }

            // [always sent]	if (bits & SU_ITEMS)
            var i2 = this.Host.Network.Reader.ReadLong();

            if(this.cl.items != i2 )
            {	// set flash times
                this.Host.Hud.Changed();
                for( var j = 0; j < 32; j++ )
                {
                    if( ( i2 & ( 1 << j ) ) != 0 && (this.cl.items & ( 1 << j ) ) == 0 )
                        this.cl.item_gettime[j] = ( float )this.cl.time;
                }

                this.cl.items = i2;
            }

            this.cl.onground = ( bits & ProtocolDef.SU_ONGROUND ) != 0;
            this.cl.inwater = ( bits & ProtocolDef.SU_INWATER ) != 0;

            if( ( bits & ProtocolDef.SU_WEAPONFRAME ) != 0 )
                this.cl.stats[QStatsDef.STAT_WEAPONFRAME] = this.Host.Network.Reader.ReadByte();
            else
                this.cl.stats[QStatsDef.STAT_WEAPONFRAME] = 0;

            if( ( bits & ProtocolDef.SU_ARMOR ) != 0 )
                i2 = this.Host.Network.Reader.ReadByte();
            else
                i2 = 0;
            if(this.cl.stats[QStatsDef.STAT_ARMOR] != i2 )
            {
                this.cl.stats[QStatsDef.STAT_ARMOR] = i2;
                this.Host.Hud.Changed();
            }

            if( ( bits & ProtocolDef.SU_WEAPON ) != 0 )
                i2 = this.Host.Network.Reader.ReadByte();
            else
                i2 = 0;
            if(this.cl.stats[QStatsDef.STAT_WEAPON] != i2 )
            {
                this.cl.stats[QStatsDef.STAT_WEAPON] = i2;
                this.Host.Hud.Changed();
            }

            i2 = this.Host.Network.Reader.ReadShort();
            if(this.cl.stats[QStatsDef.STAT_HEALTH] != i2 )
            {
                this.cl.stats[QStatsDef.STAT_HEALTH] = i2;
                this.Host.Hud.Changed();
            }

            i2 = this.Host.Network.Reader.ReadByte();
            if(this.cl.stats[QStatsDef.STAT_AMMO] != i2 )
            {
                this.cl.stats[QStatsDef.STAT_AMMO] = i2;
                this.Host.Hud.Changed();
            }

            for( i2 = 0; i2 < 4; i2++ )
            {
                var j = this.Host.Network.Reader.ReadByte();
                if(this.cl.stats[QStatsDef.STAT_SHELLS + i2] != j )
                {
                    this.cl.stats[QStatsDef.STAT_SHELLS + i2] = j;
                    this.Host.Hud.Changed();
                }
            }

            i2 = this.Host.Network.Reader.ReadByte();

            // Change
            if( MainWindow.Common.GameKind == GameKind.StandardQuake )
            {
                if(this.cl.stats[QStatsDef.STAT_ACTIVEWEAPON] != i2 )
                {
                    this.cl.stats[QStatsDef.STAT_ACTIVEWEAPON] = i2;
                    this.Host.Hud.Changed();
                }
            }
            else
            {
                if(this.cl.stats[QStatsDef.STAT_ACTIVEWEAPON] != 1 << i2 )
                {
                    this.cl.stats[QStatsDef.STAT_ACTIVEWEAPON] = 1 << i2;
                    this.Host.Hud.Changed();
                }
            }
        }

        /// <summary>
        /// CL_ParseServerInfo
        /// </summary>
        private void ParseServerInfo()
        {
            this.Host.Console.DPrint( "Serverinfo packet received.\n" );

            //
            // wipe the client_state_t struct
            //
            this.ClearState();

            // parse protocol version number
            var i = this.Host.Network.Reader.ReadLong();
            if( i != ProtocolDef.PROTOCOL_VERSION )
            {
                this.Host.Console.Print( "Server returned version {0}, not {1}", i, ProtocolDef.PROTOCOL_VERSION );
                return;
            }

            // parse maxclients
            this.cl.maxclients = this.Host.Network.Reader.ReadByte();
            if(this.cl.maxclients < 1 || this.cl.maxclients > QDef.MAX_SCOREBOARD )
            {
                this.Host.Console.Print( "Bad maxclients ({0}) from server\n", this.cl.maxclients );
                return;
            }

            this.cl.scores = new scoreboard_t[this.cl.maxclients];// Hunk_AllocName (cl.maxclients*sizeof(*cl.scores), "scores");
            for( i = 0; i < this.cl.scores.Length; i++ )
                this.cl.scores[i] = new();

            // parse gametype
            this.cl.gametype = this.Host.Network.Reader.ReadByte();

            // parse signon message
            var str = this.Host.Network.Reader.ReadString();
            this.cl.levelname = Utilities.Copy( str, 40 );

            // seperate the printfs so the server message can have a color
            this.Host.Console.Print( client.ConsoleBar );
            this.Host.Console.Print( "{0}{1}\n", ( char ) 2, str );

            //
            // first we go through and touch all of the precache data that still
            // happens to be in the cache, so precaching something else doesn't
            // needlessly purge it
            //

            // precache models
            Array.Clear(this.cl.model_precache, 0, this.cl.model_precache.Length );
            int nummodels;
            var model_precache = new string[QDef.MAX_MODELS];
            for( nummodels = 1; ; nummodels++ )
            {
                str = this.Host.Network.Reader.ReadString();
                if( string.IsNullOrEmpty( str ) )
                    break;

                if( nummodels == QDef.MAX_MODELS )
                {
                    this.Host.Console.Print( "Server sent too many model precaches\n" );
                    return;
                }
                model_precache[nummodels] = str;
                this.Host.Model.TouchModel( str );
            }

            // precache sounds
            Array.Clear(this.cl.sound_precache, 0, this.cl.sound_precache.Length );
            int numsounds;
            var sound_precache = new string[QDef.MAX_SOUNDS];
            for( numsounds = 1; ; numsounds++ )
            {
                str = this.Host.Network.Reader.ReadString();
                if( string.IsNullOrEmpty( str ) )
                    break;
                if( numsounds == QDef.MAX_SOUNDS )
                {
                    this.Host.Console.Print( "Server sent too many sound precaches\n" );
                    return;
                }
                sound_precache[numsounds] = str;
                this.Host.Sound.TouchSound( str );
            }

            //
            // now we try to load everything else until a cache allocation fails
            //
            for( i = 1; i < nummodels; i++ )
            {
                var name = model_precache[i];
                var n = name.ToLower( );
                var type = ModelType.mod_sprite;

                if ( (n.StartsWith( "*" ) && !n.Contains( ".mdl" )) || n.Contains( ".bsp" ) )
                    type = ModelType.mod_brush;
                else if ( n.Contains( ".mdl" ) )
                    type = ModelType.mod_alias;
                else
                    type = ModelType.mod_sprite;

                if ( name == "progs/player.mdl")
                {

                 }

                this.cl.model_precache[i] = this.Host.Model.ForName( name, false, type );
                if(this.cl.model_precache[i] == null )
                {
                    this.Host.Console.Print( "Model {0} not found\n", name );
                    return;
                }

                this.KeepaliveMessage();
            }

            this.Host.Sound.BeginPrecaching();
            for( i = 1; i < numsounds; i++ )
            {
                this.cl.sound_precache[i] = this.Host.Sound.PrecacheSound( sound_precache[i] );
                this.KeepaliveMessage();
            }

            this.Host.Sound.EndPrecaching();

            // local state
            this.cl.worldmodel = ( BrushModelData )this.cl.model_precache[1];
            this._Entities[0].model = this.cl.model_precache[1];

            this.Host.RenderContext.NewMap();

            this.Host.NoClipAngleHack = false; // noclip is turned off at start

            GC.Collect();
        }

        // CL_ParseStartSoundPacket
        private void ParseStartSoundPacket()
        {
            var field_mask = this.Host.Network.Reader.ReadByte();
            int volume;
            float attenuation;

            if( ( field_mask & ProtocolDef.SND_VOLUME ) != 0 )
                volume = this.Host.Network.Reader.ReadByte();
            else
                volume = snd.DEFAULT_SOUND_PACKET_VOLUME;

            if( ( field_mask & ProtocolDef.SND_ATTENUATION ) != 0 )
                attenuation = this.Host.Network.Reader.ReadByte() / 64.0f;
            else
                attenuation = snd.DEFAULT_SOUND_PACKET_ATTENUATION;

            var channel = this.Host.Network.Reader.ReadShort();
            var sound_num = this.Host.Network.Reader.ReadByte();

            var ent = channel >> 3;
            channel &= 7;

            if( ent > QDef.MAX_EDICTS )
                this.Host.Error( "CL_ParseStartSoundPacket: ent = {0}", ent );

            var pos = this.Host.Network.Reader.ReadCoords();
            this.Host.Sound.StartSound( ent, channel, this.cl.sound_precache[sound_num], ref pos, volume / 255.0f, attenuation );
        }

        // CL_NewTranslation
        private void NewTranslation( int slot )
        {
            if( slot > this.cl.maxclients )
                Utilities.Error( "CL_NewTranslation: slot > cl.maxclients" );

            var dest = this.cl.scores[slot].translations;
            var source = this.Host.Screen.vid.colormap;
            Array.Copy( source, dest, dest.Length );

            var top = this.cl.scores[slot].colors & 0xf0;
            var bottom = (this.cl.scores[slot].colors & 15 ) << 4;

            this.Host.RenderContext.TranslatePlayerSkin( slot );

            for( int i = 0, offset = 0; i < Vid.VID_GRADES; i++ )//, dest += 256, source+=256)
            {
                if( top < 128 )	// the artists made some backwards ranges.  sigh.
                    Buffer.BlockCopy( source, offset + top, dest, offset + render.TOP_RANGE, 16 );  //memcpy (dest + Render.TOP_RANGE, source + top, 16);
                else
                {
                    for( var j = 0; j < 16; j++ )
                        dest[offset + render.TOP_RANGE + j] = source[offset + top + 15 - j];
                }

                if( bottom < 128 )
                    Buffer.BlockCopy( source, offset + bottom, dest, offset + render.BOTTOM_RANGE, 16 ); // memcpy(dest + Render.BOTTOM_RANGE, source + bottom, 16);
                else
                {
                    for( var j = 0; j < 16; j++ )
                        dest[offset + render.BOTTOM_RANGE + j] = source[offset + bottom + 15 - j];
                }

                offset += 256;
            }
        }

        /// <summary>
        /// CL_EntityNum
        ///
        /// This error checks and tracks the total number of entities
        /// </summary>
        /// <param name="num"></param>
        /// <returns></returns>
        private Entity EntityNum( int num )
        {
            if( num >= this.cl.num_entities )
            {
                if( num >= QDef.MAX_EDICTS )
                    this.Host.Error( "CL_EntityNum: %i is an invalid number", num );
                while(this.cl.num_entities <= num )
                {
                    this._Entities[this.cl.num_entities].colormap = this.Host.Screen.vid.colormap;
                    this.cl.num_entities++;
                }
            }

            return this._Entities[num];
        }

        /// <summary>
        /// CL_ParseBaseline
        /// </summary>
        /// <param name="ent"></param>
        private void ParseBaseline( Entity ent )
        {
            ent.baseline.modelindex = this.Host.Network.Reader.ReadByte();
            ent.baseline.frame = this.Host.Network.Reader.ReadByte();
            ent.baseline.colormap = this.Host.Network.Reader.ReadByte();
            ent.baseline.skin = this.Host.Network.Reader.ReadByte();
            ent.baseline.origin.X = this.Host.Network.Reader.ReadCoord();
            ent.baseline.angles.X = this.Host.Network.Reader.ReadAngle();
            ent.baseline.origin.Y = this.Host.Network.Reader.ReadCoord();
            ent.baseline.angles.Y = this.Host.Network.Reader.ReadAngle();
            ent.baseline.origin.Z = this.Host.Network.Reader.ReadCoord();
            ent.baseline.angles.Z = this.Host.Network.Reader.ReadAngle();
        }

        /// <summary>
        /// CL_ParseStatic
        /// </summary>
        private void ParseStatic()
        {
            var i = this.cl.num_statics;
            if( i >= ClientDef.MAX_STATIC_ENTITIES )
                this.Host.Error( "Too many static entities" );

            var ent = this._StaticEntities[i];
            this.cl.num_statics++;
            this.ParseBaseline( ent );

            // copy it to the current state
            ent.model = this.cl.model_precache[ent.baseline.modelindex];
            ent.frame = ent.baseline.frame;
            ent.colormap = this.Host.Screen.vid.colormap;
            ent.skinnum = ent.baseline.skin;
            ent.effects = ent.baseline.effects;
            ent.origin = Utilities.ToVector( ref ent.baseline.origin );
            ent.angles = Utilities.ToVector( ref ent.baseline.angles );
            this.Host.RenderContext.AddEfrags( ent );
        }

        /// <summary>
        /// CL_ParseStaticSound
        /// </summary>
        private void ParseStaticSound()
        {
            var org = this.Host.Network.Reader.ReadCoords();
            var sound_num = this.Host.Network.Reader.ReadByte();
            var vol = this.Host.Network.Reader.ReadByte();
            var atten = this.Host.Network.Reader.ReadByte();

            this.Host.Sound.StaticSound(this.cl.sound_precache[sound_num], ref org, vol, atten );
        }

        /// <summary>
        /// CL_KeepaliveMessage
        /// When the client is taking a long time to load stuff, send keepalive messages
        /// so the server doesn't disconnect.
        /// </summary>
        private void KeepaliveMessage()
        {
            if(this.Host.Server.IsActive )
                return;	// no need if server is local
            if(this.cls.demoplayback )
                return;

            // read messages from server, should just be nops
            this.Host.Network.Message.SaveState( ref this._MsgState );

            int ret;
            do
            {
                ret = this.GetMessage();
                switch( ret )
                {
                    default:
                        this.Host.Error( "CL_KeepaliveMessage: CL_GetMessage failed" );
                        break;

                    case 0:
                        break;  // nothing waiting

                    case 1:
                        this.Host.Error( "CL_KeepaliveMessage: received a message" );
                        break;

                    case 2:
                        if(this.Host.Network.Reader.ReadByte() != ProtocolDef.svc_nop )
                            this.Host.Error( "CL_KeepaliveMessage: datagram wasn't a nop" );
                        break;
                }
            } while( ret != 0 );

            this.Host.Network.Message.RestoreState(this._MsgState );

            // check time
            var time = ( float ) Timer.GetFloatTime();
            if( time - this._LastMsg < 5 )
                return;

            this._LastMsg = time;

            // write out a nop
            this.Host.Console.Print( "--> client to server keepalive\n" );

            this.cls.message.WriteByte( ProtocolDef.clc_nop );
            this.Host.Network.SendMessage(this.cls.netcon, this.cls.message );
            this.cls.message.Clear();
        }
    }
}
