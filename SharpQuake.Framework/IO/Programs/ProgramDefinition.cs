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

namespace SharpQuake.Framework.IO.Programs
{
    using Data;
    using System.Runtime.InteropServices;

    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public class ProgramDefinition
    {
        public ushort type;		// if DEF_SAVEGLOBGAL bit is set

        // the variable needs to be saved in savegames
        public ushort ofs;

        public int s_name;

        public static int SizeInBytes = Marshal.SizeOf( typeof( ProgramDefinition ) );

        public void SwapBytes( )
        {
            this.type = ( ushort ) EndianHelper.LittleShort( ( short )this.type );
            this.ofs = ( ushort ) EndianHelper.LittleShort( ( short )this.ofs );
            this.s_name = EndianHelper.LittleLong(this.s_name );
        }
    } // ddef_t;
}
