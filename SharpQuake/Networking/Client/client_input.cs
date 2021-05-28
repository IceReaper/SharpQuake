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



// cl_input.c

namespace SharpQuake.Networking.Client
{
    using Engine.Host;
    using Framework.Definitions;
    using Framework.Engine;
    using Framework.IO;
    using Framework.Mathematics;
    using Framework.Networking.Client;
    using System;

    internal static class client_input
    {
        // kbutton_t in_xxx
        public static kbutton_t MLookBtn;

        public static kbutton_t KLookBtn;
        public static kbutton_t LeftBtn;
        public static kbutton_t RightBtn;
        public static kbutton_t ForwardBtn;
        public static kbutton_t BackBtn;
        public static kbutton_t LookUpBtn;
        public static kbutton_t LookDownBtn;
        public static kbutton_t MoveLeftBtn;
        public static kbutton_t MoveRightBtn;
        public static kbutton_t StrafeBtn;
        public static kbutton_t SpeedBtn;
        public static kbutton_t UseBtn;
        public static kbutton_t JumpBtn;
        public static kbutton_t AttackBtn;
        public static kbutton_t UpBtn;
        public static kbutton_t DownBtn;

        public static int Impulse;

        public static Host Host
        {
            get;
            private set;
        }

        public static void Init( Host host )
        {
            client_input.Host = host;

            client_input.Host.Commands.Add( "+moveup", client_input.UpDown );
            client_input.Host.Commands.Add( "-moveup", client_input.UpUp );
            client_input.Host.Commands.Add( "+movedown", client_input.DownDown );
            client_input.Host.Commands.Add( "-movedown", client_input.DownUp );
            client_input.Host.Commands.Add( "+left", client_input.LeftDown );
            client_input.Host.Commands.Add( "-left", client_input.LeftUp );
            client_input.Host.Commands.Add( "+right", client_input.RightDown );
            client_input.Host.Commands.Add( "-right", client_input.RightUp );
            client_input.Host.Commands.Add( "+forward", client_input.ForwardDown );
            client_input.Host.Commands.Add( "-forward", client_input.ForwardUp );
            client_input.Host.Commands.Add( "+back", client_input.BackDown );
            client_input.Host.Commands.Add( "-back", client_input.BackUp );
            client_input.Host.Commands.Add( "+lookup", client_input.LookupDown );
            client_input.Host.Commands.Add( "-lookup", client_input.LookupUp );
            client_input.Host.Commands.Add( "+lookdown", client_input.LookdownDown );
            client_input.Host.Commands.Add( "-lookdown", client_input.LookdownUp );
            client_input.Host.Commands.Add( "+strafe", client_input.StrafeDown );
            client_input.Host.Commands.Add( "-strafe", client_input.StrafeUp );
            client_input.Host.Commands.Add( "+moveleft", client_input.MoveleftDown );
            client_input.Host.Commands.Add( "-moveleft", client_input.MoveleftUp );
            client_input.Host.Commands.Add( "+moveright", client_input.MoverightDown );
            client_input.Host.Commands.Add( "-moveright", client_input.MoverightUp );
            client_input.Host.Commands.Add( "+speed", client_input.SpeedDown );
            client_input.Host.Commands.Add( "-speed", client_input.SpeedUp );
            client_input.Host.Commands.Add( "+attack", client_input.AttackDown );
            client_input.Host.Commands.Add( "-attack", client_input.AttackUp );
            client_input.Host.Commands.Add( "+use", client_input.UseDown );
            client_input.Host.Commands.Add( "-use", client_input.UseUp );
            client_input.Host.Commands.Add( "+jump", client_input.JumpDown );
            client_input.Host.Commands.Add( "-jump", client_input.JumpUp );
            client_input.Host.Commands.Add( "impulse", client_input.ImpulseCmd );
            client_input.Host.Commands.Add( "+klook", client_input.KLookDown );
            client_input.Host.Commands.Add( "-klook", client_input.KLookUp );
            client_input.Host.Commands.Add( "+mlook", client_input.MLookDown );
            client_input.Host.Commands.Add( "-mlook", client_input.MLookUp );
        }

        private static void KeyDown( CommandMessage msg, ref kbutton_t b )
        {
            int k;
            if ( msg.Parameters?.Length > 0 && !string.IsNullOrEmpty( msg.Parameters[0] ) )
                k = int.Parse( msg.Parameters[0] );
            else
                k = -1;	// typed manually at the console for continuous down

            if ( k == b.down0 || k == b.down1 )
                return;		// repeating key

            if ( b.down0 == 0 )
                b.down0 = k;
            else if ( b.down1 == 0 )
                b.down1 = k;
            else
            {
                client_input.Host.Console.Print( "Three keys down for a button!\n" );
                return;
            }

            if ( ( b.state & 1 ) != 0 )
                return;	// still down
            b.state |= 1 + 2; // down + impulse down
        }

