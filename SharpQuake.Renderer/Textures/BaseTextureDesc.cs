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
    public class BaseTextureDesc
    {
        public virtual string Name
        {
            get;
            set;
        }

        public virtual string Owner
        {
            get;
            set;
        }

        public virtual string Filter
        {
            get;
            set;
        }

        public virtual string BlendMode
        {
            get;
            set;
        }

        public virtual int Width
        {
            get;
            set;
        }

        public virtual int Height
        {
            get;
            set;
        }

        public virtual int ScaledWidth
        {
            get;
            set;
        }

        public virtual int ScaledHeight
        {
            get;
            set;
        }

        public virtual bool HasMipMap
        {
            get;
            set;
        }

        public virtual bool HasAlpha
        {
            get;
            set;
        }

        public virtual bool IsLightMap
        {
            get;
            set;
        }

        public virtual string LightMapFormat
        {
            get;
            set;
        }

        public virtual int LightMapBytes
        {
            get;
            set;
        }
    }
}
