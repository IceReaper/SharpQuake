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

namespace SharpQuake.Networking.Server
{
    using Framework.Definitions;
    using Framework.Engine;
    using Framework.IO;
    using Framework.IO.Input;
    using Framework.Mathematics;
    using Framework.Networking.Client;
    using System;
    using System.Linq;
    using System.Numerics;

    partial class server
    {
        public MemoryEdict Player => this._Player;

        private const int MAX_FORWARD = 6;

        private MemoryEdict _Player; // sv_player
        private bool _OnGround; // onground

        // world
        //static v3f angles - this must be a reference to _Player.v.angles
        //static v3f origin  - this must be a reference to _Player.v.origin
        //static Vector3 velocity - this must be a reference to _Player.v.velocity

        private usercmd_t _Cmd; // cmd

        private Vector3 _Forward; // forward
        private Vector3 _Right; // right
        private Vector3 _Up; // up

        private Vector3 _WishDir; // wishdir
        private float _WishSpeed; // wishspeed

        private string[] ClientMessageCommands = new string[]
        {
            "status",
            "god",
            "notarget",
            "fly",
            "name",
            "noclip",
            "say",
            "say_team",
            "tell",
            "color",
            "kill",
            "pause",
            "spawn",
            "begin",
            "prespawn",
            "kick",
            "ping",
            "give",
            "ban"
        };

        /// <summary>
        /// SV_RunClients
        /// </summary>
        public void RunClients()
        {
            for( var i = 0; i < this.svs.maxclients; i++ )
            {
                this.Host.HostClient = this.svs.clients[i];
                if( !this.Host.HostClient.active )
                    continue;

                this._Player = this.Host.HostClient.edict;

                if( !this.ReadClientMessage() )
                {
                    this.DropClient( false );	// client misbehaved...
                    continue;
                }

                if( !this.Host.HostClient.spawned )
                {
                    // clear client movement until a new packet is received
                    this.Host.HostClient.cmd.Clear();
                    continue;
                }

                // always pause in single player if in console or menus
                if( !this.sv.paused && (this.svs.maxclients > 1 || this.Host.Keyboard.Destination == KeyDestination.key_game ) )
                    this.ClientThink();
            }
        }

        /// <summary>
        /// SV_SetIdealPitch
        /// </summary>
        public void SetIdealPitch()
        {
            if( ( ( int )this._Player.v.flags & EdictFlags.FL_ONGROUND ) == 0 )
                return;

            var angleval = this._Player.v.angles.Y * Math.PI * 2 / 360;
            var sinval = Math.Sin( angleval );
            var cosval = Math.Cos( angleval );
            var z = new float[server.MAX_FORWARD];
            for( var i = 0; i < server.MAX_FORWARD; i++ )
            {
                var top = this._Player.v.origin;
                top.X += ( float ) ( cosval * ( i + 3 ) * 12 );
                top.Y += ( float ) ( sinval * ( i + 3 ) * 12 );
                top.Z += this._Player.v.view_ofs.Z;

                var bottom = top;
                bottom.Z -= 160;

                var tr = this.Move( ref top, ref Utilities.ZeroVector3f, ref Utilities.ZeroVector3f, ref bottom, 1, this._Player );
                if( tr.allsolid )
                    return;	// looking at a wall, leave ideal the way is was

                if( tr.fraction == 1 )
                    return;	// near a dropoff

                z[i] = top.Z + tr.fraction * ( bottom.Z - top.Z );
            }

            float dir = 0; // Uze: int in original code???
            var steps = 0;
            for( var j = 1; j < server.MAX_FORWARD; j++ )
            {
                var step = z[j] - z[j - 1]; // Uze: int in original code???
                if( step > -QDef.ON_EPSILON && step < QDef.ON_EPSILON ) // Uze: comparing int with ON_EPSILON (0.1)???
                    continue;

                if( dir != 0 && ( step - dir > QDef.ON_EPSILON || step - dir < -QDef.ON_EPSILON ) )
                    return;		// mixed changes

                steps++;
                dir = step;
            }

            if( dir == 0 )
            {
                this._Player.v.idealpitch = 0;
                return;
            }

            if( steps < 2 )
                return;

            this._Player.v.idealpitch = -dir * this.Host.Cvars.IdealPitchScale.Get<float>( );
        }

