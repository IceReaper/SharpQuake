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



// view.h
// view.c -- player eye positioning

// The view is allowed to move slightly from it's true position for bobbing,
// but if it exceeds 8 pixels linear distance (spherical, not box), the list of
// entities sent from the server may not include everything in the pvs, especially
// when crossing a water boudnary.

namespace SharpQuake.Rendering
{
	using Engine.Host;
	using Framework.Definitions;
	using Framework.IO;
	using Framework.IO.BSP.Q1;
	using Framework.Mathematics;
	using Networking.Client;
	using System;
	using System.Drawing;
	using System.Numerics;

	/// <summary>
	/// V_functions
	/// </summary>
	public class View
	{
		public float Crosshair => this.Host.Cvars.Crosshair.Get<float>();

		public float Gamma => this.Host.Cvars.Gamma.Get<float>();

		public Color Blend;
		private static readonly Vector3 SmallOffset = Vector3.One / 32f;

		private byte[] _GammaTable; // [256];	// palette is sent through this
		private cshift_t _CShift_empty;// = { { 130, 80, 50 }, 0 };
		private cshift_t _CShift_water;// = { { 130, 80, 50 }, 128 };
		private cshift_t _CShift_slime;// = { { 0, 25, 5 }, 150 };
		private cshift_t _CShift_lava;// = { { 255, 80, 0 }, 150 };

		// v_blend[4]		// rgba 0.0 - 1.0
		private byte[,] _Ramps = new byte[3, 256]; // ramps[3][256]

		private Vector3 _Forward; // vec3_t forward
		private Vector3 _Right; // vec3_t right
		private Vector3 _Up; // vec3_t up

		private float _DmgTime; // v_dmg_time
		private float _DmgRoll; // v_dmg_roll
		private float _DmgPitch; // v_dmg_pitch

		private float _OldZ = 0; // static oldz  from CalcRefdef()
		private float _OldYaw = 0; // static oldyaw from CalcGunAngle
		private float _OldPitch = 0; // static oldpitch from CalcGunAngle
		private float _OldGammaValue; // static float oldgammavalue from CheckGamma

		// Instances
		private Host Host
		{
			get;
			set;
		}

		// V_Init
		public void Initialise( )
		{
			this.Host.Commands.Add( "v_cshift", this.CShift_f );
			this.Host.Commands.Add( "bf", this.BonusFlash_f );
			this.Host.Commands.Add( "centerview", this.StartPitchDrift );

			if (this.Host.Cvars.LcdX == null )
			{
				this.Host.Cvars.LcdX = this.Host.CVars.Add( "lcd_x", 0f );
				this.Host.Cvars.LcdYaw = this.Host.CVars.Add( "lcd_yaw", 0f );

				this.Host.Cvars.ScrOfsX = this.Host.CVars.Add( "scr_ofsx", 0f );
				this.Host.Cvars.ScrOfsY = this.Host.CVars.Add( "scr_ofsy", 0f );
				this.Host.Cvars.ScrOfsZ = this.Host.CVars.Add( "scr_ofsz", 0f );

				this.Host.Cvars.ClRollSpeed = this.Host.CVars.Add( "cl_rollspeed", 200f );
				this.Host.Cvars.ClRollAngle = this.Host.CVars.Add( "cl_rollangle", 2.0f );

				this.Host.Cvars.ClBob = this.Host.CVars.Add( "cl_bob", 0.02f );
				this.Host.Cvars.ClBobCycle = this.Host.CVars.Add( "cl_bobcycle", 0.6f );
				this.Host.Cvars.ClBobUp = this.Host.CVars.Add( "cl_bobup", 0.5f );

				this.Host.Cvars.KickTime = this.Host.CVars.Add( "v_kicktime", 0.5f );
				this.Host.Cvars.KickRoll = this.Host.CVars.Add( "v_kickroll", 0.6f );
				this.Host.Cvars.KickPitch = this.Host.CVars.Add( "v_kickpitch", 0.6f );

				this.Host.Cvars.IYawCycle = this.Host.CVars.Add( "v_iyaw_cycle", 2f );
				this.Host.Cvars.IRollCycle = this.Host.CVars.Add( "v_iroll_cycle", 0.5f );
				this.Host.Cvars.IPitchCycle = this.Host.CVars.Add( "v_ipitch_cycle", 1f );
				this.Host.Cvars.IYawLevel = this.Host.CVars.Add( "v_iyaw_level", 0.3f );
				this.Host.Cvars.IRollLevel = this.Host.CVars.Add( "v_iroll_level", 0.1f );
				this.Host.Cvars.IPitchLevel = this.Host.CVars.Add( "v_ipitch_level", 0.3f );

				this.Host.Cvars.IdleScale = this.Host.CVars.Add( "v_idlescale", 0f );

				this.Host.Cvars.Crosshair = this.Host.CVars.Add( "crosshair", 0f, ClientVariableFlags.Archive );
				this.Host.Cvars.ClCrossX = this.Host.CVars.Add( "cl_crossx", 0f );
				this.Host.Cvars.ClCrossY = this.Host.CVars.Add( "cl_crossy", 0f );

				this.Host.Cvars.glCShiftPercent = this.Host.CVars.Add( "gl_cshiftpercent", 100f );

				this.Host.Cvars.CenterMove = this.Host.CVars.Add( "v_centermove", 0.15f );
				this.Host.Cvars.CenterSpeed = this.Host.CVars.Add( "v_centerspeed", 500f );

				this.BuildGammaTable( 1.0f );    // no gamma yet
				this.Host.Cvars.Gamma = this.Host.CVars.Add( "gamma", 1f, ClientVariableFlags.Archive );
			}
		}

