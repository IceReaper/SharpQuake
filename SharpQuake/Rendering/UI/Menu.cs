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
/// 



// menu.h
// menu.c

namespace SharpQuake.Rendering.UI
{
    using Engine.Host;
    using Framework.IO;
    using Framework.IO.Input;
    using Menus;
    using Renderer.Textures;
    using System;

    /// <summary>
    /// M_functions
    /// </summary>
    public class Menu
    {
        public bool EnterSound;
        public bool ReturnOnError;
        public string ReturnReason;
        public MenuBase ReturnMenu;
        private const int SLIDER_RANGE = 10;

        //qboolean	m_entersound	// play after drawing a frame, so caching

        // won't disrupt the sound
        private bool _RecursiveDraw; // qboolean m_recursiveDraw

        private byte[] _IdentityTable = new byte[256]; // identityTable
        private byte[] _TranslationTable = new byte[256]; //translationTable

        // Instances
        public Host Host
        {
            get;
            private set;
        }

        public Menu( Host host )
        {
            this.Host = host;
        }

        /// <summary>
        /// M_Init
        /// </summary>
        public void Initialise( )
        {
            this.Host.Commands.Add( "togglemenu", this.ToggleMenu_f );
            this.Host.Commands.Add( "menu_main", this.Menu_Main_f );
            this.Host.Commands.Add( "menu_singleplayer", this.Menu_SinglePlayer_f );
            this.Host.Commands.Add( "menu_load", this.Menu_Load_f );
            this.Host.Commands.Add( "menu_save", this.Menu_Save_f );
            this.Host.Commands.Add( "menu_multiplayer", this.Menu_MultiPlayer_f );
            this.Host.Commands.Add( "menu_setup", this.Menu_Setup_f );
            this.Host.Commands.Add( "menu_options", this.Menu_Options_f );
            this.Host.Commands.Add( "menu_keys", this.Menu_Keys_f );
            this.Host.Commands.Add( "menu_video", this.Menu_Video_f );
            this.Host.Commands.Add( "help", this.Menu_Help_f );
            this.Host.Commands.Add( "menu_quit", this.Menu_Quit_f );
        }

        /// <summary>
        /// M_Keydown
        /// </summary>
        public void KeyDown( int key )
        {
            if( MenuBase.CurrentMenu != null )
                MenuBase.CurrentMenu.KeyEvent( key );
        }

        /// <summary>
        /// M_Draw
        /// </summary>
        public void Draw()
        {
            if( MenuBase.CurrentMenu == null || this.Host.Keyboard.Destination != KeyDestination.key_menu )
                return;

            if( !this._RecursiveDraw )
            {
                this.Host.Screen.CopyEverithing = true;

                if(this.Host.Screen.ConCurrent > 0 )
                {
                    this.Host.DrawingContext.DrawConsoleBackground(this.Host.Screen.vid.height );
                    this.Host.Sound.ExtraUpdate();
                }
                else
                    this.Host.DrawingContext.FadeScreen();

                this.Host.Screen.FullUpdate = 0;
            }
            else
                this._RecursiveDraw = false;

            if( MenuBase.CurrentMenu != null )
                MenuBase.CurrentMenu.Draw();

            if(this.EnterSound )
            {
                this.Host.Sound.LocalSound( "misc/menu2.wav" );
                this.EnterSound = false;
            }

            this.Host.Sound.ExtraUpdate();
        }

        /// <summary>
        /// M_ToggleMenu_f
        /// </summary>
        public void ToggleMenu_f( CommandMessage msg )
        {
            this.EnterSound = true;

            if(this.Host.Keyboard.Destination == KeyDestination.key_menu )
            {
                if( MenuBase.CurrentMenu != MenuBase.MainMenuInstance )
                {
                    MenuBase.MainMenuInstance.Show(this.Host );
                    return;
                }
                MenuBase.CurrentMenu.Hide();
                return;
            }
            if(this.Host.Keyboard.Destination == KeyDestination.key_console )
                this.Host.Console.ToggleConsole_f( null );
            else
                MenuBase.MainMenuInstance.Show(this.Host );
        }

        public void DrawPic( int x, int y, BasePicture pic )
        {
            this.Host.Video.Device.Graphics.DrawPicture( pic, x + ( (this.Host.Screen.vid.width - 320 ) >> 1 ), y );
        }

