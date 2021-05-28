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
    using Framework.Networking;
    using System;

    public class ServerListMenu : MenuBase
    {
        private bool _Sorted;

        public override void Show( Host host )
        {
            base.Show( host );
            this._Cursor = 0;
            this.Host.Menu.ReturnOnError = false;
            this.Host.Menu.ReturnReason = string.Empty;
            this._Sorted = false;
        }

        public override void KeyEvent( int key )
        {
            switch ( key )
            {
                case KeysDef.K_ESCAPE:
                    MenuBase.LanConfigMenuInstance.Show(this.Host );
                    break;

                case KeysDef.K_SPACE:
                    MenuBase.SearchMenuInstance.Show(this.Host );
                    break;

                case KeysDef.K_UPARROW:
                case KeysDef.K_LEFTARROW:
                    this.Host.Sound.LocalSound( "misc/menu1.wav" );
                    this._Cursor--;
                    if (this._Cursor < 0 )
                        this._Cursor = this.Host.Network.HostCacheCount - 1;
                    break;

                case KeysDef.K_DOWNARROW:
                case KeysDef.K_RIGHTARROW:
                    this.Host.Sound.LocalSound( "misc/menu1.wav" );
                    this._Cursor++;
                    if (this._Cursor >= this.Host.Network.HostCacheCount )
                        this._Cursor = 0;
                    break;

                case KeysDef.K_ENTER:
                    this.Host.Sound.LocalSound( "misc/menu2.wav" );
                    this.Host.Menu.ReturnMenu = this;
                    this.Host.Menu.ReturnOnError = true;
                    this._Sorted = false;
                    MenuBase.CurrentMenu.Hide( );
                    this.Host.Commands.Buffer.Append( string.Format( "connect \"{0}\"\n", this.Host.Network.HostCache[this._Cursor].cname ) );
                    break;

                default:
                    break;
            }
        }

        public override void Draw( )
        {
            if ( !this._Sorted )
            {
                if (this.Host.Network.HostCacheCount > 1 )
                {
                    Comparison<hostcache_t> cmp = delegate ( hostcache_t a, hostcache_t b )
                    {
                        return string.Compare( a.cname, b.cname );
                    };

                    Array.Sort(this.Host.Network.HostCache, cmp );
                }

                this._Sorted = true;
            }

            var p = this.Host.DrawingContext.CachePic( "gfx/p_multi.lmp", "GL_NEAREST" );
            this.Host.Menu.DrawPic( ( 320 - p.Width ) / 2, 4, p );
            for ( var n = 0; n < this.Host.Network.HostCacheCount; n++ )
            {
                var hc = this.Host.Network.HostCache[n];
                string tmp;
                if ( hc.maxusers > 0 )
                    tmp = string.Format( "{0,-15} {1,-15} {2:D2}/{3:D2}\n", hc.name, hc.map, hc.users, hc.maxusers );
                else
                    tmp = string.Format( "{0,-15} {1,-15}\n", hc.name, hc.map );

                this.Host.Menu.Print( 16, 32 + 8 * n, tmp );
            }

            this.Host.Menu.DrawCharacter( 0, 32 + this._Cursor * 8, 12 + ( ( int ) (this.Host.RealTime * 4 ) & 1 ) );

            if ( !string.IsNullOrEmpty(this.Host.Menu.ReturnReason ) )
                this.Host.Menu.PrintWhite( 16, 148, this.Host.Menu.ReturnReason );
        }
    }
}