		/// <summary>
		/// V_RenderView
		/// The player's clipping box goes from (-16 -16 -24) to (16 16 32) from
		/// the entity origin, so any view position inside that will be valid
		/// </summary>
		public void RenderView( )
		{
			if (this.Host.Console.ForcedUp )
				return;

			// don't allow cheats in multiplayer
			if (this.Host.Client.cl.maxclients > 1 )
			{
				this.Host.CVars.Set( "scr_ofsx", 0f );
				this.Host.CVars.Set( "scr_ofsy", 0f );
				this.Host.CVars.Set( "scr_ofsz", 0f );
			}

			if (this.Host.Client.cl.intermission > 0 )
			{
				// intermission / finale rendering
				this.CalcIntermissionRefDef();
			}
			else if ( !this.Host.Client.cl.paused )
				this.CalcRefDef();

			this.Host.RenderContext.PushDlights();

			if (this.Host.Cvars.LcdX.Get<float>() != 0 )
			{
				//
				// render two interleaved views
				//
				var vid = this.Host.Screen.vid;
				var rdef = this.Host.RenderContext.RefDef;

				vid.rowbytes <<= 1;
				vid.aspect *= 0.5f;

				rdef.viewangles.Y -= this.Host.Cvars.LcdYaw.Get<float>();
				rdef.vieworg -= this._Right * this.Host.Cvars.LcdX.Get<float>();

				this.Host.RenderContext.RenderView();

				// ???????? vid.buffer += vid.rowbytes>>1;

				this.Host.RenderContext.PushDlights();

				rdef.viewangles.Y += this.Host.Cvars.LcdYaw.Get<float>() * 2;
				rdef.vieworg += this._Right * this.Host.Cvars.LcdX.Get<float>() * 2;

				this.Host.RenderContext.RenderView();

				// ????????? vid.buffer -= vid.rowbytes>>1;

				rdef.vrect.height <<= 1;

				vid.rowbytes >>= 1;
				vid.aspect *= 2;
			}
			else
				this.Host.RenderContext.RenderView();
		}

		/// <summary>
		/// V_CalcRoll
		/// Used by view and sv_user
		/// </summary>
		public float CalcRoll( ref Vector3 angles, ref Vector3 velocity )
		{
			MathLib.AngleVectors( ref angles, out this._Forward, out this._Right, out this._Up );
			var side = Vector3.Dot( velocity, this._Right );
			float sign = side < 0 ? -1 : 1;
			side = Math.Abs( side );

			var value = this.Host.Cvars.ClRollAngle.Get<float>();
			if ( side < this.Host.Cvars.ClRollSpeed.Get<float>() )
				side = side * value / this.Host.Cvars.ClRollSpeed.Get<float>();
			else
				side = value;

			return side * sign;
		}

