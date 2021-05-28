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

namespace SharpQuake.Framework.IO.Sound
{
	using System.Numerics;
	using System.Runtime.InteropServices;

	// !!! if this is changed, it much be changed in asm_i386.h too !!!
	[StructLayout( LayoutKind.Sequential )]
	public class Channel_t
	{
		public SoundEffect_t sfx;           // sfx number
		public int leftvol;       // 0-255 volume
		public int rightvol;      // 0-255 volume
		public int end;           // end time in global paintsamples
		public int pos;           // sample position in sfx
		public int looping;       // where to loop, -1 = no looping
		public int entnum;            // to allow overriding a specific sound
		public int entchannel;        //
		public Vector3 origin;          // origin of sound effect
		public float dist_mult;        // distance multiplier (attenuation/clipK)
		public int master_vol;        // 0-255 master volume

		public void Clear( )
		{
			this.sfx = null;
			this.leftvol = 0;
			this.rightvol = 0;
			this.end = 0;
			this.pos = 0;
			this.looping = 0;
			this.entnum = 0;
			this.entchannel = 0;
			this.origin = Vector3.Zero;
			this.dist_mult = 0;
			this.master_vol = 0;
		}
	} // channel_t;
}
