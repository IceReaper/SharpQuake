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

namespace SharpQuake.Framework.IO
{
    using Engine;
    using System.Collections.Generic;
    using System.Linq;

    public class CommandMessage
    {
        public CommandSource Source
        {
            get;
            private set;
        }

        public string Name
        {
            get;
            private set;
        }

        public string[] Parameters
        {
            get;
            private set;
        }

        public string StringParameters => string.Join( " ", this.Parameters );

        public string FullCommand => $"{this.Name} {string.Join( " ", this.Parameters )}";

        public bool HasParameters => this.Parameters?.Length > 0;

        public CommandMessage( string name, CommandSource source, params string[] parameters )
        {
            this.Name = name;
            this.Parameters = parameters;
            this.Source = source;
        }

        public static CommandMessage FromString( string text, CommandSource source )
        {
            var argv = new List<string>( 80 );
            var argc = 0;
            var args = string.Empty;

            while ( !string.IsNullOrEmpty( text ) )
            {
                if ( argc == 1 )
                    args = text;

                text = Tokeniser.Parse( text );

                if ( string.IsNullOrEmpty( Tokeniser.Token ) )
                    break;

                if ( argc < 80 )
                {
                    argv.Add( Tokeniser.Token );
                    argc++;
                }
            }

            if ( argc <= 0 )
                return null;

            var vals = argc == 1 ? null : argv.GetRange( 1, argc - 1 ).ToArray( );
            return new( argv[0], source, vals );
        }

        public string ParametersFrom( int index )
        {
            if (this.Parameters.Length > index )
            {
                var extraParameters = this.Parameters.ToList( )
                    .GetRange( index, this.Parameters.Length - index )
                    .ToArray( );

                return $"{string.Join( " ", extraParameters )}";
            }

            return string.Empty;
        }
    }
}
