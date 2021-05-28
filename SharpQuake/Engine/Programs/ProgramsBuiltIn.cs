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



// pr_cmds.c

namespace SharpQuake.Engine.Programs
{
    using Framework.Definitions;
    using Framework.Definitions.Bsp;
    using Framework.Engine;
    using Framework.Mathematics;
    using Game.Data.Models;
    using Host;
    using System;
    using System.Numerics;
    using System.Text;

    public class ProgramsBuiltIn
    {
        public int Count => ProgramsBuiltIn._Builtin.Length;

        /// <summary>
        /// WriteDest()
        /// </summary>
        private MessageWriter WriteDest
        {
            get
            {
                var dest = ( int )this.GetFloat( ProgramOperatorDef.OFS_PARM0 );
                switch ( dest )
                {
                    case ProgramsBuiltIn.MSG_BROADCAST:
                        return this.Host.Server.sv.datagram;

                    case ProgramsBuiltIn.MSG_ONE:
                        var ent = this.Host.Server.ProgToEdict(this.Host.Programs.GlobalStruct.msg_entity );
                        var entnum = this.Host.Server.NumForEdict( ent );
                        if ( entnum < 1 || entnum > this.Host.Server.svs.maxclients )
                            this.Host.Programs.RunError( "WriteDest: not a client" );
                        return this.Host.Server.svs.clients[entnum - 1].message;

                    case ProgramsBuiltIn.MSG_ALL:
                        return this.Host.Server.sv.reliable_datagram;

                    case ProgramsBuiltIn.MSG_INIT:
                        return this.Host.Server.sv.signon;

                    default:
                        this.Host.Programs.RunError( "WriteDest: bad destination" );
                        break;
                }

                return null;
            }
        }

        private const int MSG_BROADCAST = 0;	// unreliable to all
        private const int MSG_ONE = 1;		// reliable to one (msg_entity)
        private const int MSG_ALL = 2;		// reliable to all
        private const int MSG_INIT = 3;		// write to the init string

        private static builtin_t[] _Builtin;

        private byte[] _CheckPvs = new byte[BspDef.MAX_MAP_LEAFS / 8]; // checkpvs

        private int _TempString = -1;

        private int _InVisCount; // c_invis
        private int _NotVisCount; // c_notvis

        // Instances
        private Host Host
        {
            get;
            set;
        }

        public ProgramsBuiltIn( Host host )
        {
            this.Host = host;
        }

        public void Initialise( )
        {
            ProgramsBuiltIn._Builtin = new builtin_t[]
            {
                this.PF_Fixme,
                this.PF_makevectors,	// void(entity e)	makevectors 		= #1;
                this.PF_setorigin,	// void(entity e, vector o) setorigin	= #2;
                this.PF_setmodel,	// void(entity e, string m) setmodel	= #3;
                this.PF_setsize,	// void(entity e, vector min, vector max) setsize = #4;
                this.PF_Fixme,	// void(entity e, vector min, vector max) setabssize = #5;
                this.PF_break,	// void() break						= #6;
                this.PF_random,	// float() random						= #7;
                this.PF_sound,	// void(entity e, float chan, string samp) sound = #8;
                this.PF_normalize,	// vector(vector v) normalize			= #9;
                this.PF_error,	// void(string e) error				= #10;
                this.PF_objerror,	// void(string e) objerror				= #11;
                this.PF_vlen,	// float(vector v) vlen				= #12;
                this.PF_vectoyaw,	// float(vector v) vectoyaw		= #13;
                this.PF_Spawn,	// entity() spawn						= #14;
                this.PF_Remove,	// void(entity e) remove				= #15;
                this.PF_traceline,	// float(vector v1, vector v2, float tryents) traceline = #16;
                this.PF_checkclient,	// entity() clientlist					= #17;
                this.PF_Find,	// entity(entity start, .string fld, string match) find = #18;
                this.PF_precache_sound,	// void(string s) precache_sound		= #19;
                this.PF_precache_model,	// void(string s) precache_model		= #20;
                this.PF_stuffcmd,	// void(entity client, string s)stuffcmd = #21;
                this.PF_findradius,	// entity(vector org, float rad) findradius = #22;
                this.PF_bprint,	// void(string s) bprint				= #23;
                this.PF_sprint,	// void(entity client, string s) sprint = #24;
                this.PF_dprint,	// void(string s) dprint				= #25;
                this.PF_ftos,	// void(string s) ftos				= #26;
                this.PF_vtos,	// void(string s) vtos				= #27;
                this.PF_coredump,
                this.PF_traceon,
                this.PF_traceoff,
                this.PF_eprint,	// void(entity e) debug print an entire entity
                this.PF_walkmove, // float(float yaw, float dist) walkmove
                this.PF_Fixme, // float(float yaw, float dist) walkmove
                this.PF_droptofloor,
                this.PF_lightstyle,
                this.PF_rint,
                this.PF_floor,
                this.PF_ceil,
                this.PF_Fixme,
                this.PF_checkbottom,
                this.PF_pointcontents,
                this.PF_Fixme,
                this.PF_fabs,
                this.PF_aim,
                this.PF_cvar,
                this.PF_localcmd,
                this.PF_nextent,
                this.PF_particle,
                this.PF_changeyaw,
                this.PF_Fixme,
                this.PF_vectoangles,
                this.PF_WriteByte,
                this.PF_WriteChar,
                this.PF_WriteShort,
                this.PF_WriteLong,
                this.PF_WriteCoord,
                this.PF_WriteAngle,
                this.PF_WriteString,
                this.PF_WriteEntity,
                this.PF_Fixme,
                this.PF_Fixme,
                this.PF_Fixme,
                this.PF_Fixme,
                this.PF_Fixme,
                this.PF_Fixme,
                this.PF_Fixme,
                this.Host.Server.MoveToGoal,
                this.PF_precache_file,
                this.PF_makestatic,
                this.PF_changelevel,
                this.PF_Fixme,
                this.PF_cvar_set,
                this.PF_centerprint,
                this.PF_ambientsound,
                this.PF_precache_model,
                this.PF_precache_sound,		// precache_sound2 is different only for qcc
                this.PF_precache_file,
                this.PF_setspawnparms
            };
        }

        public void Execute( int num )
        {
            ProgramsBuiltIn._Builtin[num]( );
        }

        /// <summary>
        /// Called by Host.Programs.LoadProgs()
        /// </summary>
        public void ClearState( )
        {
            this._TempString = -1;
        }

