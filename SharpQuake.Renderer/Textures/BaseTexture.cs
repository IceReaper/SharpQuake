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

namespace SharpQuake.Renderer.Textures
{
    using Framework;
    using Framework.Data;
    using Framework.Definitions;
    using Framework.Engine;
    using Framework.Mathematics;
    using System;
    using System.Collections.Generic;

    public class BaseTexture : IDisposable
    {
        public BaseDevice Device
        {
            get;
            private set;
        }

        public BaseTextureDesc Desc
        {
            get;
            protected set;
        }

        public ByteArraySegment Buffer
        {
            get;
            protected set;
        }

        public uint[] Buffer32
        {
            get;
            protected set;
        }

        // Static

        public static float MaxSize
        {
            get;
            set;
        }

        public static float PicMip
        {
            get;
            set;
        }

        public static Dictionary<string, BaseTexture> TexturePool
        {
            get;
            private set;
        }

        public int[,] LightMapData
        {
            get;
            set;
        }

        public glRect_t[] LightMapRectChange
        {
            get;
            set;
        }

        public bool[] LightMapModified
        {
            get;
            set;
        }

        static BaseTexture( )
        {
            BaseTexture.TexturePool = new( );
        }

        public BaseTexture( BaseDevice device, BaseTextureDesc desc )
        {
            this.Device = device;
            this.Desc = desc;

            BaseTexture.TexturePool.Add( desc.Name, this );

            if (this.Desc.IsLightMap )
            {
                this.LightMapData = new int[RenderDef.MAX_LIGHTMAPS, RenderDef.BLOCK_WIDTH];
                this.LightMapRectChange = new glRect_t[RenderDef.MAX_LIGHTMAPS]; // lightmap_rectchange
                this.LightMapModified = new bool[RenderDef.MAX_LIGHTMAPS]; // lightmap_modified
            }
        }

        public virtual void Initialise( ByteArraySegment buffer )
        {
            this.Buffer = buffer;
        }

        public virtual void Initialise( uint[] buffer )
        {
            this.Buffer32 = buffer;
        }

        public virtual void Bind( )
        {
            throw new NotImplementedException( );
        }

        // GL_ResampleTexture
        protected void Resample( uint[] src, int srcWidth, int srcHeight, out uint[] dest, int destWidth, int destHeight )
        {
            dest = new uint[destWidth * destHeight];
            var fracstep = srcWidth * 0x10000 / destWidth;
            var destOffset = 0;
            for ( var i = 0; i < destHeight; i++ )
            {
                var srcOffset = srcWidth * ( i * srcHeight / destHeight );
                var frac = fracstep >> 1;
                for ( var j = 0; j < destWidth; j += 4 )
                {
                    dest[destOffset + j] = src[srcOffset + ( frac >> 16 )];
                    frac += fracstep;
                    dest[destOffset + j + 1] = src[srcOffset + ( frac >> 16 )];
                    frac += fracstep;
                    dest[destOffset + j + 2] = src[srcOffset + ( frac >> 16 )];
                    frac += fracstep;
                    dest[destOffset + j + 3] = src[srcOffset + ( frac >> 16 )];
                    frac += fracstep;
                }
                destOffset += destWidth;
            }
        }

        // GL_MipMap
        //
        // Operates in place, quartering the size of the texture
        protected void MipMap( uint[] src, int width, int height )
        {
            Union4b p1 = Union4b.Empty, p2 = Union4b.Empty, p3 = Union4b.Empty, p4 = Union4b.Empty;

            width >>= 1;
            height >>= 1;

            var dest = src;
            var srcOffset = 0;
            var destOffset = 0;
            for ( var i = 0; i < height; i++ )
            {
                for ( var j = 0; j < width; j++ )
                {
                    p1.ui0 = src[srcOffset];
                    var offset = srcOffset + 1;
                    p2.ui0 = offset < src.Length ? src[offset] : p1.ui0;
                    offset = srcOffset + ( width << 1 );
                    p3.ui0 = offset < src.Length ? src[offset] : p1.ui0;
                    offset = srcOffset + ( width << 1 ) + 1;
                    p4.ui0 = offset < src.Length ? src[offset] : p1.ui0;

                    p1.b0 = ( byte ) ( ( p1.b0 + p2.b0 + p3.b0 + p4.b0 ) >> 2 );
                    p1.b1 = ( byte ) ( ( p1.b1 + p2.b1 + p3.b1 + p4.b1 ) >> 2 );
                    p1.b2 = ( byte ) ( ( p1.b2 + p2.b2 + p3.b2 + p4.b2 ) >> 2 );
                    p1.b3 = ( byte ) ( ( p1.b3 + p2.b3 + p3.b3 + p4.b3 ) >> 2 );

                    dest[destOffset] = p1.ui0;
                    destOffset++;
                    srcOffset += 2;
                }
                srcOffset += width << 1;
            }
        }

