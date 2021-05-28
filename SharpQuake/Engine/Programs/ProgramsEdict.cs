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

namespace SharpQuake.Engine.Programs
{
    using Framework.Data;
    using Framework.Definitions;
    using Framework.Engine;
    using Framework.IO;
    using Framework.IO.Programs;
    using Framework.Mathematics;
    using Host;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Numerics;
    using System.Runtime.InteropServices;
    using System.Text;

    public partial class Programs
    {
        private struct gefv_cache
        {
            public ProgramDefinition pcache;
            public string field;// char	field[MAX_FIELD_LEN];
        }

        public int EdictSize => this._EdictSize;

        //static StringBuilder _AddedStrings = new StringBuilder(4096);
        public long GlobalStructAddr => this._GlobalStructAddr;

        public int Crc => this._Crc;

        public GlobalVariables GlobalStruct;
        private const int GEFV_CACHESIZE = 2;

        //gefv_cache;

        private gefv_cache[] _gefvCache = new gefv_cache[Programs.GEFV_CACHESIZE]; // gefvCache
        private int _gefvPos;

        private int[] _TypeSize = new int[8] // type_size
        {
            1, sizeof(int)/4, 1, 3, 1, 1, sizeof(int)/4, IntPtr.Size/4
        };       

        private Program _Progs; // progs
        private ProgramFunction[] _Functions; // pr_functions
        private string _Strings; // pr_strings
        private ProgramDefinition[] _FieldDefs; // pr_fielddefs
        private ProgramDefinition[] _GlobalDefs; // pr_globaldefs
        private Statement[] _Statements; // pr_statements

        // pr_global_struct
        private float[] _Globals; // Added by Uze: all data after globalvars_t (numglobals * 4 - globalvars_t.SizeInBytes)

        private int _EdictSize; // pr_edict_size	// in bytes
        private ushort _Crc; // pr_crc
        private GCHandle _HGlobalStruct;
        private GCHandle _HGlobals;
        private long _GlobalStructAddr;
        private long _GlobalsAddr;
        private List<string> _DynamicStrings = new( 512 );

        // Instances
        public Host Host
        {
            get;
            private set;
        }

        public Programs( Host host )
        {
            this.Host = host;

            // Temporary workaround - will fix later
            ProgramsWrapper.OnGetString += ( strId ) =>
            {
                return this.GetString( strId );
            };
        }

        // PR_Init
        public void Initialise( )
        {
            this.Host.Commands.Add( "edict", this.PrintEdict_f );
            this.Host.Commands.Add( "edicts", this.PrintEdicts );
            this.Host.Commands.Add( "edictcount", this.EdictCount );
            this.Host.Commands.Add( "profile", this.Profile_f );
            this.Host.Commands.Add( "test5", this.Test5_f );

            if (this.Host.Cvars.NoMonsters == null )
            {
                this.Host.Cvars.NoMonsters = this.Host.CVars.Add( "nomonsters", false );
                this.Host.Cvars.GameCfg = this.Host.CVars.Add( "gamecfg", false );
                this.Host.Cvars.Scratch1 = this.Host.CVars.Add( "scratch1", false );
                this.Host.Cvars.Scratch2 = this.Host.CVars.Add( "scratch2", false );
                this.Host.Cvars.Scratch3 = this.Host.CVars.Add( "scratch3", false );
                this.Host.Cvars.Scratch4 = this.Host.CVars.Add( "scratch4", false );
                this.Host.Cvars.SavedGameCfg = this.Host.CVars.Add( "savedgamecfg", false, ClientVariableFlags.Archive );
                this.Host.Cvars.Saved1 = this.Host.CVars.Add( "saved1", false, ClientVariableFlags.Archive );
                this.Host.Cvars.Saved2 = this.Host.CVars.Add( "saved2", false, ClientVariableFlags.Archive );
                this.Host.Cvars.Saved3 = this.Host.CVars.Add( "saved3", false, ClientVariableFlags.Archive );
                this.Host.Cvars.Saved4 = this.Host.CVars.Add( "saved4", false, ClientVariableFlags.Archive );
            }
        }

