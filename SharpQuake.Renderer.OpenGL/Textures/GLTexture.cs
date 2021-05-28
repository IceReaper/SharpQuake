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

namespace SharpQuake.Renderer.OpenGL.Textures
{
    using Framework;
    using Framework.Data;
    using Framework.Definitions;
    using OpenTK.Graphics.OpenGL;
    using Renderer.Textures;
    using System.Linq;
    using System.Runtime.InteropServices;

    public class GLTexture : BaseTexture
    {
        public static int CurrentTextureNumber
        {
            get;
            set;
        }

        public static int Texels
        {
            get;
            set;
        }

        static GLTexture()
        {
            GLTexture.CurrentTextureNumber = 1;
        }

        public GLTextureDesc GLDesc
        {
            get => ( GLTextureDesc )this.Desc;
            set => this.Desc = ( BaseTextureDesc ) value;
        }

        // gl_lightmap_format = 4
        public PixelInternalFormat SolidFormat = PixelInternalFormat.Three;

        // gl_solid_format = 3
        public PixelInternalFormat AlphaFormat = PixelInternalFormat.Four;


        public GLTexture( GLDevice device, GLTextureDesc desc ) : base( device, desc )
        {

        }

        public override void Initialise( ByteArraySegment buffer )
        {
            base.Initialise( buffer );

            if ( !this.Desc.IsLightMap )
            {
                this.GLDesc.TextureNumber = GLTexture.CurrentTextureNumber;
                GLTexture.GenerateTextureNumber( );
            }
        }

        public override void Initialise( uint[] buffer )
        {
            base.Initialise( buffer );

            if ( !this.Desc.IsLightMap )
            {
                this.GLDesc.TextureNumber = GLTexture.CurrentTextureNumber;
                GLTexture.GenerateTextureNumber( );
            }
        }

        public override void Bind( )
        {
            GL.BindTexture( TextureTarget.Texture2D, this.GLDesc.TextureNumber );
        }

        public override void Upload( bool resample )
        {
            if (this.Desc.IsLightMap )
            {
                this.GLDesc.TextureNumber = GLTexture.CurrentTextureNumber;

                this.Bind( );
                this.UploadLightmap( );
            }
            else
            {
                this.Bind( );

                if (this.Buffer32?.Length > 0 )
                    this.Upload32(this.Buffer32, this.Desc.HasAlpha, resample );
                else
                    this.Upload8( resample );
            }

            if (this.Desc.IsLightMap )
                GLTexture.GenerateTextureNumber( );
        }