		// V_UpdatePalette
		public void UpdatePalette( )
		{
			this.CalcPowerupCshift();

			var isnew = false;

			var cl = this.Host.Client.cl;
			for ( var i = 0; i < ColorShift.NUM_CSHIFTS; i++ )
			{
				if ( cl.cshifts[i].percent != cl.prev_cshifts[i].percent )
				{
					isnew = true;
					cl.prev_cshifts[i].percent = cl.cshifts[i].percent;
				}
				for ( var j = 0; j < 3; j++ )
				{
					if ( cl.cshifts[i].destcolor[j] != cl.prev_cshifts[i].destcolor[j] )
					{
						isnew = true;
						cl.prev_cshifts[i].destcolor[j] = cl.cshifts[i].destcolor[j];
					}
				}
			}

			// drop the damage value
			cl.cshifts[ColorShift.CSHIFT_DAMAGE].percent -= ( int ) (this.Host.FrameTime * 150 );
			if ( cl.cshifts[ColorShift.CSHIFT_DAMAGE].percent < 0 )
				cl.cshifts[ColorShift.CSHIFT_DAMAGE].percent = 0;

			// drop the bonus value
			cl.cshifts[ColorShift.CSHIFT_BONUS].percent -= ( int ) (this.Host.FrameTime * 100 );
			if ( cl.cshifts[ColorShift.CSHIFT_BONUS].percent < 0 )
				cl.cshifts[ColorShift.CSHIFT_BONUS].percent = 0;

			var force = this.CheckGamma();
			if ( !isnew && !force )
				return;

			this.CalcBlend();

			var a = (float)this.Blend.A / byte.MaxValue;
			var r = this.Blend.R * a;
			var g = this.Blend.G * a;
			var b = this.Blend.B * a;

			a = (byte)(1 - a);
			for ( var i = 0; i < 256; i++ )
			{
				var ir = ( int ) ( i * a + r );
				var ig = ( int ) ( i * a + g );
				var ib = ( int ) ( i * a + b );
				if ( ir > 255 )
					ir = 255;
				if ( ig > 255 )
					ig = 255;
				if ( ib > 255 )
					ib = 255;

				this._Ramps[0, i] = this._GammaTable[ir];
				this._Ramps[1, i] = this._GammaTable[ig];
				this._Ramps[2, i] = this._GammaTable[ib];
			}

			var basepal = this.Host.BasePal;
			var offset = 0;
			var newpal = new byte[768];

			for ( var i = 0; i < 256; i++ )
			{
				int ir = basepal[offset + 0];
				int ig = basepal[offset + 1];
				int ib = basepal[offset + 2];

				newpal[offset + 0] = this._Ramps[0, ir];
				newpal[offset + 1] = this._Ramps[1, ig];
				newpal[offset + 2] = this._Ramps[2, ib];

				offset += 3;
			}

			this.ShiftPalette( newpal );
		}

		// V_StartPitchDrift
		public void StartPitchDrift( CommandMessage msg )
		{
			var cl = this.Host.Client.cl;
			if ( cl.laststop == cl.time )
				return; // something else is keeping it from drifting

			if ( cl.nodrift || cl.pitchvel == 0 )
			{
				cl.pitchvel = this.Host.Cvars.CenterSpeed.Get<float>();
				cl.nodrift = false;
				cl.driftmove = 0;
			}
		}

		// V_StopPitchDrift
		public void StopPitchDrift( )
		{
			var cl = this.Host.Client.cl;
			cl.laststop = cl.time;
			cl.nodrift = true;
			cl.pitchvel = 0;
		}

		/// <summary>
		/// V_CalcBlend
		/// </summary>
		public void CalcBlend( )
		{
			float r = 0;
			float g = 0;
			float b = 0;
			float a = 0;

			var cshifts = this.Host.Client.cl.cshifts;

			if (this.Host.Cvars.glCShiftPercent.Get<float>() != 0 )
			{
				for ( var j = 0; j < ColorShift.NUM_CSHIFTS; j++ )
				{
					var a2 = cshifts[j].percent * this.Host.Cvars.glCShiftPercent.Get<float>() / 100.0f / 255.0f;

					if ( a2 == 0 )
						continue;

					a = a + a2 * ( 1 - a );

					a2 = a2 / a;
					r = r * ( 1 - a2 ) + cshifts[j].destcolor[0] * a2;
					g = g * ( 1 - a2 ) + cshifts[j].destcolor[1] * a2;
					b = b * ( 1 - a2 ) + cshifts[j].destcolor[2] * a2;
				}
			}

			this.Blend = Color.FromArgb((byte)Math.Clamp(a, byte.MinValue, byte.MaxValue), (byte)r, (byte)g, (byte)b);
		}

