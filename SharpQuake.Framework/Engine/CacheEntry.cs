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
    public class CacheEntry : CacheUser
    {
        public CacheEntry Next => this._Next;

        public CacheEntry Prev => this._Prev;

        public CacheEntry LruPrev => this._LruPrev;

        public CacheEntry LruNext => this._LruNext;

        private Cache Cache
        {
            get;
            set;
        }

        private CacheEntry _Prev;
        private CacheEntry _Next;
        private CacheEntry _LruPrev;
        private CacheEntry _LruNext;
        private int _Size;

        // Cache_UnlinkLRU
        public void RemoveFromLRU( )
        {
            if (this._LruNext == null || this._LruPrev == null )
                Utilities.Error( "Cache_UnlinkLRU: NULL link" );

            this._LruNext._LruPrev = this._LruPrev;
            this._LruPrev._LruNext = this._LruNext;
            this._LruPrev = this._LruNext = null;
        }

        // inserts <this> instance after <prev> in LRU list
        public void LRUInstertAfter( CacheEntry prev )
        {
            if (this._LruNext != null || this._LruPrev != null )
                Utilities.Error( "Cache_MakeLRU: active link" );

            prev._LruNext._LruPrev = this;
            this._LruNext = prev._LruNext;
            this._LruPrev = prev;
            prev._LruNext = this;
        }

        // inserts <this> instance before <next>
        public void InsertBefore( CacheEntry next )
        {
            this._Next = next;
            if ( next._Prev != null )
                this._Prev = next._Prev;
            else
                this._Prev = next;

            if ( next._Prev != null )
                next._Prev._Next = this;
            else
                next._Prev = this;
            next._Prev = this;

            if ( next._Next == null )
                next._Next = this;
        }

        public void Remove( )
        {
            this._Prev._Next = this._Next;
            this._Next._Prev = this._Prev;
            this._Next = this._Prev = null;

            this.data = null;
            this.Cache.BytesAllocated -= this._Size;
            this._Size = 0;

            this.RemoveFromLRU( );
        }

        public CacheEntry( Cache cache, bool isHead = false )
        {
            if ( isHead )
            {
                this._Next = this;
                this._Prev = this;
                this._LruNext = this;
                this._LruPrev = this;
            }
        }

        public CacheEntry( Cache cache, int size )
        {
            this.Cache = cache;

            this._Size = size;
            this.Cache.BytesAllocated += this._Size;
        }

        ~CacheEntry( )
        {
            if (this.Cache != null )
                this.Cache.BytesAllocated -= this._Size;
        }
    }
}