        private int GetClientMessageCommand( string s )
        {
            int ret;

            if (this.Host.HostClient.privileged )
                ret = 2;
            else
                ret = 0;

            var cmdName = s.Split( ' ' )[0];

            if (this.ClientMessageCommands.Contains( cmdName ) )
                ret = 1;           

            return ret;
        }

        /// <summary>
        /// SV_ReadClientMessage
        /// Returns false if the client should be killed
        /// </summary>
        private bool ReadClientMessage()
        {
            while( true )
            {
                var ret = this.Host.Network.GetMessage(this.Host.HostClient.netconnection );
                if( ret == -1 )
                {
                    this.Host.Console.DPrint( "SV_ReadClientMessage: NET_GetMessage failed\n" );
                    return false;
                }
                if( ret == 0 )
                    return true;

                this.Host.Network.Reader.Reset();

                var flag = true;
                while( flag )
                {
                    if( !this.Host.HostClient.active )
                        return false;	// a command caused an error

                    if(this.Host.Network.Reader.IsBadRead )
                    {
                        this.Host.Console.DPrint( "SV_ReadClientMessage: badread\n" );
                        return false;
                    }

                    var cmd = this.Host.Network.Reader.ReadChar();
                    switch( cmd )
                    {
                        case -1:
                            flag = false; // end of message
                            ret = 1;
                            break;

                        case ProtocolDef.clc_nop:
                            break;

                        case ProtocolDef.clc_stringcmd:
                            var s = this.Host.Network.Reader.ReadString();
                            ret = this.GetClientMessageCommand( s );
                            if( ret == 2 )
                                this.Host.Commands.Buffer.Insert( s );
                            else if( ret == 1 )
                                this.Host.Commands.ExecuteString( s, CommandSource.Client );
                            else
                                this.Host.Console.DPrint( "{0} tried to {1}\n", this.Host.HostClient.name, s );
                            break;

                        case ProtocolDef.clc_disconnect:
                            return false;

                        case ProtocolDef.clc_move:
                            this.ReadClientMove( ref this.Host.HostClient.cmd );
                            break;

                        default:
                            this.Host.Console.DPrint( "SV_ReadClientMessage: unknown command char\n" );
                            return false;
                    }
                }

                if( ret != 1 )
                    break;
            }

            return true;
        }

        /// <summary>
        /// SV_ReadClientMove
        /// </summary>
        private void ReadClientMove( ref usercmd_t move )
        {
            var client = this.Host.HostClient;

            // read ping time
            client.ping_times[client.num_pings % ServerDef.NUM_PING_TIMES] = ( float ) (this.sv.time - this.Host.Network.Reader.ReadFloat() );
            client.num_pings++;

            // read current angles
            var angles = this.Host.Network.Reader.ReadAngles();
            MathLib.Copy( ref angles, out client.edict.v.v_angle );

            // read movement
            move.forwardmove = this.Host.Network.Reader.ReadShort();
            move.sidemove = this.Host.Network.Reader.ReadShort();
            move.upmove = this.Host.Network.Reader.ReadShort();

            // read buttons
            var bits = this.Host.Network.Reader.ReadByte();
            client.edict.v.button0 = bits & 1;
            client.edict.v.button2 = ( bits & 2 ) >> 1;

            var i = this.Host.Network.Reader.ReadByte();
            if( i != 0 )
                client.edict.v.impulse = i;
        }

        /// <summary>
        /// SV_ClientThink
        /// the move fields specify an intended velocity in pix/sec
        /// the angle fields specify an exact angular motion in degrees
        /// </summary>
        private void ClientThink()
        {
            if(this._Player.v.movetype == Movetypes.MOVETYPE_NONE )
                return;

            this._OnGround = ( ( int )this._Player.v.flags & EdictFlags.FL_ONGROUND ) != 0;

            this.DropPunchAngle();

            //
            // if dead, behave differently
            //
            if(this._Player.v.health <= 0 )
                return;

            //
            // angles
            // show 1/3 the pitch angle and all the roll angle
            this._Cmd = this.Host.HostClient.cmd;

            Vector3 v_angle;
            MathLib.VectorAdd( ref this._Player.v.v_angle, ref this._Player.v.punchangle, out v_angle );
            var pang = Utilities.ToVector( ref this._Player.v.angles );
            var pvel = Utilities.ToVector( ref this._Player.v.velocity );
            this._Player.v.angles.Z = this.Host.View.CalcRoll( ref pang, ref pvel ) * 4;
            if(this._Player.v.fixangle == 0 )
            {
                this._Player.v.angles.X = -v_angle.X / 3;
                this._Player.v.angles.Y = v_angle.Y;
            }

            if( ( ( int )this._Player.v.flags & EdictFlags.FL_WATERJUMP ) != 0 )
            {
                this.WaterJump();
                return;
            }
            //
            // walk
            //
            if( this._Player.v.waterlevel >= 2 && this._Player.v.movetype != Movetypes.MOVETYPE_NOCLIP )
            {
                this.WaterMove();
                return;
            }

            this.AirMove();
        }

