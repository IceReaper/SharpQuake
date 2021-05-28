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



// cl_tent.c

namespace SharpQuake.Networking.Client
{
    using Framework.Definitions;
    using Framework.Engine;
    using Framework.IO.Sound;
    using Framework.Mathematics;
    using Framework.Rendering;
    using Game.Data.Models;
    using Game.World;
    using System;
    using System.Numerics;

    partial class client
    {
        private int _NumTempEntities; // num_temp_entities
        private Entity[] _TempEntities = new Entity[ClientDef.MAX_TEMP_ENTITIES]; // cl_temp_entities[MAX_TEMP_ENTITIES]
        private beam_t[] _Beams = new beam_t[ClientDef.MAX_BEAMS]; // cl_beams[MAX_BEAMS]

        private SoundEffect_t _SfxWizHit; // cl_sfx_wizhit
        private SoundEffect_t _SfxKnigtHit; // cl_sfx_knighthit
        private SoundEffect_t _SfxTink1; // cl_sfx_tink1
        private SoundEffect_t _SfxRic1; // cl_sfx_ric1
        private SoundEffect_t _SfxRic2; // cl_sfx_ric2
        private SoundEffect_t _SfxRic3; // cl_sfx_ric3
        private SoundEffect_t _SfxRExp3; // cl_sfx_r_exp3

        // CL_InitTEnts
        private void InitTempEntities()
        {
            this._SfxWizHit = this.Host.Sound.PrecacheSound( "wizard/hit.wav" );
            this._SfxKnigtHit = this.Host.Sound.PrecacheSound( "hknight/hit.wav" );
            this._SfxTink1 = this.Host.Sound.PrecacheSound( "weapons/tink1.wav" );
            this._SfxRic1 = this.Host.Sound.PrecacheSound( "weapons/ric1.wav" );
            this._SfxRic2 = this.Host.Sound.PrecacheSound( "weapons/ric2.wav" );
            this._SfxRic3 = this.Host.Sound.PrecacheSound( "weapons/ric3.wav" );
            this._SfxRExp3 = this.Host.Sound.PrecacheSound( "weapons/r_exp3.wav" );

            for( var i = 0; i < this._TempEntities.Length; i++ )
                this._TempEntities[i] = new();

            for( var i = 0; i < this._Beams.Length; i++ )
                this._Beams[i] = new();
        }

        // CL_UpdateTEnts
        private void UpdateTempEntities()
        {
            this._NumTempEntities = 0;

            // update lightning
            for( var i = 0; i < ClientDef.MAX_BEAMS; i++ )
            {
                var b = this._Beams[i];
                if( b.model == null || b.endtime < this.cl.time )
                    continue;

                // if coming from the player, update the start position
                if( b.entity == this.cl.viewentity )
                    b.start = this._Entities[this.cl.viewentity].origin;

                // calculate pitch and yaw
                var dist = b.end - b.start;
                float yaw, pitch, forward;

                if( dist.Y == 0 && dist.X == 0 )
                {
                    yaw = 0;
                    if( dist.Z > 0 )
                        pitch = 90;
                    else
                        pitch = 270;
                }
                else
                {
                    yaw = ( int ) ( Math.Atan2( dist.Y, dist.X ) * 180 / Math.PI );
                    if( yaw < 0 )
                        yaw += 360;

                    forward = ( float ) Math.Sqrt( dist.X * dist.X + dist.Y * dist.Y );
                    pitch = ( int ) ( Math.Atan2( dist.Z, forward ) * 180 / Math.PI );
                    if( pitch < 0 )
                        pitch += 360;
                }

                // add new entities for the lightning
                var org = b.start;
                var d = MathLib.Normalize( ref dist );
                while( d > 0 )
                {
                    var ent = this.NewTempEntity();
                    if( ent == null )
                        return;

                    ent.origin = org;
                    ent.model = b.model;
                    ent.angles.X = pitch;
                    ent.angles.Y = yaw;
                    ent.angles.Z = MathLib.Random() % 360;

                    org += dist * 30;
                    // Uze: is this code bug (i is outer loop variable!!!) or what??????????????
                    //for (i=0 ; i<3 ; i++)
                    //    org[i] += dist[i]*30;
                    d -= 30;
                }
            }
        }

