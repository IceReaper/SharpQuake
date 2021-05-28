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



// gl_refrag.c

namespace SharpQuake.Rendering
{
    using Framework.Definitions;
    using Framework.Engine;
    using Framework.IO.BSP.Q1;
    using Framework.Mathematics;
    using Game.Data.Models;
    using Game.Rendering.Memory;
    using Game.World;
    using System.Numerics;

    partial class render
    {
        private Entity _AddEnt; // r_addent
        private MemoryNode _EfragTopNode; // r_pefragtopnode
        private Vector3 _EMins; // r_emins
        private Vector3 _EMaxs; // r_emaxs

        /// <summary>
        /// efrag_t **lastlink changed to object _LastObj
        /// and may be a reference to entity_t, in wich case assign *lastlink to ((entity_t)_LastObj).efrag
        /// or to efrag_t in wich case assign *lastlink value to ((efrag_t)_LastObj).entnext
        /// </summary>
        private object _LastObj; // see comments

        /// <summary>
        /// R_AddEfrags
        /// </summary>
        public void AddEfrags( Entity ent )
        {
            if( ent.model == null )
                return;

            this._AddEnt = ent;
            this._LastObj = ent; //  lastlink = &ent->efrag;
            this._EfragTopNode = null;

            var entmodel = ent.model;
            this._EMins = ent.origin + entmodel.BoundsMin;
            this._EMaxs = ent.origin + entmodel.BoundsMax;

            this.SplitEntityOnNode(this.Host.Client.cl.worldmodel.Nodes[0] );
            ent.topnode = this._EfragTopNode;
        }

        /// <summary>
        /// R_SplitEntityOnNode
        /// </summary>
        private void SplitEntityOnNode( MemoryNodeBase node )
        {
            if( node.contents == ( int ) Q1Contents.Solid )
                return;

            // add an efrag if the node is a leaf
            if( node.contents < 0 )
            {
                if(this._EfragTopNode == null )
                    this._EfragTopNode = node as MemoryNode;

                var leaf = (MemoryLeaf)( object ) node;

                // grab an efrag off the free list
                var ef = this.Host.Client.cl.free_efrags;
                if( ef == null )
                {
                    this.Host.Console.Print( "Too many efrags!\n" );
                    return;	// no free fragments...
                }

                this.Host.Client.cl.free_efrags = this.Host.Client.cl.free_efrags.entnext;

                ef.entity = this._AddEnt;

                // add the entity link
                // *lastlink = ef;
                if(this._LastObj is Entity )
                    ( (Entity)this._LastObj ).efrag = ef;
                else
                    ( (EFrag)this._LastObj ).entnext = ef;

                this._LastObj = ef; // lastlink = &ef->entnext;
                ef.entnext = null;

                // set the leaf links
                ef.leaf = leaf;
                ef.leafnext = leaf.efrags;
                leaf.efrags = ef;

                return;
            }

            // NODE_MIXED
            var n = node as MemoryNode;
            if( n == null )
                return;

            var splitplane = n.plane;
            var sides = MathLib.BoxOnPlaneSide( ref this._EMins, ref this._EMaxs, splitplane );

            if( sides == 3 )
            {
                // split on this plane
                // if this is the first splitter of this bmodel, remember it
                if(this._EfragTopNode == null )
                    this._EfragTopNode = n;
            }

            // recurse down the contacted sides
            if( ( sides & 1 ) != 0 )
                this.SplitEntityOnNode( n.children[0] );

            if( ( sides & 2 ) != 0 )
                this.SplitEntityOnNode( n.children[1] );
        }

        /// <summary>
        /// R_StoreEfrags
        /// FIXME: a lot of this goes away with edge-based
        /// </summary>
        private void StoreEfrags( EFrag ef )
        {
            while( ef != null )
            {
                var pent = ef.entity;
                var clmodel = pent.model;

                switch( clmodel.Type )
                {
                    case ModelType.mod_alias:
                    case ModelType.mod_brush:
                    case ModelType.mod_sprite:
                        if( pent.visframe != this._FrameCount && this.Host.Client.NumVisEdicts < ClientDef.MAX_VISEDICTS )
                        {
                            this.Host.Client.VisEdicts[this.Host.Client.NumVisEdicts++] = pent;

                            // mark that we've recorded this entity for this frame
                            pent.visframe = this._FrameCount;
                        }

                        ef = ef.leafnext;
                        break;

                    default:
                        Utilities.Error( "R_StoreEfrags: Bad entity type {0}\n", clmodel.Type );
                        break;
                }
            }
        }
    }
}
