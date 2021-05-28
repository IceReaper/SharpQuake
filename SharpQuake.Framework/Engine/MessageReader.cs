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

namespace SharpQuake.Framework.Engine
{
    using Data;
    using Mathematics;
    using System.Numerics;

    // MSG_ReadXxx() functions
    public class MessageReader
    {
        /// <summary>
        /// msg_badread
        /// </summary>
        public bool IsBadRead => this._IsBadRead;

        /// <summary>
        /// msg_readcount
        /// </summary>
        public int Position => this._Count;

        private MessageWriter _Source;
        private bool _IsBadRead;
        private int _Count;
        private Union4b _Val;
        private char[] _Tmp;

        /// <summary>
        /// MSG_BeginReading
        /// </summary>
        public void Reset( )
        {
            this._IsBadRead = false;
            this._Count = 0;
        }

        /// <summary>
        /// MSG_ReadChar
        /// reads sbyte
        /// </summary>
        public int ReadChar( )
        {
            if ( !this.HasRoom( 1 ) )
                return -1;

            return ( sbyte )this._Source.Data[this._Count++];
        }

        // MSG_ReadByte (void)
        public int ReadByte( )
        {
            if ( !this.HasRoom( 1 ) )
                return -1;

            return ( byte )this._Source.Data[this._Count++];
        }

        // MSG_ReadShort (void)
        public int ReadShort( )
        {
            if ( !this.HasRoom( 2 ) )
                return -1;

            int c = ( short ) (this._Source.Data[this._Count + 0] + (this._Source.Data[this._Count + 1] << 8 ) );
            this._Count += 2;
            return c;
        }

        // MSG_ReadLong (void)
        public int ReadLong( )
        {
            if ( !this.HasRoom( 4 ) )
                return -1;

            var c = this._Source.Data[this._Count + 0] +
                (this._Source.Data[this._Count + 1] << 8 ) +
                (this._Source.Data[this._Count + 2] << 16 ) +
                (this._Source.Data[this._Count + 3] << 24 );

            this._Count += 4;
            return c;
        }

        // MSG_ReadFloat (void)
        public float ReadFloat( )
        {
            if ( !this.HasRoom( 4 ) )
                return 0;

            this._Val.b0 = this._Source.Data[this._Count + 0];
            this._Val.b1 = this._Source.Data[this._Count + 1];
            this._Val.b2 = this._Source.Data[this._Count + 2];
            this._Val.b3 = this._Source.Data[this._Count + 3];

            this._Count += 4;

            this._Val.i0 = EndianHelper.LittleLong(this._Val.i0 );
            return this._Val.f0;
        }

        // char *MSG_ReadString (void)
        public string ReadString( )
        {
            var l = 0;
            do
            {
                var c = this.ReadChar( );
                if ( c == -1 || c == 0 )
                    break;

                this._Tmp[l] = ( char ) c;
                l++;
            } while ( l < this._Tmp.Length - 1 );

            return new(this._Tmp, 0, l );
        }

        // float MSG_ReadCoord (void)
        public float ReadCoord( )
        {
            return this.ReadShort( ) * ( 1.0f / 8 );
        }

        // float MSG_ReadAngle (void)
        public float ReadAngle( )
        {
            return this.ReadChar( ) * ( 360.0f / 256 );
        }

        public Vector3 ReadCoords( )
        {
            Vector3 result;
            result.X = this.ReadCoord( );
            result.Y = this.ReadCoord( );
            result.Z = this.ReadCoord( );
            return result;
        }

        public Vector3 ReadAngles( )
        {
            Vector3 result;
            result.X = this.ReadAngle( );
            result.Y = this.ReadAngle( );
            result.Z = this.ReadAngle( );
            return result;
        }

        private bool HasRoom( int bytes )
        {
            if (this._Count + bytes > this._Source.Length )
            {
                this._IsBadRead = true;
                return false;
            }
            return true;
        }

        public MessageReader( MessageWriter source )
        {
            this._Source = source;
            this._Val = Union4b.Empty;
            this._Tmp = new char[2048];
        }
    }
}
