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

namespace SharpQuake.Framework.Definitions
{
	public static class ParticleDef
	{
		public const int MAX_PARTICLES = 2048;

		// default max # of particles at one time
		public const int ABSOLUTE_MIN_PARTICLES = 512;

		// no fewer than this no matter what's on the command line
		public const int NUMVERTEXNORMALS = 162;

		public static int[] _Ramp1 = new int[] { 0x6f, 0x6d, 0x6b, 0x69, 0x67, 0x65, 0x63, 0x61 };

		public static int[] _Ramp2 = new int[] { 0x6f, 0x6e, 0x6d, 0x6c, 0x6b, 0x6a, 0x68, 0x66 };

		public static int[] _Ramp3 = new int[] { 0x6d, 0x6b, 6, 5, 4, 3 };

		public static byte[,] _DotTexture = new byte[8, 8]
		{
			{0,1,1,0,0,0,0,0},
			{1,1,1,1,0,0,0,0},
			{1,1,1,1,0,0,0,0},
			{0,1,1,0,0,0,0,0},
			{0,0,0,0,0,0,0,0},
			{0,0,0,0,0,0,0,0},
			{0,0,0,0,0,0,0,0},
			{0,0,0,0,0,0,0,0}
		};
	}
}
