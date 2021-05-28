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



// keys.h
// keys.c

// key up events are sent even if in console mode

namespace SharpQuake.Desktop
{
    using Engine.Host;
    using Framework.Definitions;
    using Framework.Engine;
    using Framework.IO;
    using Framework.IO.Input;
    using Networking.Client;
    using System;
    using System.IO;
    using System.Text;

    /// <summary>
    /// Key_functions
    /// </summary>
    public class Keyboard
    {
        public KeyDestination Destination
        {
            get => this._KeyDest;
            set => this._KeyDest = value;
        }

        public bool TeamMessage
        {
            get => this._TeamMessage;
            set => this._TeamMessage = value;
        }

        public char[][] Lines => this._Lines;

        public int EditLine => this._EditLine;

        public string ChatBuffer => this._ChatBuffer.ToString( );

        public int LastPress => this._LastPress;

        public string[] Bindings => this._Bindings;

        // Instances
        public Host Host
        {
            get;
            private set;
        }

        public int LinePos;

        public int KeyCount;

        private char[][] _Lines = new char[32][];//, MAXCMDLINE]; // char	key_lines[32][MAXCMDLINE];

        // key_linepos
        private bool _ShiftDown; // = false;

        private int _LastPress; // key_lastpress

        private int _EditLine; // edit_line=0;
        private int _HistoryLine; // history_line=0;

        private KeyDestination _KeyDest; // key_dest

        // key_count			// incremented every key event

        private string[] _Bindings = new string[256]; // char	*keybindings[256];
        private bool[] _ConsoleKeys = new bool[256]; // consolekeys[256]	// if true, can't be rebound while in console
        private bool[] _MenuBound = new bool[256]; // menubound[256]	// if true, can't be rebound while in menu
        private int[] _KeyShift = new int[256]; // keyshift[256]		// key to map to if shift held down in console
        private int[] _Repeats = new int[256]; // key_repeats[256]	// if > 1, it is autorepeating
        private bool[] _KeyDown = new bool[256];

        private StringBuilder _ChatBuffer = new( 32 ); // chat_buffer
        private bool _TeamMessage; // qboolean team_message = false;

        public Keyboard( Host host )
        {
            this.Host = host;
        }

