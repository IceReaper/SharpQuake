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

namespace SharpQuake.Engine.Host
{
    using Desktop;
    using Framework.Definitions;
    using Framework.Engine;
    using Framework.Factories;
    using Framework.Factories.IO;
    using Framework.IO;
    using Framework.IO.Input;
    using Framework.IO.Wad;
    using Framework.Mathematics;
    using Framework.Networking;
    using Networking;
    using Networking.Client;
    using Networking.Server;
    using Programs;
    using Rendering;
    using Rendering.UI;
    using Sound;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;

    public partial class Host : MasterFactory
    {
        public QuakeParameters Parameters
        {
            get;
            private set;
        }

        public bool IsDedicated
        {
            get;
            private set;
        }

        public bool IsInitialised
        {
            get;
            private set;
        }

        public double Time
        {
            get;
            private set;
        }

        public int FrameCount
        {
            get;
            private set;
        }

        public bool IsDeveloper
        {
            get;
            private set;
        }

        public byte[] ColorMap
        {
            get;
            private set;
        }

        public byte[] BasePal
        {
            get;
            private set;
        }
        
        public int ClientNum => Array.IndexOf(this.Server.svs.clients, this.HostClient );

        public double RealTime
        {
            get;
            private set;
        }

        public double FrameTime
        {
            get;
            set;
        }

        public BinaryReader VcrReader
        {
            get;
            private set;
        }

        public BinaryWriter VcrWriter
        {
            get;
            private set;
        }

        public int CurrentSkill
        {
            get;
            set;
        }

        public bool NoClipAngleHack
        {
            get;
            set;
        }

        public bool IsDisposing
        {
            get;
            private set;
        }

        // Instances
        public MainWindow MainWindow
        {
            get;
            private set;
        }

        public client_t HostClient
        {
            get;
            set;
        }

        public Cache Cache
        {
            get;
            private set;
        }

        //private CommandBuffer CommandBuffer
        //{
        //    get;
        //    set;
        //}

        //private Command Command
        //{
        //    get;
        //    set;
        //}

        public View View
        {
            get;
            private set;
        }

        public ChaseView ChaseView
        {
            get;
            private set;
        }

        public Wad GfxWad
        {
            get;
            private set;
        }

        public Dictionary<string, Wad> WadFiles
        {
            get;
            private set;
        }

        public Dictionary<string, string> WadTextures
        {
            get;
            private set;
        }

        public Keyboard Keyboard
        {
            get;
            private set;
        }

        public Con Console
        {
            get;
            private set;
        }

        public Menu Menu
        {
            get;
            private set;
        }

        public Programs Programs
        {
            get;
            private set;
        }

        public ProgramsBuiltIn ProgramsBuiltIn
        {
            get;
            private set;
        }

        public Mod Model
        {
            get;
            private set;
        }

        public Network Network
        {
            get;
            private set;
        }

        public server Server
        {
            get;
            private set;
        }

        public client Client
        {
            get;
            private set;
        }

        public Vid Video
        {
            get;
            private set;
        }

        public Drawer DrawingContext
        {
            get;
            private set;
        }

        public Scr Screen
        {
            get;
            private set;
        }

        public render RenderContext
        {
            get;
            private set;
        }

        public snd Sound
        {
            get;
            private set;
        }

        public cd_audio CDAudio
        {
            get;
            private set;
        }

        public Hud Hud
        {
            get;
            private set;
		}

		public DedicatedServer DedicatedServer
		{
			get;
			private set;
		}

		// Factories
		public ClientVariableFactory CVars
        {
            get;
            private set;
        }

        public CommandFactory Commands
        {
            get;
            private set;
        }

        // CVars
        public Cvars Cvars
        {
            get;
            private set;
        }

        private double _TimeTotal; // static double timetotal from Host_Frame
        private int _TimeCount; // static int timecount from Host_Frame
        private double _OldRealTime; //double oldrealtime;	
        private double _Time1 = 0; // static double time1 from _Host_Frame
        private double _Time2 = 0; // static double time2 from _Host_Frame
        private double _Time3 = 0; // static double time3 from _Host_Frame

        private static int _ShutdownDepth;
        private static int _ErrorDepth;

