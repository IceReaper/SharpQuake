/// <copyright>
///
/// Rewritten in C# by Yury Kiselev, 2010.
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

// cdaudio.h

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

namespace SharpQuake.Sound
{
    using Engine.Host;
    using Framework.Engine;
    using Framework.IO;
    using Framework.Mathematics;
    using Networking.Client;
    using System;
    using System.IO;

    /// <summary>
    /// CDAudio_functions
    /// </summary>

    public class cd_audio
    {
#if _WINDOWS
        private ICDAudioController _Controller;
#else
        NullCDAudioController _Controller;
#endif

        // CHANGE
        private Host Host
        {
            get;
            set;
        }

        public cd_audio( Host host )
        {
            this.Host = host;
            this._Controller = new(this.Host );
        }
        /// <summary>
        /// CDAudio_Init
        /// </summary>
        public bool Initialise( )
        {
            if (this.Host.Client.cls.state == cactive_t.ca_dedicated )
                return false;

            if ( CommandLine.HasParam( "-nocdaudio" ) )
                return false;

            this._Controller.Initialise( );

            if (this._Controller.IsInitialised )
            {
                this.Host.Commands.Add( "cd", this.CD_f );
                this.Host.Console.Print( "CD Audio (Fallback) Initialized\n" );
            }

            return this._Controller.IsInitialised;
        }

        // CDAudio_Play(byte track, qboolean looping)
        public void Play( byte track, bool looping )
        {
            this._Controller.Play( track, looping );
#if DEBUG
            Console.WriteLine( "DEBUG: track byte:{0} - loop byte: {1}", track, looping );
#endif
        }

        // CDAudio_Stop
        public void Stop( )
        {
            this._Controller.Stop( );
        }

        // CDAudio_Pause
        public void Pause( )
        {
            this._Controller.Pause( );
        }

        // CDAudio_Resume
        public void Resume( )
        {
            this._Controller.Resume( );
        }

        // CDAudio_Shutdown
        public void Shutdown( )
        {
            this._Controller.Shutdown( );
        }

        // CDAudio_Update
        public void Update( )
        {
            this._Controller.Update( );
        }

        private void CD_f( CommandMessage msg )
        {
            if ( msg.Parameters == null || msg.Parameters.Length < 1 )
                return;

            var command = msg.Parameters[0];

            if ( Utilities.SameText( command, "on" ) )
            {
                this._Controller.IsEnabled = true;
                return;
            }

            if ( Utilities.SameText( command, "off" ) )
            {
                if (this._Controller.IsPlaying )
                    this._Controller.Stop( );

                this._Controller.IsEnabled = false;
                return;
            }

            if ( Utilities.SameText( command, "reset" ) )
            {
                this._Controller.IsEnabled = true;
                if (this._Controller.IsPlaying )
                    this._Controller.Stop( );

                this._Controller.ReloadDiskInfo( );
                return;
            }

            if ( Utilities.SameText( command, "remap" ) )
            {
                var ret = msg.Parameters.Length - 1;
                var remap = this._Controller.Remap;
                if ( ret <= 0 )
                {
                    for ( var n = 1; n < 100; n++ )
                    {
                        if ( remap[n] != n )
                            this.Host.Console.Print( "  {0} -> {1}\n", n, remap[n] );
                    }

                    return;
                }
                for ( var n = 1; n <= ret; n++ )
                    remap[n] = ( byte ) MathLib.atoi( msg.Parameters[n] );
                return;
            }

            if ( Utilities.SameText( command, "close" ) )
            {
                this._Controller.CloseDoor( );
                return;
            }

            if ( !this._Controller.IsValidCD )
            {
                this._Controller.ReloadDiskInfo( );
                if ( !this._Controller.IsValidCD )
                {
                    this.Host.Console.Print( "No CD in player.\n" );
                    return;
                }
            }

            if ( Utilities.SameText( command, "play" ) )
            {
                this._Controller.Play( ( byte ) MathLib.atoi( msg.Parameters[1] ), false );
                return;
            }

            if ( Utilities.SameText( command, "loop" ) )
            {
                this._Controller.Play( ( byte ) MathLib.atoi( msg.Parameters[1] ), true );
                return;
            }

            if ( Utilities.SameText( command, "stop" ) )
            {
                this._Controller.Stop( );
                return;
            }

            if ( Utilities.SameText( command, "pause" ) )
            {
                this._Controller.Pause( );
                return;
            }

            if ( Utilities.SameText( command, "resume" ) )
            {
                this._Controller.Resume( );
                return;
            }

            if ( Utilities.SameText( command, "eject" ) )
            {
                if (this._Controller.IsPlaying )
                    this._Controller.Stop( );

                this._Controller.Eject( );
                return;
            }

            if ( Utilities.SameText( command, "info" ) )
            {
                this.Host.Console.Print( "%u tracks\n", this._Controller.MaxTrack );
                if (this._Controller.IsPlaying )
                    this.Host.Console.Print( "Currently {0} track {1}\n", this._Controller.IsLooping ? "looping" : "playing", this._Controller.CurrentTrack );
                else if (this._Controller.IsPaused )
                    this.Host.Console.Print( "Paused {0} track {1}\n", this._Controller.IsLooping ? "looping" : "playing", this._Controller.CurrentTrack );

                this.Host.Console.Print( "Volume is {0}\n", this._Controller.Volume );
                return;
            }
        }
    }

