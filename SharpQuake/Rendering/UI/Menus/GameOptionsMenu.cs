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

namespace SharpQuake.Rendering.UI.Menus
{
    using Desktop;
    using Engine.Host;
    using Framework.Definitions;
    using Framework.Engine;

    /// <summary>
    /// M_Menu_GameOptions_functions
    /// </summary>
    public class GameOptionsMenu : MenuBase
    {
        private const int NUM_GAMEOPTIONS = 9;

        private static readonly level_t[] Levels = new level_t[]
        {
            new("start", "Entrance"),	// 0

	        new("e1m1", "Slipgate Complex"),				// 1
	        new("e1m2", "Castle of the Damned"),
            new("e1m3", "The Necropolis"),
            new("e1m4", "The Grisly Grotto"),
            new("e1m5", "Gloom Keep"),
            new("e1m6", "The Door To Chthon"),
            new("e1m7", "The House of Chthon"),
            new("e1m8", "Ziggurat Vertigo"),

            new("e2m1", "The Installation"),				// 9
	        new("e2m2", "Ogre Citadel"),
            new("e2m3", "Crypt of Decay"),
            new("e2m4", "The Ebon Fortress"),
            new("e2m5", "The Wizard's Manse"),
            new("e2m6", "The Dismal Oubliette"),
            new("e2m7", "Underearth"),

            new("e3m1", "Termination Central"),			// 16
	        new("e3m2", "The Vaults of Zin"),
            new("e3m3", "The Tomb of Terror"),
            new("e3m4", "Satan's Dark Delight"),
            new("e3m5", "Wind Tunnels"),
            new("e3m6", "Chambers of Torment"),
            new("e3m7", "The Haunted Halls"),

            new("e4m1", "The Sewage System"),				// 23
	        new("e4m2", "The Tower of Despair"),
            new("e4m3", "The Elder God Shrine"),
            new("e4m4", "The Palace of Hate"),
            new("e4m5", "Hell's Atrium"),
            new("e4m6", "The Pain Maze"),
            new("e4m7", "Azure Agony"),
            new("e4m8", "The Nameless City"),

            new("end", "Shub-Niggurath's Pit"),			// 31

	        new("dm1", "Place of Two Deaths"),				// 32
	        new("dm2", "Claustrophobopolis"),
            new("dm3", "The Abandoned Base"),
            new("dm4", "The Bad Place"),
            new("dm5", "The Cistern"),
            new("dm6", "The Dark Zone")
        };

        //MED 01/06/97 added hipnotic levels
        private static readonly level_t[] HipnoticLevels = new level_t[]
        {
           new("start", "Command HQ"),  // 0

           new("hip1m1", "The Pumping Station"),          // 1
           new("hip1m2", "Storage Facility"),
           new("hip1m3", "The Lost Mine"),
           new("hip1m4", "Research Facility"),
           new("hip1m5", "Military Complex"),

           new("hip2m1", "Ancient Realms"),          // 6
           new("hip2m2", "The Black Cathedral"),
           new("hip2m3", "The Catacombs"),
           new("hip2m4", "The Crypt"),
           new("hip2m5", "Mortum's Keep"),
           new("hip2m6", "The Gremlin's Domain"),

           new("hip3m1", "Tur Torment"),       // 12
           new("hip3m2", "Pandemonium"),
           new("hip3m3", "Limbo"),
           new("hip3m4", "The Gauntlet"),

           new("hipend", "Armagon's Lair"),       // 16

           new("hipdm1", "The Edge of Oblivion")           // 17
        };

