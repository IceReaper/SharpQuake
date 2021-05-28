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



// sv_move.c

namespace SharpQuake.Networking.Server
{
    using Framework.Definitions;
    using Framework.Engine;
    using Framework.IO.BSP.Q1;
    using Framework.Mathematics;
    using Framework.World;
    using System;
    using System.Numerics;

    partial class server
    {
        private const float DI_NODIR = -1;

        /// <summary>
        /// SV_movestep
        /// Called by monster program code.
        /// The move will be adjusted for slopes and stairs, but if the move isn't
        /// possible, no move is done, false is returned, and
        /// pr_global_struct.trace_normal is set to the normal of the blocking wall
        /// </summary>
        public bool MoveStep( MemoryEdict ent, ref Vector3 move, bool relink )
        {
            Trace_t trace;

            // try the move
            var oldorg = ent.v.origin;
            Vector3 neworg;
            MathLib.VectorAdd( ref ent.v.origin, ref move, out neworg );

            // flying monsters don't step up
            if( ( ( int ) ent.v.flags & ( EdictFlags.FL_SWIM | EdictFlags.FL_FLY ) ) != 0 )
            {
                // try one move with vertical motion, then one without
                for( var i = 0; i < 2; i++ )
                {
                    MathLib.VectorAdd( ref ent.v.origin, ref move, out neworg );
                    var enemy = this.ProgToEdict( ent.v.enemy );
                    if( i == 0 && enemy != this.sv.edicts[0] )
                    {
                        var dz = ent.v.origin.Z - enemy.v.origin.Z;
                        if( dz > 40 )
                            neworg.Z -= 8;
                        if( dz < 30 )
                            neworg.Z += 8;
                    }

                    trace = this.Move( ref ent.v.origin, ref ent.v.mins, ref ent.v.maxs, ref neworg, 0, ent );
                    if( trace.fraction == 1 )
                    {
                        if( ( ( int ) ent.v.flags & EdictFlags.FL_SWIM ) != 0 && this.PointContents( ref trace.endpos ) == ( int ) Q1Contents.Empty )
                            return false;	// swim monster left water

                        MathLib.Copy( ref trace.endpos, out ent.v.origin );
                        if( relink )
                            this.LinkEdict( ent, true );
                        return true;
                    }

                    if( enemy == this.sv.edicts[0] )
                        break;
                }

                return false;
            }

            // push down from a step height above the wished position
            neworg.Z += server.STEPSIZE;
            var end = neworg;
            end.Z -= server.STEPSIZE * 2;

            trace = this.Move( ref neworg, ref ent.v.mins, ref ent.v.maxs, ref end, 0, ent );

            if( trace.allsolid )
                return false;

            if( trace.startsolid )
            {
                neworg.Z -= server.STEPSIZE;
                trace = this.Move( ref neworg, ref ent.v.mins, ref ent.v.maxs, ref end, 0, ent );
                if( trace.allsolid || trace.startsolid )
                    return false;
            }
            if( trace.fraction == 1 )
            {
                // if monster had the ground pulled out, go ahead and fall
                if( ( ( int ) ent.v.flags & EdictFlags.FL_PARTIALGROUND ) != 0 )
                {
                    MathLib.VectorAdd( ref ent.v.origin, ref move, out ent.v.origin );
                    if( relink )
                        this.LinkEdict( ent, true );
                    ent.v.flags = ( int ) ent.v.flags & ~EdictFlags.FL_ONGROUND;
                    return true;
                }

                return false;		// walked off an edge
            }

            // check point traces down for dangling corners
            MathLib.Copy( ref trace.endpos, out ent.v.origin );

            if( !this.CheckBottom( ent ) )
            {
                if( ( ( int ) ent.v.flags & EdictFlags.FL_PARTIALGROUND ) != 0 )
                {
                    // entity had floor mostly pulled out from underneath it
                    // and is trying to correct
                    if( relink )
                        this.LinkEdict( ent, true );
                    return true;
                }
                ent.v.origin = oldorg;
                return false;
            }

            if( ( ( int ) ent.v.flags & EdictFlags.FL_PARTIALGROUND ) != 0 )
                ent.v.flags = ( int ) ent.v.flags & ~EdictFlags.FL_PARTIALGROUND;

            ent.v.groundentity = this.EdictToProg( trace.ent );

            // the move is ok
            if( relink )
                this.LinkEdict( ent, true );
            return true;
        }

