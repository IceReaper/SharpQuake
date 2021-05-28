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
    using System.Diagnostics;

    public static class Timer
    {
        private static Stopwatch _StopWatch;

        /// <summary>
        /// Sys_FloatTime
        /// </summary>
        public static double GetFloatTime( )
        {
            if ( Timer._StopWatch == null )
            {
                Timer._StopWatch = new( );
                Timer._StopWatch.Start( );
            }
            return Timer._StopWatch.Elapsed.TotalSeconds;
        }
    }
}
