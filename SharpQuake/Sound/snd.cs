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



// sound.h -- client sound i/o functions

namespace SharpQuake.Sound
{
    using Engine.Host;
    using Framework.Definitions;
    using Framework.Definitions.Bsp;
    using Framework.Engine;
    using Framework.IO;
    using Framework.IO.Sound;
    using Framework.Mathematics;
    using System.Numerics;

    // snd_started == Sound._Controller.IsInitialized
	// snd_initialized == Sound._IsInitialized

	/// <summary>
	/// S_functions
	/// </summary>
	public partial class snd
    {
        public bool IsInitialised => this._Controller.IsInitialised;

        public DMA_t shm => this._shm;

        public float BgmVolume => this.Host.Cvars.BgmVolume.Get<float>( );

        public float Volume => this.Host.Cvars.Volume.Get<float>( );

        public const int DEFAULT_SOUND_PACKET_VOLUME = 255;
        public const float DEFAULT_SOUND_PACKET_ATTENUATION = 1.0f;
        public const int MAX_CHANNELS = 128;
        public const int MAX_DYNAMIC_CHANNELS = 8;

        private const int MAX_SFX = 512;

        

        private ISoundController _Controller = new OpenALController( );// NullSoundController();
        private bool _IsInitialized; // snd_initialized

        private SoundEffect_t[] _KnownSfx = new SoundEffect_t[snd.MAX_SFX]; // hunk allocated [MAX_SFX]
        private int _NumSfx; // num_sfx
        private SoundEffect_t[] _AmbientSfx = new SoundEffect_t[AmbientDef.NUM_AMBIENTS]; // *ambient_sfx[NUM_AMBIENTS]
        private bool _Ambient = true; // snd_ambient
        private DMA_t _shm = new( ); // shm

        // 0 to MAX_DYNAMIC_CHANNELS-1	= normal entity sounds
        // MAX_DYNAMIC_CHANNELS to MAX_DYNAMIC_CHANNELS + NUM_AMBIENTS -1 = water, etc
        // MAX_DYNAMIC_CHANNELS + NUM_AMBIENTS to total_channels = static sounds
        private Channel_t[] _Channels = new Channel_t[snd.MAX_CHANNELS]; // channels[MAX_CHANNELS]

        private int _TotalChannels; // total_channels

        private float _SoundNominalClipDist = 1000.0f; // sound_nominal_clip_dist
        private Vector3 _ListenerOrigin; // listener_origin
        private Vector3 _ListenerForward; // listener_forward
        private Vector3 _ListenerRight; // listener_right
        private Vector3 _ListenerUp; // listener_up

        private int _SoundTime; // soundtime		// sample PAIRS
        private int _PaintedTime; // paintedtime 	// sample PAIRS
        private bool _SoundStarted; // sound_started
        private int _SoundBlocked = 0; // snd_blocked
        private int _OldSamplePos; // oldsamplepos from GetSoundTime()
        private int _Buffers; // buffers from GetSoundTime()
        private int _PlayHash = 345; // hash from S_Play()
        private int _PlayVolHash = 543; // hash S_PlayVol

        // CHANGE
        private Host Host
        {
            get;
            set;
        }

