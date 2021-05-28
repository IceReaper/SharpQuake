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
    using System.IO;
    using System.Linq;
    using System.Text;

    public class ClientVariableFactory : BaseFactory<string, ClientVariable>
    {
        public ClientVariableFactory() : base( )
        {
        }

        public ClientVariable Add<T>( string name, T defaultValue, ClientVariableFlags flags = ClientVariableFlags.None )
        {
            if (this.Contains( name ) )
                return null;

            var result = new ClientVariable( name, defaultValue, typeof( T ), flags );

            base.Add( name, result );

            return result;
        }

        public void Set<T>( string name, T value )
        {
            if ( !this.Contains( name ) )
                return;

            this.Get( name ).Set( value );
        }

        public string[] CompleteName( string partial )
        {
            if ( string.IsNullOrEmpty( partial ) )
                return null;

            var results = new List<string>( );

            var keysList = this.UniqueKeys ? this.DictionaryItems.Select( i => i.Key ) : this.ListItems.Select( i => i.Key );

            foreach ( var key in keysList )
            {
                if ( key.StartsWith( partial ) )
                    results.Add( key );
            }
            
            return results.Count > 0 ? results.ToArray( ) : null;
        }

        /// <summary>
        /// Cvar_WriteVariables
        /// Writes lines containing "set variable value" for all variables
        /// with the archive flag set to true.
        /// </summary>
        public void WriteVariables( Stream stream )
        {
            var sb = new StringBuilder( 4096 );

            var list = this.UniqueKeys ? this.DictionaryItems.Select( i => i.Value ) : this.ListItems.Select( i => i.Value );

            foreach ( var cvar in list )
            {
                if ( cvar.IsArchive )
                {
                    sb.Append( cvar.Name );
                    sb.Append( " \"" );
                    sb.Append( cvar.ValueType == typeof( bool ) ? cvar.Get<bool>() ? "1" : "0" : cvar.Get().ToString() );
                    sb.AppendLine( "\"" );
                }
            }
           
            var buf = Encoding.ASCII.GetBytes( sb.ToString( ) );
            stream.Write( buf, 0, buf.Length );
        }

        // Cvar_Command()
        // Handles variable inspection and changing from the console
        public bool HandleCommand( CommandMessage msg )
        {
            if ( !this.Contains( msg.Name ) )
                return false;

            var cvar = this.Get( msg.Name );

            if ( msg.Parameters == null || msg.Parameters.Length <= 0 )
                ConsoleWrapper.Print( $"\"{cvar.Name}\" is \"{cvar.Get()}\"\n" );
            else
                cvar.Set( msg.Parameters[0] );

            return true;
        }
    }
}