        // Key_Event (int key, qboolean down)
        //
        // Called by the system between frames for both key up and key down events
        // Should NOT be called during an interrupt!
        public void Event( int key, bool down )
        {
            this._KeyDown[key] = down;

            if ( !down )
                this._Repeats[key] = 0;

            this._LastPress = key;
            this.KeyCount++;
            if (this.KeyCount <= 0 )
                return;     // just catching keys for Con_NotifyBox

            // update auto-repeat status
            if ( down )
            {
                this._Repeats[key]++;
                if ( key != KeysDef.K_BACKSPACE && key != KeysDef.K_PAUSE && key != KeysDef.K_PGUP && key != KeysDef.K_PGDN && this._Repeats[key] > 1 )
                    return; // ignore most autorepeats

                if ( key >= 200 && string.IsNullOrEmpty(this._Bindings[key] ) )
                    this.Host.Console.Print( "{0} is unbound, hit F4 to set.\n", this.KeynumToString( key ) );
            }

            if ( key == KeysDef.K_SHIFT )
                this._ShiftDown = down;

            //
            // handle escape specialy, so the user can never unbind it
            //
            if ( key == KeysDef.K_ESCAPE )
            {
                if ( !down )
                    return;

                switch (this._KeyDest )
                {
                    case KeyDestination.key_message:
                        this.KeyMessage( key );
                        break;

                    case KeyDestination.key_menu:
                        this.Host.Menu.KeyDown( key );
                        break;

                    case KeyDestination.key_game:
                    case KeyDestination.key_console:
                        this.Host.Menu.ToggleMenu_f( null );
                        break;

                    default:
                        Utilities.Error( "Bad key_dest" );
                        break;
                }
                return;
            }

            //
            // key up events only generate commands if the game key binding is
            // a button command (leading + sign).  These will occur even in console mode,
            // to keep the character from continuing an action started before a console
            // switch.  Button commands include the keynum as a parameter, so multiple
            // downs can be matched with ups
            //
            if ( !down )
            {
                var kb = this._Bindings[key];

                if ( !string.IsNullOrEmpty( kb ) && kb.StartsWith( "+" ) )
                    this.Host.Commands.Buffer.Append( string.Format( "-{0} {1}\n", kb.Substring( 1 ), key ) );

                if (this._KeyShift[key] != key )
                {
                    kb = this._Bindings[this._KeyShift[key]];
                    if ( !string.IsNullOrEmpty( kb ) && kb.StartsWith( "+" ) )
                        this.Host.Commands.Buffer.Append( string.Format( "-{0} {1}\n", kb.Substring( 1 ), key ) );
                }
                return;
            }

            //
            // during demo playback, most keys bring up the main menu
            //
            if (this.Host.Client.cls.demoplayback && down && this._ConsoleKeys[key] && this._KeyDest == KeyDestination.key_game )
            {
                this.Host.Menu.ToggleMenu_f( null );
                return;
            }

            //
            // if not a consolekey, send to the interpreter no matter what mode is
            //
            if ( (this._KeyDest == KeyDestination.key_menu && this._MenuBound[key] ) ||
                (this._KeyDest == KeyDestination.key_console && !this._ConsoleKeys[key] ) ||
                (this._KeyDest == KeyDestination.key_game && ( !this.Host.Console.ForcedUp || !this._ConsoleKeys[key] ) ) )
            {
                var kb = this._Bindings[key];
                if ( !string.IsNullOrEmpty( kb ) )
                {
                    if ( kb.StartsWith( "+" ) )
                    {
                        // button commands add keynum as a parm
                        this.Host.Commands.Buffer.Append( string.Format( "{0} {1}\n", kb, key ) );
                    }
                    else
                    {
                        this.Host.Commands.Buffer.Append( kb );
                        this.Host.Commands.Buffer.Append( "\n" );
                    }
                }
                return;
            }

            if ( !down )
                return;     // other systems only care about key down events

            if (this._ShiftDown )
                key = this._KeyShift[key];

            switch (this._KeyDest )
            {
                case KeyDestination.key_message:
                    this.KeyMessage( key );
                    break;

                case KeyDestination.key_menu:
                    this.Host.Menu.KeyDown( key );
                    break;

                case KeyDestination.key_game:
                case KeyDestination.key_console:
                    this.KeyConsole( key );
                    break;

                default:
                    Utilities.Error( "Bad key_dest" );
                    break;
            }
        }

        // Key_Init (void);
        public void Initialise( )
        {
            for ( var i = 0; i < 32; i++ )
            {
                this._Lines[i] = new char[KeysDef.MAXCMDLINE];
                this._Lines[i][0] = ']'; // key_lines[i][0] = ']'; key_lines[i][1] = 0;
            }

            this.LinePos = 1;

            //
            // init ascii characters in console mode
            //
            for ( var i = 32; i < 128; i++ )
                this._ConsoleKeys[i] = true;

            this._ConsoleKeys[KeysDef.K_ENTER] = true;
            this._ConsoleKeys[KeysDef.K_TAB] = true;
            this._ConsoleKeys[KeysDef.K_LEFTARROW] = true;
            this._ConsoleKeys[KeysDef.K_RIGHTARROW] = true;
            this._ConsoleKeys[KeysDef.K_UPARROW] = true;
            this._ConsoleKeys[KeysDef.K_DOWNARROW] = true;
            this._ConsoleKeys[KeysDef.K_BACKSPACE] = true;
            this._ConsoleKeys[KeysDef.K_PGUP] = true;
            this._ConsoleKeys[KeysDef.K_PGDN] = true;
            this._ConsoleKeys[KeysDef.K_SHIFT] = true;
            this._ConsoleKeys[KeysDef.K_MWHEELUP] = true;
            this._ConsoleKeys[KeysDef.K_MWHEELDOWN] = true;
            this._ConsoleKeys['`'] = false;
            this._ConsoleKeys['~'] = false;

            for ( var i = 0; i < 256; i++ )
                this._KeyShift[i] = i;
            for ( int i = 'a'; i <= 'z'; i++ )
                this._KeyShift[i] = i - 'a' + 'A';

            this._KeyShift['1'] = '!';
            this._KeyShift['2'] = '@';
            this._KeyShift['3'] = '#';
            this._KeyShift['4'] = '$';
            this._KeyShift['5'] = '%';
            this._KeyShift['6'] = '^';
            this._KeyShift['7'] = '&';
            this._KeyShift['8'] = '*';
            this._KeyShift['9'] = '(';
            this._KeyShift['0'] = ')';
            this._KeyShift['-'] = '_';
            this._KeyShift['='] = '+';
            this._KeyShift[','] = '<';
            this._KeyShift['.'] = '>';
            this._KeyShift['/'] = '?';
            this._KeyShift[';'] = ':';
            this._KeyShift['\''] = '"';
            this._KeyShift['['] = '{';
            this._KeyShift[']'] = '}';
            this._KeyShift['`'] = '~';
            this._KeyShift['\\'] = '|';

            this._MenuBound[KeysDef.K_ESCAPE] = true;
            for ( var i = 0; i < 12; i++ )
                this._MenuBound[KeysDef.K_F1 + i] = true;

            //
            // register our functions
            //
            this.Host.Commands.Add( "bind", this.Bind_f );
            this.Host.Commands.Add( "unbind", this.Unbind_f );
            this.Host.Commands.Add( "unbindall", this.UnbindAll_f );
        }

