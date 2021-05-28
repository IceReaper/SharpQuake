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



// vid.h -- video driver defs

namespace SharpQuake.Rendering
{
    using Desktop;
    using Engine.Host;
    using Framework.Data;
    using Framework.Engine;
    using Framework.IO;
    using Framework.Mathematics;
    using Renderer;
    using System;

    /// <summary>
	/// Vid_functions
	/// </summary>
	public class Vid
    {
        public ushort[] Table8to16 => this.Device.Palette.Table8to16; //_8to16table;

        public uint[] Table8to24 => this.Device.Palette.Table8to24; //_8to24table;

        public byte[] Table15to8 => this.Device.Palette.Table15to8; //_15to8table;

        public bool glZTrick => this.Host.Cvars.glZTrick.Get<bool>( );

        public bool WindowedMouse => this.Host.Cvars.WindowedMouse.Get<bool>( );

        public bool Wait => this.Host.Cvars.Wait.Get<bool>( );

        public int ModeNum => this.Device.ChosenMode; //_ModeNum;

        public const int VID_CBITS = 6;
        public const int VID_GRADES = 1 << Vid.VID_CBITS;
        public const int VID_ROW_SIZE = 3;
        private const int WARP_WIDTH = 320;
        private const int WARP_HEIGHT = 200;        
       
        // Instances
        private Host Host
        {
            get;
            set;
        }

        public BaseDevice Device => this.Host.MainWindow.Device;

        public Vid( Host host )
        {
            this.Host = host;
        }

        /// <summary>
        /// VID_Init (unsigned char *palette)
        /// Called at startup to set up translation tables, takes 256 8 bit RGB values
        /// the palette data will go away after the call, so it must be copied off if
        /// the video driver will need it again
        /// </summary>
        /// <param name="palette"></param>
        public void Initialise( byte[] palette )
        {
            if (this.Host.Cvars.glZTrick == null )
            {
                this.Host.Cvars.glZTrick = this.Host.CVars.Add( "gl_ztrick", true );
                this.Host.Cvars.Mode = this.Host.CVars.Add( "vid_mode", 0 );
                this.Host.Cvars.DefaultMode = this.Host.CVars.Add( "_vid_default_mode", 0, ClientVariableFlags.Archive );
                this.Host.Cvars.DefaultModeWin = this.Host.CVars.Add( "_vid_default_mode_win", 3, ClientVariableFlags.Archive );
                this.Host.Cvars.Wait = this.Host.CVars.Add( "vid_wait", false );
                this.Host.Cvars.NoPageFlip = this.Host.CVars.Add( "vid_nopageflip", 0, ClientVariableFlags.Archive );
                this.Host.Cvars.WaitOverride = this.Host.CVars.Add( "_vid_wait_override", 0, ClientVariableFlags.Archive );
                this.Host.Cvars.ConfigX = this.Host.CVars.Add( "vid_config_x", 800, ClientVariableFlags.Archive );
                this.Host.Cvars.ConfigY = this.Host.CVars.Add( "vid_config_y", 600, ClientVariableFlags.Archive );
                this.Host.Cvars.StretchBy2 = this.Host.CVars.Add( "vid_stretch_by_2", 1, ClientVariableFlags.Archive );
                this.Host.Cvars.WindowedMouse = this.Host.CVars.Add( "_windowed_mouse", true, ClientVariableFlags.Archive );
            }

            this.Host.Commands.Add( "vid_nummodes", this.NumModes_f );
            this.Host.Commands.Add( "vid_describecurrentmode", this.DescribeCurrentMode_f );
            this.Host.Commands.Add( "vid_describemode", this.DescribeMode_f );
            this.Host.Commands.Add( "vid_describemodes", this.DescribeModes_f );

            this.Device.Initialise( palette );

            this.UpdateConsole( );
            this.UpdateScreen( );

            // Moved from SetMode

            // so Con_Printfs don't mess us up by forcing vid and snd updates
            var temp = this.Host.Screen.IsDisabledForLoading;
            this.Host.Screen.IsDisabledForLoading = true;
            this.Host.CDAudio.Pause( );

            this.Device.SetMode(this.Device.ChosenMode, palette );

            var vid = this.Host.Screen.vid;

            this.UpdateConsole( false );

            vid.width = this.Device.Desc.Width; // vid.conwidth
            vid.height = this.Device.Desc.Height;
            vid.numpages = 2;

            this.Host.CDAudio.Resume( );
            this.Host.Screen.IsDisabledForLoading = temp;

            this.Host.CVars.Set( "vid_mode", this.Device.ChosenMode );

            // fix the leftover Alt from any Alt-Tab or the like that switched us away
            this.ClearAllStates( );

            this.Host.Console.SafePrint( "Video mode {0} initialized.\n", this.Device.GetModeDescription(this.Device.ChosenMode ) );

            vid.recalc_refdef = true;

            if (this.Device.Desc.Renderer.StartsWith( "PowerVR", StringComparison.InvariantCultureIgnoreCase ) )
                this.Host.Screen.FullSbarDraw = true;

            if (this.Device.Desc.Renderer.StartsWith( "Permedia", StringComparison.InvariantCultureIgnoreCase ) )
                this.Host.Screen.IsPermedia = true;

            this.CheckTextureExtensions( );
        }

