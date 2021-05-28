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
    using System;

    public class DisposableWrapper<T> : IDisposable where T : class, IDisposable
    {
        public T Object => this._Object;

        private T _Object;
        private bool _Owned;

        private void Dispose( bool disposing )
        {
            if (this._Object != null && this._Owned )
            {
                this._Object.Dispose( );
                this._Object = null;
            }
        }

        public DisposableWrapper( T obj, bool dispose )
        {
            this._Object = obj;
            this._Owned = dispose;
        }

        ~DisposableWrapper( )
        {
            this.Dispose( false );
        }

        #region IDisposable Members

        public void Dispose( )
        {
            this.Dispose( true );
            GC.SuppressFinalize( this );
        }

        #endregion IDisposable Members
    }
}
