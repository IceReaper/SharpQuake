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

namespace SharpQuake.Renderer.OpenGL
{
    using Framework;
    using Framework.Engine;
    using Framework.IO;
    using Models;
    using OpenTK.Graphics.OpenGL;
    using OpenTK.Mathematics;
    using OpenTK.Windowing.Common;
    using OpenTK.Windowing.Desktop;
    using OpenTK.Windowing.GraphicsLibraryFramework;
    using Renderer.Textures;
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Numerics;
    using Textures;
    using PixelFormat = OpenTK.Graphics.OpenGL.PixelFormat;
    using Vector3 = System.Numerics.Vector3;
    using VideoMode = Renderer.VideoMode;

    public class GLDevice : BaseDevice
    {
        private unsafe Monitor* OpenTKDevice
        {
            get;
            set;
        }

        private GameWindow Form
        {
            get;
            set;
        }

        private Matrix4x4 WorldMatrix; // r_world_matrix

        public unsafe GLDevice( GameWindow form, Monitor openTKDevice )
            : base( typeof( GLDeviceDesc ), 
                  typeof( GLGraphics ), 
                  typeof( GLTextureAtlas ),
                  typeof( GLModel ),
                  typeof( GLModelDesc ),
				  typeof( GLAliasModel ),
				  typeof( GLAliasModelDesc ),
				  typeof( GLTexture ), 
                  typeof( GLTextureDesc ) )
        {
            this.Form = form;
            this.OpenTKDevice = GLFW.GetPrimaryMonitor();

            this.TextureFilters = new GLTextureFilter[]
            {
                new( "GL_NEAREST", TextureMinFilter.Nearest, TextureMagFilter.Nearest ),
                new( "GL_LINEAR", TextureMinFilter.Linear, TextureMagFilter.Linear ),
                new( "GL_NEAREST_MIPMAP_NEAREST", TextureMinFilter.NearestMipmapNearest, TextureMagFilter.Nearest ),
                new( "GL_LINEAR_MIPMAP_NEAREST", TextureMinFilter.LinearMipmapNearest, TextureMagFilter.Linear ),
                new( "GL_NEAREST_MIPMAP_LINEAR", TextureMinFilter.NearestMipmapLinear, TextureMagFilter.Nearest ),
                new( "GL_LINEAR_MIPMAP_LINEAR", TextureMinFilter.LinearMipmapLinear, TextureMagFilter.Linear )
            };

            this.BlendModes = new GLTextureBlendMode[]
            {
                new( "GL_MODULATE", TextureEnvMode.Modulate ),
                new( "GL_ADD", TextureEnvMode.Add ),
                new( "GL_REPLACE", TextureEnvMode.Replace ),
                new( "GL_DECAL",  TextureEnvMode.Decal ),
                new( "GL_REPLACE_EXT", TextureEnvMode.ReplaceExt ),
                new( "GL_TEXTURE_ENV_BIAS_SGIX", TextureEnvMode.TextureEnvBiasSgix ),
                new( "GL_COMBINE", TextureEnvMode.Combine )
            };

            this.PixelFormats = new GLPixelFormat[]
            {
                new( "GL_LUMINANCE", PixelFormat.Luminance ),
                new( "GL_RGBA", PixelFormat.Rgba ),
                new( "GL_RGB", PixelFormat.Rgb ),
                new( "GL_BGR", PixelFormat.Bgr ),
                new( "GL_BGRA", PixelFormat.Bgra ),
                new( "GL_ALPHA", PixelFormat.Alpha )
            };
        }

        /// <summary>
        /// GL_Init
        /// </summary>
        public override void Initialise( byte[] palette )
        {
            base.Initialise( palette );

            GL.ClearColor( 1, 0, 0, 0 );
            GL.CullFace( CullFaceMode.Front );
            GL.Enable( EnableCap.Texture2D );

            GL.Enable( EnableCap.AlphaTest );
            GL.AlphaFunc( AlphaFunction.Greater, 0.666f );

            GL.PolygonMode( MaterialFace.FrontAndBack, PolygonMode.Fill );
            GL.ShadeModel( ShadingModel.Flat );

            GL.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, ( int ) TextureMinFilter.Nearest );
            GL.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, ( int ) TextureMagFilter.Nearest );