		// V_ParseDamage
		public void ParseDamage( )
		{
			var armor = this.Host.Network.Reader.ReadByte();
			var blood = this.Host.Network.Reader.ReadByte();
			var from = this.Host.Network.Reader.ReadCoords();

			var count = blood * 0.5f + armor * 0.5f;
			if ( count < 10 )
				count = 10;

			var cl = this.Host.Client.cl;
			cl.faceanimtime = ( float ) cl.time + 0.2f; // put sbar face into pain frame

			cl.cshifts[ColorShift.CSHIFT_DAMAGE].percent += ( int ) ( 3 * count );
			if ( cl.cshifts[ColorShift.CSHIFT_DAMAGE].percent < 0 )
				cl.cshifts[ColorShift.CSHIFT_DAMAGE].percent = 0;
			if ( cl.cshifts[ColorShift.CSHIFT_DAMAGE].percent > 150 )
				cl.cshifts[ColorShift.CSHIFT_DAMAGE].percent = 150;

			if ( armor > blood )
			{
				cl.cshifts[ColorShift.CSHIFT_DAMAGE].destcolor[0] = 200;
				cl.cshifts[ColorShift.CSHIFT_DAMAGE].destcolor[1] = 100;
				cl.cshifts[ColorShift.CSHIFT_DAMAGE].destcolor[2] = 100;
			}
			else if ( armor != 0 )
			{
				cl.cshifts[ColorShift.CSHIFT_DAMAGE].destcolor[0] = 220;
				cl.cshifts[ColorShift.CSHIFT_DAMAGE].destcolor[1] = 50;
				cl.cshifts[ColorShift.CSHIFT_DAMAGE].destcolor[2] = 50;
			}
			else
			{
				cl.cshifts[ColorShift.CSHIFT_DAMAGE].destcolor[0] = 255;
				cl.cshifts[ColorShift.CSHIFT_DAMAGE].destcolor[1] = 0;
				cl.cshifts[ColorShift.CSHIFT_DAMAGE].destcolor[2] = 0;
			}

			//
			// calculate view angle kicks
			//
			var ent = this.Host.Client.Entities[cl.viewentity];

			from -= ent.origin; //  VectorSubtract (from, ent->origin, from);
			MathLib.Normalize( ref from );

			Vector3 forward, right, up;
			MathLib.AngleVectors( ref ent.angles, out forward, out right, out up );

			var side = Vector3.Dot( from, right );

			this._DmgRoll = count * side * this.Host.Cvars.KickRoll.Get<float>();

			side = Vector3.Dot( from, forward );
			this._DmgPitch = count * side * this.Host.Cvars.KickPitch.Get<float>();

			this._DmgTime = this.Host.Cvars.KickTime.Get<float>();
		}

		/// <summary>
		/// V_SetContentsColor
		/// Underwater, lava, etc each has a color shift
		/// </summary>
		public void SetContentsColor( int contents )
		{
			switch ( ( Q1Contents ) contents )
			{
				case Q1Contents.Empty:
				case Q1Contents.Solid:
					this.Host.Client.cl.cshifts[ColorShift.CSHIFT_CONTENTS] = this._CShift_empty;
					break;

				case Q1Contents.Lava:
					this.Host.Client.cl.cshifts[ColorShift.CSHIFT_CONTENTS] = this._CShift_lava;
					break;

				case Q1Contents.Slime:
					this.Host.Client.cl.cshifts[ColorShift.CSHIFT_CONTENTS] = this._CShift_slime;
					break;

				default:
					this.Host.Client.cl.cshifts[ColorShift.CSHIFT_CONTENTS] = this._CShift_water;
					break;
			}
		}