        public Host( MainWindow window )
        {
            this.MainWindow = window;
            this.Cvars = new();

            // Factories
            this.Commands = this.AddFactory<CommandFactory>( );
            this.CVars = this.AddFactory<ClientVariableFactory>( );

            this.Commands.Initialise(this.CVars );

            // Old
            this.Cache = new( );
            //CommandBuffer = new CommandBuffer( this );
            //Command = new Command( this );
            //CVar.Initialise( Command );
            this.View = new( this );
            this.ChaseView = new( this );
            this.GfxWad = new( );
            this.Keyboard = new( this );
            this.Console = new( this );
            this.Menu = new( this );
            this.Programs = new( this );
            this.ProgramsBuiltIn = new( this );
            this.Model = new( this );
            this.Network = new( this );
            this.Server = new( this );
            this.Client = new( this );
            this.Video = new( this );
            this.DrawingContext = new( this );
            this.Screen = new( this );
            this.RenderContext = new( this );
            this.Sound = new( this );
            this.CDAudio = new( this );
            this.Hud = new( this );
            this.DedicatedServer = new( );

            this.WadFiles = new( );
            this.WadTextures = new( );
        }

        /// <summary>
        /// Host_ServerFrame
        /// </summary>
        public void ServerFrame( )
        {
            // run the world state
            this.Programs.GlobalStruct.frametime = ( float )this.FrameTime;

            // set the time and clear the general datagram
            this.Server.ClearDatagram( );

            // check for new clients
            this.Server.CheckForNewClients( );

            // read client messages
            this.Server.RunClients( );

            // move things around and think
            // always pause in single player if in console or menus
            if ( !this.Server.sv.paused && (this.Server.svs.maxclients > 1 || this.Keyboard.Destination == KeyDestination.key_game ) )
                this.Server.Physics( );

            // send all messages to the clients
            this.Server.SendClientMessages( );
        }

        /// <summary>
        /// host_old_ClearMemory
        /// </summary>
        public void ClearMemory( )
        {
            this.Console.DPrint( "Clearing memory\n" );

            this.Model.ClearAll( );
            this.Client.cls.signon = 0;
            this.Server.sv.Clear( );
            this.Client.cl.Clear( );
        }

        /// <summary>
        /// host_Error
        /// This shuts down both the client and server
        /// </summary>
        public void Error( string error, params object[] args )
        {
            Host._ErrorDepth++;
            try
            {
                if ( Host._ErrorDepth > 1 )
                    Utilities.Error( "host_Error: recursively entered. " + error, args );

                this.Screen.EndLoadingPlaque( );		// reenable screen updates

                var message = args.Length > 0 ? string.Format( error, args ) : error;
                this.Console.Print( "host_Error: {0}\n", message );

                if (this.Server.sv.active )
                    this.ShutdownServer( false );

                if (this.Client.cls.state == cactive_t.ca_dedicated )
                    Utilities.Error( "host_Error: {0}\n", message );	// dedicated servers exit

                this.Client.Disconnect( );
                this.Client.cls.demonum = -1;

                throw new EndGameException( ); // longjmp (host_old_abortserver, 1);
            }
            finally
            {
                Host._ErrorDepth--;
            }
        }

