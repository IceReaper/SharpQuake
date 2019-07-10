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

using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using SDL2;
using SharpQuake.Framework;
using SharpQuake.Renderer;
using SharpQuake.Renderer.Desktop;
using SharpQuake.Renderer.OpenGL.Desktop;

namespace SharpQuake
{
    public class MainWindow : GLWindow//GameWindow
    {
        public static MainWindow Instance
        {
            get
            {
                return ( MainWindow ) _Instance.Target;
            }
        }

        public static Boolean IsFullscreen
        {
            get
            {
                return Instance.IsFullScreen;
            }
        }

        public Boolean ConfirmExit = true;

        private static String DumpFilePath
        {
            get
            {
                return Path.Combine( AppDomain.CurrentDomain.BaseDirectory, "error.txt" );
            }
        }

        private static Byte[] _KeyTable = new Byte[130]
        {
            0, KeysDef.K_SHIFT, KeysDef.K_SHIFT, KeysDef.K_CTRL, KeysDef.K_CTRL, KeysDef.K_ALT, KeysDef.K_ALT, 0, // 0 - 7
            0, 0, KeysDef.K_F1, KeysDef.K_F2, KeysDef.K_F3, KeysDef.K_F4, KeysDef.K_F5, KeysDef.K_F6, // 8 - 15
            KeysDef.K_F7, KeysDef.K_F8, KeysDef.K_F9, KeysDef.K_F10, KeysDef.K_F11, KeysDef.K_F12, 0, 0, // 16 - 23
            0, 0, 0, 0, 0, 0, 0, 0, // 24 - 31
            0, 0, 0, 0, 0, 0, 0, 0, // 32 - 39
            0, 0, 0, 0, 0, KeysDef.K_UPARROW, KeysDef.K_DOWNARROW, KeysDef.K_LEFTARROW, // 40 - 47
            KeysDef.K_RIGHTARROW, KeysDef.K_ENTER, KeysDef.K_ESCAPE, KeysDef.K_SPACE, KeysDef.K_TAB, KeysDef.K_BACKSPACE, KeysDef.K_INS, KeysDef.K_DEL, // 48 - 55
            KeysDef.K_PGUP, KeysDef.K_PGDN, KeysDef.K_HOME, KeysDef.K_END, 0, 0, 0, KeysDef.K_PAUSE, // 56 - 63
            0, 0, 0, KeysDef.K_INS, KeysDef.K_END, KeysDef.K_DOWNARROW, KeysDef.K_PGDN, KeysDef.K_LEFTARROW, // 64 - 71
            0, KeysDef.K_RIGHTARROW, KeysDef.K_HOME, KeysDef.K_UPARROW, KeysDef.K_PGUP, (Byte)'/', (Byte)'*', (Byte)'-', // 72 - 79
            (Byte)'+', (Byte)'.', KeysDef.K_ENTER, (Byte)'a', (Byte)'b', (Byte)'c', (Byte)'d', (Byte)'e', // 80 - 87
            (Byte)'f', (Byte)'g', (Byte)'h', (Byte)'i', (Byte)'j', (Byte)'k', (Byte)'l', (Byte)'m', // 88 - 95
            (Byte)'n', (Byte)'o', (Byte)'p', (Byte)'q', (Byte)'r', (Byte)'s', (Byte)'t', (Byte)'u', // 96 - 103
            (Byte)'v', (Byte)'w', (Byte)'x', (Byte)'y', (Byte)'z', (Byte)'0', (Byte)'1', (Byte)'2', // 104 - 111
            (Byte)'3', (Byte)'4', (Byte)'5', (Byte)'6', (Byte)'7', (Byte)'8', (Byte)'9', (Byte)'`', // 112 - 119
            (Byte)'-', (Byte)'+', (Byte)'[', (Byte)']', (Byte)';', (Byte)'\'', (Byte)',', (Byte)'.', // 120 - 127
            (Byte)'/', (Byte)'\\' // 128 - 129
        };

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

        private Int32 _MouseBtnState;
        private Stopwatch _Swatch;

        public Boolean IsDisposing
        {
            get;
            private set;
        }

