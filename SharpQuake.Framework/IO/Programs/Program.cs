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
    public class Program
    {
        public int version;
        public int crc;			// check of header file

        public int ofs_statements;
        public int numstatements;	// statement 0 is an error

        public int ofs_globaldefs;
        public int numglobaldefs;

        public int ofs_fielddefs;
        public int numfielddefs;

        public int ofs_functions;
        public int numfunctions;	// function 0 is an empty

        public int ofs_strings;
        public int numstrings;		// first string is a null string

        public int ofs_globals;
        public int numglobals;

        public int entityfields;

        public static int SizeInBytes = Marshal.SizeOf( typeof( Program ) );

        public void SwapBytes( )
        {
            this.version = EndianHelper.LittleLong(this.version );
            this.crc = EndianHelper.LittleLong(this.crc );
            this.ofs_statements = EndianHelper.LittleLong(this.ofs_statements );
            this.numstatements = EndianHelper.LittleLong(this.numstatements );
            this.ofs_globaldefs = EndianHelper.LittleLong(this.ofs_globaldefs );
            this.numglobaldefs = EndianHelper.LittleLong(this.numglobaldefs );
            this.ofs_fielddefs = EndianHelper.LittleLong(this.ofs_fielddefs );
            this.numfielddefs = EndianHelper.LittleLong(this.numfielddefs );
            this.ofs_functions = EndianHelper.LittleLong(this.ofs_functions );
            this.numfunctions = EndianHelper.LittleLong(this.numfunctions );
            this.ofs_strings = EndianHelper.LittleLong(this.ofs_strings );
            this.numstrings = EndianHelper.LittleLong(this.numstrings );
            this.ofs_globals = EndianHelper.LittleLong(this.ofs_globals );
            this.numglobals = EndianHelper.LittleLong(this.numglobals );
            this.entityfields = EndianHelper.LittleLong(this.entityfields );
        }
    } // dprograms_t;
}