        public void Initialise( QuakeParameters parms )
        {
            this.Parameters = parms;

            //Command.SetupWrapper( ); // Temporary workaround - change soon!
            this.Cache.Initialise( 1024 * 1024 * 512 ); // debug

            this.Commands.Add( "flush", this.Cache.Flush );

            //CommandBuffer.Initialise( );
           // Command.Initialise( );
            this.View.Initialise( );
            this.ChaseView.Initialise( );
            this.InitialiseVCR( parms );
            MainWindow.Common.Initialise(this.MainWindow, parms.basedir, parms.argv );
            this.InitialiseLocal( );

            // Search wads
            foreach ( var wadFile in FileSystem.Search( "*.wad" ) )
            {
                if ( wadFile == "radiant.wad" )
                    continue;

                if ( wadFile == "gfx.wad" )
                    continue;

                var data = FileSystem.LoadFile( wadFile );

                if ( data == null )
                    continue;

                var wad = new Wad( );
                wad.LoadWadFile( wadFile, data );

                this.WadFiles.Add( wadFile, wad );

                var textures = wad._Lumps.Values
                    .Select( s => Encoding.ASCII.GetString( s.name ).Replace( "\0", "" ) )
                    .ToArray( );

                foreach ( var texture in textures )
                {
                    if ( !this.WadTextures.ContainsKey( texture ) )
                        this.WadTextures.Add( texture, wadFile );
                }
            }

            this.GfxWad.LoadWadFile( "gfx.wad" );
            this.Keyboard.Initialise( );
            this.Console.Initialise( );
            this.Menu.Initialise( );
            this.Programs.Initialise( );
            this.ProgramsBuiltIn.Initialise( );
            this.Model.Initialise( );
            this.Network.Initialise( );
            this.Server.Initialise( );

            //Con.Print("Exe: "__TIME__" "__DATE__"\n");
            //Con.Print("%4.1f megabyte heap\n",parms->memsize/ (1024*1024.0));

            this.RenderContext.InitTextures( );		// needed even for dedicated servers

            if (this.Client.cls.state != cactive_t.ca_dedicated )
            {
                this.BasePal = FileSystem.LoadFile( "gfx/palette.lmp" );
                if (this.BasePal == null )
                    Utilities.Error( "Couldn't load gfx/palette.lmp" );

                this.ColorMap = FileSystem.LoadFile( "gfx/colormap.lmp" );
                if (this.ColorMap == null )
                    Utilities.Error( "Couldn't load gfx/colormap.lmp" );

                // on non win32, mouse comes before video for security reasons
                MainWindow.Input.Initialise( this );
                this.Video.Initialise(this.BasePal );
                this.DrawingContext.Initialise( );
                this.Screen.Initialise( );
                this.RenderContext.Initialise( );
                this.Sound.Initialise( );
                this.CDAudio.Initialise( );
                this.Hud.Initialise( );
                this.Client.Initialise( );
            }
			else
                this.DedicatedServer.Initialise( );

            this.Commands.Buffer.Insert( "exec quake.rc\n" );

            this.IsInitialised = true;

            this.Console.DPrint( "========Quake Initialized=========\n" );
        }

        /// <summary>
        /// host_ClientCommands
        /// Send text over to the client to be executed
        /// </summary>
        public void ClientCommands( string fmt, params object[] args )
        {
            var tmp = string.Format( fmt, args );
            this.HostClient.message.WriteByte( ProtocolDef.svc_stufftext );
            this.HostClient.message.WriteString( tmp );
        }

        // Host_InitLocal
        private void InitialiseLocal( )
        {
            this.InititaliseCommands( );

            if (this.Cvars.SystemTickRate == null )
            {
                this.Cvars.SystemTickRate = this.CVars.Add( "sys_ticrate", 0.05 );
                this.Cvars.Developer = this.CVars.Add( "developer", false );
                this.Cvars.FrameRate = this.CVars.Add( "host_framerate", 0.0 ); // set for slow motion
                this.Cvars.HostSpeeds = this.CVars.Add( "host_speeds", false ); // set for running times
                this.Cvars.ServerProfile = this.CVars.Add( "serverprofile", false );
                this.Cvars.FragLimit = this.CVars.Add( "fraglimit", 0, ClientVariableFlags.Server );
                this.Cvars.TimeLimit = this.CVars.Add( "timelimit", 0, ClientVariableFlags.Server );
                this.Cvars.TeamPlay = this.CVars.Add( "teamplay", 0, ClientVariableFlags.Server );
                this.Cvars.SameLevel = this.CVars.Add( "samelevel", false );
                this.Cvars.NoExit = this.CVars.Add( "noexit", false, ClientVariableFlags.Server );
                this.Cvars.Skill = this.CVars.Add( "skill", 1 ); // 0 - 3
                this.Cvars.Deathmatch = this.CVars.Add( "deathmatch", 0 ); // 0, 1, or 2
                this.Cvars.Coop = this.CVars.Add( "coop", false );
                this.Cvars.Pausable = this.CVars.Add( "pausable", true );
                this.Cvars.Temp1 = this.CVars.Add( "temp1", 0 );
            }

            this.FindMaxClients( );

            this.Time = 1.0;		// so a think at time 0 won't get called
        }