        // GL_Upload32
        protected override void Upload32( uint[] data, bool alpha, bool resample )
        {
            base.Upload32( data, alpha, resample );

            var filter = ( GLTextureFilter )this.Device.GetTextureFilters(this.Desc.Filter );

            var samples = alpha ? this.AlphaFormat : this.SolidFormat;
            uint[] scaled;

            GLTexture.Texels += this.Desc.ScaledWidth * this.Desc.ScaledHeight;

            if (this.Desc.ScaledWidth == this.Desc.Width && this.Desc.ScaledHeight == this.Desc.Height )
            {
                if ( !this.Desc.HasMipMap )
                {
                    var h2 = GCHandle.Alloc( data, GCHandleType.Pinned );
                    try
                    {
                        GL.TexImage2D( TextureTarget.Texture2D, 0, samples,
                            this.Desc.ScaledWidth,
                            this.Desc.ScaledHeight, 0,
                            PixelFormat.Rgba, PixelType.UnsignedByte, h2.AddrOfPinnedObject( ) );
                    }
                    finally
                    {
                        h2.Free( );
                    }
                    goto Done;
                }
                else
                {
                    scaled = new uint[this.Desc.ScaledWidth * this.Desc.ScaledHeight]; // uint[1024 * 512];
                    data.CopyTo( scaled, 0 );
                }
            }
            else if ( resample )
                this.Resample( data, this.Desc.Width, this.Desc.Height, out scaled, this.Desc.ScaledWidth, this.Desc.ScaledHeight );
            else
            {
                this.Desc.ScaledWidth = this.Desc.Width;
                this.Desc.ScaledHeight = this.Desc.Height;
                scaled = data;
            }

            var h = GCHandle.Alloc( scaled, GCHandleType.Pinned );
            try
            {
                var ptr = h.AddrOfPinnedObject( );
                GL.TexImage2D( TextureTarget.Texture2D, 0, samples,
                    this.Desc.ScaledWidth,
                    this.Desc.ScaledHeight, 0,
                    PixelFormat.Rgba, PixelType.UnsignedByte, ptr );
                var err = GL.GetError( ); // debug
                if (this.Desc.HasMipMap )
                {
                    var miplevel = 0;
                    while (this.Desc.ScaledWidth > 1 || this.Desc.ScaledHeight > 1 )
                    {
                        this.MipMap( scaled, this.Desc.ScaledWidth, this.Desc.ScaledHeight );
                        this.Desc.ScaledWidth >>= 1;
                        this.Desc.ScaledHeight >>= 1;
                        if (this.Desc.ScaledWidth < 1 )
                            this.Desc.ScaledWidth = 1;
                        if (this.Desc.ScaledHeight < 1 )
                            this.Desc.ScaledHeight = 1;
                        miplevel++;

                        GL.TexImage2D( TextureTarget.Texture2D, miplevel, samples,
                            this.Desc.ScaledWidth,
                            this.Desc.ScaledHeight, 0,
                            PixelFormat.Rgba, PixelType.UnsignedByte, ptr );
                    }
                }
            }
            finally
            {
                h.Free( );
            }

        Done:
            ;
            if ( !string.IsNullOrEmpty(this.Desc.BlendMode ) )
                this.Device.SetBlendMode(this.Desc.BlendMode );

            var min = filter.Minimise;
            var mag = filter.Maximise;

            if (this.Desc.HasMipMap )
                ( ( GLDevice )this.Device ).SetTextureFilters( min, mag );
            else
                ( ( GLDevice )this.Device ).SetTextureFilters( ( TextureMinFilter ) mag, mag );
        }

        public override void UploadLightmap( )
        {
            var lightmaps = this.Buffer.Data;

            var handle = GCHandle.Alloc( lightmaps, GCHandleType.Pinned );
            try
            {
                var ptr = handle.AddrOfPinnedObject( );
                var lmAddr = ptr.ToInt64( );

                for ( var i = 0; i < RenderDef.MAX_LIGHTMAPS; i++ )
                {
                    if (this.LightMapData[i, 0] == 0 )
                        break;		// no more used

                    this.LightMapModified[i] = false;
                    this.LightMapRectChange[i].l = RenderDef.BLOCK_WIDTH;
                    this.LightMapRectChange[i].t = RenderDef.BLOCK_HEIGHT;
                    this.LightMapRectChange[i].w = 0;
                    this.LightMapRectChange[i].h = 0;

                    GL.BindTexture( TextureTarget.Texture2D, this.GLDesc.TextureNumber + i );

                    this.Device.SetTextureFilters( "GL_LINEAR" );

                    var pixelFormat = ( GLPixelFormat )this.Device.PixelFormats.Where( p => p.Name == this.Desc.LightMapFormat ).FirstOrDefault( );

                    var addr = lmAddr + i * RenderDef.BLOCK_WIDTH * RenderDef.BLOCK_HEIGHT * this.Desc.LightMapBytes;
                    GL.TexImage2D( TextureTarget.Texture2D, 0, ( PixelInternalFormat )this.Desc.LightMapBytes,
                        RenderDef.BLOCK_WIDTH, RenderDef.BLOCK_HEIGHT, 0, pixelFormat.Value, PixelType.UnsignedByte, new( addr ) );
                    GLTexture.GenerateTextureNumber( );
                }
            }
            finally
            {
                handle.Free( );
            }

            GLTexture.GenerateTextureNumber( );
        }

