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



// gl_rsurf.c

namespace SharpQuake.Rendering
{
	using Framework.Data;
	using Framework.Definitions;
	using Framework.Definitions.Bsp;
	using Framework.Engine;
	using Framework.IO.BSP.Q1;
	using Framework.Mathematics;
	using Framework.Rendering;
	using Game.Data.Models;
	using Game.Rendering.Memory;
	using Game.Rendering.Textures;
	using Game.World;
	using Renderer;
	using Renderer.OpenGL.Textures;
	using Renderer.Textures;
	using System;
	using System.Linq;
	using System.Numerics;

	partial class render
	{
		private const double COLINEAR_EPSILON = 0.001;

		//private Int32 _LightMapTextures; // lightmap_textures
		private int _LightMapBytes; // lightmap_bytes		// 1, 2, or 4
		private MemoryVertex[] _CurrentVertBase; // r_pcurrentvertbase
		private ModelData _CurrentModel; // currentmodel
										 //private System.Boolean[] _LightMapModified = new System.Boolean[RenderDef.MAX_LIGHTMAPS]; // lightmap_modified
		private GLPoly[] _LightMapPolys = new GLPoly[RenderDef.MAX_LIGHTMAPS]; // lightmap_polys
																			   //private glRect_t[] _LightMapRectChange = new glRect_t[RenderDef.MAX_LIGHTMAPS]; // lightmap_rectchange
		private uint[] _BlockLights = new uint[18 * 18]; // blocklights
		private int _ColinElim; // nColinElim
		
		
		private Entity _TempEnt = new( ); // for DrawWorld

		// the lightmap texture data needs to be kept in
		// main memory so texsubimage can update properly
		private byte[] _LightMaps = new byte[4 * RenderDef.MAX_LIGHTMAPS * RenderDef.BLOCK_WIDTH * RenderDef.BLOCK_HEIGHT]; // lightmaps

		private BaseTexture LightMapTexture
		{
			get;
			set;
		}

		protected TextureChains TextureChains
		{
			get;
			set;
		}

		/// <summary>
		/// GL_BuildLightmaps
		/// Builds the lightmap texture with all the surfaces from all brush models
		/// </summary>
		private void BuildLightMaps( )
		{
			if (this.LightMapTexture != null )
				Array.Clear(this.LightMapTexture.LightMapData, 0, this.LightMapTexture.LightMapData.Length );
			//memset (allocated, 0, sizeof(allocated));

			this._FrameCount = 1;        // no dlightcache

			//if( _LightMapTextures == 0 )
			//   _LightMapTextures = Host.DrawingContext.GenerateTextureNumberRange( RenderDef.MAX_LIGHTMAPS );

			this.Host.DrawingContext.LightMapFormat = "GL_LUMINANCE";

			// default differently on the Permedia
			if (this.Host.Screen.IsPermedia )
				this.Host.DrawingContext.LightMapFormat = "GL_RGBA";

			if ( CommandLine.HasParam( "-lm_1" ) )
				this.Host.DrawingContext.LightMapFormat = "GL_LUMINANCE";

			if ( CommandLine.HasParam( "-lm_a" ) )
				this.Host.DrawingContext.LightMapFormat = "GL_ALPHA";

			//if (CommandLine.HasParam("-lm_i"))
			//    Host.DrawingContext.LightMapFormat = PixelFormat.Intensity;

			//if (CommandLine.HasParam("-lm_2"))
			//    Host.DrawingContext.LightMapFormat = PixelFormat.Rgba4;

			if ( CommandLine.HasParam( "-lm_4" ) )
				this.Host.DrawingContext.LightMapFormat = "GL_RGBA";

			switch (this.Host.DrawingContext.LightMapFormat )
			{
				case "GL_RGBA":
					this._LightMapBytes = 4;
					break;

				//case PixelFormat.Rgba4:
				//_LightMapBytes = 2;
				//break;

				case "GL_LUMINANCE":
				//case PixelFormat.Intensity:
				case "GL_ALPHA":
					this._LightMapBytes = 1;
					break;
			}

			var tempBuffer = new int[RenderDef.MAX_LIGHTMAPS, RenderDef.BLOCK_WIDTH];
			var brushes = this.Host.Client.cl.model_precache.Where( m => m is BrushModelData ).ToArray( );

			//for ( var j = 1; j < QDef.MAX_MODELS; j++ )
			for ( var j = 0; j < brushes.Length; j++ )
			{
				var m = ( BrushModelData ) brushes[j];
				if ( m == null )
					break;

				if ( m.Name != null && m.Name.StartsWith( "*" ) )
					continue;

				this._CurrentVertBase = m.Vertices;
				this._CurrentModel = m;
				for ( var i = 0; i < m.NumSurfaces; i++ )
				{
					this.CreateSurfaceLightmap( ref tempBuffer, m.Surfaces[i] );
					if ( ( m.Surfaces[i].flags & ( int ) Q1SurfaceFlags.Turbulence ) != 0 )
						continue;

					if ( ( m.Surfaces[i].flags & ( int ) Q1SurfaceFlags.Sky ) != 0 )
						continue;

					this.BuildSurfaceDisplayList( m.Surfaces[i] );
				}
			}

			if ( !this.Host.Cvars.glTexSort.Get<bool>( ) )
				this.Host.DrawingContext.SelectTexture( MTexTarget.TEXTURE1_SGIS );

			this.LightMapTexture = BaseTexture.FromBuffer(this.Host.Video.Device, "_Lightmaps", new ByteArraySegment(this._LightMaps ), 128, 128, false, false, isLightMap: true );

			this.LightMapTexture.Desc.LightMapBytes = this._LightMapBytes;
			this.LightMapTexture.Desc.LightMapFormat = this.Host.DrawingContext.LightMapFormat;

			Array.Copy( tempBuffer, this.LightMapTexture.LightMapData, tempBuffer.Length );

			this.LightMapTexture.UploadLightmap( );

			if ( !this.Host.Cvars.glTexSort.Get<bool>( ) )
				this.Host.DrawingContext.SelectTexture( MTexTarget.TEXTURE0_SGIS );
		}