        /// <summary>
        /// RETURN_EDICT(e) (((int *)pr_globals)[OFS_RETURN] = EDICT_TO_PROG(e))
        /// </summary>
        public unsafe void ReturnEdict( MemoryEdict e )
        {
            var prog = this.Host.Server.EdictToProg( e );
            this.ReturnInt( prog );
        }

        /// <summary>
        /// G_INT(OFS_RETURN) = value
        /// </summary>
        public unsafe void ReturnInt( int value )
        {
            var ptr = ( int* )this.Host.Programs.GlobalStructAddr;
            ptr[ProgramOperatorDef.OFS_RETURN] = value;
        }

        /// <summary>
        /// G_FLOAT(OFS_RETURN) = value
        /// </summary>
        public unsafe void ReturnFloat( float value )
        {
            var ptr = ( float* )this.Host.Programs.GlobalStructAddr;
            ptr[ProgramOperatorDef.OFS_RETURN] = value;
        }

        /// <summary>
        /// G_VECTOR(OFS_RETURN) = value
        /// </summary>
        public unsafe void ReturnVector( ref Vector3 value )
        {
            var ptr = ( float* )this.Host.Programs.GlobalStructAddr;
            ptr[ProgramOperatorDef.OFS_RETURN + 0] = value.X;
            ptr[ProgramOperatorDef.OFS_RETURN + 1] = value.Y;
            ptr[ProgramOperatorDef.OFS_RETURN + 2] = value.Z;
        }

        /// <summary>
        /// #define	G_STRING(o) (pr_strings + *(string_t *)&pr_globals[o])
        /// </summary>
        public unsafe string GetString( int parm )
        {
            var ptr = ( int* )this.Host.Programs.GlobalStructAddr;
            return this.Host.Programs.GetString( ptr[parm] );
        }

        /// <summary>
        /// G_INT(o)
        /// </summary>
        public unsafe int GetInt( int parm )
        {
            var ptr = ( int* )this.Host.Programs.GlobalStructAddr;
            return ptr[parm];
        }

        /// <summary>
        /// G_FLOAT(o)
        /// </summary>
        public unsafe float GetFloat( int parm )
        {
            var ptr = ( float* )this.Host.Programs.GlobalStructAddr;
            return ptr[parm];
        }

        /// <summary>
        /// G_VECTOR(o)
        /// </summary>
        public unsafe float* GetVector( int parm )
        {
            var ptr = ( float* )this.Host.Programs.GlobalStructAddr;
            return &ptr[parm];
        }

        /// <summary>
        /// #define	G_EDICT(o) ((edict_t *)((byte *)sv.edicts+ *(int *)&pr_globals[o]))
        /// </summary>
        public unsafe MemoryEdict GetEdict( int parm )
        {
            var ptr = ( int* )this.Host.Programs.GlobalStructAddr;
            var ed = this.Host.Server.ProgToEdict( ptr[parm] );
            return ed;
        }

        public void PF_changeyaw( )
        {
            var ent = this.Host.Server.ProgToEdict(this.Host.Programs.GlobalStruct.self );
            var current = MathLib.AngleMod( ent.v.angles.Y );
            var ideal = ent.v.ideal_yaw;
            var speed = ent.v.yaw_speed;

            if ( current == ideal )
                return;

            var move = ideal - current;
            if ( ideal > current )
            {
                if ( move >= 180 )
                    move = move - 360;
            }
            else
            {
                if ( move <= -180 )
                    move = move + 360;
            }
            if ( move > 0 )
            {
                if ( move > speed )
                    move = speed;
            }
            else
            {
                if ( move < -speed )
                    move = -speed;
            }

            ent.v.angles.Y = MathLib.AngleMod( current + move );
        }

        private int SetTempString( string value )
        {
            if (this._TempString == -1 )
                this._TempString = this.Host.Programs.NewString( value );
            else
                this.Host.Programs.SetString(this._TempString, value );

            return this._TempString;
        }

        private string PF_VarString( int first )
        {
            var sb = new StringBuilder( 256 );
            for ( var i = first; i < this.Host.Programs.Argc; i++ )
                sb.Append(this.GetString( ProgramOperatorDef.OFS_PARM0 + i * 3 ) );

            return sb.ToString( );
        }

        private unsafe void Copy( float* src, out Vector3 dest )
        {
            dest.X = src[0];
            dest.Y = src[1];
            dest.Z = src[2];
        }

        /// <summary>
        /// PF_errror
        /// This is a TERMINAL error, which will kill off the entire Host.Server.
        /// Dumps self.
        /// error(value)
        /// </summary>
        private void PF_error( )
        {
            var s = this.PF_VarString( 0 );

            this.Host.Console.Print( "======SERVER ERROR in {0}:\n{1}\n", this.Host.Programs.GetString(this.Host.Programs.xFunction.s_name ), s );
            var ed = this.Host.Server.ProgToEdict(this.Host.Programs.GlobalStruct.self );
            this.Host.Programs.Print( ed );
            this.Host.Error( "Program error" );
        }

        /*
        =================
        PF_objerror

        Dumps out self, then an error message.  The program is aborted and self is
        removed, but the level can continue.

        objerror(value)
        =================
        */

        private void PF_objerror( )
        {
            var s = this.PF_VarString( 0 );

            this.Host.Console.Print( "======OBJECT ERROR in {0}:\n{1}\n", this.GetString(this.Host.Programs.xFunction.s_name ), s );
            var ed = this.Host.Server.ProgToEdict(this.Host.Programs.GlobalStruct.self );
            this.Host.Programs.Print( ed );
            this.Host.Server.FreeEdict( ed );
            this.Host.Error( "Program error" );
        }

        /*
        ==============
        PF_makevectors

        Writes new values for v_forward, v_up, and v_right based on angles
        makevectors(vector)
        ==============
        */

        private unsafe void PF_makevectors( )
        {
            var av = this.GetVector( ProgramOperatorDef.OFS_PARM0 );
            var a = new Vector3( av[0], av[1], av[2] );
            Vector3 fw, right, up;
            MathLib.AngleVectors( ref a, out fw, out right, out up );
            MathLib.Copy( ref fw, out this.Host.Programs.GlobalStruct.v_forward );
            MathLib.Copy( ref right, out this.Host.Programs.GlobalStruct.v_right );
            MathLib.Copy( ref up, out this.Host.Programs.GlobalStruct.v_up );
        }