        /// <summary>
        /// PR_LoadProgs
        /// </summary>
        public void LoadProgs( )
        {
            this.FreeHandles( );

            this.Host.ProgramsBuiltIn.ClearState( );
            this._DynamicStrings.Clear( );

            // flush the non-C variable lookup cache
            for ( var i = 0; i < Programs.GEFV_CACHESIZE; i++ )
                this._gefvCache[i].field = null;

            Framework.Engine.Crc.Init( out this._Crc );

            var buf = FileSystem.LoadFile( "progs.dat" );

            this._Progs = Utilities.BytesToStructure<Program>( buf, 0 );
            if (this._Progs == null )
                Utilities.Error( "PR_LoadProgs: couldn't load Host.Programs.dat" );

            this.Host.Console.DPrint( "Programs occupy {0}K.\n", buf.Length / 1024 );

            for ( var i = 0; i < buf.Length; i++ )
                Framework.Engine.Crc.ProcessByte( ref this._Crc, buf[i] );

            // byte swap the header
            this._Progs.SwapBytes( );

            if (this._Progs.version != ProgramDef.PROG_VERSION )
                Utilities.Error( "progs.dat has wrong version number ({0} should be {1})", this._Progs.version, ProgramDef.PROG_VERSION );
            if (this._Progs.crc != ProgramDef.PROGHEADER_CRC )
                Utilities.Error( "progs.dat system vars have been modified, progdefs.h is out of date" );

            // Functions
            this._Functions = new ProgramFunction[this._Progs.numfunctions];
            var offset = this._Progs.ofs_functions;
            for ( var i = 0; i < this._Functions.Length; i++, offset += ProgramFunction.SizeInBytes )
            {
                this._Functions[i] = Utilities.BytesToStructure<ProgramFunction>( buf, offset );
                this._Functions[i].SwapBytes( );
            }

            // strings
            offset = this._Progs.ofs_strings;
            var str0 = offset;
            for ( var i = 0; i < this._Progs.numstrings; i++, offset++ )
            {
                // count string length
                while ( buf[offset] != 0 )
                    offset++;
            }
            var length = offset - str0;
            this._Strings = Encoding.ASCII.GetString( buf, str0, length );

            // Globaldefs
            this._GlobalDefs = new ProgramDefinition[this._Progs.numglobaldefs];
            offset = this._Progs.ofs_globaldefs;
            for ( var i = 0; i < this._GlobalDefs.Length; i++, offset += ProgramDefinition.SizeInBytes )
            {
                this._GlobalDefs[i] = Utilities.BytesToStructure<ProgramDefinition>( buf, offset );
                this._GlobalDefs[i].SwapBytes( );
            }

            // Fielddefs
            this._FieldDefs = new ProgramDefinition[this._Progs.numfielddefs];
            offset = this._Progs.ofs_fielddefs;
            for ( var i = 0; i < this._FieldDefs.Length; i++, offset += ProgramDefinition.SizeInBytes )
            {
                this._FieldDefs[i] = Utilities.BytesToStructure<ProgramDefinition>( buf, offset );
                this._FieldDefs[i].SwapBytes( );
                if ( (this._FieldDefs[i].type & ProgramDef.DEF_SAVEGLOBAL ) != 0 )
                    Utilities.Error( "PR_LoadProgs: pr_fielddefs[i].type & DEF_SAVEGLOBAL" );
            }

            // Statements
            this._Statements = new Statement[this._Progs.numstatements];
            offset = this._Progs.ofs_statements;
            for ( var i = 0; i < this._Statements.Length; i++, offset += Statement.SizeInBytes )
            {
                this._Statements[i] = Utilities.BytesToStructure<Statement>( buf, offset );
                this._Statements[i].SwapBytes( );
            }

            // Swap bytes inplace if needed
            if ( !BitConverter.IsLittleEndian )
            {
                offset = this._Progs.ofs_globals;
                for ( var i = 0; i < this._Progs.numglobals; i++, offset += 4 )
                    SwapHelper.Swap4b( buf, offset );
            }

            this.GlobalStruct = Utilities.BytesToStructure<GlobalVariables>( buf, this._Progs.ofs_globals );
            this._Globals = new float[this._Progs.numglobals - GlobalVariables.SizeInBytes / 4];
            Buffer.BlockCopy( buf, this._Progs.ofs_globals + GlobalVariables.SizeInBytes, this._Globals, 0, this._Globals.Length * 4 );

            this._EdictSize = this._Progs.entityfields * 4 + Edict.SizeInBytes - EntVars.SizeInBytes;
            ProgramDef.EdictSize = this._EdictSize;
            this._HGlobals = GCHandle.Alloc(this._Globals, GCHandleType.Pinned );
            this._GlobalsAddr = this._HGlobals.AddrOfPinnedObject( ).ToInt64( );

            this._HGlobalStruct = GCHandle.Alloc(this.Host.Programs.GlobalStruct, GCHandleType.Pinned );
            this._GlobalStructAddr = this._HGlobalStruct.AddrOfPinnedObject( ).ToInt64( );
        }