        /// <summary>
        /// GL_Upload8
        /// </summary>
        public virtual void Upload8( bool resample )
        {
            var data = this.Buffer;
            var width = this.Desc.Width;
            var height = this.Desc.Height;
            var alpha = this.Desc.HasAlpha;

            var s = width * height;
            var trans = new uint[s];
            var table = this.Device.Palette.Table8to24;
            var data1 = data.Data;
            var offset = data.StartIndex;

            // if there are no transparent pixels, make it a 3 component
            // texture even if it was specified as otherwise
            if ( alpha )
            {
                var noalpha = true;
                for ( var i = 0; i < s; i++, offset++ )
                {
                    var p = data1[offset];
                    if ( p == 255 )
                        noalpha = false;
                    trans[i] = table[p];
                }

                if ( alpha && noalpha )
                    alpha = false;
            }
            else
            {
                if ( ( s & 3 ) != 0 )
                    Utilities.Error( "GL_Upload8: s&3" );

                for ( var i = 0; i < s; i += 4, offset += 4 )
                {
                    trans[i] = table[data1[offset]];
                    trans[i + 1] = table[data1[offset + 1]];
                    trans[i + 2] = table[data1[offset + 2]];
                    trans[i + 3] = table[data1[offset + 3]];
                }
            }

            this.Upload32( trans, alpha, resample );
        }

        public virtual void Upload( bool resample )
        {
            throw new NotImplementedException( );
        }

        // GL_Upload32
        protected virtual void Upload32( uint[] data, bool alpha, bool resample )
        {
            for (this.Desc.ScaledWidth = 1; this.Desc.ScaledWidth < this.Desc.Width; this.Desc.ScaledWidth <<= 1 )
                ;
            for (this.Desc.ScaledHeight = 1; this.Desc.ScaledHeight < this.Desc.Height; this.Desc.ScaledHeight <<= 1 )
                ;

            this.Desc.ScaledWidth >>= ( int ) BaseTexture.PicMip;
            this.Desc.ScaledHeight >>= ( int ) BaseTexture.PicMip;

            if (this.Desc.ScaledWidth > BaseTexture.MaxSize )
                this.Desc.ScaledWidth = ( int ) BaseTexture.MaxSize;
            if (this.Desc.ScaledHeight > BaseTexture.MaxSize )
                this.Desc.ScaledHeight = ( int ) BaseTexture.MaxSize;
        }

        public virtual void TranslateAndUpload( byte[] original, byte[] translate, int inWidth, int inHeight, int maxWidth = 512, int maxHeight = 256, int mip = 0 )
        {
            throw new NotImplementedException( );
        }

        public virtual void UploadLightmap( )
        {
            throw new NotImplementedException( );
        }

        public virtual void BindLightmap( int number )
        {
            throw new NotImplementedException( );
        }

        public virtual void CommitLightmap( byte[] data, int i )
        {
            throw new NotImplementedException( );
        }

        public virtual void Dispose( )
        {
            throw new NotImplementedException( );
        }

        // Static methods

        public static bool ExistsInPool( string name )
        {
            return BaseTexture.TexturePool?.ContainsKey( name ) == true;
        }

        public static BaseTexture FromPool( string name )
        {
            if ( BaseTexture.ExistsInPool( name ) )
                return null;

            return BaseTexture.TexturePool[name];
        }

        public static BaseTexture FromBuffer( BaseDevice device, BaseTextureDesc desc, ByteArraySegment buffer, bool resample = true )
        {
            if ( BaseTexture.ExistsInPool( desc.Name ) )
                return BaseTexture.TexturePool[desc.Name];

            var texture = ( BaseTexture ) Activator.CreateInstance( device.TextureType, device, desc );
            texture.Initialise( buffer );
            texture.Upload( resample );

            return texture;
        }