		// BuildGammaTable
		private void BuildGammaTable( float g )
		{
			if ( g == 1.0f )
			{
				for ( var i = 0; i < 256; i++ )
					this._GammaTable[i] = ( byte ) i;
			}
			else
			{
				for ( var i = 0; i < 256; i++ )
				{
					var inf = ( int ) ( 255 * Math.Pow( ( i + 0.5 ) / 255.5, g ) + 0.5 );
					if ( inf < 0 )
						inf = 0;
					if ( inf > 255 )
						inf = 255;

					this._GammaTable[i] = ( byte ) inf;
				}
			}
		}

		// V_cshift_f
		private void CShift_f( CommandMessage msg )
		{
			int.TryParse( msg.Parameters[0], out this._CShift_empty.destcolor[0] );
			int.TryParse( msg.Parameters[1], out this._CShift_empty.destcolor[1] );
			int.TryParse( msg.Parameters[2], out this._CShift_empty.destcolor[2] );
			int.TryParse( msg.Parameters[3], out this._CShift_empty.percent );
		}

		// V_BonusFlash_f
		//
		// When you run over an item, the server sends this command
		private void BonusFlash_f( CommandMessage msg )
		{
			var cl = this.Host.Client.cl;
			cl.cshifts[ColorShift.CSHIFT_BONUS].destcolor[0] = 215;
			cl.cshifts[ColorShift.CSHIFT_BONUS].destcolor[1] = 186;
			cl.cshifts[ColorShift.CSHIFT_BONUS].destcolor[2] = 69;
			cl.cshifts[ColorShift.CSHIFT_BONUS].percent = 50;
		}

		// V_CalcIntermissionRefdef
		private void CalcIntermissionRefDef( )
		{
			// ent is the player model (visible when out of body)
			var ent = this.Host.Client.ViewEntity;

			// view is the weapon model (only visible from inside body)
			var view = this.Host.Client.ViewEnt;

			var rdef = this.Host.RenderContext.RefDef;
			rdef.vieworg = ent.origin;
			rdef.viewangles = ent.angles;
			view.model = null;

			// allways idle in intermission
			this.AddIdle( 1 );
		}

		// V_CalcRefdef
		private void CalcRefDef( )
		{
			this.DriftPitch();

			// ent is the player model (visible when out of body)
			var ent = this.Host.Client.ViewEntity;
			// view is the weapon model (only visible from inside body)
			var view = this.Host.Client.ViewEnt;

			// transform the view offset by the model's matrix to get the offset from
			// model origin for the view
			ent.angles.Y = this.Host.Client.cl.viewangles.Y; // the model should face the view dir
			ent.angles.X = -this.Host.Client.cl.viewangles.X;    // the model should face the view dir

			var bob = this.CalcBob();

			var rdef = this.Host.RenderContext.RefDef;
			var cl = this.Host.Client.cl;

			// refresh position
			rdef.vieworg = ent.origin;
			rdef.vieworg.Z += cl.viewheight + bob;

			// never let it sit exactly on a node line, because a water plane can
			// dissapear when viewed with the eye exactly on it.
			// the server protocol only specifies to 1/16 pixel, so add 1/32 in each axis
			rdef.vieworg += View.SmallOffset;
			rdef.viewangles = cl.viewangles;

			this.CalcViewRoll();
			this.AddIdle(this.Host.Cvars.IdleScale.Get<float>() );

			// offsets
			var angles = ent.angles;
			angles.X = -angles.X; // because entity pitches are actually backward

			Vector3 forward, right, up;
			MathLib.AngleVectors( ref angles, out forward, out right, out up );

			rdef.vieworg += forward * this.Host.Cvars.ScrOfsX.Get<float>() + right * this.Host.Cvars.ScrOfsY.Get<float>() + up * this.Host.Cvars.ScrOfsZ.Get<float>();

			this.BoundOffsets();

			// set up gun position
			view.angles = cl.viewangles;

			this.CalcGunAngle();

			view.origin = ent.origin;
			view.origin.Z += cl.viewheight;
			view.origin += forward * bob * 0.4f;
			view.origin.Z += bob;

			// fudge position around to keep amount of weapon visible
			// roughly equal with different FOV
			var viewSize = this.Host.Screen.ViewSize.Get<float>(); // scr_viewsize

			if ( viewSize == 110 )
				view.origin.Z += 1;
			else if ( viewSize == 100 )
				view.origin.Z += 2;
			else if ( viewSize == 90 )
				view.origin.Z += 1;
			else if ( viewSize == 80 )
				view.origin.Z += 0.5f;

			view.model = cl.model_precache[cl.stats[QStatsDef.STAT_WEAPON]];
			view.frame = cl.stats[QStatsDef.STAT_WEAPONFRAME];
			view.colormap = this.Host.Screen.vid.colormap;

			// set up the refresh position
			rdef.viewangles += cl.punchangle;

			// smooth out stair step ups
			if ( cl.onground && ent.origin.Z - this._OldZ > 0 )
			{
				var steptime = ( float ) ( cl.time - cl.oldtime );
				if ( steptime < 0 )
					steptime = 0;

				this._OldZ += steptime * 80;
				if (this._OldZ > ent.origin.Z )
					this._OldZ = ent.origin.Z;
				if ( ent.origin.Z - this._OldZ > 12 )
					this._OldZ = ent.origin.Z - 12;
				rdef.vieworg.Z += this._OldZ - ent.origin.Z;
				view.origin.Z += this._OldZ - ent.origin.Z;
			}
			else
				this._OldZ = ent.origin.Z;

			if (this.Host.ChaseView.IsActive )
				this.Host.ChaseView.Update();
		}

