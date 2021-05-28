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
	using IO.Input;

	public static class KeysDef
	{
		//
		// these are the key numbers that should be passed to Key_Event
		//
		public const int K_TAB = 9;

		public const int K_ENTER = 13;

		public const int K_ESCAPE = 27;

		public const int K_SPACE = 32;

		public const int K_BACKSPACE = 127;

		// normal keys should be passed as lowercased ascii
		public const int K_UPARROW = 128;

		public const int K_DOWNARROW = 129;

		public const int K_LEFTARROW = 130;

		public const int K_RIGHTARROW = 131;

		public const int K_ALT = 132;

		public const int K_CTRL = 133;

		public const int K_SHIFT = 134;

		public const int K_F1 = 135;

		public const int K_F2 = 136;

		public const int K_F3 = 137;

		public const int K_F4 = 138;

		public const int K_F5 = 139;

		public const int K_F6 = 140;

		public const int K_F7 = 141;

		public const int K_F8 = 142;

		public const int K_F9 = 143;

		public const int K_F10 = 144;

		public const int K_F11 = 145;

		public const int K_F12 = 146;

		public const int K_INS = 147;

		public const int K_DEL = 148;

		public const int K_PGDN = 149;

		public const int K_PGUP = 150;

		public const int K_HOME = 151;

		public const int K_END = 152;

		public const int K_PAUSE = 255;

		//
		// mouse buttons generate virtual keys
		//
		public const int K_MOUSE1 = 200;

		public const int K_MOUSE2 = 201;

		public const int K_MOUSE3 = 202;

		//
		// joystick buttons
		//
		public const int K_JOY1 = 203;

		public const int K_JOY2 = 204;

		public const int K_JOY3 = 205;

		public const int K_JOY4 = 206;

		//
		// aux keys are for multi-buttoned joysticks to generate so they can use
		// the normal binding process
		//
		public const int K_AUX1 = 207;

		public const int K_AUX2 = 208;

		public const int K_AUX3 = 209;

		public const int K_AUX4 = 210;

		public const int K_AUX5 = 211;

		public const int K_AUX6 = 212;

		public const int K_AUX7 = 213;

		public const int K_AUX8 = 214;

		public const int K_AUX9 = 215;

		public const int K_AUX10 = 216;

		public const int K_AUX11 = 217;

		public const int K_AUX12 = 218;

		public const int K_AUX13 = 219;

		public const int K_AUX14 = 220;

		public const int K_AUX15 = 221;

		public const int K_AUX16 = 222;

		public const int K_AUX17 = 223;

		public const int K_AUX18 = 224;

		public const int K_AUX19 = 225;

		public const int K_AUX20 = 226;

		public const int K_AUX21 = 227;

		public const int K_AUX22 = 228;

		public const int K_AUX23 = 229;

		public const int K_AUX24 = 230;

		public const int K_AUX25 = 231;

		public const int K_AUX26 = 232;

		public const int K_AUX27 = 233;

		public const int K_AUX28 = 234;

		public const int K_AUX29 = 235;

		public const int K_AUX30 = 236;

		public const int K_AUX31 = 237;

		public const int K_AUX32 = 238;

		public const int K_MWHEELUP = 239;

		// JACK: Intellimouse(c) Mouse Wheel Support
		public const int K_MWHEELDOWN = 240;

		public const int MAXCMDLINE = 256;

		public static KeyName[] KeyNames = new KeyName[]
		{
			new("TAB", KeysDef.K_TAB),
			new("ENTER", KeysDef.K_ENTER),
			new("ESCAPE", KeysDef.K_ESCAPE),
			new("SPACE", KeysDef.K_SPACE),
			new("BACKSPACE", KeysDef.K_BACKSPACE),
			new("UPARROW", KeysDef.K_UPARROW),
			new("DOWNARROW", KeysDef.K_DOWNARROW),
			new("LEFTARROW", KeysDef.K_LEFTARROW),
			new("RIGHTARROW", KeysDef.K_RIGHTARROW),

			new("ALT", KeysDef.K_ALT),
			new("CTRL", KeysDef.K_CTRL),
			new("SHIFT", KeysDef.K_SHIFT),

			new("F1", KeysDef.K_F1),
			new("F2", KeysDef.K_F2),
			new("F3", KeysDef.K_F3),
			new("F4", KeysDef.K_F4),
			new("F5", KeysDef.K_F5),
			new("F6", KeysDef.K_F6),
			new("F7", KeysDef.K_F7),
			new("F8", KeysDef.K_F8),
			new("F9", KeysDef.K_F9),
			new("F10", KeysDef.K_F10),
			new("F11", KeysDef.K_F11),
			new("F12", KeysDef.K_F12),

			new("INS", KeysDef.K_INS),
			new("DEL", KeysDef.K_DEL),
			new("PGDN", KeysDef.K_PGDN),
			new("PGUP", KeysDef.K_PGUP),
			new("HOME", KeysDef.K_HOME),
			new("END", KeysDef.K_END),

			new("MOUSE1", KeysDef.K_MOUSE1),
			new("MOUSE2", KeysDef.K_MOUSE2),
			new("MOUSE3", KeysDef.K_MOUSE3),

			new("JOY1", KeysDef.K_JOY1),
			new("JOY2", KeysDef.K_JOY2),
			new("JOY3", KeysDef.K_JOY3),
			new("JOY4", KeysDef.K_JOY4),

			new("AUX1", KeysDef.K_AUX1),
			new("AUX2", KeysDef.K_AUX2),
			new("AUX3", KeysDef.K_AUX3),
			new("AUX4", KeysDef.K_AUX4),
			new("AUX5", KeysDef.K_AUX5),
			new("AUX6", KeysDef.K_AUX6),
			new("AUX7", KeysDef.K_AUX7),
			new("AUX8", KeysDef.K_AUX8),
			new("AUX9", KeysDef.K_AUX9),
			new("AUX10", KeysDef.K_AUX10),
			new("AUX11", KeysDef.K_AUX11),
			new("AUX12", KeysDef.K_AUX12),
			new("AUX13", KeysDef.K_AUX13),
			new("AUX14", KeysDef.K_AUX14),
			new("AUX15", KeysDef.K_AUX15),
			new("AUX16", KeysDef.K_AUX16),
			new("AUX17", KeysDef.K_AUX17),
			new("AUX18", KeysDef.K_AUX18),
			new("AUX19", KeysDef.K_AUX19),
			new("AUX20", KeysDef.K_AUX20),
			new("AUX21", KeysDef.K_AUX21),
			new("AUX22", KeysDef.K_AUX22),
			new("AUX23", KeysDef.K_AUX23),
			new("AUX24", KeysDef.K_AUX24),
			new("AUX25", KeysDef.K_AUX25),
			new("AUX26", KeysDef.K_AUX26),
			new("AUX27", KeysDef.K_AUX27),
			new("AUX28", KeysDef.K_AUX28),
			new("AUX29", KeysDef.K_AUX29),
			new("AUX30", KeysDef.K_AUX30),
			new("AUX31", KeysDef.K_AUX31),
			new("AUX32", KeysDef.K_AUX32),

			new("PAUSE", KeysDef.K_PAUSE),

			new("MWHEELUP", KeysDef.K_MWHEELUP),
			new("MWHEELDOWN", KeysDef.K_MWHEELDOWN),

			new("SEMICOLON", ';') // because a raw semicolon seperates commands
        };

		public static byte[] KeyTable = new byte[352]
		{
			0, 0, 0, 0, 0, 0, 0, 0, // 0 - 7
			0, 0, 0, 0, 0, 0, 0, 0, // 8 - 15
			0, 0, 0, 0, 0, 0, 0, 0, // 16 - 23
			0, 0, 0, 0, 0, 0, 0, 0, // 24 - 31
			KeysDef.K_SPACE, 0, 0, 0, 0, 0, 0, (byte)'\'', // 32 - 39
			0, 0, 0, 0, (byte)',', (byte)'-', (byte)'.', (byte)'/', // 40 - 47
			(byte)'0', (byte)'1', (byte)'2', (byte)'3', (byte)'4', (byte)'5', (byte)'6', (byte)'7', // 48 - 55
			(byte)'8', (byte)'9', 0, (byte)';', 0, (byte)'=', 0, 0, // 56 - 63
			0, (byte)'a', (byte)'b', (byte)'c', (byte)'d', (byte)'e', (byte)'f', (byte)'g', // 64 - 71
			(byte)'h', (byte)'i', (byte)'j', (byte)'k', (byte)'l', (byte)'m', (byte)'n', (byte)'o', // 72 - 79
			(byte)'p', (byte)'q', (byte)'r', (byte)'s', (byte)'t', (byte)'u', (byte)'v', (byte)'w', // 80 - 87
			(byte)'x', (byte)'y', (byte)'z', (byte)'[', (byte)'\\', (byte)']', 0, 0, // 88 - 95
			(byte)'`', 0, 0, 0, 0, 0, 0, 0, // 96 - 103
			0, 0, 0, 0, 0, 0, 0, 0, // 104 - 111
			0, 0, 0, 0, 0, 0, 0, 0, // 112 - 119
			0, 0, 0, 0, 0, 0, 0, 0, // 120 - 127
			0, 0, 0, 0, 0, 0, 0, 0, // 128 - 135
			0, 0, 0, 0, 0, 0, 0, 0, // 136 - 143
			0, 0, 0, 0, 0, 0, 0, 0, // 144 - 151
			0, 0, 0, 0, 0, 0, 0, 0, // 152 - 159
			0, 0, 0, 0, 0, 0, 0, 0, // 160 - 167
			0, 0, 0, 0, 0, 0, 0, 0, // 168 - 175
			0, 0, 0, 0, 0, 0, 0, 0, // 176 - 183
			0, 0, 0, 0, 0, 0, 0, 0, // 184 - 191
			0, 0, 0, 0, 0, 0, 0, 0, // 192 - 199
			0, 0, 0, 0, 0, 0, 0, 0, // 200 - 207
			0, 0, 0, 0, 0, 0, 0, 0, // 208 - 215
			0, 0, 0, 0, 0, 0, 0, 0, // 216 - 223
			0, 0, 0, 0, 0, 0, 0, 0, // 224 - 231
			0, 0, 0, 0, 0, 0, 0, 0, // 232 - 239
			0, 0, 0, 0, 0, 0, 0, 0, // 240 - 247
			0, 0, 0, 0, 0, 0, 0, 0, // 248 - 255
			KeysDef.K_ESCAPE, KeysDef.K_ENTER, KeysDef.K_TAB, KeysDef.K_BACKSPACE, KeysDef.K_INS, KeysDef.K_DEL, KeysDef.K_RIGHTARROW, KeysDef.K_LEFTARROW, // 256 - 263
			KeysDef.K_DOWNARROW, KeysDef.K_UPARROW, KeysDef.K_PGUP, KeysDef.K_PGDN, KeysDef.K_HOME, KeysDef.K_END, 0, 0, // 264 - 271
			0, 0, 0, 0, 0, 0, 0, 0, // 272 - 279
			0, 0, 0, 0, KeysDef.K_PAUSE, 0, 0, 0, // 280 - 287
			0, 0, KeysDef.K_F1, KeysDef.K_F2, KeysDef.K_F3, KeysDef.K_F4, KeysDef.K_F5, KeysDef.K_F6, // 288 - 295
			KeysDef.K_F7, KeysDef.K_F8, KeysDef.K_F9, KeysDef.K_F10, KeysDef.K_F11, KeysDef.K_F12, 0, 0, // 296 - 303
			0, 0, 0, 0, 0, 0, 0, 0, // 304 - 311
			0, 0, 0, 0, 0, 0, 0, 0, // 312 - 319
			(byte)'0', (byte)'1', (byte)'2', (byte)'3', (byte)'4', (byte)'5', (byte)'6', (byte)'7', // 320 - 327
			(byte)'8', (byte)'9', (byte)'.', (byte)'/', (byte)'*', (byte)'-', (byte)'+', KeysDef.K_ENTER, // 328 - 335
			(byte)'=', 0, 0, 0, KeysDef.K_SHIFT, KeysDef.K_CTRL, KeysDef.K_ALT, 0, // 336 - 343
			KeysDef.K_SHIFT, KeysDef.K_CTRL, KeysDef.K_ALT, 0, 0, 0, 0, 0 // 344 - 351
        };
	}
}