        // ED_PrintEdicts
        //
        // For debugging, prints all the entities in the current server
        public void PrintEdicts( CommandMessage msg )
        {
            this.Host.Console.Print( "{0} entities\n", this.Host.Server.sv.num_edicts );
            for ( var i = 0; i < this.Host.Server.sv.num_edicts; i++ )
                this.PrintNum( i );
        }

        public int StringOffset( string value )
        {
            var tmp = '\0' + value + '\0';
            var offset = this._Strings.IndexOf( tmp, StringComparison.Ordinal );
            if ( offset != -1 )
                return this.MakeStingId( offset + 1, true );

            for ( var i = 0; i < this._DynamicStrings.Count; i++ )
            {
                if (this._DynamicStrings[i] == value )
                    return this.MakeStingId( i, false );
            }
            return -1;
        }

        /// <summary>
        /// ED_LoadFromFile
        /// The entities are directly placed in the array, rather than allocated with
        /// ED_Alloc, because otherwise an error loading the map would have entity
        /// number references out of order.
        ///
        /// Creates a server's entity / program execution context by
        /// parsing textual entity definitions out of an ent file.
        ///
        /// Used for both fresh maps and savegame loads.  A fresh map would also need
        /// to call ED_CallSpawnFunctions () to let the objects initialize themselves.
        /// </summary>
        public void LoadFromFile( string data )
        {
            MemoryEdict ent = null;
            var inhibit = 0;
            this.Host.Programs.GlobalStruct.time = ( float )this.Host.Server.sv.time;

            // parse ents
            while ( true )
            {
                // parse the opening brace
                data = Tokeniser.Parse( data );
                if ( data == null )
                    break;

                if ( Tokeniser.Token != "{" )
                    Utilities.Error( "ED_LoadFromFile: found {0} when expecting {", Tokeniser.Token );

                if ( ent == null )
                    ent = this.Host.Server.EdictNum( 0 );
                else
                    ent = this.Host.Server.AllocEdict( );
                data = this.ParseEdict( data, ent );

                // remove things from different skill levels or deathmatch
                if (this.Host.Cvars.Deathmatch.Get<int>( ) != 0 )
                {
                    if ( ( ( int ) ent.v.spawnflags & SpawnFlags.SPAWNFLAG_NOT_DEATHMATCH ) != 0 )
                    {
                        this.Host.Server.FreeEdict( ent );
                        inhibit++;
                        continue;
                    }
                }
                else if ( (this.Host.CurrentSkill == 0 && ( ( int ) ent.v.spawnflags & SpawnFlags.SPAWNFLAG_NOT_EASY ) != 0 ) ||
                    (this.Host.CurrentSkill == 1 && ( ( int ) ent.v.spawnflags & SpawnFlags.SPAWNFLAG_NOT_MEDIUM ) != 0 ) ||
                    (this.Host.CurrentSkill >= 2 && ( ( int ) ent.v.spawnflags & SpawnFlags.SPAWNFLAG_NOT_HARD ) != 0 ) )
                {
                    this.Host.Server.FreeEdict( ent );
                    inhibit++;
                    continue;
                }

                //
                // immediately call spawn function
                //
                if ( ent.v.classname == 0 )
                {
                    this.Host.Console.Print( "No classname for:\n" );
                    this.Print( ent );
                    this.Host.Server.FreeEdict( ent );
                    continue;
                }

                // look for the spawn function
                var func = this.IndexOfFunction(this.GetString( ent.v.classname ) );
                if ( func == -1 )
                {
                    this.Host.Console.Print( "No spawn function for:\n" );
                    this.Print( ent );
                    this.Host.Server.FreeEdict( ent );
                    continue;
                }

                this.GlobalStruct.self = this.Host.Server.EdictToProg( ent );
                this.Execute( func );
            }

            this.Host.Console.DPrint( "{0} entities inhibited\n", inhibit );
        }