		// V_AddIdle
		//
		// Idle swaying
		private void AddIdle( float idleScale )
		{
			var time = this.Host.Client.cl.time;
			var v = new Vector3(
				( float ) ( Math.Sin( time * this.Host.Cvars.IPitchCycle.Get<float>() ) * this.Host.Cvars.IPitchLevel.Get<float>() ),
				( float ) ( Math.Sin( time * this.Host.Cvars.IYawCycle.Get<float>() ) * this.Host.Cvars.IYawLevel.Get<float>() ),
				( float ) ( Math.Sin( time * this.Host.Cvars.IRollCycle.Get<float>() ) * this.Host.Cvars.IRollLevel.Get<float>() ) );

			this.Host.RenderContext.RefDef.viewangles += v * idleScale;
		}

		// V_DriftPitch
		//
		// Moves the client pitch angle towards cl.idealpitch sent by the server.
		//
		// If the user is adjusting pitch manually, either with lookup/lookdown,
		// mlook and mouse, or klook and keyboard, pitch drifting is constantly stopped.
		//
		// Drifting is enabled when the center view key is hit, mlook is released and
		// lookspring is non 0, or when
		private void DriftPitch( )
		{
			var cl = this.Host.Client.cl;
			if (this.Host.NoClipAngleHack || !cl.onground || this.Host.Client.cls.demoplayback )
			{
				cl.driftmove = 0;
				cl.pitchvel = 0;
				return;
			}

			// don't count small mouse motion
			if ( cl.nodrift )
			{
				if ( Math.Abs( cl.cmd.forwardmove ) < this.Host.Client.ForwardSpeed )
					cl.driftmove = 0;
				else
					cl.driftmove += ( float )this.Host.FrameTime;

				if ( cl.driftmove > this.Host.Cvars.CenterMove.Get<float>() )
					this.StartPitchDrift( null );

				return;
			}

			var delta = cl.idealpitch - cl.viewangles.X;
			if ( delta == 0 )
			{
				cl.pitchvel = 0;
				return;
			}

			var move = ( float )this.Host.FrameTime * cl.pitchvel;
			cl.pitchvel += ( float )this.Host.FrameTime * this.Host.Cvars.CenterSpeed.Get<float>();

			if ( delta > 0 )
			{
				if ( move > delta )
				{
					cl.pitchvel = 0;
					move = delta;
				}
				cl.viewangles.X += move;
			}
			else if ( delta < 0 )
			{
				if ( move > -delta )
				{
					cl.pitchvel = 0;
					move = -delta;
				}
				cl.viewangles.X -= move;
			}
		}