        private void DropPunchAngle()
        {
            var v = Utilities.ToVector( ref this._Player.v.punchangle );
            var len = MathLib.Normalize( ref v ) - 10 * this.Host.FrameTime;
            if( len < 0 )
                len = 0;
            v *= ( float ) len;
            MathLib.Copy( ref v, out this._Player.v.punchangle );
        }

        /// <summary>
        /// SV_WaterJump
        /// </summary>
        private void WaterJump()
        {
            if(this.sv.time > this._Player.v.teleport_time || this._Player.v.waterlevel == 0 )
            {
                this._Player.v.flags = ( int )this._Player.v.flags & ~EdictFlags.FL_WATERJUMP;
                this._Player.v.teleport_time = 0;
            }

            this._Player.v.velocity.X = this._Player.v.movedir.X;
            this._Player.v.velocity.Y = this._Player.v.movedir.Y;
        }

        /// <summary>
        /// SV_WaterMove
        /// </summary>
        private void WaterMove()
        {
            //
            // user intentions
            //
            var pangle = Utilities.ToVector( ref this._Player.v.v_angle );
            MathLib.AngleVectors( ref pangle, out this._Forward, out this._Right, out this._Up );
            var wishvel = this._Forward * this._Cmd.forwardmove + this._Right * this._Cmd.sidemove;

            if(this._Cmd.forwardmove == 0 && this._Cmd.sidemove == 0 && this._Cmd.upmove == 0 )
                wishvel.Z -= 60;		// drift towards bottom
            else
                wishvel.Z += this._Cmd.upmove;

            var wishspeed = wishvel.Length();
            var maxSpeed = this.Host.Cvars.MaxSpeed.Get<float>();
            if ( wishspeed > maxSpeed )
            {
                wishvel *= maxSpeed / wishspeed;
                wishspeed = maxSpeed;
            }
            wishspeed *= 0.7f;

            //
            // water friction
            //
            float newspeed, speed = MathLib.Length( ref this._Player.v.velocity );
            if( speed != 0 )
            {
                newspeed = ( float ) ( speed - this.Host.FrameTime * speed * this.Host.Cvars.Friction.Get<float>( ) );
                if( newspeed < 0 )
                    newspeed = 0;
                MathLib.VectorScale( ref this._Player.v.velocity, newspeed / speed, out this._Player.v.velocity );
            }
            else
                newspeed = 0;

            //
            // water acceleration
            //
            if( wishspeed == 0 )
                return;

            var addspeed = wishspeed - newspeed;
            if( addspeed <= 0 )
                return;

            MathLib.Normalize( ref wishvel );
            var accelspeed = ( float ) (this.Host.Cvars.Accelerate.Get<float>( ) * wishspeed * this.Host.FrameTime );
            if( accelspeed > addspeed )
                accelspeed = addspeed;

            wishvel *= accelspeed;
            this._Player.v.velocity.X += wishvel.X;
            this._Player.v.velocity.Y += wishvel.Y;
            this._Player.v.velocity.Z += wishvel.Z;
        }

