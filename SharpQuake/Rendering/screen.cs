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



// screen.h
// gl_screen.c

namespace SharpQuake.Rendering
{
    using Desktop;
    using Engine.Host;
    using Framework.Definitions;
    using Framework.Engine;
    using Framework.IO;
    using Framework.IO.Input;
    using Framework.Rendering;
    using Networking.Client;
    using Renderer;
    using Renderer.Textures;
    using System;
    using System.Drawing;

    /// <summary>
    /// SCR_functions
    /// </summary>
    public partial class Scr
    {
        public VidDef vid => this._VidDef;

        public ClientVariable ViewSize => this.Host.Cvars.ViewSize;

        public float ConCurrent => this._ConCurrent;

        public bool CopyEverithing
        {
            get => this._CopyEverything;
            set => this._CopyEverything = value;
        }

        public bool IsDisabledForLoading;
        public bool BlockDrawing
        {
            get => this.Host.Video.Device.BlockDrawing;
            set => this.Host.Video.Device.BlockDrawing = value;
        }

        public bool SkipUpdate
        {
            get => this.Host.Video.Device.SkipUpdate;
            set => this.Host.Video.Device.SkipUpdate = value;
        }

        // scr_skipupdate
        public bool FullSbarDraw;

        // fullsbardraw = false
        public bool IsPermedia;

        // only the refresh window will be updated unless these variables are flagged
        public bool CopyTop;

        public int ClearNotify;
        public int glX;
        public int glY;
        public int glWidth;
        public int glHeight;
        public float CenterTimeOff;
        public int FullUpdate;
        private VidDef _VidDef = new( );	// viddef_t vid (global video state)
        private VRect _VRect; // scr_vrect

        // scr_disabled_for_loading
        private bool _DrawLoading; // scr_drawloading

        private double _DisabledTime; // float scr_disabled_time

        // qboolean block_drawing
        private bool _DrawDialog; // scr_drawdialog

        // isPermedia
        private bool _IsInitialized;

        private bool _InUpdate;
        private BasePicture Ram;
        private BasePicture Net;
        private BasePicture Turtle;
        private int _TurtleCount; // count from SCR_DrawTurtle()
        private bool _CopyEverything;

        private float _ConCurrent; // scr_con_current
        private float _ConLines;		// lines of console to display
        private int _ClearConsole; // clearconsole
                                     // clearnotify

        private float _OldScreenSize; // float oldscreensize
        private float _OldFov; // float oldfov
        private int _CenterLines; // scr_center_lines
        private int _EraseLines; // scr_erase_lines

        //int _EraseCenter; // scr_erase_center
        private float _CenterTimeStart; // scr_centertime_start	// for slow victory printing

        // scr_centertime_off
        private string _CenterString; // char	scr_centerstring[1024]
        
        private string _NotifyString; // scr_notifystring
        private bool _IsMouseWindowed; // windowed_mouse (don't confuse with _windowed_mouse cvar)
                                                 // scr_fullupdate    set to 0 to force full redraw
                                                 // CHANGE
        private Host Host
        {
            get;
            set;
        }

        public Scr( Host host )
        {
            this.Host = host;
        }