        /// <summary>
        /// PF_setorigin
        /// This is the only valid way to move an object without using the physics of the world (setting velocity and waiting).
        /// Directly changing origin will not set internal links correctly, so clipping would be messed up.
        /// This should be called when an object is spawned, and then only if it is teleported.
        /// setorigin (entity, origin)
        /// </summary>
        private unsafe void PF_setorigin( )
        {
            var e = this.GetEdict( ProgramOperatorDef.OFS_PARM0 );
            var org = this.GetVector( ProgramOperatorDef.OFS_PARM1 );
            this.Copy( org, out e.v.origin );

            this.Host.Server.LinkEdict( e, false );
        }

        private void SetMinMaxSize( MemoryEdict e, ref Vector3 min, ref Vector3 max, bool rotate )
        {
            if ( min.X > max.X || min.Y > max.Y || min.Z > max.Z )
                this.Host.Programs.RunError( "backwards mins/maxs" );

            rotate = false;		// FIXME: implement rotation properly again

            Vector3 rmin = min, rmax = max;
            if ( !rotate )
            {
                //rmin = min;
                //rmax = max;
            }
            else
            {
                // find min / max for rotations
                //angles = e.v.angles;

                //a = angles[1] / 180 * M_PI;

                //xvector[0] = cos(a);
                //xvector[1] = sin(a);
                //yvector[0] = -sin(a);
                //yvector[1] = cos(a);

                //VectorCopy(min, bounds[0]);
                //VectorCopy(max, bounds[1]);

                //rmin[0] = rmin[1] = rmin[2] = 9999;
                //rmax[0] = rmax[1] = rmax[2] = -9999;

                //for (i = 0; i <= 1; i++)
                //{
                //    base[0] = bounds[i][0];
                //    for (j = 0; j <= 1; j++)
                //    {
                //        base[1] = bounds[j][1];
                //        for (k = 0; k <= 1; k++)
                //        {
                //            base[2] = bounds[k][2];

                //            // transform the point
                //            transformed[0] = xvector[0] * base[0] + yvector[0] * base[1];
                //            transformed[1] = xvector[1] * base[0] + yvector[1] * base[1];
                //            transformed[2] = base[2];

                //            for (l = 0; l < 3; l++)
                //            {
                //                if (transformed[l] < rmin[l])
                //                    rmin[l] = transformed[l];
                //                if (transformed[l] > rmax[l])
                //                    rmax[l] = transformed[l];
                //            }
                //        }
                //    }
                //}
            }

            // set derived values
            MathLib.Copy( ref rmin, out e.v.mins );
            MathLib.Copy( ref rmax, out e.v.maxs );
            var s = max - min;
            MathLib.Copy( ref s, out e.v.size );

            this.Host.Server.LinkEdict( e, false );
        }

        /*
        =================
        PF_setsize

        the size box is rotated by the current angle

        setsize (entity, minvector, maxvector)
        =================
        */

        private unsafe void PF_setsize( )
        {
            var e = this.GetEdict( ProgramOperatorDef.OFS_PARM0 );
            var min = this.GetVector( ProgramOperatorDef.OFS_PARM1 );
            var max = this.GetVector( ProgramOperatorDef.OFS_PARM2 );
            Vector3 vmin, vmax;
            this.Copy( min, out vmin );
            this.Copy( max, out vmax );
            this.SetMinMaxSize( e, ref vmin, ref vmax, false );
        }

        /*
        =================
        PF_setmodel

        setmodel(entity, model)
        =================
        */

        private void PF_setmodel( )
        {
            var e = this.GetEdict( ProgramOperatorDef.OFS_PARM0 );
            var m_idx = this.GetInt( ProgramOperatorDef.OFS_PARM1 );
            var m = this.Host.Programs.GetString( m_idx );

            // check to see if model was properly precached
            for ( var i = 0; i < this.Host.Server.sv.model_precache.Length; i++ )
            {
                var check = this.Host.Server.sv.model_precache[i];

                if ( check == null )
                    break;

                if ( check == m )
                {
                    e.v.model = m_idx; // m - pr_strings;
                    e.v.modelindex = i;

                    var mod = this.Host.Server.sv.models[( int ) e.v.modelindex];

                    if ( mod != null )
                    {
                        var mins = mod.BoundsMin;
                        var maxs = mod.BoundsMax;

                        this.SetMinMaxSize( e, ref mins, ref maxs, true );

                        mod.BoundsMin = mins;
                        mod.BoundsMax = maxs;
                    }
                    else
                        this.SetMinMaxSize( e, ref Utilities.ZeroVector, ref Utilities.ZeroVector, true );

                    return;
                }
            }

            this.Host.Programs.RunError( "no precache: {0}\n", m );
        }

        /*
        =================
        PF_bprint

        broadcast print to everyone on server

        bprint(value)
        =================
        */

        private void PF_bprint( )
        {
            var s = this.PF_VarString( 0 );
            this.Host.Server.BroadcastPrint( s );
        }

        /// <summary>
        /// PF_sprint
        /// single print to a specific client
        /// sprint(clientent, value)
        /// </summary>
        private void PF_sprint( )
        {
            var entnum = this.Host.Server.NumForEdict(this.GetEdict( ProgramOperatorDef.OFS_PARM0 ) );
            var s = this.PF_VarString( 1 );

            if ( entnum < 1 || entnum > this.Host.Server.svs.maxclients )
            {
                this.Host.Console.Print( "tried to sprint to a non-client\n" );
                return;
            }

            var client = this.Host.Server.svs.clients[entnum - 1];

            client.message.WriteChar( ProtocolDef.svc_print );
            client.message.WriteString( s );
        }

        /*
        =================
        PF_centerprint

        single print to a specific client

        centerprint(clientent, value)
        =================
        */

        private void PF_centerprint( )
        {
            var entnum = this.Host.Server.NumForEdict(this.GetEdict( ProgramOperatorDef.OFS_PARM0 ) );
            var s = this.PF_VarString( 1 );

            if ( entnum < 1 || entnum > this.Host.Server.svs.maxclients )
            {
                this.Host.Console.Print( "tried to centerprint to a non-client\n" );
                return;
            }

            var client = this.Host.Server.svs.clients[entnum - 1];

            client.message.WriteChar( ProtocolDef.svc_centerprint );
            client.message.WriteString( s );
        }

        /*
        =================
        PF_normalize

        vector normalize(vector)
        =================
        */

