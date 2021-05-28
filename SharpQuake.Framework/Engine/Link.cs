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
    using System.Runtime.InteropServices;

    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public class Link
    {
        private Link _Prev, _Next;
        private object _Owner;

        public Link Prev => this._Prev;

        public Link Next => this._Next;

        public object Owner => this._Owner;

        public Link( object owner )
        {
            this._Owner = owner;
        }

        public void Clear( )
        {
            this._Prev = this._Next = this;
        }

        public void ClearToNulls( )
        {
            this._Prev = this._Next = null;
        }

        public void Remove( )
        {
            this._Next._Prev = this._Prev;
            this._Prev._Next = this._Next;
            this._Next = null;
            this._Prev = null;
        }

        public void InsertBefore( Link before )
        {
            this._Next = before;
            this._Prev = before._Prev;
            this._Prev._Next = this;
            this._Next._Prev = this;
        }

        public void InsertAfter( Link after )
        {
            this._Next = after.Next;
            this._Prev = after;
            this._Prev._Next = this;
            this._Next._Prev = this;
        }
    } // link_t;
}