        // SCR_Init
        public void Initialise( )
        {
            if (this.Host.Cvars.ViewSize == null )
            {
                this.Host.Cvars.ViewSize = this.Host.CVars.Add( "viewsize", 100f, ClientVariableFlags.Archive );
                this.Host.Cvars.Fov = this.Host.CVars.Add( "fov", 90f );	// 10 - 170
                this.Host.Cvars.ConSpeed = this.Host.CVars.Add( "scr_conspeed", 3000 );
                this.Host.Cvars.CenterTime = this.Host.CVars.Add( "scr_centertime", 2 );
                this.Host.Cvars.ShowRam = this.Host.CVars.Add( "showram", true );
                this.Host.Cvars.ShowTurtle = this.Host.CVars.Add( "showturtle", false );
                this.Host.Cvars.ShowPause = this.Host.CVars.Add( "showpause", true );
                this.Host.Cvars.PrintSpeed = this.Host.CVars.Add( "scr_printspeed", 8 );
                this.Host.Cvars.glTripleBuffer = this.Host.CVars.Add( "gl_triplebuffer", 1, ClientVariableFlags.Archive );
            }

            //
            // register our commands
            //
            this.Host.Commands.Add( "screenshot", this.ScreenShot_f );
            this.Host.Commands.Add( "sizeup", this.SizeUp_f );
            this.Host.Commands.Add( "sizedown", this.SizeDown_f );

            this.Ram = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, "ram", "GL_LINEAR" );
            this.Net = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, "net", "GL_LINEAR" );
            this.Turtle = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, "turtle", "GL_LINEAR" );

            if ( CommandLine.HasParam( "-fullsbar" ) )
                this.FullSbarDraw = true;

            this._IsInitialized = true;
        }

        // void SCR_UpdateScreen (void);
        // This is called every frame, and can also be called explicitly to flush
        // text to the screen.
        //
        // WARNING: be very careful calling this from elsewhere, because the refresh
        // needs almost the entire 256k of stack space!
        public void UpdateScreen( )
        {
            if (this.BlockDrawing || !this._IsInitialized || this._InUpdate )
                return;

            this._InUpdate = true;
            try
            {
                if ( MainWindow.Instance != null && !MainWindow.Instance.IsDisposing )
                {
                    if ( MainWindow.Instance.VSync == VSyncMode.One != this.Host.Video.Wait )
                        MainWindow.Instance.VSync = this.Host.Video.Wait ? VSyncMode.One : VSyncMode.None;
                }

                this._VidDef.numpages = 2 + ( int )this.Host.Cvars.glTripleBuffer.Get<int>( );

                this.CopyTop = false;
                this._CopyEverything = false;

                if (this.IsDisabledForLoading )
                {
                    if ( this.Host.RealTime - this._DisabledTime > 60 )
                    {
                        this.IsDisabledForLoading = false;
                        this.Host.Console.Print( "Load failed.\n" );
                    }
                    else
                        return;
                }

                if ( !this.Host.Console.IsInitialized )
                    return;	// not initialized yet

                this.BeginRendering( );

                //
                // determine size of refresh window
                //
                if (this._OldFov != this.Host.Cvars.Fov.Get<float>( ) )
                {
                    this._OldFov = this.Host.Cvars.Fov.Get<float>( );
                    this._VidDef.recalc_refdef = true;
                }

                if (this._OldScreenSize != this.Host.Cvars.ViewSize.Get<float>( ) )
                {
                    this._OldScreenSize = this.Host.Cvars.ViewSize.Get<float>( );
                    this._VidDef.recalc_refdef = true;
                }

                if (this._VidDef.recalc_refdef )
                    this.CalcRefdef( );

                //
                // do 3D refresh drawing, and then update the screen
                //
                this.SetUpToDrawConsole( );

                this.Host.View.RenderView( );

                this.Host.Video.Device.Begin2DScene( );
                //Set2D();

                //
                // draw any areas not covered by the refresh
                //
                this.Host.Screen.TileClear( );

                if (this._DrawDialog )
                {
                    this.Host.Hud.Draw( );
                    this.Host.DrawingContext.FadeScreen( );
                    this.DrawNotifyString( );
                    this._CopyEverything = true;
                }
                else if (this._DrawLoading )
                {
                    this.DrawLoading( );
                    this.Host.Hud.Draw( );
                }
                else if (this.Host.Client.cl.intermission == 1 && this.Host.Keyboard.Destination == KeyDestination.key_game )
                    this.Host.Hud.IntermissionOverlay( );
                else if (this.Host.Client.cl.intermission == 2 && this.Host.Keyboard.Destination == KeyDestination.key_game )
                {
                    this.Host.Hud.FinaleOverlay( );
                    this.CheckDrawCenterString( );
                }
                else
                {
                    if (this.Host.View.Crosshair > 0 )
                        this.Host.DrawingContext.DrawCharacter(this._VRect.x + this._VRect.width / 2, this._VRect.y + this._VRect.height / 2, '+' );

                    this.DrawRam( );
                    this.DrawNet( );
                    this.DrawTurtle( );
                    this.DrawPause( );
                    this.CheckDrawCenterString( );
                    this.Host.Hud.Draw( );
                    this.DrawConsole( );
                    this.Host.Menu.Draw( );
                }

                if (this.Host.ShowFPS )
                {
                    if ( DateTime.Now.Subtract(this.Host.LastFPSUpdate ).TotalSeconds >= 1 )
                    {
                        this.Host.FPS = this.Host.FPSCounter;
                        this.Host.FPSCounter = 0;
                        this.Host.LastFPSUpdate = DateTime.Now;
                    }

                    this.Host.FPSCounter++;

                    this.Host.DrawingContext.DrawString(this.Host.Screen.vid.width - 16 - 10, 10, $"{this.Host.FPS}", Color.Yellow );
                }

                this.Host.Video.Device.End2DScene( );

                this.Host.View.UpdatePalette( );
                this.EndRendering( );
            }
            finally
            {
                this._InUpdate = false;
            }
        }

        /// <summary>
        /// GL_EndRendering
        /// </summary>
        public void EndRendering( )
        {
            if ( MainWindow.Instance == null || MainWindow.Instance.IsDisposing )
                return;

            var form = MainWindow.Instance;
            if ( form == null )
                return;

            this.Host.Video?.Device?.EndScene( );

            //if( !SkipUpdate || BlockDrawing )
            //    form.SwapBuffers();

            // handle the mouse state
            if ( !this.Host.Video.WindowedMouse )
            {
                if (this._IsMouseWindowed )
                {
                    MainWindow.Input.DeactivateMouse( );
                    MainWindow.Input.ShowMouse( );
                    this._IsMouseWindowed = false;
                }
            }
            else
            {
                this._IsMouseWindowed = true;
                if (this.Host.Keyboard.Destination == KeyDestination.key_game && !MainWindow.Input.IsMouseActive && this.Host.Client.cls.state != cactive_t.ca_disconnected )// && ActiveApp)
                {
                    MainWindow.Input.ActivateMouse( );
                    MainWindow.Input.HideMouse( );
                }
                else if ( MainWindow.Input.IsMouseActive && this.Host.Keyboard.Destination != KeyDestination.key_game )
                {
                    MainWindow.Input.DeactivateMouse( );
                    MainWindow.Input.ShowMouse( );
                }
            }

            if (this.FullSbarDraw )
                this.Host.Hud.Changed( );
        }

        // SCR_CenterPrint
        //
        // Called for important messages that should stay in the center of the screen
        // for a few moments
        public void CenterPrint( string str )
        {
            this._CenterString = str;
            this.CenterTimeOff = this.Host.Cvars.CenterTime.Get<int>( );
            this._CenterTimeStart = ( float )this.Host.Client.cl.time;

            // count the number of lines for centering
            this._CenterLines = 1;
            foreach ( var c in this._CenterString )
            {
                if ( c == '\n' )
                    this._CenterLines++;
            }
        }

        /// <summary>
        /// SCR_EndLoadingPlaque
        /// </summary>
        public void EndLoadingPlaque( )
        {
            this.Host.Screen.IsDisabledForLoading = false;
            this.Host.Screen.FullUpdate = 0;
            this.Host.Console.ClearNotify( );
        }

        /// <summary>
        /// SCR_BeginLoadingPlaque
        /// </summary>
        public void BeginLoadingPlaque( )
        {
            this.Host.Sound.StopAllSounds( true );

            if (this.Host.Client.cls.state != cactive_t.ca_connected )
                return;
            if (this.Host.Client.cls.signon != ClientDef.SIGNONS )
                return;

            // redraw with no console and the loading plaque
            this.Host.Console.ClearNotify( );
            this.CenterTimeOff = 0;
            this._ConCurrent = 0;

            this._DrawLoading = true;
            this.Host.Screen.FullUpdate = 0;
            this.Host.Hud.Changed( );
            this.UpdateScreen( );
            this._DrawLoading = false;

            this.Host.Screen.IsDisabledForLoading = true;
            this._DisabledTime = this.Host.RealTime;
            this.Host.Screen.FullUpdate = 0;
        }

        /// <summary>
        /// SCR_ModalMessage
        /// Displays a text string in the center of the screen and waits for a Y or N keypress.
        /// </summary>
        public bool ModalMessage( string text )
        {
            if (this.Host.Client.cls.state == cactive_t.ca_dedicated )
                return true;

            this._NotifyString = text;

            // draw a fresh screen
            this.Host.Screen.FullUpdate = 0;
            this._DrawDialog = true;
            this.UpdateScreen( );
            this._DrawDialog = false;

            this.Host.Sound.ClearBuffer( );		// so dma doesn't loop current sound

            do
            {
                this.Host.Keyboard.KeyCount = -1;        // wait for a key down and up
                this.Host.MainWindow.SendKeyEvents( );
            } while (this.Host.Keyboard.LastPress != 'y' && this.Host.Keyboard.LastPress != 'n' && this.Host.Keyboard.LastPress != KeysDef.K_ESCAPE );

            this.Host.Screen.FullUpdate = 0;
            this.UpdateScreen( );

            return this.Host.Keyboard.LastPress == 'y';
        }

        // SCR_SizeUp_f
        //
        // Keybinding command
        private void SizeUp_f( CommandMessage msg )
        {
            this.Host.CVars.Set( "viewsize", this.Host.Cvars.ViewSize.Get<float>( ) + 10 );
            this._VidDef.recalc_refdef = true;
        }

        // SCR_SizeDown_f
        //
        // Keybinding command
        private void SizeDown_f( CommandMessage msg )
        {
            this.Host.CVars.Set( "viewsize", this.Host.Cvars.ViewSize.Get<float>( ) - 10 );
            this._VidDef.recalc_refdef = true;
        }

        // SCR_ScreenShot_f
        private void ScreenShot_f( CommandMessage msg )
        {
            // Screenshot functionality is removed, as any os supports this anyway.
            // Also using a dependency only for this feature is not a good idea.
        }

        /// <summary>
        /// GL_BeginRendering
        /// </summary>
        private void BeginRendering( )
        {
            if ( MainWindow.Instance == null || MainWindow.Instance.IsDisposing )
                return;

            this.glX = 0;
            this.glY = 0;
            this.glWidth = 0;
            this.glHeight = 0;

            var window = MainWindow.Instance;
            if ( window != null )
            {
                var size = window.ClientSize;
                this.glWidth = size.Width;
                this.glHeight = size.Height;
            }

            this.Host.Video?.Device?.BeginScene( );
        }

        // SCR_CalcRefdef
        //
        // Must be called whenever vid changes
        // Internal use only
        private void CalcRefdef( )
        {
            this.Host.Screen.FullUpdate = 0; // force a background redraw
            this._VidDef.recalc_refdef = false;

            // force the status bar to redraw
            this.Host.Hud.Changed( );

            // bound viewsize
            if (this.Host.Cvars.ViewSize.Get<float>( ) < 30 )
                this.Host.CVars.Set( "viewsize", 30f );
            if (this.Host.Cvars.ViewSize.Get<float>( ) > 120 )
                this.Host.CVars.Set( "viewsize", 120f );

            // bound field of view
            if (this.Host.Cvars.Fov.Get<float>( ) < 10 )
                this.Host.CVars.Set( "fov", 10f );
            if (this.Host.Cvars.Fov.Get<float>( ) > 170 )
                this.Host.CVars.Set( "fov", 170f );

            // intermission is always full screen
            float size;
            if (this.Host.Client.cl.intermission > 0 )
                size = 120;
            else
                size = this.Host.Cvars.ViewSize.Get<float>( );

            if ( size >= 120 )
                this.Host.Hud.Lines = 0; // no status bar at all
            else if ( size >= 110 )
                this.Host.Hud.Lines = 24; // no inventory
            else
                this.Host.Hud.Lines = 24 + 16 + 8;

            var full = false;
            if (this.Host.Cvars.ViewSize.Get<float>( ) >= 100.0 )
            {
                full = true;
                size = 100.0f;
            }
            else
                size = this.Host.Cvars.ViewSize.Get<float>( );

            if (this.Host.Client.cl.intermission > 0 )
            {
                full = true;
                size = 100;
                this.Host.Hud.Lines = 0;
            }
            size /= 100.0f;

            var h = this._VidDef.height - this.Host.Hud.Lines;

            var rdef = this.Host.RenderContext.RefDef;
            rdef.vrect.width = ( int ) (this._VidDef.width * size );
            if ( rdef.vrect.width < 96 )
            {
                size = 96.0f / rdef.vrect.width;
                rdef.vrect.width = 96;  // min for icons
            }

            rdef.vrect.height = ( int ) (this._VidDef.height * size );
            if ( rdef.vrect.height > this._VidDef.height - this.Host.Hud.Lines )
                rdef.vrect.height = this._VidDef.height - this.Host.Hud.Lines;
            if ( rdef.vrect.height > this._VidDef.height )
                rdef.vrect.height = this._VidDef.height;
            rdef.vrect.x = (this._VidDef.width - rdef.vrect.width ) / 2;
            if ( full )
                rdef.vrect.y = 0;
            else
                rdef.vrect.y = ( h - rdef.vrect.height ) / 2;

            rdef.fov_x = this.Host.Cvars.Fov.Get<float>( );
            rdef.fov_y = this.CalcFov( rdef.fov_x, rdef.vrect.width, rdef.vrect.height );

            this._VRect = rdef.vrect;
        }

        // CalcFov
        private float CalcFov( float fov_x, float width, float height )
        {
            if ( fov_x < 1 || fov_x > 179 )
                Utilities.Error( "Bad fov: {0}", fov_x );

            var x = width / Math.Tan( fov_x / 360.0 * Math.PI );
            var a = Math.Atan( height / x );
            a = a * 360.0 / Math.PI;
            return ( float ) a;
        }

        /// <summary>
        /// SCR_SetUpToDrawConsole
        /// </summary>
        private void SetUpToDrawConsole( )
        {
            this.Host.Console.CheckResize( );

            if (this._DrawLoading )
                return;     // never a console with loading plaque

            // decide on the height of the console
            this.Host.Console.ForcedUp = this.Host.Client.cl.worldmodel == null || this.Host.Client.cls.signon != ClientDef.SIGNONS;

            if (this.Host.Console.ForcedUp )
            {
                this._ConLines = this._VidDef.height; // full screen
                this._ConCurrent = this._ConLines;
            }
            else if (this.Host.Keyboard.Destination == KeyDestination.key_console )
                this._ConLines = this._VidDef.height / 2; // half screen
            else
                this._ConLines = 0; // none visible

            if (this._ConLines < this._ConCurrent )
            {
                this._ConCurrent -= ( int ) (this.Host.Cvars.ConSpeed.Get<int>( ) * this.Host.FrameTime );
                if (this._ConLines > this._ConCurrent )
                    this._ConCurrent = this._ConLines;
            }
            else if (this._ConLines > this._ConCurrent )
            {
                this._ConCurrent += ( int ) (this.Host.Cvars.ConSpeed.Get<int>( ) * this.Host.FrameTime );
                if (this._ConLines < this._ConCurrent )
                    this._ConCurrent = this._ConLines;
            }

            if (this._ClearConsole++ < this._VidDef.numpages )
                this.Host.Hud.Changed( );
            else if (this.ClearNotify++ < this._VidDef.numpages )
            {
                //????????????
            }
            else
                this.Host.Console.NotifyLines = 0;
        }

        // SCR_TileClear
        private void TileClear( )
        {
            var rdef = this.Host.RenderContext.RefDef;
            if ( rdef.vrect.x > 0 )
            {
                // left
                this.Host.DrawingContext.TileClear( 0, 0, rdef.vrect.x, this._VidDef.height - this.Host.Hud.Lines );
                // right
                this.Host.DrawingContext.TileClear( rdef.vrect.x + rdef.vrect.width, 0,
                    this._VidDef.width - rdef.vrect.x + rdef.vrect.width,
                    this._VidDef.height - this.Host.Hud.Lines );
            }
            if ( rdef.vrect.y > 0 )
            {
                // top
                this.Host.DrawingContext.TileClear( rdef.vrect.x, 0, rdef.vrect.x + rdef.vrect.width, rdef.vrect.y );
                // bottom
                this.Host.DrawingContext.TileClear( rdef.vrect.x, rdef.vrect.y + rdef.vrect.height,
                    rdef.vrect.width,
                    this._VidDef.height - this.Host.Hud.Lines - ( rdef.vrect.height + rdef.vrect.y ) );
            }
        }

        /// <summary>
        /// SCR_DrawNotifyString
        /// </summary>
        private void DrawNotifyString( )
        {
            var offset = 0;
            var y = ( int ) (this.Host.Screen.vid.height * 0.35 );

            do
            {
                var end = this._NotifyString.IndexOf( '\n', offset );
                if ( end == -1 )
                    end = this._NotifyString.Length;
                if ( end - offset > 40 )
                    end = offset + 40;

                var length = end - offset;
                if ( length > 0 )
                {
                    var x = (this.vid.width - length * 8 ) / 2;
                    for ( var j = 0; j < length; j++, x += 8 )
                        this.Host.DrawingContext.DrawCharacter( x, y, this._NotifyString[offset + j] );

                    y += 8;
                }
                offset = end + 1;
            } while ( offset < this._NotifyString.Length );
        }

        /// <summary>
        /// SCR_DrawLoading
        /// </summary>
        private void DrawLoading( )
        {
            if ( !this._DrawLoading )
                return;

            var pic = this.Host.DrawingContext.CachePic( "gfx/loading.lmp", "GL_LINEAR" );
            this.Host.Video.Device.Graphics.DrawPicture( pic, (this.vid.width - pic.Width ) / 2, (this.vid.height - 48 - pic.Height ) / 2 );
        }

        // SCR_CheckDrawCenterString
        private void CheckDrawCenterString( )
        {
            this.CopyTop = true;
            if (this._CenterLines > this._EraseLines )
                this._EraseLines = this._CenterLines;

            this.CenterTimeOff -= ( float )this.Host.FrameTime;

            if (this.CenterTimeOff <= 0 && this.Host.Client.cl.intermission == 0 )
                return;
            if (this.Host.Keyboard.Destination != KeyDestination.key_game )
                return;

            this.DrawCenterString( );
        }

        // SCR_DrawRam
        private void DrawRam( )
        {
            if ( !this.Host.Cvars.ShowRam.Get<bool>( ) )
                return;

            if ( !this.Host.RenderContext.CacheTrash )
                return;

            this.Host.Video.Device.Graphics.DrawPicture(this.Ram, this._VRect.x + 32, this._VRect.y );
        }

        // SCR_DrawTurtle
        private void DrawTurtle( )
        {
            //int	count;

            if ( !this.Host.Cvars.ShowTurtle.Get<bool>( ) )
                return;

            if (this.Host.FrameTime < 0.1 )
            {
                this._TurtleCount = 0;
                return;
            }

            this._TurtleCount++;
            if (this._TurtleCount < 3 )
                return;

            this.Host.Video.Device.Graphics.DrawPicture(this.Turtle, this._VRect.x, this._VRect.y );
        }

        // SCR_DrawNet
        private void DrawNet( )
        {
            if (this.Host.RealTime - this.Host.Client.cl.last_received_message < 0.3 )
                return;
            if (this.Host.Client.cls.demoplayback )
                return;

            this.Host.Video.Device.Graphics.DrawPicture(this.Net, this._VRect.x + 64, this._VRect.y );
        }

        // DrawPause
        private void DrawPause( )
        {
            if ( !this.Host.Cvars.ShowPause.Get<bool>( ) )	// turn off for screenshots
                return;

            if ( !this.Host.Client.cl.paused )
                return;

            var pic = this.Host.DrawingContext.CachePic( "gfx/pause.lmp", "GL_NEAREST" );
            this.Host.Video.Device.Graphics.DrawPicture( pic, (this.vid.width - pic.Width ) / 2, (this.vid.height - 48 - pic.Height ) / 2 );
        }

        // SCR_DrawConsole
        private void DrawConsole( )
        {
            if (this._ConCurrent > 0 )
            {
                this._CopyEverything = true;
                this.Host.Console.Draw( ( int )this._ConCurrent, true );
                this._ClearConsole = 0;
            }
            else if (this.Host.Keyboard.Destination == KeyDestination.key_game || this.Host.Keyboard.Destination == KeyDestination.key_message )
                this.Host.Console.DrawNotify( );	// only draw notify in game
        }

        // SCR_DrawCenterString
        private void DrawCenterString( )
        {
            int remaining;

            // the finale prints the characters one at a time
            if (this.Host.Client.cl.intermission > 0 )
                remaining = ( int ) (this.Host.Cvars.PrintSpeed.Get<int>( ) * (this.Host.Client.cl.time - this._CenterTimeStart ) );
            else
                remaining = 9999;

            var y = 48;
            if (this._CenterLines <= 4 )
                y = ( int ) (this._VidDef.height * 0.35 );

            var lines = this._CenterString.Split( '\n' );
            for ( var i = 0; i < lines.Length; i++ )
            {
                var line = lines[i].TrimEnd( '\r' );
                var x = (this.vid.width - line.Length * 8 ) / 2;

                for ( var j = 0; j < line.Length; j++, x += 8 )
                {
                    this.Host.DrawingContext.DrawCharacter( x, y, line[j] );
                    if ( remaining-- <= 0 )
                        return;
                }
                y += 8;
            }
        }
    }
}
