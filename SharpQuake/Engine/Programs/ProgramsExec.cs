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

namespace SharpQuake.Engine.Programs
{
    using Framework.Definitions;
    using Framework.Engine;
    using Framework.IO;
    using Framework.IO.Programs;

    public partial class Programs
    {
        public int Argc => this._Argc;

        public bool Trace;

        public ProgramFunction xFunction;

        private const int MAX_STACK_DEPTH = 32;

        private const int LOCALSTACK_SIZE = 2048;

        private static readonly string[] OpNames = new string[]
        {
            "DONE",

            "MUL_F",
            "MUL_V",
            "MUL_FV",
            "MUL_VF",

            "DIV",

            "ADD_F",
            "ADD_V",

            "SUB_F",
            "SUB_V",

            "EQ_F",
            "EQ_V",
            "EQ_S",
            "EQ_E",
            "EQ_FNC",

            "NE_F",
            "NE_V",
            "NE_S",
            "NE_E",
            "NE_FNC",

            "LE",
            "GE",
            "LT",
            "GT",

            "INDIRECT",
            "INDIRECT",
            "INDIRECT",
            "INDIRECT",
            "INDIRECT",
            "INDIRECT",

            "ADDRESS",

            "STORE_F",
            "STORE_V",
            "STORE_S",
            "STORE_ENT",
            "STORE_FLD",
            "STORE_FNC",

            "STOREP_F",
            "STOREP_V",
            "STOREP_S",
            "STOREP_ENT",
            "STOREP_FLD",
            "STOREP_FNC",

            "RETURN",

            "NOT_F",
            "NOT_V",
            "NOT_S",
            "NOT_ENT",
            "NOT_FNC",

            "IF",
            "IFNOT",

            "CALL0",
            "CALL1",
            "CALL2",
            "CALL3",
            "CALL4",
            "CALL5",
            "CALL6",
            "CALL7",
            "CALL8",

            "STATE",

            "GOTO",

            "AND",
            "OR",

            "BITAND",
            "BITOR"
        };

        // pr_trace
        private ProgramStack[] _Stack = new ProgramStack[Programs.MAX_STACK_DEPTH]; // pr_stack

        private int _Depth; // pr_depth

        private int[] _LocalStack = new int[Programs.LOCALSTACK_SIZE]; // localstack
        private int _LocalStackUsed; // localstack_used

        // pr_xfunction
        private int _xStatement; // pr_xstatement

        private int _Argc; // pr_argc