		/// <summary>
		/// GL_CreateSurfaceLightmap
		/// </summary>
		private void CreateSurfaceLightmap( ref int[,] tempBuffer, MemorySurface surf )
		{
			if ( ( surf.flags & ( ( int ) Q1SurfaceFlags.Sky | ( int ) Q1SurfaceFlags.Turbulence ) ) != 0 )
				return;

			var smax = ( surf.extents[0] >> 4 ) + 1;
			var tmax = ( surf.extents[1] >> 4 ) + 1;

			surf.lightmaptexturenum = this.AllocBlock( ref tempBuffer, smax, tmax, ref surf.light_s, ref surf.light_t );
			var offset = surf.lightmaptexturenum * this._LightMapBytes * RenderDef.BLOCK_WIDTH * RenderDef.BLOCK_HEIGHT;
			offset += ( surf.light_t * RenderDef.BLOCK_WIDTH + surf.light_s ) * this._LightMapBytes;
			this.BuildLightMap( surf, new(this._LightMaps, offset ), RenderDef.BLOCK_WIDTH * this._LightMapBytes );
		}

		/// <summary>
		/// BuildSurfaceDisplayList
		/// </summary>
		private void BuildSurfaceDisplayList( MemorySurface fa )
		{
			var BrushModelData = ( BrushModelData )this._CurrentModel;
			// reconstruct the polygon
			var pedges = BrushModelData.Edges;
			var lnumverts = fa.numedges;

			//
			// draw texture
			//
			var poly = new GLPoly( );
			poly.AllocVerts( lnumverts );
			poly.next = fa.polys;
			poly.flags = fa.flags;
			fa.polys = poly;

			ushort[] r_pedge_v;
			Vector3 vec;

			for ( var i = 0; i < lnumverts; i++ )
			{
				var lindex = BrushModelData.SurfEdges[fa.firstedge + i];
				if ( lindex > 0 )
				{
					r_pedge_v = pedges[lindex].v;
					vec = this._CurrentVertBase[r_pedge_v[0]].position;
				}
				else
				{
					r_pedge_v = pedges[-lindex].v;
					vec = this._CurrentVertBase[r_pedge_v[1]].position;
				}
				var s = MathLib.DotProduct( ref vec, ref fa.texinfo.vecs[0] ) + fa.texinfo.vecs[0].W;
				s /= fa.texinfo.texture.width;

				var t = MathLib.DotProduct( ref vec, ref fa.texinfo.vecs[1] ) + fa.texinfo.vecs[1].W;
				t /= fa.texinfo.texture.height;

				poly.verts[i][0] = vec.X;
				poly.verts[i][1] = vec.Y;
				poly.verts[i][2] = vec.Z;
				poly.verts[i][3] = s;
				poly.verts[i][4] = t;

				//
				// lightmap texture coordinates
				//
				s = MathLib.DotProduct( ref vec, ref fa.texinfo.vecs[0] ) + fa.texinfo.vecs[0].W;
				s -= fa.texturemins[0];
				s += fa.light_s * 16;
				s += 8;
				s /= RenderDef.BLOCK_WIDTH * 16;

				t = MathLib.DotProduct( ref vec, ref fa.texinfo.vecs[1] ) + fa.texinfo.vecs[1].W;
				t -= fa.texturemins[1];
				t += fa.light_t * 16;
				t += 8;
				t /= RenderDef.BLOCK_HEIGHT * 16;

				poly.verts[i][5] = s;
				poly.verts[i][6] = t;
			}

			//
			// remove co-linear points - Ed
			//
			if ( !this.Host.Cvars.glKeepTJunctions.Get<bool>( ) && ( fa.flags & ( int ) Q1SurfaceFlags.Underwater ) == 0 )
			{
				for ( var i = 0; i < lnumverts; ++i )
				{
					if ( Utilities.IsCollinear( poly.verts[( i + lnumverts - 1 ) % lnumverts],
						poly.verts[i],
						poly.verts[( i + 1 ) % lnumverts] ) )
					{
						int j;
						for ( j = i + 1; j < lnumverts; ++j )
						{
							//int k;
							for ( var k = 0; k < ModelDef.VERTEXSIZE; ++k )
								poly.verts[j - 1][k] = poly.verts[j][k];
						}
						--lnumverts;
						++this._ColinElim;
						// retry next vertex next time, which is now current vertex
						--i;
					}
				}
			}
			poly.numverts = lnumverts;
		}