        // S_Init (void)
        public void Initialise( )
        {
            this.Host.Cvars.BgmVolume = this.Host.CVars.Add( "bgmvolume", 1f, ClientVariableFlags.Archive );// = { "bgmvolume", "1", true };
            this.Host.Cvars.Volume = this.Host.CVars.Add( "volume", 0.7f, ClientVariableFlags.Archive );// = { "volume", "0.7", true };
            this.Host.Cvars.NoSound = this.Host.CVars.Add( "nosound", false );// = { "nosound", "0" };
            this.Host.Cvars.Precache = this.Host.CVars.Add( "precache", true );// = { "precache", "1" };
            this.Host.Cvars.LoadAs8bit = this.Host.CVars.Add( "loadas8bit", false );// = { "loadas8bit", "0" };
            this.Host.Cvars.BgmBuffer = this.Host.CVars.Add( "bgmbuffer", 4096f );// = { "bgmbuffer", "4096" };
            this.Host.Cvars.AmbientLevel = this.Host.CVars.Add( "ambient_level", 0.3f );// = { "ambient_level", "0.3" };
            this.Host.Cvars.AmbientFade = this.Host.CVars.Add( "ambient_fade", 100f );// = { "ambient_fade", "100" };
            this.Host.Cvars.NoExtraUpdate = this.Host.CVars.Add( "snd_noextraupdate", false );// = { "snd_noextraupdate", "0" };
            this.Host.Cvars.Show = this.Host.CVars.Add( "snd_show", false );// = { "snd_show", "0" };
            this.Host.Cvars.MixAhead = this.Host.CVars.Add( "_snd_mixahead", 0.1f, ClientVariableFlags.Archive );// = { "_snd_mixahead", "0.1", true };

            this.Host.Console.Print( "\nSound Initialization\n" );

            if ( CommandLine.HasParam( "-nosound" ) )
                return;

            for ( var i = 0; i < this._Channels.Length; i++ )
                this._Channels[i] = new( );

            this.Host.Commands.Add( "play", this.Play );
            this.Host.Commands.Add( "playvol", this.PlayVol );
            this.Host.Commands.Add( "stopsound", this.StopAllSoundsCmd );
            this.Host.Commands.Add( "soundlist", this.SoundList );
            this.Host.Commands.Add( "soundinfo", this.SoundInfo_f );

            this._IsInitialized = true;

            this.Startup( );

            this.InitScaletable( );

            this._NumSfx = 0;

            this.Host.Console.Print( "Sound sampling rate: {0}\n", this._shm.speed );

            // provides a tick sound until washed clean
            this._AmbientSfx[AmbientDef.AMBIENT_WATER] = this.PrecacheSound( "ambience/water1.wav" );
            this._AmbientSfx[AmbientDef.AMBIENT_SKY] = this.PrecacheSound( "ambience/wind2.wav" );

            this.StopAllSounds( true );
        }

        // S_AmbientOff (void)
        public void AmbientOff( )
        {
            this._Ambient = false;
        }

        // S_AmbientOn (void)
        public void AmbientOn( )
        {
            this._Ambient = true;
        }

        // S_Shutdown (void)
        public void Shutdown( )
        {
            if ( !this._Controller.IsInitialised )
                return;

            if (this._shm != null )
                this._shm.gamealive = false;

            this._Controller.Shutdown( );
            this._shm = null;
        }

        // S_TouchSound (char *sample)
        public void TouchSound( string sample )
        {
            if ( !this._Controller.IsInitialised )
                return;

            var sfx = this.FindName( sample );
            this.Host.Cache.Check( sfx.cache );
        }

        // S_ClearBuffer (void)
        public void ClearBuffer( )
        {
            if ( !this._Controller.IsInitialised || this._shm == null || this._shm.buffer == null )
                return;

            this._Controller.ClearBuffer( );
        }

        // S_StaticSound (sfx_t *sfx, vec3_t origin, float vol, float attenuation)
        public void StaticSound( SoundEffect_t sfx, ref Vector3 origin, float vol, float attenuation )
        {
            if ( sfx == null )
                return;

            if (this._TotalChannels == snd.MAX_CHANNELS )
            {
                this.Host.Console.Print( "total_channels == MAX_CHANNELS\n" );
                return;
            }

            var ss = this._Channels[this._TotalChannels];
            this._TotalChannels++;

            var sc = this.LoadSound( sfx );
            if ( sc == null )
                return;

            if ( sc.loopstart == -1 )
            {
                this.Host.Console.Print( "Sound {0} not looped\n", sfx.name );
                return;
            }

            ss.sfx = sfx;
            ss.origin = origin;
            ss.master_vol = ( int ) vol;
            ss.dist_mult = attenuation / 64 / this._SoundNominalClipDist;
            ss.end = this._PaintedTime + sc.length;

            this.Spatialize( ss );
        }