        private unsafe void PF_normalize( )
        {
            var value1 = this.GetVector( ProgramOperatorDef.OFS_PARM0 );
            Vector3 tmp;
            this.Copy( value1, out tmp );
            MathLib.Normalize( ref tmp );

            this.ReturnVector( ref tmp );
        }

        /*
        =================
        PF_vlen

        scalar vlen(vector)
        =================
        */

        private unsafe void PF_vlen( )
        {
            var v = this.GetVector( ProgramOperatorDef.OFS_PARM0 );
            var result = ( float ) Math.Sqrt( v[0] * v[0] + v[1] * v[1] + v[2] * v[2] );

            this.ReturnFloat( result );
        }

        /// <summary>
        /// PF_vectoyaw
        /// float vectoyaw(vector)
        /// </summary>
        private unsafe void PF_vectoyaw( )
        {
            var value1 = this.GetVector( ProgramOperatorDef.OFS_PARM0 );
            float yaw;
            if ( value1[1] == 0 && value1[0] == 0 )
                yaw = 0;
            else
            {
                yaw = ( int ) ( Math.Atan2( value1[1], value1[0] ) * 180 / Math.PI );
                if ( yaw < 0 )
                    yaw += 360;
            }

            this.ReturnFloat( yaw );
        }

        /*
        =================
        PF_vectoangles

        vector vectoangles(vector)
        =================
        */

        private unsafe void PF_vectoangles( )
        {
            float yaw, pitch, forward;
            var value1 = this.GetVector( ProgramOperatorDef.OFS_PARM0 );

            if ( value1[1] == 0 && value1[0] == 0 )
            {
                yaw = 0;
                if ( value1[2] > 0 )
                    pitch = 90;
                else
                    pitch = 270;
            }
            else
            {
                yaw = ( int ) ( Math.Atan2( value1[1], value1[0] ) * 180 / Math.PI );
                if ( yaw < 0 )
                    yaw += 360;

                forward = ( float ) Math.Sqrt( value1[0] * value1[0] + value1[1] * value1[1] );
                pitch = ( int ) ( Math.Atan2( value1[2], forward ) * 180 / Math.PI );
                if ( pitch < 0 )
                    pitch += 360;
            }

            var result = new Vector3( pitch, yaw, 0 );
            this.ReturnVector( ref result );
        }

        /*
        =================
        PF_Random

        Returns a number from 0<= num < 1

        random()
        =================
        */

        private void PF_random( )
        {
            var num = ( MathLib.Random( ) & 0x7fff ) / ( float ) 0x7fff;
            this.ReturnFloat( num );
        }

        /*
        =================
        PF_particle

        particle(origin, color, count)
        =================
        */

        private unsafe void PF_particle( )
        {
            var org = this.GetVector( ProgramOperatorDef.OFS_PARM0 );
            var dir = this.GetVector( ProgramOperatorDef.OFS_PARM1 );
            var color = this.GetFloat( ProgramOperatorDef.OFS_PARM2 );
            var count = this.GetFloat( ProgramOperatorDef.OFS_PARM3 );
            Vector3 vorg, vdir;
            this.Copy( org, out vorg );
            this.Copy( dir, out vdir );
            this.Host.Server.StartParticle( ref vorg, ref vdir, ( int ) color, ( int ) count );
        }

        /*
        =================
        PF_ambientsound

        =================
        */

        private unsafe void PF_ambientsound( )
        {
            var pos = this.GetVector( ProgramOperatorDef.OFS_PARM0 );
            var samp = this.GetString( ProgramOperatorDef.OFS_PARM1 );
            var vol = this.GetFloat( ProgramOperatorDef.OFS_PARM2 );
            var attenuation = this.GetFloat( ProgramOperatorDef.OFS_PARM3 );

            // check to see if samp was properly precached
            for ( var i = 0; i < this.Host.Server.sv.sound_precache.Length; i++ )
            {
                if (this.Host.Server.sv.sound_precache[i] == null )
                    break;

                if ( samp == this.Host.Server.sv.sound_precache[i] )
                {
                    // add an svc_spawnambient command to the level signon packet
                    var msg = this.Host.Server.sv.signon;

                    msg.WriteByte( ProtocolDef.svc_spawnstaticsound );
                    for ( var i2 = 0; i2 < 3; i2++ )
                        msg.WriteCoord( pos[i2] );

                    msg.WriteByte( i );

                    msg.WriteByte( ( int ) ( vol * 255 ) );
                    msg.WriteByte( ( int ) ( attenuation * 64 ) );

                    return;
                }
            }

            this.Host.Console.Print( "no precache: {0}\n", samp );
        }

        /*
        =================
        PF_sound

        Each entity can have eight independant sound sources, like voice,
        weapon, feet, etc.

        Channel 0 is an auto-allocate channel, the others override anything
        allready running on that entity/channel pair.

        An attenuation of 0 will play full volume everywhere in the level.
        Larger attenuations will drop off.

        =================
        */

        private void PF_sound( )
        {
            var entity = this.GetEdict( ProgramOperatorDef.OFS_PARM0 );
            var channel = ( int )this.GetFloat( ProgramOperatorDef.OFS_PARM1 );
            var sample = this.GetString( ProgramOperatorDef.OFS_PARM2 );
            var volume = ( int ) (this.GetFloat( ProgramOperatorDef.OFS_PARM3 ) * 255 );
            var attenuation = this.GetFloat( ProgramOperatorDef.OFS_PARM4 );

            this.Host.Server.StartSound( entity, channel, sample, volume, attenuation );
        }

        /*
        =================
        PF_break

        break()
        =================
        */

        private void PF_break( )
        {
            this.Host.Console.Print( "break statement\n" );
            //*(int *)-4 = 0;	// dump to debugger
        }

        /*
        =================
        PF_traceline

        Used for use tracing and shot targeting
        Traces are blocked by bbox and exact bsp entityes, and also slide box entities
        if the tryents flag is set.

        traceline (vector1, vector2, tryents)
        =================
        */

