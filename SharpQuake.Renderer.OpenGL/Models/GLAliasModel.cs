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

namespace SharpQuake.Renderer.OpenGL.Models
{
	using Framework;
	using Framework.Mathematics;
	using Framework.Rendering.Alias;
	using OpenTK.Graphics.OpenGL;
	using Renderer.Models;
	using System.Numerics;

	public class GLAliasModel : BaseAliasModel
	{
		public GLAliasModel( BaseDevice device, BaseAliasModelDesc desc ) : base( device, desc )
		{
		}

		public override void DrawAliasModel( float shadeLight, Vector3 shadeVector, float[] shadeDots, float lightSpotZ, aliashdr_t paliashdr, double realTime, double time, ref int poseNum, ref int poseNum2, ref float frameStartTime, ref float frameInterval, ref Vector3 origin1, ref Vector3 origin2, ref float translateStartTime, ref Vector3 angles1, ref Vector3 angles2, ref float rotateStartTime, bool shadows = true, bool smoothModels = true, bool affineModels = false, bool noColours = false, bool isEyes = false, bool useInterpolation = true )
		{
			this.Device.DisableMultitexture( );

			GL.Enable( EnableCap.Texture2D );

			GL.PushMatrix( );

			if ( useInterpolation )
				this.Device.BlendedRotateForEntity(this.Desc.Origin, this.Desc.EulerAngles, realTime, ref origin1, ref origin2, ref translateStartTime, ref angles1, ref angles2, ref rotateStartTime );
			else
				this.Device.RotateForEntity(this.Desc.Origin, this.Desc.EulerAngles );

			if ( isEyes )
			{
				var v = this.Desc.ScaleOrigin;
				v.Z -= 22 + 8;
				GL.Translate( v.X, v.Y, v.Z );
				// double size of eyes, since they are really hard to see in gl
				var s = this.Desc.Scale * 2.0f;
				GL.Scale( s.X, s.Y, s.Z );
			}
			else
			{
				GL.Translate(this.Desc.ScaleOrigin.X, this.Desc.ScaleOrigin.Y, this.Desc.ScaleOrigin.Z );
				GL.Scale(this.Desc.Scale.X, this.Desc.Scale.Y, this.Desc.Scale.Z );
			}

			var anim = ( int ) ( time * 10 ) & 3;
			//var texture = Host.Model.SkinTextures[paliashdr.gl_texturenum[_CurrentEntity.skinnum, anim]];
			this.Desc.Texture.Bind( );

			// we can't dynamically colormap textures, so they are cached
			// seperately for the players.  Heads are just uncolored.
			//if ( _CurrentEntity.colormap != Host.Screen.vid.colormap && !noColours && playernum >= 1 )
			//{
			//    PlayerTextures[playernum > 0 ? playernum - 1 : playernum].Bind( );
			//    //Host.DrawingContext.Bind( _PlayerTextures - 1 + playernum );
			//}

			if ( smoothModels )
				GL.ShadeModel( ShadingModel.Smooth );

			GL.TexEnv( TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvMode, ( int ) TextureEnvMode.Modulate );

			if ( affineModels )
				GL.Hint( HintTarget.PerspectiveCorrectionHint, HintMode.Fastest );

			//SetupAliasFrame( shadeLight, Desc.AliasFrame, time, paliashdr, shadeDots );
			this.SetupAliasBlendedFrame( shadeLight, this.AliasDesc.AliasFrame, realTime, time, paliashdr, shadeDots, ref poseNum, ref poseNum2, ref frameStartTime, ref frameInterval );

			GL.TexEnv( TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvMode, ( int ) TextureEnvMode.Replace );

			GL.ShadeModel( ShadingModel.Flat );
			if ( affineModels )
				GL.Hint( HintTarget.PerspectiveCorrectionHint, HintMode.Nicest );

			GL.PopMatrix( );

			if ( shadows )
			{
				GL.PushMatrix( );
				this.Device.RotateForEntity(this.Desc.Origin, this.Desc.EulerAngles );
				GL.Disable( EnableCap.Texture2D );
				GL.Enable( EnableCap.Blend );
				GL.Color4( 0, 0, 0, 0.5f );
				this.DrawAliasShadow( paliashdr, this.AliasDesc.LastPoseNumber, lightSpotZ, shadeVector );
				GL.Enable( EnableCap.Texture2D );
				GL.Disable( EnableCap.Blend );
				GL.Color4( 1f, 1, 1, 1 );
				GL.PopMatrix( );
			}

			GL.Disable( EnableCap.Texture2D );
		}

