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

namespace SharpQuake.Networking.Client
{
    using Desktop;
    using Engine.Host;
    using Framework.Definitions;
    using Framework.IO;
    using Framework.Mathematics;
    using Framework.Networking.Client;
    using Framework.Rendering;
    using Framework.World;
    using Game.Rendering;
    using System;
    using System.Numerics;

    partial class client
    {
        // Instance
        public Host Host
        {
            get;
            private set;
        }

        // CL_Init
        public void Initialise( )
        {
            this.InitInput(this.Host );
            this.InitTempEntities();

            if(this.Host.Cvars.Name == null )
            {
                this.Host.Cvars.Name = this.Host.CVars.Add( "_cl_name", "player", ClientVariableFlags.Archive );
                this.Host.Cvars.Color = this.Host.CVars.Add( "_cl_color", 0f, ClientVariableFlags.Archive );
                this.Host.Cvars.ShowNet = this.Host.CVars.Add( "cl_shownet", 0 );	// can be 0, 1, or 2
                this.Host.Cvars.NoLerp = this.Host.CVars.Add( "cl_nolerp", false );
                this.Host.Cvars.LookSpring = this.Host.CVars.Add( "lookspring", false, ClientVariableFlags.Archive );
                this.Host.Cvars.LookStrafe = this.Host.CVars.Add( "lookstrafe", false, ClientVariableFlags.Archive );
                this.Host.Cvars.Sensitivity = this.Host.CVars.Add( "sensitivity", 3f, ClientVariableFlags.Archive );
                this.Host.Cvars.MPitch = this.Host.CVars.Add( "m_pitch", 0.022f, ClientVariableFlags.Archive );
                this.Host.Cvars.MYaw = this.Host.CVars.Add( "m_yaw", 0.022f, ClientVariableFlags.Archive );
                this.Host.Cvars.MForward = this.Host.CVars.Add( "m_forward", 1f, ClientVariableFlags.Archive );
                this.Host.Cvars.MSide = this.Host.CVars.Add( "m_side", 0.8f, ClientVariableFlags.Archive );
                this.Host.Cvars.UpSpeed = this.Host.CVars.Add( "cl_upspeed", 200f );
                this.Host.Cvars.ForwardSpeed = this.Host.CVars.Add( "cl_forwardspeed", 200f, ClientVariableFlags.Archive );
                this.Host.Cvars.BackSpeed = this.Host.CVars.Add( "cl_backspeed", 200f, ClientVariableFlags.Archive );
                this.Host.Cvars.SideSpeed = this.Host.CVars.Add( "cl_sidespeed", 350f );
                this.Host.Cvars.MoveSpeedKey = this.Host.CVars.Add( "cl_movespeedkey", 2.0f );
                this.Host.Cvars.YawSpeed = this.Host.CVars.Add( "cl_yawspeed", 140f );
                this.Host.Cvars.PitchSpeed = this.Host.CVars.Add( "cl_pitchspeed", 150f );
                this.Host.Cvars.AngleSpeedKey = this.Host.CVars.Add( "cl_anglespeedkey", 1.5f );
                this.Host.Cvars.AnimationBlend = this.Host.CVars.Add( "cl_animationblend", false );
			}

            for( var i = 0; i < this._EFrags.Length; i++ )
                this._EFrags[i] = new();

            for( var i = 0; i < this._Entities.Length; i++ )
                this._Entities[i] = new();

            for( var i = 0; i < this._StaticEntities.Length; i++ )
                this._StaticEntities[i] = new();

            for( var i = 0; i < this._DLights.Length; i++ )
                this._DLights[i] = new();

            //
            // register our commands
            //
            this.Host.Commands.Add( "cmd", this.ForwardToServer_f );
            this.Host.Commands.Add( "entities", this.PrintEntities_f );
            this.Host.Commands.Add( "disconnect", this.Disconnect_f );
            this.Host.Commands.Add( "record", this.Record_f );
            this.Host.Commands.Add( "stop", this.Stop_f );
            this.Host.Commands.Add( "playdemo", this.PlayDemo_f );
            this.Host.Commands.Add( "timedemo", this.TimeDemo_f );
        }