        //PGM 01/07/97 added rogue levels
        //PGM 03/02/97 added dmatch level
        private static readonly level_t[] RogueLevels = new level_t[]
        {
            new("start", "Split Decision"),
            new("r1m1", "Deviant's Domain"),
            new("r1m2", "Dread Portal"),
            new("r1m3", "Judgement Call"),
            new("r1m4", "Cave of Death"),
            new("r1m5", "Towers of Wrath"),
            new("r1m6", "Temple of Pain"),
            new("r1m7", "Tomb of the Overlord"),
            new("r2m1", "Tempus Fugit"),
            new("r2m2", "Elemental Fury I"),
            new("r2m3", "Elemental Fury II"),
            new("r2m4", "Curse of Osiris"),
            new("r2m5", "Wizard's Keep"),
            new("r2m6", "Blood Sacrifice"),
            new("r2m7", "Last Bastion"),
            new("r2m8", "Source of Evil"),
            new("ctf1", "Division of Change")
        };

        private static readonly episode_t[] Episodes = new episode_t[]
        {
            new("Welcome to Quake", 0, 1),
            new("Doomed Dimension", 1, 8),
            new("Realm of Black Magic", 9, 7),
            new("Netherworld", 16, 7),
            new("The Elder World", 23, 8),
            new("Final Level", 31, 1),
            new("Deathmatch Arena", 32, 6)
        };

        //MED 01/06/97  added hipnotic episodes
        private static readonly episode_t[] HipnoticEpisodes = new episode_t[]
        {
           new("Scourge of Armagon", 0, 1),
           new("Fortress of the Dead", 1, 5),
           new("Dominion of Darkness", 6, 6),
           new("The Rift", 12, 4),
           new("Final Level", 16, 1),
           new("Deathmatch Arena", 17, 1)
        };

        //PGM 01/07/97 added rogue episodes
        //PGM 03/02/97 added dmatch episode
        private static readonly episode_t[] RogueEpisodes = new episode_t[]
        {
            new("Introduction", 0, 1),
            new("Hell's Fortress", 1, 7),
            new("Corridors of Time", 8, 8),
            new("Deathmatch Arena", 16, 1)
        };

        private static readonly int[] _CursorTable = new int[]
        {
            40, 56, 64, 72, 80, 88, 96, 112, 120
        };

        private int _StartEpisode;

        private int _StartLevel;

        private int _MaxPlayers;

        private bool _ServerInfoMessage;

        private double _ServerInfoMessageTime;


        public override void Show( Host host )
        {
            base.Show( host );

            if (this._MaxPlayers == 0 )
                this._MaxPlayers = this.Host.Server.svs.maxclients;
            if (this._MaxPlayers < 2 )
                this._MaxPlayers = this.Host.Server.svs.maxclientslimit;
        }

        public override void KeyEvent( int key )
        {
            switch ( key )
            {
                case KeysDef.K_ESCAPE:
                    MenuBase.LanConfigMenuInstance.Show(this.Host );
                    break;

                case KeysDef.K_UPARROW:
                    this.Host.Sound.LocalSound( "misc/menu1.wav" );
                    this._Cursor--;
                    if (this._Cursor < 0 )
                        this._Cursor = GameOptionsMenu.NUM_GAMEOPTIONS - 1;
                    break;

                case KeysDef.K_DOWNARROW:
                    this.Host.Sound.LocalSound( "misc/menu1.wav" );
                    this._Cursor++;
                    if (this._Cursor >= GameOptionsMenu.NUM_GAMEOPTIONS )
                        this._Cursor = 0;
                    break;

                case KeysDef.K_LEFTARROW:
                    if (this._Cursor == 0 )
                        break;

                    this.Host.Sound.LocalSound( "misc/menu3.wav" );
                    this.Change( -1 );
                    break;

                case KeysDef.K_RIGHTARROW:
                    if (this._Cursor == 0 )
                        break;

                    this.Host.Sound.LocalSound( "misc/menu3.wav" );
                    this.Change( 1 );
                    break;

                case KeysDef.K_ENTER:
                    this.Host.Sound.LocalSound( "misc/menu2.wav" );
                    if (this._Cursor == 0 )
                    {
                        if (this.Host.Server.IsActive )
                            this.Host.Commands.Buffer.Append( "disconnect\n" );

                        this.Host.Commands.Buffer.Append( "listen 0\n" );	// so host_netport will be re-examined
                        this.Host.Commands.Buffer.Append( string.Format( "maxplayers {0}\n", this._MaxPlayers ) );
                        this.Host.Screen.BeginLoadingPlaque( );

                        if ( MainWindow.Common.GameKind == GameKind.Hipnotic )
                        {
                            this.Host.Commands.Buffer.Append( string.Format( "map {0}\n",
                                GameOptionsMenu.HipnoticLevels[GameOptionsMenu.HipnoticEpisodes[this._StartEpisode].firstLevel + this._StartLevel].name ) );
                        }
                        else if ( MainWindow.Common.GameKind == GameKind.Rogue )
                        {
                            this.Host.Commands.Buffer.Append( string.Format( "map {0}\n",
                                GameOptionsMenu.RogueLevels[GameOptionsMenu.RogueEpisodes[this._StartEpisode].firstLevel + this._StartLevel].name ) );
                        }
                        else
                            this.Host.Commands.Buffer.Append( string.Format( "map {0}\n", GameOptionsMenu.Levels[GameOptionsMenu.Episodes[this._StartEpisode].firstLevel + this._StartLevel].name ) );

                        return;
                    }

                    this.Change( 1 );
                    break;
            }
        }

