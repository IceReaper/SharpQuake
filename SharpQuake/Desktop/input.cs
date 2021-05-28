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



// input.h -- external (non-keyboard) input devices

namespace SharpQuake.Desktop
{
    using Engine.Host;
    using Framework.Definitions;
    using Framework.Networking.Client;
    using Networking.Client;
    using OpenTK.Mathematics;
    using System.Drawing;
    using Vector2 = System.Numerics.Vector2;

    /// <summary>
    /// In_functions
    /// </summary>
    public class Input
    {
        public bool IsMouseActive => this._IsMouseActive;

        public Point WindowCenter
        {
            get
            {
                var bounds = MainWindow.Instance.Bounds;
                var p = bounds.Location;
                p.Offset( bounds.Width / 2, bounds.Height / 2 );
                return p;
            }
        }

        private Vector2 _OldMouse; // old_mouse_x, old_mouse_y
        private Vector2 _Mouse; // mouse_x, mouse_y
        private Vector2 _MouseAccum; // mx_accum, my_accum
        private bool _IsMouseActive; // mouseactive
        private int _MouseButtons; // mouse_buttons
        private int _MouseOldButtonState; // mouse_oldbuttonstate
        private bool _MouseActivateToggle; // mouseactivatetoggle
        private bool _MouseShowToggle = true; // mouseshowtoggle

        // Instances
        private static Host Host
        {
            get;
            set;
        }

        // IN_Init
        public void Initialise( Host host )
        {
            Input.Host = host;

            if ( Input.Host.Cvars.MouseFilter == null )
                Input.Host.Cvars.MouseFilter = Input.Host.CVars.Add( "m_filter", false );

            this._IsMouseActive = Input.Host.MainWindow.IsMouseActive;

            if (this._IsMouseActive )
                this._MouseButtons = 3; //??? TODO: properly upgrade this to 3.0.1
        }

        /// <summary>
        /// IN_Shutdown
        /// </summary>
        public void Shutdown( )
        {
            this.DeactivateMouse( );
            this.ShowMouse( );
        }

        // IN_Commands
        // oportunity for devices to stick commands on the script buffer
        public void Commands( )
        {
            // joystick not supported
        }

        /// <summary>
        /// IN_ActivateMouse
        /// </summary>
        public void ActivateMouse( )
        {
            this._MouseActivateToggle = true;

            if ( Input.Host.MainWindow.IsMouseActive )
            {
                //if (mouseparmsvalid)
                //    restore_spi = SystemParametersInfo (SPI_SETMOUSE, 0, newmouseparms, 0);

                //Cursor.Position = Input.WindowCenter;
                Input.Host.MainWindow.SetMousePosition(this.WindowCenter.X, this.WindowCenter.Y );


                //SetCapture(mainwindow);

                //Cursor.Clip = MainWindow.Instance.Bounds;

                this._IsMouseActive = true;
            }
        }

        /// <summary>
        /// IN_DeactivateMouse
        /// </summary>
        public void DeactivateMouse( )
        {
            this._MouseActivateToggle = false;

            //Cursor.Clip = Screen.PrimaryScreen.Bounds;

            this._IsMouseActive = false;
        }

        /// <summary>
        /// IN_HideMouse
        /// </summary>
        public void HideMouse( )
        {
            if (this._MouseShowToggle )
            {
                //Cursor.Hide();
                this._MouseShowToggle = false;
            }
        }

        /// <summary>
        /// IN_ShowMouse
        /// </summary>
        public void ShowMouse( )
        {
            if ( !this._MouseShowToggle )
            {
                if ( !MainWindow.IsFullscreen )
                {
                    //Cursor.Show();
                }

                this._MouseShowToggle = true;
            }
        }

        // IN_Move
        // add additional movement on top of the keyboard move cmd
        public void Move( usercmd_t cmd )
        {
            if ( !MainWindow.Instance.Focused )
                return;

            if ( MainWindow.Instance.IsMinimised )
                return;

            this.MouseMove( cmd );
        }

        // IN_ClearStates
        // restores all button and position states to defaults
        public void ClearStates( )
        {
            if (this._IsMouseActive )
            {
                this._MouseAccum = Vector2.Zero;
                this._MouseOldButtonState = 0;
            }
        }

        /// <summary>
        /// IN_MouseEvent
        /// </summary>
        public void MouseEvent( int mstate )
        {
            if (this._IsMouseActive )
            {
                // perform button actions
                for ( var i = 0; i < this._MouseButtons; i++ )
                {
                    if ( ( mstate & ( 1 << i ) ) != 0 && (this._MouseOldButtonState & ( 1 << i ) ) == 0 )
                        Input.Host.Keyboard.Event( KeysDef.K_MOUSE1 + i, true );

                    if ( ( mstate & ( 1 << i ) ) == 0 && (this._MouseOldButtonState & ( 1 << i ) ) != 0 )
                        Input.Host.Keyboard.Event( KeysDef.K_MOUSE1 + i, false );
                }

                this._MouseOldButtonState = mstate;
            }
        }

        /// <summary>
        /// IN_MouseMove
        /// </summary>
        private void MouseMove( usercmd_t cmd )
        {
            if ( !this._IsMouseActive )
                return;

            var current_pos = Input.Host.MainWindow.GetMousePosition( ); //Cursor.Position;
            var window_center = this.WindowCenter;

            var mx = ( int ) ( current_pos.X - window_center.X + this._MouseAccum.X );
            var my = ( int ) ( current_pos.Y - window_center.Y + this._MouseAccum.Y );
            this._MouseAccum.X = 0;
            this._MouseAccum.Y = 0;

            if ( Input.Host.Cvars.MouseFilter.Get<bool>( ) )
            {
                this._Mouse.X = ( mx + this._OldMouse.X ) * 0.5f;
                this._Mouse.Y = ( my + this._OldMouse.Y ) * 0.5f;
            }
            else
            {
                this._Mouse.X = mx;
                this._Mouse.Y = my;
            }

            this._OldMouse.X = mx;
            this._OldMouse.Y = my;

            this._Mouse *= Input.Host.Client.Sensitivity;

            // add mouse X/Y movement to cmd
            if ( client_input.StrafeBtn.IsDown || ( Input.Host.Client.LookStrafe && client_input.MLookBtn.IsDown ) )
                cmd.sidemove += Input.Host.Client.MSide * this._Mouse.X;
            else
                Input.Host.Client.cl.viewangles.Y -= Input.Host.Client.MYaw * this._Mouse.X;

            Input.Host.View.StopPitchDrift( );

            Input.Host.Client.cl.viewangles.X += Input.Host.Client.MPitch * this._Mouse.Y;

            // modernized to always use mouse look
            Input.Host.Client.cl.viewangles.X = MathHelper.Clamp( Input.Host.Client.cl.viewangles.X, -70, 80 );

            // if the mouse has moved, force it to the center, so there's room to move
            if ( mx != 0 || my != 0 )
            {
                //Cursor.Position = window_center;
                Input.Host.MainWindow.SetMousePosition( window_center.X, window_center.Y );
            }
        }
    }
}