        // void	Cmd_ForwardToServer (void);
        // adds the current command line as a clc_stringcmd to the client message.
        // things like godmode, noclip, etc, are commands directed to the server,
        // so when they are typed in at the console, they will need to be forwarded.
        //
        // Sends the entire command line over to the server
        public void ForwardToServer_f( CommandMessage msg )
        {
            if (this.Host.Client.cls.state != cactive_t.ca_connected )
            {
                this.Host.Console.Print( $"Can't \"{msg.Name}\", not connected\n" );
                return;
            }

            if (this.Host.Client.cls.demoplayback )
                return;		// not really connected

            var writer = this.Host.Client.cls.message;
            writer.WriteByte( ProtocolDef.clc_stringcmd );
            if ( !msg.Name.Equals( "cmd" ) )
                writer.Print( msg.Name + " " );

            if ( msg.HasParameters )
                writer.Print( msg.StringParameters );
            else
                writer.Print( "\n" );
        }

        /// <summary>
        /// CL_EstablishConnection
        /// </summary>
        public void EstablishConnection( string host )
        {
            if(this.cls.state == cactive_t.ca_dedicated )
                return;

            if(this.cls.demoplayback )
                return;

            this.Disconnect();

            this.cls.netcon = this.Host.Network.Connect( host );
            if(this.cls.netcon == null )
                this.Host.Error( "CL_Connect: connect failed\n" );

            this.Host.Console.DPrint( "CL_EstablishConnection: connected to {0}\n", host );

            this.cls.demonum = -1;			// not in the demo loop now
            this.cls.state = cactive_t.ca_connected;
            this.cls.signon = 0;				// need all the signon messages before playing
        }

        /// <summary>
        /// CL_NextDemo
        ///
        /// Called to play the next demo in the demo loop
        /// </summary>
        public void NextDemo()
        {
            if(this.cls.demonum == -1 )
                return;		// don't play demos

            this.Host.Screen.BeginLoadingPlaque();

            if( string.IsNullOrEmpty(this.cls.demos[this.cls.demonum] ) || this.cls.demonum == ClientDef.MAX_DEMOS )
            {
                this.cls.demonum = 0;
                if( string.IsNullOrEmpty(this.cls.demos[this.cls.demonum] ) )
                {
                    this.Host.Console.Print( "No demos listed with startdemos\n" );
                    this.cls.demonum = -1;
                    return;
                }
            }

            this.Host.Commands.Buffer.Insert( string.Format( "playdemo {0}\n", this.cls.demos[this.cls.demonum] ) );
            this.cls.demonum++;
        }

        /// <summary>
        /// CL_AllocDlight
        /// </summary>
        public dlight_t AllocDlight( int key )
        {
            dlight_t dl;

            // first look for an exact key match
            if( key != 0 )
            {
                for( var i = 0; i < ClientDef.MAX_DLIGHTS; i++ )
                {
                    dl = this._DLights[i];
                    if( dl.key == key )
                    {
                        dl.Clear();
                        dl.key = key;
                        return dl;
                    }
                }
            }

            // then look for anything else
            //dl = cl_dlights;
            for( var i = 0; i < ClientDef.MAX_DLIGHTS; i++ )
            {
                dl = this._DLights[i];
                if( dl.die < this.cl.time )
                {
                    dl.Clear();
                    dl.key = key;
                    return dl;
                }
            }

            dl = this._DLights[0];
            dl.Clear();
            dl.key = key;
            return dl;
        }

        /// <summary>
        /// CL_DecayLights
        /// </summary>
        public void DecayLights()
        {
            var time = ( float ) (this.cl.time - this.cl.oldtime );

            for( var i = 0; i < ClientDef.MAX_DLIGHTS; i++ )
            {
                var dl = this._DLights[i];
                if( dl.die < this.cl.time || dl.radius == 0 )
                    continue;

                dl.radius -= time * dl.decay;
                if( dl.radius < 0 )
                    dl.radius = 0;
            }
        }

        // CL_Disconnect_f
        public void Disconnect_f( CommandMessage msg )
        {
            this.Disconnect();
            if(this.Host.Server.IsActive )
                this.Host.ShutdownServer( false );
        }

