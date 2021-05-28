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



// gl_draw.c

namespace SharpQuake.Rendering
{
    using Engine.Host;
    using Framework.Data;
    using Framework.Definitions;
    using Framework.Engine;
    using Framework.IO;
    using Framework.IO.Wad;
    using Framework.Rendering;
    using Renderer.Textures;
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Runtime.InteropServices;
    using Font = Renderer.Font;

    /// <summary>
    /// Draw_functions, GL_functions
    /// </summary>
    public class Drawer
    {
        public int CurrentTexture = -1;

        public string LightMapFormat = "GL_RGBA";

        private readonly GLTexture_t[] _glTextures = new GLTexture_t[DrawDef.MAX_GLTEXTURES];

        private readonly Dictionary<string, BasePicture> _MenuCachePics = new( );

        public byte[] _MenuPlayerPixels = new byte[4096];
        public int _MenuPlayerPixelWidth;
        public int _MenuPlayerPixelHeight;

        public BasePicture Disc
        {
            get;
            private set;
        }

        public BasePicture ConsoleBackground
        {
            get;
            private set;
        }

        public BasePicture BackgroundTile
        {
            get;
            private set;
        }

        private Font CharSetFont
        {
            get;
            set;
        }

        private BaseTexture TranslateTexture
        {
            get;
            set;
        }

        // texture_extension_number = 1;
        // currenttexture = -1		// to avoid unnecessary texture sets
        private MTexTarget _OldTarget = MTexTarget.TEXTURE0_SGIS;

        // oldtarget
        private int[] _CntTextures = new int[2] { -1, -1 };

        // cnttextures
        private string CurrentFilter = "GL_LINEAR_MIPMAP_NEAREST";

        // menu_cachepics
        private int _MenuNumCachePics;

        public bool IsInitialised
        {
            get;
            private set;
        }

        // CHANGE
        private Host Host
        {
            get;
            set;
        }

        public Drawer( Host host )
        {
            this.Host = host;
        }

        // Draw_Init
        public void Initialise( )
        {
            if (this.Host.Cvars.glNoBind == null )
            {
                this.Host.Cvars.glNoBind = this.Host.CVars.Add( "gl_nobind", false );
                this.Host.Cvars.glMaxSize = this.Host.CVars.Add( "gl_max_size", 8192 );
                this.Host.Cvars.glPicMip = this.Host.CVars.Add( "gl_picmip", 0f );
            }

            // 3dfx can only handle 256 wide textures
            var renderer = this.Host.Video.Device.Desc.Renderer;

            if ( renderer.Contains( "3dfx" ) || renderer.Contains( "Glide" ) )
                this.Host.CVars.Set( "gl_max_size", 256 );

            this.Host.Commands.Add( "gl_texturemode", this.TextureMode_f );
            this.Host.Commands.Add( "imagelist", this.Imagelist_f );

            // load the console background and the charset
            // by hand, because we need to write the version
            // string into the background before turning
            // it into a texture
            var offset = this.Host.GfxWad.GetLumpNameOffset( "conchars" );
            var draw_chars = this.Host.GfxWad.Data; // draw_chars
            for ( var i = 0; i < 256 * 64; i++ )
            {
                if ( draw_chars[offset + i] == 0 )
                    draw_chars[offset + i] = 255;	// proper transparent color
            }

            // Temporarily set here
            BaseTexture.PicMip = this.Host.Cvars.glPicMip.Get<float>( );
            BaseTexture.MaxSize = this.Host.Cvars.glMaxSize.Get<int>();

            this.CharSetFont = new(this.Host.Video.Device, "charset" );
            this.CharSetFont.Initialise( new( draw_chars, offset ) );

            var buf = FileSystem.LoadFile( "gfx/conback.lmp" );
            if ( buf == null )
                Utilities.Error( "Couldn't load gfx/conback.lmp" );

            var cbHeader = Utilities.BytesToStructure<WadPicHeader>( buf, 0 );
            EndianHelper.SwapPic( cbHeader );

            // hack the version number directly into the pic
            var ver = string.Format( $"(c# {QDef.CSQUAKE_VERSION,7:F2}) {QDef.VERSION,7:F2}" );
            var offset2 = Marshal.SizeOf( typeof( WadPicHeader ) ) + 320 * 186 + 320 - 11 - 8 * ver.Length;
            var y = ver.Length;
            for ( var x = 0; x < y; x++ )
                this.CharToConback( ver[x], new( buf, offset2 + ( x << 3 ) ), new( draw_chars, offset ) );

            var ncdataIndex = Marshal.SizeOf( typeof( WadPicHeader ) ); // cb->data;

            this.ConsoleBackground = BasePicture.FromBuffer(this.Host.Video.Device, new( buf, ncdataIndex ), ( int ) cbHeader.width, ( int ) cbHeader.height, "conback", "GL_LINEAR" );

            this.TranslateTexture = BaseTexture.FromDynamicBuffer(this.Host.Video.Device, "_TranslateTexture", new(this._MenuPlayerPixels ), this._MenuPlayerPixelWidth, this._MenuPlayerPixelHeight, false, true, "GL_LINEAR" );

            //
            // get the other pics we need
            //
            this.Disc = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, "disc", "GL_NEAREST" );