		// V_CalcBob
		private float CalcBob( )
		{
			var cl = this.Host.Client.cl;
			var bobCycle = this.Host.Cvars.ClBobCycle.Get<float>();
			var bobUp = this.Host.Cvars.ClBobUp.Get<float>();
			var cycle = ( float ) ( cl.time - ( int ) ( cl.time / bobCycle ) * bobCycle );
			cycle /= bobCycle;
			if ( cycle < bobUp )
				cycle = ( float ) Math.PI * cycle / bobUp;
			else
				cycle = ( float ) ( Math.PI + Math.PI * ( cycle - bobUp ) / ( 1.0 - bobUp ) );

			// bob is proportional to velocity in the xy plane
			// (don't count Z, or jumping messes it up)
			var tmp = new Vector2(cl.velocity.X, cl.velocity.Y);
			double bob = tmp.Length() * this.Host.Cvars.ClBob.Get<float>();
			bob = bob * 0.3 + bob * 0.7 * Math.Sin( cycle );
			if ( bob > 4 )
				bob = 4;
			else if ( bob < -7 )
				bob = -7;
			return ( float ) bob;
		}

		// V_CalcViewRoll
		//
		// Roll is induced by movement and damage
		private void CalcViewRoll( )
		{
			var cl = this.Host.Client.cl;
			var rdef = this.Host.RenderContext.RefDef;
			var side = this.CalcRoll( ref this.Host.Client.ViewEntity.angles, ref cl.velocity );
			rdef.viewangles.Z += side;

			if (this._DmgTime > 0 )
			{
				rdef.viewangles.Z += this._DmgTime / this.Host.Cvars.KickTime.Get<float>() * this._DmgRoll;
				rdef.viewangles.X += this._DmgTime / this.Host.Cvars.KickTime.Get<float>() * this._DmgPitch;
				this._DmgTime -= ( float )this.Host.FrameTime;
			}

			if ( cl.stats[QStatsDef.STAT_HEALTH] <= 0 )
			{
				rdef.viewangles.Z = 80; // dead view angle
				return;
			}
		}

		// V_BoundOffsets
		private void BoundOffsets( )
		{
			var ent = this.Host.Client.ViewEntity;

			// absolutely bound refresh reletive to entity clipping hull
			// so the view can never be inside a solid wall
			var rdef = this.Host.RenderContext.RefDef;
			if ( rdef.vieworg.X < ent.origin.X - 14 )
				rdef.vieworg.X = ent.origin.X - 14;
			else if ( rdef.vieworg.X > ent.origin.X + 14 )
				rdef.vieworg.X = ent.origin.X + 14;

			if ( rdef.vieworg.Y < ent.origin.Y - 14 )
				rdef.vieworg.Y = ent.origin.Y - 14;
			else if ( rdef.vieworg.Y > ent.origin.Y + 14 )
				rdef.vieworg.Y = ent.origin.Y + 14;

			if ( rdef.vieworg.Z < ent.origin.Z - 22 )
				rdef.vieworg.Z = ent.origin.Z - 22;
			else if ( rdef.vieworg.Z > ent.origin.Z + 30 )
				rdef.vieworg.Z = ent.origin.Z + 30;
		}

		/// <summary>
		/// CalcGunAngle
		/// </summary>
		private void CalcGunAngle( )
		{
			var rdef = this.Host.RenderContext.RefDef;
			var yaw = rdef.viewangles.Y;
			var pitch = -rdef.viewangles.X;

			yaw = this.AngleDelta( yaw - rdef.viewangles.Y ) * 0.4f;
			if ( yaw > 10 )
				yaw = 10;
			if ( yaw < -10 )
				yaw = -10;
			pitch = this.AngleDelta( -pitch - rdef.viewangles.X ) * 0.4f;
			if ( pitch > 10 )
				pitch = 10;
			if ( pitch < -10 )
				pitch = -10;
			var move = ( float )this.Host.FrameTime * 20;
			if ( yaw > this._OldYaw )
			{
				if (this._OldYaw + move < yaw )
					yaw = this._OldYaw + move;
			}
			else
			{
				if (this._OldYaw - move > yaw )
					yaw = this._OldYaw - move;
			}

			if ( pitch > this._OldPitch )
			{
				if (this._OldPitch + move < pitch )
					pitch = this._OldPitch + move;
			}
			else
			{
				if (this._OldPitch - move > pitch )
					pitch = this._OldPitch - move;
			}

			this._OldYaw = yaw;
			this._OldPitch = pitch;

			var cl = this.Host.Client.cl;
			cl.viewent.angles.Y = rdef.viewangles.Y + yaw;
			cl.viewent.angles.X = -( rdef.viewangles.X + pitch );

			var idleScale = this.Host.Cvars.IdleScale.Get<float>();
			cl.viewent.angles.Z -= ( float ) ( idleScale * Math.Sin( cl.time * this.Host.Cvars.IRollCycle.Get<float>() ) * this.Host.Cvars.IRollLevel.Get<float>() );
			cl.viewent.angles.X -= ( float ) ( idleScale * Math.Sin( cl.time * this.Host.Cvars.IPitchCycle.Get<float>() ) * this.Host.Cvars.IPitchLevel.Get<float>() );
			cl.viewent.angles.Y -= ( float ) ( idleScale * Math.Sin( cl.time * this.Host.Cvars.IYawCycle.Get<float>() ) * this.Host.Cvars.IYawLevel.Get<float>() );
		}