        // CL_SendCmd
        public void SendCmd()
        {
            if(this.cls.state != cactive_t.ca_connected )
                return;

            if(this.cls.signon == ClientDef.SIGNONS )
            {
                var cmd = new usercmd_t();

                // get basic movement from keyboard
                this.BaseMove( ref cmd );

                // allow mice or other external controllers to add to the move
                MainWindow.Input.Move( cmd );

                // send the unreliable message
                this.Host.Client.SendMove( ref cmd );
            }

            if(this.cls.demoplayback )
            {
                this.cls.message.Clear();//    SZ_Clear (cls.message);
                return;
            }

            // send the reliable message
            if(this.cls.message.IsEmpty )
                return;		// no message at all

            if( !this.Host.Network.CanSendMessage(this.cls.netcon ) )
            {
                this.Host.Console.DPrint( "CL_WriteToServer: can't send\n" );
                return;
            }

            if(this.Host.Network.SendMessage(this.cls.netcon, this.cls.message ) == -1 )
                this.Host.Error( "CL_WriteToServer: lost server connection" );

            this.cls.message.Clear();
        }

        // CL_ReadFromServer
        //
        // Read all incoming data from the server
        public int ReadFromServer()
        {
            this.cl.oldtime = this.cl.time;
            this.cl.time += this.Host.FrameTime;

            int ret;
            do
            {
                ret = this.GetMessage();
                if( ret == -1 )
                    this.Host.Error( "CL_ReadFromServer: lost server connection" );
                if( ret == 0 )
                    break;

                this.cl.last_received_message = ( float )this.Host.RealTime;
                this.ParseServerMessage();
            } while( ret != 0 && this.cls.state == cactive_t.ca_connected );

            if(this.Host.Cvars.ShowNet.Get<int>( ) != 0 )
                this.Host.Console.Print( "\n" );

            //
            // bring the links up to date
            //
            this.RelinkEntities();
            this.UpdateTempEntities();

            return 0;
        }

        /// <summary>
        /// CL_Disconnect
        ///
        /// Sends a disconnect message to the server
        /// This is also called on Host_Error, so it shouldn't cause any errors
        /// </summary>
        public void Disconnect()
        {
            // stop sounds (especially looping!)
            this.Host.Sound.StopAllSounds( true );

            // bring the console down and fade the colors back to normal
            //	SCR_BringDownConsole ();

            // if running a local server, shut it down
            if(this.cls.demoplayback )
                this.StopPlayback();
            else if(this.cls.state == cactive_t.ca_connected )
            {
                if(this.cls.demorecording )
                    this.Stop_f( null );

                this.Host.Console.DPrint( "Sending clc_disconnect\n" );
                this.cls.message.Clear();
                this.cls.message.WriteByte( ProtocolDef.clc_disconnect );
                this.Host.Network.SendUnreliableMessage(this.cls.netcon, this.cls.message );
                this.cls.message.Clear();
                this.Host.Network.Close(this.cls.netcon );

                this.cls.state = cactive_t.ca_disconnected;
                if(this.Host.Server.sv.active )
                    this.Host.ShutdownServer( false );
            }

            this.cls.demoplayback = this.cls.timedemo = false;
            this.cls.signon = 0;
        }

        // CL_PrintEntities_f
        private void PrintEntities_f( CommandMessage msg )
        {
            for( var i = 0; i < this._State.num_entities; i++ )
            {
                var ent = this._Entities[i];
                this.Host.Console.Print( "{0:d3}:", i );
                if( ent.model == null )
                {
                    this.Host.Console.Print( "EMPTY\n" );
                    continue;
                }

                this.Host.Console.Print( "{0}:{1:d2}  ({2}) [{3}]\n", ent.model.Name, ent.frame, ent.origin, ent.angles );
            }
        }

