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

namespace SharpQuake.Desktop
{
    using Engine;
    using Engine.Host;
    using Framework.Definitions;
    using Framework.Engine;
    using Framework.IO.Input;
    using Renderer;
    using Renderer.OpenGL.Desktop;
    using System;
    using System.Diagnostics;
    using System.Drawing;
    using System.IO;

    public class MainWindow : GLWindow//GameWindow
    {
        public static MainWindow Instance => ( MainWindow ) MainWindow._Instance.Target;

        public static bool IsFullscreen => MainWindow.Instance.IsFullScreen;

        public bool ConfirmExit = true;

        private static string DumpFilePath => Path.Combine( AppDomain.CurrentDomain.BaseDirectory, "error.txt" );

        // This is where we start porting stuff over to proper instanced classes - TODO
        public Host Host
        {
            get;
            private set;
        }

        public static Input Input
        {
            get;
            private set;
        }

        public static Common Common
        {
            get;
            private set;
        }

        private static WeakReference _Instance;

        private int _MouseBtnState;
        private Stopwatch _Swatch;

        public bool IsDisposing
        {
            get;
            private set;
        }

        protected override void OnFocusedChanged( )
        {
            base.OnFocusedChanged( );

            if (this.Focused )
                this.Host.Sound.UnblockSound( );
            else
                this.Host.Sound.BlockSound( );
        }

        protected override void OnClosing(  )
        {
            // Turned this of as I hate this prompt so much 
            /*if (this.ConfirmExit)
            {
                int button_id;
                SDL.SDL_MessageBoxButtonData[] buttons = new SDL.SDL_MessageBoxButtonData[2];

                buttons[0].flags = SDL.SDL_MessageBoxButtonFlags.SDL_MESSAGEBOX_BUTTON_ESCAPEKEY_DEFAULT;
                buttons[0].buttonid = 0;
                buttons[0].text = "cancel";

                buttons[1].flags = SDL.SDL_MessageBoxButtonFlags.SDL_MESSAGEBOX_BUTTON_RETURNKEY_DEFAULT;
                buttons[1].buttonid = 1;
                buttons[1].text = "yes";

                SDL.SDL_MessageBoxData messageBoxData = new SDL.SDL_MessageBoxData();
                messageBoxData.flags = SDL.SDL_MessageBoxFlags.SDL_MESSAGEBOX_INFORMATION;
                messageBoxData.window = IntPtr.Zero;
                messageBoxData.title = "test";
                messageBoxData.message = "test";
                messageBoxData.numbuttons = 2;
                messageBoxData.buttons = buttons;
                SDL.SDL_ShowMessageBox(ref messageBoxData, out button_id);

                if (button_id == -1)
                {
                    "error displaying message box"
                }
                else
                {
                   "selection was %s"
                }

                // e.Cancel = (MessageBox.Show("Are you sure you want to quit?",
                //"Confirm Exit", MessageBoxButtons.YesNo) != DialogResult.Yes);
            }
            */
            base.OnClosing(  );
        }

        protected override void OnUpdateFrame( double time )
        {
            try
            {
                if (this.IsMinimised || this.Host.Screen.BlockDrawing || this.Host.IsDisposing )
                    this.Host.Screen.SkipUpdate = true;	// no point in bothering to draw

                this._Swatch.Stop( );
                var ts = this._Swatch.Elapsed.TotalSeconds;
                this._Swatch.Reset( );
                this._Swatch.Start( );
                this.Host.Frame( ts );
            }
            catch ( EndGameException )
            {
                // nothing to do
            }
        }

        private static MainWindow CreateInstance( Size size, bool fullScreen )
        {
            if ( MainWindow._Instance != null )
                throw new( "Game instance is already created!" );

            return new( size, fullScreen );
        }

        private static void DumpError( Exception ex )
        {
            try
            {
                var fs = new FileStream( MainWindow.DumpFilePath, FileMode.Append, FileAccess.Write, FileShare.Read );
                using ( var writer = new StreamWriter( fs ) )
                {
                    writer.WriteLine( );

                    var ex1 = ex;
                    while ( ex1 != null )
                    {
                        writer.WriteLine( "[" + DateTime.Now.ToString( ) + "] Unhandled exception:" );
                        writer.WriteLine( ex1.Message );
                        writer.WriteLine( );
                        writer.WriteLine( "Stack trace:" );
                        writer.WriteLine( ex1.StackTrace );
                        writer.WriteLine( );

                        ex1 = ex1.InnerException;
                    }
                }
            }
            catch ( Exception )
            {
            }
        }

        private static void SafeShutdown( )
        {
            try
            {
                MainWindow.Instance.Dispose( );
            }
            catch ( Exception ex )
            {
                MainWindow.DumpError( ex );

                if ( Debugger.IsAttached )
                    throw new( "Exception in SafeShutdown()!", ex );
            }
        }