		// angledelta()
		private float AngleDelta( float a )
		{
			a = MathLib.AngleMod( a );
			if ( a > 180 )
				a -= 360;
			return a;
		}

		// V_CalcPowerupCshift
		private void CalcPowerupCshift( )
		{
			var cl = this.Host.Client.cl;
			if ( cl.HasItems( QItemsDef.IT_QUAD ) )
			{
				cl.cshifts[ColorShift.CSHIFT_POWERUP].destcolor[0] = 0;
				cl.cshifts[ColorShift.CSHIFT_POWERUP].destcolor[1] = 0;
				cl.cshifts[ColorShift.CSHIFT_POWERUP].destcolor[2] = 255;
				cl.cshifts[ColorShift.CSHIFT_POWERUP].percent = 30;
			}
			else if ( cl.HasItems( QItemsDef.IT_SUIT ) )
			{
				cl.cshifts[ColorShift.CSHIFT_POWERUP].destcolor[0] = 0;
				cl.cshifts[ColorShift.CSHIFT_POWERUP].destcolor[1] = 255;
				cl.cshifts[ColorShift.CSHIFT_POWERUP].destcolor[2] = 0;
				cl.cshifts[ColorShift.CSHIFT_POWERUP].percent = 20;
			}
			else if ( cl.HasItems( QItemsDef.IT_INVISIBILITY ) )
			{
				cl.cshifts[ColorShift.CSHIFT_POWERUP].destcolor[0] = 100;
				cl.cshifts[ColorShift.CSHIFT_POWERUP].destcolor[1] = 100;
				cl.cshifts[ColorShift.CSHIFT_POWERUP].destcolor[2] = 100;
				cl.cshifts[ColorShift.CSHIFT_POWERUP].percent = 100;
			}
			else if ( cl.HasItems( QItemsDef.IT_INVULNERABILITY ) )
			{
				cl.cshifts[ColorShift.CSHIFT_POWERUP].destcolor[0] = 255;
				cl.cshifts[ColorShift.CSHIFT_POWERUP].destcolor[1] = 255;
				cl.cshifts[ColorShift.CSHIFT_POWERUP].destcolor[2] = 0;
				cl.cshifts[ColorShift.CSHIFT_POWERUP].percent = 30;
			}
			else
				cl.cshifts[ColorShift.CSHIFT_POWERUP].percent = 0;
		}

		// V_CheckGamma
		private bool CheckGamma( )
		{
			if (this.Host.Cvars.Gamma.Get<float>() == this._OldGammaValue )
				return false;

			this._OldGammaValue = this.Host.Cvars.Gamma.Get<float>();

			this.BuildGammaTable(this.Host.Cvars.Gamma.Get<float>() );
			this.Host.Screen.vid.recalc_refdef = true;   // force a surface cache flush

			return true;
		}

		// VID_ShiftPalette from gl_vidnt.c
		private void ShiftPalette( byte[] palette )
		{
			//	VID_SetPalette (palette);
			//	gammaworks = SetDeviceGammaRamp (maindc, ramps);
		}

		public View( Host host )
		{
			this.Host = host;

			this._GammaTable = new byte[256];

			this._CShift_empty = new( new[] { 130, 80, 50 }, 0 );
			this._CShift_water = new( new[] { 130, 80, 50 }, 128 );
			this._CShift_slime = new( new[] { 0, 25, 5 }, 150 );
			this._CShift_lava = new( new[] { 255, 80, 0 }, 150 );
		}
	}
}