        private unsafe void PF_traceline( )
        {
            var v1 = this.GetVector( ProgramOperatorDef.OFS_PARM0 );
            var v2 = this.GetVector( ProgramOperatorDef.OFS_PARM1 );
            var nomonsters = ( int )this.GetFloat( ProgramOperatorDef.OFS_PARM2 );
            var ent = this.GetEdict( ProgramOperatorDef.OFS_PARM3 );

            Vector3 vec1, vec2;
            this.Copy( v1, out vec1 );
            this.Copy( v2, out vec2 );
            var trace = this.Host.Server.Move( ref vec1, ref Utilities.ZeroVector, ref Utilities.ZeroVector, ref vec2, nomonsters, ent );

            this.Host.Programs.GlobalStruct.trace_allsolid = trace.allsolid ? 1 : 0;
            this.Host.Programs.GlobalStruct.trace_startsolid = trace.startsolid ? 1 : 0;
            this.Host.Programs.GlobalStruct.trace_fraction = trace.fraction;
            this.Host.Programs.GlobalStruct.trace_inwater = trace.inwater ? 1 : 0;
            this.Host.Programs.GlobalStruct.trace_inopen = trace.inopen ? 1 : 0;
            MathLib.Copy( ref trace.endpos, out this.Host.Programs.GlobalStruct.trace_endpos );
            MathLib.Copy( ref trace.plane.normal, out this.Host.Programs.GlobalStruct.trace_plane_normal );
            this.Host.Programs.GlobalStruct.trace_plane_dist = trace.plane.dist;
            if ( trace.ent != null )
                this.Host.Programs.GlobalStruct.trace_ent = this.Host.Server.EdictToProg( trace.ent );
            else
                this.Host.Programs.GlobalStruct.trace_ent = this.Host.Server.EdictToProg(this.Host.Server.sv.edicts[0] );
        }

        private int PF_newcheckclient( int check )
        {
            // cycle to the next one

            if ( check < 1 )
                check = 1;
            if ( check > this.Host.Server.svs.maxclients )
                check = this.Host.Server.svs.maxclients;

            var i = check + 1;
            if ( check == this.Host.Server.svs.maxclients )
                i = 1;

            MemoryEdict ent;
            for ( ; ; i++ )
            {
                if ( i == this.Host.Server.svs.maxclients + 1 )
                    i = 1;

                ent = this.Host.Server.EdictNum( i );

                if ( i == check )
                    break;	// didn't find anything else

                if ( ent.free )
                    continue;
                if ( ent.v.health <= 0 )
                    continue;
                if ( ( ( int ) ent.v.flags & EdictFlags.FL_NOTARGET ) != 0 )
                    continue;

                // anything that is a client, or has a client as an enemy
                break;
            }

            // get the PVS for the entity
            var org = Utilities.ToVector( ref ent.v.origin ) + Utilities.ToVector( ref ent.v.view_ofs );
            var leaf = this.Host.Server.sv.worldmodel.PointInLeaf( ref org );
            var pvs = this.Host.Server.sv.worldmodel.LeafPVS( leaf );
            Buffer.BlockCopy( pvs, 0, this._CheckPvs, 0, pvs.Length );

            return i;
        }

        /// <summary>
        /// PF_checkclient
        /// Returns a client (or object that has a client enemy) that would be a
        /// valid target.
        ///
        /// If there are more than one valid options, they are cycled each frame
        ///
        /// If (self.origin + self.viewofs) is not in the PVS of the current target,
        /// it is not returned at all.
        ///
        /// name checkclient ()
        /// </summary>
        private void PF_checkclient( )
        {
            // find a new check if on a new frame
            if (this.Host.Server.sv.time - this.Host.Server.sv.lastchecktime >= 0.1 )
            {
                this.Host.Server.sv.lastcheck = this.PF_newcheckclient(this.Host.Server.sv.lastcheck );
                this.Host.Server.sv.lastchecktime = this.Host.Server.sv.time;
            }

            // return check if it might be visible
            var ent = this.Host.Server.EdictNum(this.Host.Server.sv.lastcheck );
            if ( ent.free || ent.v.health <= 0 )
            {
                this.ReturnEdict(this.Host.Server.sv.edicts[0] );
                return;
            }

            // if current entity can't possibly see the check entity, return 0
            var self = this.Host.Server.ProgToEdict(this.Host.Programs.GlobalStruct.self );
            var view = Utilities.ToVector( ref self.v.origin ) + Utilities.ToVector( ref self.v.view_ofs );
            var leaf = this.Host.Server.sv.worldmodel.PointInLeaf( ref view );
            var l = Array.IndexOf(this.Host.Server.sv.worldmodel.Leaves, leaf ) - 1;
            if ( l < 0 || (this._CheckPvs[l >> 3] & ( 1 << ( l & 7 ) ) ) == 0 )
            {
                this._NotVisCount++;
                this.ReturnEdict(this.Host.Server.sv.edicts[0] );
                return;
            }

            // might be able to see it
            this._InVisCount++;
            this.ReturnEdict( ent );
        }

        //============================================================================

        /// <summary>
        /// PF_stuffcmd
        /// Sends text over to the client's execution buffer
        /// stuffcmd (clientent, value)
        /// </summary>
        private void PF_stuffcmd( )
        {
            var entnum = this.Host.Server.NumForEdict(this.GetEdict( ProgramOperatorDef.OFS_PARM0 ) );
            if ( entnum < 1 || entnum > this.Host.Server.svs.maxclients )
                this.Host.Programs.RunError( "Parm 0 not a client" );
            var str = this.GetString( ProgramOperatorDef.OFS_PARM1 );

            var old = this.Host.HostClient;
            this.Host.HostClient = this.Host.Server.svs.clients[entnum - 1];
            this.Host.ClientCommands( "{0}", str );
            this.Host.HostClient = old;
        }

        /// <summary>
        /// PF_localcmd
        /// Sends text over to the client's execution buffer
        /// localcmd (string)
        /// </summary>
        private void PF_localcmd( )
        {
            var cmd = this.GetString( ProgramOperatorDef.OFS_PARM0 );
            this.Host.Commands.Buffer.Append( cmd );
        }

        /*
        =================
        PF_cvar

        float cvar (string)
        =================
        */

        private void PF_cvar( )
        {
            var str = this.GetString( ProgramOperatorDef.OFS_PARM0 );
            var cvar = this.Host.CVars.Get( str );
            var singleValue = 0f;

            if ( cvar.ValueType == typeof( bool ) )
                singleValue = cvar.Get<bool>( ) ? 1f : 0f;
            else if ( cvar.ValueType == typeof( string ) )
                return;
            else if ( cvar.ValueType == typeof( float ) )
                singleValue = cvar.Get<float>( );
            else if ( cvar.ValueType == typeof( int ) )
                singleValue = ( float ) cvar.Get<int>( );

            this.ReturnFloat( singleValue );
        }