        /// <summary>
        /// SV_CheckBottom
        /// </summary>
        public bool CheckBottom( MemoryEdict ent )
        {
            Vector3 mins, maxs;
            MathLib.VectorAdd( ref ent.v.origin, ref ent.v.mins, out mins );
            MathLib.VectorAdd( ref ent.v.origin, ref ent.v.maxs, out maxs );

            // if all of the points under the corners are solid world, don't bother
            // with the tougher checks
            // the corners must be within 16 of the midpoint
            Vector3 start;
            start.Z = mins.Z - 1;
            for( var x = 0; x <= 1; x++ )
                for( var y = 0; y <= 1; y++ )
                {
                    start.X = x != 0 ? maxs.X : mins.X;
                    start.Y = y != 0 ? maxs.Y : mins.Y;
                    if(this.PointContents( ref start ) != ( int ) Q1Contents.Solid )
                        goto RealCheck;
                }

            return true;        // we got out easy

RealCheck:

//
// check it for real...
//
            start.Z = mins.Z;

            // the midpoint must be within 16 of the bottom
            start.X = ( mins.X + maxs.X ) * 0.5f;
            start.Y = ( mins.Y + maxs.Y ) * 0.5f;
            var stop = start;
            stop.Z -= 2 * server.STEPSIZE;
            var trace = this.Move( ref start, ref Utilities.ZeroVector, ref Utilities.ZeroVector, ref stop, 1, ent );

            if( trace.fraction == 1.0 )
                return false;

            var mid = trace.endpos.Z;
            var bottom = mid;

            // the corners must be within 16 of the midpoint
            for( var x = 0; x <= 1; x++ )
                for( var y = 0; y <= 1; y++ )
                {
                    start.X = stop.X = x != 0 ? maxs.X : mins.X;
                    start.Y = stop.Y = y != 0 ? maxs.Y : mins.Y;

                    trace = this.Move( ref start, ref Utilities.ZeroVector, ref Utilities.ZeroVector, ref stop, 1, ent );

                    if( trace.fraction != 1.0 && trace.endpos.Z > bottom )
                        bottom = trace.endpos.Z;
                    if( trace.fraction == 1.0 || mid - trace.endpos.Z > server.STEPSIZE )
                        return false;
                }

            return true;
        }

        /// <summary>
        /// SV_MoveToGoal
        /// </summary>
        public void MoveToGoal()
        {
            var ent = this.ProgToEdict(this.Host.Programs.GlobalStruct.self );
            var goal = this.ProgToEdict( ent.v.goalentity );
            var dist = this.Host.ProgramsBuiltIn.GetFloat( ProgramOperatorDef.OFS_PARM0 );

            if( ( ( int ) ent.v.flags & ( EdictFlags.FL_ONGROUND | EdictFlags.FL_FLY | EdictFlags.FL_SWIM ) ) == 0 )
            {
                this.Host.ProgramsBuiltIn.ReturnFloat( 0 );
                return;
            }

            // if the next step hits the enemy, return immediately
            if(this.ProgToEdict( ent.v.enemy ) != this.sv.edicts[0] && this.CloseEnough( ent, goal, dist ) )
                return;

            // bump around...
            if( ( MathLib.Random() & 3 ) == 1 || !this.StepDirection( ent, ent.v.ideal_yaw, dist ) )
                this.NewChaseDir( ent, goal, dist );
        }

        /// <summary>
        /// SV_CloseEnough
        /// </summary>
        private bool CloseEnough( MemoryEdict ent, MemoryEdict goal, float dist )
        {
            if( goal.v.absmin.X > ent.v.absmax.X + dist )
                return false;
            if( goal.v.absmin.Y > ent.v.absmax.Y + dist )
                return false;
            if( goal.v.absmin.Z > ent.v.absmax.Z + dist )
                return false;

            if( goal.v.absmax.X < ent.v.absmin.X - dist )
                return false;
            if( goal.v.absmax.Y < ent.v.absmin.Y - dist )
                return false;
            if( goal.v.absmax.Z < ent.v.absmin.Z - dist )
                return false;

            return true;
        }

