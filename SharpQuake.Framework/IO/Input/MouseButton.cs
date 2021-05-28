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
/// Borrowed from OpenTK


namespace SharpQuake.Framework.IO.Input
{

	//
	// Summary:
	//     Enumerates all possible mouse buttons.
	public enum MouseButton
    {
	    /// <summary>The first button.</summary>
	    Button1 = 0,
	    /// <summary>
	    ///     The left mouse button. This corresponds to <see cref="F:OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Button1" />.
	    /// </summary>
	    Left = 0,
	    /// <summary>The second button.</summary>
	    Button2 = 1,
	    /// <summary>
	    ///     The right mouse button. This corresponds to <see cref="F:OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Button2" />.
	    /// </summary>
	    Right = 1,
	    /// <summary>The third button.</summary>
	    Button3 = 2,
	    /// <summary>
	    ///     The middle mouse button. This corresponds to <see cref="F:OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Button3" />.
	    /// </summary>
	    Middle = 2,
	    /// <summary>The fourth button.</summary>
	    Button4 = 3,
	    /// <summary>The fifth button.</summary>
	    Button5 = 4,
	    /// <summary>The sixth button.</summary>
	    Button6 = 5,
	    /// <summary>The seventh button.</summary>
	    Button7 = 6,
	    /// <summary>The eighth button.</summary>
	    Button8 = 7,
	    /// <summary>The highest mouse button available.</summary>
	    Last = 7
    }
}
