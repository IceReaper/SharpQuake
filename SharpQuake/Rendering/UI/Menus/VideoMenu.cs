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

    public class VideoMenu : MenuBase
    {
        private struct modedesc_t
        {
            public int modenum;
            public string desc;
            public bool iscur;
        } //modedesc_t;

        private const int MAX_COLUMN_SIZE = 9;
        private const int MODE_AREA_HEIGHT = VideoMenu.MAX_COLUMN_SIZE + 2;
        private const int MAX_MODEDESCS = VideoMenu.MAX_COLUMN_SIZE * 3;

        private int _WModes; // vid_wmodes
        private modedesc_t[] _ModeDescs = new modedesc_t[VideoMenu.MAX_MODEDESCS]; // modedescs

        public override void KeyEvent( int key )
        {
            switch ( key )
            {
                case KeysDef.K_ESCAPE:
                    this.Host.Sound.LocalSound( "misc/menu1.wav" );
                    MenuBase.OptionsMenuInstance.Show(this.Host );
                    break;

                default:
                    break;
            }
        }

        public override void Draw( )
        {
            var p = this.Host.DrawingContext.CachePic( "gfx/vidmodes.lmp", "GL_NEAREST" );
            this.Host.Menu.DrawPic( ( 320 - p.Width ) / 2, 4, p );

            this._WModes = 0;
            var lnummodes = this.Host.Video.Device.AvailableModes.Length;

            for ( var i = 1; i < lnummodes && this._WModes < VideoMenu.MAX_MODEDESCS; i++ )
            {
                var m = this.Host.Video.Device.AvailableModes[i];

                var k = this._WModes;

                this._ModeDescs[k].modenum = i;
                this._ModeDescs[k].desc = string.Format( "{0}x{1}x{2}", m.Width, m.Height, m.BitsPerPixel );
                this._ModeDescs[k].iscur = false;

                if ( i == this.Host.Video.ModeNum )
                    this._ModeDescs[k].iscur = true;

                this._WModes++;
            }

            if (this._WModes > 0 )
            {
                this.Host.Menu.Print( 2 * 8, 36 + 0 * 8, "Fullscreen Modes (WIDTHxHEIGHTxBPP)" );

                var column = 8;
                var row = 36 + 2 * 8;

                for ( var i = 0; i < this._WModes; i++ )
                {
                    if (this._ModeDescs[i].iscur )
                        this.Host.Menu.PrintWhite( column, row, this._ModeDescs[i].desc );
                    else
                        this.Host.Menu.Print( column, row, this._ModeDescs[i].desc );

                    column += 13 * 8;

                    if ( i % Vid.VID_ROW_SIZE == Vid.VID_ROW_SIZE - 1 )
                    {
                        column = 8;
                        row += 8;
                    }
                }
            }

            this.Host.Menu.Print( 3 * 8, 36 + VideoMenu.MODE_AREA_HEIGHT * 8 + 8 * 2, "Video modes must be set from the" );
            this.Host.Menu.Print( 3 * 8, 36 + VideoMenu.MODE_AREA_HEIGHT * 8 + 8 * 3, "command line with -width <width>" );
            this.Host.Menu.Print( 3 * 8, 36 + VideoMenu.MODE_AREA_HEIGHT * 8 + 8 * 4, "and -bpp <bits-per-pixel>" );
            this.Host.Menu.Print( 3 * 8, 36 + VideoMenu.MODE_AREA_HEIGHT * 8 + 8 * 6, "Select windowed mode with -window" );
        }
    }
}