        private static void KeyUp( CommandMessage msg, ref kbutton_t b )
        {
            int k;
            if ( msg.Parameters?.Length > 0 && !string.IsNullOrEmpty( msg.Parameters[0] ) )
                k = int.Parse( msg.Parameters[0] );
            else
            {
                // typed manually at the console, assume for unsticking, so clear all
                b.down0 = b.down1 = 0;
                b.state = 4;	// impulse up
                return;
            }

            if ( b.down0 == k )
                b.down0 = 0;
            else if ( b.down1 == k )
                b.down1 = 0;
            else
                return;	// key up without coresponding down (menu pass through)

            if ( b.down0 != 0 || b.down1 != 0 )
                return;	// some other key is still holding it down

            if ( ( b.state & 1 ) == 0 )
                return;		// still up (this should not happen)
            b.state &= ~1;		// now up
            b.state |= 4; 		// impulse up
        }

        private static void KLookDown( CommandMessage msg )
        {
            client_input.KeyDown( msg, ref client_input.KLookBtn );
        }

        private static void KLookUp( CommandMessage msg )
        {
            client_input.KeyUp( msg, ref client_input.KLookBtn );
        }

        private static void MLookDown( CommandMessage msg )
        {
            client_input.KeyDown( msg, ref client_input.MLookBtn );
        }

        private static void MLookUp( CommandMessage msg )
        {
            client_input.KeyUp( msg, ref client_input.MLookBtn );

            if ( ( client_input.MLookBtn.state & 1 ) == 0 && client_input.Host.Client.LookSpring )
                client_input.Host.View.StartPitchDrift( null );
        }

        private static void UpDown( CommandMessage msg )
        {
            client_input.KeyDown( msg, ref client_input.UpBtn );
        }

        private static void UpUp( CommandMessage msg )
        {
            client_input.KeyUp( msg, ref client_input.UpBtn );
        }

        private static void DownDown( CommandMessage msg )
        {
            client_input.KeyDown( msg, ref client_input.DownBtn );
        }

        private static void DownUp( CommandMessage msg )
        {
            client_input.KeyUp( msg, ref client_input.DownBtn );
        }

        private static void LeftDown( CommandMessage msg )
        {
            client_input.KeyDown( msg, ref client_input.LeftBtn );
        }

        private static void LeftUp( CommandMessage msg )
        {
            client_input.KeyUp( msg, ref client_input.LeftBtn );
        }

        private static void RightDown( CommandMessage msg )
        {
            client_input.KeyDown( msg, ref client_input.RightBtn );
        }

        private static void RightUp( CommandMessage msg )
        {
            client_input.KeyUp( msg, ref client_input.RightBtn );
        }

        private static void ForwardDown( CommandMessage msg )
        {
            client_input.KeyDown( msg, ref client_input.ForwardBtn );
        }

        private static void ForwardUp( CommandMessage msg )
        {
            client_input.KeyUp( msg, ref client_input.ForwardBtn );
        }

        private static void BackDown( CommandMessage msg )
        {
            client_input.KeyDown( msg, ref client_input.BackBtn );
        }

        private static void BackUp( CommandMessage msg )
        {
            client_input.KeyUp( msg, ref client_input.BackBtn );
        }

        private static void LookupDown( CommandMessage msg )
        {
            client_input.KeyDown( msg, ref client_input.LookUpBtn );
        }

        private static void LookupUp( CommandMessage msg )
        {
            client_input.KeyUp( msg, ref client_input.LookUpBtn );
        }

        private static void LookdownDown( CommandMessage msg )
        {
            client_input.KeyDown( msg, ref client_input.LookDownBtn );
        }

        private static void LookdownUp( CommandMessage msg )
        {
            client_input.KeyUp( msg, ref client_input.LookDownBtn );
        }

        private static void MoveleftDown( CommandMessage msg )
        {
            client_input.KeyDown( msg, ref client_input.MoveLeftBtn );
        }

        private static void MoveleftUp( CommandMessage msg )
        {
            client_input.KeyUp( msg, ref client_input.MoveLeftBtn );
        }

        private static void MoverightDown( CommandMessage msg )
        {
            client_input.KeyDown( msg, ref client_input.MoveRightBtn );
        }

