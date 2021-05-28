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

namespace SharpQuake.Renderer.OpenGL.Desktop
{
    using Framework.IO.Input;
    using OpenTK.Windowing.Common;
    using OpenTK.Windowing.Desktop;
    using OpenTK.Windowing.GraphicsLibraryFramework;
    using Renderer.Desktop;
    using System.Drawing;
    using MouseButton = Framework.IO.Input.MouseButton;
    using VSyncMode = Renderer.VSyncMode;

    public class GLWindow : BaseWindow
    {
        private GameWindow OpenTKWindow
        {
            get;
            set;
        }

        private Monitor DisplayDevice
        {
            get;
            set;
        }

        public override VSyncMode VSync
        {

            get
            {
                switch (this.OpenTKWindow.VSync )
                {
                    case OpenTK.Windowing.Common.VSyncMode.On:
                        return VSyncMode.One;

                    case OpenTK.Windowing.Common.VSyncMode.Adaptive:
                        return VSyncMode.Other;
                }

                return VSyncMode.None;
            }
            set
            {
                switch ( value )
                {
                    case VSyncMode.One:
                        this.OpenTKWindow.VSync = OpenTK.Windowing.Common.VSyncMode.On;
                        break;

                    case VSyncMode.None:
                        this.OpenTKWindow.VSync = OpenTK.Windowing.Common.VSyncMode.Off;
                        break;

                    case VSyncMode.Other:
                        this.OpenTKWindow.VSync = OpenTK.Windowing.Common.VSyncMode.Adaptive;
                        break;
                }
            }
        }

        public override Size ClientSize
        {
            get => new(this.OpenTKWindow.Size.X, this.OpenTKWindow.Size.Y);
            set => this.OpenTKWindow.Size = new(value.Width, value.Height);
        }

        public override bool IsFullScreen => this.OpenTKWindow.WindowState == WindowState.Fullscreen;

        public override bool Focused => this.OpenTKWindow.IsFocused;

        public override bool IsMinimised => this.OpenTKWindow.WindowState == WindowState.Minimized;

        public override bool CursorVisible
        {
            get => this.OpenTKWindow.CursorVisible;
            set => this.OpenTKWindow.CursorVisible = value;
        }

        public override Rectangle Bounds
        {
            get => new(this.OpenTKWindow.Bounds.Min.X, this.OpenTKWindow.Bounds.Min.Y, this.OpenTKWindow.Bounds.Max.X - this.OpenTKWindow.Bounds.Min.X, this.OpenTKWindow.Bounds.Max.Y - this.OpenTKWindow.Bounds.Min.Y);
            set => this.OpenTKWindow.Bounds = new(value.Left, value.Top, value.Right, value.Bottom);
        }

        public override bool IsMouseActive => true;

        public unsafe GLWindow( string title, Size size, bool isFullScreen ) : base( title, size, isFullScreen )
        {
            //Workaround for SDL2 mouse input issues
            GLFW.Init();

            // select display device
            this.DisplayDevice = *GLFW.GetPrimaryMonitor();

            this.OpenTKWindow = new( new(), new(){ Size = new (size.Width, size.Height), Title = title, IsFullscreen = isFullScreen, Profile = ContextProfile.Compatability} );

            this.RouteEvents( );

            this.Device = new GLDevice(this.OpenTKWindow, this.DisplayDevice );
        }

        public override void RouteEvents( )
        {
            this.OpenTKWindow.FocusedChanged += ( args ) =>
            {
                this.OnFocusedChanged( );
            };

            this.OpenTKWindow.Closing += ( args ) =>
            {
                this.OnClosing( );
            };

            this.OpenTKWindow.UpdateFrame += ( args ) =>
            {
                this.OnUpdateFrame( args.Time );
            };

            this.OpenTKWindow.KeyDown += ( args ) =>
            {
                this.KeyDown?.Invoke( null, new( ( Key ) ( int ) args.Key ) );
            };

            this.OpenTKWindow.KeyUp += ( args ) =>
            {
                this.KeyUp?.Invoke( null, new( ( Key ) ( int ) args.Key ) );
            };

            this.OpenTKWindow.MouseMove += ( args ) =>
            {
                this.MouseMove?.Invoke( null, new( ) );
            };

            this.OpenTKWindow.MouseDown += ( args ) =>
            {
                this.MouseDown?.Invoke( null, new( ( MouseButton ) ( int ) args.Button, args.IsPressed ) );
            };

            this.OpenTKWindow.MouseUp += ( args ) =>
            {
                this.MouseUp?.Invoke( null, new( ( MouseButton ) ( int ) args.Button, args.IsPressed ) );
            };

            this.OpenTKWindow.MouseWheel += ( args ) =>
            {
                this.MouseWheel?.Invoke( null, new( (int)args.OffsetY ) );
            };
        }

        public override void Run( )
        {
            this.OpenTKWindow.Run( );
        }

        protected override void OnFocusedChanged( )
        {
            //throw new NotImplementedException( );
        }

        protected override void OnClosing( )
        {
            //throw new NotImplementedException( );
        }

        protected override void OnUpdateFrame( double Time )
        {
            //throw new NotImplementedException( );
        }

        public override void Present( )
        {
            this.OpenTKWindow.SwapBuffers( );
        }

        public override void SetFullScreen( bool isFullScreen )
        {
            if ( isFullScreen )
            {
                this.OpenTKWindow.WindowState = WindowState.Fullscreen;
                this.OpenTKWindow.WindowBorder = WindowBorder.Hidden;
            }
            else
            {
                this.OpenTKWindow.WindowState = WindowState.Normal;
                this.OpenTKWindow.WindowBorder = WindowBorder.Fixed;
            }
        }


        public override void ProcessEvents( )
        {
            this.OpenTKWindow.ProcessEvents( );
        }

        public override void Exit( )
        {
            this.OpenTKWindow.Close( );
        }

        public override void SetMousePosition( int x, int y )
        {
            this.OpenTKWindow.MousePosition = new( x, y );
        }

        public override Point GetMousePosition( )
        {
            return new( (int)this.OpenTKWindow.MousePosition.X,
                (int)this.OpenTKWindow.MousePosition.Y );
        }

        public override void Dispose( )
        {
            base.Dispose( );

            this.OpenTKWindow.Dispose( );

            this.IsDisposed = true;
        }
    }
}