        /// <summary>
        /// ED_ParseEdict
        /// Parses an edict out of the given string, returning the new position
        /// ed should be a properly initialized empty edict.
        /// Used for initial level load and for savegames.
        /// </summary>
        public string ParseEdict( string data, MemoryEdict ent )
        {
            var init = false;

            // clear it
            if ( ent != this.Host.Server.sv.edicts[0] )	// hack
                ent.Clear( );

            // go through all the dictionary pairs
            bool anglehack;
            while ( true )
            {
                // parse key
                data = Tokeniser.Parse( data );
                if ( Tokeniser.Token.StartsWith( "}" ) )
                    break;

                if ( data == null )
                    Utilities.Error( "ED_ParseEntity: EOF without closing brace" );

                var token = Tokeniser.Token;

                // anglehack is to allow QuakeEd to write single scalar angles
                // and allow them to be turned into vectors. (FIXME...)
                if ( token == "angle" )
                {
                    token = "angles";
                    anglehack = true;
                }
                else
                    anglehack = false;

                // FIXME: change light to _light to get rid of this hack
                if ( token == "light" )
                    token = "light_lev";	// hack for single light def

                var keyname = token.TrimEnd( );

                // parse value
                data = Tokeniser.Parse( data );
                if ( data == null )
                    Utilities.Error( "ED_ParseEntity: EOF without closing brace" );

                if ( Tokeniser.Token.StartsWith( "}" ) )
                    Utilities.Error( "ED_ParseEntity: closing brace without data" );

                init = true;

                // keynames with a leading underscore are used for utility comments,
                // and are immediately discarded by quake
                if ( keyname[0] == '_' )
                    continue;

                var key = this.FindField( keyname );
                if ( key == null )
                {
                    this.Host.Console.Print( "'{0}' is not a field\n", keyname );
                    continue;
                }

                token = Tokeniser.Token;
                if ( anglehack )
                    token = "0 " + token + " 0";

                if ( !this.ParsePair( ent, key, token ) )
                    this.Host.Error( "ED_ParseEdict: parse error" );
            }

            if ( !init )
                ent.free = true;

            return data;
        }

        /// <summary>
        /// ED_Print
        /// For debugging
        /// </summary>
        public unsafe void Print( MemoryEdict ed )
        {
            if ( ed.free )
            {
                this.Host.Console.Print( "FREE\n" );
                return;
            }

            this.Host.Console.Print( "\nEDICT {0}:\n", this.Host.Server.NumForEdict( ed ) );
            for ( var i = 1; i < this._Progs.numfielddefs; i++ )
            {
                var d = this._FieldDefs[i];
                var name = this.GetString( d.s_name );

                if ( name.Length > 2 && name[name.Length - 2] == '_' )
                    continue; // skip _x, _y, _z vars

                var type = d.type & ~ProgramDef.DEF_SAVEGLOBAL;
                int offset;
                if ( ed.IsV( d.ofs, out offset ) )
                {
                    fixed ( void* ptr = &ed.v )
                    {
                        var v = ( int* ) ptr + offset;
                        if (this.IsEmptyField( type, v ) )
                            continue;

                        this.Host.Console.Print( "{0,15} ", name );
                        this.Host.Console.Print( "{0}\n", this.ValueString( ( EdictType ) d.type, ( void* ) v ) );
                    }
                }
                else
                {
                    fixed ( void* ptr = ed.fields )
                    {
                        var v = ( int* ) ptr + offset;
                        if (this.IsEmptyField( type, v ) )
                            continue;

                        this.Host.Console.Print( "{0,15} ", name );
                        this.Host.Console.Print( "{0}\n", this.ValueString( ( EdictType ) d.type, ( void* ) v ) );
                    }
                }
            }
        }

        public string GetString( int strId )
        {
            int offset;
            if (this.IsStaticString( strId, out offset ) )
            {
                var i0 = offset;
                while ( offset < this._Strings.Length && this._Strings[offset] != 0 )
                    offset++;

                var length = offset - i0;
                if ( length > 0 )
                    return this._Strings.Substring( i0, length );
            }
            else
            {
                if ( offset < 0 || offset >= this._DynamicStrings.Count )
                    throw new ArgumentException( "Invalid string id!" );

                return this._DynamicStrings[offset];
            }

            return string.Empty;
        }

        public bool SameName( int name1, string name2 )
        {
            var offset = name1;
            if ( offset + name2.Length > this._Strings.Length )
                return false;

            for ( var i = 0; i < name2.Length; i++, offset++ )
            {
                if (this._Strings[offset] != name2[i] )
                    return false;
            }

            if ( offset < this._Strings.Length && this._Strings[offset] != 0 )
                return false;

            return true;
        }

        /// <summary>
        /// Like ED_NewString but returns string id (string_t)
        /// </summary>
        public int NewString( string s )
        {
            var id = this.AllocString( );
            var sb = new StringBuilder( s.Length );
            var len = s.Length;
            for ( var i = 0; i < len; i++ )
            {
                if ( s[i] == '\\' && i < len - 1 )
                {
                    i++;
                    if ( s[i] == 'n' )
                        sb.Append( '\n' );
                    else
                        sb.Append( '\\' );
                }
                else
                    sb.Append( s[i] );
            }

            this.SetString( id, sb.ToString( ) );
            return id;
        }

        public float GetEdictFieldFloat( MemoryEdict ed, string field, float defValue = 0 )
        {
            var def = this.CachedSearch( ed, field );
            if ( def == null )
                return defValue;

            return ed.GetFloat( def.ofs );
        }

