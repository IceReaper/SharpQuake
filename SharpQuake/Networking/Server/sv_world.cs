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



// world.c -- world query functions

// entities never clip against themselves, or their owner
//
// line of sight checks trace->crosscontent, but bullets don't

namespace SharpQuake.Networking.Server
{
	using Framework.Definitions;
	using Framework.Engine;
	using Framework.IO.BSP;
	using Framework.IO.BSP.Q1;
	using Framework.Mathematics;
	using Framework.Networking.Server;
	using Framework.World;
	using Game.Data.Models;
	using Game.Rendering.Memory;
	using System;
	using System.Numerics;
	using Plane = Framework.Mathematics.Plane;

	partial class server
	{
		// 1/32 epsilon to keep floating point happy
		private const float DIST_EPSILON = 0.03125f;

		private const int MOVE_NORMAL = 0;
		private const int MOVE_NOMONSTERS = 1;
		private const int MOVE_MISSILE = 2;

		private const int AREA_DEPTH = 4;

		private const int AREA_NODES = 32;

		private areanode_t[] _AreaNodes = new areanode_t[server.AREA_NODES];

		// sv_areanodes
		private int _NumAreaNodes;

		// sv_numareanodes
		private BspHull _BoxHull = new();

		// box_hull
		private BspClipNode[] _BoxClipNodes = new BspClipNode[6];

		private Plane[] _BoxPlanes = new Plane[6];

		/// <summary>
		/// SV_ClearWorld
		/// called after the world model has been loaded, before linking any entities
		/// </summary>
		public void ClearWorld( )
		{
			this.InitBoxHull();

			foreach ( var node in this._AreaNodes )
				node.Clear();

			this._NumAreaNodes = 0;

			this.CreateAreaNode( 0, this.sv.worldmodel.BoundsMin, this.sv.worldmodel.BoundsMax );
		}

		/// <summary>
		/// SV_UnlinkEdict
		/// call before removing an entity, and before trying to move one,
		/// so it doesn't clip against itself
		/// flags ent->v.modified
		/// </summary>
		public void UnlinkEdict( MemoryEdict ent )
		{
			if ( ent.area.Prev == null )
				return;     // not linked in anywhere

			ent.area.Remove();  //RemoveLink(&ent->area);
								//ent->area.prev = ent->area.next = NULL;
		}

		/// <summary>
		/// SV_LinkEdict
		///
		/// Needs to be called any time an entity changes origin, mins, maxs, or solid
		/// flags ent->v.modified
		/// sets ent->v.absmin and ent->v.absmax
		/// if touchtriggers, calls prog functions for the intersected triggers
		/// </summary>
		public void LinkEdict( MemoryEdict ent, bool touch_triggers )
		{
			if ( ent.area.Prev != null )
				this.UnlinkEdict( ent ); // unlink from old position

			if ( ent == this.sv.edicts[0] )
				return;     // don't add the world

			if ( ent.free )
				return;

			// set the abs box
			MathLib.VectorAdd( ref ent.v.origin, ref ent.v.mins, out ent.v.absmin );
			MathLib.VectorAdd( ref ent.v.origin, ref ent.v.maxs, out ent.v.absmax );

			//
			// to make items easier to pick up and allow them to be grabbed off
			// of shelves, the abs sizes are expanded
			//
			if ( ( ( int ) ent.v.flags & EdictFlags.FL_ITEM ) != 0 )
			{
				ent.v.absmin.X -= 15;
				ent.v.absmin.Y -= 15;
				ent.v.absmax.X += 15;
				ent.v.absmax.Y += 15;
			}
			else
			{   // because movement is clipped an epsilon away from an actual edge,
				// we must fully check even when bounding boxes don't quite touch
				ent.v.absmin.X -= 1;
				ent.v.absmin.Y -= 1;
				ent.v.absmin.Z -= 1;
				ent.v.absmax.X += 1;
				ent.v.absmax.Y += 1;
				ent.v.absmax.Z += 1;
			}

			// link to PVS leafs
			ent.num_leafs = 0;
			if ( ent.v.modelindex != 0 )
				this.FindTouchedLeafs( ent, this.sv.worldmodel.Nodes[0] );

			if ( ent.v.solid == Solids.SOLID_NOT )
				return;

			// find the first node that the ent's box crosses
			var node = this._AreaNodes[0];
			while ( true )
			{
				if ( node.axis == -1 )
					break;
				if ( MathLib.Comp( ref ent.v.absmin, node.axis ) > node.dist )
					node = node.children[0];
				else if ( MathLib.Comp( ref ent.v.absmax, node.axis ) < node.dist )
					node = node.children[1];
				else
					break;      // crosses the node
			}

			// link it in

			if ( ent.v.solid == Solids.SOLID_TRIGGER )
				ent.area.InsertBefore( node.trigger_edicts );
			else
				ent.area.InsertBefore( node.solid_edicts );

			// if touch_triggers, touch all entities at this node and decend for more
			if ( touch_triggers )
				this.TouchLinks( ent, this._AreaNodes[0] );
		}

