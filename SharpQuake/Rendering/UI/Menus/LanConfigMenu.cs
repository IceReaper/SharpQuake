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
    using Engine.Host;
    using Framework.Definitions;
    using Framework.Mathematics;

    /// <summary>
    /// M_Menu_LanConfig_functions
    /// </summary>
    public class LanConfigMenu : MenuBase
    {
        public bool JoiningGame => MenuBase.MultiPlayerMenuInstance.Cursor == 0;

        public bool StartingGame => MenuBase.MultiPlayerMenuInstance.Cursor == 1;

        private const int NUM_LANCONFIG_CMDS = 3;

        private static readonly int[] _CursorTable = new int[] { 72, 92, 124 };

        private int _Port;
        private string _PortName;
        private string _JoinName;

        public override void Show( Host host )
        {
            base.Show( host );

            if (this._Cursor == -1 )
            {
                if (this.JoiningGame )
                    this._Cursor = 2;
                else
                    this._Cursor = 1;
            }
            if (this.StartingGame && this._Cursor == 2 )
                this._Cursor = 1;

            this._Port = this.Host.Network.DefaultHostPort;
            this._PortName = this._Port.ToString( );

            this.Host.Menu.ReturnOnError = false;
            this.Host.Menu.ReturnReason = string.Empty;
        }

        public override void KeyEvent( int key )
        {
            switch ( key )
            {
                case KeysDef.K_ESCAPE:
                    MenuBase.MultiPlayerMenuInstance.Show(this.Host );
                    break;

                case KeysDef.K_UPARROW:
                    this.Host.Sound.LocalSound( "misc/menu1.wav" );
                    this._Cursor--;
                    if (this._Cursor < 0 )
                        this._Cursor = LanConfigMenu.NUM_LANCONFIG_CMDS - 1;
                    break;

                case KeysDef.K_DOWNARROW:
                    this.Host.Sound.LocalSound( "misc/menu1.wav" );
                    this._Cursor++;
                    if (this._Cursor >= LanConfigMenu.NUM_LANCONFIG_CMDS )
                        this._Cursor = 0;
                    break;

                case KeysDef.K_ENTER:
                    if (this._Cursor == 0 )
                        break;

                    this.Host.Menu.EnterSound = true;
                    this.Host.Network.HostPort = this._Port;

                    if (this._Cursor == 1 )
                    {
                        if (this.StartingGame )
                            MenuBase.GameOptionsMenuInstance.Show(this.Host );
                        else
                            MenuBase.SearchMenuInstance.Show(this.Host );

                        break;
                    }

                    if (this._Cursor == 2 )
                    {
                        this.Host.Menu.ReturnMenu = this;
                        this.Host.Menu.ReturnOnError = true;
                        MenuBase.CurrentMenu.Hide( );
                        this.Host.Commands.Buffer.Append( string.Format( "connect \"{0}\"\n", this._JoinName ) );
                        break;
                    }
                    break;

                case KeysDef.K_BACKSPACE:
                    if (this._Cursor == 0 )
                    {
                        if ( !string.IsNullOrEmpty(this._PortName ) )
                            this._PortName = this._PortName.Substring( 0, this._PortName.Length - 1 );
                    }

                    if (this._Cursor == 2 )
                    {
                        if ( !string.IsNullOrEmpty(this._JoinName ) )
                            this._JoinName = this._JoinName.Substring( 0, this._JoinName.Length - 1 );
                    }
                    break;

                default:
                    if ( key < 32 || key > 127 )
                        break;

                    if (this._Cursor == 2 )
                    {
                        if (this._JoinName.Length < 21 )
                            this._JoinName += ( char ) key;
                    }

                    if ( key < '0' || key > '9' )
                        break;

                    if (this._Cursor == 0 )
                    {
                        if (this._PortName.Length < 5 )
                            this._PortName += ( char ) key;
                    }
                    break;
            }

            if (this.StartingGame && this._Cursor == 2 )
            {
                if ( key == KeysDef.K_UPARROW )
                    this._Cursor = 1;
                else
                    this._Cursor = 0;
            }

            var k = MathLib.atoi(this._PortName );
            if ( k > 65535 )
                k = this._Port;
            else
                this._Port = k;

            this._PortName = this._Port.ToString( );
        }

        public override void Draw( )
        {
            this.Host.Menu.DrawTransPic( 16, 4, this.Host.DrawingContext.CachePic( "gfx/qplaque.lmp", "GL_NEAREST" ) );
            var p = this.Host.DrawingContext.CachePic( "gfx/p_multi.lmp", "GL_NEAREST" );
            var basex = ( 320 - p.Width ) / 2;
            this.Host.Menu.DrawPic( basex, 4, p );

            string startJoin;
            if (this.StartingGame )
                startJoin = "New Game - TCP/IP";
            else
                startJoin = "Join Game - TCP/IP";

            this.Host.Menu.Print( basex, 32, startJoin );
            basex += 8;

            this.Host.Menu.Print( basex, 52, "Address:" );
            this.Host.Menu.Print( basex + 9 * 8, 52, this.Host.Network.MyTcpIpAddress );

            this.Host.Menu.Print( basex, LanConfigMenu._CursorTable[0], "Port" );
            this.Host.Menu.DrawTextBox( basex + 8 * 8, LanConfigMenu._CursorTable[0] - 8, 6, 1 );
            this.Host.Menu.Print( basex + 9 * 8, LanConfigMenu._CursorTable[0], this._PortName );

            if (this.JoiningGame )
            {
                this.Host.Menu.Print( basex, LanConfigMenu._CursorTable[1], "Search for local games..." );
                this.Host.Menu.Print( basex, 108, "Join game at:" );
                this.Host.Menu.DrawTextBox( basex + 8, LanConfigMenu._CursorTable[2] - 8, 22, 1 );
                this.Host.Menu.Print( basex + 16, LanConfigMenu._CursorTable[2], this._JoinName );
            }
            else
            {
                this.Host.Menu.DrawTextBox( basex, LanConfigMenu._CursorTable[1] - 8, 2, 1 );
                this.Host.Menu.Print( basex + 8, LanConfigMenu._CursorTable[1], "OK" );
            }

            this.Host.Menu.DrawCharacter( basex - 8, LanConfigMenu._CursorTable[this._Cursor], 12 + ( ( int ) (this.Host.RealTime * 4 ) & 1 ) );

            if (this._Cursor == 0 )
            {
                this.Host.Menu.DrawCharacter( basex + 9 * 8 + 8 * this._PortName.Length,
                    LanConfigMenu._CursorTable[0], 10 + ( ( int ) (this.Host.RealTime * 4 ) & 1 ) );
            }

            if (this._Cursor == 2 )
            {
                this.Host.Menu.DrawCharacter( basex + 16 + 8 * this._JoinName.Length, LanConfigMenu._CursorTable[2],
                    10 + ( ( int ) (this.Host.RealTime * 4 ) & 1 ) );
            }

            if ( !string.IsNullOrEmpty(this.Host.Menu.ReturnReason ) )
                this.Host.Menu.PrintWhite( basex, 148, this.Host.Menu.ReturnReason );
        }

        public LanConfigMenu( )
        {
            this._Cursor = -1;
            this._JoinName = string.Empty;
        }
    }
}