		// returns a texture number and the position inside it
		private int AllocBlock( ref int[,] data, int w, int h, ref int x, ref int y )
		{
			for ( var texnum = 0; texnum < RenderDef.MAX_LIGHTMAPS; texnum++ )
			{
				var best = RenderDef.BLOCK_HEIGHT;

				for ( var i = 0; i < RenderDef.BLOCK_WIDTH - w; i++ )
				{
					int j = 0, best2 = 0;

					for ( j = 0; j < w; j++ )
					{
						if ( data[texnum, i + j] >= best )
							break;
						if ( data[texnum, i + j] > best2 )
							best2 = data[texnum, i + j];
					}

					if ( j == w )
					{
						// this is a valid spot
						x = i;
						y = best = best2;
					}
				}

				if ( best + h > RenderDef.BLOCK_HEIGHT )
					continue;

				for ( var i = 0; i < w; i++ )
					data[texnum, x + i] = best + h;

				return texnum;
			}

			Utilities.Error( "AllocBlock: full" );
			return 0; // shut up compiler
		}

		/// <summary>
		/// R_BuildLightMap
		/// Combine and scale multiple lightmaps into the 8.8 format in blocklights
		/// </summary>
		private void BuildLightMap( MemorySurface surf, ByteArraySegment dest, int stride )
		{
			surf.cached_dlight = surf.dlightframe == this._FrameCount;

			var smax = ( surf.extents[0] >> 4 ) + 1;
			var tmax = ( surf.extents[1] >> 4 ) + 1;
			var size = smax * tmax;

			var srcOffset = surf.sampleofs;
			var lightmap = surf.sample_base;// surf.samples;

			// set to full bright if no light data
			if (this.Host.Cvars.FullBright.Get<bool>( ) || this.Host.Client.cl.worldmodel.LightData == null )
			{
				for ( var i = 0; i < size; i++ )
					this._BlockLights[i] = 255 * 256;
			}
			else
			{
				// clear to no light
				for ( var i = 0; i < size; i++ )
					this._BlockLights[i] = 0;

				// add all the lightmaps
				if ( lightmap != null )
				{
					for ( var maps = 0; maps < BspDef.MAXLIGHTMAPS && surf.styles[maps] != 255; maps++ )
					{
						var scale = this._LightStyleValue[surf.styles[maps]];
						surf.cached_light[maps] = scale;    // 8.8 fraction
						for ( var i = 0; i < size; i++ )
							this._BlockLights[i] += ( uint ) ( lightmap[srcOffset + i] * scale );
						srcOffset += size; // lightmap += size;	// skip to next lightmap
					}
				}

				// add all the dynamic lights
				if ( surf.dlightframe == this._FrameCount )
					this.AddDynamicLights( surf );
			}
			// bound, invert, and shift
			//store:
			var blOffset = 0;
			var destOffset = dest.StartIndex;
			var data = dest.Data;
			switch (this.Host.DrawingContext.LightMapFormat )
			{
				case "GL_RGBA":
					stride -= smax << 2;
					for ( var i = 0; i < tmax; i++, destOffset += stride ) // dest += stride
					{
						for ( var j = 0; j < smax; j++ )
						{
							var t = this._BlockLights[blOffset++];// *bl++;
							t >>= 7;
							if ( t > 255 )
								t = 255;
							data[destOffset + 3] = ( byte ) ( 255 - t ); //dest[3] = 255 - t;
							destOffset += 4;
						}
					}
					break;

				case "GL_ALPHA":
				case "GL_LUMINANCE":
					//case GL_INTENSITY:
					for ( var i = 0; i < tmax; i++, destOffset += stride )
					{
						for ( var j = 0; j < smax; j++ )
						{
							var t = this._BlockLights[blOffset++];// *bl++;
							t >>= 7;
							if ( t > 255 )
								t = 255;
							data[destOffset + j] = ( byte ) ( 255 - t ); // dest[j] = 255 - t;
						}
					}
					break;

				default:
					Utilities.Error( "Bad lightmap format" );
					break;
			}
		}