        // S_StartSound (int entnum, int entchannel, sfx_t *sfx, vec3_t origin, float fvol,  float attenuation)
        public void StartSound( int entnum, int entchannel, SoundEffect_t sfx, ref Vector3 origin, float fvol, float attenuation )
        {
            if ( !this._SoundStarted || sfx == null )
                return;

            if (this.Host.Cvars.NoSound.Get<bool>( ) )
                return;

            var vol = ( int ) ( fvol * 255 );

            // pick a channel to play on
            var target_chan = this.PickChannel( entnum, entchannel );
            if ( target_chan == null )
                return;

            // spatialize
            //memset (target_chan, 0, sizeof(*target_chan));
            target_chan.origin = origin;
            target_chan.dist_mult = attenuation / this._SoundNominalClipDist;
            target_chan.master_vol = vol;
            target_chan.entnum = entnum;
            target_chan.entchannel = entchannel;
            this.Spatialize( target_chan );

            if ( target_chan.leftvol == 0 && target_chan.rightvol == 0 )
                return;		// not audible at all

            // new channel
            var sc = this.LoadSound( sfx );
            if ( sc == null )
            {
                target_chan.sfx = null;
                return;		// couldn't load the sound's data
            }

            target_chan.sfx = sfx;
            target_chan.pos = 0;
            target_chan.end = this._PaintedTime + sc.length;

            // if an identical sound has also been started this frame, offset the pos
            // a bit to keep it from just making the first one louder
            for ( var i = AmbientDef.NUM_AMBIENTS; i < AmbientDef.NUM_AMBIENTS + snd.MAX_DYNAMIC_CHANNELS; i++ )
            {
                var check = this._Channels[i];
                if ( check == target_chan )
                    continue;

                if ( check.sfx == sfx && check.pos == 0 )
                {
                    var skip = MathLib.Random( ( int ) ( 0.1 * this._shm.speed ) );// rand() % (int)(0.1 * shm->speed);
                    if ( skip >= target_chan.end )
                        skip = target_chan.end - 1;
                    target_chan.pos += skip;
                    target_chan.end -= skip;
                    break;
                }
            }
        }

        // S_StopSound (int entnum, int entchannel)
        public void StopSound( int entnum, int entchannel )
        {
            for ( var i = 0; i < snd.MAX_DYNAMIC_CHANNELS; i++ )
            {
                if (this._Channels[i].entnum == entnum && this._Channels[i].entchannel == entchannel )
                {
                    this._Channels[i].end = 0;
                    this._Channels[i].sfx = null;
                    return;
                }
            }
        }

        // sfx_t *S_PrecacheSound (char *sample)
        public SoundEffect_t PrecacheSound( string sample )
        {
            if ( !this._IsInitialized || this.Host.Cvars.NoSound.Get<bool>( ) )
                return null;

            var sfx = this.FindName( sample );

            // cache it in
            if (this.Host.Cvars.Precache.Get<bool>( ) )
                this.LoadSound( sfx );

            return sfx;
        }

        // void S_ClearPrecache (void)
        public void ClearPrecache( )
        {
            // nothing to do
        }

