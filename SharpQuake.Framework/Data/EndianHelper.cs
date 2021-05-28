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

namespace SharpQuake.Framework.Data
{
    using IO.Wad;
    using System;
    using System.Numerics;

    public static class EndianHelper
    {
        private static IByteOrderConverter _Converter;

        public static IByteOrderConverter Converter => EndianHelper._Converter;

        public static bool IsBigEndian => !BitConverter.IsLittleEndian;

        static EndianHelper( )
        {
            // set the byte swapping variables in a portable manner
            if ( BitConverter.IsLittleEndian )
                EndianHelper._Converter = new LittleEndianConverter( );
            else
                EndianHelper._Converter = new BigEndianConverter( );
        }

        public static short BigShort( short l )
        {
            return EndianHelper._Converter.BigShort( l );
        }

        public static short LittleShort( short l )
        {
            return EndianHelper._Converter.LittleShort( l );
        }

        public static int BigLong( int l )
        {
            return EndianHelper._Converter.BigLong( l );
        }

        public static int LittleLong( int l )
        {
            return EndianHelper._Converter.LittleLong( l );
        }

        public static float BigFloat( float l )
        {
            return EndianHelper._Converter.BigFloat( l );
        }

        public static float LittleFloat( float l )
        {
            return EndianHelper._Converter.LittleFloat( l );
        }

        public static Vector3 LittleVector( Vector3 src )
        {
            return new( EndianHelper._Converter.LittleFloat( src.X ),
                EndianHelper._Converter.LittleFloat( src.Y ), EndianHelper._Converter.LittleFloat( src.Z ) );
        }

        public static Vector3 LittleVector3( float[] src )
        {
            return new( EndianHelper._Converter.LittleFloat( src[0] ),
                EndianHelper._Converter.LittleFloat( src[1] ), EndianHelper._Converter.LittleFloat( src[2] ) );
        }

        public static Vector4 LittleVector4( float[] src, int offset )
        {
            return new( EndianHelper._Converter.LittleFloat( src[offset + 0] ),
                EndianHelper._Converter.LittleFloat( src[offset + 1] ),
                EndianHelper._Converter.LittleFloat( src[offset + 2] ),
                EndianHelper._Converter.LittleFloat( src[offset + 3] ) );
        }

        // SwapPic (qpic_t *pic)
        public static void SwapPic( WadPicHeader pic )
        {
            pic.width = EndianHelper.LittleLong( pic.width );
            pic.height = EndianHelper.LittleLong( pic.height );
        }

    }
}
