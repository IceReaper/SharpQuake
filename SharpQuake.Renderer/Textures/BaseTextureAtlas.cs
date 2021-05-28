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

namespace SharpQuake.Renderer.Textures
{
    using Framework;
    using Framework.Data;
    using Framework.Engine;
    using System;
    using System.Drawing;

    public class BaseTextureAtlas : IDisposable
    {
        public BaseDevice Device
        {
            get;
            private set;
        }

        public bool IsDirty
        {
            get;
            private set;
        }

        public int UploadCount
        {
            get;
            private set;
        }

        private int[][] Allocated
        {
            get;
            set;
        }

        private byte[][] Texels
        {
            get;
            set;
        }

        private int MaxTextures
        {
            get;
            set;
        }

        private int Width
        {
            get;
            set;
        }

        private int Height
        {
            get;
            set;
        }

        public BaseTexture[] Textures
        {
            get;
            private set;
        }

        public BaseTextureAtlas( BaseDevice device, int maxTextures, int width, int height )
        {
            this.Device = device;
            this.MaxTextures = maxTextures;
            this.Width = width;
            this.Height = height;
            this.Textures = new BaseTexture[this.MaxTextures];

            this.Allocated = new int[this.MaxTextures][]; //[MAX_SCRAPS][BLOCK_WIDTH];
            for ( var i = 0; i < this.Allocated.GetLength( 0 ); i++ )
                this.Allocated[i] = new int[this.Width];

            this.Texels = new byte[this.MaxTextures][]; // [MAX_SCRAPS][BLOCK_WIDTH*BLOCK_HEIGHT*4];
            for ( var i = 0; i < this.Texels.GetLength( 0 ); i++ )
                this.Texels[i] = new byte[this.Width * this.Height * 4];
        }

        public virtual void Initialise( )
        {
        }       

        public virtual void Upload( bool resample )
        {
            this.UploadCount++;

            for ( var i = 0; i < this.MaxTextures; i++ )
            {
                var texture = this.Textures[i];

                if ( texture == null )
                {
                    texture = BaseTexture.FromBuffer(
                        this.Device, Guid.NewGuid( ).ToString( ),
                        new ByteArraySegment(this.Texels[i] ),
                        this.Width,
                        this.Height, false, true, "GL_NEAREST" );
                }
                else
                {
                    texture.Initialise( new ByteArraySegment(this.Texels[i] ) );
                    texture.Bind( );
                    texture.Upload8( resample );
                }

                this.Textures[i] = texture;
            }

            this.IsDirty = false;
        }

        public virtual void Dispose( )
        {
        }

        public virtual BaseTexture Add( ByteArraySegment buffer, BasePicture picture )
        {
            var textureNumber = this.Allocate( picture.Width, picture.Height, out var x, out var y );

            var source = new RectangleF( );
            source.X = ( float ) ( ( x + 0.01 ) / ( float )this.Height );
            source.Width = picture.Width / ( float )this.Width;
            source.Y = ( float ) ( ( y + 0.01 ) / ( float  )this.Height );
            source.Height = picture.Height / ( float )this.Height;

            picture.Source = source;

            this.IsDirty = true;

            var k = 0;

            for ( var i = 0; i < picture.Height; i++ )
            {
                for ( var j = 0; j < picture.Width; j++, k++ )
                    this.Texels[textureNumber][( y + i ) * this.Width + x + j] = buffer.Data[buffer.StartIndex + k];// p->data[k];
            }

            this.Upload( true );

            return this.Textures[textureNumber];
        }

        // Scrap_AllocBlock
        // returns a texture number and the position inside it
        protected virtual int Allocate( int width, int height, out int x, out int y )
        {
            x = -1;
            y = -1;

            for ( var texnum = 0; texnum < this.MaxTextures; texnum++ )
            {
                var best = this.Height;

                for ( var i = 0; i < this.Width - width; i++ )
                {
                    int best2 = 0, j;

                    for ( j = 0; j < width; j++ )
                    {
                        if (this.Allocated[texnum][i + j] >= best )
                            break;
                        if (this.Allocated[texnum][i + j] > best2 )
                            best2 = this.Allocated[texnum][i + j];
                    }
                    if ( j == width )
                    {
                        // this is a valid spot
                        x = i;
                        y = best = best2;
                    }
                }

                if ( best + height > this.Height )
                    continue;

                for ( var i = 0; i < width; i++ )
                    this.Allocated[texnum][x + i] = best + height;

                return texnum;
            }

            Utilities.Error( "Scrap_AllocBlock: full" );
            return -1;
        }
    }
}