        // void S_Update (vec3_t origin, vec3_t v_forward, vec3_t v_right, vec3_t v_up)
        //
        // Called once each time through the main loop
        public void Update( ref Vector3 origin, ref Vector3 forward, ref Vector3 right, ref Vector3 up )
        {
            if ( !this._IsInitialized || this._SoundBlocked > 0 )
                return;

            this._ListenerOrigin = origin;
            this._ListenerForward = forward;
            this._ListenerRight = right;
            this._ListenerUp = up;

            // update general area ambient sound sources
            this.UpdateAmbientSounds( );

            Channel_t combine = null;

            // update spatialization for and dynamic sounds
            //channel_t ch = channels + NUM_AMBIENTS;
            for ( var i = AmbientDef.NUM_AMBIENTS; i < this._TotalChannels; i++ )
            {
                var ch = this._Channels[i];// channels + NUM_AMBIENTS;
                if ( ch.sfx == null )
                    continue;

                this.Spatialize( ch );  // respatialize channel
                if ( ch.leftvol == 0 && ch.rightvol == 0 )
                    continue;

                // try to combine static sounds with a previous channel of the same
                // sound effect so we don't mix five torches every frame
                if ( i >= snd.MAX_DYNAMIC_CHANNELS + AmbientDef.NUM_AMBIENTS )
                {
                    // see if it can just use the last one
                    if ( combine != null && combine.sfx == ch.sfx )
                    {
                        combine.leftvol += ch.leftvol;
                        combine.rightvol += ch.rightvol;
                        ch.leftvol = ch.rightvol = 0;
                        continue;
                    }
                    // search for one
                    combine = this._Channels[snd.MAX_DYNAMIC_CHANNELS + AmbientDef.NUM_AMBIENTS];// channels + MAX_DYNAMIC_CHANNELS + NUM_AMBIENTS;
                    int j;
                    for ( j = snd.MAX_DYNAMIC_CHANNELS + AmbientDef.NUM_AMBIENTS; j < i; j++ )
                    {
                        combine = this._Channels[j];
                        if ( combine.sfx == ch.sfx )
                            break;
                    }

                    if ( j == this._TotalChannels )
                        combine = null;
                    else
                    {
                        if ( combine != ch )
                        {
                            combine.leftvol += ch.leftvol;
                            combine.rightvol += ch.rightvol;
                            ch.leftvol = ch.rightvol = 0;
                        }
                        continue;
                    }
                }
            }

            //
            // debugging output
            //
            if (this.Host.Cvars.Show.Get<bool>( ) )
            {
                var total = 0;
                for ( var i = 0; i < this._TotalChannels; i++ )
                {
                    var ch = this._Channels[i];
                    if ( ch.sfx != null && ( ch.leftvol > 0 || ch.rightvol > 0 ) )
                        total++;
                }

                this.Host.Console.Print( "----({0})----\n", total );
            }

            // mix some sound
            this.Update( );
        }

        // S_StopAllSounds (qboolean clear)
        public void StopAllSounds( bool clear )
        {
            if ( !this._Controller.IsInitialised )
                return;

            this._TotalChannels = snd.MAX_DYNAMIC_CHANNELS + AmbientDef.NUM_AMBIENTS;	// no statics

            for ( var i = 0; i < snd.MAX_CHANNELS; i++ )
            {
                if (this._Channels[i].sfx != null )
                    this._Channels[i].Clear( );
            }

            if ( clear )
                this.ClearBuffer( );
        }

        // void S_BeginPrecaching (void)
        public void BeginPrecaching( )
        {
            // nothing to do
        }

        // void S_EndPrecaching (void)
        public void EndPrecaching( )
        {
            // nothing to do
        }

        // void S_ExtraUpdate (void)
        public void ExtraUpdate( )
        {
            if ( !this._IsInitialized )
                return;
#if _WIN32
	        IN_Accumulate ();
#endif

            if (this.Host.Cvars.NoExtraUpdate.Get<bool>( ) )
                return;		// don't pollute timings

            this.Update( );
        }

        // void S_LocalSound (char *s)
        public void LocalSound( string sound )
        {
            if (this.Host.Cvars.NoSound.Get<bool>( ) )
                return;

            if ( !this._Controller.IsInitialised )
                return;

            var sfx = this.PrecacheSound( sound );
            if ( sfx == null )
            {
                this.Host.Console.Print( "S_LocalSound: can't cache {0}\n", sound );
                return;
            }

            this.StartSound(this.Host.Client.cl.viewentity, -1, sfx, ref Utilities.ZeroVector, 1, 1 );
        }