        /// <summary>
        /// SV_StepDirection
        /// Turns to the movement direction, and walks the current distance if facing it.
        /// </summary>
        private bool StepDirection( MemoryEdict ent, float yaw, float dist )
        {
            ent.v.ideal_yaw = yaw;
            this.Host.ProgramsBuiltIn.PF_changeyaw();

            yaw = ( float ) ( yaw * Math.PI * 2.0 / 360 );
            Vector3 move;
            move.X = ( float ) Math.Cos( yaw ) * dist;
            move.Y = ( float ) Math.Sin( yaw ) * dist;
            move.Z = 0;

            var oldorigin = ent.v.origin;
            if(this.MoveStep( ent, ref move, false ) )
            {
                var delta = ent.v.angles.Y - ent.v.ideal_yaw;
                if( delta > 45 && delta < 315 )
                {
                    // not turned far enough, so don't take the step
                    ent.v.origin = oldorigin;
                }

                this.LinkEdict( ent, true );
                return true;
            }

            this.LinkEdict( ent, true );

            return false;
        }

        /// <summary>
        /// SV_NewChaseDir
        /// </summary>
        private void NewChaseDir( MemoryEdict actor, MemoryEdict enemy, float dist )
        {
            var olddir = MathLib.AngleMod( ( int ) ( actor.v.ideal_yaw / 45 ) * 45 );
            var turnaround = MathLib.AngleMod( olddir - 180 );

            var deltax = enemy.v.origin.X - actor.v.origin.X;
            var deltay = enemy.v.origin.Y - actor.v.origin.Y;
            Vector3 d;
            if( deltax > 10 )
                d.Y = 0;
            else if( deltax < -10 )
                d.Y = 180;
            else
                d.Y = server.DI_NODIR;
            if( deltay < -10 )
                d.Z = 270;
            else if( deltay > 10 )
                d.Z = 90;
            else
                d.Z = server.DI_NODIR;

            // try direct route
            float tdir;
            if( d.Y != server.DI_NODIR && d.Z != server.DI_NODIR )
            {
                if( d.Y == 0 )
                    tdir = d.Z == 90 ? 45 : 315;
                else
                    tdir = d.Z == 90 ? 135 : 215;

                if( tdir != turnaround && this.StepDirection( actor, tdir, dist ) )
                    return;
            }

            // try other directions
            if( ( MathLib.Random() & 3 & 1 ) != 0 || Math.Abs( deltay ) > Math.Abs( deltax ) )
            {
                tdir = d.Y;
                d.Y = d.Z;
                d.Z = tdir;
            }

            if( d.Y != server.DI_NODIR && d.Y != turnaround && this.StepDirection( actor, d.Y, dist ) )
                return;

            if( d.Z != server.DI_NODIR && d.Z != turnaround && this.StepDirection( actor, d.Z, dist ) )
                return;

            // there is no direct path to the player, so pick another direction

            if( olddir != server.DI_NODIR && this.StepDirection( actor, olddir, dist ) )
                return;

            if( ( MathLib.Random() & 1 ) != 0 ) 	//randomly determine direction of search
            {
                for( tdir = 0; tdir <= 315; tdir += 45 )
                {
                    if( tdir != turnaround && this.StepDirection( actor, tdir, dist ) )
                        return;
                }
            }
            else
            {
                for( tdir = 315; tdir >= 0; tdir -= 45 )
                {
                    if( tdir != turnaround && this.StepDirection( actor, tdir, dist ) )
                        return;
                }
            }

            if( turnaround != server.DI_NODIR && this.StepDirection( actor, turnaround, dist ) )
                return;

            actor.v.ideal_yaw = olddir;		// can't move

            // if a bridge was pulled out from underneath a monster, it may not have
            // a valid standing position at all

            if( !this.CheckBottom( actor ) )
                this.FixCheckBottom( actor );
        }

        /// <summary>
        /// SV_FixCheckBottom
        /// </summary>
        private void FixCheckBottom( MemoryEdict ent )
        {
            ent.v.flags = ( int ) ent.v.flags | EdictFlags.FL_PARTIALGROUND;
        }
    }
}
