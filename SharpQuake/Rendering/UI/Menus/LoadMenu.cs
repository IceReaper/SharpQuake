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
    using Framework.IO;
    using System.IO;
    using System.Text;

    public class LoadMenu : MenuBase
    {
        public const int MAX_SAVEGAMES = 12;
        protected string[] _FileNames; //[MAX_SAVEGAMES]; // filenames
        protected bool[] _Loadable; //[MAX_SAVEGAMES]; // loadable

        public override void Show( Host host )
        {
            base.Show( host );
            this.ScanSaves( );
        }

        public override void KeyEvent( int key )
        {
            switch ( key )
            {
                case KeysDef.K_ESCAPE:
                    MenuBase.SinglePlayerMenuInstance.Show(this.Host );
                    break;

                case KeysDef.K_ENTER:
                    this.Host.Sound.LocalSound( "misc/menu2.wav" );
                    if ( !this._Loadable[this._Cursor] )
                        return;
                    MenuBase.CurrentMenu.Hide( );

                    // Host_Loadgame_f can't bring up the loading plaque because too much
                    // stack space has been used, so do it now
                    this.Host.Screen.BeginLoadingPlaque( );

                    // issue the load command
                    this.Host.Commands.Buffer.Append( string.Format( "load s{0}\n", this._Cursor ) );
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
            var p = this.Host.DrawingContext.CachePic( "gfx/p_load.lmp", "GL_NEAREST" );
            this.Host.Menu.DrawPic( ( 320 - p.Width ) / 2, 4, p );

            for ( var i = 0; i < LoadMenu.MAX_SAVEGAMES; i++ )
                this.Host.Menu.Print( 16, 32 + 8 * i, this._FileNames[i] );

            // line cursor
            this.Host.Menu.DrawCharacter( 8, 32 + this._Cursor * 8, 12 + ( ( int ) (this.Host.RealTime * 4 ) & 1 ) );
        }

        /// <summary>
        /// M_ScanSaves
        /// </summary>
        protected void ScanSaves( )
        {
            for ( var i = 0; i < LoadMenu.MAX_SAVEGAMES; i++ )
            {
                this._FileNames[i] = "--- UNUSED SLOT ---";
                this._Loadable[i] = false;
                var name = string.Format( "{0}/s{1}.sav", FileSystem.GameDir, i );
                var fs = FileSystem.OpenRead( name );
                if ( fs == null )
                    continue;

                using ( var reader = new StreamReader( fs, Encoding.ASCII ) )
                {
                    var version = reader.ReadLine( );
                    if ( version == null )
                        continue;
                    var info = reader.ReadLine( );
                    if ( info == null )
                        continue;
                    info = info.TrimEnd( '\0', '_' ).Replace( '_', ' ' );
                    if ( !string.IsNullOrEmpty( info ) )
                    {
                        this._FileNames[i] = info;
                        this._Loadable[i] = true;
                    }
                }
            }
        }

        public LoadMenu( )
        {
            this._FileNames = new string[LoadMenu.MAX_SAVEGAMES];
            this._Loadable = new bool[LoadMenu.MAX_SAVEGAMES];
        }
    }
}
