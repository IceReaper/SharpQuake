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



// chase.c -- chase camera code

namespace SharpQuake.Rendering
{
    using Engine.Host;
    using Framework.Mathematics;
    using Framework.World;
    using System;
    using System.Numerics;

    /// <summary>
    /// Chase_functions
    /// </summary>
    public class ChaseView
    {
        /// <summary>
        /// chase_active.value != 0
        /// </summary>
        public bool IsActive => this.Host.Cvars.Active.Get<bool>( );

        private Vector3 _Dest;

        // Instances
        public Host Host
        {
            get;
            private set;
        }

        public ChaseView( Host host )
        {
            this.Host = host;
        }

        // Chase_Init
        public void Initialise()
        {
            if(this.Host.Cvars.Back == null )
            {
                this.Host.Cvars.Back = this.Host.CVars.Add( "chase_back", 100f );
                this.Host.Cvars.Up = this.Host.CVars.Add( "chase_up", 16f );
                this.Host.Cvars.Right = this.Host.CVars.Add( "chase_right", 0f );
                this.Host.Cvars.Active = this.Host.CVars.Add( "chase_active", false );
            }
        }

        // Chase_Reset
        public void Reset()
        {
            // for respawning and teleporting
            //	start position 12 units behind head
        }

        // Chase_Update
        public void Update()
        {
            // if can't see player, reset
            Vector3 forward, up, right;
            MathLib.AngleVectors( ref this.Host.Client.cl.viewangles, out forward, out right, out up );

            // calc exact destination
            this._Dest = this.Host.RenderContext.RefDef.vieworg - forward * this.Host.Cvars.Back.Get<float>( ) - right * this.Host.Cvars.Right.Get<float>( );
            this._Dest.Z = this.Host.RenderContext.RefDef.vieworg.Z + this.Host.Cvars.Up.Get<float>( );

            // find the spot the player is looking at
            var dest = this.Host.RenderContext.RefDef.vieworg + forward * 4096;

            Vector3 stop;
            this.TraceLine( ref this.Host.RenderContext.RefDef.vieworg, ref dest, out stop );

            // calculate pitch to look at the same spot from camera
            stop -= this.Host.RenderContext.RefDef.vieworg;
            float dist;
            dist = Vector3.Dot( stop, forward );
            if( dist < 1 )
                dist = 1;

            this.Host.RenderContext.RefDef.viewangles.X = ( float ) ( -Math.Atan( stop.Z / dist ) / Math.PI * 180.0 );
            //r_refdef.viewangles[PITCH] = -atan(stop[2] / dist) / M_PI * 180;

            // move towards destination
            this.Host.RenderContext.RefDef.vieworg = this._Dest; //VectorCopy(chase_dest, r_refdef.vieworg);
        }

        private void TraceLine( ref Vector3 start, ref Vector3 end, out Vector3 impact )
        {
            var trace = new Trace_t();

            this.Host.Server.RecursiveHullCheck(this.Host.Client.cl.worldmodel.Hulls[0], 0, 0, 1, ref start, ref end, trace );

            impact = trace.endpos; // VectorCopy(trace.endpos, impact);
        }
    }
}
