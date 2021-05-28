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
    public static class CommandLine
    {
        public static string[] _Argv;
        public static string _Args; // com_cmdline

        public static int Argc => CommandLine._Argv.Length;

        public static string[] Args
        {
            get => CommandLine._Argv;
            set
            {
                CommandLine._Argv = new string[value.Length];
                value.CopyTo( CommandLine._Argv, 0 );
                CommandLine._Args = string.Join( " ", value );
            }
        }

        // for passing as reference
        private static string[] safeargvs = new string[]
        {
            "-stdvid",
            "-nolan",
            "-nosound",
            "-nocdaudio",
            "-nojoy",
            "-nomouse",
            "-dibonly"
        };

        public static string Argv( int index )
        {
            return CommandLine._Argv[index];
        }

        // int COM_CheckParm (char *parm)
        // Returns the position (1 to argc-1) in the program's argument list
        // where the given parameter apears, or 0 if not present
        public static int CheckParm( string parm )
        {
            for ( var i = 1; i < CommandLine._Argv.Length; i++ )
            {
                if ( CommandLine._Argv[i].Equals( parm ) )
                    return i;
            }

            return 0;
        }

        public static bool HasParam( string parm )
        {
            return CommandLine.CheckParm( parm ) > 0;
        }

        // void COM_Init (char *path)
        public static void Init( string path, string[] argv )
        {
            CommandLine._Argv = argv;
        }

        // void COM_InitArgv (int argc, char **argv)
        public static void InitArgv( string[] argv )
        {
            // reconstitute the command line for the cmdline externally visible cvar
            CommandLine._Args = string.Join( " ", argv );
            CommandLine._Argv = new string[argv.Length];
            argv.CopyTo( CommandLine._Argv, 0 );

            var safe = false;
            foreach ( var arg in CommandLine._Argv )
            {
                if ( arg == "-safe" )
                {
                    safe = true;
                    break;
                }
            }

            if ( safe )
            {
                // force all the safe-mode switches. Note that we reserved extra space in
                // case we need to add these, so we don't need an overflow check
                var largv = new string[CommandLine._Argv.Length + CommandLine.safeargvs.Length];
                CommandLine._Argv.CopyTo( largv, 0 );
                CommandLine.safeargvs.CopyTo( largv, CommandLine._Argv.Length );
                CommandLine._Argv = largv;
            }
        }
    }
}
