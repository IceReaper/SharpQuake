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
    using Framework.Engine;
    using Framework.IO;
    using Framework.IO.Wad;
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.IO;
    using System.Runtime.InteropServices;

    public class BasePicture
    {
        public string Identifier
        {
            get;
            set;
        }

        public BaseTexture Texture
        {
            get;
            set;
        }

        public virtual int Width
        {
            get;
            set;
        }

        public virtual int Height
        {
            get;
            set;
        }

        public virtual RectangleF Source
        {
            get;
            set;
        }

        public static Dictionary<string, BasePicture> PicturePool
        {
            get;
            private set;
        }

        static BasePicture()
        {
            BasePicture.PicturePool = new( );
        }

        public static BasePicture FromBuffer( BaseDevice device, ByteArraySegment buffer, int width, int height, string identifier = null, string filter = "GL_LINEAR_MIPMAP_NEAREST", bool ignoreAtlas = false )
        {
            if ( BasePicture.PicturePool.ContainsKey( identifier ) )
                return BasePicture.PicturePool[identifier];

            var picture = new BasePicture( );
            picture.Width = width;
            picture.Height = height;
            picture.Identifier = identifier;

            if ( !ignoreAtlas && picture.Width < 64 && picture.Height < 64 )
                picture.Texture = device.TextureAtlas.Add( buffer, picture );
            else
                picture.Texture = BaseTexture.FromBuffer( device, picture, buffer, filter );

            return picture;
        }

        public static BasePicture FromFile( BaseDevice device, string path, string filter = "GL_LINEAR_MIPMAP_NEAREST", bool ignoreAtlas = false )
        {
            if ( BasePicture.PicturePool.ContainsKey( path ) )
                return BasePicture.PicturePool[path];

            var data = FileSystem.LoadFile( path );

            if ( data == null )
                Utilities.Error( $"BaseTexture_FromFile: failed to load {path}" );

            var ext = Path.GetExtension( path ).ToLower( ).Substring( 1 );

            switch ( ext )
            {
                case "lmp":
                    var header = Utilities.BytesToStructure<WadPicHeader>( data, 0 );

                    EndianHelper.SwapPic( header );

                    var headerSize = Marshal.SizeOf( typeof( WadPicHeader ) );

                    return BasePicture.FromBuffer( device, new( data, headerSize ), ( int ) header.width, ( int ) header.height, path, filter, ignoreAtlas );

                case "jpg":
                case "bmp":
                case "png":
                    break;

                case "tga":
                    break;
            }

            return null;
        }

        //qpic_t *Draw_PicFromWad (char *name);
        public static BasePicture FromWad( BaseDevice device, Wad wad, string name, string filter = "GL_LINEAR_MIPMAP_NEAREST" )
        {
            if ( BasePicture.PicturePool.ContainsKey( name ) )
                return BasePicture.PicturePool[name];

            var offset = wad.GetLumpNameOffset( name );
            var ptr = new IntPtr( wad.DataPointer.ToInt64( ) + offset );
            var header = ( WadPicHeader ) Marshal.PtrToStructure( ptr, typeof( WadPicHeader ) );

            offset += Marshal.SizeOf( typeof( WadPicHeader ) );

            return BasePicture.FromBuffer( device, new( wad.Data, offset ), header.width, header.height, name, filter );
        }
    }
}