        [STAThread]
        private static int Main( string[] args )
        {
            if ( File.Exists( MainWindow.DumpFilePath ) )
                File.Delete( MainWindow.DumpFilePath );

            var parms = new QuakeParameters( );

            parms.basedir = AppDomain.CurrentDomain.BaseDirectory; //Application.StartupPath;

            var args2 = new string[args.Length + 1];
            args2[0] = string.Empty;
            args.CopyTo( args2, 1 );

            MainWindow.Common = new( );
            MainWindow.Common.InitArgv( args2 );

            MainWindow.Input = new( );

            parms.argv = new string[CommandLine.Argc];
            CommandLine.Args.CopyTo( parms.argv, 0 );

            if ( CommandLine.HasParam( "-dedicated" ) )
                throw new QuakeException( "Dedicated server mode not supported!" );

            var size = new Size( 1280, 720 );

            using ( var form = MainWindow.CreateInstance( size, false ) )
            {
                form.Host.Console.DPrint( "Host.Init\n" );
                form.Host.Initialise( parms );
                MainWindow.Instance.CursorVisible = false; //Hides mouse cursor during main menu on start up
                form.Run( );
            }
            // host.Shutdown();
#if !DEBUG
            }
            catch (QuakeSystemError se)
            {
                HandleException(se);
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
#endif
            return 0; // all Ok
        }

        private void Mouse_WheelChanged( object sender, MouseWheelEventArgs e )
        {
            if ( e.Delta > 0 )
            {
                MainWindow.Instance.Host.Keyboard.Event( KeysDef.K_MWHEELUP, true );
                MainWindow.Instance.Host.Keyboard.Event( KeysDef.K_MWHEELUP, false );
            }
            else
            {
                MainWindow.Instance.Host.Keyboard.Event( KeysDef.K_MWHEELDOWN, true );
                MainWindow.Instance.Host.Keyboard.Event( KeysDef.K_MWHEELDOWN, false );
            }
        }

        private void Mouse_ButtonEvent( object sender, MouseButtonEventArgs e )
        {
            this._MouseBtnState = 0;

            if ( e.Button == MouseButton.Left && e.IsPressed )
                this._MouseBtnState |= 1;

            if ( e.Button == MouseButton.Right && e.IsPressed )
                this._MouseBtnState |= 2;

            if ( e.Button == MouseButton.Middle && e.IsPressed )
                this._MouseBtnState |= 4;

            MainWindow.Input.MouseEvent(this._MouseBtnState );
        }

        private void Mouse_Move( object sender, EventArgs e )
        {
            MainWindow.Input.MouseEvent(this._MouseBtnState );
        }

        private int MapKey( Key srcKey )
        {
            var key = ( int ) srcKey;

            if ( key >= KeysDef.KeyTable.Length || key < 0 )
                return 0;

            if ( KeysDef.KeyTable[key] == 0 )
                this.Host.Console.DPrint( "key 0x{0:X} has no translation\n", key );

            return KeysDef.KeyTable[key];
        }

        private void Keyboard_KeyUp( object sender, KeyboardKeyEventArgs e )
        {
            this.Host.Keyboard.Event(this.MapKey( e.Key ), false );
        }

        private void Keyboard_KeyDown( object sender, KeyboardKeyEventArgs e )
        {
            this.Host.Keyboard.Event(this.MapKey( e.Key ), true );
        }

        private MainWindow( Size size, bool isFullScreen )
        : base( "SharpQuakeEvolved", size, isFullScreen )
        {
            MainWindow._Instance = new( this );
            this._Swatch = new( );
            this.VSync = VSyncMode.One;
            //Icon = Icon.ExtractAssociatedIcon( AppDomain.CurrentDomain.FriendlyName ); //Application.ExecutablePath

            this.KeyDown += new EventHandler<KeyboardKeyEventArgs>(this.Keyboard_KeyDown );
            this.KeyUp += new EventHandler<KeyboardKeyEventArgs>(this.Keyboard_KeyUp );

            this.MouseMove += new EventHandler<EventArgs>(this.Mouse_Move );
            this.MouseDown += new EventHandler<MouseButtonEventArgs>(this.Mouse_ButtonEvent );
            this.MouseUp += new EventHandler<MouseButtonEventArgs>(this.Mouse_ButtonEvent );
            this.MouseWheel += new EventHandler<MouseWheelEventArgs>(this.Mouse_WheelChanged );

            this.Host = new( this );
        }

        public override void Dispose( )
        {
            this.IsDisposing = true;

            this.Host.Dispose( );

            base.Dispose( );
		}

		/// <summary>
		/// Sys_SendKeyEventsa
		/// </summary>
		public void SendKeyEvents( )
		{
            this.Host.Screen.SkipUpdate = false;
            this.ProcessEvents( );
		}

		/// <summary>
		/// Sys_Quit
		/// </summary>
		public void Quit( )
		{
            this.ConfirmExit = false;
            this.Exit( );
            this.Dispose( );

			// Temp fix
			Environment.Exit( 0 );
		}
	}
}
