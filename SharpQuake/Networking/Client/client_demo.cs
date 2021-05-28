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
    using Framework.Data;
    using Framework.Definitions;
    using Framework.Engine;
    using Framework.IO;
    using Framework.Mathematics;
    using System.IO;
    using System.Text;

    partial class client
    {
        /// <summary>
        /// CL_StopPlayback
        ///
        /// Called when a demo file runs out, or the user starts a game
        /// </summary>
        public void StopPlayback( )
        {
            if ( !this.cls.demoplayback )
                return;

            if (this.cls.demofile != null )
            {
                this.cls.demofile.Dispose( );
                this.cls.demofile = null;
            }

            this.cls.demoplayback = false;
            this.cls.state = cactive_t.ca_disconnected;

            if (this.cls.timedemo )
                this.FinishTimeDemo( );
        }

        /// <summary>
        /// CL_Record_f
        /// record <demoname> <map> [cd track]
        /// </summary>
        private void Record_f( CommandMessage msg )
        {
            if ( msg.Source != CommandSource.Command )
                return;

            var c = msg.Parameters != null ? msg.Parameters.Length : 0;

            if ( c != 1 && c != 2 && c != 3 )
            {
                this.Host.Console.Print( "record <demoname> [<map> [cd track]]\n" );
                return;
            }

            if ( msg.Parameters[0].Contains( ".." ) )
            {
                this.Host.Console.Print( "Relative pathnames are not allowed.\n" );
                return;
            }

            if ( c == 2 && this.cls.state == cactive_t.ca_connected )
            {
                this.Host.Console.Print( "Can not record - already connected to server\nClient demo recording must be started before connecting\n" );
                return;
            }

            // write the forced cd track number, or -1
            int track;
            if ( c == 3 )
            {
                track = MathLib.atoi( msg.Parameters[2] );
                this.Host.Console.Print( "Forcing CD track to {0}\n", track );
            }
            else
                track = -1;

            var name = Path.Combine( FileSystem.GameDir, msg.Parameters[0] );

            //
            // start the map up
            //
            if ( c > 1 )
                this.Host.Commands.ExecuteString( string.Format( "map {0}", msg.Parameters[1] ), CommandSource.Command );

            //
            // open the demo file
            //
            name = Path.ChangeExtension( name, ".dem" );

            this.Host.Console.Print( "recording to {0}.\n", name );
            var fs = FileSystem.OpenWrite( name, true );
            if ( fs == null )
            {
                this.Host.Console.Print( "ERROR: couldn't open.\n" );
                return;
            }
            var writer = new BinaryWriter( fs, Encoding.ASCII );
            this.cls.demofile = new DisposableWrapper<BinaryWriter>( writer, true );
            this.cls.forcetrack = track;
            var tmp = Encoding.ASCII.GetBytes(this.cls.forcetrack.ToString( ) );
            writer.Write( tmp );
            writer.Write( '\n' );
            this.cls.demorecording = true;
        }

        /// <summary>
        /// CL_Stop_f
        /// stop recording a demo
        /// </summary>
        private void Stop_f( CommandMessage msg )
        {
            if ( msg.Source != CommandSource.Command )
                return;

            if ( !this.cls.demorecording )
            {
                this.Host.Console.Print( "Not recording a demo.\n" );
                return;
            }

            // write a disconnect message to the demo file
            this.Host.Network.Message.Clear( );
            this.Host.Network.Message.WriteByte( ProtocolDef.svc_disconnect );
            this.WriteDemoMessage( );

            // finish up
            if (this.cls.demofile != null )
            {
                this.cls.demofile.Dispose( );
                this.cls.demofile = null;
            }

            this.cls.demorecording = false;
            this.Host.Console.Print( "Completed demo\n" );
        }

        // CL_PlayDemo_f
        //
        // play [demoname]
        private void PlayDemo_f( CommandMessage msg )
        {
            if ( msg.Source != CommandSource.Command )
                return;

            var c = msg.Parameters != null ? msg.Parameters.Length : 0;

            if ( c != 1 )
            {
                this.Host.Console.Print( "play <demoname> : plays a demo\n" );
                return;
            }

            //
            // disconnect from server
            //
            this.Disconnect( );

            //
            // open the demo file
            //
            var name = Path.ChangeExtension( msg.Parameters[0], ".dem" );

            this.Host.Console.Print( "Playing demo from {0}.\n", name );
            if (this.cls.demofile != null )
                this.cls.demofile.Dispose( );

            DisposableWrapper<BinaryReader> reader;
            FileSystem.FOpenFile( name, out reader );
            this.cls.demofile = reader;
            if (this.cls.demofile == null )
            {
                this.Host.Console.Print( "ERROR: couldn't open.\n" );
                this.cls.demonum = -1;		// stop demo loop
                return;
            }

            this.cls.demoplayback = true;
            this.cls.state = cactive_t.ca_connected;
            this.cls.forcetrack = 0;

            var s = reader.Object;
            c = 0;
            var neg = false;
            while ( true )
            {
                c = s.ReadByte( );
                if ( c == '\n' )
                    break;

                if ( c == '-' )
                    neg = true;
                else
                    this.cls.forcetrack = this.cls.forcetrack * 10 + ( c - '0' );
            }

            if ( neg )
                this.cls.forcetrack = -this.cls.forcetrack;
            // ZOID, fscanf is evil
            //	fscanf (cls.demofile, "%i\n", &cls.forcetrack);
        }

        /// <summary>
        /// CL_TimeDemo_f
        /// timedemo [demoname]
        /// </summary>
        private void TimeDemo_f( CommandMessage msg )
        {
            if ( msg.Source != CommandSource.Command )
                return;

            var c = msg.Parameters != null ? msg.Parameters.Length : 0;

            if ( c != 1 )
            {
                this.Host.Console.Print( "timedemo <demoname> : gets demo speeds\n" );
                return;
            }

            this.PlayDemo_f( msg );

            // cls.td_starttime will be grabbed at the second frame of the demo, so
            // all the loading time doesn't get counted
            this._Static.timedemo = true;
            this._Static.td_startframe = this.Host.FrameCount;
            this._Static.td_lastframe = -1;		// get a new message this frame
        }

        /// <summary>
        /// CL_GetMessage
        /// Handles recording and playback of demos, on top of NET_ code
        /// </summary>
        /// <returns></returns>
        private int GetMessage( )
        {
            if (this.cls.demoplayback )
            {
                // decide if it is time to grab the next message
                if (this.cls.signon == ClientDef.SIGNONS )	// allways grab until fully connected
                {
                    if (this.cls.timedemo )
                    {
                        if (this.Host.FrameCount == this.cls.td_lastframe )
                            return 0;		// allready read this frame's message

                        this.cls.td_lastframe = this.Host.FrameCount;
                        // if this is the second frame, grab the real td_starttime
                        // so the bogus time on the first frame doesn't count
                        if (this.Host.FrameCount == this.cls.td_startframe + 1 )
                            this.cls.td_starttime = ( float )this.Host.RealTime;
                    }
                    else if (this.cl.time <= this.cl.mtime[0] )
                        return 0;	// don't need another message yet
                }

                // get the next message
                var reader = ( ( DisposableWrapper<BinaryReader> )this.cls.demofile ).Object;
                var size = EndianHelper.LittleLong( reader.ReadInt32( ) );
                if ( size > QDef.MAX_MSGLEN )
                    Utilities.Error( "Demo message > MAX_MSGLEN" );

                this.cl.mviewangles[1] = this.cl.mviewangles[0];
                this.cl.mviewangles[0].X = EndianHelper.LittleFloat( reader.ReadSingle( ) );
                this.cl.mviewangles[0].Y = EndianHelper.LittleFloat( reader.ReadSingle( ) );
                this.cl.mviewangles[0].Z = EndianHelper.LittleFloat( reader.ReadSingle( ) );

                this.Host.Network.Message.FillFrom( reader.BaseStream, size );
                if (this.Host.Network.Message.Length < size )
                {
                    this.StopPlayback( );
                    return 0;
                }
                return 1;
            }

            int r;
            while ( true )
            {
                r = this.Host.Network.GetMessage(this.cls.netcon );

                if ( r != 1 && r != 2 )
                    return r;

                // discard nop keepalive message
                if (this.Host.Network.Message.Length == 1 && this.Host.Network.Message.Data[0] == ProtocolDef.svc_nop )
                    this.Host.Console.Print( "<-- server to client keepalive\n" );
                else
                    break;
            }

            if (this.cls.demorecording )
                this.WriteDemoMessage( );

            return r;
        }

        /// <summary>
        /// CL_FinishTimeDemo
        /// </summary>
        private void FinishTimeDemo( )
        {
            this.cls.timedemo = false;

            // the first frame didn't count
            var frames = this.Host.FrameCount - this.cls.td_startframe - 1;
            var time = ( float )this.Host.RealTime - this.cls.td_starttime;
            if ( time == 0 )
                time = 1;

            this.Host.Console.Print( "{0} frames {1:F5} seconds {2:F2} fps\n", frames, time, frames / time );
        }

        /// <summary>
        /// CL_WriteDemoMessage
        /// Dumps the current net message, prefixed by the length and view angles
        /// </summary>
        private void WriteDemoMessage( )
        {
            var len = EndianHelper.LittleLong(this.Host.Network.Message.Length );
            var writer = ( ( DisposableWrapper<BinaryWriter> )this.cls.demofile ).Object;
            writer.Write( len );
            writer.Write( EndianHelper.LittleFloat(this.cl.viewangles.X ) );
            writer.Write( EndianHelper.LittleFloat(this.cl.viewangles.Y ) );
            writer.Write( EndianHelper.LittleFloat(this.cl.viewangles.Z ) );
            writer.Write(this.Host.Network.Message.Data, 0, this.Host.Network.Message.Length );
            writer.Flush( );
        }
    }
}
