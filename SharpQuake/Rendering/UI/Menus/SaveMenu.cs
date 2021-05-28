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

    public class SaveMenu : LoadMenu
    {
        public override void Show( Host host )
        {
            if ( !this.Host.Server.sv.active )
                return;
            if (this.Host.Client.cl.intermission != 0 )
                return;
            if (this.Host.Server.svs.maxclients != 1 )
                return;

            base.Show( host );
        }

        public override void KeyEvent( int key )
        {
            switch ( key )
            {
                case KeysDef.K_ESCAPE:
                    MenuBase.SinglePlayerMenuInstance.Show(this.Host );
                    break;

                case KeysDef.K_ENTER:
                    MenuBase.CurrentMenu.Hide( );
                    this.Host.Commands.Buffer.Append( string.Format( "save s{0}\n", this._Cursor ) );
                    return;

                case KeysDef.K_UPARROW:
                case KeysDef.K_LEFTARROW:
                    this.Host.Sound.LocalSound( "misc/menu1.wav" );
                    this._Cursor--;
                    if (this._Cursor < 0 )
                        this._Cursor = LoadMenu.MAX_SAVEGAMES - 1;
                    break;

                case KeysDef.K_DOWNARROW:
                case KeysDef.K_RIGHTARROW:
                    this.Host.Sound.LocalSound( "misc/menu1.wav" );
                    this._Cursor++;
                    if (this._Cursor >= LoadMenu.MAX_SAVEGAMES )
                        this._Cursor = 0;
                    break;
            }
        }

        public override void Draw( )
        {
            var p = this.Host.DrawingContext.CachePic( "gfx/p_save.lmp", "GL_NEAREST" );
            this.Host.Menu.DrawPic( ( 320 - p.Width ) / 2, 4, p );

            for ( var i = 0; i < LoadMenu.MAX_SAVEGAMES; i++ )
                this.Host.Menu.Print( 16, 32 + 8 * i, this._FileNames[i] );

            // line cursor
            this.Host.Menu.DrawCharacter( 8, 32 + this._Cursor * 8, 12 + ( ( int ) (this.Host.RealTime * 4 ) & 1 ) );
        }
    }
}
