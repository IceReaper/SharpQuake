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

    public class OptionsMenu : MenuBase
    {
        private const int OPTIONS_ITEMS = 13;

        //private float _BgmVolumeCoeff = 0.1f;

        public override void Show( Host host )
        {
            /*if( sys.IsWindows )  fix cd audio first
             {
                 _BgmVolumeCoeff = 1.0f;
             }*/

            if (this._Cursor > OptionsMenu.OPTIONS_ITEMS - 1 )
                this._Cursor = 0;

            if (this._Cursor == OptionsMenu.OPTIONS_ITEMS - 1 && MenuBase.VideoMenuInstance == null )
                this._Cursor = 0;

            base.Show( host );
        }

        public override void KeyEvent( int key )
        {
            switch ( key )
            {
                case KeysDef.K_ESCAPE:
                    MenuBase.MainMenuInstance.Show(this.Host );
                    break;

                case KeysDef.K_ENTER:
                    this.Host.Menu.EnterSound = true;
                    switch (this._Cursor )
                    {
                        case 0:
                            MenuBase.KeysMenuInstance.Show(this.Host );
                            break;

                        case 1:
                            MenuBase.CurrentMenu.Hide( );
                            this.Host.Console.ToggleConsole_f( null );
                            break;

                        case 2:
                            this.Host.Commands.Buffer.Append( "exec default.cfg\n" );
                            break;

                        case 12:
                            MenuBase.VideoMenuInstance.Show(this.Host );
                            break;

                        default:
                            this.AdjustSliders( 1 );
                            break;
                    }
                    return;

                case KeysDef.K_UPARROW:
                    this.Host.Sound.LocalSound( "misc/menu1.wav" );
                    this._Cursor--;
                    if (this._Cursor < 0 )
                        this._Cursor = OptionsMenu.OPTIONS_ITEMS - 1;
                    break;

                case KeysDef.K_DOWNARROW:
                    this.Host.Sound.LocalSound( "misc/menu1.wav" );
                    this._Cursor++;
                    if (this._Cursor >= OptionsMenu.OPTIONS_ITEMS )
                        this._Cursor = 0;
                    break;

                case KeysDef.K_LEFTARROW:
                    this.AdjustSliders( -1 );
                    break;

                case KeysDef.K_RIGHTARROW:
                    this.AdjustSliders( 1 );
                    break;
            }

            /*if( _Cursor == 12 && VideoMenu == null )
            {
                if( key == KeysDef.K_UPARROW )
                    _Cursor = 11;
                else
                    _Cursor = 0;
            }*/

            if (this._Cursor == 12 )
            {
                if ( key == KeysDef.K_UPARROW )
                    this._Cursor = 11;
                else
                    this._Cursor = 0;
            }

            /*#if _WIN32
                        if ((options_cursor == 13) && (modestate != MS_WINDOWED))
                        {
                            if (k == K_UPARROW)
                                options_cursor = 12;
                            else
                                options_cursor = 0;
                        }
            #endif*/
        }

        public override void Draw( )
        {
            this.Host.Menu.DrawTransPic( 16, 4, this.Host.DrawingContext.CachePic( "gfx/qplaque.lmp", "GL_NEAREST" ) );
            var p = this.Host.DrawingContext.CachePic( "gfx/p_option.lmp", "GL_NEAREST" );
            this.Host.Menu.DrawPic( ( 320 - p.Width ) / 2, 4, p );

            this.Host.Menu.Print( 16, 32, "    Customize controls" );
            this.Host.Menu.Print( 16, 40, "         Go to console" );
            this.Host.Menu.Print( 16, 48, "     Reset to defaults" );

            this.Host.Menu.Print( 16, 56, "           Screen size" );
            var r = (this.Host.Screen.ViewSize.Get<float>( ) - 30 ) / ( 120 - 30 );
            this.Host.Menu.DrawSlider( 220, 56, r );

            this.Host.Menu.Print( 16, 64, "            Brightness" );
            r = ( 1.0f - this.Host.View.Gamma ) / 0.5f;
            this.Host.Menu.DrawSlider( 220, 64, r );

            this.Host.Menu.Print( 16, 72, "           Mouse Speed" );
            r = (this.Host.Client.Sensitivity - 1 ) / 10;
            this.Host.Menu.DrawSlider( 220, 72, r );

            this.Host.Menu.Print( 16, 80, "       CD Music Volume" );
            r = this.Host.Sound.BgmVolume;
            this.Host.Menu.DrawSlider( 220, 80, r );

            this.Host.Menu.Print( 16, 88, "          Sound Volume" );
            r = this.Host.Sound.Volume;
            this.Host.Menu.DrawSlider( 220, 88, r );

            this.Host.Menu.Print( 16, 96, "            Always Run" );
            this.Host.Menu.DrawCheckbox( 220, 96, this.Host.Client.ForwardSpeed > 200 );

            this.Host.Menu.Print( 16, 104, "          Invert Mouse" );
            this.Host.Menu.DrawCheckbox( 220, 104, this.Host.Client.MPitch < 0 );

            this.Host.Menu.Print( 16, 112, "            Lookspring" );
            this.Host.Menu.DrawCheckbox( 220, 112, this.Host.Client.LookSpring );

            this.Host.Menu.Print( 16, 120, "            Lookstrafe" );
            this.Host.Menu.DrawCheckbox( 220, 120, this.Host.Client.LookStrafe );

            /*if( VideoMenu != null )
                Host.Menu.Print( 16, 128, "         Video Options" );*/

#if _WIN32
	if (modestate == MS_WINDOWED)
	{
		Host.Menu.Print (16, 136, "             Use Mouse");
		Host.Menu.DrawCheckbox (220, 136, _windowed_mouse.value);
	}
#endif

            // cursor
            this.Host.Menu.DrawCharacter( 200, 32 + this._Cursor * 8, 12 + ( ( int ) (this.Host.RealTime * 4 ) & 1 ) );
        }

        /// <summary>
        /// M_AdjustSliders
        /// </summary>
        private void AdjustSliders( int dir )
        {
            this.Host.Sound.LocalSound( "misc/menu3.wav" );
            float value;

            switch (this._Cursor )
            {
                case 3:	// screen size
                    value = this.Host.Screen.ViewSize.Get<float>( ) + dir * 10;
                    if ( value < 30 )
                        value = 30;
                    if ( value > 120 )
                        value = 120;

                    this.Host.CVars.Set( "viewsize", value );
                    break;

                case 4:	// gamma
                    value = this.Host.View.Gamma - dir * 0.05f;
                    if ( value < 0.5 )
                        value = 0.5f;
                    if ( value > 1 )
                        value = 1;

                    this.Host.CVars.Set( "gamma", value );
                    break;

                case 5:	// mouse speed
                    value = this.Host.Client.Sensitivity + dir * 0.5f;
                    if ( value < 1 )
                        value = 1;
                    if ( value > 11 )
                        value = 11;

                    this.Host.CVars.Set( "sensitivity", value );
                    break;

                case 6:	// music volume
                    value = this.Host.Sound.BgmVolume + dir * 0.1f; ///_BgmVolumeCoeff;
                    if ( value < 0 )
                        value = 0;
                    if ( value > 1 )
                        value = 1;

                    this.Host.CVars.Set( "bgmvolume", value );
                    break;

                case 7:	// sfx volume
                    value = this.Host.Sound.Volume + dir * 0.1f;
                    if ( value < 0 )
                        value = 0;
                    if ( value > 1 )
                        value = 1;

                    this.Host.CVars.Set( "volume", value );
                    break;

                case 8:	// allways run
                    if (this.Host.Client.ForwardSpeed > 200 )
                    {
                        this.Host.CVars.Set( "cl_forwardspeed", 200f );
                        this.Host.CVars.Set( "cl_backspeed", 200f );
                    }
                    else
                    {
                        this.Host.CVars.Set( "cl_forwardspeed", 400f );
                        this.Host.CVars.Set( "cl_backspeed", 400f );
                    }
                    break;

                case 9:	// invert mouse
                    this.Host.CVars.Set( "m_pitch", -this.Host.Client.MPitch );
                    break;

                case 10:	// lookspring
                    this.Host.CVars.Set( "lookspring", !this.Host.Client.LookSpring );
                    break;

                case 11:	// lookstrafe
                    this.Host.CVars.Set( "lookstrafe", !this.Host.Client.LookStrafe );
                    break;

#if _WIN32
	        case 13:	// _windowed_mouse
		        Cvar_SetValue ("_windowed_mouse", !_windowed_mouse.value);
		        break;
#endif
            }
        }
    }
}