        /// <summary>
        /// PR_ExecuteProgram
        /// </summary>
        public unsafe void Execute( int fnum )
        {
            if( fnum < 1 || fnum >= this._Functions.Length )
            {
                if(this.GlobalStruct.self != 0 )
                    this.Print(this.Host.Server.ProgToEdict(this.GlobalStruct.self ) );

                this.Host.Error( "PR_ExecuteProgram: NULL function" );
            }

            var f = this._Functions[fnum];

            var runaway = 100000;
            this.Trace = false;

            // make a stack frame
            var exitdepth = this._Depth;

            int ofs;
            var s = this.EnterFunction( f );
            MemoryEdict ed;

            while( true )
            {
                s++;	// next statement

                var a = (EVal*)this.Get(this._Statements[s].a );
                var b = (EVal*)this.Get(this._Statements[s].b );
                var c = (EVal*)this.Get(this._Statements[s].c );

                if( --runaway == 0 )
                    this.RunError( "runaway loop error" );

                this.xFunction.profile++;
                this._xStatement = s;

                if(this.Trace )
                    this.PrintStatement( ref this._Statements[s] );

                switch( (ProgramOperator)this._Statements[s].op )
                {
                    case ProgramOperator.OP_ADD_F:
                        c->_float = a->_float + b->_float;
                        break;

                    case ProgramOperator.OP_ADD_V:
                        c->vector[0] = a->vector[0] + b->vector[0];
                        c->vector[1] = a->vector[1] + b->vector[1];
                        c->vector[2] = a->vector[2] + b->vector[2];
                        break;

                    case ProgramOperator.OP_SUB_F:
                        c->_float = a->_float - b->_float;
                        break;

                    case ProgramOperator.OP_SUB_V:
                        c->vector[0] = a->vector[0] - b->vector[0];
                        c->vector[1] = a->vector[1] - b->vector[1];
                        c->vector[2] = a->vector[2] - b->vector[2];
                        break;

                    case ProgramOperator.OP_MUL_F:
                        c->_float = a->_float * b->_float;
                        break;

                    case ProgramOperator.OP_MUL_V:
                        c->_float = a->vector[0] * b->vector[0]
                                + a->vector[1] * b->vector[1]
                                + a->vector[2] * b->vector[2];
                        break;

                    case ProgramOperator.OP_MUL_FV:
                        c->vector[0] = a->_float * b->vector[0];
                        c->vector[1] = a->_float * b->vector[1];
                        c->vector[2] = a->_float * b->vector[2];
                        break;

                    case ProgramOperator.OP_MUL_VF:
                        c->vector[0] = b->_float * a->vector[0];
                        c->vector[1] = b->_float * a->vector[1];
                        c->vector[2] = b->_float * a->vector[2];
                        break;

                    case ProgramOperator.OP_DIV_F:
                        c->_float = a->_float / b->_float;
                        break;

                    case ProgramOperator.OP_BITAND:
                        c->_float = ( int ) a->_float & ( int ) b->_float;
                        break;

                    case ProgramOperator.OP_BITOR:
                        c->_float = ( int ) a->_float | ( int ) b->_float;
                        break;

                    case ProgramOperator.OP_GE:
                        c->_float = a->_float >= b->_float ? 1 : 0;
                        break;

                    case ProgramOperator.OP_LE:
                        c->_float = a->_float <= b->_float ? 1 : 0;
                        break;

                    case ProgramOperator.OP_GT:
                        c->_float = a->_float > b->_float ? 1 : 0;
                        break;

                    case ProgramOperator.OP_LT:
                        c->_float = a->_float < b->_float ? 1 : 0;
                        break;

                    case ProgramOperator.OP_AND:
                        c->_float = a->_float != 0 && b->_float != 0 ? 1 : 0;
                        break;

                    case ProgramOperator.OP_OR:
                        c->_float = a->_float != 0 || b->_float != 0 ? 1 : 0;
                        break;

                    case ProgramOperator.OP_NOT_F:
                        c->_float = a->_float != 0 ? 0 : 1;
                        break;

                    case ProgramOperator.OP_NOT_V:
                        c->_float = a->vector[0] == 0 && a->vector[1] == 0 && a->vector[2] == 0 ? 1 : 0;
                        break;

                    case ProgramOperator.OP_NOT_S:
                        c->_float = a->_string == 0 || string.IsNullOrEmpty(this.GetString( a->_string ) ) ? 1 : 0;
                        break;

                    case ProgramOperator.OP_NOT_FNC:
                        c->_float = a->function == 0 ? 1 : 0;
                        break;

                    case ProgramOperator.OP_NOT_ENT:
                        c->_float = this.Host.Server.ProgToEdict( a->edict ) == this.Host.Server.sv.edicts[0] ? 1 : 0;
                        break;

                    case ProgramOperator.OP_EQ_F:
                        c->_float = a->_float == b->_float ? 1 : 0;
                        break;

                    case ProgramOperator.OP_EQ_V:
                        c->_float = a->vector[0] == b->vector[0] &&
                            a->vector[1] == b->vector[1] &&
                            a->vector[2] == b->vector[2] ? 1 : 0;
                        break;

                    case ProgramOperator.OP_EQ_S:
                        c->_float = this.GetString( a->_string ) == this.GetString( b->_string ) ? 1 : 0; //!strcmp(pr_strings + a->_string, pr_strings + b->_string);
                        break;

                    case ProgramOperator.OP_EQ_E:
                        c->_float = a->_int == b->_int ? 1 : 0;
                        break;

                    case ProgramOperator.OP_EQ_FNC:
                        c->_float = a->function == b->function ? 1 : 0;
                        break;

                    case ProgramOperator.OP_NE_F:
                        c->_float = a->_float != b->_float ? 1 : 0;
                        break;

                    case ProgramOperator.OP_NE_V:
                        c->_float = a->vector[0] != b->vector[0] ||
                            a->vector[1] != b->vector[1] || a->vector[2] != b->vector[2] ? 1 : 0;
                        break;

                    case ProgramOperator.OP_NE_S:
                        c->_float = this.GetString( a->_string ) != this.GetString( b->_string ) ? 1 : 0; //strcmp(pr_strings + a->_string, pr_strings + b->_string);
                        break;

                    case ProgramOperator.OP_NE_E:
                        c->_float = a->_int != b->_int ? 1 : 0;
                        break;

                    case ProgramOperator.OP_NE_FNC:
                        c->_float = a->function != b->function ? 1 : 0;
                        break;

                    case ProgramOperator.OP_STORE_F:
                    case ProgramOperator.OP_STORE_ENT:
                    case ProgramOperator.OP_STORE_FLD:		// integers
                    case ProgramOperator.OP_STORE_S:
                    case ProgramOperator.OP_STORE_FNC:		// pointers
                        b->_int = a->_int;
                        break;

                    case ProgramOperator.OP_STORE_V:
                        b->vector[0] = a->vector[0];
                        b->vector[1] = a->vector[1];
                        b->vector[2] = a->vector[2];
                        break;

                    case ProgramOperator.OP_STOREP_F:
                    case ProgramOperator.OP_STOREP_ENT:
                    case ProgramOperator.OP_STOREP_FLD:		// integers
                    case ProgramOperator.OP_STOREP_S:
                    case ProgramOperator.OP_STOREP_FNC:		// pointers
                        ed = this.EdictFromAddr( b->_int, out ofs );
                        ed.StoreInt( ofs, a );
                        break;

                    case ProgramOperator.OP_STOREP_V:
                        ed = this.EdictFromAddr( b->_int, out ofs );
                        ed.StoreVector( ofs, a );
                        break;

                    case ProgramOperator.OP_ADDRESS:
                        ed = this.Host.Server.ProgToEdict( a->edict );
                        if( ed == this.Host.Server.sv.edicts[0] && this.Host.Server.IsActive )
                            this.RunError( "assignment to world entity" );
                        c->_int = this.MakeAddr( a->edict, b->_int );
                        break;

                    case ProgramOperator.OP_LOAD_F:
                    case ProgramOperator.OP_LOAD_FLD:
                    case ProgramOperator.OP_LOAD_ENT:
                    case ProgramOperator.OP_LOAD_S:
                    case ProgramOperator.OP_LOAD_FNC:
                        ed = this.Host.Server.ProgToEdict( a->edict );
                        ed.LoadInt( b->_int, c );
                        break;

                    case ProgramOperator.OP_LOAD_V:
                        ed = this.Host.Server.ProgToEdict( a->edict );
                        ed.LoadVector( b->_int, c );
                        break;

                    case ProgramOperator.OP_IFNOT:
                        if( a->_int == 0 )
                            s += this._Statements[s].b - 1;	// offset the s++
                        break;

                    case ProgramOperator.OP_IF:
                        if( a->_int != 0 )
                            s += this._Statements[s].b - 1;	// offset the s++
                        break;

                    case ProgramOperator.OP_GOTO:
                        s += this._Statements[s].a - 1;	// offset the s++
                        break;

                    case ProgramOperator.OP_CALL0:
                    case ProgramOperator.OP_CALL1:
                    case ProgramOperator.OP_CALL2:
                    case ProgramOperator.OP_CALL3:
                    case ProgramOperator.OP_CALL4:
                    case ProgramOperator.OP_CALL5:
                    case ProgramOperator.OP_CALL6:
                    case ProgramOperator.OP_CALL7:
                    case ProgramOperator.OP_CALL8:
                        this._Argc = this._Statements[s].op - ( int ) ProgramOperator.OP_CALL0;
                        if( a->function == 0 )
                            this.RunError( "NULL function" );

                        var newf = this._Functions[a->function];

                        if( newf.first_statement < 0 )
                        {
                            // negative statements are built in functions
                            var i = -newf.first_statement;
                            if( i >= this.Host.ProgramsBuiltIn.Count )
                                this.RunError( "Bad builtin call number" );

                            this.Host.ProgramsBuiltIn.Execute( i );
                            break;
                        }

                        s = this.EnterFunction( newf );
                        break;

                    case ProgramOperator.OP_DONE:
                    case ProgramOperator.OP_RETURN:
                        var ptr = ( float* )this._GlobalStructAddr;
                        int sta = this._Statements[s].a;
                        ptr[ProgramOperatorDef.OFS_RETURN + 0] = *( float* )this.Get( sta );
                        ptr[ProgramOperatorDef.OFS_RETURN + 1] = *( float* )this.Get( sta + 1 );
                        ptr[ProgramOperatorDef.OFS_RETURN + 2] = *( float* )this.Get( sta + 2 );

                        s = this.LeaveFunction();
                        if(this._Depth == exitdepth )
                            return;		// all done
                        break;

                    case ProgramOperator.OP_STATE:
                        ed = this.Host.Server.ProgToEdict(this.GlobalStruct.self );
#if FPS_20
                        ed->v.nextthink = pr_global_struct->time + 0.05;
#else
                        ed.v.nextthink = this.GlobalStruct.time + 0.1f;
#endif
                        if( a->_float != ed.v.frame )
                            ed.v.frame = a->_float;

                        ed.v.think = b->function;
                        break;

                    default:
                        this.RunError( "Bad opcode %i", this._Statements[s].op );
                        break;
                }
            }
        }