        /*
        =================
        PF_cvar_set

        float cvar (string)
        =================
        */

        private void PF_cvar_set( )
        {
            this.Host.CVars.Set(this.GetString( ProgramOperatorDef.OFS_PARM0 ), this.GetString( ProgramOperatorDef.OFS_PARM1 ) );
        }

        /*
        =================
        PF_findradius

        Returns a chain of entities that have origins within a spherical area

        findradius (origin, radius)
        =================
        */

        private unsafe void PF_findradius( )
        {
            var chain = this.Host.Server.sv.edicts[0];

            var org = this.GetVector( ProgramOperatorDef.OFS_PARM0 );
            var rad = this.GetFloat( ProgramOperatorDef.OFS_PARM1 );

            Vector3 vorg;
            this.Copy( org, out vorg );

            for ( var i = 1; i < this.Host.Server.sv.num_edicts; i++ )
            {
                var ent = this.Host.Server.sv.edicts[i];
                if ( ent.free )
                    continue;
                if ( ent.v.solid == Solids.SOLID_NOT )
                    continue;

                var v = vorg - ( Utilities.ToVector( ref ent.v.origin ) +
                    ( Utilities.ToVector( ref ent.v.mins ) + Utilities.ToVector( ref ent.v.maxs ) ) * 0.5f );
                if ( v.Length() > rad )
                    continue;

                ent.v.chain = this.Host.Server.EdictToProg( chain );
                chain = ent;
            }

            this.ReturnEdict( chain );
        }

        /*
        =========
        PF_dprint
        =========
        */

        private void PF_dprint( )
        {
            this.Host.Console.DPrint(this.PF_VarString( 0 ) );
        }

        private void PF_ftos( )
        {
            var v = this.GetFloat( ProgramOperatorDef.OFS_PARM0 );

            if ( v == ( int ) v )
                this.SetTempString( string.Format( "{0}", ( int ) v ) );
            else
                this.SetTempString( string.Format( "{0:F1}", v ) ); //  sprintf(pr_string_temp, "%5.1f", v);

            this.ReturnInt(this._TempString );
        }

        private void PF_fabs( )
        {
            var v = this.GetFloat( ProgramOperatorDef.OFS_PARM0 );
            this.ReturnFloat( Math.Abs( v ) );
        }

        private unsafe void PF_vtos( )
        {
            var v = this.GetVector( ProgramOperatorDef.OFS_PARM0 );
            this.SetTempString( string.Format( "'{0,5:F1} {1,5:F1} {2,5:F1}'", v[0], v[1], v[2] ) );
            this.ReturnInt(this._TempString );
        }

        private void PF_Spawn( )
        {
            var ed = this.Host.Server.AllocEdict( );
            this.ReturnEdict( ed );
        }

        private void PF_Remove( )
        {
            var ed = this.GetEdict( ProgramOperatorDef.OFS_PARM0 );
            this.Host.Server.FreeEdict( ed );
        }

        /// <summary>
        /// PF_Find
        /// entity (entity start, .string field, string match) find = #5;
        /// </summary>
        private void PF_Find( )
        {
            var e = this.GetInt( ProgramOperatorDef.OFS_PARM0 );
            var f = this.GetInt( ProgramOperatorDef.OFS_PARM1 );
            var s = this.GetString( ProgramOperatorDef.OFS_PARM2 );
            if ( s == null )
                this.Host.Programs.RunError( "PF_Find: bad search string" );

            for ( e++; e < this.Host.Server.sv.num_edicts; e++ )
            {
                var ed = this.Host.Server.EdictNum( e );
                if ( ed.free )
                    continue;
                var t = this.Host.Programs.GetString( ed.GetInt( f ) ); // E_STRING(ed, f);
                if ( string.IsNullOrEmpty( t ) )
                    continue;
                if ( t == s )
                {
                    this.ReturnEdict( ed );
                    return;
                }
            }

            this.ReturnEdict(this.Host.Server.sv.edicts[0] );
        }

        private void CheckEmptyString( string s )
        {
            if ( s == null || s.Length == 0 || s[0] <= ' ' )
                this.Host.Programs.RunError( "Bad string" );
        }

        private void PF_precache_file( )
        {
            // precache_file is only used to copy files with qcc, it does nothing
            this.ReturnInt(this.GetInt( ProgramOperatorDef.OFS_PARM0 ) );
        }

        private void PF_precache_sound( )
        {
            if ( !this.Host.Server.IsLoading )
                this.Host.Programs.RunError( "PF_Precache_*: Precache can only be done in spawn functions" );

            var s = this.GetString( ProgramOperatorDef.OFS_PARM0 );
            this.ReturnInt(this.GetInt( ProgramOperatorDef.OFS_PARM0 ) ); //  G_INT(OFS_RETURN) = G_INT(OFS_PARM0);
            this.CheckEmptyString( s );

            for ( var i = 0; i < QDef.MAX_SOUNDS; i++ )
            {
                if (this.Host.Server.sv.sound_precache[i] == null )
                {
                    this.Host.Server.sv.sound_precache[i] = s;
                    return;
                }
                if (this.Host.Server.sv.sound_precache[i] == s )
                    return;
            }

            this.Host.Programs.RunError( "PF_precache_sound: overflow" );
        }

        private void PF_precache_model( )
        {
            if ( !this.Host.Server.IsLoading )
                this.Host.Programs.RunError( "PF_Precache_*: Precache can only be done in spawn functions" );

            var s = this.GetString( ProgramOperatorDef.OFS_PARM0 );
            this.ReturnInt(this.GetInt( ProgramOperatorDef.OFS_PARM0 ) ); //G_INT(OFS_RETURN) = G_INT(OFS_PARM0);
            this.CheckEmptyString( s );

            for ( var i = 0; i < QDef.MAX_MODELS; i++ )
            {
                if (this.Host.Server.sv.model_precache[i] == null )
                {
                    this.Host.Server.sv.model_precache[i] = s;

                    var n = s.ToLower( );
                    var type = ModelType.mod_sprite;

                    if ( (n.StartsWith( "*" ) && !n.Contains( ".mdl" )) || n.Contains( ".bsp" ) )
                        type = ModelType.mod_brush;
                    else if ( n.Contains( ".mdl" ) )
                        type = ModelType.mod_alias;
                    else
                        type = ModelType.mod_sprite;

                    this.Host.Server.sv.models[i] = this.Host.Model.ForName( s, true, type );
                    return;
                }
                if (this.Host.Server.sv.model_precache[i] == s )
                    return;
            }

            this.Host.Programs.RunError( "PF_precache_model: overflow" );
        }