        private void InitialiseVCR( QuakeParameters parms )
        {
            if ( CommandLine.HasParam( "-playback" ) )
            {
                if ( CommandLine.Argc != 2 )
                    Utilities.Error( "No other parameters allowed with -playback\n" );

                Stream file = FileSystem.OpenRead( "quake.vcr" );
                if ( file == null )
                    Utilities.Error( "playback file not found\n" );

                this.VcrReader = new( file, Encoding.ASCII );
                var signature = this.VcrReader.ReadInt32( );  //Sys_FileRead(vcrFile, &i, sizeof(int));
                if ( signature != HostDef.VCR_SIGNATURE )
                    Utilities.Error( "Invalid signature in vcr file\n" );

                var argc = this.VcrReader.ReadInt32( ); // Sys_FileRead(vcrFile, &com_argc, sizeof(int));
                var argv = new string[argc + 1];
                argv[0] = parms.argv[0];

                for ( var i = 1; i < argv.Length; i++ )
                    argv[i] = Utilities.ReadString(this.VcrReader );

                CommandLine.Args = argv;
                parms.argv = argv;
            }

            var n = CommandLine.CheckParm( "-record" );
            if ( n != 0 )
            {
                Stream file = FileSystem.OpenWrite( "quake.vcr" ); // vcrFile = Sys_FileOpenWrite("quake.vcr");
                this.VcrWriter = new( file, Encoding.ASCII );

                this.VcrWriter.Write( HostDef.VCR_SIGNATURE ); //  Sys_FileWrite(vcrFile, &i, sizeof(int));
                this.VcrWriter.Write( CommandLine.Argc - 1 );
                for ( var i = 1; i < CommandLine.Argc; i++ )
                {
                    if ( i == n )
                    {
                        Utilities.WriteString(this.VcrWriter, "-playback" );
                        continue;
                    }
                    Utilities.WriteString(this.VcrWriter, CommandLine.Argv( i ) );
                }
            }
        }
        /// <summary>
        /// Host_FindMaxClients
        /// </summary>
        private void FindMaxClients( )
        {
            var svs = this.Server.svs;
            var cls = this.Client.cls;

            svs.maxclients = 1;

            var i = CommandLine.CheckParm( "-dedicated" );
            if ( i > 0 )
            {
                cls.state = cactive_t.ca_dedicated;
                if ( i != CommandLine.Argc - 1 )
                    svs.maxclients = MathLib.atoi( CommandLine.Argv( i + 1 ) );
                else
                    svs.maxclients = 8;
            }
            else
                cls.state = cactive_t.ca_disconnected;

            i = CommandLine.CheckParm( "-listen" );
            if ( i > 0 )
            {
                if ( cls.state == cactive_t.ca_dedicated )
                    Utilities.Error( "Only one of -dedicated or -listen can be specified" );
                if ( i != CommandLine.Argc - 1 )
                    svs.maxclients = MathLib.atoi( CommandLine.Argv( i + 1 ) );
                else
                    svs.maxclients = 8;
            }
            if ( svs.maxclients < 1 )
                svs.maxclients = 8;
            else if ( svs.maxclients > QDef.MAX_SCOREBOARD )
                svs.maxclients = QDef.MAX_SCOREBOARD;

            svs.maxclientslimit = svs.maxclients;
            if ( svs.maxclientslimit < 4 )
                svs.maxclientslimit = 4;
            svs.clients = new client_t[svs.maxclientslimit]; // Hunk_AllocName (svs.maxclientslimit*sizeof(client_t), "clients");
            for ( i = 0; i < svs.clients.Length; i++ )
                svs.clients[i] = new( );

            if ( svs.maxclients > 1 )
                this.CVars.Set( "deathmatch", 1 );
            else
                this.CVars.Set( "deathmatch", 0 );
        }

        /// <summary>
        /// Host_FilterTime
        /// Returns false if the time is too short to run a frame
        /// </summary>
        private bool FilterTime( double time )
        {
            this.RealTime += time;

            if ( !this.Client.cls.timedemo && this.RealTime - this._OldRealTime < 1.0 / 72.0 )
                return false;	// framerate is too high

            this.FrameTime = this.RealTime - this._OldRealTime;
            this._OldRealTime = this.RealTime;

            if (this.Cvars.FrameRate.Get<double>( ) > 0 )
                this.FrameTime = this.Cvars.FrameRate.Get<double>( );
            else
            {	// don't allow really long or short frames
                if (this.FrameTime > 0.1 )
                    this.FrameTime = 0.1;
                if (this.FrameTime < 0.001 )
                    this.FrameTime = 0.001;
            }

            return true;
        }

