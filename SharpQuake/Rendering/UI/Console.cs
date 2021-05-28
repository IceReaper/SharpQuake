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

namespace SharpQuake.Rendering.UI
{
	using Engine.Host;
	using Framework;
	using Framework.Definitions;
	using Framework.Engine;
	using Framework.IO;
	using Framework.IO.Input;
	using Menus;
	using Networking.Client;
	using System;
	using System.IO;
	using System.Text;

	/// <summary>
	/// Con_functions
	/// </summary>
	public class Con
	{
		public bool IsInitialized => this._IsInitialized;

		public bool ForcedUp
		{
			get => this._ForcedUp;
			set => this._ForcedUp = value;
		}

		public int NotifyLines
		{
			get => this._NotifyLines;
			set => this._NotifyLines = value;
		}

		public int TotalLines => this._TotalLines;

		public int BackScroll;
		private const string LOG_FILE_NAME = "qconsole.log";

		private const int CON_TEXTSIZE = 16384;
		private const int NUM_CON_TIMES = 4;

		private char[] _Text = new char[Con.CON_TEXTSIZE]; // char		*con_text=0;
		private int _VisLines; // con_vislines
		private int _TotalLines; // con_totallines   // total lines in console scrollback

		// con_backscroll		// lines up from bottom to display
		private int _Current; // con_current		// where next message will be printed

		private int _X; // con_x		// offset in current line for next print
		private int _CR; // from Print()
		private double[] _Times = new double[Con.NUM_CON_TIMES]; // con_times	// realtime time the line was generated

		// for transparent notify lines
		private int _LineWidth; // con_linewidth

		private bool _DebugLog; // qboolean	con_debuglog;
		private bool _IsInitialized; // qboolean con_initialized;
		private bool _ForcedUp; // qboolean con_forcedup		// because no entities to refresh
		private int _NotifyLines; // con_notifylines	// scan lines to clear for notify lines
		private float _CursorSpeed = 4; // con_cursorspeed
		private FileStream _Log;

		public Con( Host host )
		{
			this.Host = host;
		}

		// Con_CheckResize (void)
		public void CheckResize( )
		{
			var width = (this.Host.Screen.vid.width >> 3 ) - 2;
			if ( width == this._LineWidth )
				return;

			if ( width < 1 ) // video hasn't been initialized yet
			{
				width = 38;
				this._LineWidth = width; // con_linewidth = width;
				this._TotalLines = Con.CON_TEXTSIZE / this._LineWidth;
				Utilities.FillArray(this._Text, ' ' ); // Q_memset (con_text, ' ', CON_TEXTSIZE);
			}
			else
			{
				var oldwidth = this._LineWidth;
				this._LineWidth = width;
				var oldtotallines = this._TotalLines;
				this._TotalLines = Con.CON_TEXTSIZE / this._LineWidth;
				var numlines = oldtotallines;

				if (this._TotalLines < numlines )
					numlines = this._TotalLines;

				var numchars = oldwidth;

				if (this._LineWidth < numchars )
					numchars = this._LineWidth;

				var tmp = this._Text;
				this._Text = new char[Con.CON_TEXTSIZE];
				Utilities.FillArray(this._Text, ' ' );

				for ( var i = 0; i < numlines; i++ )
				{
					for ( var j = 0; j < numchars; j++ )
					{
						this._Text[(this._TotalLines - 1 - i ) * this._LineWidth + j] = tmp[(this._Current - i + oldtotallines ) %
							oldtotallines * oldwidth + j];
					}
				}

				this.ClearNotify();
			}

			this.BackScroll = 0;
			this._Current = this._TotalLines - 1;
		}

		// Instances
		private Host Host
		{
			get;
			set;
		}

		// Con_Init (void)
		public void Initialise( )
		{
			this._DebugLog = CommandLine.CheckParm( "-condebug" ) > 0;

			if (this._DebugLog )
			{
				var path = Path.Combine( FileSystem.GameDir, Con.LOG_FILE_NAME );
				if ( File.Exists( path ) )
					File.Delete( path );

				this._Log = new( path, FileMode.Create, FileAccess.Write, FileShare.Read );
			}

			this._LineWidth = -1;
			this.CheckResize();

			this.Print( "Console initialized.\n" );

			//
			// register our commands
			//
			if (this.Host.Cvars.NotifyTime == null )
				this.Host.Cvars.NotifyTime = this.Host.CVars.Add( "con_notifytime", 3 );

			this.Host.Commands.Add( "toggleconsole", this.ToggleConsole_f );
			this.Host.Commands.Add( "messagemode", this.MessageMode_f );
			this.Host.Commands.Add( "messagemode2", this.MessageMode2_f );
			this.Host.Commands.Add( "clear", this.Clear_f );

			ConsoleWrapper.OnPrint += ( txt ) =>
			{
				this.Print( txt );
			};

			ConsoleWrapper.OnPrint2 += ( fmt, args ) =>
			{
				this.Print( fmt, args );
			};

			ConsoleWrapper.OnDPrint += ( fmt, args ) =>
			{
				this.DPrint( fmt, args );
			};

			this._IsInitialized = true;
		}

