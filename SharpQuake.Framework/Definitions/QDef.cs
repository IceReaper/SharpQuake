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

//define	PARANOID			// speed sapping error checking


// quakedef.h

namespace SharpQuake.Framework.Definitions
{
    public static class QDef
    {
        public const float VERSION = 1.09f;
        public const float CSQUAKE_VERSION = 1.50f;
        public const float GLQUAKE_VERSION = 1.00f;
        public const float D3DQUAKE_VERSION = 0.01f;
        public const float WINQUAKE_VERSION = 0.996f;
        public const float LINUX_VERSION = 1.30f;
        public const float X11_VERSION = 1.10f;

        public const string GAMENAME = "Id1";		// directory to look in by default

        public const int MAX_NUM_ARGVS = 50;

        // up / down
        public const int PITCH = 0;

        // left / right
        public const int YAW = 1;

        // fall over
        public const int ROLL = 2;

        public const int MAX_QPATH = 64;			// max length of a quake game pathname
        public const int MAX_OSPATH = 128;			// max length of a filesystem pathname

        public const float ON_EPSILON = 0.1f;		// point on plane side epsilon

        public const int MAX_MSGLEN = 8000;		// max length of a reliable message
        public const int MAX_DATAGRAM = 1024;		// max length of unreliable message

        //
        // per-level limits
        //
        public const int MAX_EDICTS = 600;	//600 	// FIXME: ouch! ouch! ouch!

        public const int MAX_LIGHTSTYLES = 64;
        public const int MAX_MODELS = 256;	//256		// these are sent over the net as bytes
        public const int MAX_SOUNDS = 256;			// so they cannot be blindly increased

        public const int SAVEGAME_COMMENT_LENGTH = 39;

        public const int MAX_STYLESTRING = 64;

        public const int MAX_SCOREBOARD = 16;
        public const int MAX_SCOREBOARDNAME = 32;

        public const int SOUND_CHANNELS = 8;

        public const double BACKFACE_EPSILON = 0.01;
    }
}