		/// <summary>
		/// R_AddDynamicLights
		/// </summary>
		private void AddDynamicLights( MemorySurface surf )
		{
			var smax = ( surf.extents[0] >> 4 ) + 1;
			var tmax = ( surf.extents[1] >> 4 ) + 1;
			var tex = surf.texinfo;
			var dlights = this.Host.Client.DLights;

			for ( var lnum = 0; lnum < ClientDef.MAX_DLIGHTS; lnum++ )
			{
				if ( ( surf.dlightbits & ( 1 << lnum ) ) == 0 )
					continue;       // not lit by this light

				var rad = dlights[lnum].radius;
				var dist = Vector3.Dot( dlights[lnum].origin, surf.plane.normal ) - surf.plane.dist;
				rad -= Math.Abs( dist );
				var minlight = dlights[lnum].minlight;
				if ( rad < minlight )
					continue;
				minlight = rad - minlight;

				var impact = dlights[lnum].origin - surf.plane.normal * dist;

				var local0 = Vector3.Dot( impact, new(tex.vecs[0].X, tex.vecs[0].Y, tex.vecs[0].Z) ) + tex.vecs[0].W;
				var local1 = Vector3.Dot( impact, new(tex.vecs[1].X, tex.vecs[1].Y, tex.vecs[1].Z) ) + tex.vecs[1].W;

				local0 -= surf.texturemins[0];
				local1 -= surf.texturemins[1];

				for ( var t = 0; t < tmax; t++ )
				{
					var td = ( int ) ( local1 - t * 16 );
					if ( td < 0 )
						td = -td;
					for ( var s = 0; s < smax; s++ )
					{
						var sd = ( int ) ( local0 - s * 16 );
						if ( sd < 0 )
							sd = -sd;
						if ( sd > td )
							dist = sd + ( td >> 1 );
						else
							dist = td + ( sd >> 1 );
						if ( dist < minlight )
							this._BlockLights[t * smax + s] += ( uint ) ( ( rad - dist ) * 256 );
					}
				}
			}
		}

		/// <summary>
		/// R_DrawWaterSurfaces
		/// </summary>
		private void DrawWaterSurfaces( )
		{
			if (this.Host.Cvars.WaterAlpha.Get<float>( ) == 1.0f && this.Host.Cvars.glTexSort.Get<bool>( ) )
				return;

			//
			// go back to the world matrix
			//
			this.Host.Video.Device.ResetMatrix( );

			// WaterAlpha is broken - will fix when we introduce GLSL...
			//if ( _WaterAlpha.Value < 1.0 )
			//{
			//    GL.Enable( EnableCap.Blend );
			//    GL.Color4( 1, 1, 1, _WaterAlpha.Value );
			//    GL.TexEnv( TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvMode, ( Int32 ) TextureEnvMode.Modulate );
			//}

			if ( !this.Host.Cvars.glTexSort.Get<bool>( ) )
			{
				if (this.TextureChains.WaterChain == null )
					return;

				for ( var s = this.TextureChains.WaterChain; s != null; s = s.texturechain )
				{
					s.texinfo.texture.texture.Bind( );
					this.WarpableTextures.EmitWaterPolys(this.Host.RealTime, s );
				}

				this.TextureChains.WaterChain = null;
			}
			else
			{
				for ( var i = 0; i < this.Host.Client.cl.worldmodel.NumTextures; i++ )
				{
					var t = this.Host.Client.cl.worldmodel.Textures[i];
					if ( t == null )
						continue;

					var s = t.texturechain;
					if ( s == null )
						continue;

					if ( ( s.flags & ( int ) Q1SurfaceFlags.Turbulence ) == 0 )
						continue;

					// set modulate mode explicitly

					t.texture.Bind( );

					for ( ; s != null; s = s.texturechain )
						this.WarpableTextures.EmitWaterPolys(this.Host.RealTime, s );

					t.texturechain = null;
				}
			}

			// WaterAlpha is broken - will fix when we introduce GLSL...
			//if( _WaterAlpha.Value < 1.0 )
			//{
			//    GL.TexEnv( TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvMode, ( Int32 ) TextureEnvMode.Replace );
			//    GL.Color4( 1f, 1, 1, 1 );
			//    GL.Disable( EnableCap.Blend );
			//}
		}		

