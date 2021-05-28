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
    using IO;

    /// <summary>
    /// Cache_functions
    /// </summary>
    public class Cache
    {
        public CacheEntry Head
        {
            get;
            private set;
        }

        public int Capacity
        {
            get;
            private set;
        }

        public int BytesAllocated
        {
            get;
            set;
        }


        // Cache_Init
        public void Initialise( int capacity )
        {
            this.Capacity = capacity;
            this.BytesAllocated = 0;
            this.Head = new( this, true );
        }

        // Cache_Check
        /// <summary>
        /// Cache_Check
        /// Returns value of c.data if still cached or null
        /// </summary>
        public object Check( CacheUser c )
        {
            var cs = ( CacheEntry ) c;

            if ( cs == null || cs.data == null )
                return null;

            // move to head of LRU
            cs.RemoveFromLRU( );
            cs.LRUInstertAfter(this.Head );

            return cs.data;
        }

        // Cache_Alloc
        public CacheUser Alloc( int size, string name )
        {
            if ( size <= 0 )
                Utilities.Error( "Cache_Alloc: size {0}", size );

            size = ( size + 15 ) & ~15;

            CacheEntry entry = null;

            // find memory for it
            while ( true )
            {
                entry = this.TryAlloc( size );
                if ( entry != null )
                    break;

                // free the least recently used cahedat
                if (this.Head.LruPrev == this.Head )// cache_head.lru_prev == &cache_head)
                    Utilities.Error( "Cache_Alloc: out of memory" );
                // not enough memory at all
                this.Free(this.Head.LruPrev );
            }

            this.Check( entry );
            return entry;
        }

        /// <summary>
        /// Cache_Report
        /// </summary>
        public void Report( )
        {
            ConsoleWrapper.DPrint( "{0,4:F1} megabyte data cache, used {1,4:F1} megabyte\n", this.Capacity / ( float ) ( 1024 * 1024 ), this.BytesAllocated / ( float ) ( 1024 * 1024 ) );
        }

        //Cache_Flush
        //
        //Throw everything out, so new data will be demand cached
        public void Flush( CommandMessage msg )
        {
            while (this.Head.Next != this.Head )
                this.Free(this.Head.Next ); // reclaim the space
        }

        // Cache_Free
        //
        // Frees the memory and removes it from the LRU list
        private void Free( CacheUser c )
        {
            if ( c.data == null )
                Utilities.Error( "Cache_Free: not allocated" );

            var entry = ( CacheEntry ) c;
            entry.Remove( );
        }

        // Cache_TryAlloc
        private CacheEntry TryAlloc( int size )
        {
            if (this.BytesAllocated + size > this.Capacity )
                return null;

            var result = new CacheEntry( this, size );
            this.Head.InsertBefore( result );
            result.LRUInstertAfter(this.Head );
            return result;
        }
    }
}
