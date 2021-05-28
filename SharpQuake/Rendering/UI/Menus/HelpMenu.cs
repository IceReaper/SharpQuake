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

    public class HelpMenu : MenuBase
    {
        private const int NUM_HELP_PAGES = 6;

        private int _Page;

        public override void Show( Host host )
        {
            this._Page = 0;
            base.Show( host );
        }

        public override void KeyEvent( int key )
        {
            switch ( key )
            {
                case KeysDef.K_ESCAPE:
                    MenuBase.MainMenuInstance.Show(this.Host );
                    break;

                case KeysDef.K_UPARROW:
                case KeysDef.K_RIGHTARROW:
                    this.Host.Menu.EnterSound = true;
                    if ( ++this._Page >= HelpMenu.NUM_HELP_PAGES )
                        this._Page = 0;
                    break;

                case KeysDef.K_DOWNARROW:
                case KeysDef.K_LEFTARROW:
                    this.Host.Menu.EnterSound = true;
                    if ( --this._Page < 0 )
                        this._Page = HelpMenu.NUM_HELP_PAGES - 1;
                    break;
            }
        }

        public override void Draw( )
        {
            this.Host.Menu.DrawPic( 0, 0, this.Host.DrawingContext.CachePic( string.Format( "gfx/help{0}.lmp", this._Page ), "GL_NEAREST" ) );
        }
    }
}
