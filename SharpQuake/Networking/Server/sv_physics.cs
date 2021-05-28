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
		private const float STOP_EPSILON = 0.1f;
		private const int MAX_CLIP_PLANES = 5;
		private const float STEPSIZE = 18;

		/// <summary>
		/// SV_Physics
		/// </summary>
		public void Physics( )
		{
			// let the progs know that a new frame has started
			this.Host.Programs.GlobalStruct.self = this.EdictToProg(this.sv.edicts[0] );
			this.Host.Programs.GlobalStruct.other = this.Host.Programs.GlobalStruct.self;
			this.Host.Programs.GlobalStruct.time = ( float )this.sv.time;
			this.Host.Programs.Execute(this.Host.Programs.GlobalStruct.StartFrame );

			//
			// treat each object in turn
			//
			for ( var i = 0; i < this.sv.num_edicts; i++ )
			{
				var ent = this.sv.edicts[i];
				if ( ent.free )
					continue;

				if (this.Host.Programs.GlobalStruct.force_retouch != 0 )
					this.LinkEdict( ent, true ); // force retouch even for stationary

				if ( i > 0 && i <= this.svs.maxclients )
					this.Physics_Client( ent, i );
				else
				{
					switch ( ( int ) ent.v.movetype )
					{
						case Movetypes.MOVETYPE_PUSH:
							this.Physics_Pusher( ent );
							break;

						case Movetypes.MOVETYPE_NONE:
							this.Physics_None( ent );
							break;

						case Movetypes.MOVETYPE_NOCLIP:
							this.Physics_Noclip( ent );
							break;

						case Movetypes.MOVETYPE_STEP:
							this.Physics_Step( ent );
							break;

						case Movetypes.MOVETYPE_TOSS:
						case Movetypes.MOVETYPE_BOUNCE:
						case Movetypes.MOVETYPE_FLY:
						case Movetypes.MOVETYPE_FLYMISSILE:
							this.Physics_Toss( ent );
							break;

						default:
							Utilities.Error( "SV_Physics: bad movetype {0}", ( int ) ent.v.movetype );
							break;
					}
				}
			}

			if (this.Host.Programs.GlobalStruct.force_retouch != 0 )
				this.Host.Programs.GlobalStruct.force_retouch -= 1;

			this.sv.time += this.Host.FrameTime;
		}

		/// <summary>
		/// SV_Physics_Toss
		/// Toss, bounce, and fly movement.  When onground, do nothing.
		/// </summary>
		private void Physics_Toss( MemoryEdict ent )
		{
			// regular thinking
			if ( !this.RunThink( ent ) )
				return;

			// if onground, return without moving
			if ( ( ( int ) ent.v.flags & EdictFlags.FL_ONGROUND ) != 0 )
				return;

			this.CheckVelocity( ent );

			// add gravity
			if ( ent.v.movetype != Movetypes.MOVETYPE_FLY && ent.v.movetype != Movetypes.MOVETYPE_FLYMISSILE )
				this.AddGravity( ent );

			// move angles
			MathLib.VectorMA( ref ent.v.angles, ( float )this.Host.FrameTime, ref ent.v.avelocity, out ent.v.angles );

			// move origin
			Vector3 move;
			MathLib.VectorScale( ref ent.v.velocity, ( float )this.Host.FrameTime, out move );
			var trace = this.PushEntity( ent, ref move );

			if ( trace.fraction == 1 )
				return;
			if ( ent.free )
				return;

			float backoff;
			if ( ent.v.movetype == Movetypes.MOVETYPE_BOUNCE )
				backoff = 1.5f;
			else
				backoff = 1;

			this.ClipVelocity( ref ent.v.velocity, ref trace.plane.normal, out ent.v.velocity, backoff );

			// stop if on ground
			if ( trace.plane.normal.Z > 0.7f )
			{
				if ( ent.v.velocity.Z < 60 || ent.v.movetype != Movetypes.MOVETYPE_BOUNCE )
				{
					ent.v.flags = ( int ) ent.v.flags | EdictFlags.FL_ONGROUND;
					ent.v.groundentity = this.EdictToProg( trace.ent );
					ent.v.velocity = default;
					ent.v.avelocity = default;
				}
			}

			// check for in water
			this.CheckWaterTransition( ent );
		}

		/// <summary>
		/// ClipVelocity
		/// Slide off of the impacting object
		/// returns the blocked flags (1 = floor, 2 = step / wall)
		/// </summary>
		private int ClipVelocity( ref Vector3 src, ref Vector3 normal, out Vector3 dest, float overbounce )
		{
			var blocked = 0;
			if ( normal.Z > 0 )
				blocked |= 1;       // floor
			if ( normal.Z == 0 )
				blocked |= 2;       // step

			var backoff = ( src.X * normal.X + src.Y * normal.Y + src.Z * normal.Z ) * overbounce;

			dest.X = src.X - normal.X * backoff;
			dest.Y = src.Y - normal.Y * backoff;
			dest.Z = src.Z - normal.Z * backoff;

			if ( dest.X > -server.STOP_EPSILON && dest.X < server.STOP_EPSILON )
				dest.X = 0;
			if ( dest.Y > -server.STOP_EPSILON && dest.Y < server.STOP_EPSILON )
				dest.Y = 0;
			if ( dest.Z > -server.STOP_EPSILON && dest.Z < server.STOP_EPSILON )
				dest.Z = 0;

			return blocked;
		}

		/// <summary>
		/// PushEntity
		/// Does not change the entities velocity at all
		/// </summary>
		private Trace_t PushEntity( MemoryEdict ent, ref Vector3 push )
		{
			Vector3 end;
			MathLib.VectorAdd( ref ent.v.origin, ref push, out end );

			Trace_t trace;
			if ( ent.v.movetype == Movetypes.MOVETYPE_FLYMISSILE )
				trace = this.Move( ref ent.v.origin, ref ent.v.mins, ref ent.v.maxs, ref end, server.MOVE_MISSILE, ent );
			else if ( ent.v.solid == Solids.SOLID_TRIGGER || ent.v.solid == Solids.SOLID_NOT )
				// only clip against bmodels
				trace = this.Move( ref ent.v.origin, ref ent.v.mins, ref ent.v.maxs, ref end, server.MOVE_NOMONSTERS, ent );
			else
				trace = this.Move( ref ent.v.origin, ref ent.v.mins, ref ent.v.maxs, ref end, server.MOVE_NORMAL, ent );

			MathLib.Copy( ref trace.endpos, out ent.v.origin );
			this.LinkEdict( ent, true );

			if ( trace.ent != null )
				this.Impact( ent, trace.ent );

			return trace;
		}

		/// <summary>
		/// SV_CheckWaterTransition
		/// </summary>
		private void CheckWaterTransition( MemoryEdict ent )
		{
			var org = Utilities.ToVector( ref ent.v.origin );
			var cont = this.PointContents( ref org );

			if ( ent.v.watertype == 0 )
			{
				// just spawned here
				ent.v.watertype = cont;
				ent.v.waterlevel = 1;
				return;
			}

			if ( cont <= ( int ) Q1Contents.Water )
			{
				if ( ent.v.watertype == ( int ) Q1Contents.Empty )
				{
					// just crossed into water
					this.StartSound( ent, 0, "misc/h2ohit1.wav", 255, 1 );
				}
				ent.v.watertype = cont;
				ent.v.waterlevel = 1;
			}
			else
			{
				if ( ent.v.watertype != ( int ) Q1Contents.Empty )
				{
					// just crossed into water
					this.StartSound( ent, 0, "misc/h2ohit1.wav", 255, 1 );
				}
				ent.v.watertype = ( int ) Q1Contents.Empty;
				ent.v.waterlevel = cont;
			}
		}

		/// <summary>
		/// SV_AddGravity
		/// </summary>
		private void AddGravity( MemoryEdict ent )
		{
			var val = this.Host.Programs.GetEdictFieldFloat( ent, "gravity" );
			if ( val == 0 )
				val = 1;
			ent.v.velocity.Z -= ( float ) ( val * this.Host.Cvars.Gravity.Get<float>() * this.Host.FrameTime );
		}

		/// <summary>
		/// SV_Physics_Step
		/// </summary>
		private void Physics_Step( MemoryEdict ent )
		{
			bool hitsound;

			// freefall if not onground
			if ( ( ( int ) ent.v.flags & ( EdictFlags.FL_ONGROUND | EdictFlags.FL_FLY | EdictFlags.FL_SWIM ) ) == 0 )
			{
				if ( ent.v.velocity.Z < this.Host.Cvars.Gravity.Get<float>() * -0.1 )
					hitsound = true;
				else
					hitsound = false;

				this.AddGravity( ent );
				this.CheckVelocity( ent );
				this.FlyMove( ent, ( float )this.Host.FrameTime, null );
				this.LinkEdict( ent, true );

				if ( ( ( int ) ent.v.flags & EdictFlags.FL_ONGROUND ) != 0 )  // just hit ground
				{
					if ( hitsound )
						this.StartSound( ent, 0, "demon/dland2.wav", 255, 1 );
				}
			}

			// regular thinking
			this.RunThink( ent );

			this.CheckWaterTransition( ent );
		}

		/// <summary>
		/// SV_Physics_Noclip
		/// A moving object that doesn't obey physics
		/// </summary>
		private void Physics_Noclip( MemoryEdict ent )
		{
			// regular thinking
			if ( !this.RunThink( ent ) )
				return;

			MathLib.VectorMA( ref ent.v.angles, ( float )this.Host.FrameTime, ref ent.v.avelocity, out ent.v.angles );
			MathLib.VectorMA( ref ent.v.origin, ( float )this.Host.FrameTime, ref ent.v.velocity, out ent.v.origin );
			this.LinkEdict( ent, false );
		}

		/// <summary>
		/// SV_Physics_None
		/// Non moving objects can only think
		/// </summary>
		private void Physics_None( MemoryEdict ent )
		{
			// regular thinking
			this.RunThink( ent );
		}

		/// <summary>
		/// SV_Physics_Pusher
		/// </summary>
		private void Physics_Pusher( MemoryEdict ent )
		{
			var oldltime = ent.v.ltime;
			var thinktime = ent.v.nextthink;
			float movetime;
			if ( thinktime < ent.v.ltime + this.Host.FrameTime )
			{
				movetime = thinktime - ent.v.ltime;
				if ( movetime < 0 )
					movetime = 0;
			}
			else
				movetime = ( float )this.Host.FrameTime;

			if ( movetime != 0 )
				this.PushMove( ent, movetime );  // advances ent.v.ltime if not blocked

			if ( thinktime > oldltime && thinktime <= ent.v.ltime )
			{
				ent.v.nextthink = 0;
				this.Host.Programs.GlobalStruct.time = ( float )this.sv.time;
				this.Host.Programs.GlobalStruct.self = this.EdictToProg( ent );
				this.Host.Programs.GlobalStruct.other = this.EdictToProg(this.sv.edicts[0] );
				this.Host.Programs.Execute( ent.v.think );
				if ( ent.free )
					return;
			}
		}

		/// <summary>
		/// SV_Physics_Client
		/// Player character actions
		/// </summary>
		private void Physics_Client( MemoryEdict ent, int num )
		{
			if ( !this.svs.clients[num - 1].active )
				return;     // unconnected slot

			//
			// call standard client pre-think
			//
			this.Host.Programs.GlobalStruct.time = ( float )this.sv.time;
			this.Host.Programs.GlobalStruct.self = this.EdictToProg( ent );
			this.Host.Programs.Execute(this.Host.Programs.GlobalStruct.PlayerPreThink );

			//
			// do a move
			//
			this.CheckVelocity( ent );

			//
			// decide which move function to call
			//
			switch ( ( int ) ent.v.movetype )
			{
				case Movetypes.MOVETYPE_NONE:
					if ( !this.RunThink( ent ) )
						return;
					break;

				case Movetypes.MOVETYPE_WALK:
					if ( !this.RunThink( ent ) )
						return;
					if ( !this.CheckWater( ent ) && ( ( int ) ent.v.flags & EdictFlags.FL_WATERJUMP ) == 0 )
						this.AddGravity( ent );

					this.CheckStuck( ent );

					this.WalkMove( ent );
					break;

				case Movetypes.MOVETYPE_TOSS:
				case Movetypes.MOVETYPE_BOUNCE:
					this.Physics_Toss( ent );
					break;

				case Movetypes.MOVETYPE_FLY:
					if ( !this.RunThink( ent ) )
						return;

					this.FlyMove( ent, ( float )this.Host.FrameTime, null );
					break;

				case Movetypes.MOVETYPE_NOCLIP:
					if ( !this.RunThink( ent ) )
						return;
					MathLib.VectorMA( ref ent.v.origin, ( float )this.Host.FrameTime, ref ent.v.velocity, out ent.v.origin );
					break;

				default:
					Utilities.Error( "SV_Physics_client: bad movetype {0}", ( int ) ent.v.movetype );
					break;
			}

			//
			// call standard player post-think
			//
			this.LinkEdict( ent, true );

			this.Host.Programs.GlobalStruct.time = ( float )this.sv.time;
			this.Host.Programs.GlobalStruct.self = this.EdictToProg( ent );
			this.Host.Programs.Execute(this.Host.Programs.GlobalStruct.PlayerPostThink );
		}

		/// <summary>
		/// SV_WalkMove
		/// Only used by players
		/// </summary>
		private void WalkMove( MemoryEdict ent )
		{
			//
			// do a regular slide move unless it looks like you ran into a step
			//
			var oldonground = ( int ) ent.v.flags & EdictFlags.FL_ONGROUND;
			ent.v.flags = ( int ) ent.v.flags & ~EdictFlags.FL_ONGROUND;

			var oldorg = ent.v.origin;
			var oldvel = ent.v.velocity;
			var steptrace = new Trace_t();
			var clip = this.FlyMove( ent, ( float )this.Host.FrameTime, steptrace );

			if ( ( clip & 2 ) == 0 )
				return;     // move didn't block on a step

			if ( oldonground == 0 && ent.v.waterlevel == 0 )
				return;     // don't stair up while jumping

			if ( ent.v.movetype != Movetypes.MOVETYPE_WALK )
				return;     // gibbed by a trigger

			if (this.Host.Cvars.NoStep.Get<bool>() )
				return;

			if ( ( ( int )this._Player.v.flags & EdictFlags.FL_WATERJUMP ) != 0 )
				return;

			var nosteporg = ent.v.origin;
			var nostepvel = ent.v.velocity;

			//
			// try moving up and forward to go up a step
			//
			ent.v.origin = oldorg;  // back to start pos

			var upmove = Utilities.ZeroVector3f;
			var downmove = upmove;
			upmove.Z = server.STEPSIZE;
			downmove.Z = ( float ) ( -server.STEPSIZE + oldvel.Z * this.Host.FrameTime );

			// move up
			this.PushEntity( ent, ref upmove );  // FIXME: don't link?

			// move forward
			ent.v.velocity.X = oldvel.X;
			ent.v.velocity.Y = oldvel.Y;
			ent.v.velocity.Z = 0;
			clip = this.FlyMove( ent, ( float )this.Host.FrameTime, steptrace );

			// check for stuckness, possibly due to the limited precision of floats
			// in the clipping hulls
			if ( clip != 0 )
			{
				if ( Math.Abs( oldorg.Y - ent.v.origin.Y ) < 0.03125 && Math.Abs( oldorg.X - ent.v.origin.X ) < 0.03125 )
				{
					// stepping up didn't make any progress
					clip = this.TryUnstick( ent, ref oldvel );
				}
			}

			// extra friction based on view angle
			if ( ( clip & 2 ) != 0 )
				this.WallFriction( ent, steptrace );

			// move down
			var downtrace = this.PushEntity( ent, ref downmove );    // FIXME: don't link?

			if ( downtrace.plane.normal.Z > 0.7 )
			{
				if ( ent.v.solid == Solids.SOLID_BSP )
				{
					ent.v.flags = ( int ) ent.v.flags | EdictFlags.FL_ONGROUND;
					ent.v.groundentity = this.EdictToProg( downtrace.ent );
				}
			}
			else
			{
				// if the push down didn't end up on good ground, use the move without
				// the step up.  This happens near wall / slope combinations, and can
				// cause the player to hop up higher on a slope too steep to climb
				ent.v.origin = nosteporg;
				ent.v.velocity = nostepvel;
			}
		}

		/// <summary>
		/// SV_TryUnstick
		/// Player has come to a dead stop, possibly due to the problem with limited
		/// float precision at some angle joins in the BSP hull.
		///
		/// Try fixing by pushing one pixel in each direction.
		///
		/// This is a hack, but in the interest of good gameplay...
		/// </summary>
		private int TryUnstick( MemoryEdict ent, ref Vector3 oldvel )
		{
			var oldorg = ent.v.origin;
			var dir = Utilities.ZeroVector3f;

			var steptrace = new Trace_t();
			for ( var i = 0; i < 8; i++ )
			{
				// try pushing a little in an axial direction
				switch ( i )
				{
					case 0:
						dir.X = 2;
						dir.Y = 0;
						break;

					case 1:
						dir.X = 0;
						dir.Y = 2;
						break;

					case 2:
						dir.X = -2;
						dir.Y = 0;
						break;

					case 3:
						dir.X = 0;
						dir.Y = -2;
						break;

					case 4:
						dir.X = 2;
						dir.Y = 2;
						break;

					case 5:
						dir.X = -2;
						dir.Y = 2;
						break;

					case 6:
						dir.X = 2;
						dir.Y = -2;
						break;

					case 7:
						dir.X = -2;
						dir.Y = -2;
						break;
				}

				this.PushEntity( ent, ref dir );

				// retry the original move
				ent.v.velocity.X = oldvel.X;
				ent.v.velocity.Y = oldvel.Y;
				ent.v.velocity.Z = 0;
				var clip = this.FlyMove( ent, 0.1f, steptrace );

				if ( Math.Abs( oldorg.Y - ent.v.origin.Y ) > 4 || Math.Abs( oldorg.X - ent.v.origin.X ) > 4 )
					return clip;

				// go back to the original pos and try again
				ent.v.origin = oldorg;
			}

			ent.v.velocity = Utilities.ZeroVector3f;
			return 7;       // still not moving
		}

		/// <summary>
		/// SV_WallFriction
		/// </summary>
		private void WallFriction( MemoryEdict ent, Trace_t trace )
		{
			Vector3 forward, right, up, vangle = Utilities.ToVector( ref ent.v.v_angle );
			MathLib.AngleVectors( ref vangle, out forward, out right, out up );
			var d = Vector3.Dot( trace.plane.normal, forward );

			d += 0.5f;
			if ( d >= 0 )
				return;

			// cut the tangential velocity
			var vel = Utilities.ToVector( ref ent.v.velocity );
			var i = Vector3.Dot( trace.plane.normal, vel );
			var into = trace.plane.normal * i;
			var side = vel - into;

			ent.v.velocity.X = side.X * ( 1 + d );
			ent.v.velocity.Y = side.Y * ( 1 + d );
		}

		/// <summary>
		/// SV_CheckStuck
		/// This is a big hack to try and fix the rare case of getting stuck in the world
		/// clipping hull.
		/// </summary>
		private void CheckStuck( MemoryEdict ent )
		{
			if (this.TestEntityPosition( ent ) == null )
			{
				ent.v.oldorigin = ent.v.origin;
				return;
			}

			var org = ent.v.origin;
			ent.v.origin = ent.v.oldorigin;
			if (this.TestEntityPosition( ent ) == null )
			{
				this.Host.Console.DPrint( "Unstuck.\n" );
				this.LinkEdict( ent, true );
				return;
			}

			for ( var z = 0; z < 18; z++ )
				for ( var i = -1; i <= 1; i++ )
					for ( var j = -1; j <= 1; j++ )
					{
						ent.v.origin.X = org.X + i;
						ent.v.origin.Y = org.Y + j;
						ent.v.origin.Z = org.Z + z;
						if (this.TestEntityPosition( ent ) == null )
						{
							this.Host.Console.DPrint( "Unstuck.\n" );
							this.LinkEdict( ent, true );
							return;
						}
					}

			ent.v.origin = org;
			this.Host.Console.DPrint( "player is stuck.\n" );
		}

		/// <summary>
		/// SV_CheckWater
		/// </summary>
		private bool CheckWater( MemoryEdict ent )
		{
			Vector3 point;
			point.X = ent.v.origin.X;
			point.Y = ent.v.origin.Y;
			point.Z = ent.v.origin.Z + ent.v.mins.Z + 1;

			ent.v.waterlevel = 0;
			ent.v.watertype = ( int ) Q1Contents.Empty;
			var cont = this.PointContents( ref point );
			if ( cont <= ( int ) Q1Contents.Water )
			{
				ent.v.watertype = cont;
				ent.v.waterlevel = 1;
				point.Z = ent.v.origin.Z + ( ent.v.mins.Z + ent.v.maxs.Z ) * 0.5f;
				cont = this.PointContents( ref point );
				if ( cont <= ( int ) Q1Contents.Water )
				{
					ent.v.waterlevel = 2;
					point.Z = ent.v.origin.Z + ent.v.view_ofs.Z;
					cont = this.PointContents( ref point );
					if ( cont <= ( int ) Q1Contents.Water )
						ent.v.waterlevel = 3;
				}
			}

			return ent.v.waterlevel > 1;
		}

		/// <summary>
		/// SV_RunThink
		/// Runs thinking code if time.  There is some play in the exact time the think
		/// function will be called, because it is called before any movement is done
		/// in a frame.  Not used for pushmove objects, because they must be exact.
		/// Returns false if the entity removed itself.
		/// </summary>
		private bool RunThink( MemoryEdict ent )
		{
			float thinktime;

			thinktime = ent.v.nextthink;
			if ( thinktime <= 0 || thinktime > this.sv.time + this.Host.FrameTime )
				return true;

			if ( thinktime < this.sv.time )
				thinktime = ( float )this.sv.time; // don't let things stay in the past.

			// it is possible to start that way
			// by a trigger with a local time.
			ent.v.nextthink = 0;
			this.Host.Programs.GlobalStruct.time = thinktime;
			this.Host.Programs.GlobalStruct.self = this.EdictToProg( ent );
			this.Host.Programs.GlobalStruct.other = this.EdictToProg(this.sv.edicts[0] );
			this.Host.Programs.Execute( ent.v.think );

			return !ent.free;
		}

		/// <summary>
		/// SV_CheckVelocity
		/// </summary>
		private void CheckVelocity( MemoryEdict ent )
		{
			//
			// bound velocity
			//
			if ( MathLib.CheckNaN( ref ent.v.velocity, 0 ) )
				this.Host.Console.Print( "Got a NaN velocity on {0}\n", this.Host.Programs.GetString( ent.v.classname ) );

			if ( MathLib.CheckNaN( ref ent.v.origin, 0 ) )
				this.Host.Console.Print( "Got a NaN origin on {0}\n", this.Host.Programs.GetString( ent.v.classname ) );

			var max = Vector3.One * this.Host.Cvars.MaxVelocity.Get<float>();
			var min = -Vector3.One * this.Host.Cvars.MaxVelocity.Get<float>();
			MathLib.Clamp( ref ent.v.velocity, ref min, ref max, out ent.v.velocity );
		}

		/// <summary>
		/// SV_FlyMove
		/// The basic solid body movement clip that slides along multiple planes
		/// Returns the clipflags if the velocity was modified (hit something solid)
		/// 1 = floor
		/// 2 = wall / step
		/// 4 = dead stop
		/// If steptrace is not NULL, the trace of any vertical wall hit will be stored
		/// </summary>
		private int FlyMove( MemoryEdict ent, float time, Trace_t steptrace )
		{
			var original_velocity = ent.v.velocity;
			var primal_velocity = ent.v.velocity;

			var numbumps = 4;
			var blocked = 0;
			var planes = new Vector3[server.MAX_CLIP_PLANES];
			var numplanes = 0;
			var time_left = time;

			for ( var bumpcount = 0; bumpcount < numbumps; bumpcount++ )
			{
				if ( ent.v.velocity == Vector3.Zero )
					break;

				Vector3 end;
				MathLib.VectorMA( ref ent.v.origin, time_left, ref ent.v.velocity, out end );

				var trace = this.Move( ref ent.v.origin, ref ent.v.mins, ref ent.v.maxs, ref end, 0, ent );

				if ( trace.allsolid )
				{   // entity is trapped in another solid
					ent.v.velocity = default;
					return 3;
				}

				if ( trace.fraction > 0 )
				{   // actually covered some distance
					MathLib.Copy( ref trace.endpos, out ent.v.origin );
					original_velocity = ent.v.velocity;
					numplanes = 0;
				}

				if ( trace.fraction == 1 )
					break;      // moved the entire distance

				if ( trace.ent == null )
					Utilities.Error( "SV_FlyMove: !trace.ent" );

				if ( trace.plane.normal.Z > 0.7 )
				{
					blocked |= 1;       // floor
					if ( trace.ent.v.solid == Solids.SOLID_BSP )
					{
						ent.v.flags = ( int ) ent.v.flags | EdictFlags.FL_ONGROUND;
						ent.v.groundentity = this.EdictToProg( trace.ent );
					}
				}

				if ( trace.plane.normal.Z == 0 )
				{
					blocked |= 2;       // step
					if ( steptrace != null )
						steptrace.CopyFrom( trace );    // save for player extrafriction
				}

				//
				// run the impact function
				//
				this.Impact( ent, trace.ent );
				if ( ent.free )
					break;      // removed by the impact function

				time_left -= time_left * trace.fraction;

				// cliped to another plane
				if ( numplanes >= server.MAX_CLIP_PLANES )
				{
					// this shouldn't really happen
					ent.v.velocity = default;
					return 3;
				}

				planes[numplanes] = trace.plane.normal;
				numplanes++;

				//
				// modify original_velocity so it parallels all of the clip planes
				//
				var new_velocity = default( Vector3 );
				int i, j;
				for ( i = 0; i < numplanes; i++ )
				{
					this.ClipVelocity( ref original_velocity, ref planes[i], out new_velocity, 1 );
					for ( j = 0; j < numplanes; j++ )
					{
						if ( j != i )
						{
							var dot = new_velocity.X * planes[j].X + new_velocity.Y * planes[j].Y + new_velocity.Z * planes[j].Z;
							if ( dot < 0 )
								break;  // not ok
						}
					}

					if ( j == numplanes )
						break;
				}

				if ( i != numplanes )
				{
					// go along this plane
					ent.v.velocity = new_velocity;
				}
				else
				{
					// go along the crease
					if ( numplanes != 2 )
					{
						ent.v.velocity = default;
						return 7;
					}
					var dir = Vector3.Cross( planes[0], planes[1] );
					var d = dir.X * ent.v.velocity.X + dir.Y * ent.v.velocity.Y + dir.Z * ent.v.velocity.Z;
					MathLib.Copy( ref dir, out ent.v.velocity );
					MathLib.VectorScale( ref ent.v.velocity, d, out ent.v.velocity );
				}

				//
				// if original velocity is against the original velocity, stop dead
				// to avoid tiny occilations in sloping corners
				//
				if ( MathLib.DotProduct( ref ent.v.velocity, ref primal_velocity ) <= 0 )
				{
					ent.v.velocity = default;
					return blocked;
				}
			}

			return blocked;
		}

		/// <summary>
		/// SV_Impact
		/// Two entities have touched, so run their touch functions
		/// </summary>
		private void Impact( MemoryEdict e1, MemoryEdict e2 )
		{
			var old_self = this.Host.Programs.GlobalStruct.self;
			var old_other = this.Host.Programs.GlobalStruct.other;

			this.Host.Programs.GlobalStruct.time = ( float )this.sv.time;
			if ( e1.v.touch != 0 && e1.v.solid != Solids.SOLID_NOT )
			{
				this.Host.Programs.GlobalStruct.self = this.EdictToProg( e1 );
				this.Host.Programs.GlobalStruct.other = this.EdictToProg( e2 );
				this.Host.Programs.Execute( e1.v.touch );
			}

			if ( e2.v.touch != 0 && e2.v.solid != Solids.SOLID_NOT )
			{
				this.Host.Programs.GlobalStruct.self = this.EdictToProg( e2 );
				this.Host.Programs.GlobalStruct.other = this.EdictToProg( e1 );
				this.Host.Programs.Execute( e2.v.touch );
			}

			this.Host.Programs.GlobalStruct.self = old_self;
			this.Host.Programs.GlobalStruct.other = old_other;
		}

		/// <summary>
		/// SV_PushMove
		/// </summary>
		private void PushMove( MemoryEdict pusher, float movetime )
		{
			if ( pusher.v.velocity == Vector3.Zero )
			{
				pusher.v.ltime += movetime;
				return;
			}

			Vector3 move, mins, maxs;
			MathLib.VectorScale( ref pusher.v.velocity, movetime, out move );
			MathLib.VectorAdd( ref pusher.v.absmin, ref move, out mins );
			MathLib.VectorAdd( ref pusher.v.absmax, ref move, out maxs );

			var pushorig = pusher.v.origin;

			var moved_edict = new MemoryEdict[QDef.MAX_EDICTS];
			var moved_from = new Vector3[QDef.MAX_EDICTS];

			// move the pusher to it's final position

			MathLib.VectorAdd( ref pusher.v.origin, ref move, out pusher.v.origin );
			pusher.v.ltime += movetime;
			this.LinkEdict( pusher, false );

			// see if any solid entities are inside the final position
			var num_moved = 0;
			for ( var e = 1; e < this.sv.num_edicts; e++ )
			{
				var check = this.sv.edicts[e];
				if ( check.free )
					continue;
				if ( check.v.movetype == Movetypes.MOVETYPE_PUSH ||
					check.v.movetype == Movetypes.MOVETYPE_NONE ||
					check.v.movetype == Movetypes.MOVETYPE_NOCLIP )
					continue;

				// if the entity is standing on the pusher, it will definately be moved
				if ( !( ( ( int ) check.v.flags & EdictFlags.FL_ONGROUND ) != 0 && this.ProgToEdict( check.v.groundentity ) == pusher ) )
				{
					if ( check.v.absmin.X >= maxs.X || check.v.absmin.Y >= maxs.Y ||
						check.v.absmin.Z >= maxs.Z || check.v.absmax.X <= mins.X ||
						check.v.absmax.Y <= mins.Y || check.v.absmax.Z <= mins.Z )
						continue;

					// see if the ent's bbox is inside the pusher's final position
					if (this.TestEntityPosition( check ) == null )
						continue;
				}

				// remove the onground flag for non-players
				if ( check.v.movetype != Movetypes.MOVETYPE_WALK )
					check.v.flags = ( int ) check.v.flags & ~EdictFlags.FL_ONGROUND;

				var entorig = check.v.origin;
				moved_from[num_moved] = entorig;
				moved_edict[num_moved] = check;
				num_moved++;

				// try moving the contacted entity
				pusher.v.solid = Solids.SOLID_NOT;
				this.PushEntity( check, ref move );
				pusher.v.solid = Solids.SOLID_BSP;

				// if it is still inside the pusher, block
				var block = this.TestEntityPosition( check );
				if ( block != null )
				{
					// fail the move
					if ( check.v.mins.X == check.v.maxs.X )
						continue;
					if ( check.v.solid == Solids.SOLID_NOT || check.v.solid == Solids.SOLID_TRIGGER )
					{
						// corpse
						check.v.mins.X = check.v.mins.Y = 0;
						check.v.maxs = check.v.mins;
						continue;
					}

					check.v.origin = entorig;
					this.LinkEdict( check, true );

					pusher.v.origin = pushorig;
					this.LinkEdict( pusher, false );
					pusher.v.ltime -= movetime;

					// if the pusher has a "blocked" function, call it
					// otherwise, just stay in place until the obstacle is gone
					if ( pusher.v.blocked != 0 )
					{
						this.Host.Programs.GlobalStruct.self = this.EdictToProg( pusher );
						this.Host.Programs.GlobalStruct.other = this.EdictToProg( check );
						this.Host.Programs.Execute( pusher.v.blocked );
					}

					// move back any entities we already moved
					for ( var i = 0; i < num_moved; i++ )
					{
						moved_edict[i].v.origin = moved_from[i];
						this.LinkEdict( moved_edict[i], false );
					}
					return;
				}
			}
		}
	}
}