        public static BaseTexture FromBuffer( BaseDevice device, BaseTextureDesc desc, uint[] buffer, bool resample = false )
        {
            if ( BaseTexture.ExistsInPool( desc.Name ) )
                return BaseTexture.TexturePool[desc.Name];

            var texture = ( BaseTexture ) Activator.CreateInstance( device.TextureType, device, desc );
            texture.Initialise( buffer );
            texture.Upload( resample );

            return texture;
        }

        public static BaseTexture FromBuffer( BaseDevice device, string identifier, ByteArraySegment buffer, int width, int height, bool hasMipMap, bool hasAlpha, string filter = "GL_LINEAR_MIPMAP_NEAREST", string blendMode = "", bool isLightMap = false )
        {
            if ( BaseTexture.ExistsInPool( identifier ) )
                return BaseTexture.TexturePool[identifier];

            var desc = ( BaseTextureDesc ) Activator.CreateInstance( device.TextureDescType );
            desc.Name = identifier;
            desc.HasAlpha = hasAlpha;
            desc.Width = width;
            desc.Height = height;
            desc.HasMipMap = hasMipMap;
            desc.Filter = filter;
            desc.BlendMode = blendMode;
            desc.IsLightMap = isLightMap;

            return BaseTexture.FromBuffer( device, desc, buffer );
        }

        public static BaseTexture FromBuffer( BaseDevice device, string identifier, uint[] buffer, int width, int height, bool hasMipMap, bool hasAlpha, string filter = "GL_LINEAR_MIPMAP_NEAREST", string blendMode = "", bool isLightMap = false )
        {
            if ( BaseTexture.ExistsInPool( identifier ) )
                return BaseTexture.TexturePool[identifier];

            var desc = ( BaseTextureDesc ) Activator.CreateInstance( device.TextureDescType );
            desc.Name = identifier;
            desc.HasAlpha = hasAlpha;
            desc.Width = width;
            desc.Height = height;
            desc.HasMipMap = hasMipMap;
            desc.Filter = filter;
            desc.BlendMode = blendMode;
            desc.IsLightMap = isLightMap;

            return BaseTexture.FromBuffer( device, desc, buffer );
        }
        
        /// <summary>
        /// GL_LoadPicTexture
        /// </summary>
        public static BaseTexture FromBuffer( BaseDevice device, BasePicture picture, ByteArraySegment buffer, string filter = "GL_LINEAR_MIPMAP_NEAREST", bool isLightMap = false )
        {
            if ( picture.Source.Width <= 0 )
                picture.Source = new( 0, 0, 1, 1 );

            if ( string.IsNullOrEmpty( picture.Identifier ) )
                picture.Identifier = Guid.NewGuid( ).ToString( );

            return BaseTexture.FromBuffer( device, picture.Identifier, buffer, picture.Width, picture.Height, false, true, filter, isLightMap: isLightMap );
        }

        public static BaseTexture FromBuffer( BaseDevice device, BasePicture picture, uint[] buffer, string filter = "GL_LINEAR_MIPMAP_NEAREST", bool isLightMap = false )
        {
            if ( picture.Source.Width <= 0 )
                picture.Source = new( 0, 0, 1, 1 );

            if ( string.IsNullOrEmpty( picture.Identifier ) )
                picture.Identifier = Guid.NewGuid( ).ToString( );

            return BaseTexture.FromBuffer( device, picture.Identifier, buffer, picture.Width, picture.Height, false, true, filter, isLightMap: isLightMap );
        }

        public static BaseTexture FromDynamicBuffer( BaseDevice device, string identifier, ByteArraySegment buffer, int width, int height, bool hasMipMap, bool hasAlpha, string filter = "GL_LINEAR_MIPMAP_NEAREST", string blendMode = "", bool isLightMap = false )
        {
            if ( BaseTexture.ExistsInPool( identifier ) )
                return BaseTexture.TexturePool[identifier];

            var desc = ( BaseTextureDesc ) Activator.CreateInstance( device.TextureDescType );
            desc.Name = identifier;
            desc.HasAlpha = hasAlpha;
            desc.Width = width;
            desc.Height = height;
            desc.HasMipMap = hasMipMap;
            desc.Filter = filter;
            desc.BlendMode = blendMode;
            desc.IsLightMap = isLightMap;

            var texture = ( BaseTexture ) Activator.CreateInstance( device.TextureType, device, desc );
            texture.Initialise( buffer );

            return texture;
        }

        public static void DisposePool()
        {
            if ( BaseTexture.TexturePool == null )
                return;

            foreach ( var kvp in BaseTexture.TexturePool )
                kvp.Value?.Dispose( );
        }
    }
}
