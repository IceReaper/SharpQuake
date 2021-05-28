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

namespace SharpQuake.Game.World
{
	using Data.Models;
	using Framework;
	using Framework.Engine;
	using Rendering.Memory;
	using System.Numerics;

	public class Entity
    {
		public bool forcelink;        // model changed
		public int update_type;
		public EntityState baseline;        // to fill in defaults in updates
		public double msgtime;      // time of last update
		public Vector3[] msg_origins; //[2];	// last two updates (0 is newest)
		public Vector3 origin;
		public Vector3[] msg_angles; //[2];	// last two updates (0 is newest)
		public Vector3 angles;
		public ModelData model;         // NULL = no model
		public EFrag efrag;         // linked list of efrags
		public int frame;
		public float syncbase;     // for client-side animations
		public byte[] colormap;
		public int effects;       // light, particals, etc
		public int skinnum;       // for Alias models
		public int visframe;      // last frame this entity was
									//  found in an active leaf

		public int dlightframe;   // dynamic lighting
		public int dlightbits;

		// FIXME: could turn these into a union
		public int trivial_accept;

		public MemoryNode topnode;      // for bmodels, first world node
										//  that splits bmodel, or NULL if
										//  not split

		// fenix@io.com: model animation interpolation
		public float frame_start_time;
		public float frame_interval;
		public int pose1;
		public int pose2;

		// fenix@io.com: model transform interpolation
		public float translate_start_time;
		public Vector3 origin1;
		public Vector3 origin2;

		public float rotate_start_time;
		public Vector3 angles1;
		public Vector3 angles2;

		public bool useInterpolation = false;

		public void Clear( )
        {
	        this.forcelink = false;
	        this.update_type = 0;

	        this.baseline = EntityState.Empty;

	        this.msgtime = 0;
	        this.msg_origins[0] = Vector3.Zero;
	        this.msg_origins[1] = Vector3.Zero;

	        this.origin = Vector3.Zero;
	        this.msg_angles[0] = Vector3.Zero;
	        this.msg_angles[1] = Vector3.Zero;
	        this.angles = Vector3.Zero;
	        this.model = null;
	        this.efrag = null;
	        this.frame = 0;
	        this.syncbase = 0;
	        this.colormap = null;
	        this.effects = 0;
	        this.skinnum = 0;
	        this.visframe = 0;

	        this.dlightframe = 0;
	        this.dlightbits = 0;

	        this.trivial_accept = 0;
	        this.topnode = null;
        }

        public Entity( )
        {
	        this.msg_origins = new Vector3[2];
	        this.msg_angles = new Vector3[2];
        }
    } // entity_t;
}