            GL.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureWrapS, ( int ) TextureWrapMode.Repeat );
            GL.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureWrapT, ( int ) TextureWrapMode.Repeat );
            GL.BlendFunc( BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha );
            GL.TexEnv( TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvMode, ( int ) TextureEnvMode.Replace );
        }

        public void SetTextureFilters( TextureMinFilter min, TextureMagFilter mag )
        {
            GL.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, ( int ) min );
            GL.TexParameter( TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, ( int ) mag );
        }

        public override void SetTextureFilters( string name )
        {
            var filter = ( GLTextureFilter )this.GetTextureFilters( name );

            if ( filter != null )
                this.SetTextureFilters( filter.Minimise, filter.Maximise );
        }

        public void SetBlendMode( TextureEnvMode mode )
        {
            GL.TexEnv( TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvMode, ( int ) mode );
        }

        public override void SetBlendMode( string name )
        {
            var mode = ( GLTextureBlendMode )this.GetBlendMode( name );

            if ( mode != null )
                this.SetBlendMode( mode.Mode );
        }

        protected override unsafe void GetAvailableModes( )
        {
            var availableResolutions = GLFW.GetVideoModes(this.OpenTKDevice);
            var tmp = new List<VideoMode>(availableResolutions.Length );

            foreach ( var res in availableResolutions )
            {
                if ( res.RedBits + res.GreenBits + res.BlueBits <= 8 )
                    continue;

                Predicate<VideoMode> SameMode = delegate ( VideoMode m )
                {
                    return m.Width == res.Width && m.Height == res.Height && m.BitsPerPixel == res.RedBits + res.GreenBits + res.BlueBits;
                };

                if ( tmp.Exists( SameMode ) )
                    continue;

                var mode = new VideoMode( );
                mode.Width = res.Width;
                mode.Height = res.Height;
                mode.BitsPerPixel = res.RedBits + res.GreenBits + res.BlueBits;
                mode.RefreshRate = res.RefreshRate;
                tmp.Add( mode );
            }

            this.AvailableModes = tmp.ToArray( );

            var current = *GLFW.GetVideoMode(this.OpenTKDevice);

            this.FirstAvailableMode = new( );
            this.FirstAvailableMode.Width = current.Width;
            this.FirstAvailableMode.Height = current.Height;
            this.FirstAvailableMode.BitsPerPixel = current.RedBits + current.GreenBits + current.BlueBits;
            this.FirstAvailableMode.RefreshRate = current.RefreshRate;
            this.FirstAvailableMode.FullScreen = true;
        }

        protected override void ChangeMode( VideoMode mode )
        {
            try
            {
                this.Form.WindowState = mode.FullScreen ? WindowState.Fullscreen : WindowState.Normal;
                this.Form.Size = new(mode.Width, mode.Height);
                this.Form.RenderFrequency = mode.RefreshRate;
            }
            catch ( Exception ex )
            {
                Utilities.Error( $"Couldn't set video mode: {ex.Message}" );
            }

            if (this.Desc.IsFullScreen )
            {
                this.Form.WindowState = WindowState.Fullscreen;
                this.Form.WindowBorder = WindowBorder.Hidden;
            }
            else
            {
                this.Form.WindowState = WindowState.Normal;
                this.Form.WindowBorder = WindowBorder.Fixed;
            }

            this.Desc.ActualWidth = this.Form.ClientSize.X;
            this.Desc.ActualHeight = this.Form.ClientSize.Y;
        }

        public override void BeginScene( )
        {
            base.BeginScene( );

            GL.Color3( 1f, 1, 1 );
        }

        public override void EndScene( )
        {
            base.EndScene( );
        }

        public override void ResetMatrix( )
        {
            var m = new Matrix4(
                this.WorldMatrix.M11, this.WorldMatrix.M12, this.WorldMatrix.M13, this.WorldMatrix.M14,
                this.WorldMatrix.M21, this.WorldMatrix.M22, this.WorldMatrix.M23, this.WorldMatrix.M24,
                this.WorldMatrix.M31, this.WorldMatrix.M32, this.WorldMatrix.M33, this.WorldMatrix.M34,
                this.WorldMatrix.M41, this.WorldMatrix.M42, this.WorldMatrix.M43, this.WorldMatrix.M44
            );

            GL.LoadMatrix( ref m );
        }

        public override void PushMatrix( )
        {
            GL.PushMatrix( );
        }

        public override void PopMatrix( )
        {
            GL.PopMatrix( );
        }

        protected override void Present( )
        {
            this.Form?.SwapBuffers( );
        }

        public override void SetZWrite( bool enable )
        {
            GL.DepthMask( enable );
        }

        public override void SetViewport( int x, int y, int width, int height )
        {
            GL.Viewport( x, y, width, height );
        }

        public override void Begin2DScene( )
        {
            this.SetViewport(this.Desc.ViewRect );

            GL.MatrixMode( MatrixMode.Projection );
            GL.LoadIdentity( );
            GL.Ortho( 0, this.Desc.Width, this.Desc.Height, 0, -99999, 99999 );

            GL.MatrixMode( MatrixMode.Modelview );
            GL.LoadIdentity( );

            GL.Disable( EnableCap.DepthTest );
            GL.Disable( EnableCap.CullFace );
            GL.Disable( EnableCap.Blend );
            GL.Enable( EnableCap.AlphaTest );

            GL.Color4( 1.0f, 1.0f, 1.0f, 1.0f );
        }

        public override void End2DScene( )
        {

        }

        public override void Setup3DScene( bool cull, refdef_t renderDef, bool isEnvMap )
        {
            //
            // set up viewpoint
            //
            GL.MatrixMode( MatrixMode.Projection );
            GL.LoadIdentity( );
            var x = renderDef.vrect.x * this.Desc.ActualWidth / this.Desc.Width;
            var x2 = ( renderDef.vrect.x + renderDef.vrect.width ) * this.Desc.ActualWidth / this.Desc.Width;
            var y = (this.Desc.Height - renderDef.vrect.y ) * this.Desc.ActualHeight / this.Desc.Height;
            var y2 = (this.Desc.Height - ( renderDef.vrect.y + renderDef.vrect.height ) ) * this.Desc.ActualHeight / this.Desc.Height;

            // fudge around because of frac screen scale
            if ( x > 0 )
                x--;
            if ( x2 < this.Desc.ActualWidth )
                x2++;
            if ( y2 < 0 )
                y2--;
            if ( y < this.Desc.ActualHeight )
                y++;

            var w = x2 - x;
            var h = y - y2;

            if ( isEnvMap )
            {
                x = y2 = 0;
                w = h = 256;
            }

            GL.Viewport( x, y2, w, h );

            var screenaspect = ( float ) renderDef.vrect.width / renderDef.vrect.height;

            this.MYgluPerspective( renderDef.fov_y, screenaspect, 4, 4096 );

            GL.CullFace( CullFaceMode.Front );

            GL.MatrixMode( MatrixMode.Modelview );
            GL.LoadIdentity( );

            GL.Rotate( -90f, 1, 0, 0 );	    // put Z going up
            GL.Rotate( 90f, 0, 0, 1 );	    // put Z going up
            GL.Rotate( -renderDef.viewangles.Z, 1, 0, 0 );
            GL.Rotate( -renderDef.viewangles.X, 0, 1, 0 );
            GL.Rotate( -renderDef.viewangles.Y, 0, 0, 1 );
            GL.Translate( -renderDef.vieworg.X, -renderDef.vieworg.Y, -renderDef.vieworg.Z );

            GL.GetFloat( GetPName.ModelviewMatrix, out Matrix4 m );
            this.WorldMatrix = new(
                m.M11, m.M12, m.M13, m.M14,
                m.M21, m.M22, m.M23, m.M24,
                m.M31, m.M32, m.M33, m.M34,
                m.M41, m.M42, m.M43, m.M44
            );

            //
            // set drawing parms
            //
            if ( cull )
                GL.Enable( EnableCap.CullFace );
            else
                GL.Disable( EnableCap.CullFace );

            GL.Disable( EnableCap.Blend );
            GL.Disable( EnableCap.AlphaTest );
            GL.Enable( EnableCap.DepthTest );
        }

        private void MYgluPerspective( double fovy, double aspect, double zNear, double zFar )
        {
            var ymax = zNear * Math.Tan( fovy * Math.PI / 360.0 );
            var ymin = -ymax;

            var xmin = ymin * aspect;
            var xmax = ymax * aspect;

            GL.Frustum( xmin, xmax, ymin, ymax, zNear, zFar );
        }

        public override void Clear( bool zTrick, float clear )
        {
            if ( zTrick )
            {
                if ( clear != 0 )
                    GL.Clear( ClearBufferMask.ColorBufferBit );

                this.Desc.TrickFrame++;
                if ( (this.Desc.TrickFrame & 1 ) != 0 )
                {
                    this.Desc.DepthMinimum = 0;
                    this.Desc.DepthMaximum = 0.49999f;
                    GL.DepthFunc( DepthFunction.Lequal );
                }
                else
                {
                    this.Desc.DepthMinimum = 1;
                    this.Desc.DepthMaximum = 0.5f;
                    GL.DepthFunc( DepthFunction.Gequal );
                }
            }
            else
            {
                if ( clear != 0 )
                {
                    GL.Clear( ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit );
                    // Uze
                    //Host.StatusBar.Changed( );
                }
                else
                    GL.Clear( ClearBufferMask.DepthBufferBit );

                this.Desc.DepthMinimum = 0;
                this.Desc.DepthMaximum = 1;
                GL.DepthFunc( DepthFunction.Lequal );
            }

            this.SetDepth(this.Desc.DepthMinimum, this.Desc.DepthMaximum );
        }

        public override void SetDepth( float minimum, float maximum )
        {
            GL.DepthRange( minimum, maximum );
        }

        public override void SetDrawBuffer( bool isFront )
        {
            if ( isFront )
                GL.DrawBuffer( DrawBufferMode.Front );
            else
                GL.DrawBuffer( DrawBufferMode.Back );
        }

        ///<summary>
        /// Needed probably for GL only
        ///</summary>
        public override void Finish( )
        {
            GL.Finish( );
        }

        public override void SelectTexture( MTexTarget target )
        {
            if ( !this.Desc.SupportsMultiTexture )
                return;

            switch ( target )
            {
                case MTexTarget.TEXTURE0_SGIS:
                    GL.Arb.ActiveTexture( TextureUnit.Texture0 );
                    break;

                case MTexTarget.TEXTURE1_SGIS:
                    GL.Arb.ActiveTexture( TextureUnit.Texture1 );
                    break;

                default:
                    Utilities.Error( "GL_SelectTexture: Unknown target\n" );
                    break;
            }
        }

        public override void DisableMultitexture( )
        {
            if (this.Desc.MultiTexturing )
            {
                GL.Disable( EnableCap.Texture2D );
                this.SelectTexture( MTexTarget.TEXTURE0_SGIS );
                this.Desc.MultiTexturing = false;
            }
        }

        /// <summary>
        /// GL_EnableMultitexture
        /// </summary>
        public override void EnableMultitexture( )
        {
            if (this.Desc.SupportsMultiTexture )
            {
                this.SelectTexture( MTexTarget.TEXTURE1_SGIS );
                GL.Enable( EnableCap.Texture2D );
                this.Desc.MultiTexturing = true;
            }
        }

        /// <summary>
        /// R_RotateForEntity
        /// </summary>
        public override void RotateForEntity( Vector3 origin, Vector3 angles )
        {
            GL.Translate( origin.X, origin.Y, origin.Z );

            GL.Rotate( angles.Y, 0, 0, 1 );
            GL.Rotate( -angles.X, 0, 1, 0 );
            GL.Rotate( angles.Z, 1, 0, 0 );
        }

		/// <summary>
		/// R_BlendedRotateForEntity
		/// </summary>
		public override void BlendedRotateForEntity( Vector3 origin, Vector3 angles, double realTime, ref Vector3 origin1, ref Vector3 origin2, ref float translateStartTime, ref Vector3 angles1, ref Vector3 angles2, ref float rotateStartTime )
		{
			// positional interpolation

			var blend = 0f;
			var timepassed = realTime - translateStartTime;

			if ( translateStartTime == 0 || timepassed > 1 )
			{
				translateStartTime = ( float ) realTime;

				origin1 = origin;
				origin2 = origin;
				blend = 0f;
			}
			if ( origin != origin2 )
			{
				translateStartTime = ( float ) realTime;
				origin1 = origin2;
				origin2 = origin;
				blend = 0;
			}
			else
			{
				blend = ( float ) ( timepassed / 0.1f );

				if ( /*cl.paused || */blend > 1 )
					blend = 1;
			}

			var d = origin2 - origin1;

			GL.Translate( origin1.X + blend * d.X, origin1.Y + blend * d.Y, origin1.Z + blend * d.Z );

			// orientation interpolation (Euler angles, yuck!)

			timepassed = realTime - rotateStartTime;

			if ( rotateStartTime == 0 || timepassed > 1 )
			{
				rotateStartTime = ( float ) realTime;
				angles1 = angles;
				angles2 = angles;
			}

			if ( angles != angles2 )
			{
				rotateStartTime = ( float ) realTime;
				angles1 = angles2;
				angles2 = angles;
				blend = 0;
			}
			else
			{
				blend = ( float ) ( timepassed / 0.1 );

				if ( /*cl.paused ||*/ blend > 1 ) blend = 1;
			}

			d = angles2 - angles1;

			// always interpolate along the shortest path
            if ( d.X > 180 )
                d.X -= 360;
            else if ( d.X < -180 )
                d.X += 360;
            
            if ( d.Y > 180 )
                d.Y -= 360;
            else if ( d.Y < -180 )
                d.Y += 360;
            
            if ( d.Z > 180 )
                d.Z -= 360;
            else if ( d.Z < -180 )
                d.Z += 360;

			GL.Rotate( angles1.Y + blend * d.Y, 0, 0, 1 );
			GL.Rotate( -angles1.X + -blend * d.X, 0, 1, 0 );
			GL.Rotate( angles1.Z + blend * d.Z, 1, 0, 0 );
		}
	}
}