		/// <summary>
		/// R_DrawWorld
		/// </summary>
		private void DrawWorld( )
		{
			this._TempEnt.Clear( );
			this._TempEnt.model = this.Host.Client.cl.worldmodel;

			this._ModelOrg = this._RefDef.vieworg;
			this._CurrentEntity = this._TempEnt;
			this.Host.DrawingContext.CurrentTexture = -1;

			Array.Clear(this._LightMapPolys, 0, this._LightMapPolys.Length );

			this.RecursiveWorldNode( ( ( BrushModelData )this._TempEnt.model ).Nodes[0] );

			this.DrawTextureChains( );

			this.BlendLightmaps( );
		}

		/// <summary>
		/// R_BlendLightmaps
		/// </summary>
		private void BlendLightmaps( )
		{
			if (this.Host.Cvars.FullBright.Get<bool>( ) )
				return;
			if ( !this.Host.Cvars.glTexSort.Get<bool>( ) )
				return;

			this.Host.Video.Device.Graphics.BeginBlendLightMap( !this.Host.Cvars.LightMap.Get<bool>( ), this.Host.DrawingContext.LightMapFormat );

			for ( var i = 0; i < RenderDef.MAX_LIGHTMAPS; i++ )
			{
				var p = this._LightMapPolys[i];
				if ( p == null )
					continue;

				this.LightMapTexture.BindLightmap( ( ( GLTextureDesc )this.LightMapTexture.Desc ).TextureNumber + i );

				if (this.LightMapTexture.LightMapModified[i] )
					this.CommitLightmap( i );

				for ( ; p != null; p = p.chain )
				{
					if ( ( p.flags & ( int ) Q1SurfaceFlags.Underwater ) != 0 )
						this.Host.Video.Device.Graphics.DrawWaterPolyLightmap( p, this.Host.RealTime );
					else
						this.Host.Video.Device.Graphics.DrawPoly( p, isLightmap: true );
				}
			}

			this.Host.Video.Device.Graphics.EndBlendLightMap( !this.Host.Cvars.LightMap.Get<bool>( ), this.Host.DrawingContext.LightMapFormat );
		}

		private void DrawTextureChains( )
		{
			if ( !this.Host.Cvars.glTexSort.Get<bool>( ) )
			{
				this.Host.Video.Device.DisableMultitexture( );

				if (this.TextureChains.SkyChain != null )
				{
					this.WarpableTextures.DrawSkyChain(this.Host.RealTime, this.Host.RenderContext.Origin, this.TextureChains.SkyChain );
					this.TextureChains.SkyChain = null;
				}
				return;
			}
			var world = this.Host.Client.cl.worldmodel;
			for ( var i = 0; i < world.NumTextures; i++ )
			{
				var t = world.Textures[i];
				if ( t == null )
					continue;

				var s = t.texturechain;
				if ( s == null )
					continue;

				if ( i == this._SkyTextureNum )
					this.WarpableTextures.DrawSkyChain(this.Host.RealTime, this.Host.RenderContext.Origin, s );
				//else if( i == _MirrorTextureNum && _MirrorAlpha.Value != 1.0f )
				//{
				//    MirrorChain( s );
				//    continue;
				//}
				else
				{
					if ( ( s.flags & ( int ) Q1SurfaceFlags.Turbulence ) != 0 && this.Host.Cvars.WaterAlpha.Get<float>( ) != 1.0f )
						continue;   // draw translucent water later
					for ( ; s != null; s = s.texturechain )
						this.RenderBrushPoly( s );
				}

				t.texturechain = null;
			}
		}

