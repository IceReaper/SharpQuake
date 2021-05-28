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
    using Framework.IO;
    using Framework.IO.Input;
    using Framework.Mathematics;
    using Game.Data.Models;
    using Networking.Client;
    using Rendering.UI.Menus;
    using System;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Text;

    public partial class Host
    {
        public uint FPSCounter = 0;
        public uint FPS = 0;
        public DateTime LastFPSUpdate;

        public bool ShowFPS
        {
            get;
            private set;
        }

        public void ShowFPS_f( CommandMessage msg )
        {
            this.ShowFPS = !this.ShowFPS;
        }

        /// <summary>
        /// Host_Quit_f
        /// </summary>
        public void Quit_f( CommandMessage msg )
        {
            if (this.Keyboard.Destination != KeyDestination.key_console && this.Client.cls.state != cactive_t.ca_dedicated )
            {
                MenuBase.QuitMenuInstance.Show( this );
                return;
            }

            this.Client.Disconnect( );
            this.ShutdownServer( false );
            this.MainWindow.Quit( );
        }

        /// <summary>
        /// Host_InitCommands
        /// </summary>
        private void InititaliseCommands( )
        {
            this.Commands.Add( "status", this.Status_f );
            this.Commands.Add( "quit", this.Quit_f );
            this.Commands.Add( "god", this.God_f );
            this.Commands.Add( "notarget", this.Notarget_f );
            this.Commands.Add( "fly", this.Fly_f );
            this.Commands.Add( "map", this.Map_f );
            this.Commands.Add( "restart", this.Restart_f );
            this.Commands.Add( "changelevel", this.Changelevel_f );
            this.Commands.Add( "connect", this.Connect_f );
            this.Commands.Add( "reconnect", this.Reconnect_f );
            this.Commands.Add( "name", this.Name_f );
            this.Commands.Add( "noclip", this.Noclip_f );
            this.Commands.Add( "version", this.Version_f );
            this.Commands.Add( "say", this.Say_f );
            this.Commands.Add( "say_team", this.Say_Team_f );
            this.Commands.Add( "tell", this.Tell_f );
            this.Commands.Add( "color", this.Color_f );
            this.Commands.Add( "kill", this.Kill_f );
            this.Commands.Add( "pause", this.Pause_f );
            this.Commands.Add( "spawn", this.Spawn_f );
            this.Commands.Add( "begin", this.Begin_f );
            this.Commands.Add( "prespawn", this.PreSpawn_f );
            this.Commands.Add( "kick", this.Kick_f );
            this.Commands.Add( "ping", this.Ping_f );
            this.Commands.Add( "load", this.Loadgame_f );
            this.Commands.Add( "save", this.Savegame_f );
            this.Commands.Add( "give", this.Give_f );

            this.Commands.Add( "startdemos", this.Startdemos_f );
            this.Commands.Add( "demos", this.Demos_f );
            this.Commands.Add( "stopdemo", this.Stopdemo_f );

            this.Commands.Add( "viewmodel", this.Viewmodel_f );
            this.Commands.Add( "viewframe", this.Viewframe_f );
            this.Commands.Add( "viewnext", this.Viewnext_f );
            this.Commands.Add( "viewprev", this.Viewprev_f );

            this.Commands.Add( "mcache", this.Model.Print );

            // New
            this.Commands.Add( "showfps", this.ShowFPS_f );
        }

        /// <summary>
        /// Host_Viewmodel_f
        /// </summary>
        /// <param name="msg"></param>
        private void Viewmodel_f( CommandMessage msg )
        {
            var e = this.FindViewthing( );
            if ( e == null )
                return;

            var m = this.Model.ForName( msg.Parameters[0], false, ModelType.mod_alias );
            if ( m == null )
            {
                this.Console.Print( "Can't load {0}\n", msg.Parameters[0] );
                return;
            }

            e.v.frame = 0;
            this.Client.cl.model_precache[( int ) e.v.modelindex] = m;
        }

        /// <summary>
        /// Host_Viewframe_f
        /// </summary>
        private void Viewframe_f( CommandMessage msg )
        {
            var e = this.FindViewthing( );
            if ( e == null )
                return;

            var m = this.Client.cl.model_precache[( int ) e.v.modelindex];

            var f = MathLib.atoi( msg.Parameters[0] );
            if ( f >= m.FrameCount )
                f = m.FrameCount - 1;

            e.v.frame = f;
        }

        private void PrintFrameName( ModelData m, int frame )
        {
            var hdr = this.Model.GetExtraData( m );
            if ( hdr == null )
                return;

            this.Console.Print( "frame {0}: {1}\n", frame, hdr.frames[frame].name );
        }

        /// <summary>
        /// Host_Viewnext_f
        /// </summary>
        private void Viewnext_f( CommandMessage msg )
        {
            var e = this.FindViewthing( );
            if ( e == null )
                return;

            var m = this.Client.cl.model_precache[( int ) e.v.modelindex];

            e.v.frame = e.v.frame + 1;
            if ( e.v.frame >= m.FrameCount )
                e.v.frame = m.FrameCount - 1;

            this.PrintFrameName( m, ( int ) e.v.frame );
        }

        /// <summary>
        /// Host_Viewprev_f
        /// </summary>
        private void Viewprev_f( CommandMessage msg )
        {
            var e = this.FindViewthing( );
            if ( e == null )
                return;

            var m = this.Client.cl.model_precache[( int ) e.v.modelindex];

            e.v.frame = e.v.frame - 1;
            if ( e.v.frame < 0 )
                e.v.frame = 0;

            this.PrintFrameName( m, ( int ) e.v.frame );
        }

        /// <summary>
        /// Host_Status_f
        /// </summary>
        private void Status_f( CommandMessage msg )
        {
            var flag = true;
            if ( msg.Source == CommandSource.Command )
            {
                if ( !this.Server.sv.active )
                {
                    this.Client.ForwardToServer_f( msg );
                    return;
                }
            }
            else
                flag = false;

            var sb = new StringBuilder( 256 );
            sb.Append( string.Format( "host:    {0}\n", this.CVars.Get( "hostname" ).Get<string>( ) ) );
            sb.Append( string.Format( "version: {0:F2}\n", QDef.VERSION ) );
            if (this.Network.TcpIpAvailable )
            {
                sb.Append( "tcp/ip:  " );
                sb.Append(this.Network.MyTcpIpAddress );
                sb.Append( '\n' );
            }

            sb.Append( "map:     " );
            sb.Append(this.Server.sv.name );
            sb.Append( '\n' );
            sb.Append( string.Format( "players: {0} active ({1} max)\n\n", this.Network.ActiveConnections, this.Server.svs.maxclients ) );
            for ( var j = 0; j < this.Server.svs.maxclients; j++ )
            {
                var client = this.Server.svs.clients[j];
                if ( !client.active )
                    continue;

                var seconds = ( int ) (this.Network.Time - client.netconnection.connecttime );
                int hours, minutes = seconds / 60;
                if ( minutes > 0 )
                {
                    seconds -= minutes * 60;
                    hours = minutes / 60;
                    if ( hours > 0 )
                        minutes -= hours * 60;
                }
                else
                    hours = 0;
                sb.Append( string.Format( "#{0,-2} {1,-16}  {2}  {2}:{4,2}:{5,2}",
                    j + 1, client.name, ( int ) client.edict.v.frags, hours, minutes, seconds ) );
                sb.Append( "   " );
                sb.Append( client.netconnection.address );
                sb.Append( '\n' );
            }

            if ( flag )
                this.Console.Print( sb.ToString( ) );
            else
                this.Server.ClientPrint( sb.ToString( ) );
        }

        /// <summary>
        /// Host_God_f
        /// Sets client to godmode
        /// </summary>
        private void God_f( CommandMessage msg )
        {
            if ( msg.Source == CommandSource.Command )
            {
                this.Client.ForwardToServer_f( msg );
                return;
            }

            if (this.Programs.GlobalStruct.deathmatch != 0 && !this.HostClient.privileged )
                return;

            this.Server.Player.v.flags = ( int )this.Server.Player.v.flags ^ EdictFlags.FL_GODMODE;
            if ( ( ( int )this.Server.Player.v.flags & EdictFlags.FL_GODMODE ) == 0 )
                this.Server.ClientPrint( "godmode OFF\n" );
            else
                this.Server.ClientPrint( "godmode ON\n" );
        }

        /// <summary>
        /// Host_Notarget_f
        /// </summary>
        private void Notarget_f( CommandMessage msg )
        {
            if ( msg.Source == CommandSource.Command )
            {
                this.Client.ForwardToServer_f( msg );
                return;
            }

            if (this.Programs.GlobalStruct.deathmatch != 0 && !this.HostClient.privileged )
                return;

            this.Server.Player.v.flags = ( int )this.Server.Player.v.flags ^ EdictFlags.FL_NOTARGET;
            if ( ( ( int )this.Server.Player.v.flags & EdictFlags.FL_NOTARGET ) == 0 )
                this.Server.ClientPrint( "notarget OFF\n" );
            else
                this.Server.ClientPrint( "notarget ON\n" );
        }

        /// <summary>
        /// Host_Noclip_f
        /// </summary>
        private void Noclip_f( CommandMessage msg )
        {
            if ( msg.Source == CommandSource.Command )
            {
                this.Client.ForwardToServer_f( msg );
                return;
            }

            if (this.Programs.GlobalStruct.deathmatch > 0 && !this.HostClient.privileged )
                return;

            if (this.Server.Player.v.movetype != Movetypes.MOVETYPE_NOCLIP )
            {
                this.NoClipAngleHack = true;
                this.Server.Player.v.movetype = Movetypes.MOVETYPE_NOCLIP;
                this.Server.ClientPrint( "noclip ON\n" );
            }
            else
            {
                this.NoClipAngleHack = false;
                this.Server.Player.v.movetype = Movetypes.MOVETYPE_WALK;
                this.Server.ClientPrint( "noclip OFF\n" );
            }
        }

        /// <summary>
        /// Host_Fly_f
        /// Sets client to flymode
        /// </summary>
        private void Fly_f( CommandMessage msg )
        {
            if ( msg.Source == CommandSource.Command )
            {
                this.Client.ForwardToServer_f( msg );
                return;
            }

            if (this.Programs.GlobalStruct.deathmatch > 0 && !this.HostClient.privileged )
                return;

            if (this.Server.Player.v.movetype != Movetypes.MOVETYPE_FLY )
            {
                this.Server.Player.v.movetype = Movetypes.MOVETYPE_FLY;
                this.Server.ClientPrint( "flymode ON\n" );
            }
            else
            {
                this.Server.Player.v.movetype = Movetypes.MOVETYPE_WALK;
                this.Server.ClientPrint( "flymode OFF\n" );
            }
        }

        /// <summary>
        /// Host_Ping_f
        /// </summary>
        private void Ping_f( CommandMessage msg )
        {
            if ( msg.Source == CommandSource.Command )
            {
                this.Client.ForwardToServer_f( msg );
                return;
            }

            this.Server.ClientPrint( "Client ping times:\n" );
            for ( var i = 0; i < this.Server.svs.maxclients; i++ )
            {
                var client = this.Server.svs.clients[i];
                if ( !client.active )
                    continue;
                float total = 0;
                for ( var j = 0; j < ServerDef.NUM_PING_TIMES; j++ )
                    total += client.ping_times[j];
                total /= ServerDef.NUM_PING_TIMES;
                this.Server.ClientPrint( "{0,4} {1}\n", ( int ) ( total * 1000 ), client.name );
            }
        }

        /// <summary>
        /// Host_Map_f
        ///
        /// handle a
        /// map [servername]
        /// command from the console.  Active clients are kicked off.
        /// </summary>
        /// <param name="msg"></param>
        private void Map_f( CommandMessage msg )
        {
            if ( msg.Source != CommandSource.Command )
                return;

            this.Client.cls.demonum = -1;		// stop demo loop in case this fails

            this.Client.Disconnect( );
            this.ShutdownServer( false );

            this.Keyboard.Destination = KeyDestination.key_game;			// remove console or menu
            this.Screen.BeginLoadingPlaque( );

            this.Client.cls.mapstring = msg.FullCommand + "\n";

            this.Server.svs.serverflags = 0;			// haven't completed an episode yet
            var name = msg.Parameters[0];
            this.Server.SpawnServer( name );

            if ( !this.Server.IsActive )
                return;

            if (this.Client.cls.state != cactive_t.ca_dedicated )
            {
                this.Client.cls.spawnparms = msg.FullCommand;
                this.Commands.ExecuteString( "connect local", CommandSource.Command );
            }
        }

        /// <summary>
        /// Host_Changelevel_f
        /// Goes to a new map, taking all clients along
        /// </summary>
        private void Changelevel_f( CommandMessage msg )
        {
            if ( msg.Parameters == null || msg.Parameters.Length != 1 )
            {
                this.Console.Print( "changelevel <levelname> : continue game on a new level\n" );
                return;
            }
            if ( !this.Server.sv.active || this.Client.cls.demoplayback )
            {
                this.Console.Print( "Only the server may changelevel\n" );
                return;
            }

            this.Server.SaveSpawnparms( );
            var level = msg.Parameters[0];
            this.Server.SpawnServer( level );
        }

        // Host_Restart_f
        //
        // Restarts the current server for a dead player
        private void Restart_f( CommandMessage msg )
        {
            if (this.Client.cls.demoplayback || !this.Server.IsActive )
                return;

            if ( msg.Source != CommandSource.Command )
                return;

            var mapname = this.Server.sv.name; // must copy out, because it gets cleared
                                          // in sv_spawnserver
            this.Server.SpawnServer( mapname );
        }

        /// <summary>
        /// Host_Reconnect_f
        /// This command causes the client to wait for the signon messages again.
        /// This is sent just before a server changes levels
        /// </summary>
        private void Reconnect_f( CommandMessage msg )
        {
            this.Screen.BeginLoadingPlaque( );
            this.Client.cls.signon = 0;		// need new connection messages
        }

        /// <summary>
        /// Host_Connect_f
        /// User command to connect to server
        /// </summary>
        private void Connect_f( CommandMessage msg )
        {
            this.Client.cls.demonum = -1;		// stop demo loop in case this fails
            if (this.Client.cls.demoplayback )
            {
                this.Client.StopPlayback( );
                this.Client.Disconnect( );
            }
            var name = msg.Parameters[0];
            this.Client.EstablishConnection( name );
            this.Reconnect_f( null );
        }

        /// <summary>
        /// Host_SavegameComment
        /// Writes a SAVEGAME_COMMENT_LENGTH character comment describing the current
        /// </summary>
        private string SavegameComment( )
        {
            var result = string.Format( "{0} kills:{1,3}/{2,3}",
                this.Client.cl.levelname,
                this.Client.cl.stats[QStatsDef.STAT_MONSTERS],
                this.Client.cl.stats[QStatsDef.STAT_TOTALMONSTERS] );

            // convert space to _ to make stdio happy
            result = result.Replace( ' ', '_' );

            if ( result.Length < QDef.SAVEGAME_COMMENT_LENGTH - 1 )
                result = result.PadRight( QDef.SAVEGAME_COMMENT_LENGTH - 1, '_' );

            if ( result.Length > QDef.SAVEGAME_COMMENT_LENGTH - 1 )
                result = result.Remove( QDef.SAVEGAME_COMMENT_LENGTH - 2 );

            return result + '\0';
        }

        /// <summary>
        /// Host_Savegame_f
        /// </summary>
        private void Savegame_f( CommandMessage msg )
        {
            if ( msg.Source != CommandSource.Command )
                return;

            if ( !this.Server.sv.active )
            {
                this.Console.Print( "Not playing a local game.\n" );
                return;
            }

            if (this.Client.cl.intermission != 0 )
            {
                this.Console.Print( "Can't save in intermission.\n" );
                return;
            }

            if (this.Server.svs.maxclients != 1 )
            {
                this.Console.Print( "Can't save multiplayer games.\n" );
                return;
            }

            if ( msg.Parameters == null || msg.Parameters.Length != 1 )
            {
                this.Console.Print( "save <savename> : save a game\n" );
                return;
            }

            if ( msg.Parameters[0].Contains( ".." ) )
            {
                this.Console.Print( "Relative pathnames are not allowed.\n" );
                return;
            }

            for ( var i = 0; i < this.Server.svs.maxclients; i++ )
            {
                if (this.Server.svs.clients[i].active && this.Server.svs.clients[i].edict.v.health <= 0 )
                {
                    this.Console.Print( "Can't savegame with a dead player\n" );
                    return;
                }
            }

            var name = Path.ChangeExtension( Path.Combine( FileSystem.GameDir, msg.Parameters[0] ), ".sav" );

            this.Console.Print( "Saving game to {0}...\n", name );
            var fs = FileSystem.OpenWrite( name, true );
            if ( fs == null )
            {
                this.Console.Print( "ERROR: couldn't open.\n" );
                return;
            }
            using ( var writer = new StreamWriter( fs, Encoding.ASCII ) )
            {
                writer.WriteLine( HostDef.SAVEGAME_VERSION );
                writer.WriteLine(this.SavegameComment( ) );

                for ( var i = 0; i < ServerDef.NUM_SPAWN_PARMS; i++ )
                {
                    writer.WriteLine(
                        this.Server.svs.clients[0].spawn_parms[i].ToString( "F6",
                            CultureInfo.InvariantCulture.NumberFormat ) );
                }

                writer.WriteLine(this.CurrentSkill );
                writer.WriteLine(this.Server.sv.name );
                writer.WriteLine(
                    this.Server.sv.time.ToString( "F6",
                    CultureInfo.InvariantCulture.NumberFormat ) );

                // write the light styles

                for ( var i = 0; i < QDef.MAX_LIGHTSTYLES; i++ )
                {
                    if ( !string.IsNullOrEmpty(this.Server.sv.lightstyles[i] ) )
                        writer.WriteLine(this.Server.sv.lightstyles[i] );
                    else
                        writer.WriteLine( "m" );
                }

                this.Programs.WriteGlobals( writer );
                for ( var i = 0; i < this.Server.sv.num_edicts; i++ )
                {
                    this.Programs.WriteEdict( writer, this.Server.EdictNum( i ) );
                    writer.Flush( );
                }
            }

            this.Console.Print( "done.\n" );
        }

        /// <summary>
        /// Host_Loadgame_f
        /// </summary>
        private void Loadgame_f( CommandMessage msg )
        {
            if ( msg.Source != CommandSource.Command )
                return;

            if ( msg.Parameters == null || msg.Parameters.Length != 1 )
            {
                this.Console.Print( "load <savename> : load a game\n" );
                return;
            }

            this.Client.cls.demonum = -1;		// stop demo loop in case this fails

            var name = Path.ChangeExtension( Path.Combine( FileSystem.GameDir, msg.Parameters[0] ), ".sav" );

            // we can't call SCR_BeginLoadingPlaque, because too much stack space has
            // been used.  The menu calls it before stuffing loadgame command
            //	SCR_BeginLoadingPlaque ();

            this.Console.Print( "Loading game from {0}...\n", name );
            var fs = FileSystem.OpenRead( name );
            if ( fs == null )
            {
                this.Console.Print( "ERROR: couldn't open.\n" );
                return;
            }

            using ( var reader = new StreamReader( fs, Encoding.ASCII ) )
            {
                var line = reader.ReadLine( );
                var version = MathLib.atoi( line );
                if ( version != HostDef.SAVEGAME_VERSION )
                {
                    this.Console.Print( "Savegame is version {0}, not {1}\n", version, HostDef.SAVEGAME_VERSION );
                    return;
                }
                line = reader.ReadLine( );

                var spawn_parms = new float[ServerDef.NUM_SPAWN_PARMS];
                for ( var i = 0; i < spawn_parms.Length; i++ )
                {
                    line = reader.ReadLine( );
                    spawn_parms[i] = MathLib.atof( line );
                }
                // this silliness is so we can load 1.06 save files, which have float skill values
                line = reader.ReadLine( );
                var tfloat = MathLib.atof( line );
                this.CurrentSkill = ( int ) ( tfloat + 0.1 );
                this.CVars.Set( "skill", ( float )this.CurrentSkill );

                var mapname = reader.ReadLine( );
                line = reader.ReadLine( );
                var time = MathLib.atof( line );

                this.Client.Disconnect_f( null );
                this.Server.SpawnServer( mapname );

                if ( !this.Server.sv.active )
                {
                    this.Console.Print( "Couldn't load map\n" );
                    return;
                }

                this.Server.sv.paused = true;		// pause until all clients connect
                this.Server.sv.loadgame = true;

                // load the light styles

                for ( var i = 0; i < QDef.MAX_LIGHTSTYLES; i++ )
                {
                    line = reader.ReadLine( );
                    this.Server.sv.lightstyles[i] = line;
                }

                // load the edicts out of the savegame file
                var entnum = -1;		// -1 is the globals
                var sb = new StringBuilder( 32768 );
                while ( !reader.EndOfStream )
                {
                    line = reader.ReadLine( );
                    if ( line == null )
                        Utilities.Error( "EOF without closing brace" );

                    sb.AppendLine( line );
                    var idx = line.IndexOf( '}' );
                    if ( idx != -1 )
                    {
                        var length = 1 + sb.Length - ( line.Length - idx );
                        var data = Tokeniser.Parse( sb.ToString( 0, length ) );
                        if ( string.IsNullOrEmpty( Tokeniser.Token ) )
                            break; // end of file
                        if ( Tokeniser.Token != "{" )
                            Utilities.Error( "First token isn't a brace" );

                        if ( entnum == -1 )
                        {
                            // parse the global vars
                            this.Programs.ParseGlobals( data );
                        }
                        else
                        {
                            // parse an edict
                            var ent = this.Server.EdictNum( entnum );
                            ent.Clear( );
                            this.Programs.ParseEdict( data, ent );

                            // link it into the bsp tree
                            if ( !ent.free )
                                this.Server.LinkEdict( ent, false );
                        }

                        entnum++;
                        sb.Remove( 0, length );
                    }
                }

                this.Server.sv.num_edicts = entnum;
                this.Server.sv.time = time;

                for ( var i = 0; i < ServerDef.NUM_SPAWN_PARMS; i++ )
                    this.Server.svs.clients[0].spawn_parms[i] = spawn_parms[i];
            }

            if (this.Client.cls.state != cactive_t.ca_dedicated )
            {
                this.Client.EstablishConnection( "local" );
                this.Reconnect_f( null );
            }
        }

        /// <summary>
        /// Host_Name_f
        /// </summary>
        /// <param name="msg"></param>
        private void Name_f( CommandMessage msg )
        {
            if ( msg.Parameters == null || msg.Parameters.Length <= 0 )
            {
                this.Console.Print( "\"name\" is \"{0}\"\n", this.Client.Name );
                return;
            }

            string newName;
            if ( msg.Parameters.Length == 1 )
                newName = msg.Parameters[0];
            else
                newName = msg.StringParameters;

            if ( newName.Length > 16 )
                newName = newName.Remove( 15 );

            if ( msg.Source == CommandSource.Command )
            {
                if (this.Client.Name == newName )
                    return;

                this.CVars.Set( "_cl_name", newName );
                if (this.Client.cls.state == cactive_t.ca_connected )
                    this.Client.ForwardToServer_f( msg );
                return;
            }

            if ( !string.IsNullOrEmpty(this.HostClient.name ) && this.HostClient.name != "unconnected" )
            {
                if (this.HostClient.name != newName )
                    this.Console.Print( "{0} renamed to {1}\n", this.HostClient.name, newName );
            }

            this.HostClient.name = newName;
            this.HostClient.edict.v.netname = this.Programs.NewString( newName );

            // send notification to all clients
            var m = this.Server.sv.reliable_datagram;
            m.WriteByte( ProtocolDef.svc_updatename );
            m.WriteByte(this.ClientNum );
            m.WriteString( newName );
        }

        /// <summary>
        /// Host_Version_f
        /// </summary>
        /// <param name="msg"></param>
        private void Version_f( CommandMessage msg )
        {
            this.Console.Print( "Version {0}\n", QDef.VERSION );
            this.Console.Print( "Exe hash code: {0}\n", Assembly.GetExecutingAssembly( ).GetHashCode( ) );
        }

        /// <summary>
        /// Host_Say
        /// </summary>
        private void Say( CommandMessage msg, bool teamonly )
        {
            var fromServer = false;
            if ( msg.Source == CommandSource.Command )
            {
                if (this.Client.cls.state == cactive_t.ca_dedicated )
                {
                    fromServer = true;
                    teamonly = false;
                }
                else
                {
                    this.Client.ForwardToServer_f( msg );
                    return;
                }
            }

            if ( msg.Parameters == null || msg.Parameters.Length < 1 )
                return;

            var save = this.HostClient;

            var p = msg.StringParameters;
            // remove quotes if present
            if ( p.StartsWith( "\"" ) )
                p = p.Substring( 1, p.Length - 2 );

            // turn on color set 1
            string text;
            if ( !fromServer )
                text = ( char ) 1 + save.name + ": ";
            else
                text = ( char ) 1 + "<" + this.Network.HostName + "> ";

            text += p + "\n";

            for ( var j = 0; j < this.Server.svs.maxclients; j++ )
            {
                var client = this.Server.svs.clients[j];
                if ( client == null || !client.active || !client.spawned )
                    continue;
                if (this.Cvars.TeamPlay.Get<int>( ) != 0 && teamonly && client.edict.v.team != save.edict.v.team )
                    continue;

                this.HostClient = client;
                this.Server.ClientPrint( text );
            }

            this.HostClient = save;
        }

        /// <summary>
        /// Host_Say_f
        /// </summary>
        /// <param name="msg"></param>
        private void Say_f( CommandMessage msg )
        {
            this.Say( msg, false );
        }

        /// <summary>
        /// Host_Say_Team_f
        /// </summary>
        /// <param name="msg"></param>
        private void Say_Team_f( CommandMessage msg )
        {
            this.Say( msg, true );
        }

        /// <summary>
        /// Host_Tell_f
        /// </summary>
        /// <param name="msg"></param>
        private void Tell_f( CommandMessage msg )
        {
            if ( msg.Source == CommandSource.Command )
            {
                this.Client.ForwardToServer_f( msg );
                return;
            }

            if ( msg.Parameters == null || msg.Parameters.Length < 2 )
                return;

            var text = this.HostClient.name + ": ";
            var p = msg.StringParameters;

            // remove quotes if present
            if ( p.StartsWith( "\"" ) )
                p = p.Substring( 1, p.Length - 2 );

            text += p + "\n";

            var save = this.HostClient;
            for ( var j = 0; j < this.Server.svs.maxclients; j++ )
            {
                var client = this.Server.svs.clients[j];
                if ( !client.active || !client.spawned )
                    continue;
                if ( client.name == msg.Parameters[0] )
                    continue;

                this.HostClient = client;
                this.Server.ClientPrint( text );
                break;
            }

            this.HostClient = save;
        }

        /// <summary>
        /// Host_Color_f
        /// </summary>
        /// <param name="msg"></param>
        private void Color_f( CommandMessage msg )
        {
            if ( msg.Parameters == null || msg.Parameters.Length <= 0 )
            {
                this.Console.Print( "\"color\" is \"{0} {1}\"\n", ( int )this.Client.Color >> 4, ( int )this.Client.Color & 0x0f );
                this.Console.Print( "color <0-13> [0-13]\n" );
                return;
            }

            int top, bottom;
            if ( msg.Parameters?.Length == 1 )
                top = bottom = MathLib.atoi( msg.Parameters[0] );
            else
            {
                top = MathLib.atoi( msg.Parameters[0] );
                bottom = MathLib.atoi( msg.Parameters[1] );
            }

            top &= 15;
            if ( top > 13 )
                top = 13;
            bottom &= 15;
            if ( bottom > 13 )
                bottom = 13;

            var playercolor = top * 16 + bottom;

            if ( msg.Source == CommandSource.Command )
            {
                this.CVars.Set( "_cl_color", playercolor );
                if (this.Client.cls.state == cactive_t.ca_connected )
                    this.Client.ForwardToServer_f( msg );
                return;
            }

            this.HostClient.colors = playercolor;
            this.HostClient.edict.v.team = bottom + 1;

            // send notification to all clients
            var m = this.Server.sv.reliable_datagram;
            m.WriteByte( ProtocolDef.svc_updatecolors );
            m.WriteByte(this.ClientNum );
            m.WriteByte(this.HostClient.colors );
        }

        /// <summary>
        /// Host_Kill_f
        /// </summary>
        private void Kill_f( CommandMessage msg )
        {
            if ( msg.Source == CommandSource.Command )
            {
                this.Client.ForwardToServer_f( msg );
                return;
            }

            if (this.Server.Player.v.health <= 0 )
            {
                this.Server.ClientPrint( "Can't suicide -- allready dead!\n" );
                return;
            }

            this.Programs.GlobalStruct.time = ( float )this.Server.sv.time;
            this.Programs.GlobalStruct.self = this.Server.EdictToProg(this.Server.Player );
            this.Programs.Execute(this.Programs.GlobalStruct.ClientKill );
        }

        /// <summary>
        /// Host_Pause_f
        /// </summary>
        private void Pause_f( CommandMessage msg )
        {
            if ( msg.Source == CommandSource.Command )
            {
                this.Client.ForwardToServer_f( msg );
                return;
            }
            if ( !this.Cvars.Pausable.Get<bool>( ) )
                this.Server.ClientPrint( "Pause not allowed.\n" );
            else
            {
                this.Server.sv.paused = !this.Server.sv.paused;

                if (this.Server.sv.paused )
                    this.Server.BroadcastPrint( "{0} paused the game\n", this.Programs.GetString(this.Server.Player.v.netname ) );
                else
                    this.Server.BroadcastPrint( "{0} unpaused the game\n", this.Programs.GetString(this.Server.Player.v.netname ) );

                // send notification to all clients
                this.Server.sv.reliable_datagram.WriteByte( ProtocolDef.svc_setpause );
                this.Server.sv.reliable_datagram.WriteByte(this.Server.sv.paused ? 1 : 0 );
            }
        }

        /// <summary>
        /// Host_PreSpawn_f
        /// </summary>
        private void PreSpawn_f( CommandMessage msg )
        {
            if ( msg.Source == CommandSource.Command )
            {
                this.Console.Print( "prespawn is not valid from the console\n" );
                return;
            }

            if (this.HostClient.spawned )
            {
                this.Console.Print( "prespawn not valid -- allready spawned\n" );
                return;
            }

            var m = this.HostClient.message;
            m.Write(this.Server.sv.signon.Data, 0, this.Server.sv.signon.Length );
            m.WriteByte( ProtocolDef.svc_signonnum );
            m.WriteByte( 2 );
            this.HostClient.sendsignon = true;
        }

        /// <summary>
        /// Host_Spawn_f
        /// </summary>
        private void Spawn_f( CommandMessage msg )
        {
            if ( msg.Source == CommandSource.Command )
            {
                this.Console.Print( "spawn is not valid from the console\n" );
                return;
            }

            if (this.HostClient.spawned )
            {
                this.Console.Print( "Spawn not valid -- allready spawned\n" );
                return;
            }

            MemoryEdict ent;

            // run the entrance script
            if (this.Server.sv.loadgame )
            {
                // loaded games are fully inited allready
                // if this is the last client to be connected, unpause
                this.Server.sv.paused = false;
            }
            else
            {
                // set up the edict
                ent = this.HostClient.edict;

                ent.Clear( ); //memset(&ent.v, 0, Programs.entityfields * 4);
                ent.v.colormap = this.Server.NumForEdict( ent );
                ent.v.team = (this.HostClient.colors & 15 ) + 1;
                ent.v.netname = this.Programs.NewString(this.HostClient.name );

                // copy spawn parms out of the client_t
                this.Programs.GlobalStruct.SetParams(this.HostClient.spawn_parms );

                // call the spawn function

                this.Programs.GlobalStruct.time = ( float )this.Server.sv.time;
                this.Programs.GlobalStruct.self = this.Server.EdictToProg(this.Server.Player );
                this.Programs.Execute(this.Programs.GlobalStruct.ClientConnect );

                if ( Timer.GetFloatTime( ) - this.HostClient.netconnection.connecttime <= this.Server.sv.time )
                    this.Console.DPrint( "{0} entered the game\n", this.HostClient.name );

                this.Programs.Execute(this.Programs.GlobalStruct.PutClientInServer );
            }

            // send all current names, colors, and frag counts
            var m = this.HostClient.message;
            m.Clear( );

            // send time of update
            m.WriteByte( ProtocolDef.svc_time );
            m.WriteFloat( ( float )this.Server.sv.time );

            for ( var i = 0; i < this.Server.svs.maxclients; i++ )
            {
                var client = this.Server.svs.clients[i];
                m.WriteByte( ProtocolDef.svc_updatename );
                m.WriteByte( i );
                m.WriteString( client.name );
                m.WriteByte( ProtocolDef.svc_updatefrags );
                m.WriteByte( i );
                m.WriteShort( client.old_frags );
                m.WriteByte( ProtocolDef.svc_updatecolors );
                m.WriteByte( i );
                m.WriteByte( client.colors );
            }

            // send all current light styles
            for ( var i = 0; i < QDef.MAX_LIGHTSTYLES; i++ )
            {
                m.WriteByte( ProtocolDef.svc_lightstyle );
                m.WriteByte( ( char ) i );
                m.WriteString(this.Server.sv.lightstyles[i] );
            }

            //
            // send some stats
            //
            m.WriteByte( ProtocolDef.svc_updatestat );
            m.WriteByte( QStatsDef.STAT_TOTALSECRETS );
            m.WriteLong( ( int )this.Programs.GlobalStruct.total_secrets );

            m.WriteByte( ProtocolDef.svc_updatestat );
            m.WriteByte( QStatsDef.STAT_TOTALMONSTERS );
            m.WriteLong( ( int )this.Programs.GlobalStruct.total_monsters );

            m.WriteByte( ProtocolDef.svc_updatestat );
            m.WriteByte( QStatsDef.STAT_SECRETS );
            m.WriteLong( ( int )this.Programs.GlobalStruct.found_secrets );

            m.WriteByte( ProtocolDef.svc_updatestat );
            m.WriteByte( QStatsDef.STAT_MONSTERS );
            m.WriteLong( ( int )this.Programs.GlobalStruct.killed_monsters );

            //
            // send a fixangle
            // Never send a roll angle, because savegames can catch the server
            // in a state where it is expecting the client to correct the angle
            // and it won't happen if the game was just loaded, so you wind up
            // with a permanent head tilt
            ent = this.Server.EdictNum( 1 + this.ClientNum );
            m.WriteByte( ProtocolDef.svc_setangle );
            m.WriteAngle( ent.v.angles.X );
            m.WriteAngle( ent.v.angles.Y );
            m.WriteAngle( 0 );

            this.Server.WriteClientDataToMessage(this.Server.Player, this.HostClient.message );

            m.WriteByte( ProtocolDef.svc_signonnum );
            m.WriteByte( 3 );
            this.HostClient.sendsignon = true;
        }

        /// <summary>
        /// Host_Begin_f
        /// </summary>
        /// <param name="msg"></param>
        private void Begin_f( CommandMessage msg )
        {
            if ( msg.Source == CommandSource.Command )
            {
                this.Console.Print( "begin is not valid from the console\n" );
                return;
            }

            this.HostClient.spawned = true;
        }

        /// <summary>
        /// Host_Kick_f
        /// Kicks a user off of the server
        /// </summary>
        private void Kick_f( CommandMessage msg )
        {
            if ( msg.Source == CommandSource.Command )
            {
                if ( !this.Server.sv.active )
                {
                    this.Client.ForwardToServer_f( msg );
                    return;
                }
            }
            else if (this.Programs.GlobalStruct.deathmatch != 0 && !this.HostClient.privileged )
                return;

            var save = this.HostClient;
            var byNumber = false;
            int i;
            if ( msg.Parameters?.Length > 1 && msg.Parameters[0] == "#" )
            {
                i = ( int ) MathLib.atof( msg.Parameters[1] ) - 1;
                if ( i < 0 || i >= this.Server.svs.maxclients )
                    return;
                if ( !this.Server.svs.clients[i].active )
                    return;

                this.HostClient = this.Server.svs.clients[i];
                byNumber = true;
            }
            else
            {
                for ( i = 0; i < this.Server.svs.maxclients; i++ )
                {
                    this.HostClient = this.Server.svs.clients[i];
                    if ( !this.HostClient.active )
                        continue;
                    if ( Utilities.SameText(this.HostClient.name, msg.Parameters[0] ) )
                        break;
                }
            }

            if ( i < this.Server.svs.maxclients )
            {
                string who;
                if ( msg.Source == CommandSource.Command )
                {
                    if (this.Client.cls.state == cactive_t.ca_dedicated )
                        who = "Console";
                    else
                        who = this.Client.Name;
                }
                else
                    who = save.name;

                // can't kick yourself!
                if (this.HostClient == save )
                    return;

                string message = null;
                if ( msg.Parameters?.Length > 1 )
                {
                    message = Tokeniser.Parse( msg.StringParameters );
                    if ( byNumber )
                    {
                        message = message.Substring( 1 ); // skip the #
                        message = message.Trim( ); // skip white space
                        message = message.Substring( msg.Parameters[1].Length );	// skip the number
                    }
                    message = message.Trim( );
                }
                if ( !string.IsNullOrEmpty( message ) )
                    this.Server.ClientPrint( "Kicked by {0}: {1}\n", who, message );
                else
                    this.Server.ClientPrint( "Kicked by {0}\n", who );

                this.Server.DropClient( false );
            }

            this.HostClient = save;
        }

        /// <summary>
        /// Host_Give_f
        /// </summary>
        private void Give_f( CommandMessage msg )
        {
            if ( msg.Source == CommandSource.Command )
            {
                this.Client.ForwardToServer_f( msg );
                return;
            }

            if (this.Programs.GlobalStruct.deathmatch != 0 && !this.HostClient.privileged )
                return;

            var t =  msg.Parameters[0];
            var v = MathLib.atoi(  msg.Parameters[1] );

            if ( string.IsNullOrEmpty( t ) )
                return;

            switch ( t[0] )
            {
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                    // CHANGE
                    // MED 01/04/97 added hipnotic give stuff
                    if ( MainWindow.Common.GameKind == GameKind.Hipnotic )
                    {
                        if ( t[0] == '6' )
                        {
                            if ( t[1] == 'a' )
                                this.Server.Player.v.items = ( int )this.Server.Player.v.items | QItemsDef.HIT_PROXIMITY_GUN;
                            else
                                this.Server.Player.v.items = ( int )this.Server.Player.v.items | QItemsDef.IT_GRENADE_LAUNCHER;
                        }
                        else if ( t[0] == '9' )
                            this.Server.Player.v.items = ( int )this.Server.Player.v.items | QItemsDef.HIT_LASER_CANNON;
                        else if ( t[0] == '0' )
                            this.Server.Player.v.items = ( int )this.Server.Player.v.items | QItemsDef.HIT_MJOLNIR;
                        else if ( t[0] >= '2' )
                            this.Server.Player.v.items = ( int )this.Server.Player.v.items | ( QItemsDef.IT_SHOTGUN << ( t[0] - '2' ) );
                    }
                    else
                    {
                        if ( t[0] >= '2' )
                            this.Server.Player.v.items = ( int )this.Server.Player.v.items | ( QItemsDef.IT_SHOTGUN << ( t[0] - '2' ) );
                    }
                    break;

                case 's':
                    if ( MainWindow.Common.GameKind == GameKind.Rogue )
                        this.Programs.SetEdictFieldFloat(this.Server.Player, "ammo_shells1", v );

                    this.Server.Player.v.ammo_shells = v;
                    break;

                case 'n':
                    if ( MainWindow.Common.GameKind == GameKind.Rogue )
                    {
                        if (this.Programs.SetEdictFieldFloat(this.Server.Player, "ammo_nails1", v ) )
                        {
                            if (this.Server.Player.v.weapon <= QItemsDef.IT_LIGHTNING )
                                this.Server.Player.v.ammo_nails = v;
                        }
                    }
                    else
                        this.Server.Player.v.ammo_nails = v;
                    break;

                case 'l':
                    if ( MainWindow.Common.GameKind == GameKind.Rogue )
                    {
                        if (this.Programs.SetEdictFieldFloat(this.Server.Player, "ammo_lava_nails", v ) )
                        {
                            if (this.Server.Player.v.weapon > QItemsDef.IT_LIGHTNING )
                                this.Server.Player.v.ammo_nails = v;
                        }
                    }
                    break;

                case 'r':
                    if ( MainWindow.Common.GameKind == GameKind.Rogue )
                    {
                        if (this.Programs.SetEdictFieldFloat(this.Server.Player, "ammo_rockets1", v ) )
                        {
                            if (this.Server.Player.v.weapon <= QItemsDef.IT_LIGHTNING )
                                this.Server.Player.v.ammo_rockets = v;
                        }
                    }
                    else
                        this.Server.Player.v.ammo_rockets = v;

                    break;

                case 'm':
                    if ( MainWindow.Common.GameKind == GameKind.Rogue )
                    {
                        if (this.Programs.SetEdictFieldFloat(this.Server.Player, "ammo_multi_rockets", v ) )
                        {
                            if (this.Server.Player.v.weapon > QItemsDef.IT_LIGHTNING )
                                this.Server.Player.v.ammo_rockets = v;
                        }
                    }
                    break;

                case 'h':
                    this.Server.Player.v.health = v;
                    break;

                case 'c':
                    if ( MainWindow.Common.GameKind == GameKind.Rogue )
                    {
                        if (this.Programs.SetEdictFieldFloat(this.Server.Player, "ammo_cells1", v ) )
                        {
                            if (this.Server.Player.v.weapon <= QItemsDef.IT_LIGHTNING )
                                this.Server.Player.v.ammo_cells = v;
                        }
                    }
                    else
                        this.Server.Player.v.ammo_cells = v;

                    break;

                case 'p':
                    if ( MainWindow.Common.GameKind == GameKind.Rogue )
                    {
                        if (this.Programs.SetEdictFieldFloat(this.Server.Player, "ammo_plasma", v ) )
                        {
                            if (this.Server.Player.v.weapon > QItemsDef.IT_LIGHTNING )
                                this.Server.Player.v.ammo_cells = v;
                        }
                    }
                    break;
            }
        }

        private MemoryEdict FindViewthing( )
        {
            for ( var i = 0; i < this.Server.sv.num_edicts; i++ )
            {
                var e = this.Server.EdictNum( i );
                if (this.Programs.GetString( e.v.classname ) == "viewthing" )
                    return e;
            }

            this.Console.Print( "No viewthing on map\n" );
            return null;
        }

        /// <summary>
        /// Host_Startdemos_f
        /// </summary>
        /// <param name="msg"></param>
        private void Startdemos_f( CommandMessage msg )
        {
            if (this.Client.cls.state == cactive_t.ca_dedicated )
            {
                if ( !this.Server.sv.active )
                    this.Commands.Buffer.Append( "map start\n" );
                return;
            }

            var c = msg.Parameters.Length;
            if ( c > ClientDef.MAX_DEMOS )
            {
                this.Console.Print( "Max {0} demos in demoloop\n", ClientDef.MAX_DEMOS );
                c = ClientDef.MAX_DEMOS;
            }

            this.Console.Print( "{0} demo(s) in loop\n", c );

            for ( var i = 0; i < c; i++ )
                this.Client.cls.demos[i] = Utilities.Copy( msg.Parameters[i], ClientDef.MAX_DEMONAME );

            if ( !this.Server.sv.active && this.Client.cls.demonum != -1 && !this.Client.cls.demoplayback )
            {
                this.Client.cls.demonum = 0;
                this.Client.NextDemo( );
            }
            else
                this.Client.cls.demonum = -1;
        }

        /// <summary>
        /// Host_Demos_f
        /// Return to looping demos
        /// </summary>
        private void Demos_f( CommandMessage msg )
        {
            if (this.Client.cls.state == cactive_t.ca_dedicated )
                return;
            if (this.Client.cls.demonum == -1 )
                this.Client.cls.demonum = 1;

            this.Client.Disconnect_f( null );
            this.Client.NextDemo( );
        }

        /// <summary>
        /// Host_Stopdemo_f
        /// Return to looping demos
        /// </summary>
        private void Stopdemo_f( CommandMessage msg )
        {
            if (this.Client.cls.state == cactive_t.ca_dedicated || !this.Client.cls.demoplayback )
                return;

            this.Client.StopPlayback( );
            this.Client.Disconnect( );
        }
    }
}
