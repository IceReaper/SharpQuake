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
    using Framework.IO.Input;
    using Networking.Client;

    /// <summary>
    /// MainMenu
    /// </summary>
    public class MainMenu : MenuBase
    {
        private const int MAIN_ITEMS = 5;
        private int _SaveDemoNum;

        public override void Show( Host host )
        {
            if ( host.Keyboard.Destination != KeyDestination.key_menu )
            {
                this._SaveDemoNum = host.Client.cls.demonum;
                host.Client.cls.demonum = -1;
            }

            base.Show( host );
        }

        /// <summary>
        /// M_Main_Key
        /// </summary>
        public override void KeyEvent( int key )
        {
            switch ( key )
            {
                case KeysDef.K_ESCAPE:
                    //Host.Keyboard.Destination = keydest_t.key_game;
                    MenuBase.CurrentMenu.Hide( );
                    this.Host.Client.cls.demonum = this._SaveDemoNum;
                    if (this.Host.Client.cls.demonum != -1 && !this.Host.Client.cls.demoplayback && this.Host.Client.cls.state != cactive_t.ca_connected )
                        this.Host.Client.NextDemo( );
                    break;

                case KeysDef.K_DOWNARROW:
                    this.Host.Sound.LocalSound( "misc/menu1.wav" );
                    if ( ++this._Cursor >= MainMenu.MAIN_ITEMS )
                        this._Cursor = 0;
                    break;

                case KeysDef.K_UPARROW:
                    this.Host.Sound.LocalSound( "misc/menu1.wav" );
                    if ( --this._Cursor < 0 )
                        this._Cursor = MainMenu.MAIN_ITEMS - 1;
                    break;

                case KeysDef.K_ENTER:
                    this.Host.Menu.EnterSound = true;

                    switch (this._Cursor )
                    {
                        case 0:
                            MenuBase.SinglePlayerMenuInstance.Show(this.Host );
                            break;

                        case 1:
                            MenuBase.MultiPlayerMenuInstance.Show(this.Host );
                            break;

                        case 2:
                            MenuBase.OptionsMenuInstance.Show(this.Host );
                            break;

                        case 3:
                            MenuBase.HelpMenuInstance.Show(this.Host );
                            break;

                        case 4:
                            MenuBase.QuitMenuInstance.Show(this.Host );
                            break;
                    }
                    break;
            }
        }

        public override void Draw( )
        {
            this.Host.Menu.DrawTransPic( 16, 4, this.Host.DrawingContext.CachePic( "gfx/qplaque.lmp", "GL_NEAREST" ) );
            var p = this.Host.DrawingContext.CachePic( "gfx/ttl_main.lmp", "GL_NEAREST" );
            this.Host.Menu.DrawPic( ( 320 - p.Width ) / 2, 4, p );
            this.Host.Menu.DrawTransPic(72, 32, this.Host.DrawingContext.CachePic( "gfx/mainmenu.lmp", "GL_NEAREST" ) );
            
            var f = ( int ) (this.Host.Time * 10 ) % 6;

            this.Host.Menu.DrawTransPic( 54, 32 + this._Cursor * 20, this.Host.DrawingContext.CachePic( string.Format( "gfx/menudot{0}.lmp", f + 1 ), "GL_NEAREST" ) );
        }
    }
}