        private static void MoverightUp( CommandMessage msg )
        {
            client_input.KeyUp( msg, ref client_input.MoveRightBtn );
        }

        private static void SpeedDown( CommandMessage msg )
        {
            client_input.KeyDown( msg, ref client_input.SpeedBtn );
        }

        private static void SpeedUp( CommandMessage msg )
        {
            client_input.KeyUp( msg, ref client_input.SpeedBtn );
        }

        private static void StrafeDown( CommandMessage msg )
        {
            client_input.KeyDown( msg, ref client_input.StrafeBtn );
        }

        private static void StrafeUp( CommandMessage msg )
        {
            client_input.KeyUp( msg, ref client_input.StrafeBtn );
        }

        private static void AttackDown( CommandMessage msg )
        {
            client_input.KeyDown( msg, ref client_input.AttackBtn );
        }

        private static void AttackUp( CommandMessage msg )
        {
            client_input.KeyUp( msg, ref client_input.AttackBtn );
        }

        private static void UseDown( CommandMessage msg )
        {
            client_input.KeyDown( msg, ref client_input.UseBtn );
        }

        private static void UseUp( CommandMessage msg )
        {
            client_input.KeyUp( msg, ref client_input.UseBtn );
        }

        private static void JumpDown( CommandMessage msg )
        {
            client_input.KeyDown( msg, ref client_input.JumpBtn );
        }

        private static void JumpUp( CommandMessage msg )
        {
            client_input.KeyUp( msg, ref client_input.JumpBtn );
        }

        private static void ImpulseCmd( CommandMessage msg )
        {
            client_input.Impulse = MathLib.atoi( msg.Parameters[0] );
        }
    }

    partial class client
    {
        // CL_SendMove
        public void SendMove( ref usercmd_t cmd )
        {
            this.cl.cmd = cmd; // cl.cmd = *cmd - struct copying!!!

            var msg = new MessageWriter( 128 );

            //
            // send the movement message
            //
            msg.WriteByte( ProtocolDef.clc_move );

            msg.WriteFloat( ( float )this.cl.mtime[0] );	// so server can get ping times

            msg.WriteAngle(this.cl.viewangles.X );
            msg.WriteAngle(this.cl.viewangles.Y );
            msg.WriteAngle(this.cl.viewangles.Z );

            msg.WriteShort( ( short ) cmd.forwardmove );
            msg.WriteShort( ( short ) cmd.sidemove );
            msg.WriteShort( ( short ) cmd.upmove );

            //
            // send button bits
            //
            var bits = 0;

            if ( ( client_input.AttackBtn.state & 3 ) != 0 )
                bits |= 1;
            client_input.AttackBtn.state &= ~2;

            if ( ( client_input.JumpBtn.state & 3 ) != 0 )
                bits |= 2;
            client_input.JumpBtn.state &= ~2;

            msg.WriteByte( bits );

            msg.WriteByte( client_input.Impulse );
            client_input.Impulse = 0;

            //
            // deliver the message
            //
            if (this.cls.demoplayback )
                return;

            //
            // allways dump the first two message, because it may contain leftover inputs
            // from the last level
            //
            if ( ++this.cl.movemessages <= 2 )
                return;

            if (this.Host.Network.SendUnreliableMessage(this.cls.netcon, msg ) == -1 )
            {
                this.Host.Console.Print( "CL_SendMove: lost server connection\n" );
                this.Disconnect( );
            }
        }

        // CL_InitInput
        private void InitInput( Host host )
        {
            client_input.Init( host );
        }

