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

namespace SharpQuake.Game.Data.Models
{
    using Framework;
    using Framework.Engine;
    using Framework.Rendering;
    using Framework.World;
    using Rendering.Textures;
    using System.Numerics;

    public class ModelData
    {
        public string Name
        {
            get;
            set;
        }

        // bmodels and sprites don't cache normally
        public bool IsLoadRequired
        {
            get;
            set;
        }

        public ModelType Type
        {
            get;
            set;
        }

        public int FrameCount
        {
            get;
            set;
        }

        public SyncType SyncType
        {
            get;
            set;
        }

        public EntityFlags Flags
        {
            get;
            set;
        }

        //
        // volume occupied by the model graphics
        //
        public Vector3 BoundsMin
        {
            get;
            set;
        }

        public Vector3 BoundsMax
        {
            get;
            set;
        }

        public float Radius
        {
            get;
            set;
        }

        //
        // solid volume for clipping 
        //
        public bool ClipBox
        {
            get;
            set;
        }

        public Vector3 ClipMin
        {
            get;
            set;
        }

        public Vector3 ClipMax
        {
            get;
            set;
        }

        //
        // additional model data
        //

        public CacheUser cache
        {
            get;
            set;
        } // cache_user_t	cache		// only access through Mod_Extradata

        protected ModelTexture NoTexture
        {
            get;
            set;
        }

        protected byte[] Buffer
        {
            get;
            set;
        }

        public ModelData( ModelTexture noTexture )
        {
            this.NoTexture = noTexture;
        }

        public virtual void Clear( )
        {
            this.Name = null;
            this.IsLoadRequired = false;
            this.Type = 0;
            this.FrameCount = 0;
            this.SyncType = 0;
            this.Flags = 0;
            this.BoundsMin = Vector3.Zero;
            this.BoundsMax = Vector3.Zero;
            this.Radius = 0;
            this.ClipBox = false;
            this.ClipMin = Vector3.Zero;
            this.ClipMax = Vector3.Zero;
            this.cache = null;
        }

        public virtual void CopyFrom( ModelData src )
        {
            this.Name = src.Name;
            this.IsLoadRequired = src.IsLoadRequired;
            this.Type = src.Type;
            this.FrameCount = src.FrameCount;
            this.SyncType = src.SyncType;
            this.Flags = src.Flags;
            this.BoundsMin = src.BoundsMin;
            this.BoundsMax = src.BoundsMax;
            this.Radius = src.Radius;
            this.ClipBox = src.ClipBox;
            this.ClipMin = src.ClipMin;
            this.ClipMax = src.ClipMax;

            this.cache = src.cache;
        }
    } //model_t;
}