        public bool SetEdictFieldFloat( MemoryEdict ed, string field, float value )
        {
            var def = this.CachedSearch( ed, field );
            if ( def != null )
            {
                ed.SetFloat( def.ofs, value );
                return true;
            }
            return false;
        }

        public int AllocString( )
        {
            var id = this._DynamicStrings.Count;
            this._DynamicStrings.Add( string.Empty );
            return this.MakeStingId( id, false );
        }

        public void SetString( int id, string value )
        {
            int offset;
            if (this.IsStaticString( id, out offset ) )
                throw new ArgumentException( "Static strings are read-only!" );

            if ( offset < 0 || offset >= this._DynamicStrings.Count )
                throw new ArgumentException( "Invalid string id!" );

            this._DynamicStrings[offset] = value;
        }

        /// <summary>
        /// ED_WriteGlobals
        /// </summary>
        public unsafe void WriteGlobals( StreamWriter writer )
        {
            writer.WriteLine( "{" );
            for ( var i = 0; i < this._Progs.numglobaldefs; i++ )
            {
                var def = this._GlobalDefs[i];
                var type = ( EdictType ) def.type;
                if ( ( def.type & ProgramDef.DEF_SAVEGLOBAL ) == 0 )
                    continue;

                type &= ( EdictType ) ~ProgramDef.DEF_SAVEGLOBAL;

                if ( type != EdictType.ev_string && type != EdictType.ev_float && type != EdictType.ev_entity )
                    continue;

                writer.Write( "\"" );
                writer.Write(this.GetString( def.s_name ) );
                writer.Write( "\" \"" );
                writer.Write(this.UglyValueString( type, ( EVal* )this.Get( def.ofs ) ) );
                writer.WriteLine( "\"" );
            }
            writer.WriteLine( "}" );
        }

        /// <summary>
        /// ED_Write
        /// </summary>
        public unsafe void WriteEdict( StreamWriter writer, MemoryEdict ed )
        {
            writer.WriteLine( "{" );

            if ( ed.free )
            {
                writer.WriteLine( "}" );
                return;
            }

            for ( var i = 1; i < this._Progs.numfielddefs; i++ )
            {
                var d = this._FieldDefs[i];
                var name = this.GetString( d.s_name );
                if ( name != null && name.Length > 2 && name[name.Length - 2] == '_' )// [strlen(name) - 2] == '_')
                    continue;	// skip _x, _y, _z vars

                var type = d.type & ~ProgramDef.DEF_SAVEGLOBAL;
                int offset1;
                if ( ed.IsV( d.ofs, out offset1 ) )
                {
                    fixed ( void* ptr = &ed.v )
                    {
                        var v = ( int* ) ptr + offset1;
                        if (this.IsEmptyField( type, v ) )
                            continue;

                        writer.WriteLine( "\"{0}\" \"{1}\"", name, this.UglyValueString( ( EdictType ) d.type, ( EVal* ) v ) );
                    }
                }
                else
                {
                    fixed ( void* ptr = ed.fields )
                    {
                        var v = ( int* ) ptr + offset1;
                        if (this.IsEmptyField( type, v ) )
                            continue;

                        writer.WriteLine( "\"{0}\" \"{1}\"", name, this.UglyValueString( ( EdictType ) d.type, ( EVal* ) v ) );
                    }
                }
            }

            writer.WriteLine( "}" );
        }

        /// <summary>
        /// ED_ParseGlobals
        /// </summary>
        public void ParseGlobals( string data )
        {
            while ( true )
            {
                // parse key
                data = Tokeniser.Parse( data );
                if ( Tokeniser.Token.StartsWith( "}" ) )
                    break;

                if ( string.IsNullOrEmpty( data ) )
                    Utilities.Error( "ED_ParseEntity: EOF without closing brace" );

                var keyname = Tokeniser.Token;

                // parse value
                data = Tokeniser.Parse( data );
                if ( string.IsNullOrEmpty( data ) )
                    Utilities.Error( "ED_ParseEntity: EOF without closing brace" );

                if ( Tokeniser.Token.StartsWith( "}" ) )
                    Utilities.Error( "ED_ParseEntity: closing brace without data" );

                var key = this.FindGlobal( keyname );
                if ( key == null )
                {
                    this.Host.Console.Print( "'{0}' is not a global\n", keyname );
                    continue;
                }

                if ( !this.ParseGlobalPair( key, Tokeniser.Token ) )
                    this.Host.Error( "ED_ParseGlobals: parse error" );
            }
        }

        /// <summary>
        /// ED_PrintNum
        /// </summary>
        public void PrintNum( int ent )
        {
            this.Print(this.Host.Server.EdictNum( ent ) );
        }

