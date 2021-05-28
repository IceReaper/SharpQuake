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

namespace SharpQuake.Renderer.Models
{
	using System;
	using System.Collections.Generic;
	using Textures;

	public class BaseModel : IDisposable
	{
		public BaseDevice Device
		{
			get;
			private set;
		}

		public BaseModelDesc Desc
		{
			get;
			protected set;
		}

		public static Dictionary<string, BaseModel> ModelPool
		{
			get;
			protected set;
		}

		static BaseModel( )
		{
			BaseModel.ModelPool = new( );
		}

		public BaseModel( BaseDevice device, BaseModelDesc desc )
		{
			this.Device = device;
			this.Desc = desc;
			BaseModel.ModelPool.Add(this.Desc.Name, this );
		}

		public virtual void Initialise( )
		{
			//throw new NotImplementedException( );
		}

		public virtual void Draw( )
		{
		}
		

		public virtual void Dispose( )
		{
		}

		public static BaseModel Create( BaseDevice device, string identifier, BaseTexture texture, Type modelType, Type descType )
		{
			if ( BaseModel.ModelPool.ContainsKey( identifier ) )
				return BaseModel.ModelPool[identifier];

			var desc = ( BaseAliasModelDesc ) Activator.CreateInstance( descType );
			desc.Name = identifier;
			desc.Texture = texture;

			var model = ( BaseModel ) Activator.CreateInstance( modelType, device, desc );
			model.Initialise( );

			return model;
		}
	}
}
