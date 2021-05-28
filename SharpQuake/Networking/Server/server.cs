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

namespace SharpQuake.Networking.Server
{
    using Engine.Host;
    using Framework.Definitions;
    using Framework.Engine;
    using Framework.Networking.Server;
    using Game.Networking.Server;
    using System;

    public partial class server
    {
        public server_t sv => this._Server;

        public server_static_t svs => this._ServerStatic;

        public bool IsActive => this._Server.active;

        public float Gravity => this.Host.Cvars.Gravity.Get<float>( );

        public bool IsLoading => this._Server.state == server_state_t.Loading;

        public float Aim => this.Host.Cvars.Aim.Get<float>( );

        private server_t _Server;
        private server_static_t _ServerStatic;

        private string[] _LocalModels = new string[QDef.MAX_MODELS]; //[MAX_MODELS][5];	// inline model names for precache

        /// <summary>
        /// EDICT_NUM
        /// </summary>
        public MemoryEdict EdictNum( int n )
        {
            if( n < 0 || n >= this._Server.max_edicts )
                Utilities.Error( "EDICT_NUM: bad number {0}", n );
            return this._Server.edicts[n];
        }

        /// <summary>
        /// ED_Alloc
        /// Either finds a free edict, or allocates a new one.
        /// Try to avoid reusing an entity that was recently freed, because it
        /// can cause the client to think the entity morphed into something else
        /// instead of being removed and recreated, which can cause interpolated
        /// angles and bad trails.
        /// </summary>
        public MemoryEdict AllocEdict()
        {
            MemoryEdict e;
            int i;
            for( i = this.svs.maxclients + 1; i < this.sv.num_edicts; i++ )
            {
                e = this.EdictNum( i );

                // the first couple seconds of server time can involve a lot of
                // freeing and allocating, so relax the replacement policy
                if( e.free && ( e.freetime < 2 || this.sv.time - e.freetime > 0.5 ) )
                {
                    e.Clear();
                    return e;
                }
            }

            if( i == QDef.MAX_EDICTS )
                Utilities.Error( "ED_Alloc: no free edicts" );

            this.sv.num_edicts++;
            e = this.EdictNum( i );
            e.Clear();

            return e;
        }

        /// <summary>
        /// ED_Free
        /// Marks the edict as free
        /// FIXME: walk all entities and NULL out references to this entity
        /// </summary>
        public void FreeEdict( MemoryEdict ed )
        {
            this.UnlinkEdict( ed );		// unlink from world bsp

            ed.free = true;
            ed.v.model = 0;
            ed.v.takedamage = 0;
            ed.v.modelindex = 0;
            ed.v.colormap = 0;
            ed.v.skin = 0;
            ed.v.frame = 0;
            ed.v.origin = default;
            ed.v.angles = default;
            ed.v.nextthink = -1;
            ed.v.solid = 0;

            ed.freetime = ( float )this.sv.time;
        }

        /// <summary>
        /// EDICT_TO_PROG(e)
        /// </summary>
        public int EdictToProg( MemoryEdict e )
        {
            return Array.IndexOf(this._Server.edicts, e ); // todo: optimize this
        }

        /// <summary>
        /// PROG_TO_EDICT(e)
        /// Offset in bytes!
        /// </summary>
        public MemoryEdict ProgToEdict( int e )
        {
            if( e < 0 || e > this.sv.edicts.Length )
                Utilities.Error( "ProgToEdict: Bad prog!" );
            return this.sv.edicts[e];
        }

        /// <summary>
        /// NUM_FOR_EDICT
        /// </summary>
        public int NumForEdict( MemoryEdict e )
        {
            var i = Array.IndexOf(this.sv.edicts, e ); // todo: optimize this

            if( i < 0 )
                Utilities.Error( "NUM_FOR_EDICT: bad pointer" );
            return i;
        }

        public server( Host host )
        {
            this.Host = host;

            this._Server = new();
            this._ServerStatic = new();
        }
    }    
}
