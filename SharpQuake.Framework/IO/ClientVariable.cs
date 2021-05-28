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
    using System;
    using System.Drawing;
    using System.Linq;
    using System.Numerics;

    public class ClientVariable
    {
        public string Name
        {
            get;
            private set;
        }

        public bool IsArchive => this.Flags.HasFlag( ClientVariableFlags.Archive );

        public bool IsServer => this.Flags.HasFlag( ClientVariableFlags.Server );

        private object Value
        {
            get;
            set;
        }

        private object DefaultValue
        {
            get;
            set;
        }

        public Type ValueType
        {
            get;
            private set;
        }

        public ClientVariableFlags Flags
        {
            get;
            private set;
        }

        public ClientVariable( string name, object defaultValue, Type valueType, ClientVariableFlags flags = ClientVariableFlags.None )
        {
            if ( string.IsNullOrEmpty( name ) )
                throw new ArgumentNullException( "name" );

            this.Name = name;
            this.DefaultValue = defaultValue;
            this.Value = this.DefaultValue;
            this.ValueType = valueType;
            this.Flags = flags;

            //var var = Find( name );
            //if ( var != null )
            //{
            //    throw new ArgumentException( String.Format( "Can't register variable {0}, already defined!\n", name ) );
            //    //Con_Printf("Can't register variable %s, allready defined\n", variable->name);
            //    //return;
            //}
            //if ( CommandInstance.Exists( name ) )
            //{
            //    throw new ArgumentException( String.Format( "Can't register variable: {0} is a command!\n", name ) );
            //}
        }
        
        public ClientVariable( string name, string defaultValue, ClientVariableFlags flags = ClientVariableFlags.None )
                   : this( name, defaultValue, typeof( string ), flags )
        {
        }

        public ClientVariable( string name, short defaultValue, ClientVariableFlags flags = ClientVariableFlags.None )
                   : this( name, defaultValue, typeof( short ), flags )
        {
        }

        public ClientVariable( string name, int defaultValue, ClientVariableFlags flags = ClientVariableFlags.None )
                   : this( name, defaultValue, typeof( int ), flags )
        {
        }

        public ClientVariable( string name, long defaultValue, ClientVariableFlags flags = ClientVariableFlags.None )
                   : this( name, defaultValue, typeof( long ), flags )
        {
        }

        public ClientVariable( string name, ushort defaultValue, ClientVariableFlags flags = ClientVariableFlags.None )
                   : this( name, defaultValue, typeof( ushort ), flags )
        {
        }

        public ClientVariable( string name, uint defaultValue, ClientVariableFlags flags = ClientVariableFlags.None )
                   : this( name, defaultValue, typeof( uint ), flags )
        {
        }

        public ClientVariable( string name, ulong defaultValue, ClientVariableFlags flags = ClientVariableFlags.None )
                   : this( name, defaultValue, typeof( ulong ), flags )
        {
        }

        public ClientVariable( string name, float defaultValue, ClientVariableFlags flags = ClientVariableFlags.None )
                   : this( name, defaultValue, typeof( float ), flags )
        {
        }

        public ClientVariable( string name, double defaultValue, ClientVariableFlags flags = ClientVariableFlags.None )
                   : this( name, defaultValue, typeof( double ), flags )
        {
        }

        public ClientVariable( string name, Vector2 defaultValue, ClientVariableFlags flags = ClientVariableFlags.None )
                   : this( name, defaultValue, typeof( Vector2 ), flags )
        {
        }

        public ClientVariable( string name, Vector3 defaultValue, ClientVariableFlags flags = ClientVariableFlags.None )
                   : this( name, defaultValue, typeof( Vector3 ), flags )
        {
        }

        public ClientVariable( string name, Vector4 defaultValue, ClientVariableFlags flags = ClientVariableFlags.None )
                   : this( name, defaultValue, typeof( Vector4 ), flags )
        {
        }

        public ClientVariable( string name, bool defaultValue, ClientVariableFlags flags = ClientVariableFlags.None )
                   : this( name, defaultValue, typeof( bool ), flags )
        {
        }

        public ClientVariable( string name, Rectangle defaultValue, ClientVariableFlags flags = ClientVariableFlags.None )
                   : this( name, defaultValue, typeof( Rectangle ), flags )
        {
        }

        public ClientVariable( string name, Point defaultValue, ClientVariableFlags flags = ClientVariableFlags.None )
                   : this( name, defaultValue, typeof( Point ), flags )
        {
        }

        public ClientVariable( string name, Size defaultValue, ClientVariableFlags flags = ClientVariableFlags.None )
                   : this( name, defaultValue, typeof( Size ), flags )
        {
        }

        public ClientVariable( string name, Color defaultValue, ClientVariableFlags flags = ClientVariableFlags.None )
                   : this( name, defaultValue, typeof( Color ), flags )
        {
        }

        public T Get<T>( )
        {
            var type = typeof( T );

            return ( T )this.Value;
        }

        public object Get( )
        {
            return this.Value;
        }

        public void Set<T>( T value )
        {
            var newValueType = typeof( T );

            if ( newValueType != this.ValueType )
            {
                if ( value is string stringValue )
                {
                    switch (this.ValueType.Name )
                    {
                        case "Single":
                            if ( float.TryParse( stringValue, out var singleResult ) )
                                this.Value = singleResult;
                            else
                                Utilities.Error( $"Failed to set value for {this.Name}, invalid format" );
                            break;

                        case "Double":
                            if ( double.TryParse( stringValue, out var doubleResult ) )
                                this.Value = doubleResult;
                            else
                                Utilities.Error( $"Failed to set value for {this.Name}, invalid format" );
                            break;

                        case "Int16":
                            if ( short.TryParse( stringValue, out var int16Result ) )
                                this.Value = int16Result;
                            else
                                Utilities.Error( $"Failed to set value for {this.Name}, invalid format" );
                            break;

                        case "Boolean":
                            if ( short.TryParse( stringValue, out var booleanResult ) )
                                this.Value = booleanResult == 1;
                            else
                                Utilities.Error( $"Failed to set value for {this.Name}, invalid format" );
                            break;

                        case "Int32":
                            if ( int.TryParse( stringValue, out var int32Result ) )
                                this.Value = int32Result;
                            else
                                Utilities.Error( $"Failed to set value for {this.Name}, invalid format" );
                            break;

                        case "Int64":
                            if ( long.TryParse( stringValue, out var int64Result ) )
                                this.Value = int64Result;
                            else
                                Utilities.Error( $"Failed to set value for {this.Name}, invalid format" );
                            break;

                        case "UInt16":
                            if ( ushort.TryParse( stringValue, out var uint16Result ) )
                                this.Value = uint16Result;
                            else
                                Utilities.Error( $"Failed to set value for {this.Name}, invalid format" );
                            break;

                        case "UInt32":
                            if ( uint.TryParse( stringValue, out var uint32Result ) )
                                this.Value = uint32Result;
                            else
                                Utilities.Error( $"Failed to set value for {this.Name}, invalid format" );
                            break;

                        case "UInt64":
                            if ( ulong.TryParse( stringValue, out var uint64Result ) )
                                this.Value = uint64Result;
                            else
                                Utilities.Error( $"Failed to set value for {this.Name}, invalid format" );
                            break;

                        case "Vector2":
                            var vector2Values = stringValue.Split( ' ' )?.Select( s => float.Parse( s ) ).ToArray();

                            if ( vector2Values == null || vector2Values.Length < 2 )
                                Utilities.Error( $"Failed to set value for {this.Name}, invalid format" );
                            else
                                this.Value = new Vector2( vector2Values[0], vector2Values[1] );
                            break;

                        case "Vector3":
                            var vector3Values = stringValue.Split( ' ' )?.Select( s => float.Parse( s ) ).ToArray( );

                            if ( vector3Values == null || vector3Values.Length < 3 )
                                Utilities.Error( $"Failed to set value for {this.Name}, invalid format" );
                            else
                                this.Value = new Vector3( vector3Values[0], vector3Values[1], vector3Values[2] );
                            break;

                        case "Vector4":
                            var vector4Values = stringValue.Split( ' ' )?.Select( s => float.Parse( s ) ).ToArray( );

                            if ( vector4Values == null || vector4Values.Length < 4 )
                                Utilities.Error( $"Failed to set value for {this.Name}, invalid format" );
                            else
                                this.Value = new Vector4( vector4Values[0], vector4Values[1], vector4Values[2], vector4Values[3] );
                            break;

                        case "Rectangle":
                            var rectValues = stringValue.Split( ' ' )?.Select( s => int.Parse( s ) ).ToArray( );

                            if ( rectValues == null || rectValues.Length < 4 )
                                Utilities.Error( $"Failed to set value for {this.Name}, invalid format" );
                            else
                                this.Value = new Rectangle( rectValues[0], rectValues[1], rectValues[2], rectValues[3] );
                            break;

                        case "Point":
                            var pointValues = stringValue.Split( ' ' )?.Select( s => int.Parse( s ) ).ToArray( );

                            if ( pointValues == null || pointValues.Length < 2 )
                                Utilities.Error( $"Failed to set value for {this.Name}, invalid format" );
                            else
                                this.Value = new Point( pointValues[0], pointValues[1] );
                            break;

                        case "Size":
                            var sizeValues = stringValue.Split( ' ' )?.Select( s => int.Parse( s ) ).ToArray( );

                            if ( sizeValues == null || sizeValues.Length < 2 )
                                Utilities.Error( $"Failed to set value for {this.Name}, invalid format" );
                            else
                                this.Value = new Size( sizeValues[0], sizeValues[1] );
                            break;

                        case "Color":
                            var colourValues = stringValue.Split( ' ' )?
                                .Select( s => ( int ) ( float.Parse( s ) * 255f ) )
                                .ToArray( );

                            if ( colourValues == null || colourValues.Length < 3 )
                                Utilities.Error( $"Failed to set value for {this.Name}, invalid format" );
                            else
                                this.Value = Color.FromArgb( colourValues[0], colourValues[1], colourValues[2], colourValues.Length < 4 ? 255 : colourValues[3] );
                            break;
                    }
                }
                else
                    Utilities.Error( $"Failed to set value for {this.Name}, expected {this.ValueType.Name} got {newValueType.Name}" );
            }
            else
                this.Value = value;
            //var changed = ( String.Compare( _String, value ) != 0 );
            //if ( !changed )
            //    return;

            //_String = value;
            //_Value = MathLib.atof( _String );

            //if ( IsServer && CommandInstance.Host.Server.sv.active )
            //{
            //    CommandInstance.Host.Server.BroadcastPrint( "\"{0}\" changed to \"{1}\"\n", _Name, _String );
            //}
        }
    }
}