            this.BackgroundTile = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, "backtile", "GL_NEAREST" );

            this.IsInitialised = true;
        }

        // Draw_BeginDisc
        //
        // Draws the little blue disc in the corner of the screen.
        // Call before beginning any disc IO.
        public void BeginDisc( )
        {
            if (this.Disc != null )
            {
                this.Host.Video.Device.SetDrawBuffer( true );
                this.Host.Video.Device.Graphics.DrawPicture(this.Disc, this.Host.Screen.vid.width - 24, 0 );
                this.Host.Video.Device.SetDrawBuffer( false );
            }
        }

        // Draw_EndDisc
        // Erases the disc iHost.Console.
        // Call after completing any disc IO
        public void EndDisc( )
        {
            // nothing to do?
        }

        // Draw_TileClear
        //
        // This repeats a 64*64 tile graphic to fill the screen around a sized down
        // refresh window.
        public void TileClear( int x, int y, int w, int h )
        {
            this.BackgroundTile.Source = new( x / 64.0f, y / 64.0f, w / 64f, h / 64f );

            this.Host.Video.Device.Graphics.DrawPicture(this.BackgroundTile, x, y, w, h );
        }
        
        // Draw_FadeScreen
        public void FadeScreen( )
        {
            this.Host.Video.Device.Graphics.FadeScreen( );
            this.Host.Hud.Changed( );
        }

        // Draw_Character
        //
        // Draws one 8*8 graphics character with 0 being transparent.
        // It can be clipped to the top of the screen to allow the console to be
        // smoothly scrolled off.
        // Vertex color modification has no effect currently
        public void DrawCharacter( int x, int y, int num, Color? color = null )
        {
            this.CharSetFont.DrawCharacter( x, y, num, color );
        }

        // Draw_String
        public void DrawString( int x, int y, string str, Color? color = null )
        {
            this.CharSetFont.Draw( x, y, str, color );
        }

        // Draw_CachePic
        public BasePicture CachePic( string path, string filter = "GL_LINEAR_MIPMAP_NEAREST", bool ignoreAtlas = false )
        {
            if (this._MenuCachePics.ContainsKey( path ) )
                return this._MenuCachePics[path];

            if (this._MenuNumCachePics == DrawDef.MAX_CACHED_PICS )
                Utilities.Error( "menu_numcachepics == MAX_CACHED_PICS" );

            var picture = BasePicture.FromFile(this.Host.Video.Device, path, filter, ignoreAtlas );

            if ( picture != null )
            {
                this._MenuNumCachePics++;

                this._MenuCachePics.Add( path, picture );
            }

            return picture;
        }

        /// <summary>
        /// Draw_TransPicTranslate
        /// Only used for the player color selection menu
        /// </summary>
        public void TransPicTranslate( int x, int y, BasePicture pic, byte[] translation )
        {
            this.Host.Video.Device.Graphics.DrawTransTranslate(this.TranslateTexture, x, y, pic.Width, pic.Height, translation );
        }

        // Draw_ConsoleBackground
        public void DrawConsoleBackground( int lines )
        {
            var y = (this.Host.Screen.vid.height * 3 ) >> 2;

            if ( lines > y )
                this.Host.Video.Device.Graphics.DrawPicture(this.ConsoleBackground, 0, lines - this.Host.Screen.vid.height, this.Host.Screen.vid.width, this.Host.Screen.vid.height );
            else
            {
                var alpha = ( int ) Math.Min( 255 * ( 1.2f * lines / y ), 255 );

                this.Host.Video.Device.Graphics.DrawPicture(this.ConsoleBackground, 0, lines - this.Host.Screen.vid.height, this.Host.Screen.vid.width, this.Host.Screen.vid.height, Color.FromArgb( alpha, Color.White ) );
            }
        }

        /// <summary>
        /// GL_SelectTexture
        /// </summary>
        public void SelectTexture( MTexTarget target )
        {
            if ( !this.Host.Video.Device.Desc.SupportsMultiTexture )
                return;

            this.Host.Video.Device.SelectTexture( target );

            if ( target == this._OldTarget )
                return;

            this._CntTextures[this._OldTarget - MTexTarget.TEXTURE0_SGIS] = this.Host.DrawingContext.CurrentTexture;
            this.Host.DrawingContext.CurrentTexture = this._CntTextures[target - MTexTarget.TEXTURE0_SGIS];
            this._OldTarget = target;
        }

        /// <summary>
        /// Draw_TextureMode_f
        /// </summary>
        private void TextureMode_f( CommandMessage msg )
        {
            if ( msg.Parameters == null || msg.Parameters.Length == 0 )
            {
                foreach ( var textureFilter in this.Host.Video.Device.TextureFilters )
                {
                    if (this.CurrentFilter == textureFilter.Name )
                    {
                        this.Host.Console.Print( $"{textureFilter.Name}\n" );
                        return;
                    }
                }

                this.Host.Console.Print( "current filter is unknown???\n" );
                return;
            }

            BaseTextureFilter newFilter = null;

            foreach ( var textureFilter in this.Host.Video.Device.TextureFilters )
            {
                if ( Utilities.SameText( textureFilter.Name, msg.Parameters[0] ) )
                {
                    newFilter = textureFilter;
                    break;
                }
            }

            if ( newFilter == null )
            {
                this.Host.Console.Print( "bad filter name!\n" );
                return;
            }

            var count = 0;

            // change all the existing mipmap texture objects
            foreach ( var texture in BaseTexture.TexturePool )
            {
                var t = texture.Value;

                if ( t.Desc.HasMipMap )
                {
                    t.Desc.Filter = newFilter.Name;
                    t.Bind( );

                    this.Host.Video.Device.SetTextureFilters( newFilter.Name );

                    count++;
                }
            }

            this.Host.Console.Print( $"Set {count} textures to {newFilter.Name}\n" );
            this.CurrentFilter = newFilter.Name;
        }

        private void Imagelist_f( CommandMessage msg )
        {
            short textureCount = 0;

            foreach ( var glTexture in this._glTextures )
            {
                if ( glTexture != null )
                {
                    this.Host.Console.Print( "{0} x {1}   {2}:{3}\n", glTexture.width, glTexture.height,
                    glTexture.owner, glTexture.identifier );
                    textureCount++;
                }
            }

            this.Host.Console.Print( "{0} textures currently loaded.\n", textureCount );
        }

        private void CharToConback( int num, ByteArraySegment dest, ByteArraySegment drawChars )
        {
            var row = num >> 4;
            var col = num & 15;
            var destOffset = dest.StartIndex;
            var srcOffset = drawChars.StartIndex + ( row << 10 ) + ( col << 3 );
            //source = draw_chars + (row<<10) + (col<<3);
            var drawline = 8;

            while ( drawline-- > 0 )
            {
                for ( var x = 0; x < 8; x++ )
                {
                    if ( drawChars.Data[srcOffset + x] != 255 )
                        dest.Data[destOffset + x] = ( byte ) ( 0x60 + drawChars.Data[srcOffset + x] ); // source[x];
                }

                srcOffset += 128; // source += 128;
                destOffset += 320; // dest += 320;
            }
        }
    }
}
