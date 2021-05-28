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

namespace SharpQuake.Framework.IO.Wad
{
    using Data;
    using Engine;
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;

    /// <summary>
	/// W_functions
	/// </summary>
	public class Wad
    {
        public byte[] Data => this._Data;

        public IntPtr DataPointer => this._DataPtr;

        public const int CMP_NONE = 0;
        public const int CMP_LZSS = 1;

        public const int TYP_NONE = 0;
        public const int TYP_LABEL = 1;

        public const int TYP_LUMPY = 64;				// 64 + grab command number
        public const int TYP_PALETTE = 64;
        public const int TYP_QTEX = 65;
        public const int TYP_QPIC = 66;
        public const int TYP_SOUND = 67;
        public const int TYP_MIPTEX = 68;

        private byte[] _Data; // void* wad_base
        public Dictionary<string, WadLumpInfo> _Lumps
        {
            get;
            private set;
        }

        private string Version
        {
            get;
            set;
        }

        private GCHandle _Handle;
        private IntPtr _DataPtr;

        // W_LoadWadFile (char *filename)
        public void LoadWadFile( string filename )
        {
            this.LoadWadFile( filename, FileSystem.LoadFile( filename ) );
        }

        public void LoadWadFile( string filename, byte[] buffer )
        {
            this._Data = buffer;
            if (this._Data == null )
                Utilities.Error( "Wad.LoadWadFile: couldn't load {0}", filename );

            if (this._Handle.IsAllocated )
                this._Handle.Free( );

            this._Handle = GCHandle.Alloc(this._Data, GCHandleType.Pinned );
            this._DataPtr = this._Handle.AddrOfPinnedObject( );

            var header = Utilities.BytesToStructure<WadInfo>(this._Data, 0 );

            this.Version = Encoding.ASCII.GetString( header.identification );

            if (this.Version != "WAD2" && this.Version != "WAD3" )
                Utilities.Error( $"Wad file {filename} doesn't have WAD2 or WAD3 id, got {this.Version}\n", filename );

            var numlumps = EndianHelper.LittleLong( header.numlumps );
            var infotableofs = EndianHelper.LittleLong( header.infotableofs );
            var lumpInfoSize = Marshal.SizeOf( typeof( WadLumpInfo ) );

            this._Lumps = new( numlumps );

            for ( var i = 0; i < numlumps; i++ )
            {
                var ptr = new IntPtr(this._DataPtr.ToInt64( ) + infotableofs + i * lumpInfoSize );
                var lump = ( WadLumpInfo ) Marshal.PtrToStructure( ptr, typeof( WadLumpInfo ) );
                lump.filepos = EndianHelper.LittleLong( lump.filepos );
                lump.size = EndianHelper.LittleLong( lump.size );
                if ( lump.type == Wad.TYP_QPIC )
                {
                    ptr = new(this._DataPtr.ToInt64( ) + lump.filepos );
                    var pic = ( WadPicHeader ) Marshal.PtrToStructure( ptr, typeof( WadPicHeader ) );
                    EndianHelper.SwapPic( pic );
                    Marshal.StructureToPtr( pic, ptr, true );
                }

                this._Lumps.Add( Encoding.ASCII.GetString( lump.name ).TrimEnd( '\0' ).ToLower( ), lump );
            }
        }

        // lumpinfo_t *W_GetLumpinfo (char *name)
        public WadLumpInfo GetLumpInfo( string name )
        {
            WadLumpInfo lump;
            if(this._Lumps.TryGetValue( name, out lump ) )
                return lump;
            else
                Utilities.Error( "W_GetLumpinfo: {0} not found", name );

            // We must never be there
            throw new InvalidOperationException( "W_GetLumpinfo: Unreachable code reached!" );
        }

        // void	*W_GetLumpName (char *name)
        // Uze: returns index in _Data byte array where the lumpinfo_t starts
        public int GetLumpNameOffset( string name )
        {
            return this.GetLumpInfo( name ).filepos; // GetLumpInfo() never returns null
        }

        public Tuple<byte[], Size, byte[]> GetLumpBuffer( string name )
        {
            var lump = this._Lumps
                .Where( l => Encoding.ASCII.GetString( l.Value.name ).Replace( "\0", "" ).ToLower() == name.ToLower( ) )
                .FirstOrDefault( );

            if ( lump.Value == null )
                return null;

            var lumpInfo = lump.Value;

			if (this.Version == "WAD2" && lumpInfo.type != 0x44 )
			{
				var offset = this.GetLumpNameOffset( name );
				var ptr = new IntPtr(this.DataPointer.ToInt64( ) + offset );
				var picHeader = ( WadPicHeader ) Marshal.PtrToStructure( ptr, typeof( WadPicHeader ) );

				offset += Marshal.SizeOf( typeof( WadPicHeader ) );
				
				return null;
			}

			var mtOffset = lumpInfo.filepos;

            mtOffset = EndianHelper.LittleLong( mtOffset );

            if ( mtOffset == -1 )
                return null;

            //var ptr = new IntPtr( DataPointer.ToInt64( ) + offset );


            var header = Utilities.BytesToStructure<WadMipTex>(this.Data, mtOffset );

            //var header = ( WadMipTex ) Marshal.PtrToStructure( ptr, typeof( WadMipTex ) );
            var headerSize = WadMipTex.SizeInBytes;
            
            var width = EndianHelper.LittleLong( ( int ) header.width );
            var height = EndianHelper.LittleLong( ( int ) header.height );
            
			// Dirty code
			if ( name == "conchars" )
				width = height = 128;

            if ( ( width & 15 ) != 0 || ( height & 15 ) != 0 )
                Utilities.Error( "Texture {0} is not 16 aligned", name );
                        
			if ( name == "conchars" )
			{
				var draw_chars = this.Data; // draw_chars
				for ( var i = 0; i < 256 * 64; i++ )
				{
					if ( draw_chars[mtOffset + i] == 0 )
						draw_chars[mtOffset + i] = 255;   // proper transparent color
				}
			}
            var pixelCount = ( int ) ( width * height / 64 * 85 );
            var pixels = new byte[pixelCount];

            //offset += WadMipTex.SizeInBytes;

            byte[] palette = null;
			var isWad3 = this.Version == "WAD3";

			if ( isWad3 )
            {
                var lastOffset = EndianHelper.LittleLong( ( int ) header.offsets[3] );
                lastOffset += width / 8 * ( height / 8 ) + 2;

                var palOffset = mtOffset + lastOffset;

                palette = new byte[256 * 3];
                Buffer.BlockCopy(this.Data, palOffset, palette, 0, palette.Length );
            }

#warning BlockCopy tries to copy data over the bounds of _ModBase if certain mods are loaded. Needs proof fix!
            if ( mtOffset + WadMipTex.SizeInBytes + pixelCount <= this.Data.Length )
                Buffer.BlockCopy(this.Data, mtOffset + ( isWad3 ? WadMipTex.SizeInBytes : 0 ), pixels, 0, pixelCount );
            else
            {
                Buffer.BlockCopy(this.Data, mtOffset + ( isWad3 ? WadMipTex.SizeInBytes : 0 ), pixels, 0, pixelCount );
                ConsoleWrapper.Print( $"Texture info of {name} truncated to fit in bounds of _ModBase\n" );
            }

            return new( pixels, new( width, height ), palette );
        }
    }
}