        private void PF_coredump( )
        {
            this.Host.Programs.PrintEdicts( null );
        }

        private void PF_traceon( )
        {
            this.Host.Programs.Trace = true;
        }

        private void PF_traceoff( )
        {
            this.Host.Programs.Trace = false;
        }

        private void PF_eprint( )
        {
            this.Host.Programs.PrintNum(this.Host.Server.NumForEdict(this.GetEdict( ProgramOperatorDef.OFS_PARM0 ) ) );
        }

        /// <summary>
        /// PF_walkmove
        /// float(float yaw, float dist) walkmove
        /// </summary>
        private void PF_walkmove( )
        {
            var ent = this.Host.Server.ProgToEdict(this.Host.Programs.GlobalStruct.self );
            var yaw = this.GetFloat( ProgramOperatorDef.OFS_PARM0 );
            var dist = this.GetFloat( ProgramOperatorDef.OFS_PARM1 );

            if ( ( ( int ) ent.v.flags & ( EdictFlags.FL_ONGROUND | EdictFlags.FL_FLY | EdictFlags.FL_SWIM ) ) == 0 )
            {
                this.ReturnFloat( 0 );
                return;
            }

            yaw = ( float ) ( yaw * Math.PI * 2.0 / 360.0 );

            Vector3 move;
            move.X = ( float ) Math.Cos( yaw ) * dist;
            move.Y = ( float ) Math.Sin( yaw ) * dist;
            move.Z = 0;

            // save program state, because SV_movestep may call other progs
            var oldf = this.Host.Programs.xFunction;
            var oldself = this.Host.Programs.GlobalStruct.self;

            this.ReturnFloat(this.Host.Server.MoveStep( ent, ref move, true ) ? 1 : 0 );

            // restore program state
            this.Host.Programs.xFunction = oldf;
            this.Host.Programs.GlobalStruct.self = oldself;
        }

        /*
        ===============
        PF_droptofloor

        void() droptofloor
        ===============
        */

        private void PF_droptofloor( )
        {
            var ent = this.Host.Server.ProgToEdict(this.Host.Programs.GlobalStruct.self );

            Vector3 org, mins, maxs;
            MathLib.Copy( ref ent.v.origin, out org );
            MathLib.Copy( ref ent.v.mins, out mins );
            MathLib.Copy( ref ent.v.maxs, out maxs );
            var end = org;
            end.Z -= 256;

            var trace = this.Host.Server.Move( ref org, ref mins, ref maxs, ref end, 0, ent );

            if ( trace.fraction == 1 || trace.allsolid )
                this.ReturnFloat( 0 );
            else
            {
                MathLib.Copy( ref trace.endpos, out ent.v.origin );
                this.Host.Server.LinkEdict( ent, false );
                ent.v.flags = ( int ) ent.v.flags | EdictFlags.FL_ONGROUND;
                ent.v.groundentity = this.Host.Server.EdictToProg( trace.ent );
                this.ReturnFloat( 1 );
            }
        }

        /*
        ===============
        PF_lightstyle

        void(float style, string value) lightstyle
        ===============
        */

        private void PF_lightstyle( )
        {
            var style = ( int )this.GetFloat( ProgramOperatorDef.OFS_PARM0 ); // Uze: ???
            var val = this.GetString( ProgramOperatorDef.OFS_PARM1 );

            // change the string in sv
            this.Host.Server.sv.lightstyles[style] = val;

            // send message to all clients on this server
            if ( !this.Host.Server.IsActive )
                return;

            for ( var j = 0; j < this.Host.Server.svs.maxclients; j++ )
            {
                var client = this.Host.Server.svs.clients[j];
                if ( client.active || client.spawned )
                {
                    client.message.WriteChar( ProtocolDef.svc_lightstyle );
                    client.message.WriteChar( style );
                    client.message.WriteString( val );
                }
            }
        }

        private void PF_rint( )
        {
            var f = this.GetFloat( ProgramOperatorDef.OFS_PARM0 );
            if ( f > 0 )
                this.ReturnFloat( ( int ) ( f + 0.5 ) );
            else
                this.ReturnFloat( ( int ) ( f - 0.5 ) );
        }

        private void PF_floor( )
        {
            this.ReturnFloat( ( float ) Math.Floor(this.GetFloat( ProgramOperatorDef.OFS_PARM0 ) ) );
        }

        private void PF_ceil( )
        {
            this.ReturnFloat( ( float ) Math.Ceiling(this.GetFloat( ProgramOperatorDef.OFS_PARM0 ) ) );
        }

        /// <summary>
        /// PF_checkbottom
        /// </summary>
        private void PF_checkbottom( )
        {
            var ent = this.GetEdict( ProgramOperatorDef.OFS_PARM0 );
            this.ReturnFloat(this.Host.Server.CheckBottom( ent ) ? 1 : 0 );
        }

        /// <summary>
        /// PF_pointcontents
        /// </summary>
        private unsafe void PF_pointcontents( )
        {
            var v = this.GetVector( ProgramOperatorDef.OFS_PARM0 );
            Vector3 tmp;
            this.Copy( v, out tmp );
            this.ReturnFloat(this.Host.Server.PointContents( ref tmp ) );
        }

        /*
        =============
        PF_nextent

        entity nextent(entity)
        =============
        */

        private void PF_nextent( )
        {
            var i = this.Host.Server.NumForEdict(this.GetEdict( ProgramOperatorDef.OFS_PARM0 ) );
            while ( true )
            {
                i++;
                if ( i == this.Host.Server.sv.num_edicts )
                {
                    this.ReturnEdict(this.Host.Server.sv.edicts[0] );
                    return;
                }
                var ent = this.Host.Server.EdictNum( i );
                if ( !ent.free )
                {
                    this.ReturnEdict( ent );
                    return;
                }
            }
        }

        /*
        =============
        PF_aim

        Pick a vector for the player to shoot along
        vector aim(entity, missilespeed)
        =============
        */

