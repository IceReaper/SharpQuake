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
    using Framework.IO.Sound;
    using OpenTK.Audio.OpenAL;
    using System;
    using System.Collections.Generic;

    internal class OpenALController : ISoundController
    {
        private const int AL_BUFFER_COUNT = 24;
        private const int BUFFER_SIZE = 0x10000;

        private bool _IsInitialized;
        private int _Source;
        private int[] _Buffers;
        private int[] _BufferBytes;
        private ALFormat _BufferFormat;
        private int _SamplesSent;
        private Queue<int> _FreeBuffers;

        private void FreeContext()
        {
            if(this._Source != 0 )
            {
                AL.SourceStop(this._Source );
                AL.DeleteSource(this._Source );
                this._Source = 0;
            }
            if(this._Buffers != null )
            {
                AL.DeleteBuffers(this._Buffers );
                this._Buffers = null;
            }
        }

        #region ISoundController Members

        public bool IsInitialised => this._IsInitialized;

        public Host Host
        {
            get;
            private set;
        }

        public void Initialise( object host )
        {
            this.Host = ( Host ) host;

            this.FreeContext();

            this._Source = AL.GenSource();
            this._Buffers = new int[OpenALController.AL_BUFFER_COUNT];
            this._BufferBytes = new int[OpenALController.AL_BUFFER_COUNT];
            this._FreeBuffers = new( OpenALController.AL_BUFFER_COUNT );

            for( var i = 0; i < this._Buffers.Length; i++ )
            {
                this._Buffers[i] = AL.GenBuffer();
                this._FreeBuffers.Enqueue(this._Buffers[i] );
            }

            AL.SourcePlay(this._Source );
            AL.Source(this._Source, ALSourceb.Looping, false );

            this.Host.Sound.shm.channels = 2;
            this.Host.Sound.shm.samplebits = 16;
            this.Host.Sound.shm.speed = 11025;
            this.Host.Sound.shm.buffer = new byte[OpenALController.BUFFER_SIZE];
            this.Host.Sound.shm.soundalive = true;
            this.Host.Sound.shm.splitbuffer = false;
            this.Host.Sound.shm.samples = this.Host.Sound.shm.buffer.Length / (this.Host.Sound.shm.samplebits / 8 );
            this.Host.Sound.shm.samplepos = 0;
            this.Host.Sound.shm.submission_chunk = 1;

            if(this.Host.Sound.shm.samplebits == 8 )
            {
                if(this.Host.Sound.shm.channels == 2 )
                    this._BufferFormat = ALFormat.Stereo8;
                else
                    this._BufferFormat = ALFormat.Mono8;
            }
            else
            {
                if(this.Host.Sound.shm.channels == 2 )
                    this._BufferFormat = ALFormat.Stereo16;
                else
                    this._BufferFormat = ALFormat.Mono16;
            }

            this._IsInitialized = true;
        }

        public void Shutdown()
        {
            this.FreeContext();
            this._IsInitialized = false;
        }

        public void ClearBuffer()
        {
            AL.SourceStop(this._Source );
        }

        public byte[] LockBuffer()
        {
            return this.Host.Sound.shm.buffer;
        }

        public unsafe void UnlockBuffer( int bytes )
        {
            int processed;
            AL.GetSource(this._Source, ALGetSourcei.BuffersProcessed, out processed );
            if( processed > 0 )
            {
                var bufs = AL.SourceUnqueueBuffers(this._Source, processed );
                foreach( var buffer in bufs )
                {
                    if( buffer == 0 )
                        continue;

                    var idx = Array.IndexOf(this._Buffers, buffer );
                    if( idx != -1 )
                    {
                        this._SamplesSent += this._BufferBytes[idx] >> ( this.Host.Sound.shm.samplebits / 8 - 1 );
                        this._SamplesSent &= this.Host.Sound.shm.samples - 1;
                        this._BufferBytes[idx] = 0;
                    }
                    if( !this._FreeBuffers.Contains( buffer ) )
                        this._FreeBuffers.Enqueue( buffer );
                }
            }

            if(this._FreeBuffers.Count == 0 )
            {
                this.Host.Console.DPrint( "UnlockBuffer: No free buffers!\n" );
                return;
            }

            var buf = this._FreeBuffers.Dequeue();
            if( buf != 0 )
            {
                AL.BufferData(buf, this._BufferFormat, this.Host.Sound.shm.buffer, this.Host.Sound.shm.speed);

                AL.SourceQueueBuffer(this._Source, buf );

                var idx = Array.IndexOf(this._Buffers, buf );
                if( idx != -1 )
                    this._BufferBytes[idx] = bytes;

                int state;
                AL.GetSource(this._Source, ALGetSourcei.SourceState, out state );
                if( (ALSourceState)state != ALSourceState.Playing )
                {
                    AL.SourcePlay(this._Source );

                    this.Host.Console.DPrint( "Sound resumed from {0}, free {1} of {2} buffers\n",
                        ( (ALSourceState)state ).ToString( "F" ),
                        this._FreeBuffers.Count,
                        this._Buffers.Length );
                }
            }
        }

        public int GetPosition()
        {
            int state, offset = 0;
            AL.GetSource(this._Source, ALGetSourcei.SourceState, out state );
            if( (ALSourceState)state != ALSourceState.Playing )
            {
                for( var i = 0; i < this._BufferBytes.Length; i++ )
                {
                    this._SamplesSent += this._BufferBytes[i] >> ( this.Host.Sound.shm.samplebits / 8 - 1 );
                    this._BufferBytes[i] = 0;
                }

                this._SamplesSent &= this.Host.Sound.shm.samples - 1;
            }
            else
                AL.GetSource(this._Source, ALGetSourcei.SampleOffset, out offset );

            return (this._SamplesSent + offset ) & (this.Host.Sound.shm.samples - 1 );
        }

        #endregion ISoundController Members
    }
}