		/// <summary>
		/// SV_PointContents
		/// </summary>
		public int PointContents( ref Vector3 p )
		{
			var cont = this.HullPointContents(this.sv.worldmodel.Hulls[0], 0, ref p );
			if ( cont <= ( int ) Q1Contents.Current0 && cont >= ( int ) Q1Contents.CurrentDown )
				cont = ( int ) Q1Contents.Water;
			return cont;
		}

		/// <summary>
		/// SV_Move
		/// mins and maxs are relative
		/// if the entire move stays in a solid volume, trace.allsolid will be set
		/// if the starting point is in a solid, it will be allowed to move out to an open area
		/// nomonsters is used for line of sight or edge testing, where mosnters
		/// shouldn't be considered solid objects
		/// passedict is explicitly excluded from clipping checks (normally NULL)
		/// </summary>
		public Trace_t Move( ref Vector3 start, ref Vector3 mins, ref Vector3 maxs, ref Vector3 end, int type, MemoryEdict passedict )
		{
			var clip = new moveclip_t();

			// clip to world
			clip.trace = this.ClipMoveToEntity(this.sv.edicts[0], ref start, ref mins, ref maxs, ref end );

			clip.start = start;
			clip.end = end;
			clip.mins = mins;
			clip.maxs = maxs;
			clip.type = type;
			clip.passedict = passedict;

			if ( type == server.MOVE_MISSILE )
			{
				clip.mins2 = Vector3.One * -15;
				clip.maxs2 = Vector3.One * 15;
			}
			else
			{
				clip.mins2 = mins;
				clip.maxs2 = maxs;
			}

			// create the bounding box of the entire move
			this.MoveBounds( ref start, ref clip.mins2, ref clip.maxs2, ref end, out clip.boxmins, out clip.boxmaxs );

			// clip to entities
			this.ClipToLinks(this._AreaNodes[0], clip );

			return clip.trace;
		}

