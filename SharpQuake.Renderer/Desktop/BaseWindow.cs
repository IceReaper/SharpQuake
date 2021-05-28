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

namespace SharpQuake.Renderer.Desktop
{
    using Framework.IO.Input;
    using System;
    using System.Drawing;

    public class BaseWindow : IDisposable
    {
        public bool IsDisposing
        {
            get;
            protected set;
        }

        public bool IsDisposed
        {
            get;
            protected set;
        }

        public virtual VSyncMode VSync
        {
            get;
            set;
        }

        public virtual bool IsFullScreen
        {
            get;
        }

        public virtual Size ClientSize
        {
            get;
            set;
        }

        public virtual bool Focused
        {
            get;
        }

        public virtual bool IsMinimised
        {
            get;
        }

        public virtual bool CursorVisible
        {
            get;
            set;
        }

        public virtual Rectangle Bounds
        {
            get;
            set;
        }

        public virtual bool IsMouseActive
        {
            get;
        }

        public BaseDevice Device
        {
            get;
            protected set;
        }

        public EventHandler<KeyboardKeyEventArgs> KeyUp;
        public EventHandler<KeyboardKeyEventArgs> KeyDown;
        public EventHandler<EventArgs> MouseMove;
        public EventHandler<MouseButtonEventArgs> MouseUp;
        public EventHandler<MouseButtonEventArgs> MouseDown;
        public EventHandler<MouseWheelEventArgs> MouseWheel;

        public BaseWindow( string title, Size size, bool isFullScreen )
        {
        }

        public virtual void RouteEvents()
        {
            throw new NotImplementedException( );
        }
        
        public virtual void Run( )
        {
            throw new NotImplementedException( );
        }

        protected virtual void OnFocusedChanged( )
        {
            throw new NotImplementedException( );
        }

        protected virtual void OnClosing( )
        {
            throw new NotImplementedException( );
        }

        protected virtual void OnUpdateFrame( double Time )
        {
            throw new NotImplementedException( );
        }

        public virtual void Present( )
        {
            throw new NotImplementedException( );
        }

        public virtual void SetFullScreen( bool isFullScreen )
        {
            throw new NotImplementedException( );
        }

        public virtual void ProcessEvents( )
        {
            throw new NotImplementedException( );
        }

        public virtual void Exit( )
        {
            throw new NotImplementedException( );
        }

        public virtual void SetMousePosition( int x, int y )
        {
            throw new NotImplementedException( );
        }

        public virtual Point GetMousePosition( )
        {
            throw new NotImplementedException( );
        }

        public virtual void Dispose( )
        {
            this.IsDisposing = true;

            this.Device.Dispose( );
        }
    }
}
