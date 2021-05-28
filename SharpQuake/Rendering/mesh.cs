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



// gl_mesh.c

namespace SharpQuake.Rendering
{
    using Framework.Engine;
    using Framework.Mathematics;
    using Framework.Rendering;
    using Framework.Rendering.Alias;
    using Game.Data.Models;
    using System;

    internal static class mesh
    {
        private const int MAX_COMMANDS = 8192;
        private const int MAX_STRIP = 128;

        private static ModelData _AliasModel; // AliasModelData
        private static aliashdr_t _AliasHdr; // paliashdr

        private static byte[] _Used = new byte[mesh.MAX_COMMANDS]; // qboolean used. changed to vyte because can have values 0, 1, 2...

        // the command list holds counts and s/t values that are valid for
        // every frame
        private static int[] _Commands = new int[mesh.MAX_COMMANDS]; // commands

        private static int _NumCommands; // numcommands

        // all frames will have their vertexes rearranged and expanded
        // so they are in the order expected by the command list
        private static int[] _VertexOrder = new int[mesh.MAX_COMMANDS]; // vertexorder

        private static int _NumOrder; // numorder

        private static int _AllVerts; // allverts
        private static int _AllTris; // alltris

        private static int[] _StripVerts = new int[mesh.MAX_STRIP]; // stripverts
        private static int[] _StripTris = new int[mesh.MAX_STRIP]; // striptris
        private static int _StripCount; // stripcount

        /// <summary>
        /// GL_MakeAliasModelDisplayLists
        /// </summary>
        public static void MakeAliasModelDisplayLists( AliasModelData m )
        {
            mesh._AliasModel = m;
            mesh._AliasHdr = m.Header;

            //
            // build it from scratch
            //
            mesh.BuildTris( m );		// trifans or lists

            //
            // save the data out
            //
            mesh._AliasHdr.poseverts = mesh._NumOrder;

            var cmds = new int[mesh._NumCommands]; //Hunk_Alloc (numcommands * 4);
            mesh._AliasHdr.commands = cmds; // in bytes??? // (byte*)cmds - (byte*)paliashdr;
            Buffer.BlockCopy( mesh._Commands, 0, cmds, 0, mesh._NumCommands * 4 ); //memcpy (cmds, commands, numcommands * 4);

            var poseverts = m.PoseVerts;
            var verts = new trivertx_t[mesh._AliasHdr.numposes * mesh._AliasHdr.poseverts]; // Hunk_Alloc (paliashdr->numposes * paliashdr->poseverts * sizeof(trivertx_t) );
            mesh._AliasHdr.posedata = verts; // (byte*)verts - (byte*)paliashdr;
            var offset = 0;

            for ( var i = 0; i < mesh._AliasHdr.numposes; i++)
                for ( var j = 0; j < mesh._NumOrder; j++)
                    verts[offset++] = poseverts[i][mesh._VertexOrder[j]];  // *verts++ = poseverts[i][vertexorder[j]];
        }

        /// <summary>
        /// BuildTris
        /// Generate a list of trifans or strips for the model, which holds for all frames
        /// </summary>
        private static void BuildTris( AliasModelData m )
        {
            var bestverts = new int[1024];
            var besttris = new int[1024];

            // Uze
            // All references to pheader from model.c changed to _AliasHdr (former paliashdr)

            //
            // build tristrips
            //
            var stverts = m.STVerts;
            var triangles = m.Triangles;
            mesh._NumOrder = 0;
            mesh._NumCommands = 0;
            Array.Clear( mesh._Used, 0, mesh._Used.Length ); // memset (used, 0, sizeof(used));
            int besttype = 0, len;
            for( var i = 0; i < mesh._AliasHdr.numtris; i++ )
            {
                // pick an unused triangle and start the trifan
                if( mesh._Used[i] != 0 )
                    continue;

                var bestlen = 0;
                for( var type = 0; type < 2; type++ )
                {
                    for( var startv = 0; startv < 3; startv++ )
                    {
                        if( type == 1 )
                            len = mesh.StripLength( m, i, startv );
                        else
                            len = mesh.FanLength( m, i, startv );
                        if( len > bestlen )
                        {
                            besttype = type;
                            bestlen = len;
                            for( var j = 0; j < bestlen + 2; j++ )
                                bestverts[j] = mesh._StripVerts[j];
                            for( var j = 0; j < bestlen; j++ )
                                besttris[j] = mesh._StripTris[j];
                        }
                    }
                }

                // mark the tris on the best strip as used
                for( var j = 0; j < bestlen; j++ )
                    mesh._Used[besttris[j]] = 1;

                if( besttype == 1 )
                    mesh._Commands[mesh._NumCommands++] = bestlen + 2;
                else
                    mesh._Commands[mesh._NumCommands++] = -( bestlen + 2 );

                var uval = Union4b.Empty;
                for( var j = 0; j < bestlen + 2; j++ )
                {
                    // emit a vertex into the reorder buffer
                    var k = bestverts[j];
                    mesh._VertexOrder[mesh._NumOrder++] = k;

                    // emit s/t coords into the commands stream
                    float s = stverts[k].s;
                    float t = stverts[k].t;
                    if( triangles[besttris[0]].facesfront == 0 && stverts[k].onseam != 0 )
                        s += mesh._AliasHdr.skinwidth / 2;	// on back side
                    s = ( s + 0.5f ) / mesh._AliasHdr.skinwidth;
                    t = ( t + 0.5f ) / mesh._AliasHdr.skinheight;

                    uval.f0 = s;
                    mesh._Commands[mesh._NumCommands++] = uval.i0;
                    uval.f0 = t;
                    mesh._Commands[mesh._NumCommands++] = uval.i0;
                }
            }

            mesh._Commands[mesh._NumCommands++] = 0;		// end of list marker

            ConsoleWrapper.DPrint( "{0,3} tri {1,3} vert {2,3} cmd\n", mesh._AliasHdr.numtris, mesh._NumOrder, mesh._NumCommands );

            mesh._AllVerts += mesh._NumOrder;
            mesh._AllTris += mesh._AliasHdr.numtris;
        }