        public override void Draw( )
        {
            this.Host.Menu.DrawTransPic( 16, 4, this.Host.DrawingContext.CachePic( "gfx/qplaque.lmp", "GL_NEAREST" ) );
            var p = this.Host.DrawingContext.CachePic( "gfx/p_multi.lmp", "GL_NEAREST" );
            this.Host.Menu.DrawPic( ( 320 - p.Width ) / 2, 4, p );

            this.Host.Menu.DrawTextBox( 152, 32, 10, 1 );
            this.Host.Menu.Print( 160, 40, "begin game" );

            this.Host.Menu.Print( 0, 56, "      Max players" );
            this.Host.Menu.Print( 160, 56, this._MaxPlayers.ToString( ) );

            this.Host.Menu.Print( 0, 64, "        Game Type" );
            if (this.Host.Cvars.Coop.Get<bool>( ) )
                this.Host.Menu.Print( 160, 64, "Cooperative" );
            else
                this.Host.Menu.Print( 160, 64, "Deathmatch" );

            this.Host.Menu.Print( 0, 72, "        Teamplay" );
            if ( MainWindow.Common.GameKind == GameKind.Rogue )
            {
                string msg;
                switch (this.Host.Cvars.TeamPlay.Get<int>( ) )
                {
                    case 1:
                        msg = "No Friendly Fire";
                        break;

                    case 2:
                        msg = "Friendly Fire";
                        break;

                    case 3:
                        msg = "Tag";
                        break;

                    case 4:
                        msg = "Capture the Flag";
                        break;

                    case 5:
                        msg = "One Flag CTF";
                        break;

                    case 6:
                        msg = "Three Team CTF";
                        break;

                    default:
                        msg = "Off";
                        break;
                }

                this.Host.Menu.Print( 160, 72, msg );
            }
            else
            {
                string msg;
                switch (this.Host.Cvars.TeamPlay.Get<int>( ) )
                {
                    case 1:
                        msg = "No Friendly Fire";
                        break;

                    case 2:
                        msg = "Friendly Fire";
                        break;

                    default:
                        msg = "Off";
                        break;
                }

                this.Host.Menu.Print( 160, 72, msg );
            }

            this.Host.Menu.Print( 0, 80, "            Skill" );
            if (this.Host.Cvars.Skill.Get<int>( ) == 0 )
                this.Host.Menu.Print( 160, 80, "Easy difficulty" );
            else if (this.Host.Cvars.Skill.Get<int>( ) == 1 )
                this.Host.Menu.Print( 160, 80, "Normal difficulty" );
            else if (this.Host.Cvars.Skill.Get<int>( ) == 2 )
                this.Host.Menu.Print( 160, 80, "Hard difficulty" );
            else
                this.Host.Menu.Print( 160, 80, "Nightmare difficulty" );

            this.Host.Menu.Print( 0, 88, "       Frag Limit" );
            if (this.Host.Cvars.FragLimit.Get<int>( ) == 0 )
                this.Host.Menu.Print( 160, 88, "none" );
            else
                this.Host.Menu.Print( 160, 88, string.Format( "{0} frags", this.Host.Cvars.FragLimit.Get<int>( ) ) );

            this.Host.Menu.Print( 0, 96, "       Time Limit" );
            if (this.Host.Cvars.TimeLimit.Get<int>( ) == 0 )
                this.Host.Menu.Print( 160, 96, "none" );
            else
                this.Host.Menu.Print( 160, 96, string.Format( "{0} minutes", this.Host.Cvars.TimeLimit.Get<int>( ) ) );

            this.Host.Menu.Print( 0, 112, "         Episode" );
            //MED 01/06/97 added hipnotic episodes
            if ( MainWindow.Common.GameKind == GameKind.Hipnotic )
                this.Host.Menu.Print( 160, 112, GameOptionsMenu.HipnoticEpisodes[this._StartEpisode].description );
            //PGM 01/07/97 added rogue episodes
            else if ( MainWindow.Common.GameKind == GameKind.Rogue )
                this.Host.Menu.Print( 160, 112, GameOptionsMenu.RogueEpisodes[this._StartEpisode].description );
            else
                this.Host.Menu.Print( 160, 112, GameOptionsMenu.Episodes[this._StartEpisode].description );

            this.Host.Menu.Print( 0, 120, "           Level" );
            //MED 01/06/97 added hipnotic episodes
            if ( MainWindow.Common.GameKind == GameKind.Hipnotic )
            {
                this.Host.Menu.Print( 160, 120, GameOptionsMenu.HipnoticLevels[GameOptionsMenu.HipnoticEpisodes[this._StartEpisode].firstLevel + this._StartLevel].description );
                this.Host.Menu.Print( 160, 128, GameOptionsMenu.HipnoticLevels[GameOptionsMenu.HipnoticEpisodes[this._StartEpisode].firstLevel + this._StartLevel].name );
            }
            //PGM 01/07/97 added rogue episodes
            else if ( MainWindow.Common.GameKind == GameKind.Rogue )
            {
                this.Host.Menu.Print( 160, 120, GameOptionsMenu.RogueLevels[GameOptionsMenu.RogueEpisodes[this._StartEpisode].firstLevel + this._StartLevel].description );
                this.Host.Menu.Print( 160, 128, GameOptionsMenu.RogueLevels[GameOptionsMenu.RogueEpisodes[this._StartEpisode].firstLevel + this._StartLevel].name );
            }
            else
            {
                this.Host.Menu.Print( 160, 120, GameOptionsMenu.Levels[GameOptionsMenu.Episodes[this._StartEpisode].firstLevel + this._StartLevel].description );
                this.Host.Menu.Print( 160, 128, GameOptionsMenu.Levels[GameOptionsMenu.Episodes[this._StartEpisode].firstLevel + this._StartLevel].name );
            }

            // line cursor
            this.Host.Menu.DrawCharacter( 144, GameOptionsMenu._CursorTable[this._Cursor], 12 + ( ( int ) (this.Host.RealTime * 4 ) & 1 ) );

            if (this._ServerInfoMessage )
            {
                if ( this.Host.RealTime - this._ServerInfoMessageTime < 5.0 )
                {
                    var x = ( 320 - 26 * 8 ) / 2;
                    this.Host.Menu.DrawTextBox( x, 138, 24, 4 );
                    x += 8;
                    this.Host.Menu.Print( x, 146, "  More than 4 players   " );
                    this.Host.Menu.Print( x, 154, " requires using command " );
                    this.Host.Menu.Print( x, 162, "line parameters; please " );
                    this.Host.Menu.Print( x, 170, "   see techinfo.txt.    " );
                }
                else
                    this._ServerInfoMessage = false;
            }
        }