		// Con_DrawConsole
		//
		// Draws the console with the solid background
		// The typing input line at the bottom should only be drawn if typing is allowed
		public void Draw( int lines, bool drawinput )
		{
			if ( lines <= 0 )
				return;

			// draw the background
			this.Host.DrawingContext.DrawConsoleBackground( lines );

			// draw the text
			this._VisLines = lines;

			var rows = ( lines - 16 ) >> 3;     // rows of text to draw
			var y = lines - 16 - ( rows << 3 ); // may start slightly negative

			for ( var i = this._Current - rows + 1; i <= this._Current; i++, y += 8 )
			{
				var j = i - this.BackScroll;
				if ( j < 0 )
					j = 0;

				var offset = j % this._TotalLines * this._LineWidth;

				for ( var x = 0; x < this._LineWidth; x++ )
					this.Host.DrawingContext.DrawCharacter( ( x + 1 ) << 3, y, this._Text[offset + x] );
			}

			// draw the input prompt, user text, and cursor if desired
			if ( drawinput )
				this.DrawInput();
		}

		/// <summary>
		/// Con_Printf
		/// </summary>
		public void Print( string fmt, params object[] args )
		{
			var msg = args.Length > 0 ? string.Format( fmt, args ) : fmt;

			Console.WriteLine( msg ); // Debug stuff

			// log all messages to file
			if (this._DebugLog )
				this.DebugLog( msg );

			if ( !this._IsInitialized )
				return;

			if (this.Host.Client.cls.state == cactive_t.ca_dedicated )
				return;     // no graphics mode

			// write it to the scrollable buffer
			this.Print( msg );

			// update the screen if the console is displayed
			if (this.Host.Client.cls.signon != ClientDef.SIGNONS && !this.Host.Screen.IsDisabledForLoading )
				this.Host.Screen.UpdateScreen();
		}

		public void Shutdown( )
		{
			if (this._Log != null )
			{
				this._Log.Flush();
				this._Log.Dispose();
				this._Log = null;
			}
		}

		//
		// Con_DPrintf
		//
		// A Con_Printf that only shows up if the "developer" cvar is set
		public void DPrint( string fmt, params object[] args )
		{
			// don't confuse non-developers with techie stuff...
			if (this.Host != null && this.Host.IsDeveloper )
				this.Print( fmt, args );
		}

		// Con_SafePrintf (char *fmt, ...)
		//
		// Okay to call even when the screen can't be updated
		public void SafePrint( string fmt, params object[] args )
		{
			var temp = this.Host.Screen.IsDisabledForLoading;
			this.Host.Screen.IsDisabledForLoading = true;
			this.Print( fmt, args );
			this.Host.Screen.IsDisabledForLoading = temp;
		}

		/// <summary>
		/// Con_DrawNotify
		/// </summary>
		public void DrawNotify( )
		{
			var v = 0;
			for ( var i = this._Current - Con.NUM_CON_TIMES + 1; i <= this._Current; i++ )
			{
				if ( i < 0 )
					continue;
				var time = this._Times[i % Con.NUM_CON_TIMES];
				if ( time == 0 )
					continue;
				time = this.Host.RealTime - time;
				if ( time > this.Host.Cvars.NotifyTime.Get<int>() )
					continue;

				var textOffset = i % this._TotalLines * this._LineWidth;

				this.Host.Screen.ClearNotify = 0;
				this.Host.Screen.CopyTop = true;

				for ( var x = 0; x < this._LineWidth; x++ )
					this.Host.DrawingContext.DrawCharacter( ( x + 1 ) << 3, v, this._Text[textOffset + x] );

				v += 8;
			}

			if (this.Host.Keyboard.Destination == KeyDestination.key_message )
			{
				this.Host.Screen.ClearNotify = 0;
				this.Host.Screen.CopyTop = true;

				var x = 0;

				this.Host.DrawingContext.DrawString( 8, v, "say:" );
				var chat = this.Host.Keyboard.ChatBuffer;
				for ( ; x < chat.Length; x++ )
					this.Host.DrawingContext.DrawCharacter( ( x + 5 ) << 3, v, chat[x] );

				this.Host.DrawingContext.DrawCharacter( ( x + 5 ) << 3, v, 10 + ( ( int ) (this.Host.RealTime * this._CursorSpeed ) & 1 ) );
				v += 8;
			}

			if ( v > this._NotifyLines )
				this._NotifyLines = v;
		}

		// Con_ClearNotify (void)
		public void ClearNotify( )
		{
			for ( var i = 0; i < Con.NUM_CON_TIMES; i++ )
				this._Times[i] = 0;
		}

		/// <summary>
		/// Con_ToggleConsole_f
		/// </summary>
		public void ToggleConsole_f( CommandMessage msg )
		{
			if (this.Host.Keyboard.Destination == KeyDestination.key_console )
			{
				if (this.Host.Client.cls.state == cactive_t.ca_connected )
				{
					this.Host.Keyboard.Destination = KeyDestination.key_game;
					this.Host.Keyboard.Lines[this.Host.Keyboard.EditLine][1] = '\0';  // clear any typing
					this.Host.Keyboard.LinePos = 1;
				}
				else
					MenuBase.MainMenuInstance.Show(this.Host );
			}
			else
				this.Host.Keyboard.Destination = KeyDestination.key_console;

			this.Host.Screen.EndLoadingPlaque();
			Array.Clear(this._Times, 0, this._Times.Length );
		}