        // S_Startup
        public void Startup( )
        {
            if (this._IsInitialized && !this._Controller.IsInitialised )
            {
                this._Controller.Initialise(this.Host );
                this._SoundStarted = this._Controller.IsInitialised;
            }
        }

        /// <summary>
        /// S_BlockSound
        /// </summary>
        public void BlockSound( )
        {
            this._SoundBlocked++;

            if (this._SoundBlocked == 1 )
                this._Controller.ClearBuffer( );  //waveOutReset (hWaveOut);
        }

        /// <summary>
        /// S_UnblockSound
        /// </summary>
        public void UnblockSound( )
        {
            this._SoundBlocked--;
        }

        // S_Play
        private void Play( CommandMessage msg )
        {
            foreach ( var parameter in msg.Parameters )
            {
                var name = parameter;

                var k = name.IndexOf( '.' );
                if ( k == -1 )
                    name += ".wav";

                var sfx = this.PrecacheSound( name );
                this.StartSound(this._PlayHash++, 0, sfx, ref this._ListenerOrigin, 1.0f, 1.0f );
            }
        }

        // S_PlayVol
        private void PlayVol( CommandMessage msg )
        {
            for ( var i = 0; i < msg.Parameters.Length; i += 2 )
            {
                var name = msg.Parameters[i];
                var k = name.IndexOf( '.' );
                if ( k == -1 )
                    name += ".wav";

                var sfx = this.PrecacheSound( name );
                var vol = float.Parse( msg.Parameters[i + 1] );
                this.StartSound(this._PlayVolHash++, 0, sfx, ref this._ListenerOrigin, vol, 1.0f );
            }
        }

        // S_SoundList
        private void SoundList( CommandMessage msg )
        {
            var total = 0;
            for ( var i = 0; i < this._NumSfx; i++ )
            {
                var sfx = this._KnownSfx[i];
                var sc = ( SoundEffectCache_t )this.Host.Cache.Check( sfx.cache );
                if ( sc == null )
                    continue;

                var size = sc.length * sc.width * ( sc.stereo + 1 );
                total += size;
                if ( sc.loopstart >= 0 )
                    this.Host.Console.Print( "L" );
                else
                    this.Host.Console.Print( " " );

                this.Host.Console.Print( "({0:d2}b) {1:g6} : {2}\n", sc.width * 8, size, sfx.name );
            }

            this.Host.Console.Print( "Total resident: {0}\n", total );
        }

        // S_SoundInfo_f
        private void SoundInfo_f( CommandMessage msg )
        {
            if ( !this._Controller.IsInitialised || this._shm == null )
            {
                this.Host.Console.Print( "sound system not started\n" );
                return;
            }

            this.Host.Console.Print( "{0:d5} stereo\n", this._shm.channels - 1 );
            this.Host.Console.Print( "{0:d5} samples\n", this._shm.samples );
            this.Host.Console.Print( "{0:d5} samplepos\n", this._shm.samplepos );
            this.Host.Console.Print( "{0:d5} samplebits\n", this._shm.samplebits );
            this.Host.Console.Print( "{0:d5} submission_chunk\n", this._shm.submission_chunk );
            this.Host.Console.Print( "{0:d5} speed\n", this._shm.speed );
            //Host.Console.Print("0x%x dma buffer\n", _shm.buffer);
            this.Host.Console.Print( "{0:d5} total_channels\n", this._TotalChannels );
        }

        // S_StopAllSoundsC
        private void StopAllSoundsCmd( CommandMessage msg )
        {
            this.StopAllSounds( true );
        }