        private class level_t
        {
            public string name;
            public string description;

            public level_t( string name, string desc )
            {
                this.name = name;
                this.description = desc;
            }
        } //level_t;

        private class episode_t
        {
            public string description;
            public int firstLevel;
            public int levels;

            public episode_t( string desc, int firstLevel, int levels )
            {
                this.description = desc;
                this.firstLevel = firstLevel;
                this.levels = levels;
            }
        } //episode_t;

        /// <summary>
        /// M_NetStart_Change
        /// </summary>
        private void Change( int dir )
        {
            int count;

            switch (this._Cursor )
            {
                case 1:
                    this._MaxPlayers += dir;
                    if (this._MaxPlayers > this.Host.Server.svs.maxclientslimit )
                    {
                        this._MaxPlayers = this.Host.Server.svs.maxclientslimit;
                        this._ServerInfoMessage = true;
                        this._ServerInfoMessageTime = this.Host.RealTime;
                    }
                    if (this._MaxPlayers < 2 )
                        this._MaxPlayers = 2;
                    break;

                case 2:
                    this.Host.CVars.Set( "coop", this.Host.Cvars.Coop.Get<bool>( ) );
                    break;

                case 3:
                    if ( MainWindow.Common.GameKind == GameKind.Rogue )
                        count = 6;
                    else
                        count = 2;

                    var tp = this.Host.Cvars.TeamPlay.Get<int>( ) + dir;
                    if ( tp > count )
                        tp = 0;
                    else if ( tp < 0 )
                        tp = count;

                    this.Host.CVars.Set( "teamplay", tp );
                    break;

                case 4:
                    var skill = this.Host.Cvars.Skill.Get<int>( ) + dir;
                    if ( skill > 3 )
                        skill = 0;
                    if ( skill < 0 )
                        skill = 3;

                    this.Host.CVars.Set( "skill", skill );
                    break;

                case 5:
                    var fraglimit = this.Host.Cvars.FragLimit.Get<int>( ) + dir * 10;
                    if ( fraglimit > 100 )
                        fraglimit = 0;
                    if ( fraglimit < 0 )
                        fraglimit = 100;

                    this.Host.CVars.Set( "fraglimit", fraglimit );
                    break;

                case 6:
                    var timelimit = this.Host.Cvars.TimeLimit.Get<int>( ) + dir * 5;
                    if ( timelimit > 60 )
                        timelimit = 0;
                    if ( timelimit < 0 )
                        timelimit = 60;

                    this.Host.CVars.Set( "timelimit", timelimit );
                    break;

                case 7:
                    this._StartEpisode += dir;
                    //MED 01/06/97 added hipnotic count
                    if ( MainWindow.Common.GameKind == GameKind.Hipnotic )
                        count = 6;
                    //PGM 01/07/97 added rogue count
                    //PGM 03/02/97 added 1 for dmatch episode
                    else if ( MainWindow.Common.GameKind == GameKind.Rogue )
                        count = 4;
                    else if ( MainWindow.Common.IsRegistered )
                        count = 7;
                    else
                        count = 2;

                    if (this._StartEpisode < 0 )
                        this._StartEpisode = count - 1;

                    if (this._StartEpisode >= count )
                        this._StartEpisode = 0;

                    this._StartLevel = 0;
                    break;

                case 8:
                    this._StartLevel += dir;
                    //MED 01/06/97 added hipnotic episodes
                    if ( MainWindow.Common.GameKind == GameKind.Hipnotic )
                        count = GameOptionsMenu.HipnoticEpisodes[this._StartEpisode].levels;
                    //PGM 01/06/97 added hipnotic episodes
                    else if ( MainWindow.Common.GameKind == GameKind.Rogue )
                        count = GameOptionsMenu.RogueEpisodes[this._StartEpisode].levels;
                    else
                        count = GameOptionsMenu.Episodes[this._StartEpisode].levels;

                    if (this._StartLevel < 0 )
                        this._StartLevel = count - 1;

                    if (this._StartLevel >= count )
                        this._StartLevel = 0;
                    break;
            }
        }
    }

}
