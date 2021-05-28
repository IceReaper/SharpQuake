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
    using Framework.Definitions;
    using Framework.IO.Input;

    public class SinglePlayerMenu : MenuBase
    {
        private const int SINGLEPLAYER_ITEMS = 3;

        /// <summary>
        /// M_SinglePlayer_Key
        /// </summary>
        public override void KeyEvent( int key )
        {
            switch ( key )
            {
                case KeysDef.K_ESCAPE:
                    MenuBase.MainMenuInstance.Show(this.Host );
                    break;

                case KeysDef.K_DOWNARROW:
                    this.Host.Sound.LocalSound( "misc/menu1.wav" );
                    if ( ++this._Cursor >= SinglePlayerMenu.SINGLEPLAYER_ITEMS )
                        this._Cursor = 0;
                    break;

                case KeysDef.K_UPARROW:
                    this.Host.Sound.LocalSound( "misc/menu1.wav" );
                    if ( --this._Cursor < 0 )
                        this._Cursor = SinglePlayerMenu.SINGLEPLAYER_ITEMS - 1;
                    break;

                case KeysDef.K_ENTER:
                    this.Host.Menu.EnterSound = true;

                    switch (this._Cursor )
                    {
                        case 0:
                            if (this.Host.Server.sv.active )
                            {
                                if ( !this.Host.Screen.ModalMessage( "Are you sure you want to\nstart a new game?\n" ) )
                                    break;
                            }

                            this.Host.Keyboard.Destination = KeyDestination.key_game;
                            if (this.Host.Server.sv.active )
                                this.Host.Commands.Buffer.Append( "disconnect\n" );

                            this.Host.Commands.Buffer.Append( "maxplayers 1\n" );
                            this.Host.Commands.Buffer.Append( "map start\n" );
                            break;

                        case 1:
                            MenuBase.LoadMenuInstance.Show(this.Host );
                            break;

                        case 2:
                            MenuBase.SaveMenuInstance.Show(this.Host );
                            break;
                    }
                    break;
            }
        }

        /// <summary>
        /// M_SinglePlayer_Draw
        /// </summary>
        public override void Draw( )
        {
            this.Host.Menu.DrawTransPic( 16, 4, this.Host.DrawingContext.CachePic( "gfx/qplaque.lmp", "GL_NEAREST" ) );
            var p = this.Host.DrawingContext.CachePic( "gfx/ttl_sgl.lmp", "GL_NEAREST" );
            this.Host.Menu.DrawPic( ( 320 - p.Width ) / 2, 4, p );
            this.Host.Menu.DrawTransPic( 72, 32, this.Host.DrawingContext.CachePic( "gfx/sp_menu.lmp", "GL_NEAREST" ) );

            var f = ( int ) (this.Host.Time * 10 ) % 6;

            this.Host.Menu.DrawTransPic( 54, 32 + this._Cursor * 20, this.Host.DrawingContext.CachePic( string.Format( "gfx/menudot{0}.lmp", f + 1 ), "GL_NEAREST" ) );
        }
    }
}