    internal class NullCDAudioController
    {
        private byte[] _Remap;
        //private WaveOutEvent waveOut; // or WaveOutEvent()
        private bool _isLooping;
        private string trackid;
        private string trackpath;
        private bool _noAudio = false;
        private bool _noPlayback = false;
        private float _Volume;
        private bool _isPlaying;
        private bool _isPaused;

        private Host Host
        {
            get;
            set;
        }

        public NullCDAudioController( Host host )
        {
            this.Host = host;
            this._Remap = new byte[100];
        }

        #region ICDAudioController Members

        public bool IsInitialised => true;

        public bool IsEnabled
        {
            get => true;
            set
            {

            }
        }

        public bool IsPlaying => this._isPlaying;

        public bool IsPaused => this._isPaused;

        public bool IsValidCD => false;

        public bool IsLooping => this._isLooping;

        public byte[] Remap => this._Remap;

        public byte MaxTrack => 0;

        public byte CurrentTrack => 0;

        public float Volume
        {
            get => this._Volume;
            set => this._Volume = value;
        }

        public void Initialise( )
        {
            this._Volume = this.Host.Sound.BgmVolume;

            if ( Directory.Exists( string.Format( "{0}/{1}/music/", QuakeParameter.globalbasedir, QuakeParameter.globalgameid ) ) == false )
                this._noAudio = true;
        }

        public void Play( byte track, bool looping )
        {
            if (this._noAudio == false )
            {
                this.trackid = track.ToString( "00" );
                this.trackpath = string.Format( "{0}/{1}/music/track{2}.ogg", QuakeParameter.globalbasedir, QuakeParameter.globalgameid, this.trackid );
#if DEBUG
                Console.WriteLine( "DEBUG: track path:{0} ", this.trackpath );
#endif
                try
                {
                    this._isLooping = looping;
                    this._noPlayback = false;
                }
                catch ( Exception e )
                {
                    Console.WriteLine( "Could not find or play {0}", this.trackpath );
                    this._noPlayback = true;
                    //throw;
                }
            }
        }

        public void Stop( )
        {
            if (this._noAudio == true )
                return;
        }

        public void Pause( )
        {
            if (this._noAudio == true )
                return;
        }

        public void Resume( )
        {
        }

        public void Shutdown( )
        {
            if (this._noAudio == true )
                return;
        }

        public void Update( )
        {
            if (this._noAudio == true )
                return;

            if (this._noPlayback == true )
                return;

            /*if (waveOut.PlaybackState == PlaybackState.Paused)
            {
                _isPaused = true;
            }
            else if (waveOut.PlaybackState == PlaybackState.Playing)
            {
                _isPaused = false;
            }

            if (waveOut.PlaybackState == PlaybackState.Paused || waveOut.PlaybackState == PlaybackState.Stopped)
            {
                _isPlaying = false;
            }
            else if (waveOut.PlaybackState == PlaybackState.Playing)
            {
                _isPlaying = true;
            }*/

            this._Volume = this.Host.Sound.BgmVolume;
        }

        public void ReloadDiskInfo( )
        {
        }

        public void CloseDoor( )
        {
        }

        public void Eject( )
        {
        }

        #endregion ICDAudioController Members
    }
}
