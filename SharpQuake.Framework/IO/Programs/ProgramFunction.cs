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
    using Definitions;
    using System.Runtime.InteropServices;

    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public class ProgramFunction
    {
        public int first_statement;	// negative numbers are builtins
        public int parm_start;
        public int locals;				// total ints of parms + locals

        public int profile;		// runtime

        public int s_name;
        public int s_file;			// source file defined in

        public int numparms;

        [MarshalAs( UnmanagedType.ByValArray, SizeConst = ProgramDef.MAX_PARMS )]
        public byte[] parm_size; // [MAX_PARMS];

        public static int SizeInBytes = Marshal.SizeOf( typeof( ProgramFunction ) );

        public string FileName => ProgramsWrapper.GetString(this.s_file );

        public string Name => ProgramsWrapper.GetString(this.s_name );

        public void SwapBytes( )
        {
            this.first_statement = EndianHelper.LittleLong(this.first_statement );
            this.parm_start = EndianHelper.LittleLong(this.parm_start );
            this.locals = EndianHelper.LittleLong(this.locals );
            this.s_name = EndianHelper.LittleLong(this.s_name );
            this.s_file = EndianHelper.LittleLong(this.s_file );
            this.numparms = EndianHelper.LittleLong(this.numparms );
        }

        public override string ToString( )
        {
            return string.Format( "{{{0}: {1}()}}", this.FileName, this.Name );
        }
    } // dfunction_t;
}