        /// <summary>
        /// SV_AirMove
        /// </summary>
        private void AirMove()
        {
            var pangles = Utilities.ToVector( ref this._Player.v.angles );
            MathLib.AngleVectors( ref pangles, out this._Forward, out this._Right, out this._Up );

            var fmove = this._Cmd.forwardmove;
            var smove = this._Cmd.sidemove;

            // hack to not let you back into teleporter
            if(this.sv.time < this._Player.v.teleport_time && fmove < 0 )
                fmove = 0;

            var wishvel = this._Forward * fmove + this._Right * smove;

            if( ( int )this._Player.v.movetype != Movetypes.MOVETYPE_WALK )
                wishvel.Z = this._Cmd.upmove;
            else
                wishvel.Z = 0;

            this._WishDir = wishvel;
            this._WishSpeed = MathLib.Normalize( ref this._WishDir );
            var maxSpeed = this.Host.Cvars.MaxSpeed.Get<float>();
            if (this._WishSpeed > maxSpeed )
            {
                wishvel *= maxSpeed / this._WishSpeed;
                this._WishSpeed = maxSpeed;
            }

            if(this._Player.v.movetype == Movetypes.MOVETYPE_NOCLIP )
            {
                // noclip
                MathLib.Copy( ref wishvel, out this._Player.v.velocity );
            }
            else if(this._OnGround )
            {
                this.UserFriction();
                this.Accelerate();
            }
            else
            {	// not on ground, so little effect on velocity
                this.AirAccelerate( wishvel );
            }
        }

        /// <summary>
        /// SV_UserFriction
        /// </summary>
        private void UserFriction()
        {
            var speed = MathLib.LengthXY( ref this._Player.v.velocity );
            if( speed == 0 )
                return;

            // if the leading edge is over a dropoff, increase friction
            Vector3 start, stop;
            start.X = stop.X = this._Player.v.origin.X + this._Player.v.velocity.X / speed * 16;
            start.Y = stop.Y = this._Player.v.origin.Y + this._Player.v.velocity.Y / speed * 16;
            start.Z = this._Player.v.origin.Z + this._Player.v.mins.Z;
            stop.Z = start.Z - 34;

            var trace = this.Move( ref start, ref Utilities.ZeroVector, ref Utilities.ZeroVector, ref stop, 1, this._Player );
            var friction = this.Host.Cvars.Friction.Get<float>( );
            if( trace.fraction == 1.0 )
                friction *= this.Host.Cvars.EdgeFriction.Get<float>( );

            // apply friction
            var control = speed < this.Host.Cvars.StopSpeed.Get<float>( ) ? this.Host.Cvars.StopSpeed.Get<float>( ) : speed;
            var newspeed = ( float ) ( speed - this.Host.FrameTime * control * friction );

            if( newspeed < 0 )
                newspeed = 0;
            newspeed /= speed;

            MathLib.VectorScale( ref this._Player.v.velocity, newspeed, out this._Player.v.velocity );
        }

        /// <summary>
        /// SV_Accelerate
        /// </summary>
        private void Accelerate()
        {
            var currentspeed = Vector3.Dot( Utilities.ToVector( ref this._Player.v.velocity ), this._WishDir );
            var addspeed = this._WishSpeed - currentspeed;
            if( addspeed <= 0 )
                return;

            var accelspeed = ( float ) (this.Host.Cvars.Accelerate.Get<float>( ) * this.Host.FrameTime * this._WishSpeed );
            if( accelspeed > addspeed )
                accelspeed = addspeed;

            this._Player.v.velocity.X += this._WishDir.X * accelspeed;
            this._Player.v.velocity.Y += this._WishDir.Y * accelspeed;
            this._Player.v.velocity.Z += this._WishDir.Z * accelspeed;
        }

        /// <summary>
        /// SV_AirAccelerate
        /// </summary>
        private void AirAccelerate( Vector3 wishveloc )
        {
            var wishspd = MathLib.Normalize( ref wishveloc );
            if( wishspd > 30 )
                wishspd = 30;
            var currentspeed = Vector3.Dot( Utilities.ToVector( ref this._Player.v.velocity ), wishveloc );
            var addspeed = wishspd - currentspeed;
            if( addspeed <= 0 )
                return;
            var accelspeed = ( float ) (this.Host.Cvars.Accelerate.Get<float>( ) * this._WishSpeed * this.Host.FrameTime );
            if( accelspeed > addspeed )
                accelspeed = addspeed;

            wishveloc *= accelspeed;
            this._Player.v.velocity.X += wishveloc.X;
            this._Player.v.velocity.Y += wishveloc.Y;
            this._Player.v.velocity.Z += wishveloc.Z;
        }
    }
}