        private void Test5_f( CommandMessage msg )
        {
            var p = this.Host.Client.ViewEntity;
            if ( p == null )
                return;

            var org = p.origin;

            for ( var i = 0; i < this.Host.Server.sv.edicts.Length; i++ )
            {
                var ed = this.Host.Server.sv.edicts[i];

                if ( ed.free )
                    continue;

                Vector3 vmin, vmax;
                MathLib.Copy( ref ed.v.absmax, out vmax );
                MathLib.Copy( ref ed.v.absmin, out vmin );

                if ( org.X >= vmin.X && org.Y >= vmin.Y && org.Z >= vmin.Z &&
                    org.X <= vmax.X && org.Y <= vmax.Y && org.Z <= vmax.Z )
                    this.Host.Console.Print( "{0}\n", i );
            }
        }

        private void FreeHandles( )
        {
            if (this._HGlobals.IsAllocated )
            {
                this._HGlobals.Free( );
                this._GlobalsAddr = 0;
            }
            if (this._HGlobalStruct.IsAllocated )
            {
                this._HGlobalStruct.Free( );
                this._GlobalStructAddr = 0;
            }
        }

        /// <summary>
        /// ED_PrintEdict_f
        /// For debugging, prints a single edict
        /// </summary>
        private void PrintEdict_f( CommandMessage msg )
        {
            var i = MathLib.atoi( msg.Parameters[0] );
            if ( i >= this.Host.Server.sv.num_edicts )
            {
                this.Host.Console.Print( "Bad edict number\n" );
                return;
            }

            this.Host.Programs.PrintNum( i );
        }

        // ED_Count
        //
        // For debugging
        private void EdictCount( CommandMessage msg )
        {
            int active = 0, models = 0, solid = 0, step = 0;

            for ( var i = 0; i < this.Host.Server.sv.num_edicts; i++ )
            {
                var ent = this.Host.Server.EdictNum( i );
                if ( ent.free )
                    continue;
                active++;
                if ( ent.v.solid != 0 )
                    solid++;
                if ( ent.v.model != 0 )
                    models++;
                if ( ent.v.movetype == Movetypes.MOVETYPE_STEP )
                    step++;
            }

            this.Host.Console.Print( "num_edicts:{0}\n", this.Host.Server.sv.num_edicts );
            this.Host.Console.Print( "active    :{0}\n", active );
            this.Host.Console.Print( "view      :{0}\n", models );
            this.Host.Console.Print( "touch     :{0}\n", solid );
            this.Host.Console.Print( "step      :{0}\n", step );
        }