        // S_FindName
        private SoundEffect_t FindName( string name )
        {
            if ( string.IsNullOrEmpty( name ) )
                Utilities.Error( "S_FindName: NULL or empty\n" );

            if ( name.Length >= QDef.MAX_QPATH )
                Utilities.Error( "Sound name too long: {0}", name );

            // see if already loaded
            for ( var i = 0; i < this._NumSfx; i++ )
            {
                if (this._KnownSfx[i].name == name )// !Q_strcmp(known_sfx[i].name, name))
                    return this._KnownSfx[i];
            }

            if (this._NumSfx == snd.MAX_SFX )
                Utilities.Error( "S_FindName: out of sfx_t" );

            var sfx = this._KnownSfx[this._NumSfx];
            sfx.name = name;

            this._NumSfx++;
            return sfx;
        }

        // SND_Spatialize
        private void Spatialize( Channel_t ch )
        {
            // anything coming from the view entity will allways be full volume
            if ( ch.entnum == this.Host.Client.cl.viewentity )
            {
                ch.leftvol = ch.master_vol;
                ch.rightvol = ch.master_vol;
                return;
            }

            // calculate stereo seperation and distance attenuation
            var snd = ch.sfx;
            var source_vec = ch.origin - this._ListenerOrigin;

            var dist = MathLib.Normalize( ref source_vec ) * ch.dist_mult;
            var dot = Vector3.Dot(this._ListenerRight, source_vec );

            float rscale, lscale;
            if (this._shm.channels == 1 )
            {
                rscale = 1.0f;
                lscale = 1.0f;
            }
            else
            {
                rscale = 1.0f + dot;
                lscale = 1.0f - dot;
            }

            // add in distance effect
            var scale = ( 1.0f - dist ) * rscale;
            ch.rightvol = ( int ) ( ch.master_vol * scale );
            if ( ch.rightvol < 0 )
                ch.rightvol = 0;

            scale = ( 1.0f - dist ) * lscale;
            ch.leftvol = ( int ) ( ch.master_vol * scale );
            if ( ch.leftvol < 0 )
                ch.leftvol = 0;
        }

        // S_LoadSound
        private SoundEffectCache_t LoadSound( SoundEffect_t s )
        {
            // see if still in memory
            var sc = ( SoundEffectCache_t )this.Host.Cache.Check( s.cache );
            if ( sc != null )
                return sc;

            // load it in
            var namebuffer = "sound/" + s.name;

            var data = FileSystem.LoadFile( namebuffer );
            if ( data == null )
            {
                this.Host.Console.Print( "Couldn't load {0}\n", namebuffer );
                return null;
            }

            var info = this.GetWavInfo( s.name, data );
            if ( info.channels != 1 )
            {
                this.Host.Console.Print( "{0} is a stereo sample\n", s.name );
                return null;
            }

            var stepscale = info.rate / ( float )this._shm.speed;
            var len = ( int ) ( info.samples / stepscale );

            len *= info.width * info.channels;

            s.cache = this.Host.Cache.Alloc( len, s.name );
            if ( s.cache == null )
                return null;

            sc = new( );
            sc.length = info.samples;
            sc.loopstart = info.loopstart;
            sc.speed = info.rate;
            sc.width = info.width;
            sc.stereo = info.channels;
            s.cache.data = sc;

            this.ResampleSfx( s, sc.speed, sc.width, new( data, info.dataofs ) );

            return sc;
        }

        // SND_PickChannel
        private Channel_t PickChannel( int entnum, int entchannel )
        {
            // Check for replacement sound, or find the best one to replace
            var first_to_die = -1;
            var life_left = 0x7fffffff;
            for ( var ch_idx = AmbientDef.NUM_AMBIENTS; ch_idx < AmbientDef.NUM_AMBIENTS + snd.MAX_DYNAMIC_CHANNELS; ch_idx++ )
            {
                if ( entchannel != 0		// channel 0 never overrides
                    && this._Channels[ch_idx].entnum == entnum
                    && (this._Channels[ch_idx].entchannel == entchannel || entchannel == -1 ) )
                {
                    // allways override sound from same entity
                    first_to_die = ch_idx;
                    break;
                }

                // don't let monster sounds override player sounds
                if (this._Channels[ch_idx].entnum == this.Host.Client.cl.viewentity && entnum != this.Host.Client.cl.viewentity && this._Channels[ch_idx].sfx != null )
                    continue;

                if (this._Channels[ch_idx].end - this._PaintedTime < life_left )
                {
                    life_left = this._Channels[ch_idx].end - this._PaintedTime;
                    first_to_die = ch_idx;
                }
            }

            if ( first_to_die == -1 )
                return null;

            if (this._Channels[first_to_die].sfx != null )
                this._Channels[first_to_die].sfx = null;

            return this._Channels[first_to_die];
        }