        /// <summary>
        /// CL_RelinkEntities
        /// </summary>
        private void RelinkEntities()
        {
            // determine partial update time
            var frac = this.LerpPoint();

            this.NumVisEdicts = 0;

            //
            // interpolate player info
            //
            this.cl.velocity = this.cl.mvelocity[1] + frac * (this.cl.mvelocity[0] - this.cl.mvelocity[1] );

            if(this.cls.demoplayback )
            {
                // interpolate the angles
                var angleDelta = this.cl.mviewangles[0] - this.cl.mviewangles[1];
                MathLib.CorrectAngles180( ref angleDelta );
                this.cl.viewangles = this.cl.mviewangles[1] + frac * angleDelta;
            }

            var bobjrotate = MathLib.AngleMod( 100 * this.cl.time );

            // start on the entity after the world
            for( var i = 1; i < this.cl.num_entities; i++ )
            {
                var ent = this._Entities[i];
                if( ent.model == null )
                {
                    // empty slot
                    if( ent.forcelink )
                        this.Host.RenderContext.RemoveEfrags( ent );	// just became empty
                    continue;
                }

                // if the object wasn't included in the last packet, remove it
                if( ent.msgtime != this.cl.mtime[0] )
                {
                    ent.model = null;
                    continue;
                }

                var oldorg = ent.origin;

                if( ent.forcelink )
                {
                    // the entity was not updated in the last message
                    // so move to the final spot
                    ent.origin = ent.msg_origins[0];
                    ent.angles = ent.msg_angles[0];
                }
                else
                {
                    // if the delta is large, assume a teleport and don't lerp
                    var f = frac;
                    var delta = ent.msg_origins[0] - ent.msg_origins[1];
                    if( Math.Abs( delta.X ) > 100 || Math.Abs( delta.Y ) > 100 || Math.Abs( delta.Z ) > 100 )
                        f = 1; // assume a teleportation, not a motion

                    // interpolate the origin and angles
                    ent.origin = ent.msg_origins[1] + f * delta;
                    var angleDelta = ent.msg_angles[0] - ent.msg_angles[1];
                    MathLib.CorrectAngles180( ref angleDelta );
                    ent.angles = ent.msg_angles[1] + f * angleDelta;
                }

                // rotate binary objects locally
                if( ent.model.Flags.HasFlag( EntityFlags.Rotate ) )
                    ent.angles.Y = bobjrotate;

                if( ( ent.effects & EntityEffects.EF_BRIGHTFIELD ) != 0 )
                    this.Host.RenderContext.Particles.EntityParticles(this.Host.Client.cl.time, ent.origin );

                if( ( ent.effects & EntityEffects.EF_MUZZLEFLASH ) != 0 )
                {
                    var dl = this.AllocDlight( i );
                    dl.origin = ent.origin;
                    dl.origin.Z += 16;
                    Vector3 fv, rv, uv;
                    MathLib.AngleVectors( ref ent.angles, out fv, out rv, out uv );
                    dl.origin += fv * 18;
                    dl.radius = 200 + ( MathLib.Random() & 31 );
                    dl.minlight = 32;
                    dl.die = ( float )this.cl.time + 0.1f;
                }
                if( ( ent.effects & EntityEffects.EF_BRIGHTLIGHT ) != 0 )
                {
                    var dl = this.AllocDlight( i );
                    dl.origin = ent.origin;
                    dl.origin.Z += 16;
                    dl.radius = 400 + ( MathLib.Random() & 31 );
                    dl.die = ( float )this.cl.time + 0.001f;
                }
                if( ( ent.effects & EntityEffects.EF_DIMLIGHT ) != 0 )
                {
                    var dl = this.AllocDlight( i );
                    dl.origin = ent.origin;
                    dl.radius = 200 + ( MathLib.Random() & 31 );
                    dl.die = ( float )this.cl.time + 0.001f;
                }

                if ( ent.model.Flags.HasFlag( EntityFlags.Gib ))
                    this.Host.RenderContext.Particles.RocketTrail(this.Host.Client.cl.time, ref oldorg, ref ent.origin, 2 );
                else if ( ent.model.Flags.HasFlag( EntityFlags.ZomGib ) )
                    this.Host.RenderContext.Particles.RocketTrail(this.Host.Client.cl.time, ref oldorg, ref ent.origin, 4 );
                else if ( ent.model.Flags.HasFlag( EntityFlags.Tracer ) )
                    this.Host.RenderContext.Particles.RocketTrail(this.Host.Client.cl.time, ref oldorg, ref ent.origin, 3 );
                else if ( ent.model.Flags.HasFlag( EntityFlags.Tracer2 ) )
                    this.Host.RenderContext.Particles.RocketTrail(this.Host.Client.cl.time, ref oldorg, ref ent.origin, 5 );
                else if ( ent.model.Flags.HasFlag( EntityFlags.Rocket ) )
                {
                    this.Host.RenderContext.Particles.RocketTrail(this.Host.Client.cl.time, ref oldorg, ref ent.origin, 0 );
                    var dl = this.AllocDlight( i );
                    dl.origin = ent.origin;
                    dl.radius = 200;
                    dl.die = ( float )this.cl.time + 0.01f;
                }
                else if ( ent.model.Flags.HasFlag( EntityFlags.Grenade ) )
                    this.Host.RenderContext.Particles.RocketTrail(this.Host.Client.cl.time, ref oldorg, ref ent.origin, 1 );
                else if ( ent.model.Flags.HasFlag( EntityFlags.Tracer3 ) )
                    this.Host.RenderContext.Particles.RocketTrail(this.Host.Client.cl.time, ref oldorg, ref ent.origin, 6 );

                ent.forcelink = false;

                if( i == this.cl.viewentity && !this.Host.ChaseView.IsActive )
                    continue;

                if(this.NumVisEdicts < ClientDef.MAX_VISEDICTS )
                {
                    this._VisEdicts[this.NumVisEdicts] = ent;
                    this.NumVisEdicts++;
                }
            }
        }

