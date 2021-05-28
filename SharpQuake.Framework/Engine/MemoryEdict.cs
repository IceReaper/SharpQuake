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

namespace SharpQuake.Framework.Engine
{
    using Definitions;
    using System;

    /// <summary>
    /// In-memory edict
    /// </summary>
    public class MemoryEdict
    {
        public bool free;
        public Link area; // linked to a division node or leaf

        public int num_leafs;
        public short[] leafnums; // [MAX_ENT_LEAFS];

        public EntityState baseline;

        public float freetime;			// sv.time when the object was freed
        public EntVars v;					// C exported fields from progs
        public float[] fields; // other fields from progs

        public void Clear( )
        {
            this.v = default;
            if (this.fields != null )
                Array.Clear(this.fields, 0, this.fields.Length );

            this.free = false;
        }

        public bool IsV( int offset, out int correctedOffset )
        {
            if ( offset < EntVars.SizeInBytes >> 2 )
            {
                correctedOffset = offset;
                return true;
            }
            correctedOffset = offset - ( EntVars.SizeInBytes >> 2 );
            return false;
        }

        public unsafe void LoadInt( int offset, EVal* result )
        {
            int offset1;
            if (this.IsV( offset, out offset1 ) )
            {
                fixed ( void* pv = &this.v )
                {
                    var a = ( EVal* ) ( ( int* ) pv + offset1 );
                    result->_int = a->_int;
                }
            }
            else
            {
                fixed ( void* pv = this.fields )
                {
                    var a = ( EVal* ) ( ( int* ) pv + offset1 );
                    result->_int = a->_int;
                }
            }
        }

        public unsafe void StoreInt( int offset, EVal* value )
        {
            int offset1;
            if (this.IsV( offset, out offset1 ) )
            {
                fixed ( void* pv = &this.v )
                {
                    var a = ( EVal* ) ( ( int* ) pv + offset1 );
                    a->_int = value->_int;
                }
            }
            else
            {
                fixed ( void* pv = this.fields )
                {
                    var a = ( EVal* ) ( ( int* ) pv + offset1 );
                    a->_int = value->_int;
                }
            }
        }

        public unsafe void LoadVector( int offset, EVal* result )
        {
            int offset1;
            if (this.IsV( offset, out offset1 ) )
            {
                fixed ( void* pv = &this.v )
                {
                    var a = ( EVal* ) ( ( int* ) pv + offset1 );
                    result->vector[0] = a->vector[0];
                    result->vector[1] = a->vector[1];
                    result->vector[2] = a->vector[2];
                }
            }
            else
            {
                fixed ( void* pf = this.fields )
                {
                    var a = ( EVal* ) ( ( int* ) pf + offset1 );
                    result->vector[0] = a->vector[0];
                    result->vector[1] = a->vector[1];
                    result->vector[2] = a->vector[2];
                }
            }
        }

        public unsafe void StoreVector( int offset, EVal* value )
        {
            int offset1;
            if (this.IsV( offset, out offset1 ) )
            {
                fixed ( void* pv = &this.v )
                {
                    var a = ( EVal* ) ( ( int* ) pv + offset1 );
                    a->vector[0] = value->vector[0];
                    a->vector[1] = value->vector[1];
                    a->vector[2] = value->vector[2];
                }
            }
            else
            {
                fixed ( void* pf = this.fields )
                {
                    var a = ( EVal* ) ( ( int* ) pf + offset1 );
                    a->vector[0] = value->vector[0];
                    a->vector[1] = value->vector[1];
                    a->vector[2] = value->vector[2];
                }
            }
        }

        public unsafe int GetInt( int offset )
        {
            int offset1, result;
            if (this.IsV( offset, out offset1 ) )
            {
                fixed ( void* pv = &this.v )
                {
                    var a = ( EVal* ) ( ( int* ) pv + offset1 );
                    result = a->_int;
                }
            }
            else
            {
                fixed ( void* pv = this.fields )
                {
                    var a = ( EVal* ) ( ( int* ) pv + offset1 );
                    result = a->_int;
                }
            }
            return result;
        }

        public unsafe float GetFloat( int offset )
        {
            int offset1;
            float result;
            if (this.IsV( offset, out offset1 ) )
            {
                fixed ( void* pv = &this.v )
                {
                    var a = ( EVal* ) ( ( float* ) pv + offset1 );
                    result = a->_float;
                }
            }
            else
            {
                fixed ( void* pv = this.fields )
                {
                    var a = ( EVal* ) ( ( float* ) pv + offset1 );
                    result = a->_float;
                }
            }
            return result;
        }

        public unsafe void SetFloat( int offset, float value )
        {
            int offset1;
            if (this.IsV( offset, out offset1 ) )
            {
                fixed ( void* pv = &this.v )
                {
                    var a = ( EVal* ) ( ( float* ) pv + offset1 );
                    a->_float = value;
                }
            }
            else
            {
                fixed ( void* pv = this.fields )
                {
                    var a = ( EVal* ) ( ( float* ) pv + offset1 );
                    a->_float = value;
                }
            }
        }

        public MemoryEdict( )
        {
            this.area = new( this );
            this.leafnums = new short[ProgramDef.MAX_ENT_LEAFS];
            this.fields = new float[( ProgramDef.EdictSize - EntVars.SizeInBytes ) >> 2];
        }
    } // edict_t;
}