        /// <summary>
        /// PR_RunError
        /// Aborts the currently executing function
        /// </summary>
        public void RunError( string fmt, params object[] args )
        {
            this.PrintStatement( ref this._Statements[this._xStatement] );
            this.StackTrace();
            this.Host.Console.Print( fmt, args );

            this._Depth = 0;		// dump the stack so host_error can shutdown functions

            this.Host.Error( "Program error" );
        }

        public MemoryEdict EdictFromAddr( int addr, out int ofs )
        {
            var prog = ( addr >> 16 ) & 0xFFFF;
            ofs = addr & 0xFFFF;
            return this.Host.Server.ProgToEdict( prog );
        }

        // PR_Profile_f
        private void Profile_f( CommandMessage msg )
        {
            if(this._Functions == null )
                return;

            ProgramFunction best;
            var num = 0;
            do
            {
                var max = 0;
                best = null;
                for( var i = 0; i < this._Functions.Length; i++ )
                {
                    var f = this._Functions[i];
                    if( f.profile > max )
                    {
                        max = f.profile;
                        best = f;
                    }
                }
                if( best != null )
                {
                    if( num < 10 )
                        this.Host.Console.Print( "{0,7} {1}\n", best.profile, this.GetString( best.s_name ) );
                    num++;
                    best.profile = 0;
                }
            } while( best != null );
        }