		/// <summary>
		/// R_RenderBrushPoly
		/// </summary>
		private void RenderBrushPoly( MemorySurface fa )
		{
			this._BrushPolys++;

			if ( ( fa.flags & ( int ) Q1SurfaceFlags.Sky ) != 0 )
			{   // warp texture, no lightmaps
				this.WarpableTextures.EmitBothSkyLayers(this.Host.RealTime, this.Host.RenderContext.Origin, fa );
				return;
			}

			var t = this.TextureAnimation( fa.texinfo.texture );
			t.texture.Bind( );

			if ( ( fa.flags & ( int ) Q1SurfaceFlags.Turbulence ) != 0 )
			{   // warp texture, no lightmaps
				this.WarpableTextures.EmitWaterPolys(this.Host.RealTime, fa );
				return;
			}

			if ( ( fa.flags & ( int ) Q1SurfaceFlags.Underwater ) != 0 )
				this.Host.Video.Device.Graphics.DrawWaterPoly( fa.polys, this.Host.RealTime );
			else
				this.Host.Video.Device.Graphics.DrawPoly( fa.polys, t.scaleX, t.scaleY );

			// add the poly to the proper lightmap chain

			fa.polys.chain = this._LightMapPolys[fa.lightmaptexturenum];
			this._LightMapPolys[fa.lightmaptexturenum] = fa.polys;

			// check for lightmap modification
			var modified = false;
			for ( var maps = 0; maps < BspDef.MAXLIGHTMAPS && fa.styles[maps] != 255; maps++ )
			{
				if (this._LightStyleValue[fa.styles[maps]] != fa.cached_light[maps] )
				{
					modified = true;
					break;
				}
			}

			if ( modified ||
				fa.dlightframe == this._FrameCount ||    // dynamic this frame
				fa.cached_dlight )          // dynamic previously
			{
				if (this.Host.Cvars.Dynamic.Get<bool>( ) )
				{
					this.LightMapTexture.LightMapModified[fa.lightmaptexturenum] = true;
					this.UpdateRect( fa, ref this.LightMapTexture.LightMapRectChange[fa.lightmaptexturenum] );
					var offset = fa.lightmaptexturenum * this._LightMapBytes * RenderDef.BLOCK_WIDTH * RenderDef.BLOCK_HEIGHT;
					offset += fa.light_t * RenderDef.BLOCK_WIDTH * this._LightMapBytes + fa.light_s * this._LightMapBytes;
					this.BuildLightMap( fa, new(this._LightMaps, offset ), RenderDef.BLOCK_WIDTH * this._LightMapBytes );
				}
			}
		}

		private void UpdateRect( MemorySurface fa, ref glRect_t theRect )
		{
			if ( fa.light_t < theRect.t )
			{
				if ( theRect.h != 0 )
					theRect.h += ( byte ) ( theRect.t - fa.light_t );
				theRect.t = ( byte ) fa.light_t;
			}
			if ( fa.light_s < theRect.l )
			{
				if ( theRect.w != 0 )
					theRect.w += ( byte ) ( theRect.l - fa.light_s );
				theRect.l = ( byte ) fa.light_s;
			}
			var smax = ( fa.extents[0] >> 4 ) + 1;
			var tmax = ( fa.extents[1] >> 4 ) + 1;
			if ( theRect.w + theRect.l < fa.light_s + smax )
				theRect.w = ( byte ) ( fa.light_s - theRect.l + smax );
			if ( theRect.h + theRect.t < fa.light_t + tmax )
				theRect.h = ( byte ) ( fa.light_t - theRect.t + tmax );
		}

		/// <summary>
		/// R_MirrorChain
		/// </summary>
		//private void MirrorChain( MemorySurface s )
		//{
		//    if( _IsMirror )
		//        return;
		//    _IsMirror = true;
		//    _MirrorPlane = s.plane;
		//}

		/// <summary>
		/// R_RecursiveWorldNode
		/// </summary>
		private void RecursiveWorldNode( MemoryNodeBase node )
		{
			this.Occlusion.RecursiveWorldNode( node,
				this._ModelOrg,
				this._FrameCount, ref this._Frustum, ( surf ) => 
			{
				this.DrawSequentialPoly( surf );
			}, ( efrags ) => 
			{
				this.StoreEfrags( efrags );
			} );
		}