        /// <summary>
        /// CL_NewTempEntity
        /// </summary>
        private Entity NewTempEntity()
        {
            if(this.NumVisEdicts == ClientDef.MAX_VISEDICTS )
                return null;
            if(this._NumTempEntities == ClientDef.MAX_TEMP_ENTITIES )
                return null;

            var ent = this._TempEntities[this._NumTempEntities];
            this._NumTempEntities++;
            this._VisEdicts[this.NumVisEdicts] = ent;
            this.NumVisEdicts++;

            ent.colormap = this.Host.Screen.vid.colormap;

            return ent;
        }

        /// <summary>
        /// CL_ParseTEnt
        /// </summary>
        private void ParseTempEntity()
        {
            Vector3 pos;
            dlight_t dl;
            var type = this.Host.Network.Reader.ReadByte();
            switch( type )
            {
                case ProtocolDef.TE_WIZSPIKE:			// spike hitting wall
                    pos = this.Host.Network.Reader.ReadCoords();
                    this.Host.RenderContext.Particles.RunParticleEffect(this.Host.Client.cl.time, ref pos, ref Utilities.ZeroVector, 20, 30 );
                    this.Host.Sound.StartSound( -1, 0, this._SfxWizHit, ref pos, 1, 1 );
                    break;

                case ProtocolDef.TE_KNIGHTSPIKE:			// spike hitting wall
                    pos = this.Host.Network.Reader.ReadCoords();
                    this.Host.RenderContext.Particles.RunParticleEffect(this.Host.Client.cl.time, ref pos, ref Utilities.ZeroVector, 226, 20 );
                    this.Host.Sound.StartSound( -1, 0, this._SfxKnigtHit, ref pos, 1, 1 );
                    break;

                case ProtocolDef.TE_SPIKE:			// spike hitting wall
                    pos = this.Host.Network.Reader.ReadCoords();
#if GLTEST
                    Test_Spawn (pos);
#else
                    this.Host.RenderContext.Particles.RunParticleEffect(this.Host.Client.cl.time, ref pos, ref Utilities.ZeroVector, 0, 10 );
#endif
                    if( MathLib.Random() % 5 != 0 )
                        this.Host.Sound.StartSound( -1, 0, this._SfxTink1, ref pos, 1, 1 );
                    else
                    {
                        var rnd = MathLib.Random() & 3;
                        if( rnd == 1 )
                            this.Host.Sound.StartSound( -1, 0, this._SfxRic1, ref pos, 1, 1 );
                        else if( rnd == 2 )
                            this.Host.Sound.StartSound( -1, 0, this._SfxRic2, ref pos, 1, 1 );
                        else
                            this.Host.Sound.StartSound( -1, 0, this._SfxRic3, ref pos, 1, 1 );
                    }
                    break;

                case ProtocolDef.TE_SUPERSPIKE:			// super spike hitting wall
                    pos = this.Host.Network.Reader.ReadCoords();
                    this.Host.RenderContext.Particles.RunParticleEffect(this.Host.Client.cl.time, ref pos, ref Utilities.ZeroVector, 0, 20 );

                    if( MathLib.Random() % 5 != 0 )
                        this.Host.Sound.StartSound( -1, 0, this._SfxTink1, ref pos, 1, 1 );
                    else
                    {
                        var rnd = MathLib.Random() & 3;
                        if( rnd == 1 )
                            this.Host.Sound.StartSound( -1, 0, this._SfxRic1, ref pos, 1, 1 );
                        else if( rnd == 2 )
                            this.Host.Sound.StartSound( -1, 0, this._SfxRic2, ref pos, 1, 1 );
                        else
                            this.Host.Sound.StartSound( -1, 0, this._SfxRic3, ref pos, 1, 1 );
                    }
                    break;

                case ProtocolDef.TE_GUNSHOT:			// bullet hitting wall
                    pos = this.Host.Network.Reader.ReadCoords();
                    this.Host.RenderContext.Particles.RunParticleEffect(this.Host.Client.cl.time, ref pos, ref Utilities.ZeroVector, 0, 20 );
                    break;

                case ProtocolDef.TE_EXPLOSION:			// rocket explosion
                    pos = this.Host.Network.Reader.ReadCoords();
                    this.Host.RenderContext.Particles.ParticleExplosion(this.Host.Client.cl.time, ref pos );
                    dl = this.AllocDlight( 0 );
                    dl.origin = pos;
                    dl.radius = 350;
                    dl.die = ( float )this.cl.time + 0.5f;
                    dl.decay = 300;
                    this.Host.Sound.StartSound( -1, 0, this._SfxRExp3, ref pos, 1, 1 );
                    break;

                case ProtocolDef.TE_TAREXPLOSION:			// tarbaby explosion
                    pos = this.Host.Network.Reader.ReadCoords();
                    this.Host.RenderContext.Particles.BlobExplosion(this.Host.Client.cl.time, ref pos );
                    this.Host.Sound.StartSound( -1, 0, this._SfxRExp3, ref pos, 1, 1 );
                    break;

                case ProtocolDef.TE_LIGHTNING1:				// lightning bolts
                    this.ParseBeam(this.Host.Model.ForName( "progs/bolt.mdl", true, ModelType.mod_alias ) );
                    break;

                case ProtocolDef.TE_LIGHTNING2:				// lightning bolts
                    this.ParseBeam(this.Host.Model.ForName( "progs/bolt2.mdl", true, ModelType.mod_alias) );
                    break;

                case ProtocolDef.TE_LIGHTNING3:				// lightning bolts
                    this.ParseBeam(this.Host.Model.ForName( "progs/bolt3.mdl", true, ModelType.mod_alias ) );
                    break;

                // PGM 01/21/97
                case ProtocolDef.TE_BEAM:				// grappling hook beam
                    this.ParseBeam(this.Host.Model.ForName( "progs/beam.mdl", true, ModelType.mod_alias ) );
                    break;
                // PGM 01/21/97

                case ProtocolDef.TE_LAVASPLASH:
                    pos = this.Host.Network.Reader.ReadCoords();
                    this.Host.RenderContext.Particles.LavaSplash(this.Host.Client.cl.time, ref pos );
                    break;

                case ProtocolDef.TE_TELEPORT:
                    pos = this.Host.Network.Reader.ReadCoords();
                    this.Host.RenderContext.Particles.TeleportSplash(this.Host.Client.cl.time, ref pos );
                    break;

                case ProtocolDef.TE_EXPLOSION2:				// color mapped explosion
                    pos = this.Host.Network.Reader.ReadCoords();
                    var colorStart = this.Host.Network.Reader.ReadByte();
                    var colorLength = this.Host.Network.Reader.ReadByte();
                    this.Host.RenderContext.Particles.ParticleExplosion(this.Host.Client.cl.time, ref pos, colorStart, colorLength );
                    dl = this.AllocDlight( 0 );
                    dl.origin = pos;
                    dl.radius = 350;
                    dl.die = ( float )this.cl.time + 0.5f;
                    dl.decay = 300;
                    this.Host.Sound.StartSound( -1, 0, this._SfxRExp3, ref pos, 1, 1 );
                    break;

                default:
                    Utilities.Error( "CL_ParseTEnt: bad type" );
                    break;
            }
        }

        /// <summary>
        /// CL_ParseBeam
        /// </summary>
        private void ParseBeam( ModelData m )
        {
            var ent = this.Host.Network.Reader.ReadShort();

            var start = this.Host.Network.Reader.ReadCoords();
            var end = this.Host.Network.Reader.ReadCoords();

            // override any beam with the same entity
            for( var i = 0; i < ClientDef.MAX_BEAMS; i++ )
            {
                var b = this._Beams[i];
                if( b.entity == ent )
                {
                    b.entity = ent;
                    b.model = m;
                    b.endtime = ( float ) (this.cl.time + 0.2 );
                    b.start = start;
                    b.end = end;
                    return;
                }
            }

            // find a free beam
            for( var i = 0; i < ClientDef.MAX_BEAMS; i++ )
            {
                var b = this._Beams[i];
                if( b.model == null || b.endtime < this.cl.time )
                {
                    b.entity = ent;
                    b.model = m;
                    b.endtime = ( float ) (this.cl.time + 0.2 );
                    b.start = start;
                    b.end = end;
                    return;
                }
            }

            this.Host.Console.Print( "beam list overflow!\n" );
        }
    }
}
