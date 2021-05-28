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
    using System;
    using System.IO;

    // MSG_WriteXxx() functions
    public class MessageWriter
    {
        public byte[] Data => this._Buffer;

        public bool IsEmpty => this._Count == 0;

        public int Length => this._Count;

        public bool AllowOverflow
        {
            get; set;
        }

        public bool IsOveflowed
        {
            get; set;
        }

        public int Capacity
        {
            get => this._Buffer.Length;
            set => this.SetBufferSize( value );
        }

        public byte[] _Buffer;

        public int _Count;

        private Union4b _Val = Union4b.Empty;

        public object GetState( )
        {
            object st = null;
            this.SaveState( ref st );
            return st;
        }

        public void SaveState( ref object state )
        {
            if ( state == null )
                state = new State( );

            var st = this.GetState( state );
            if ( st.Buffer == null || st.Buffer.Length != this._Buffer.Length )
                st.Buffer = new byte[this._Buffer.Length];

            Buffer.BlockCopy(this._Buffer, 0, st.Buffer, 0, this._Buffer.Length );
            st.Count = this._Count;
        }

        public void RestoreState( object state )
        {
            var st = this.GetState( state );
            this.SetBufferSize( st.Buffer.Length );
            Buffer.BlockCopy( st.Buffer, 0, this._Buffer, 0, this._Buffer.Length );
            this._Count = st.Count;
        }

        // void MSG_WriteChar(sizebuf_t* sb, int c);
        public void WriteChar( int c )
        {
#if PARANOID
            if (c < -128 || c > 127)
                Utilities.Error("MSG_WriteChar: range error");
#endif
            this.NeedRoom( 1 );
            this._Buffer[this._Count++] = ( byte ) c;
        }

        // MSG_WriteByte(sizebuf_t* sb, int c);
        public void WriteByte( int c )
        {
#if PARANOID
            if (c < 0 || c > 255)
                Utilities.Error("MSG_WriteByte: range error");
#endif
            this.NeedRoom( 1 );
            this._Buffer[this._Count++] = ( byte ) c;
        }

        // MSG_WriteShort(sizebuf_t* sb, int c)
        public void WriteShort( int c )
        {
#if PARANOID
            if (c < short.MinValue || c > short.MaxValue)
                Utilities.Error("MSG_WriteShort: range error");
#endif
            this.NeedRoom( 2 );
            this._Buffer[this._Count++] = ( byte ) ( c & 0xff );
            this._Buffer[this._Count++] = ( byte ) ( c >> 8 );
        }

        // MSG_WriteLong(sizebuf_t* sb, int c);
        public void WriteLong( int c )
        {
            this.NeedRoom( 4 );
            this._Buffer[this._Count++] = ( byte ) ( c & 0xff );
            this._Buffer[this._Count++] = ( byte ) ( ( c >> 8 ) & 0xff );
            this._Buffer[this._Count++] = ( byte ) ( ( c >> 16 ) & 0xff );
            this._Buffer[this._Count++] = ( byte ) ( c >> 24 );
        }

        // MSG_WriteFloat(sizebuf_t* sb, float f)
        public void WriteFloat( float f )
        {
            this.NeedRoom( 4 );
            this._Val.f0 = f;
            this._Val.i0 = EndianHelper.LittleLong(this._Val.i0 );

            this._Buffer[this._Count++] = this._Val.b0;
            this._Buffer[this._Count++] = this._Val.b1;
            this._Buffer[this._Count++] = this._Val.b2;
            this._Buffer[this._Count++] = this._Val.b3;
        }

        // MSG_WriteString(sizebuf_t* sb, char* s)
        public void WriteString( string s )
        {
            var count = 1;
            if ( !string.IsNullOrEmpty( s ) )
                count += s.Length;

            this.NeedRoom( count );
            for ( var i = 0; i < count - 1; i++ )
                this._Buffer[this._Count++] = ( byte ) s[i];

            this._Buffer[this._Count++] = 0;
        }

        // SZ_Print()
        public void Print( string s )
        {
            if (this._Count > 0 && this._Buffer[this._Count - 1] == 0 )
                this._Count--; // remove previous trailing 0

            this.WriteString( s );
        }

        // MSG_WriteCoord(sizebuf_t* sb, float f)
        public void WriteCoord( float f )
        {
            this.WriteShort( ( int ) ( f * 8 ) );
        }

        // MSG_WriteAngle(sizebuf_t* sb, float f)
        public void WriteAngle( float f )
        {
            this.WriteByte( ( ( int ) f * 256 / 360 ) & 255 );
        }

        public void Write( byte[] src, int offset, int count )
        {
            if ( count > 0 )
            {
                this.NeedRoom( count );
                Buffer.BlockCopy( src, offset, this._Buffer, this._Count, count );
                this._Count += count;
            }
        }

        public void Clear( )
        {
            this._Count = 0;
        }

        public void FillFrom( Stream src, int count )
        {
            this.Clear( );
            this.NeedRoom( count );
            while (this._Count < count )
            {
                var r = src.Read(this._Buffer, this._Count, count - this._Count );
                if ( r == 0 )
                    break;

                this._Count += r;
            }
        }

        public void FillFrom( byte[] src, int startIndex, int count )
        {
            this.Clear( );
            this.NeedRoom( count );
            Buffer.BlockCopy( src, startIndex, this._Buffer, 0, count );
            this._Count = count;
        }

        // Moved to net.cs temporarily as an extension method
        //public Int32 FillFrom( Socket socket, ref EndPoint ep )
        //{
        //    Clear( );
        //    var result = net.LanDriver.Read( socket, _Buffer, _Buffer.Length, ref ep );
        //    if ( result >= 0 )
        //        _Count = result;
        //    return result;
        //}

        public void AppendFrom( byte[] src, int startIndex, int count )
        {
            this.NeedRoom( count );
            Buffer.BlockCopy( src, startIndex, this._Buffer, this._Count, count );
            this._Count += count;
        }

        protected void NeedRoom( int bytes )
        {
            if (this._Count + bytes > this._Buffer.Length )
            {
                if ( !this.AllowOverflow )
                    Utilities.Error( "MsgWriter: overflow without allowoverflow set!" );

                this.IsOveflowed = true;
                this._Count = 0;
                if ( bytes > this._Buffer.Length )
                    Utilities.Error( "MsgWriter: Requested more than whole buffer has!" );
            }
        }

        private class State
        {
            public byte[] Buffer;
            public int Count;
        }

        private void SetBufferSize( int value )
        {
            if (this._Buffer != null )
            {
                if (this._Buffer.Length == value )
                    return;

                Array.Resize( ref this._Buffer, value );

                if (this._Count > this._Buffer.Length )
                    this._Count = this._Buffer.Length;
            }
            else
                this._Buffer = new byte[value];
        }

        private State GetState( object state )
        {
            if ( state == null )
                throw new ArgumentNullException( );

            var st = state as State;
            if ( st == null )
                throw new ArgumentException( "Passed object is not a state!" );

            return st;
        }

        public MessageWriter( )
                    : this( 0 )
        {
        }

        public MessageWriter( int capacity )
        {
            this.SetBufferSize( capacity );
            this.AllowOverflow = false;
        }
    }
}