        protected override void OnFocusedChanged( )
        {
            base.OnFocusedChanged( );

            if ( this.Focused )
                Host.Sound.UnblockSound( );
            else
                Host.Sound.BlockSound( );
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

        protected override void OnUpdateFrame( Double time )
        {
            try
            {
                if ( IsMinimised || Host.Screen.BlockDrawing || Host.IsDisposing )
                    Host.Screen.SkipUpdate = true;	// no point in bothering to draw

                _Swatch.Stop( );
                var ts = _Swatch.Elapsed.TotalSeconds;
                _Swatch.Reset( );
                _Swatch.Start( );
                Host.Frame( ts );
            }
            catch ( EndGameException )
            {
                // nothing to do
            }
        }

        private static MainWindow CreateInstance( Size size, Boolean fullScreen )
        {
            if ( _Instance != null )
            {
                throw new Exception( "Game instance is already created!" );
            }
            return new MainWindow( size, fullScreen );
        }

        private static void DumpError( Exception ex )
        {
            try
            {
                var fs = new FileStream( DumpFilePath, FileMode.Append, FileAccess.Write, FileShare.Read );
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
                Instance.Dispose( );
            }
            catch ( Exception ex )
            {
                DumpError( ex );

                if ( Debugger.IsAttached )
                    throw new Exception( "Exception in SafeShutdown()!", ex );
            }
        }

        private static void HandleException( Exception ex )
        {
            DumpError( ex );

            if ( Debugger.IsAttached )
                throw new Exception( "Fatal error!", ex );

            Instance.CursorVisible = true;
            SDL.SDL_ShowSimpleMessageBox( SDL.SDL_MessageBoxFlags.SDL_MESSAGEBOX_ERROR, "Fatal error!", ex.Message, IntPtr.Zero ); //MessageBox.Show(ex.Message);
            SafeShutdown( );
            
        } 
        
        

        [STAThread]
        private static Int32 Main( String[] args )
        {
            if ( File.Exists( DumpFilePath ) )
                File.Delete( DumpFilePath );

            var parms = new QuakeParameters( );

            parms.basedir = AppDomain.CurrentDomain.BaseDirectory; //Application.StartupPath;

            var args2 = new String[args.Length + 1];
            args2[0] = String.Empty;
            args.CopyTo( args2, 1 );

            Common = new Common( );
            Common.InitArgv( args2 );

            Input = new Input( );

            parms.argv = new String[CommandLine.Argc];
            CommandLine.Args.CopyTo( parms.argv, 0 );

            if ( CommandLine.HasParam( "-dedicated" ) )
                throw new QuakeException( "Dedicated server mode not supported!" );

            var size = new Size( 1280, 720 );

            using ( var form = MainWindow.CreateInstance( size, false ) )
            {
                form.Host.Console.DPrint( "Host.Init\n" );
                form.Host.Initialise( parms );
                Instance.CursorVisible = false; //Hides mouse cursor during main menu on start up
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

        private void Mouse_WheelChanged( Object sender, MouseWheelEventArgs e )
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

        private void Mouse_ButtonEvent( Object sender, MouseButtonEventArgs e )
        {
            _MouseBtnState = 0;

            if ( e.Button == MouseButton.Left && e.IsPressed )
                _MouseBtnState |= 1;

            if ( e.Button == MouseButton.Right && e.IsPressed )
                _MouseBtnState |= 2;

            if ( e.Button == MouseButton.Middle && e.IsPressed )
                _MouseBtnState |= 4;

            Input.MouseEvent( _MouseBtnState );
        }

        private void Mouse_Move( Object sender, EventArgs e )
        {
            Input.MouseEvent( _MouseBtnState );
        }

        private Int32 MapKey( Key srcKey )
        {
            var key = ( Int32 ) srcKey;
            key &= 255;

            if ( key >= _KeyTable.Length )
                return 0;

            if ( _KeyTable[key] == 0 )
                MainWindow.Instance.Host.Console.DPrint( "key 0x{0:X} has no translation\n", key );

            return _KeyTable[key];
        }

        private void Keyboard_KeyUp( Object sender, KeyboardKeyEventArgs e )
        {
            MainWindow.Instance.Host.Keyboard.Event( MapKey( e.Key ), false );
        }

        private void Keyboard_KeyDown( Object sender, KeyboardKeyEventArgs e )
        {
            MainWindow.Instance.Host.Keyboard.Event( MapKey( e.Key ), true );
        }

        private MainWindow( Size size, Boolean isFullScreen )
        : base( "SharpQuakeEvolved", size, isFullScreen )
        {
            _Instance = new WeakReference( this );
            _Swatch = new Stopwatch( );
            this.VSync = VSyncMode.One;
            this.Icon = Icon.ExtractAssociatedIcon( AppDomain.CurrentDomain.FriendlyName ); //Application.ExecutablePath

            this.KeyDown += new EventHandler<KeyboardKeyEventArgs>( Keyboard_KeyDown );
            this.KeyUp += new EventHandler<KeyboardKeyEventArgs>( Keyboard_KeyUp );

            this.MouseMove += new EventHandler<EventArgs>( Mouse_Move );
            this.MouseDown += new EventHandler<MouseButtonEventArgs>( Mouse_ButtonEvent );
            this.MouseUp += new EventHandler<MouseButtonEventArgs>( Mouse_ButtonEvent );
            this.MouseWheel += new EventHandler<MouseWheelEventArgs>( Mouse_WheelChanged );

            Host = new Host( this );
        }

        public override void Dispose( )
        {
            IsDisposing = true;

            Host.Dispose( );

            base.Dispose( );
        }
    }
}
