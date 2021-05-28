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

namespace SharpQuake.Renderer
{
    using Framework;
    using Framework.Rendering;
    using Framework.Rendering.Sprite;
    using System;
    using System.Drawing;
    using System.Numerics;
    using Textures;

    public class BaseGraphics : IDisposable
    {
        public BaseDevice Device
        {
            get;
            private set;
        }

        protected BaseTexture CurrentParticleTexture
        {
            get;
            private set;
        }

        public BaseGraphics( BaseDevice device )
        {
            this.Device = device;
        }

        public virtual void Initialise()
        {
            //throw new NotImplementedException( );
        }

        public virtual void Dispose( )
        {
            //throw new NotImplementedException( );
        }

        public virtual void DrawTexture2D( BaseTexture texture, int x, int y, Color? colour = null, bool hasAlpha = false )
        {
            this.DrawTexture2D( texture, x, y, texture.Desc.Width, texture.Desc.Height, colour, hasAlpha );
        }

        public virtual void DrawTexture2D( BaseTexture texture, int x, int y, int width, int height, Color? colour = null, bool hasAlpha = false )
        {
            this.DrawTexture2D( texture, new( x, y, width, height ), colour, hasAlpha );
        }

        public virtual void DrawTexture2D( BaseTexture texture, Rectangle destRect, Color? colour = null, bool hasAlpha = false )
        {
            var srcRectF = new RectangleF( );
            srcRectF.X = 0;
            srcRectF.Y = 0;
            srcRectF.Width = 1;
            srcRectF.Height = 1;

            this.DrawTexture2D( texture, srcRectF, destRect, colour, hasAlpha );
        }

        public virtual void DrawTexture2D( BaseTexture texture, RectangleF sourceRect, int x, int y, Color? colour = null, bool hasAlpha = false )
        {
            this.DrawTexture2D( texture, sourceRect, new( x, y, texture.Desc.Width, texture.Desc.Height ), colour, hasAlpha );
        }

        public virtual void DrawTexture2D( BaseTexture texture, RectangleF sourceRect, Rectangle destRect, Color? colour = null, bool hasAlpha = false )
        {
            throw new NotImplementedException( );
        }

        public virtual void DrawPicture( BasePicture picture, int x, int y, Color? colour = null, bool hasAlpha = false )
        {
            if (this.Device.TextureAtlas.IsDirty )
                this.Device.TextureAtlas.Upload( true );

            this.DrawTexture2D( picture.Texture, picture.Source, new( x, y, picture.Width, picture.Height ), colour, hasAlpha );
        }

        public virtual void DrawPicture( BasePicture picture, int x, int y, int width, int height, Color? colour = null, bool hasAlpha = false )
        {
            if (this.Device.TextureAtlas.IsDirty )
                this.Device.TextureAtlas.Upload( true );

            this.DrawTexture2D( picture.Texture, picture.Source, new( x, y, width, height ), colour, hasAlpha );
        }
        
        public virtual void BeginParticles( BaseTexture texture )
        {
            this.CurrentParticleTexture = texture;
        }

        public virtual void DrawParticle( float colour, Vector3 up, Vector3 right, Vector3 origin, float scale )
        {
            throw new NotImplementedException( );
        }

        public virtual void EndParticles( )
        {
            this.CurrentParticleTexture = null;
        }

        /// <summary>
        /// EmitSkyPolys
        /// </summary>
        public virtual void EmitSkyPolys( GLPoly polys, Vector3 origin, float speed, bool blend = false )
        {
            throw new NotImplementedException( );
        }

        public virtual void DrawPoly( GLPoly p, float scaleX = 1f, float scaleY = 1f, bool isLightmap = false )
        {
            throw new NotImplementedException( );
        }

        /// <summary>
        /// EmitWaterPolys
        /// Does a water warp on the pre-fragmented glpoly_t chain
        /// </summary>
        public virtual void EmitWaterPolys( ref float[] turbSin, double time, double turbScale, GLPoly polys )
        {
            throw new NotImplementedException( );
        }

        public virtual void DrawWaterPoly( GLPoly p, double time )
        {
            throw new NotImplementedException( );
        }

        public virtual void DrawWaterPolyLightmap( GLPoly p, double time, bool blend = false )
        {
            throw new NotImplementedException( );
        }

        public virtual void DrawSequentialPoly( BaseTexture texture, BaseTexture lightMapTexture, GLPoly p, int lightMapNumber )
        {
            throw new NotImplementedException( );
        }

        public virtual void DrawSequentialPolyMultiTexture( BaseTexture texture, BaseTexture lightMapTexture, byte[] lightMapData, GLPoly p, int lightMapNumber )
        {
            throw new NotImplementedException( );
        }

        public virtual void DrawWaterPolyMultiTexture( byte[] lightMapData, BaseTexture texture, BaseTexture lightMapTexture, int lightMapTextureNumber, GLPoly p, double time )
        {
            throw new NotImplementedException( );
        }

        public virtual void Fill( int x, int y, int width, int height, Color color )
        {
            throw new NotImplementedException( );
        }

        public virtual void DrawTransTranslate( BaseTexture texture, int x, int y, int width, int height, byte[] translation )
        {
            throw new NotImplementedException( );
        }

        public virtual void BeginBlendLightMap( bool lightMapCvar, string filter = "GL_LUMINANCE" )
        {
            throw new NotImplementedException( );
        }

        public virtual void EndBlendLightMap( bool lightMapCvar, string filter = "GL_LUMINANCE" )
        {
            throw new NotImplementedException( );
        }

        public virtual void BeginDLights( )
        {
            throw new NotImplementedException( );
        }

        public virtual void EndDLights( )
        {
            throw new NotImplementedException( );
        }

        public virtual void DrawDLight( dlight_t light, Vector3 viewProj, Vector3 viewUp, Vector3 viewRight )
        {
            throw new NotImplementedException( );
        }

        public virtual void DrawSpriteModel( BaseTexture texture, mspriteframe_t frame, Vector3 up, Vector3 right, Vector3 origin )
        {
            throw new NotImplementedException( );
        }

        public virtual void PolyBlend( Color colour )
        {
            throw new NotImplementedException( );
        }

        // Draw_Fill
        //
        // Fills a box of pixels with a single color
        public virtual void FillUsingPalette( int x, int y, int width, int height, int colour )
        {
            this.Fill( x, y, width, height, this.Device.Palette.ToColour( colour ) );
        }

        public virtual void FadeScreen()
        {
            throw new NotImplementedException( );
        }
    }
}