        /// <summary>
        /// Key_WriteBindings
        /// </summary>
        public void WriteBindings( Stream dest )
        {
            var sb = new StringBuilder( 4096 );
            for ( var i = 0; i < 256; i++ )
            {
                if ( !string.IsNullOrEmpty(this._Bindings[i] ) )
                {
                    sb.Append( "bind \"" );
                    sb.Append(this.KeynumToString( i ) );
                    sb.Append( "\" \"" );
                    sb.Append(this._Bindings[i] );
                    sb.AppendLine( "\"" );
                }
            }
            var buf = Encoding.ASCII.GetBytes( sb.ToString( ) );
            dest.Write( buf, 0, buf.Length );
        }

        /// <summary>
        /// Key_SetBinding
        /// </summary>
        public void SetBinding( int keynum, string binding )
        {
            if ( keynum != -1 )
                this._Bindings[keynum] = binding;
        }

        // Key_ClearStates (void)
        public void ClearStates( )
        {
            for ( var i = 0; i < 256; i++ )
            {
                this._KeyDown[i] = false;
                this._Repeats[i] = 0;
            }
        }

        // Key_KeynumToString
        //
        // Returns a string (either a single ascii char, or a K_* name) for the
        // given keynum.
        // FIXME: handle quote special (general escape sequence?)
        public string KeynumToString( int keynum )
        {
            if ( keynum == -1 )
                return "<KEY NOT FOUND>";

            if ( keynum > 32 && keynum < 127 )
            {
                // printable ascii
                return ( ( char ) keynum ).ToString( );
            }

            foreach ( var kn in KeysDef.KeyNames )
            {
                if ( kn.keynum == keynum )
                    return kn.name;
            }
            return "<UNKNOWN KEYNUM>";
        }

        // Key_StringToKeynum
        //
        // Returns a key number to be used to index keybindings[] by looking at
        // the given string.  Single ascii characters return themselves, while
        // the K_* names are matched up.
        private int StringToKeynum( string str )
        {
            if ( string.IsNullOrEmpty( str ) )
                return -1;
            if ( str.Length == 1 )
                return str[0];

            foreach ( var keyname in KeysDef.KeyNames )
            {
                if ( Utilities.SameText( keyname.name, str ) )
                    return keyname.keynum;
            }
            return -1;
        }

        //Key_Unbind_f
        private void Unbind_f( CommandMessage msg )
        {
            var c = msg.Parameters != null ? msg.Parameters.Length : 0;

            if ( c != 1 )
            {
                this.Host.Console.Print( "unbind <key> : remove commands from a key\n" );
                return;
            }

            var b = this.StringToKeynum( msg.Parameters[0] );
            if ( b == -1 )
            {
                this.Host.Console.Print( $"\"{msg.Parameters[0]}\" isn't a valid key\n" );
                return;
            }

            this.SetBinding( b, null );
        }