        /// <summary>
        /// CL_BaseMove
        /// Send the intended movement message to the server
        /// </summary>
        private void BaseMove( ref usercmd_t cmd )
        {
            if (this.cls.signon != ClientDef.SIGNONS )
                return;

            this.AdjustAngles( );

            cmd.Clear( );

            if ( client_input.StrafeBtn.IsDown )
            {
                cmd.sidemove += this.Host.Cvars.SideSpeed.Get<float>( ) * this.KeyState( ref client_input.RightBtn );
                cmd.sidemove -= this.Host.Cvars.SideSpeed.Get<float>( ) * this.KeyState( ref client_input.LeftBtn );
            }

            cmd.sidemove += this.Host.Cvars.SideSpeed.Get<float>( ) * this.KeyState( ref client_input.MoveRightBtn );
            cmd.sidemove -= this.Host.Cvars.SideSpeed.Get<float>( ) * this.KeyState( ref client_input.MoveLeftBtn );

            var upBtn = this.KeyState( ref client_input.UpBtn );
            if ( upBtn > 0 )
                Console.WriteLine( "asd" );
            cmd.upmove += this.Host.Cvars.UpSpeed.Get<float>( ) * this.KeyState( ref client_input.UpBtn );
            cmd.upmove -= this.Host.Cvars.UpSpeed.Get<float>( ) * this.KeyState( ref client_input.DownBtn );

            if ( !client_input.KLookBtn.IsDown )
            {
                cmd.forwardmove += this.Host.Cvars.ForwardSpeed.Get<float>( ) * this.KeyState( ref client_input.ForwardBtn );
                cmd.forwardmove -= this.Host.Cvars.BackSpeed.Get<float>( ) * this.KeyState( ref client_input.BackBtn );
            }

            //
            // adjust for speed key
            //
            if ( client_input.SpeedBtn.IsDown )
            {
                cmd.forwardmove *= this.Host.Cvars.MoveSpeedKey.Get<float>( );
                cmd.sidemove *= this.Host.Cvars.MoveSpeedKey.Get<float>( );
                cmd.upmove *= this.Host.Cvars.MoveSpeedKey.Get<float>( );
            }
        }

        // CL_AdjustAngles
        //
        // Moves the local angle positions
        private void AdjustAngles( )
        {
            var speed = ( float )this.Host.FrameTime;

            if ( client_input.SpeedBtn.IsDown )
                speed *= this.Host.Cvars.AngleSpeedKey.Get<float>( );

            if ( !client_input.StrafeBtn.IsDown )
            {
                this.cl.viewangles.Y -= speed * this.Host.Cvars.YawSpeed.Get<float>( ) * this.KeyState( ref client_input.RightBtn );
                this.cl.viewangles.Y += speed * this.Host.Cvars.YawSpeed.Get<float>( ) * this.KeyState( ref client_input.LeftBtn );
                this.cl.viewangles.Y = MathLib.AngleMod(this.cl.viewangles.Y );
            }

            if ( client_input.KLookBtn.IsDown )
            {
                this.Host.View.StopPitchDrift( );
                this.cl.viewangles.X -= speed * this.Host.Cvars.PitchSpeed.Get<float>( ) * this.KeyState( ref client_input.ForwardBtn );
                this.cl.viewangles.X += speed * this.Host.Cvars.PitchSpeed.Get<float>( ) * this.KeyState( ref client_input.BackBtn );
            }

            var up = this.KeyState( ref client_input.LookUpBtn );
            var down = this.KeyState( ref client_input.LookDownBtn );

            this.cl.viewangles.X -= speed * this.Host.Cvars.PitchSpeed.Get<float>( ) * up;
            this.cl.viewangles.X += speed * this.Host.Cvars.PitchSpeed.Get<float>( ) * down;

            if ( up != 0 || down != 0 )
                this.Host.View.StopPitchDrift( );

            if (this.cl.viewangles.X > 80 )
                this.cl.viewangles.X = 80;
            if (this.cl.viewangles.X < -70 )
                this.cl.viewangles.X = -70;

            if (this.cl.viewangles.Z > 50 )
                this.cl.viewangles.Z = 50;
            if (this.cl.viewangles.Z < -50 )
                this.cl.viewangles.Z = -50;
        }

        // CL_KeyState
        //
        // Returns 0.25 if a key was pressed and released during the frame,
        // 0.5 if it was pressed and held
        // 0 if held then released, and
        // 1.0 if held for the entire time
        private float KeyState( ref kbutton_t key )
        {
            var impulsedown = ( key.state & 2 ) != 0;
            var impulseup = ( key.state & 4 ) != 0;
            var down = key.IsDown;// ->state & 1;
            float val = 0;

            if ( impulsedown && !impulseup )
            {
                if ( down )
                    val = 0.5f;	// pressed and held this frame
                else
                    val = 0;	//	I_Error ();
            }

            if ( impulseup && !impulsedown )
            {
                if ( down )
                    val = 0;	//	I_Error ();
                else
                    val = 0;	// released this frame
            }

            if ( !impulsedown && !impulseup )
            {
                if ( down )
                    val = 1.0f;	// held the entire frame
                else
                    val = 0;	// up the entire frame
            }

            if ( impulsedown && impulseup )
            {
                if ( down )
                    val = 0.75f;	// released and re-pressed this frame
                else
                    val = 0.25f;	// pressed and released this frame
            }

            key.state &= 1;		// clear impulses

            return val;
        }
    }
}