		/// <summary>
		/// SV_RecursiveHullCheck
		/// </summary>
		public bool RecursiveHullCheck( BspHull hull, int num, float p1f, float p2f, ref Vector3 p1, ref Vector3 p2, Trace_t trace )
		{
			// check for empty
			if ( num < 0 )
			{
				if ( num != ( int ) Q1Contents.Solid )
				{
					trace.allsolid = false;
					if ( num == ( int ) Q1Contents.Empty )
						trace.inopen = true;
					else
						trace.inwater = true;
				}
				else
					trace.startsolid = true;
				return true;        // empty
			}

			if ( num < hull.firstclipnode || num > hull.lastclipnode )
				Utilities.Error( "SV_RecursiveHullCheck: bad node number" );

			//
			// find the point distances
			//
			var node_children = hull.clipnodes[num].children;
			var plane = hull.planes[hull.clipnodes[num].planenum];
			float t1, t2;

			if ( plane.type < 3 )
			{
				t1 = MathLib.Comp( ref p1, plane.type ) - plane.dist;
				t2 = MathLib.Comp( ref p2, plane.type ) - plane.dist;
			}
			else
			{
				t1 = Vector3.Dot( plane.normal, p1 ) - plane.dist;
				t2 = Vector3.Dot( plane.normal, p2 ) - plane.dist;
			}

			if ( t1 >= 0 && t2 >= 0 )
				return this.RecursiveHullCheck( hull, node_children[0], p1f, p2f, ref p1, ref p2, trace );
			if ( t1 < 0 && t2 < 0 )
				return this.RecursiveHullCheck( hull, node_children[1], p1f, p2f, ref p1, ref p2, trace );

			// put the crosspoint DIST_EPSILON pixels on the near side
			float frac;
			if ( t1 < 0 )
				frac = ( t1 + server.DIST_EPSILON ) / ( t1 - t2 );
			else
				frac = ( t1 - server.DIST_EPSILON ) / ( t1 - t2 );
			if ( frac < 0 )
				frac = 0;
			if ( frac > 1 )
				frac = 1;

			var midf = p1f + ( p2f - p1f ) * frac;
			var mid = p1 + ( p2 - p1 ) * frac;

			var side = t1 < 0 ? 1 : 0;

			// move up to the node
			if ( !this.RecursiveHullCheck( hull, node_children[side], p1f, midf, ref p1, ref mid, trace ) )
				return false;

			if (this.HullPointContents( hull, node_children[side ^ 1], ref mid ) != ( int ) Q1Contents.Solid )
				// go past the node
				return this.RecursiveHullCheck( hull, node_children[side ^ 1], midf, p2f, ref mid, ref p2, trace );

			if ( trace.allsolid )
				return false;       // never got out of the solid area

			//==================
			// the other side of the node is solid, this is the impact point
			//==================
			if ( side == 0 )
			{
				trace.plane.normal = plane.normal;
				trace.plane.dist = plane.dist;
			}
			else
			{
				trace.plane.normal = -plane.normal;
				trace.plane.dist = -plane.dist;
			}

			while (this.HullPointContents( hull, hull.firstclipnode, ref mid ) == ( int ) Q1Contents.Solid )
			{
				// shouldn't really happen, but does occasionally
				frac -= 0.1f;
				if ( frac < 0 )
				{
					trace.fraction = midf;
					trace.endpos = mid;
					this.Host.Console.DPrint( "backup past 0\n" );
					return false;
				}
				midf = p1f + ( p2f - p1f ) * frac;
				mid = p1 + ( p2 - p1 ) * frac;
			}

			trace.fraction = midf;
			trace.endpos = mid;

			return false;
		}

		/// <summary>
		/// SV_CreateAreaNode
		/// </summary>
		private areanode_t CreateAreaNode( int depth, Vector3 mins, Vector3 maxs )
		{
			var anode = this._AreaNodes[this._NumAreaNodes];
			this._NumAreaNodes++;

			anode.trigger_edicts.Clear();
			anode.solid_edicts.Clear();

			if ( depth == server.AREA_DEPTH )
			{
				anode.axis = -1;
				anode.children[0] = anode.children[1] = null;
				return anode;
			}

			var size = maxs - mins;
			var mins1 = mins;
			var mins2 = mins;
			var maxs1 = maxs;
			var maxs2 = maxs;

			if ( size.X > size.Y )
			{
				anode.axis = 0;
				anode.dist = 0.5f * ( maxs.X + mins.X );
				maxs1.X = mins2.X = anode.dist;
			}
			else
			{
				anode.axis = 1;
				anode.dist = 0.5f * ( maxs.Y + mins.Y );
				maxs1.Y = mins2.Y = anode.dist;
			}

			anode.children[0] = this.CreateAreaNode( depth + 1, mins2, maxs2 );
			anode.children[1] = this.CreateAreaNode( depth + 1, mins1, maxs1 );

			return anode;
		}

		/// <summary>
		/// SV_TestEntityPosition
		/// This could be a lot more efficient...
		/// </summary>
		private MemoryEdict TestEntityPosition( MemoryEdict ent )
		{
			var trace = this.Move( ref ent.v.origin, ref ent.v.mins, ref ent.v.maxs, ref ent.v.origin, 0, ent );

			if ( trace.startsolid )
				return this.sv.edicts[0];

			return null;
		}

