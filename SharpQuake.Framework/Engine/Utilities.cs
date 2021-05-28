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



//
// Source: common.h + common.c
//

// All of Quake's data access is through a hierarchical file system, but the contents of the file system can be transparently merged from several sources.
//
// The "base directory" is the path to the directory holding the quake.exe and all game directories.  The sys_* files pass this to host_init in quakeparms_t->basedir.  This can be overridden with the "-basedir" command line parm to allow code debugging in a different directory.  The base directory is
// only used during filesystem initialization.
//
// The "game directory" is the first tree on the search path and directory that all generated files (savegames, screenshots, demos, config files) will be saved to.  This can be overridden with the "-game" command line parameter.  The game directory can never be changed while quake is executing.  This is a precacution against having a malicious server instruct clients to write files over areas they shouldn't.
//
// The "cache directory" is only used during development to save network bandwidth, especially over ISDN / T1 lines.  If there is a cache directory
// specified, when a file is found by the normal search path, it will be mirrored
// into the cache directory, then opened there.
//
//
//
// FIXME:
// The file "parms.txt" will be read out of the game directory and appended to the current command line arguments to allow different games to initialize startup parms differently.  This could be used to add a "-sspeed 22050" for the high quality sound edition.  Because they are added at the end, they will not override an explicit setting on the original command line.

namespace SharpQuake.Framework.Engine
{
    using Mathematics;
    using System;
    using System.IO;
    using System.Numerics;
    using System.Runtime.InteropServices;
    using System.Text;
    using Plane = Mathematics.Plane;

    public static class Utilities
    {
        public static bool IsWindows
        {
            get
            {
                var platform = Environment.OSVersion.Platform;
                return platform == PlatformID.Win32Windows || platform == PlatformID.Win32NT || platform == PlatformID.WinCE || platform == PlatformID.Xbox;
            }
        }

        public static Vector3 ZeroVector = Vector3.Zero;

        // for passing as reference
        public static Vector3 ZeroVector3f = default;

        private static readonly byte[] ZeroBytes = new byte[4096];

		private const double COLINEAR_EPSILON = 0.001;

		public static bool SameText( string a, string b )
        {
            return string.Compare( a, b, true ) == 0;
        }

        public static bool SameText( string a, string b, int count )
        {
            return string.Compare( a, 0, b, 0, count, true ) == 0;
        }

        public static void FillArray<T>( T[] dest, T value )
        {
            var elementSizeInBytes = Marshal.SizeOf( typeof( T ) );
            var blockSize = Math.Min( dest.Length, 4096 / elementSizeInBytes );

            for ( var i = 0; i < blockSize; i++ )
                dest[i] = value;

            var blockSizeInBytes = blockSize * elementSizeInBytes;
            var offset = blockSizeInBytes;
            var lengthInBytes = Buffer.ByteLength( dest );

            while ( true )// offset + blockSize <= lengthInBytes)
            {
                var left = lengthInBytes - offset;
                if ( left < blockSizeInBytes )
                    blockSizeInBytes = left;

                if ( blockSizeInBytes <= 0 )
                    break;

                Buffer.BlockCopy( dest, 0, dest, offset, blockSizeInBytes );
                offset += blockSizeInBytes;
            }
        }

        public static void ZeroArray<T>( T[] dest, int startIndex, int length )
        {
            var elementBytes = Marshal.SizeOf( typeof( T ) );
            var offset = startIndex * elementBytes;
            var sizeInBytes = dest.Length * elementBytes - offset;

            while ( true )
            {
                var blockSize = sizeInBytes - offset;
                if ( blockSize > Utilities.ZeroBytes.Length )
                    blockSize = Utilities.ZeroBytes.Length;

                if ( blockSize <= 0 )
                    break;

                Buffer.BlockCopy( Utilities.ZeroBytes, 0, dest, offset, blockSize );
                offset += blockSize;
            }
        }

        public static string Copy( string src, int maxLength )
        {
            if ( src == null )
                return null;

            return src.Length > maxLength ? src.Substring( 1, maxLength ) : src;
        }

        public static void Copy( float[] src, out Vector3 dest )
        {
            dest.X = src[0];
            dest.Y = src[1];
            dest.Z = src[2];
        }

        public static void Copy( ref Vector3 src, float[] dest )
        {
            dest[0] = src.X;
            dest[1] = src.Y;
            dest[2] = src.Z;
        }

