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



// gl_warp.c

namespace SharpQuake.Game.Rendering.Textures
{
	using Framework;
	using Framework.Definitions;
	using Framework.Mathematics;
	using Memory;
	using Renderer;
	using Renderer.Textures;
	using System.Numerics;

	public class WarpableTextures
	{
		private BaseTexture SolidSkyTexture
		{
			get;
			set;
		}

		private BaseTexture AlphaSkyTexture
		{
			get;
			set;
		}

		private BaseDevice Device
		{
			get;
			set;
		}

		private float SpeedScale
		{
			get;
			set;
		}

		public WarpableTextures( BaseDevice device )
		{
			this.Device = device;
		}

		/// <summary>
		/// R_InitSky
		/// called at level load
		/// A sky texture is 256*128, with the right side being a masked overlay
		/// </summary>
		public void InitSky( ModelTexture mt )
		{
			var src = mt.pixels;
			var offset = mt.offsets[0];

			// make an average value for the back to avoid
			// a fringe on the top level
			const int size = 128 * 128;
			var trans = new uint[size];
			var v8to24 = this.Device.Palette.Table8to24;
			var r = 0;
			var g = 0;
			var b = 0;
			var rgba = Union4b.Empty;
			for ( var i = 0; i < 128; i++ )
				for ( var j = 0; j < 128; j++ )
				{
					int p = src[offset + i * 256 + j + 128];
					rgba.ui0 = v8to24[p];
					trans[i * 128 + j] = rgba.ui0;
					r += rgba.b0;
					g += rgba.b1;
					b += rgba.b2;
				}

			rgba.b0 = ( byte ) ( r / size );
			rgba.b1 = ( byte ) ( g / size );
			rgba.b2 = ( byte ) ( b / size );
			rgba.b3 = 0;

			var transpix = rgba.ui0;

			this.SolidSkyTexture = BaseTexture.FromBuffer(this.Device, "_SolidSkyTexture", trans, 128, 128, false, false, "GL_LINEAR" );

			for ( var i = 0; i < 128; i++ )
				for ( var j = 0; j < 128; j++ )
				{
					int p = src[offset + i * 256 + j];
					if ( p == 0 )
						trans[i * 128 + j] = transpix;
					else
						trans[i * 128 + j] = v8to24[p];
				}

			this.AlphaSkyTexture = BaseTexture.FromBuffer(this.Device, "_AlphaSkyTexture", trans, 128, 128, false, true, "GL_LINEAR" );
		}


		/// <summary>
		/// EmitWaterPolys
		/// Does a water warp on the pre-fragmented glpoly_t chain
		/// </summary>
		public void EmitWaterPolys( double realTime, MemorySurface fa )
		{
			this.Device.Graphics.EmitWaterPolys( ref WarpDef._TurbSin, realTime, WarpDef.TURBSCALE, fa.polys );
		}

		/// <summary>
		/// R_DrawSkyChain
		/// </summary>
		public void DrawSkyChain( double realTime, Vector3 origin, MemorySurface s )
		{
			this.Device.DisableMultitexture( );

			this.SolidSkyTexture.Bind( );

			// used when gl_texsort is on
			this.SpeedScale = ( float ) realTime * 8;
			this.SpeedScale -= ( int )this.SpeedScale & ~127;

			for ( var fa = s; fa != null; fa = fa.texturechain )
				this.Device.Graphics.EmitSkyPolys( fa.polys, origin, this.SpeedScale );

			this.AlphaSkyTexture.Bind( );
			this.SpeedScale = ( float ) realTime * 16;
			this.SpeedScale -= ( int )this.SpeedScale & ~127;

			for ( var fa = s; fa != null; fa = fa.texturechain )
				this.Device.Graphics.EmitSkyPolys( fa.polys, origin, this.SpeedScale, true );
		}

		/// <summary>
		/// EmitBothSkyLayers
		/// Does a sky warp on the pre-fragmented glpoly_t chain
		/// This will be called for brushmodels, the world
		/// will have them chained together.
		/// </summary>
		public void EmitBothSkyLayers( double realTime, Vector3 origin, MemorySurface fa )
		{
			this.Device.DisableMultitexture( );

			this.SolidSkyTexture.Bind( );
			this.SpeedScale = ( float ) realTime * 8;
			this.SpeedScale -= ( int )this.SpeedScale & ~127;

			this.Device.Graphics.EmitSkyPolys( fa.polys, origin, this.SpeedScale );

			this.AlphaSkyTexture.Bind( );
			this.SpeedScale = ( float ) realTime * 16;
			this.SpeedScale -= ( int )this.SpeedScale & ~127;

			this.Device.Graphics.EmitSkyPolys( fa.polys, origin, this.SpeedScale, true );
		}
	}
}