		protected override void DrawAliasShadow( aliashdr_t paliashdr, int posenum, float lightSpotZ, Vector3 shadeVector )
		{
			var lheight = this.Desc.Origin.Z - lightSpotZ;
			float height = 0;
			var verts = paliashdr.posedata;
			var voffset = posenum * paliashdr.poseverts;
			var order = paliashdr.commands;

			height = -lheight + 1.0f;
			var orderOffset = 0;

			while ( true )
			{
				// get the vertex count and primitive type
				var count = order[orderOffset++];
				if ( count == 0 )
					break;      // done

				if ( count < 0 )
				{
					count = -count;
					GL.Begin( PrimitiveType.TriangleFan );
				}
				else
					GL.Begin( PrimitiveType.TriangleStrip );

				do
				{
					// texture coordinates come from the draw list
					// (skipped for shadows) glTexCoord2fv ((float *)order);
					orderOffset += 2;

					// normals and vertexes come from the frame list
					var point = new Vector3(
						verts[voffset].v[0] * paliashdr.scale.X + paliashdr.scale_origin.X,
						verts[voffset].v[1] * paliashdr.scale.Y + paliashdr.scale_origin.Y,
						verts[voffset].v[2] * paliashdr.scale.Z + paliashdr.scale_origin.Z
					);

					point.X -= shadeVector.X * ( point.Z + lheight );
					point.Y -= shadeVector.Y * ( point.Z + lheight );
					point.Z = height;

					GL.Vertex3( point.X, point.Y, point.Z );

					voffset++;
				} while ( --count > 0 );

				GL.End( );
			}
		}

		/*
         =============
         GL_DrawAliasBlendedFrame

         fenix@io.com: model animation interpolation
         =============
         */

		protected override void DrawAliasBlendedFrame( float shadeLight, float[] shadeDots, aliashdr_t paliashdr, int posenum, int posenum2, float blend )
		{
			this.AliasDesc.LastPoseNumber0 = posenum;
			this.AliasDesc.LastPoseNumber = posenum2;

			var verts = paliashdr.posedata;
			var vert2 = verts;
			var vertsOffset = posenum * paliashdr.poseverts;

			var verts2Offset = posenum2 * paliashdr.poseverts;

			var order = paliashdr.commands;
			var orderOffset = 0;

			while ( true )
			{
				if ( orderOffset >= order.Length )
					break;

				// get the vertex count and primitive type
				var count = order[orderOffset];
				orderOffset++;
				if ( count == 0 )
					break;      // done

				if ( count < 0 )
				{
					count = -count;
					GL.Begin( PrimitiveType.TriangleFan );
				}
				else
					GL.Begin( PrimitiveType.TriangleStrip );

				Union4b u1 = Union4b.Empty, u2 = Union4b.Empty;
				do
				{
					if ( orderOffset + 1 >= order.Length )
						break;
					// texture coordinates come from the draw list
					u1.i0 = order[orderOffset + 0];
					u2.i0 = order[orderOffset + 1];
					orderOffset += 2;
					GL.TexCoord2( u1.f0, u2.f0 );

					// normals and vertexes come from the frame list
					// blend the light intensity from the two frames together
					var d = Vector3.Zero;

					if ( vertsOffset >= verts.Length )
					{
						count = 0;
						break;
					}

					d.X = shadeDots[vert2[verts2Offset].lightnormalindex] -
						   shadeDots[verts[vertsOffset].lightnormalindex];

					//var l = shadeDots[verts[vertsOffset].lightnormalindex] * shadeLight;
					var l = shadeLight * ( shadeDots[verts[vertsOffset].lightnormalindex] + blend * d.X );
					GL.Color3( l, l, l );

					var v = new Vector3( verts[vertsOffset].v[0], verts[vertsOffset].v[1], verts[vertsOffset].v[2] );
					var v2 = new Vector3( vert2[verts2Offset].v[0], vert2[verts2Offset].v[1], verts[verts2Offset].v[2] );
					d = v2 - v;

					GL.Vertex3( ( float ) verts[vertsOffset].v[0] + blend * d.X, verts[vertsOffset].v[1] + blend * d.Y, verts[vertsOffset].v[2] + blend * d.Z );
					vertsOffset++;
					verts2Offset++;
				} while ( --count > 0 );
				GL.End( );
			}
		}

		/// <summary>
		/// GL_DrawAliasFrame
		/// </summary>
		protected override void DrawAliasFrame( float shadeLight, float[] shadeDots, aliashdr_t paliashdr, int posenum )
		{
			this.AliasDesc.LastPoseNumber = posenum;

			var verts = paliashdr.posedata;
			var vertsOffset = posenum * paliashdr.poseverts;
			var order = paliashdr.commands;
			var orderOffset = 0;

			while ( true )
			{
				// get the vertex count and primitive type
				var count = order[orderOffset++];
				if ( count == 0 )
					break;      // done

				if ( count < 0 )
				{
					count = -count;
					GL.Begin( PrimitiveType.TriangleFan );
				}
				else
					GL.Begin( PrimitiveType.TriangleStrip );

				Union4b u1 = Union4b.Empty, u2 = Union4b.Empty;
				do
				{
					// texture coordinates come from the draw list
					u1.i0 = order[orderOffset + 0];
					u2.i0 = order[orderOffset + 1];
					orderOffset += 2;
					GL.TexCoord2( u1.f0, u2.f0 );

					// normals and vertexes come from the frame list
					var l = shadeDots[verts[vertsOffset].lightnormalindex] * shadeLight;
					GL.Color3( l, l, l );
					GL.Vertex3( ( float ) verts[vertsOffset].v[0], verts[vertsOffset].v[1], verts[vertsOffset].v[2] );
					vertsOffset++;
				} while ( --count > 0 );
				GL.End( );
			}
		}
	}
}