		/// <summary>
		/// R_DrawSequentialPoly
		/// Systems that have fast state and texture changes can
		/// just do everything as it passes with no need to sort
		/// </summary>
		private void DrawSequentialPoly( MemorySurface s )
		{
			//
			// normal lightmaped poly
			//
			if ( ( s.flags & ( ( int ) Q1SurfaceFlags.Sky | ( int ) Q1SurfaceFlags.Turbulence | ( int ) Q1SurfaceFlags.Underwater ) ) == 0 )
			{
				this.RenderDynamicLightmaps( s );
				var p = s.polys;
				var t = this.TextureAnimation( s.texinfo.texture );
				if (this.Host.Video.Device.Desc.SupportsMultiTexture )
				{
					this.Host.Video.Device.Graphics.DrawSequentialPolyMultiTexture( t.texture, this.LightMapTexture, this._LightMaps, p, s.lightmaptexturenum );
					return;
				}
				else
					this.Host.Video.Device.Graphics.DrawSequentialPoly( t.texture, this.LightMapTexture, p, s.lightmaptexturenum );

				return;
			}

			//
			// subdivided water surface warp
			//

			if ( ( s.flags & ( int ) Q1SurfaceFlags.Turbulence ) != 0 )
			{
				this.Host.Video.Device.DisableMultitexture( );
				s.texinfo.texture.texture.Bind( );
				this.WarpableTextures.EmitWaterPolys(this.Host.RealTime, s );
				return;
			}

			//
			// subdivided sky warp
			//
			if ( ( s.flags & ( int ) Q1SurfaceFlags.Sky ) != 0 )
			{
				this.WarpableTextures.EmitBothSkyLayers(this.Host.RealTime, this.Host.RenderContext.Origin, s );
				return;
			}

			//
			// underwater warped with lightmap
			//
			this.RenderDynamicLightmaps( s );
			if (this.Host.Video.Device.Desc.SupportsMultiTexture )
			{
				var t = this.TextureAnimation( s.texinfo.texture );

				this.Host.DrawingContext.SelectTexture( MTexTarget.TEXTURE0_SGIS );

				this.Host.Video.Device.Graphics.DrawWaterPolyMultiTexture(this._LightMaps, t.texture, this.LightMapTexture, s.lightmaptexturenum, s.polys, this.Host.RealTime );
			}
			else
			{
				var p = s.polys;

				var t = this.TextureAnimation( s.texinfo.texture );
				t.texture.Bind( );
				this.Host.Video.Device.Graphics.DrawWaterPoly( p, this.Host.RealTime );

				this.LightMapTexture.BindLightmap( ( ( GLTextureDesc )this.LightMapTexture.Desc ).TextureNumber + s.lightmaptexturenum );
				this.Host.Video.Device.Graphics.DrawWaterPolyLightmap( p, this.Host.RealTime, true );
			}
		}

		private void CommitLightmap( int i )
		{
			this.LightMapTexture.CommitLightmap(this._LightMaps, i );
		}

		/// <summary>
		/// R_TextureAnimation
		/// Returns the proper texture for a given time and base texture
		/// </summary>
		private ModelTexture TextureAnimation( ModelTexture t )
		{
			if (this._CurrentEntity.frame != 0 )
			{
				if ( t.alternate_anims != null )
					t = t.alternate_anims;
			}

			if ( t.anim_total == 0 )
				return t;

			var reletive = ( int ) (this.Host.Client.cl.time * 10 ) % t.anim_total;
			var count = 0;
			while ( t.anim_min > reletive || t.anim_max <= reletive )
			{
				t = t.anim_next;
				if ( t == null )
					Utilities.Error( "R_TextureAnimation: broken cycle" );
				if ( ++count > 100 )
					Utilities.Error( "R_TextureAnimation: infinite cycle" );
			}

			return t;
		}

		/// <summary>
		/// R_RenderDynamicLightmaps
		/// Multitexture
		/// </summary>
		private void RenderDynamicLightmaps( MemorySurface fa )
		{
			this._BrushPolys++;

			if ( ( fa.flags & ( ( int ) Q1SurfaceFlags.Sky | ( int ) Q1SurfaceFlags.Turbulence ) ) != 0 )
				return;

			fa.polys.chain = this._LightMapPolys[fa.lightmaptexturenum];
			this._LightMapPolys[fa.lightmaptexturenum] = fa.polys;

			// check for lightmap modification
			var flag = false;
			for ( var maps = 0; maps < BspDef.MAXLIGHTMAPS && fa.styles[maps] != 255; maps++ )
			{
				if (this._LightStyleValue[fa.styles[maps]] != fa.cached_light[maps] )
				{
					flag = true;
					break;
				}
			}

			if ( flag ||
				fa.dlightframe == this._FrameCount || // dynamic this frame
				fa.cached_dlight )  // dynamic previously
			{
				if (this.Host.Cvars.Dynamic.Get<bool>( ) )
				{
					this.LightMapTexture.LightMapModified[fa.lightmaptexturenum] = true;
					this.UpdateRect( fa, ref this.LightMapTexture.LightMapRectChange[fa.lightmaptexturenum] );
					var offset = fa.lightmaptexturenum * this._LightMapBytes * RenderDef.BLOCK_WIDTH * RenderDef.BLOCK_HEIGHT +
						fa.light_t * RenderDef.BLOCK_WIDTH * this._LightMapBytes + fa.light_s * this._LightMapBytes;

					this.BuildLightMap( fa, new(this._LightMaps, offset ), RenderDef.BLOCK_WIDTH * this._LightMapBytes );
				}
			}
		}