        public static string GetString( byte[] src )
        {
            var count = 0;

            while ( count < src.Length && src[count] != 0 )
                count++;

            return count > 0 ? Encoding.ASCII.GetString( src, 0, count ) : string.Empty;
        }

        public static Vector3 ToVector( ref Vector3 v )
        {
            return new( v.X, v.Y, v.Z );
        }

        public static void WriteInt( byte[] dest, int offset, int value )
        {
            var u = Union4b.Empty;
            u.i0 = value;
            dest[offset + 0] = u.b0;
            dest[offset + 1] = u.b1;
            dest[offset + 2] = u.b2;
            dest[offset + 3] = u.b3;
        }

        /// <summary>
        /// Sys_Error
        /// an error will cause the entire program to exit
        /// </summary>
        public static void Error( string fmt, params object[] args )
        {
            throw new QuakeSystemError( args.Length > 0 ? string.Format( fmt, args ) : fmt );
        }

        public static T ReadStructure<T>( Stream stream )
        {
            var count = Marshal.SizeOf( typeof( T ) );
            var buf = new byte[count];

            if ( stream.Read( buf, 0, count ) < count )
                throw new IOException( "Stream reading error!" );

            return Utilities.BytesToStructure<T>( buf, 0 );
        }
        
        public static void WriteString( BinaryWriter dest, string value )
        {
            var buf = Encoding.ASCII.GetBytes( value );
            dest.Write( buf.Length );
            dest.Write( buf );
        }

        public static string ReadString( BinaryReader src )
        {
            var length = src.ReadInt32( );

            if ( length <= 0 )
                throw new( "Invalid string length: " + length.ToString( ) );

            var buf = new byte[length];
            src.Read( buf, 0, length );

            return Encoding.ASCII.GetString( buf );
        }

        public static T BytesToStructure<T>( byte[] src, int startIndex )
        {
            var handle = GCHandle.Alloc( src, GCHandleType.Pinned );

            try
            {
                var ptr = handle.AddrOfPinnedObject( );
                if ( startIndex != 0 )
                {
                    var ptr2 = ptr.ToInt64( ) + startIndex;
                    ptr = new( ptr2 );
                }
                return ( T ) Marshal.PtrToStructure( ptr, typeof( T ) );
            }
            finally
            {
                handle.Free( );
            }
        }

        public static byte[] StructureToBytes<T>( ref T src )
        {
            var buf = new byte[Marshal.SizeOf( typeof( T ) )];
            var handle = GCHandle.Alloc( buf, GCHandleType.Pinned );

            try
            {
                Marshal.StructureToPtr( src, handle.AddrOfPinnedObject( ), true );
            }
            finally
            {
                handle.Free( );
            }

            return buf;
        }

        public static void StructureToBytes<T>( ref T src, byte[] dest, int offset )
        {
            var handle = GCHandle.Alloc( dest, GCHandleType.Pinned );

            try
            {
                var addr = handle.AddrOfPinnedObject( ).ToInt64( ) + offset;
                Marshal.StructureToPtr( src, new( addr ), true );
            }
            finally
            {
                handle.Free( );
            }
        }

		/// <summary>
		/// R_CullBox
		/// Returns true if the box is completely outside the frustom
		/// </summary>
		public static bool CullBox( ref Vector3 mins, ref Vector3 maxs, ref Plane[] frustum )
		{
			for ( var i = 0; i < 4; i++ )
			{
				if ( MathLib.BoxOnPlaneSide( ref mins, ref maxs, frustum[i] ) == 2 )
					return true;
			}
			return false;
		}

		public static bool IsCollinear( float[] prev, float[] cur, float[] next )
		{
			var v1 = new Vector3( cur[0] - prev[0], cur[1] - prev[1], cur[2] - prev[2] );
			MathLib.Normalize( ref v1 );
			var v2 = new Vector3( next[0] - prev[0], next[1] - prev[1], next[2] - prev[2] );
			MathLib.Normalize( ref v2 );
			v1 -= v2;
			return Math.Abs( v1.X ) <= Utilities.COLINEAR_EPSILON &&
                Math.Abs( v1.Y ) <= Utilities.COLINEAR_EPSILON &&
                Math.Abs( v1.Z ) <= Utilities.COLINEAR_EPSILON;
		}
	}
}