        public override void CommitLightmap( byte[] data, int i )
        {
            this.LightMapModified[i] = false;
            var theRect = this.LightMapRectChange[i];
            var handle = GCHandle.Alloc( data, GCHandleType.Pinned );

            var format = ( GLPixelFormat )this.Device.PixelFormats.Where( p => p.Name == this.Desc.LightMapFormat ).FirstOrDefault( );

            try
            {
                var addr = handle.AddrOfPinnedObject( ).ToInt64( ) +
                    ( i * RenderDef.BLOCK_HEIGHT + theRect.t ) * RenderDef.BLOCK_WIDTH * this.Desc.LightMapBytes;
                GL.TexSubImage2D( TextureTarget.Texture2D, 0, 0, theRect.t,
                    RenderDef.BLOCK_WIDTH, theRect.h, format == null ? PixelFormat.Rgba : format.Value,
                    PixelType.UnsignedByte, new( addr ) );
            }
            finally
            {
                handle.Free( );
            }
            theRect.l = RenderDef.BLOCK_WIDTH;
            theRect.t = RenderDef.BLOCK_HEIGHT;
            theRect.h = 0;
            theRect.w = 0;
            this.LightMapRectChange[i] = theRect;
        }

        public override void BindLightmap( int number )
        {
            GL.BindTexture( TextureTarget.Texture2D, number );
        }

        public override void TranslateAndUpload( byte[] original, byte[] translate, int inWidth, int inHeight, int maxWidth = 512, int maxHeight = 256, int mip = 0 )
        {
            // because this happens during gameplay, do it fast
            // instead of sending it through gl_upload 8
            this.Bind( );
            //Host.DrawingContext.Bind( _PlayerTextures + playernum );

            var scaled_width = ( int ) ( maxWidth < 512 ? maxWidth : 512 );
            var scaled_height = ( int ) ( maxHeight < 256 ? maxHeight : 256 );

            // allow users to crunch sizes down even more if they want
            scaled_width >>= ( int ) mip;
            scaled_height >>= ( int ) mip;

            uint fracstep, frac;
            int destOffset;

            var translate32 = new uint[256];
            for ( var i = 0; i < 256; i++ )
                translate32[i] = this.Device.Palette.Table8to24[translate[i]];

            var dest = new uint[512 * 256];
            destOffset = 0;
            fracstep = ( uint ) ( inWidth * 0x10000 / scaled_width );
            for ( var i = 0; i < scaled_height; i++, destOffset += scaled_width )
            {
                var srcOffset = inWidth * ( i * inHeight / scaled_height );
                frac = fracstep >> 1;
                for ( var j = 0; j < scaled_width; j += 4 )
                {
                    dest[destOffset + j] = translate32[original[srcOffset + ( frac >> 16 )]];
                    frac += fracstep;
                    dest[destOffset + j + 1] = translate32[original[srcOffset + ( frac >> 16 )]];
                    frac += fracstep;
                    dest[destOffset + j + 2] = translate32[original[srcOffset + ( frac >> 16 )]];
                    frac += fracstep;
                    dest[destOffset + j + 3] = translate32[original[srcOffset + ( frac >> 16 )]];
                    frac += fracstep;
                }
            }
            var handle = GCHandle.Alloc( dest, GCHandleType.Pinned );
            try
            {
                GL.TexImage2D( TextureTarget.Texture2D, 0, PixelInternalFormat.Three, scaled_width, scaled_height, 0,
                     PixelFormat.Rgba, PixelType.UnsignedByte, handle.AddrOfPinnedObject( ) );
            }
            finally
            {
                handle.Free( );
            }
            GL.TexEnv( TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvMode, ( int ) TextureEnvMode.Modulate );
            this.Device.SetTextureFilters( "GL_LINEAR" );
        }

        /// <summary>
        /// gets texture_extension_number++
        /// </summary>
        public static int GenerateTextureNumber( )
        {
            return GLTexture.CurrentTextureNumber++;
        }
    }
}
