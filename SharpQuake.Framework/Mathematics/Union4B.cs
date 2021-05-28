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

namespace SharpQuake.Framework.Mathematics
{
    using System.Runtime.InteropServices;

    [StructLayout( LayoutKind.Explicit )]
    public struct Union4b
    {
        [FieldOffset( 0 )]
        public uint ui0;

        [FieldOffset( 0 )]
        public int i0;

        [FieldOffset( 0 )]
        public float f0;

        [FieldOffset( 0 )]
        public short s0;

        [FieldOffset( 2 )]
        public short s1;

        [FieldOffset( 0 )]
        public ushort us0;

        [FieldOffset( 2 )]
        public ushort us1;

        [FieldOffset( 0 )]
        public byte b0;

        [FieldOffset( 1 )]
        public byte b1;

        [FieldOffset( 2 )]
        public byte b2;

        [FieldOffset( 3 )]
        public byte b3;

        public static readonly Union4b Empty = new( 0, 0, 0, 0 );

        public Union4b( byte b0, byte b1, byte b2, byte b3 )
        {
            // Shut up compiler
            this.ui0 = 0;
            this.i0 = 0;
            this.f0 = 0;
            this.s0 = 0;
            this.s1 = 0;
            this.us0 = 0;
            this.us1 = 0;
            this.b0 = b0;
            this.b1 = b1;
            this.b2 = b2;
            this.b3 = b3;
        }
    }
}