        private void PF_aim( )
        {
            var ent = this.GetEdict( ProgramOperatorDef.OFS_PARM0 );
            var speed = this.GetFloat( ProgramOperatorDef.OFS_PARM1 );

            var start = Utilities.ToVector( ref ent.v.origin );
            start.Z += 20;

            // try sending a trace straight
            Vector3 dir;
            MathLib.Copy( ref this.Host.Programs.GlobalStruct.v_forward, out dir );
            var end = start + dir * 2048;
            var tr = this.Host.Server.Move( ref start, ref Utilities.ZeroVector, ref Utilities.ZeroVector, ref end, 0, ent );
            if ( tr.ent != null && tr.ent.v.takedamage == Damages.DAMAGE_AIM &&
                (this.Host.Cvars.TeamPlay.Get<int>( ) == 0 || ent.v.team <= 0 || ent.v.team != tr.ent.v.team ) )
            {
                this.ReturnVector( ref this.Host.Programs.GlobalStruct.v_forward );
                return;
            }

            // try all possible entities
            var bestdir = dir;
            var bestdist = this.Host.Server.Aim;
            MemoryEdict bestent = null;

            for ( var i = 1; i < this.Host.Server.sv.num_edicts; i++ )
            {
                var check = this.Host.Server.sv.edicts[i];
                if ( check.v.takedamage != Damages.DAMAGE_AIM )
                    continue;
                if ( check == ent )
                    continue;
                if (this.Host.Cvars.TeamPlay.Get<int>( ) > 0 && ent.v.team > 0 && ent.v.team == check.v.team )
                    continue;	// don't aim at teammate

                Vector3 tmp;
                MathLib.VectorAdd( ref check.v.mins, ref check.v.maxs, out tmp );
                MathLib.VectorMA( ref check.v.origin, 0.5f, ref tmp, out tmp );
                MathLib.Copy( ref tmp, out end );

                dir = end - start;
                MathLib.Normalize( ref dir );
                var dist = Vector3.Dot( dir, Utilities.ToVector( ref this.Host.Programs.GlobalStruct.v_forward ) );
                if ( dist < bestdist )
                    continue;	// to far to turn
                tr = this.Host.Server.Move( ref start, ref Utilities.ZeroVector, ref Utilities.ZeroVector, ref end, 0, ent );
                if ( tr.ent == check )
                {	// can shoot at this one
                    bestdist = dist;
                    bestent = check;
                }
            }

            if ( bestent != null )
            {
                Vector3 dir2, end2;
                MathLib.VectorSubtract( ref bestent.v.origin, ref ent.v.origin, out dir2 );
                var dist = MathLib.DotProduct( ref dir2, ref this.Host.Programs.GlobalStruct.v_forward );
                MathLib.VectorScale( ref this.Host.Programs.GlobalStruct.v_forward, dist, out end2 );
                end2.Z = dir2.Z;
                MathLib.Normalize( ref end2 );
                this.ReturnVector( ref end2 );
            }
            else
                this.ReturnVector( ref bestdir );
        }

        /*
        ==============
        PF_changeyaw

        This was a major timewaster in progs, so it was converted to C
        ==============
        */

        private void PF_WriteByte( )
        {
            this.WriteDest.WriteByte( ( int )this.GetFloat( ProgramOperatorDef.OFS_PARM1 ) );
        }

        private void PF_WriteChar( )
        {
            this.WriteDest.WriteChar( ( int )this.GetFloat( ProgramOperatorDef.OFS_PARM1 ) );
        }

        private void PF_WriteShort( )
        {
            this.WriteDest.WriteShort( ( int )this.GetFloat( ProgramOperatorDef.OFS_PARM1 ) );
        }

        private void PF_WriteLong( )
        {
            this.WriteDest.WriteLong( ( int )this.GetFloat( ProgramOperatorDef.OFS_PARM1 ) );
        }

        private void PF_WriteAngle( )
        {
            this.WriteDest.WriteAngle(this.GetFloat( ProgramOperatorDef.OFS_PARM1 ) );
        }

        private void PF_WriteCoord( )
        {
            this.WriteDest.WriteCoord(this.GetFloat( ProgramOperatorDef.OFS_PARM1 ) );
        }

        private void PF_WriteString( )
        {
            this.WriteDest.WriteString(this.GetString( ProgramOperatorDef.OFS_PARM1 ) );
        }

        private void PF_WriteEntity( )
        {
            this.WriteDest.WriteShort(this.Host.Server.NumForEdict(this.GetEdict( ProgramOperatorDef.OFS_PARM1 ) ) );
        }

        private void PF_makestatic( )
        {
            var ent = this.GetEdict( ProgramOperatorDef.OFS_PARM0 );
            var msg = this.Host.Server.sv.signon;

            msg.WriteByte( ProtocolDef.svc_spawnstatic );
            msg.WriteByte(this.Host.Server.ModelIndex(this.Host.Programs.GetString( ent.v.model ) ) );
            msg.WriteByte( ( int ) ent.v.frame );
            msg.WriteByte( ( int ) ent.v.colormap );
            msg.WriteByte( ( int ) ent.v.skin );
            for ( var i = 0; i < 3; i++ )
            {
                msg.WriteCoord( MathLib.Comp( ref ent.v.origin, i ) );
                msg.WriteAngle( MathLib.Comp( ref ent.v.angles, i ) );
            }

            // throw the entity away now
            this.Host.Server.FreeEdict( ent );
        }

        /*
        ==============
        PF_setspawnparms
        ==============
        */

        private void PF_setspawnparms( )
        {
            var ent = this.GetEdict( ProgramOperatorDef.OFS_PARM0 );
            var i = this.Host.Server.NumForEdict( ent );
            if ( i < 1 || i > this.Host.Server.svs.maxclients )
                this.Host.Programs.RunError( "Entity is not a client" );

            // copy spawn parms out of the client_t
            var client = this.Host.Server.svs.clients[i - 1];

            this.Host.Programs.GlobalStruct.SetParams( client.spawn_parms );
        }

        /*
        ==============
        PF_changelevel
        ==============
        */

        private void PF_changelevel( )
        {
            // make sure we don't issue two changelevels
            if (this.Host.Server.svs.changelevel_issued )
                return;

            this.Host.Server.svs.changelevel_issued = true;

            var s = this.GetString( ProgramOperatorDef.OFS_PARM0 );
            this.Host.Commands.Buffer.Append( string.Format( "changelevel {0}\n", s ) );
        }

        private void PF_Fixme( )
        {
            this.Host.Programs.RunError( "unimplemented bulitin" );
        }
    }
}