        /// <summary>
        /// PR_EnterFunction
        /// Returns the new program statement counter
        /// </summary>
        private unsafe int EnterFunction( ProgramFunction f )
        {
            this._Stack[this._Depth].s = this._xStatement;
            this._Stack[this._Depth].f = this.xFunction;
            this._Depth++;
            if(this._Depth >= Programs.MAX_STACK_DEPTH )
                this.RunError( "stack overflow" );

            // save off any locals that the new function steps on
            var c = f.locals;
            if(this._LocalStackUsed + c > Programs.LOCALSTACK_SIZE )
                this.RunError( "PR_ExecuteProgram: locals stack overflow\n" );

            for( var i = 0; i < c; i++ )
                this._LocalStack[this._LocalStackUsed + i] = *( int* )this.Get( f.parm_start + i );

            this._LocalStackUsed += c;

            // copy parameters
            var o = f.parm_start;
            for( var i = 0; i < f.numparms; i++ )
            {
                for( var j = 0; j < f.parm_size[i]; j++ )
                {
                    this.Set( o, *( int* )this.Get( ProgramOperatorDef.OFS_PARM0 + i * 3 + j ) );
                    o++;
                }
            }

            this.xFunction = f;
            return f.first_statement - 1;	// offset the s++
        }

        /// <summary>
        /// PR_StackTrace
        /// </summary>
        private void StackTrace()
        {
            if(this._Depth == 0 )
            {
                this.Host.Console.Print( "<NO STACK>\n" );
                return;
            }

            this._Stack[this._Depth].f = this.Host.Programs.xFunction;
            for( var i = this._Depth; i >= 0; i-- )
            {
                var f = this._Stack[i].f;

                if( f == null )
                    this.Host.Console.Print( "<NO FUNCTION>\n" );
                else
                    this.Host.Console.Print( "{0,12} : {1}\n", this.GetString( f.s_file ), this.GetString( f.s_name ) );
            }
        }

