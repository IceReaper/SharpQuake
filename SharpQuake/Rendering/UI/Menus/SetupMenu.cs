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
    using Framework.IO.Wad;
    using System;
    using System.Runtime.InteropServices;

    public class SetupMenu : MenuBase
    {
        private const int NUM_SETUP_CMDS = 5;

        private readonly int[] _CursorTable = new int[]
        {
            40, 56, 80, 104, 140
        }; // setup_cursor_table

        private string _HostName; // setup_hostname[16]
        private string _MyName; // setup_myname[16]
        private int _OldTop; // setup_oldtop
        private int _OldBottom; // setup_oldbottom
        private int _Top; // setup_top
        private int _Bottom; // setup_bottom
        private bool hasPlayPixels;

        /// <summary>
        /// M_Menu_Setup_f
        /// </summary>
        public override void Show( Host host )
        {
            this._MyName = host.Client.Name;
            this._HostName = host.Network.HostName;
            this._Top = this._OldTop = ( int ) host.Client.Color >> 4;
            this._Bottom = this._OldBottom = ( int ) host.Client.Color & 15;

            base.Show( host );
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
                        this._Cursor = SetupMenu.NUM_SETUP_CMDS - 1;
                    break;

                case KeysDef.K_DOWNARROW:
                    this.Host.Sound.LocalSound( "misc/menu1.wav" );
                    this._Cursor++;
                    if (this._Cursor >= SetupMenu.NUM_SETUP_CMDS )
                        this._Cursor = 0;
                    break;

                case KeysDef.K_LEFTARROW:
                    if (this._Cursor < 2 )
                        return;

                    this.Host.Sound.LocalSound( "misc/menu3.wav" );
                    if (this._Cursor == 2 )
                        this._Top = this._Top - 1;
                    if (this._Cursor == 3 )
                        this._Bottom = this._Bottom - 1;
                    break;

                case KeysDef.K_RIGHTARROW:
                    if (this._Cursor < 2 )
                        return;
                    forward:
                    this.Host.Sound.LocalSound( "misc/menu3.wav" );
                    if (this._Cursor == 2 )
                        this._Top = this._Top + 1;
                    if (this._Cursor == 3 )
                        this._Bottom = this._Bottom + 1;
                    break;

                case KeysDef.K_ENTER:
                    if (this._Cursor == 0 || this._Cursor == 1 )
                        return;

                    if (this._Cursor == 2 || this._Cursor == 3 )
                        goto forward;

                    // _Cursor == 4 (OK)
                    if (this._MyName != this.Host.Client.Name )
                        this.Host.Commands.Buffer.Append( string.Format( "name \"{0}\"\n", this._MyName ) );
                    if (this.Host.Network.HostName != this._HostName )
                        this.Host.CVars.Set( "hostname", this._HostName );
                    if (this._Top != this._OldTop || this._Bottom != this._OldBottom )
                        this.Host.Commands.Buffer.Append( string.Format( "color {0} {1}\n", this._Top, this._Bottom ) );

                    this.Host.Menu.EnterSound = true;
                    MenuBase.MultiPlayerMenuInstance.Show(this.Host );
                    break;

                case KeysDef.K_BACKSPACE:
                    if (this._Cursor == 0 )
                    {
                        if ( !string.IsNullOrEmpty(this._HostName ) )
                            this._HostName = this._HostName.Substring( 0, this._HostName.Length - 1 );// setup_hostname[strlen(setup_hostname) - 1] = 0;
                    }

                    if (this._Cursor == 1 )
                    {
                        if ( !string.IsNullOrEmpty(this._MyName ) )
                            this._MyName = this._MyName.Substring( 0, this._MyName.Length - 1 );
                    }
                    break;

                default:
                    if ( key < 32 || key > 127 )
                        break;
                    if (this._Cursor == 0 )
                    {
                        var l = this._HostName.Length;
                        if ( l < 15 )
                            this._HostName = this._HostName + ( char ) key;
                    }
                    if (this._Cursor == 1 )
                    {
                        var l = this._MyName.Length;
                        if ( l < 15 )
                            this._MyName = this._MyName + ( char ) key;
                    }
                    break;
            }

            if (this._Top > 13 )
                this._Top = 0;
            if (this._Top < 0 )
                this._Top = 13;
            if (this._Bottom > 13 )
                this._Bottom = 0;
            if (this._Bottom < 0 )
                this._Bottom = 13;
        }

        public override void Draw( )
        {
            this.Host.Menu.DrawTransPic( 16, 4, this.Host.DrawingContext.CachePic( "gfx/qplaque.lmp", "GL_NEAREST" ) );
            var p = this.Host.DrawingContext.CachePic( "gfx/p_multi.lmp", "GL_NEAREST" );
            this.Host.Menu.DrawPic( ( 320 - p.Width ) / 2, 4, p );

            this.Host.Menu.Print( 64, 40, "Hostname" );
            this.Host.Menu.DrawTextBox( 160, 32, 16, 1 );
            this.Host.Menu.Print( 168, 40, this._HostName );

            this.Host.Menu.Print( 64, 56, "Your name" );
            this.Host.Menu.DrawTextBox( 160, 48, 16, 1 );
            this.Host.Menu.Print( 168, 56, this._MyName );

            this.Host.Menu.Print( 64, 80, "Shirt color" );
            this.Host.Menu.Print( 64, 104, "Pants color" );

            this.Host.Menu.DrawTextBox( 64, 140 - 8, 14, 1 );
            this.Host.Menu.Print( 72, 140, "Accept Changes" );

            p = this.Host.DrawingContext.CachePic( "gfx/bigbox.lmp", "GL_NEAREST" );
            this.Host.Menu.DrawTransPic( 160, 64, p );
            p = this.Host.DrawingContext.CachePic( "gfx/menuplyr.lmp", "GL_NEAREST", true );
            
            if ( !this.hasPlayPixels && p != null )
            {
                // HACK HACK HACK --- we need to keep the bytes for
                // the translatable player picture just for the menu
                // configuration dialog

                var headerSize = Marshal.SizeOf( typeof( WadPicHeader ) );
                var data = FileSystem.LoadFile( p.Identifier );
                this.Host.DrawingContext._MenuPlayerPixelWidth = p.Texture.Desc.Width;
                this.Host.DrawingContext._MenuPlayerPixelHeight = p.Texture.Desc.Height;
                Buffer.BlockCopy( data, headerSize, this.Host.DrawingContext._MenuPlayerPixels, 0, p.Texture.Desc.Width * p.Texture.Desc.Height );
                //memcpy (menuplyr_pixels, dat->data, dat->width*dat->height);

                this.hasPlayPixels = true;
            }

            this.Host.Menu.BuildTranslationTable(this._Top * 16, this._Bottom * 16 );
            this.Host.Menu.DrawTransPicTranslate( 172, 72, p );

            this.Host.Menu.DrawCharacter( 56, this._CursorTable[this._Cursor], 12 + ( ( int ) (this.Host.RealTime * 4 ) & 1 ) );

            if (this._Cursor == 0 )
                this.Host.Menu.DrawCharacter( 168 + 8 * this._HostName.Length, this._CursorTable[this._Cursor], 10 + ( ( int ) (this.Host.RealTime * 4 ) & 1 ) );

            if (this._Cursor == 1 )
                this.Host.Menu.DrawCharacter( 168 + 8 * this._MyName.Length, this._CursorTable[this._Cursor], 10 + ( ( int ) (this.Host.RealTime * 4 ) & 1 ) );
        }
    }
}