        public void DrawTransPic( int x, int y, BasePicture pic )
        {
            this.Host.Video.Device.Graphics.DrawPicture( pic, x + ( (this.Host.Screen.vid.width - 320 ) >> 1 ), y, hasAlpha: true );
        }

        /// <summary>
        /// M_DrawTransPicTranslate
        /// </summary>
        public void DrawTransPicTranslate( int x, int y, BasePicture pic )
        {
            this.Host.DrawingContext.TransPicTranslate( x + ( (this.Host.Screen.vid.width - 320 ) >> 1 ), y, pic, this._TranslationTable );
        }

        /// <summary>
        /// M_Print
        /// </summary>
        public void Print( int cx, int cy, string str )
        {
            for( var i = 0; i < str.Length; i++ )
            {
                this.DrawCharacter( cx, cy, str[i] + 128 );
                cx += 8;
            }
        }

        /// <summary>
        /// M_DrawCharacter
        /// </summary>
        public void DrawCharacter( int cx, int line, int num )
        {
            this.Host.DrawingContext.DrawCharacter( cx + ( (this.Host.Screen.vid.width - 320 ) >> 1 ), line, num );
        }

        /// <summary>
        /// M_PrintWhite
        /// </summary>
        public void PrintWhite( int cx, int cy, string str )
        {
            for( var i = 0; i < str.Length; i++ )
            {
                this.DrawCharacter( cx, cy, str[i] );
                cx += 8;
            }
        }

        /// <summary>
        /// M_DrawTextBox
        /// </summary>
        public void DrawTextBox( int x, int y, int width, int lines )
        {
            // draw left side
            var cx = x;
            var cy = y;
            var p = this.Host.DrawingContext.CachePic( "gfx/box_tl.lmp", "GL_NEAREST" );
            this.DrawTransPic( cx, cy, p );
            p = this.Host.DrawingContext.CachePic( "gfx/box_ml.lmp", "GL_NEAREST" );
            for( var n = 0; n < lines; n++ )
            {
                cy += 8;
                this.DrawTransPic( cx, cy, p );
            }
            p = this.Host.DrawingContext.CachePic( "gfx/box_bl.lmp", "GL_NEAREST" );
            this.DrawTransPic( cx, cy + 8, p );

            // draw middle
            cx += 8;
            while( width > 0 )
            {
                cy = y;
                p = this.Host.DrawingContext.CachePic( "gfx/box_tm.lmp", "GL_NEAREST" );
                this.DrawTransPic( cx, cy, p );
                p = this.Host.DrawingContext.CachePic( "gfx/box_mm.lmp", "GL_NEAREST" );
                for( var n = 0; n < lines; n++ )
                {
                    cy += 8;
                    if( n == 1 )
                        p = this.Host.DrawingContext.CachePic( "gfx/box_mm2.lmp", "GL_NEAREST" );

                    this.DrawTransPic( cx, cy, p );
                }
                p = this.Host.DrawingContext.CachePic( "gfx/box_bm.lmp", "GL_NEAREST" );
                this.DrawTransPic( cx, cy + 8, p );
                width -= 2;
                cx += 16;
            }

            // draw right side
            cy = y;
            p = this.Host.DrawingContext.CachePic( "gfx/box_tr.lmp", "GL_NEAREST" );
            this.DrawTransPic( cx, cy, p );
            p = this.Host.DrawingContext.CachePic( "gfx/box_mr.lmp", "GL_NEAREST" );
            for( var n = 0; n < lines; n++ )
            {
                cy += 8;
                this.DrawTransPic( cx, cy, p );
            }
            p = this.Host.DrawingContext.CachePic( "gfx/box_br.lmp", "GL_NEAREST" );
            this.DrawTransPic( cx, cy + 8, p );
        }

        /// <summary>
        /// M_DrawSlider
        /// </summary>
        public void DrawSlider( int x, int y, float range )
        {
            if( range < 0 )
                range = 0;
            if( range > 1 )
                range = 1;

            this.DrawCharacter( x - 8, y, 128 );
            int i;
            for( i = 0; i < Menu.SLIDER_RANGE; i++ )
                this.DrawCharacter( x + i * 8, y, 129 );

            this.DrawCharacter( x + i * 8, y, 130 );
            this.DrawCharacter( ( int ) ( x + ( Menu.SLIDER_RANGE - 1 ) * 8 * range ), y, 131 );
        }