        // Key_Unbindall_f
        private void UnbindAll_f( CommandMessage msg )
        {
            for ( var i = 0; i < 256; i++ )
            {
                if ( !string.IsNullOrEmpty(this._Bindings[i] ) )
                    this.SetBinding( i, null );
            }
        }

        //Key_Bind_f
        private void Bind_f( CommandMessage msg )
        {
            var c = msg.Parameters != null ? msg.Parameters.Length : 0;
            if ( c != 1 && c != 2 )
            {
                this.Host.Console.Print( "bind <key> [command] : attach a command to a key\n" );
                return;
            }

            var b = this.StringToKeynum( msg.Parameters[0] );
            if ( b == -1 )
            {
                this.Host.Console.Print( $"\"{msg.Parameters[0]}\" isn't a valid key\n" );
                return;
            }

            if ( c == 1 )
            {
                if ( !string.IsNullOrEmpty(this._Bindings[b] ) )// keybindings[b])
                    this.Host.Console.Print( $"\"{msg.Parameters[0]}\" = \"{this._Bindings[b]}\"\n" );
                else
                    this.Host.Console.Print( $"\"{msg.Parameters[0]}\" is not bound\n" );
                return;
            }

            // copy the rest of the command line
            // start out with a null string

            var args = string.Empty;

            if ( msg.Parameters.Length > 1 )
                args = msg.ParametersFrom( 1 );

            this.SetBinding( b, args );
        }

        // Key_Message (int key)
        private void KeyMessage( int key )
        {
            if ( key == KeysDef.K_ENTER )
            {
                if (this._TeamMessage )
                    this.Host.Commands.Buffer.Append( "say_team \"" );
                else
                    this.Host.Commands.Buffer.Append( "say \"" );

                this.Host.Commands.Buffer.Append(this._ChatBuffer.ToString( ) );
                this.Host.Commands.Buffer.Append( "\"\n" );

                this.Destination = KeyDestination.key_game;
                this._ChatBuffer.Length = 0;
                return;
            }

            if ( key == KeysDef.K_ESCAPE )
            {
                this.Destination = KeyDestination.key_game;
                this._ChatBuffer.Length = 0;
                return;
            }

            if ( key < 32 || key > 127 )
                return;	// non printable

            if ( key == KeysDef.K_BACKSPACE )
            {
                if (this._ChatBuffer.Length > 0 )
                    this._ChatBuffer.Length--;

                return;
            }

            if (this._ChatBuffer.Length == 31 )
                return; // all full

            this._ChatBuffer.Append( ( char ) key );
        }