		/// <summary>
		/// SV_InitBoxHull
		/// Set up the planes and clipnodes so that the six floats of a bounding box
		/// can just be stored out and get a proper hull_t structure.
		/// </summary>
		private void InitBoxHull( )
		{
			this._BoxHull.clipnodes = this._BoxClipNodes;
			this._BoxHull.planes = this._BoxPlanes;
			this._BoxHull.firstclipnode = 0;
			this._BoxHull.lastclipnode = 5;

			for ( var i = 0; i < 6; i++ )
			{
				this._BoxClipNodes[i].planenum = i;

				var side = i & 1;

				this._BoxClipNodes[i].children[side] = ( int ) Q1Contents.Empty;
				if ( i != 5 )
					this._BoxClipNodes[i].children[side ^ 1] = ( short ) ( i + 1 );
				else
					this._BoxClipNodes[i].children[side ^ 1] = ( int ) Q1Contents.Solid;

				this._BoxPlanes[i].type = ( byte ) ( i >> 1 );
				switch ( i >> 1 )
				{
					case 0:
						this._BoxPlanes[i].normal.X = 1;
						break;

					case 1:
						this._BoxPlanes[i].normal.Y = 1;
						break;

					case 2:
						this._BoxPlanes[i].normal.Z = 1;
						break;
				}
				//_BoxPlanes[i].normal[i>>1] = 1;
			}
		}

		// SV_HullForBox
		//
		// To keep everything totally uniform, bounding boxes are turned into small
		// BSP trees instead of being compared directly.
		private BspHull HullForBox( ref Vector3 mins, ref Vector3 maxs )
		{
			this._BoxPlanes[0].dist = maxs.X;
			this._BoxPlanes[1].dist = mins.X;
			this._BoxPlanes[2].dist = maxs.Y;
			this._BoxPlanes[3].dist = mins.Y;
			this._BoxPlanes[4].dist = maxs.Z;
			this._BoxPlanes[5].dist = mins.Z;

			return this._BoxHull;
		}

		/// <summary>
		/// SV_HullForEntity
		/// Returns a hull that can be used for testing or clipping an object of mins/maxs size.
		/// Offset is filled in to contain the adjustment that must be added to the
		/// testing object's origin to get a point to use with the returned hull.
		/// </summary>
		private BspHull HullForEntity( MemoryEdict ent, ref Vector3 mins, ref Vector3 maxs, out Vector3 offset )
		{
			BspHull hull = null;

			// decide which clipping hull to use, based on the size
			if ( ent.v.solid == Solids.SOLID_BSP )
			{   // explicit hulls in the BSP model
				if ( ent.v.movetype != Movetypes.MOVETYPE_PUSH )
					Utilities.Error( "SOLID_BSP without MOVETYPE_PUSH" );

				var model = ( BrushModelData )this.sv.models[( int ) ent.v.modelindex];

				if ( model == null || model.Type != ModelType.mod_brush )
					Utilities.Error( "MOVETYPE_PUSH with a non bsp model" );

				var size = maxs - mins;
				if ( size.X < 3 )
					hull = model.Hulls[0];
				else if ( size.X <= 32 )
					hull = model.Hulls[1];
				else
					hull = model.Hulls[2];

				// calculate an offset value to center the origin
				offset = hull.clip_mins - mins;
				offset += Utilities.ToVector( ref ent.v.origin );
			}
			else
			{
				// create a temp hull from bounding box sizes
				var hullmins = Utilities.ToVector( ref ent.v.mins ) - maxs;
				var hullmaxs = Utilities.ToVector( ref ent.v.maxs ) - mins;
				hull = this.HullForBox( ref hullmins, ref hullmaxs );

				offset = Utilities.ToVector( ref ent.v.origin );
			}

			return hull;
		}