		/// <summary>
		/// Con_DebugLog
		/// </summary>
		private void DebugLog( string msg )
		{
			if (this._Log != null )
			{
				var tmp = Encoding.UTF8.GetBytes( msg );
				this._Log.Write( tmp, 0, tmp.Length );
			}
		}

		// Con_Print (char *txt)
		//
		// Handles cursor positioning, line wrapping, etc
		// All console printing must go through this in order to be logged to disk
		// If no console is visible, the notify window will pop up.
		private void Print( string txt )
		{
			if ( string.IsNullOrEmpty( txt ) )
				return;

			int mask, offset = 0;

			this.BackScroll = 0;

			if ( txt.StartsWith( ( char ) 1 ) )// [0] == 1)
			{
				mask = 128; // go to colored text
				this.Host.Sound.LocalSound( "misc/talk.wav" ); // play talk wav
				offset++;
			}
			else if ( txt.StartsWith( ( ( char ) 2 ).ToString() ) ) //txt[0] == 2)
			{
				mask = 128; // go to colored text
				offset++;
			}
			else
				mask = 0;

			while ( offset < txt.Length )
			{
				var c = txt[offset];

				int l;
				// count word length
				for ( l = 0; l < this._LineWidth && offset + l < txt.Length; l++ )
				{
					if ( txt[offset + l] <= ' ' )
						break;
				}

				// word wrap
				if ( l != this._LineWidth && this._X + l > this._LineWidth )
					this._X = 0;

				offset++;

				if (this._CR != 0 )
				{
					this._Current--;
					this._CR = 0;
				}

				if (this._X == 0 )
				{
					this.LineFeed();
					// mark time for transparent overlay
					if (this._Current >= 0 )
						this._Times[this._Current % Con.NUM_CON_TIMES] = this.Host.RealTime; // realtime
				}

				switch ( c )
				{
					case '\n':
						this._X = 0;
						break;

					case '\r':
						this._X = 0;
						this._CR = 1;
						break;

					default:    // display character and advance
						var y = this._Current % this._TotalLines;
						this._Text[y * this._LineWidth + this._X] = ( char ) ( c | mask );
						this._X++;
						if (this._X >= this._LineWidth )
							this._X = 0;
						break;
				}
			}
		}

		/// <summary>
		/// Con_Clear_f
		/// </summary>
		private void Clear_f( CommandMessage msg )
		{
			Utilities.FillArray(this._Text, ' ' );
		}

		// Con_MessageMode_f
		private void MessageMode_f( CommandMessage msg )
		{
			this.Host.Keyboard.Destination = KeyDestination.key_message;
			this.Host.Keyboard.TeamMessage = false;
		}

		//Con_MessageMode2_f
		private void MessageMode2_f( CommandMessage msg )
		{
			this.Host.Keyboard.Destination = KeyDestination.key_message;
			this.Host.Keyboard.TeamMessage = true;
		}

		// Con_Linefeed
		private void LineFeed( )
		{
			this._X = 0;
			this._Current++;

			for ( var i = 0; i < this._LineWidth; i++ )
				this._Text[this._Current % this._TotalLines * this._LineWidth + i] = ' ';
		}

		// Con_DrawInput
		//
		// The input line scrolls horizontally if typing goes beyond the right edge
		private void DrawInput( )
		{
			if (this.Host.Keyboard.Destination != KeyDestination.key_console && !this._ForcedUp )
				return;     // don't draw anything

			// add the cursor frame
			this.Host.Keyboard.Lines[this.Host.Keyboard.EditLine][this.Host.Keyboard.LinePos] = ( char ) ( 10 + ( ( int ) (this.Host.RealTime * this._CursorSpeed ) & 1 ) );

			// fill out remainder with spaces
			for ( var i = this.Host.Keyboard.LinePos + 1; i < this._LineWidth; i++ )
				this.Host.Keyboard.Lines[this.Host.Keyboard.EditLine][i] = ' ';

			//	prestep if horizontally scrolling
			var offset = 0;
			if (this.Host.Keyboard.LinePos >= this._LineWidth )
				offset = 1 + this.Host.Keyboard.LinePos - this._LineWidth;
			//text += 1 + key_linepos - con_linewidth;

			// draw it
			var y = this._VisLines - 16;

			for ( var i = 0; i < this._LineWidth; i++ )
				this.Host.DrawingContext.DrawCharacter( ( i + 1 ) << 3, this._VisLines - 16, this.Host.Keyboard.Lines[this.Host.Keyboard.EditLine][offset + i] );

			// remove cursor
			this.Host.Keyboard.Lines[this.Host.Keyboard.EditLine][this.Host.Keyboard.LinePos] = '\0';
		}
	}
}