        // _Host_Frame
        //
        //Runs all active servers
        private void InternalFrame( double time )
        {
            // keep the random time dependent
            MathLib.Random( );

            // decide the simulation time
            if ( !this.FilterTime( time ) )
                return;         // don't run too fast, or packets will flood out

			// get new key events
            this.MainWindow.SendKeyEvents( );

            // allow mice or other external controllers to add commands
            MainWindow.Input.Commands( );

            // process console commands
            this.Commands.Buffer.Execute( );

            this.Network.Poll( );

            // if running the server locally, make intentions now
            if (this.Server.sv.active )
                this.Client.SendCmd( );

            //-------------------
            //
            // server operations
            //
            //-------------------

            // check for commands typed to the host
            this.GetConsoleCommands( );

            if (this.Server.sv.active )
                this.ServerFrame( );

            //-------------------
            //
            // client operations
            //
            //-------------------

            // if running the server remotely, send intentions now after
            // the incoming messages have been read
            if ( !this.Server.sv.active )
                this.Client.SendCmd( );

            this.Time += this.FrameTime;

            // fetch results from server
            if (this.Client.cls.state == cactive_t.ca_connected )
                this.Client.ReadFromServer( );

            // update video
            if (this.Cvars.HostSpeeds.Get<bool>( ) )
                this._Time1 = Timer.GetFloatTime( );

            this.Screen.UpdateScreen( );

            if (this.Cvars.HostSpeeds.Get<bool>( ) )
                this._Time2 = Timer.GetFloatTime( );

            // update audio
            if (this.Client.cls.signon == ClientDef.SIGNONS )
            {
                this.Sound.Update( ref this.RenderContext.Origin, ref this.RenderContext.ViewPn, ref this.RenderContext.ViewRight, ref this.RenderContext.ViewUp );
                this.Client.DecayLights( );
            }
            else
                this.Sound.Update( ref Utilities.ZeroVector, ref Utilities.ZeroVector, ref Utilities.ZeroVector, ref Utilities.ZeroVector );

            this.CDAudio.Update( );

            if (this.Cvars.HostSpeeds.Get<bool>( ) )
            {
                var pass1 = ( int ) ( (this._Time1 - this._Time3 ) * 1000 );
                this._Time3 = Timer.GetFloatTime( );
                var pass2 = ( int ) ( (this._Time2 - this._Time1 ) * 1000 );
                var pass3 = ( int ) ( (this._Time3 - this._Time2 ) * 1000 );
                this.Console.Print( "{0,3} tot {1,3} server {2,3} gfx {3,3} snd\n", pass1 + pass2 + pass3, pass1, pass2, pass3 );
            }

            this.FrameCount++;
        }

        // Host_GetConsoleCommands
        //
        // Add them exactly as if they had been typed at the console
        private void GetConsoleCommands( )
        {
            while ( true )
            {
                var cmd = this.DedicatedServer.ConsoleInput( );

                if ( string.IsNullOrEmpty( cmd ) )
                    break;

                this.Commands.Buffer.Append( cmd );
            }
        }

        /// <summary>
        /// host_EndGame
        /// </summary>
        public void EndGame( string message, params object[] args )
        {
            var str = string.Format( message, args );
            this.Console.DPrint( "host_old_EndGame: {0}\n", str );

            if (this.Server.IsActive )
                this.ShutdownServer( false );

            if (this.Client.cls.state == cactive_t.ca_dedicated )
                Utilities.Error( "host_old_EndGame: {0}\n", str );	// dedicated servers exit

            if (this.Client.cls.demonum != -1 )
                this.Client.NextDemo( );
            else
                this.Client.Disconnect( );

            throw new EndGameException( );  //longjmp (host_old_abortserver, 1);
        }

        // Host_Frame
        public void Frame( double time )
        {
            if ( !this.Cvars.ServerProfile.Get<bool>( ) )
            {
                this.InternalFrame( time );
                return;
            }

            var time1 = Timer.GetFloatTime( );
            this.InternalFrame( time );
            var time2 = Timer.GetFloatTime( );

            this._TimeTotal += time2 - time1;
            this._TimeCount++;

            if (this._TimeCount < 1000 )
                return;

            var m = ( int ) (this._TimeTotal * 1000 / this._TimeCount );
            this._TimeCount = 0;
            this._TimeTotal = 0;
            var c = 0;
            foreach ( var cl in this.Server.svs.clients )
            {
                if ( cl.active )
                    c++;
            }

            this.Console.Print( "serverprofile: {0,2:d} clients {1,2:d} msec\n", c, m );
        }