        private int IndexOfFunction( string name )
        {
            for ( var i = 0; i < this._Functions.Length; i++ )
            {
                if (this.SameName(this._Functions[i].s_name, name ) )
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Since memory block containing original edict_t plus additional data
        /// is split into two fiels - edict_t.v and edict_t.fields we must check key.ofs
        /// to choose between thistwo parts.
        /// Warning: Key offset is in integers not bytes!
        /// </summary>
        private unsafe bool ParsePair( MemoryEdict ent, ProgramDefinition key, string s )
        {
            int offset1;
            if ( ent.IsV( key.ofs, out offset1 ) )
            {
                fixed ( EntVars* ptr = &ent.v )
                    return this.ParsePair( ( int* ) ptr + offset1, key, s );
            }
            else
            {
                fixed ( float* ptr = ent.fields )
                    return this.ParsePair( ptr + offset1, key, s );
            }
        }

        /// <summary>
        /// ED_ParseEpair
        /// Can parse either fields or globals returns false if error
        /// Uze: Warning! value pointer is already with correct offset (value = base + key.ofs)!
        /// </summary>
        private unsafe bool ParsePair( void* value, ProgramDefinition key, string s )
        {
            var d = value;// (void *)((int *)base + key->ofs);

            switch ( ( EdictType ) ( key.type & ~ProgramDef.DEF_SAVEGLOBAL ) )
            {
                case EdictType.ev_string:
                    *( int* ) d = this.NewString( s );// - pr_strings;
                    break;

                case EdictType.ev_float:
                    *( float* ) d = MathLib.atof( s );
                    break;

                case EdictType.ev_vector:
                    var vs = s.Split( ' ' );
                    ( ( float* ) d )[0] = MathLib.atof( vs[0] );
                    ( ( float* ) d )[1] = vs.Length > 1 ? MathLib.atof( vs[1] ) : 0;
                    ( ( float* ) d )[2] = vs.Length > 2 ? MathLib.atof( vs[2] ) : 0;
                    break;

                case EdictType.ev_entity:
                    *( int* ) d = this.Host.Server.EdictToProg(this.Host.Server.EdictNum( MathLib.atoi( s ) ) );
                    break;

                case EdictType.ev_field:
                    var f = this.IndexOfField( s );
                    if ( f == -1 )
                    {
                        this.Host.Console.Print( "Can't find field {0}\n", s );
                        return false;
                    }
                    *( int* ) d = this.GetInt32(this._FieldDefs[f].ofs );
                    break;

                case EdictType.ev_function:
                    var func = this.IndexOfFunction( s );
                    if ( func == -1 )
                    {
                        this.Host.Console.Print( "Can't find function {0}\n", s );
                        return false;
                    }
                    *( int* ) d = func;// - pr_functions;
                    break;

                default:
                    break;
            }
            return true;
        }

        private int IndexOfField( string name )
        {
            for ( var i = 0; i < this._FieldDefs.Length; i++ )
            {
                if (this.SameName(this._FieldDefs[i].s_name, name ) )
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Returns true if ofs is inside GlobalStruct or false if ofs is in _Globals
        /// Out parameter offset is set to correct offset inside either GlobalStruct or _Globals
        /// </summary>
        private bool IsGlobalStruct( int ofs, out int offset )
        {
            if ( ofs < GlobalVariables.SizeInBytes >> 2 )
            {
                offset = ofs;
                return true;
            }
            offset = ofs - ( GlobalVariables.SizeInBytes >> 2 );
            return false;
        }

        /// <summary>
        /// Mimics G_xxx macros
        /// But globals are split too, so we must check offset and choose
        /// GlobalStruct or _Globals
        /// </summary>
        private unsafe void* Get( int offset )
        {
            int offset1;
            if (this.IsGlobalStruct( offset, out offset1 ) )
                return ( int* )this._GlobalStructAddr + offset1;

            return ( int* )this._GlobalsAddr + offset1;
        }

        private unsafe void Set( int offset, int value )
        {
            if ( offset < GlobalVariables.SizeInBytes >> 2 )
                *( ( int* )this._GlobalStructAddr + offset ) = value;
            else
                *( ( int* )this._GlobalsAddr + offset - ( GlobalVariables.SizeInBytes >> 2 ) ) = value;
        }

        private unsafe int GetInt32( int offset )
        {
            return *( int* )this.Get( offset );
        }

        /// <summary>
        /// ED_FindField
        /// </summary>
        private ProgramDefinition FindField( string name )
        {
            var i = this.IndexOfField( name );
            if ( i != -1 )
                return this._FieldDefs[i];

            return null;
        }

        /// <summary>
        /// PR_ValueString
        /// </summary>
        private unsafe string ValueString( EdictType type, void* val )
        {
            string result;
            type &= ( EdictType ) ~ProgramDef.DEF_SAVEGLOBAL;

            switch ( type )
            {
                case EdictType.ev_string:
                    result = this.GetString( *( int* ) val );
                    break;

                case EdictType.ev_entity:
                    result = "entity " + this.Host.Server.NumForEdict(this.Host.Server.ProgToEdict( *( int* ) val ) );
                    break;

                case EdictType.ev_function:
                    var f = this._Functions[*( int* ) val];
                    result = this.GetString( f.s_name ) + "()";
                    break;

                case EdictType.ev_field:
                    var def = this.FindField( *( int* ) val );
                    result = "." + this.GetString( def.s_name );
                    break;

                case EdictType.ev_void:
                    result = "void";
                    break;

                case EdictType.ev_float:
                    result = ( *( float* ) val ).ToString( "F1", CultureInfo.InvariantCulture.NumberFormat );
                    break;

                case EdictType.ev_vector:
                    result = string.Format( CultureInfo.InvariantCulture.NumberFormat,
                        "{0,5:F1} {1,5:F1} {2,5:F1}", ( ( float* ) val )[0], ( ( float* ) val )[1], ( ( float* ) val )[2] );
                    break;

                case EdictType.ev_pointer:
                    result = "pointer";
                    break;

                default:
                    result = "bad type " + type.ToString( );
                    break;
            }

            return result;
        }

        private int IndexOfField( int ofs )
        {
            for ( var i = 0; i < this._FieldDefs.Length; i++ )
            {
                if (this._FieldDefs[i].ofs == ofs )
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// ED_FieldAtOfs
        /// </summary>
        private ProgramDefinition FindField( int ofs )
        {
            var i = this.IndexOfField( ofs );
            if ( i != -1 )
                return this._FieldDefs[i];

            return null;
        }

        private ProgramDefinition CachedSearch( MemoryEdict ed, string field )
        {
            ProgramDefinition def = null;
            for ( var i = 0; i < Programs.GEFV_CACHESIZE; i++ )
            {
                if ( field == this._gefvCache[i].field )
                {
                    def = this._gefvCache[i].pcache;
                    return def;
                }
            }

            def = this.FindField( field );

            this._gefvCache[this._gefvPos].pcache = def;
            this._gefvCache[this._gefvPos].field = field;
            this._gefvPos ^= 1;

            return def;
        }

        private int MakeStingId( int index, bool isStatic )
        {
            return ( ( isStatic ? 0 : 1 ) << 24 ) + ( index & 0xFFFFFF );
        }

        private bool IsStaticString( int stringId, out int offset )
        {
            offset = stringId & 0xFFFFFF;
            return ( ( stringId >> 24 ) & 1 ) == 0;
        }

        /// <summary>
        /// PR_UglyValueString
        /// Returns a string describing *data in a type specific manner
        /// Easier to parse than PR_ValueString
        /// </summary>
        private unsafe string UglyValueString( EdictType type, EVal* val )
        {
            type &= ( EdictType ) ~ProgramDef.DEF_SAVEGLOBAL;
            string result;

            switch ( type )
            {
                case EdictType.ev_string:
                    result = this.GetString( val->_string );
                    break;

                case EdictType.ev_entity:
                    result = this.Host.Server.NumForEdict(this.Host.Server.ProgToEdict( val->edict ) ).ToString( );
                    break;

                case EdictType.ev_function:
                    var f = this._Functions[val->function];
                    result = this.GetString( f.s_name );
                    break;

                case EdictType.ev_field:
                    var def = this.FindField( val->_int );
                    result = this.GetString( def.s_name );
                    break;

                case EdictType.ev_void:
                    result = "void";
                    break;

                case EdictType.ev_float:
                    result = val->_float.ToString( "F6", CultureInfo.InvariantCulture.NumberFormat );
                    break;

                case EdictType.ev_vector:
                    result = string.Format( CultureInfo.InvariantCulture.NumberFormat,
                        "{0:F6} {1:F6} {2:F6}", val->vector[0], val->vector[1], val->vector[2] );
                    break;

                default:
                    result = "bad type " + type.ToString( );
                    break;
            }

            return result;
        }

        private unsafe bool IsEmptyField( int type, int* v )
        {
            for ( var j = 0; j < this._TypeSize[type]; j++ )
            {
                if ( v[j] != 0 )
                    return false;
            }

            return true;
        }

        /// <summary>
        /// ED_FindGlobal
        /// </summary>
        private ProgramDefinition FindGlobal( string name )
        {
            for ( var i = 0; i < this._GlobalDefs.Length; i++ )
            {
                var def = this._GlobalDefs[i];
                if ( name == this.GetString( def.s_name ) )
                    return def;
            }
            return null;
        }

        private unsafe bool ParseGlobalPair( ProgramDefinition key, string value )
        {
            int offset;
            if (this.IsGlobalStruct( key.ofs, out offset ) )
                return this.ParsePair( ( float* )this._GlobalStructAddr + offset, key, value );

            return this.ParsePair( ( float* )this._GlobalsAddr + offset, key, value );
        }

        /// <summary>
        /// PR_GlobalString
        /// Returns a string with a description and the contents of a global,
        /// padded to 20 field width
        /// </summary>
        private unsafe string GlobalString( int ofs )
        {
            var line = string.Empty;
            var val = this.Get( ofs );// (void*)&pr_globals[ofs];
            var def = this.GlobalAtOfs( ofs );
            if ( def == null )
                line = string.Format( "{0}(???)", ofs );
            else
            {
                var s = this.ValueString( ( EdictType ) def.type, val );
                line = string.Format( "{0}({1}){2} ", ofs, this.GetString( def.s_name ), s );
            }

            line = line.PadRight( 20 );

            return line;
        }

        /// <summary>
        /// PR_GlobalStringNoContents
        /// </summary>
        private string GlobalStringNoContents( int ofs )
        {
            var line = string.Empty;
            var def = this.GlobalAtOfs( ofs );
            if ( def == null )
                line = string.Format( "{0}(???)", ofs );
            else
                line = string.Format( "{0}({1}) ", ofs, this.GetString( def.s_name ) );

            line = line.PadRight( 20 );

            return line;
        }

        /// <summary>
        /// ED_GlobalAtOfs
        /// </summary>
        private ProgramDefinition GlobalAtOfs( int ofs )
        {
            for ( var i = 0; i < this._GlobalDefs.Length; i++ )
            {
                var def = this._GlobalDefs[i];
                if ( def.ofs == ofs )
                    return def;
            }
            return null;
        }
    }
}
