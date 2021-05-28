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
    using System.Drawing;

    public class BaseDeviceDesc
    {
        public virtual bool IsFullScreen
        {
            get;
            set;
        }

        public virtual bool SupportsMultiTexture
        {
            get;
            set;
        }

        public virtual bool MultiTexturing
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

        public virtual int ActualWidth
        {
            get;
            set;
        }

        public virtual int ActualHeight
        {
            get;
            set;
        }

        public virtual double AspectRatio
        {
            get;
            set;
        }

        public virtual float Gamma
        {
            get;
            set;
        }

        public virtual string Renderer
        {
            get;
            set;
        }

        public virtual string Vendor
        {
            get;
            set;
        }

        public virtual string Version
        {
            get;
            set;
        }

        public virtual string Extensions
        {
            get;
            set;
        }

        public virtual Rectangle ViewRect
        {
            get;
            set;
        }

        public virtual float DepthMinimum
        {
            get;
            set;
        }

        public virtual float DepthMaximum
        {
            get;
            set;
        }

        public virtual int TrickFrame
        {
            get;
            set;
        }
    }
}