        /// <summary>
        /// PR_PrintStatement
        /// </summary>
        private void PrintStatement( ref Statement s )
        {
            if( s.op < Programs.OpNames.Length )
                this.Host.Console.Print( "{0,10} ", Programs.OpNames[s.op] );

            var op = (ProgramOperator)s.op;
            if( op == ProgramOperator.OP_IF || op == ProgramOperator.OP_IFNOT )
                this.Host.Console.Print( "{0}branch {1}", this.GlobalString( s.a ), s.b );
            else if( op == ProgramOperator.OP_GOTO )
                this.Host.Console.Print( "branch {0}", s.a );
            else if( ( uint ) ( s.op - ProgramOperator.OP_STORE_F ) < 6 )
            {
                this.Host.Console.Print(this.GlobalString( s.a ) );
                this.Host.Console.Print(this.GlobalStringNoContents( s.b ) );
            }
            else
            {
                if( s.a != 0 )
                    this.Host.Console.Print(this.GlobalString( s.a ) );
                if( s.b != 0 )
                    this.Host.Console.Print(this.GlobalString( s.b ) );
                if( s.c != 0 )
                    this.Host.Console.Print(this.GlobalStringNoContents( s.c ) );
            }

            this.Host.Console.Print( "\n" );
        }

        /// <summary>
        /// PR_LeaveFunction
        /// </summary>
        private int LeaveFunction()
        {
            if(this._Depth <= 0 )
                Utilities.Error( "prog stack underflow" );

            // restore locals from the stack
            var c = this.xFunction.locals;
            this._LocalStackUsed -= c;
            if(this._LocalStackUsed < 0 )
                this.RunError( "PR_ExecuteProgram: locals stack underflow\n" );

            for( var i = 0; i < c; i++ )
            this.Set(this.xFunction.parm_start + i, this._LocalStack[this._LocalStackUsed + i] );
            //((int*)pr_globals)[pr_xfunction->parm_start + i] = localstack[localstack_used + i];

            // up stack
            this._Depth--;
            this.xFunction = this._Stack[this._Depth].f;

            return this._Stack[this._Depth].s;
        }

        private int MakeAddr( int prog, int offset )
        {
            return ( ( prog & 0xFFFF ) << 16 ) + ( offset & 0xFFFF );
        }
    }
}