        /// <summary>
        /// CL_SignonReply
        ///
        /// An svc_signonnum has been received, perform a client side setup
        /// </summary>
        private void SignonReply()
        {
            this.Host.Console.DPrint( "CL_SignonReply: {0}\n", this.cls.signon );

            switch(this.cls.signon )
            {
                case 1:
                    this.cls.message.WriteByte( ProtocolDef.clc_stringcmd );
                    this.cls.message.WriteString( "prespawn" );
                    break;

                case 2:
                    this.cls.message.WriteByte( ProtocolDef.clc_stringcmd );
                    this.cls.message.WriteString( string.Format( "name \"{0}\"\n", this.Host.Cvars.Name.Get<string>( ) ) );

                    this.cls.message.WriteByte( ProtocolDef.clc_stringcmd );
                    this.cls.message.WriteString( string.Format( "color {0} {1}\n", ( int )this.Host.Cvars.Color.Get<float>( ) >> 4, ( int )this.Host.Cvars.Color.Get<float>( ) & 15 ) );

                    this.cls.message.WriteByte( ProtocolDef.clc_stringcmd );
                    this.cls.message.WriteString( "spawn " + this.cls.spawnparms );
                    break;

                case 3:
                    this.cls.message.WriteByte( ProtocolDef.clc_stringcmd );
                    this.cls.message.WriteString( "begin" );
                    this.Host.Cache.Report();	// print remaining memory
                    break;

                case 4:
                    this.Host.Screen.EndLoadingPlaque();		// allow normal screen updates
                    break;
            }
        }

        /// <summary>  
        /// CL_ClearState
        /// </summary>
        private void ClearState()
        {
            if( !this.Host.Server.sv.active )
                this.Host.ClearMemory();

            // wipe the entire cl structure
            this._State.Clear();

            this.cls.message.Clear();

            // clear other arrays
            foreach( var ef in this._EFrags )
                ef.Clear();
            foreach( var et in this._Entities )
                et.Clear();

            foreach( var dl in this._DLights )
                dl.Clear();

            Array.Clear(this._LightStyle, 0, this._LightStyle.Length );

            foreach( var et in this._TempEntities )
                et.Clear();

            foreach( var b in this._Beams )
                b.Clear();

            //
            // allocate the efrags and chain together into a free list
            //
            this.cl.free_efrags = this._EFrags[0];// cl_efrags;
            for( var i = 0; i < ClientDef.MAX_EFRAGS - 1; i++ )
                this._EFrags[i].entnext = this._EFrags[i + 1];

            this._EFrags[ClientDef.MAX_EFRAGS - 1].entnext = null;
        }

        /// <summary>
        /// CL_LerpPoint
        /// Determines the fraction between the last two messages that the objects
        /// should be put at.
        /// </summary>
        private float LerpPoint()
        {
            var f = this.cl.mtime[0] - this.cl.mtime[1];
            if( f == 0 || this.Host.Cvars.NoLerp.Get<bool>( ) || this.cls.timedemo || this.Host.Server.IsActive )
            {
                this.cl.time = this.cl.mtime[0];
                return 1;
            }

            if( f > 0.1 )
            {	// dropped packet, or start of demo
                this.cl.mtime[1] = this.cl.mtime[0] - 0.1;
                f = 0.1;
            }
            var frac = (this.cl.time - this.cl.mtime[1] ) / f;
            if( frac < 0 )
            {
                if( frac < -0.01 )
                    this.cl.time = this.cl.mtime[1];

                frac = 0;
            }
            else if( frac > 1 )
            {
                if( frac > 1.01 )
                    this.cl.time = this.cl.mtime[0];

                frac = 1;
            }
            return ( float ) frac;
        }
    }
}