		/// <summary>
		/// R_DrawBrushModel
		/// </summary>
		private void DrawBrushModel( Entity e )
		{
			this._CurrentEntity = e;
			this.Host.DrawingContext.CurrentTexture = -1;

			var clmodel = ( BrushModelData ) e.model;
			var rotated = false;
			Vector3 mins, maxs;
			if ( e.angles.X != 0 || e.angles.Y != 0 || e.angles.Z != 0 )
			{
				rotated = true;
				mins = e.origin;
				mins.X -= clmodel.Radius;
				mins.Y -= clmodel.Radius;
				mins.Z -= clmodel.Radius;
				maxs = e.origin;
				maxs.X += clmodel.Radius;
				maxs.Y += clmodel.Radius;
				maxs.Z += clmodel.Radius;
			}
			else
			{
				mins = e.origin + clmodel.BoundsMin;
				maxs = e.origin + clmodel.BoundsMax;
			}

			if ( Utilities.CullBox( ref mins, ref maxs, ref this._Frustum ) )
				return;

			Array.Clear(this._LightMapPolys, 0, this._LightMapPolys.Length );
			this._ModelOrg = this._RefDef.vieworg - e.origin;
			if ( rotated )
			{
				var temp = this._ModelOrg;
				Vector3 forward, right, up;
				MathLib.AngleVectors( ref e.angles, out forward, out right, out up );
				this._ModelOrg.X = Vector3.Dot( temp, forward );
				this._ModelOrg.Y = -Vector3.Dot( temp, right );
				this._ModelOrg.Z = Vector3.Dot( temp, up );
			}

			// calculate dynamic lighting for bmodel if it's not an
			// instanced model
			if ( clmodel.FirstModelSurface != 0 && !this.Host.Cvars.glFlashBlend.Get<bool>( ) )
			{
				for ( var k = 0; k < ClientDef.MAX_DLIGHTS; k++ )
				{
					if ( this.Host.Client.DLights[k].die < this.Host.Client.cl.time || this.Host.Client.DLights[k].radius == 0 )
						continue;

					this.MarkLights(this.Host.Client.DLights[k], 1 << k, clmodel.Nodes[clmodel.Hulls[0].firstclipnode] );
				}
			}

			this.Host.Video.Device.PushMatrix( );
			e.angles.X = -e.angles.X;   // stupid quake bug
			this.Host.Video.Device.RotateForEntity( e.origin, e.angles );
			e.angles.X = -e.angles.X;   // stupid quake bug

			var surfOffset = clmodel.FirstModelSurface;
			var psurf = clmodel.Surfaces; //[clmodel.firstmodelsurface];

			//
			// draw texture
			//
			for ( var i = 0; i < clmodel.NumModelSurfaces; i++, surfOffset++ )
			{
				// find which side of the node we are on
				var pplane = psurf[surfOffset].plane;

				var dot = Vector3.Dot(this._ModelOrg, pplane.normal ) - pplane.dist;

				// draw the polygon
				var planeBack = ( psurf[surfOffset].flags & ( int ) Q1SurfaceFlags.PlaneBack ) != 0;
				if ( ( planeBack && dot < -QDef.BACKFACE_EPSILON ) || ( !planeBack && dot > QDef.BACKFACE_EPSILON ) )
				{
					if (this.Host.Cvars.glTexSort.Get<bool>( ) )
						this.RenderBrushPoly( psurf[surfOffset] );
					else
						this.DrawSequentialPoly( psurf[surfOffset] );
				}
			}

			this.BlendLightmaps( );

			this.Host.Video.Device.PopMatrix( );
		}
	}

	//glRect_t;
}