        private static int StripLength( AliasModelData m, int starttri, int startv )
        {
            mesh._Used[starttri] = 2;

            var triangles = m.Triangles;

            var vidx = triangles[starttri].vertindex; //last = &triangles[starttri];
            mesh._StripVerts[0] = vidx[startv % 3];
            mesh._StripVerts[1] = vidx[( startv + 1 ) % 3];
            mesh._StripVerts[2] = vidx[( startv + 2 ) % 3];

            mesh._StripTris[0] = starttri;
            mesh._StripCount = 1;

            var m1 = mesh._StripVerts[2]; // last->vertindex[(startv + 2) % 3];
            var m2 = mesh._StripVerts[1]; // last->vertindex[(startv + 1) % 3];
            var lastfacesfront = triangles[starttri].facesfront;

// look for a matching triangle
nexttri:
            for( var j = starttri + 1; j < mesh._AliasHdr.numtris; j++ )
            {
                if( triangles[j].facesfront != lastfacesfront )
                    continue;

                vidx = triangles[j].vertindex;

                for( var k = 0; k < 3; k++ )
                {
                    if( vidx[k] != m1 )
                        continue;
                    if( vidx[( k + 1 ) % 3] != m2 )
                        continue;

                    // this is the next part of the fan

                    // if we can't use this triangle, this tristrip is done
                    if( mesh._Used[j] != 0 )
                        goto done;

                    // the new edge
                    if( ( mesh._StripCount & 1 ) != 0 )
                        m2 = vidx[( k + 2 ) % 3];
                    else
                        m1 = vidx[( k + 2 ) % 3];

                    mesh._StripVerts[mesh._StripCount + 2] = triangles[j].vertindex[( k + 2 ) % 3];
                    mesh._StripTris[mesh._StripCount] = j;
                    mesh._StripCount++;

                    mesh._Used[j] = 2;
                    goto nexttri;
                }
            }
done:

// clear the temp used flags
            for( var j = starttri + 1; j < mesh._AliasHdr.numtris; j++ )
            {
                if( mesh._Used[j] == 2 )
                    mesh._Used[j] = 0;
            }

            return mesh._StripCount;
        }

        private static int FanLength( AliasModelData m, int starttri, int startv )
        {
            mesh._Used[starttri] = 2;

            var triangles = m.Triangles;
            //last = &triangles[starttri];

            var vidx = triangles[starttri].vertindex;

            mesh._StripVerts[0] = vidx[startv % 3];
            mesh._StripVerts[1] = vidx[( startv + 1 ) % 3];
            mesh._StripVerts[2] = vidx[( startv + 2 ) % 3];

            mesh._StripTris[0] = starttri;
            mesh._StripCount = 1;

            var m1 = vidx[( startv + 0 ) % 3];
            var m2 = vidx[( startv + 2 ) % 3];
            var lastfacesfront = triangles[starttri].facesfront;

// look for a matching triangle
nexttri:
            for( var j = starttri + 1; j < mesh._AliasHdr.numtris; j++ )//, check++)
            {
                vidx = triangles[j].vertindex;
                if( triangles[j].facesfront != lastfacesfront )
                    continue;

                for( var k = 0; k < 3; k++ )
                {
                    if( vidx[k] != m1 )
                        continue;
                    if( vidx[( k + 1 ) % 3] != m2 )
                        continue;

                    // this is the next part of the fan

                    // if we can't use this triangle, this tristrip is done
                    if( mesh._Used[j] != 0 )
                        goto done;

                    // the new edge
                    m2 = vidx[( k + 2 ) % 3];

                    mesh._StripVerts[mesh._StripCount + 2] = m2;
                    mesh._StripTris[mesh._StripCount] = j;
                    mesh._StripCount++;

                    mesh._Used[j] = 2;
                    goto nexttri;
                }
            }
done:

// clear the temp used flags
            for( var j = starttri + 1; j < mesh._AliasHdr.numtris; j++ )
            {
                if( mesh._Used[j] == 2 )
                    mesh._Used[j] = 0;
            }

            return mesh._StripCount;
        }
    }
}
