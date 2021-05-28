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

namespace SharpQuake.Framework.Networking
{
    using Engine;

    // struct net_driver_t
    public interface INetDriver
    {
        string Name
        {
            get;
        }

        bool IsInitialised
        {
            get;
        }

        void Initialise( object host );

        void Listen( bool state );

        void SearchForHosts( bool xmit );

        qsocket_t Connect( string host );

        qsocket_t CheckNewConnections( );

        int GetMessage( qsocket_t sock );

        int SendMessage( qsocket_t sock, MessageWriter data );

        int SendUnreliableMessage( qsocket_t sock, MessageWriter data );

        bool CanSendMessage( qsocket_t sock );

        bool CanSendUnreliableMessage( qsocket_t sock );

        void Close( qsocket_t sock );

        void Shutdown( );
    } //net_driver_t;
}
