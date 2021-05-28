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

namespace SharpQuake.Framework.Factories.IO
{
    using Engine;
    using Framework.IO;
    using System.Collections.Generic;
    using System.Text;

    // Command execution takes a string, breaks it into tokens,
	// then searches for a command or variable that matches the first token.
	//
	// Commands can come from three sources, but the handler functions may choose
	// to dissallow the action or forward it to a remote server if the source is
	// not apropriate.
	public class CommandFactory : BaseFactory<string, CommandDelegate>
    {
        public const int MAX_ALIAS_NAME = 32;

        private Dictionary<string, string> Aliases
        {
            get;
            set;
        }

        private ClientVariableFactory Cvars
        {
            get;
            set;
        }

        public CommandBuffer Buffer
        {
            get;
            private set;
        }

        public CommandFactory( ) : base( )
        {
            this.Aliases = new( );
            this.Buffer = new( this );
        }

        public void Initialise( ClientVariableFactory cvars )
        {
            this.Cvars = cvars;

            this.Add( "stuffcmds", this.StuffCmds_f );
            this.Add( "exec", this.Exec_f );
            this.Add( "echo", this.Echo_f );
            this.Add( "alias", this.Alias_f );
            this.Add( "wait", this.Buffer.Wait_f ); // todo: move to Cbuf class?
        }

        public bool ContainsAlias( string name )
        {
            return this.Aliases.ContainsKey( name );
        }

        // Cmd_CompleteCommand()
        // attempts to match a partial command for automatic command line completion
        // returns NULL if nothing fits
        public string[] Complete( string partial )
        {
            if ( string.IsNullOrEmpty( partial ) )
                return null;

            var result = new List<string>( );
            foreach ( var cmd in this.DictionaryItems.Keys )
            {
                if ( cmd.StartsWith( partial ) )
                    result.Add( cmd );
            }
            return result.Count > 0 ? result.ToArray( ) : null;
        }

        // void	Cmd_ExecuteString (char *text, cmd_source_t src);
        // Parses a single line of text into arguments and tries to execute it.
        // The text can come from the command buffer, a remote client, or stdin.
        //
        // A complete command line has been parsed, so try to execute it
        // FIXME: lookupnoadd the token to speed search?
        public bool ExecuteString( string text, CommandSource source )
        {
            var handled = false;

            var msg = CommandMessage.FromString( text, source );

            // execute the command line
            if ( msg == null )
                return handled;

            if (this.Contains( msg.Name ) )
            {
                var handler = this.Get( msg.Name );
                handler?.Invoke( msg );
                handled = true;
            }
            else
            {
                if (this.ContainsAlias( msg.Name ) )
                    this.Buffer.Insert(this.Aliases[msg.Name] );
                else
                {
                    if ( !this.Cvars.HandleCommand( msg ) )
                        ConsoleWrapper.Print( $"Unknown command \"{msg.Name}\"\n" );
                }
            }

            return handled;
        }

        /// <summary>
        /// Cmd_StuffCmds_f
        /// Adds command line parameters as script statements
        /// Commands lead with a +, and continue until a - or another +
        /// quake +prog jctest.qp +cmd amlev1
        /// quake -nosound +cmd amlev1
        /// </summary>
        private void StuffCmds_f( CommandMessage msg )
        {
            if ( msg.Parameters?.Length != 0 )
            {
                ConsoleWrapper.Print( "stuffcmds : execute command line parameters\n" );
                return;
            }

            // build the combined string to parse from
            var sb = new StringBuilder( 1024 );
            sb.Append( msg.StringParameters );

            // pull out the commands
            var text = sb.ToString();
            sb.Length = 0;

            for ( var i = 0; i < text.Length; i++ )
            {
                if ( text[i] == '+' )
                {
                    i++;

                    var j = i;
                    while ( j < text.Length && text[j] != '+' && text[j] != '-' )
                        j++;

                    sb.Append( text.Substring( i, j - i + 1 ) );
                    sb.AppendLine( );
                    i = j - 1;
                }
            }

            if ( sb.Length > 0 )
                this.Buffer.Insert( sb.ToString( ) );
        }


        // Cmd_Exec_f
        private void Exec_f( CommandMessage msg )
        {
            if ( msg.Parameters == null || msg.Parameters.Length != 1 )
            {
                ConsoleWrapper.Print( "exec <filename> : execute a script file\n" );
                return;
            }

            var file = msg.Parameters[0];
            var bytes = FileSystem.LoadFile( file );

            if ( bytes == null )
            {
                ConsoleWrapper.Print( $"couldn't exec {file}\n" );
                return;
            }

            var script = Encoding.ASCII.GetString( bytes );
            ConsoleWrapper.Print( $"execing {file}\n" );
            this.Buffer.Insert( script );
        }

        // Cmd_Echo_f
        // Just prints the rest of the line to the console
        private void Echo_f( CommandMessage msg )
        {
            foreach ( var parameter in msg.Parameters )
                ConsoleWrapper.Print( $"{parameter} " );

            ConsoleWrapper.Print( "\n" );
        }

        // Cmd_Alias_f
        // Creates a new command that executes a command string (possibly ; seperated)
        private void Alias_f( CommandMessage msg )
        {
            if ( msg.Parameters == null || msg.Parameters?.Length == 0 )
            {
                ConsoleWrapper.Print( "Current alias commands:\n" );

                foreach ( var alias in this.Aliases )
                    ConsoleWrapper.Print( $"{alias.Key} : {alias.Value}\n" );

                return;
            }

            var name = msg.Parameters[0];

            if ( name.Length >= CommandFactory.MAX_ALIAS_NAME )
            {
                ConsoleWrapper.Print( "Alias name is too long\n" );
                return;
            }

            var args = string.Empty;

            // copy the rest of the command line
            if ( msg.Parameters.Length > 1 )
                args = msg.ParametersFrom( 1 );

            if (this.Aliases.ContainsKey( name ) )
                this.Aliases[name] = args;
            else
                this.Aliases.Add( name, args );
        }
    }
}