		/// <summary>
		/// SV_FindTouchedLeafs
		/// </summary>
		private void FindTouchedLeafs( MemoryEdict ent, MemoryNodeBase node )
		{
			if ( node.contents == ( int ) Q1Contents.Solid )
				return;

			// add an efrag if the node is a leaf

			if ( node.contents < 0 )
			{
				if ( ent.num_leafs == ProgramDef.MAX_ENT_LEAFS )
					return;

				var leaf = ( MemoryLeaf ) node;
				var leafnum = Array.IndexOf(this.sv.worldmodel.Leaves, leaf ) - 1;

				ent.leafnums[ent.num_leafs] = ( short ) leafnum;
				ent.num_leafs++;
				return;
			}

			// NODE_MIXED
			var n = ( MemoryNode ) node;
			var splitplane = n.plane;
			var sides = MathLib.BoxOnPlaneSide( ref ent.v.absmin, ref ent.v.absmax, splitplane );

			// recurse down the contacted sides
			if ( ( sides & 1 ) != 0 )
				this.FindTouchedLeafs( ent, n.children[0] );

			if ( ( sides & 2 ) != 0 )
				this.FindTouchedLeafs( ent, n.children[1] );
		}

		/// <summary>
		/// SV_TouchLinks
		/// </summary>
		private void TouchLinks( MemoryEdict ent, areanode_t node )
		{
			// touch linked edicts
			Link next;
			for ( var l = node.trigger_edicts.Next; l != node.trigger_edicts; l = next )
			{
				next = l.Next;
				var touch = ( MemoryEdict ) l.Owner;// EDICT_FROM_AREA(l);
				if ( touch == ent )
					continue;
				if ( touch.v.touch == 0 || touch.v.solid != Solids.SOLID_TRIGGER )
					continue;
				if ( ent.v.absmin.X > touch.v.absmax.X || ent.v.absmin.Y > touch.v.absmax.Y ||
					ent.v.absmin.Z > touch.v.absmax.Z || ent.v.absmax.X < touch.v.absmin.X ||
					ent.v.absmax.Y < touch.v.absmin.Y || ent.v.absmax.Z < touch.v.absmin.Z )
					continue;

				var old_self = this.Host.Programs.GlobalStruct.self;
				var old_other = this.Host.Programs.GlobalStruct.other;

				this.Host.Programs.GlobalStruct.self = this.EdictToProg( touch );
				this.Host.Programs.GlobalStruct.other = this.EdictToProg( ent );
				this.Host.Programs.GlobalStruct.time = ( float )this.sv.time;
				this.Host.Programs.Execute( touch.v.touch );

				this.Host.Programs.GlobalStruct.self = old_self;
				this.Host.Programs.GlobalStruct.other = old_other;
			}

			// recurse down both sides
			if ( node.axis == -1 )
				return;

			if ( MathLib.Comp( ref ent.v.absmax, node.axis ) > node.dist )
				this.TouchLinks( ent, node.children[0] );
			if ( MathLib.Comp( ref ent.v.absmin, node.axis ) < node.dist )
				this.TouchLinks( ent, node.children[1] );
		}

		/// <summary>
		/// SV_ClipMoveToEntity
		/// Handles selection or creation of a clipping hull, and offseting (and
		/// eventually rotation) of the end points
		/// </summary>
		private Trace_t ClipMoveToEntity( MemoryEdict ent, ref Vector3 start, ref Vector3 mins, ref Vector3 maxs, ref Vector3 end )
		{
			var trace = new Trace_t();
			// fill in a default trace
			trace.fraction = 1;
			trace.allsolid = true;
			trace.endpos = end;

			// get the clipping hull
			Vector3 offset;
			var hull = this.HullForEntity( ent, ref mins, ref maxs, out offset );

			var start_l = start - offset;
			var end_l = end - offset;

			// trace a line through the apropriate clipping hull
			this.RecursiveHullCheck( hull, hull.firstclipnode, 0, 1, ref start_l, ref end_l, trace );

			// fix trace up by the offset
			if ( trace.fraction != 1 )
				trace.endpos += offset;

			// did we clip the move?
			if ( trace.fraction < 1 || trace.startsolid )
				trace.ent = ent;

			return trace;
		}

		/// <summary>
		/// SV_MoveBounds
		/// </summary>
		private void MoveBounds( ref Vector3 start, ref Vector3 mins, ref Vector3 maxs, ref Vector3 end, out Vector3 boxmins, out Vector3 boxmaxs )
		{
			boxmins = Vector3.Min( start, end ) + mins - Vector3.One;
			boxmaxs = Vector3.Max( start, end ) + maxs + Vector3.One;
		}