        /// <summary>
        /// Host_WriteConfiguration
        /// Writes key bindings and archived cvars to config.cfg
        /// </summary>
        private void WriteConfiguration( )
        {
            // dedicated servers initialize the host but don't parse and set the
            // config.cfg cvars
            if (this.IsInitialised & !this.IsDedicated )
            {
                var path = Path.Combine( FileSystem.GameDir, "config.cfg" );

                using ( var fs = FileSystem.OpenWrite( path, true ) )
                {
                    if ( fs != null )
                    {
                        this.Keyboard.WriteBindings( fs );
                        this.CVars.WriteVariables( fs );
                    }
                }
            }
        }

        /// <summary>
        /// Host_ShutdownServer
        /// This only happens at the end of a game, not between levels
        /// </summary>
        public void ShutdownServer( bool crash )
        {
            if ( !this.Server.IsActive )
                return;

            this.Server.sv.active = false;

            // stop all client sounds immediately
            if (this.Client.cls.state == cactive_t.ca_connected )
                this.Client.Disconnect( );

            // flush any pending messages - like the score!!!
            var start = Timer.GetFloatTime( );
            int count;
            do
            {
                count = 0;
                for ( var i = 0; i < this.Server.svs.maxclients; i++ )
                {
                    this.HostClient = this.Server.svs.clients[i];
                    if (this.HostClient.active && !this.HostClient.message.IsEmpty )
                    {
                        if (this.Network.CanSendMessage(this.HostClient.netconnection ) )
                        {
                            this.Network.SendMessage(this.HostClient.netconnection, this.HostClient.message );
                            this.HostClient.message.Clear( );
                        }
                        else
                        {
                            this.Network.GetMessage(this.HostClient.netconnection );
                            count++;
                        }
                    }
                }
                if ( Timer.GetFloatTime( ) - start > 3.0 )
                    break;
            }
            while ( count > 0 );

            // make sure all the clients know we're disconnecting
            var writer = new MessageWriter( 4 );
            writer.WriteByte( ProtocolDef.svc_disconnect );
            count = this.Network.SendToAll( writer, 5 );

            if ( count != 0 )
                this.Console.Print( "Host_ShutdownServer: NET_SendToAll failed for {0} clients\n", count );

            for ( var i = 0; i < this.Server.svs.maxclients; i++ )
            {
                this.HostClient = this.Server.svs.clients[i];

                if (this.HostClient.active )
                    this.Server.DropClient( crash );
            }

            //
            // clear structures
            //
            this.Server.sv.Clear( );

            for ( var i = 0; i < this.Server.svs.clients.Length; i++ )
                this.Server.svs.clients[i].Clear( );
        }

        /// <summary>
        /// Host_Shutdown
        /// </summary>
        public override void Dispose( )
        {
            this.IsDisposing = true;

            Host._ShutdownDepth++;
            try
            {
                if ( Host._ShutdownDepth > 1 )
                    return;

                // keep Con_Printf from trying to update the screen
                this.Screen.IsDisabledForLoading = true;

                this.WriteConfiguration( );

                this.CDAudio.Shutdown( );
                this.Network.Shutdown( );
                this.Sound.Shutdown( );
                MainWindow.Input.Shutdown( );

                if (this.VcrWriter != null )
                {
                    this.Console.Print( "Closing vcrfile.\n" );
                    this.VcrWriter.Close( );
                    this.VcrWriter = null;
                }
                if (this.VcrReader != null )
                {
                    this.Console.Print( "Closing vcrfile.\n" );
                    this.VcrReader.Close( );
                    this.VcrReader = null;
                }

                if (this.Client.cls.state != cactive_t.ca_dedicated )
                    this.Video.Shutdown( );

                this.Console.Shutdown( );

                base.Dispose( );
            }
            finally
            {
                Host._ShutdownDepth--;

                // Hack to close process property
               // Environment.Exit( 0 );
            }
        }
    }
}