        // S_UpdateAmbientSounds
        private void UpdateAmbientSounds( )
        {
            if ( !this._Ambient )
                return;

            // calc ambient sound levels
            if (this.Host.Client.cl.worldmodel == null )
                return;

            var l = this.Host.Client.cl.worldmodel.PointInLeaf( ref this._ListenerOrigin );
            if ( l == null || this.Host.Cvars.AmbientLevel.Get<float>( ) == 0 )
            {
                for ( var i = 0; i < AmbientDef.NUM_AMBIENTS; i++ )
                    this._Channels[i].sfx = null;
                return;
            }

            for ( var i = 0; i < AmbientDef.NUM_AMBIENTS; i++ )
            {
                var chan = this._Channels[i];
                chan.sfx = this._AmbientSfx[i];

                var vol = this.Host.Cvars.AmbientLevel.Get<float>( ) * l.ambient_sound_level[i];
                if ( vol < 8 )
                    vol = 0;

                // don't adjust volume too fast
                if ( chan.master_vol < vol )
                {
                    chan.master_vol += ( int ) (this.Host.FrameTime * this.Host.Cvars.AmbientFade.Get<float>( ) );
                    if ( chan.master_vol > vol )
                        chan.master_vol = ( int ) vol;
                }
                else if ( chan.master_vol > vol )
                {
                    chan.master_vol -= ( int ) (this.Host.FrameTime * this.Host.Cvars.AmbientFade.Get<float>( ) );
                    if ( chan.master_vol < vol )
                        chan.master_vol = ( int ) vol;
                }

                chan.leftvol = chan.rightvol = chan.master_vol;
            }
        }

        // S_Update_
        private void Update( )
        {
            if ( !this._SoundStarted || this._SoundBlocked > 0 || this._shm == null )
                return;

            // Updates DMA time
            this.GetSoundTime( );

            // check to make sure that we haven't overshot
            if (this._PaintedTime < this._SoundTime )
                this._PaintedTime = this._SoundTime;

            // mix ahead of current position
            var endtime = ( int ) (this._SoundTime + this.Host.Cvars.MixAhead.Get<float>( ) * this._shm.speed );
            var samps = this._shm.samples >> (this._shm.channels - 1 );
            if ( endtime - this._SoundTime > samps )
                endtime = this._SoundTime + samps;

            this.PaintChannels( endtime );
        }

        // GetSoundtime
        private void GetSoundTime( )
        {
            var fullsamples = this._shm.samples / this._shm.channels;
            var samplepos = this._Controller.GetPosition( );
            if ( samplepos < this._OldSamplePos )
            {
                this._Buffers++; // buffer wrapped

                if (this._PaintedTime > 0x40000000 )
                {
                    // time to chop things off to avoid 32 bit limits
                    this._Buffers = 0;
                    this._PaintedTime = fullsamples;
                    this.StopAllSounds( true );
                }
            }

            this._OldSamplePos = samplepos;
            this._SoundTime = this._Buffers * fullsamples + samplepos / this._shm.channels;
        }

        public snd( Host host )
        {
            this.Host = host;

            for ( var i = 0; i < this._KnownSfx.Length; i++ )
                this._KnownSfx[i] = new( );
        }
    }    
}