		/// <summary>
		/// SV_ClipToLinks
		/// Mins and maxs enclose the entire area swept by the move
		/// </summary>
		private void ClipToLinks( areanode_t node, moveclip_t clip )
		{
			Link next;
			Trace_t trace;

			// touch linked edicts
			for ( var l = node.solid_edicts.Next; l != node.solid_edicts; l = next )
			{
				next = l.Next;
				var touch = ( MemoryEdict ) l.Owner;// EDICT_FROM_AREA(l);
				if ( touch.v.solid == Solids.SOLID_NOT )
					continue;
				if ( touch == clip.passedict )
					continue;
				if ( touch.v.solid == Solids.SOLID_TRIGGER )
					Utilities.Error( "Trigger in clipping list" );

				if ( clip.type == server.MOVE_NOMONSTERS && touch.v.solid != Solids.SOLID_BSP )
					continue;

				if ( clip.boxmins.X > touch.v.absmax.X || clip.boxmins.Y > touch.v.absmax.Y ||
					clip.boxmins.Z > touch.v.absmax.Z || clip.boxmaxs.X < touch.v.absmin.X ||
					clip.boxmaxs.Y < touch.v.absmin.Y || clip.boxmaxs.Z < touch.v.absmin.Z )
					continue;

				if ( clip.passedict != null && clip.passedict.v.size.X != 0 && touch.v.size.X == 0 )
					continue;   // points never interact

				// might intersect, so do an exact clip
				if ( clip.trace.allsolid )
					return;
				if ( clip.passedict != null )
				{
					if (this.ProgToEdict( touch.v.owner ) == clip.passedict )
						continue;   // don't clip against own missiles
					if (this.ProgToEdict( clip.passedict.v.owner ) == touch )
						continue;   // don't clip against owner
				}

				if ( ( ( int ) touch.v.flags & EdictFlags.FL_MONSTER ) != 0 )
					trace = this.ClipMoveToEntity( touch, ref clip.start, ref clip.mins2, ref clip.maxs2, ref clip.end );
				else
					trace = this.ClipMoveToEntity( touch, ref clip.start, ref clip.mins, ref clip.maxs, ref clip.end );

				if ( trace.allsolid || trace.startsolid || trace.fraction < clip.trace.fraction )
				{
					trace.ent = touch;
					if ( clip.trace.startsolid )
					{
						clip.trace = trace;
						clip.trace.startsolid = true;
					}
					else
						clip.trace = trace;
				}
				else if ( trace.startsolid )
					clip.trace.startsolid = true;
			}

			// recurse down both sides
			if ( node.axis == -1 )
				return;

			if ( MathLib.Comp( ref clip.boxmaxs, node.axis ) > node.dist )
				this.ClipToLinks( node.children[0], clip );
			if ( MathLib.Comp( ref clip.boxmins, node.axis ) < node.dist )
				this.ClipToLinks( node.children[1], clip );
		}

		/// <summary>
		/// SV_HullPointContents
		/// </summary>
		private int HullPointContents( BspHull hull, int num, ref Vector3 p )
		{
			while ( num >= 0 )
			{
				if ( num < hull.firstclipnode || num > hull.lastclipnode )
					Utilities.Error( "SV_HullPointContents: bad node number" );

				var node_children = hull.clipnodes[num].children;
				var plane = hull.planes[hull.clipnodes[num].planenum];
				float d;
				if ( plane.type < 3 )
					d = MathLib.Comp( ref p, plane.type ) - plane.dist;
				else
					d = Vector3.Dot( plane.normal, p ) - plane.dist;
				if ( d < 0 )
					num = node_children[1];
				else
					num = node_children[0];
			}

			return num;
		}

		private class moveclip_t
		{
			public Vector3 boxmins, boxmaxs;// enclose the test object along entire move
			public Vector3 mins, maxs;  // size of the moving object
			public Vector3 mins2, maxs2;    // size when clipping against mosnters
			public Vector3 start, end;
			public Trace_t trace;
			public int type;
			public MemoryEdict passedict;
		} //moveclip_t;
	}
}