        private void UpdateScreen()
        {
            this.Host.Screen.vid.maxwarpwidth = Vid.WARP_WIDTH;
            this.Host.Screen.vid.maxwarpheight = Vid.WARP_HEIGHT;
            this.Host.Screen.vid.colormap = this.Host.ColorMap;
            var v = BitConverter.ToInt32(this.Host.ColorMap, 2048 );
            this.Host.Screen.vid.fullbright = 256 - EndianHelper.LittleLong( v );
        }

        private void UpdateConsole( bool isInitialStage = true )
        {
            var vid = this.Host.Screen.vid;

            if ( isInitialStage )
            {
                var i2 = CommandLine.CheckParm( "-conwidth" );

                if ( i2 > 0 )
                    vid.conwidth = MathLib.atoi( CommandLine.Argv( i2 + 1 ) );
                else
                    vid.conwidth = 640;

                vid.conwidth &= 0xfff8; // make it a multiple of eight

                if ( vid.conwidth < 320 )
                    vid.conwidth = 320;

                // pick a conheight that matches with correct aspect
                vid.conheight = vid.conwidth * 3 / 4;

                i2 = CommandLine.CheckParm( "-conheight" );

                if ( i2 > 0 )
                    vid.conheight = MathLib.atoi( CommandLine.Argv( i2 + 1 ) );

                if ( vid.conheight < 200 )
                    vid.conheight = 200;
            }
            else
            {
                if ( vid.conheight > this.Device.Desc.Height )
                    vid.conheight = this.Device.Desc.Height;
                if ( vid.conwidth > this.Device.Desc.Width )
                    vid.conwidth = this.Device.Desc.Width;
            }
        }

        /// <summary>
        /// VID_Shutdown
        /// Called at shutdown
        /// </summary>
        public void Shutdown()
        {
            this.Device.Dispose( );
            //_IsInitialized = false;
        }

        /// <summary>
        /// VID_GetModeDescription
        /// </summary>
        public string GetModeDescription( int mode )
        {
            return this.Device.GetModeDescription( mode );
        }

        /// <summary>
        /// VID_NumModes_f
        /// </summary>
        /// <param name="msg"></param>
        private void NumModes_f( CommandMessage msg )
        {
            var nummodes = this.Device.AvailableModes.Length;

            if( nummodes == 1 )
                this.Host.Console.Print( "{0} video mode is available\n", nummodes );
            else
                this.Host.Console.Print( "{0} video modes are available\n", nummodes );
        }

        /// <summary>
        /// VID_DescribeCurrentMode_f
        /// </summary>
        /// <param name="msg"></param>
        private void DescribeCurrentMode_f( CommandMessage msg )
        {
            this.Host.Console.Print( "{0}\n", this.GetModeDescription(this.Device.ChosenMode ) );
        }

        /// <summary>
        /// VID_DescribeMode_f
        /// </summary>
        /// <param name="msg"></param>
        private void DescribeMode_f( CommandMessage msg )
        {
            var modenum = MathLib.atoi( msg.Parameters[0] );

            this.Host.Console.Print( "{0}\n", this.GetModeDescription( modenum ) );
        }

        /// <summary>
        /// VID_DescribeModes_f
        /// </summary>
        /// <param name="msg"></param>
        private void DescribeModes_f( CommandMessage msg )
        {
            for( var i = 0; i < this.Device.AvailableModes.Length; i++ )
                this.Host.Console.Print( "{0}:{1}\n", i, this.GetModeDescription( i ) );
        }

        /// <summary>
        /// ClearAllStates
        /// </summary>
        private void ClearAllStates()
        {
            // send an up event for each key, to make sure the server clears them all
            for( var i = 0; i < 256; i++ )
                this.Host.Keyboard.Event( i, false );

            this.Host.Keyboard.ClearStates();
            MainWindow.Input.ClearStates();
        }

        /// <summary>
        /// CheckTextureExtensions
        /// </summary>
        private void CheckTextureExtensions()
        {
            const string TEXTURE_EXT_STRING = "GL_EXT_texture_object";

            // check for texture extension
            var texture_ext = this.Device.Desc.Extensions.Contains( TEXTURE_EXT_STRING );
        }
    }
}