        /// <summary>
        /// Key_Console
        /// Interactive line editing and console scrollback
        /// </summary>
        private void KeyConsole( int key )
        {
            if ( key == KeysDef.K_ENTER )
            {
                var line = new string(this._Lines[this._EditLine] ).TrimEnd( '\0', ' ' );
                var cmd = line.Substring( 1 );
                this.Host.Commands.Buffer.Append( cmd );	// skip the >
                this.Host.Commands.Buffer.Append( "\n" );
                this.Host.Console.Print( "{0}\n", line );
                this._EditLine = (this._EditLine + 1 ) & 31;
                this._HistoryLine = this._EditLine;
                this._Lines[this._EditLine][0] = ']';
                this.LinePos = 1;
                if (this.Host.Client.cls.state == cactive_t.ca_disconnected )
                    this.Host.Screen.UpdateScreen( );	// force an update, because the command
                // may take some time
                return;
            }

            if ( key == KeysDef.K_TAB )
            {
                // command completion
                var txt = new string(this._Lines[this._EditLine], 1, KeysDef.MAXCMDLINE - 1 ).TrimEnd( '\0', ' ' );
                var cmds = this.Host.Commands.Complete( txt );
                var vars = this.Host.CVars.CompleteName( txt );
                string match = null;
                if ( cmds != null )
                {
                    if ( cmds.Length > 1 || vars != null )
                    {
                        this.Host.Console.Print( "\nCommands:\n" );
                        foreach ( var s in cmds )
                            this.Host.Console.Print( "  {0}\n", s );
                    }
                    else
                        match = cmds[0];
                }
                if ( vars != null )
                {
                    if ( vars.Length > 1 || cmds != null )
                    {
                        this.Host.Console.Print( "\nVariables:\n" );
                        foreach ( var s in vars )
                            this.Host.Console.Print( "  {0}\n", s );
                    }
                    else if ( match == null )
                        match = vars[0];
                }
                if ( !string.IsNullOrEmpty( match ) )
                {
                    var len = Math.Min( match.Length, KeysDef.MAXCMDLINE - 3 );
                    for ( var i = 0; i < len; i++ )
                        this._Lines[this._EditLine][i + 1] = match[i];

                    this.LinePos = len + 1;
                    this._Lines[this._EditLine][this.LinePos] = ' ';
                    this.LinePos++;
                    this._Lines[this._EditLine][this.LinePos] = '\0';
                    return;
                }
            }

            if ( key == KeysDef.K_BACKSPACE || key == KeysDef.K_LEFTARROW )
            {
                if (this.LinePos > 1 )
                    this.LinePos--;
                return;
            }

            if ( key == KeysDef.K_UPARROW )
            {
                do
                    this._HistoryLine = (this._HistoryLine - 1 ) & 31;
                while (this._HistoryLine != this._EditLine && this._Lines[this._HistoryLine][1] == 0 );
                if (this._HistoryLine == this._EditLine )
                    this._HistoryLine = (this._EditLine + 1 ) & 31;
                Array.Copy(this._Lines[this._HistoryLine], this._Lines[this._EditLine], KeysDef.MAXCMDLINE );
                this.LinePos = 0;
                while (this._Lines[this._EditLine][this.LinePos] != '\0' && this.LinePos < KeysDef.MAXCMDLINE )
                    this.LinePos++;
                return;
            }

            if ( key == KeysDef.K_DOWNARROW )
            {
                if (this._HistoryLine == this._EditLine )
                    return;
                do
                    this._HistoryLine = (this._HistoryLine + 1 ) & 31;
                while (this._HistoryLine != this._EditLine && this._Lines[this._HistoryLine][1] == '\0' );
                if (this._HistoryLine == this._EditLine )
                {
                    this._Lines[this._EditLine][0] = ']';
                    this.LinePos = 1;
                }
                else
                {
                    Array.Copy(this._Lines[this._HistoryLine], this._Lines[this._EditLine], KeysDef.MAXCMDLINE );
                    this.LinePos = 0;
                    while (this._Lines[this._EditLine][this.LinePos] != '\0' && this.LinePos < KeysDef.MAXCMDLINE )
                        this.LinePos++;
                }
                return;
            }

            if ( key == KeysDef.K_PGUP || key == KeysDef.K_MWHEELUP )
            {
                this.Host.Console.BackScroll += 2;
                if (this.Host.Console.BackScroll > this.Host.Console.TotalLines - (this.Host.Screen.vid.height >> 3 ) - 1 )
                    this.Host.Console.BackScroll = this.Host.Console.TotalLines - (this.Host.Screen.vid.height >> 3 ) - 1;
                return;
            }

            if ( key == KeysDef.K_PGDN || key == KeysDef.K_MWHEELDOWN )
            {
                this.Host.Console.BackScroll -= 2;
                if (this.Host.Console.BackScroll < 0 )
                    this.Host.Console.BackScroll = 0;
                return;
            }

            if ( key == KeysDef.K_HOME )
            {
                this.Host.Console.BackScroll = this.Host.Console.TotalLines - (this.Host.Screen.vid.height >> 3 ) - 1;
                return;
            }

            if ( key == KeysDef.K_END )
            {
                this.Host.Console.BackScroll = 0;
                return;
            }

            if ( key < 32 || key > 127 )
                return;	// non printable

            if (this.LinePos < KeysDef.MAXCMDLINE - 1 )
            {
                this._Lines[this._EditLine][this.LinePos] = ( char ) key;
                this.LinePos++;
                this._Lines[this._EditLine][this.LinePos] = '\0';
            }
        }
    }

    // keydest_t;
}