        /// <summary>
        /// M_DrawCheckbox
        /// </summary>
        public void DrawCheckbox( int x, int y, bool on )
        {
            if( on )
                this.Print( x, y, "on" );
            else
                this.Print( x, y, "off" );
        }

        /// <summary>
        /// M_BuildTranslationTable
        /// </summary>
        public void BuildTranslationTable( int top, int bottom )
        {
            for( var j = 0; j < 256; j++ )
                this._IdentityTable[j] = ( byte ) j;

            this._IdentityTable.CopyTo(this._TranslationTable, 0 );

            if( top < 128 )	// the artists made some backwards ranges.  sigh.
                Array.Copy(this._IdentityTable, top, this._TranslationTable, render.TOP_RANGE, 16 ); // memcpy (dest + Render.TOP_RANGE, source + top, 16);
            else
            {
                for( var j = 0; j < 16; j++ )
                    this._TranslationTable[render.TOP_RANGE + j] = this._IdentityTable[top + 15 - j];
            }

            if( bottom < 128 )
                Array.Copy(this._IdentityTable, bottom, this._TranslationTable, render.BOTTOM_RANGE, 16 ); // memcpy(dest + Render.BOTTOM_RANGE, source + bottom, 16);
            else
            {
                for( var j = 0; j < 16; j++ )
                    this._TranslationTable[render.BOTTOM_RANGE + j] = this._IdentityTable[bottom + 15 - j];
            }
        }

        /// <summary>
        /// M_Menu_Main_f
        /// </summary>
        private void Menu_Main_f( CommandMessage msg )
        {
            MenuBase.MainMenuInstance.Show(this.Host );
        }

        /// <summary>
        /// M_Menu_SinglePlayer_f
        /// </summary>
        private void Menu_SinglePlayer_f( CommandMessage msg )
        {
            MenuBase.SinglePlayerMenuInstance.Show(this.Host );
        }

        /// <summary>
        /// M_Menu_Load_f
        /// </summary>
        /// <param name="msg"></param>
        private void Menu_Load_f( CommandMessage msg )
        {
            MenuBase.LoadMenuInstance.Show(this.Host );
        }

        /// <summary>
        /// M_Menu_Save_f
        /// </summary>
        /// <param name="msg"></param>
        private void Menu_Save_f( CommandMessage msg )
        {
            MenuBase.SaveMenuInstance.Show(this.Host );
        }

        /// <summary>
        /// M_Menu_MultiPlayer_f
        /// </summary>
        /// <param name="msg"></param>
        private void Menu_MultiPlayer_f( CommandMessage msg )
        {
            MenuBase.MultiPlayerMenuInstance.Show(this.Host );
        }

        /// <summary>
        /// M_Menu_Setup_f
        /// </summary>
        /// <param name="msg"></param>
        private void Menu_Setup_f( CommandMessage msg )
        {
            MenuBase.SetupMenuInstance.Show(this.Host );
        }

        /// <summary>
        /// M_Menu_Options_f
        /// </summary>
        /// <param name="msg"></param>
        private void Menu_Options_f( CommandMessage msg )
        {
            MenuBase.OptionsMenuInstance.Show(this.Host );
        }

        /// <summary>
        /// M_Menu_Keys_f
        /// </summary>
        private void Menu_Keys_f( CommandMessage msg )
        {
            MenuBase.KeysMenuInstance.Show(this.Host );
        }

        /// <summary>
        /// M_Menu_Video_f
        /// </summary>
        private void Menu_Video_f( CommandMessage msg )
        {
            MenuBase.VideoMenuInstance.Show(this.Host );
        }

        /// <summary>
        /// M_Menu_Help_f
        /// </summary>
        private void Menu_Help_f( CommandMessage msg )
        {
            MenuBase.HelpMenuInstance.Show(this.Host );
        }

        /// <summary>
        /// M_Menu_Quit_f
        /// </summary>
        private void Menu_Quit_f( CommandMessage msg )
        {
            MenuBase.QuitMenuInstance.Show(this.Host );
        }
    }
}
