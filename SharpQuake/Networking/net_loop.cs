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

namespace SharpQuake.Networking
{
    using Client;
    using Engine.Host;
    using Framework.Definitions;
    using Framework.Engine;
    using Framework.Networking;
    using System;

    internal class net_loop : INetDriver
    {
        private bool _IsInitialised;
        private bool _LocalConnectPending; // localconnectpending
        private qsocket_t _Client; // loop_client
        private qsocket_t _Server; // loop_server

        #region INetDriver Members

        public string Name => "Loopback";

        public bool IsInitialised => this._IsInitialised;

        // CHANGE
        private Host Host
        {
            get;
            set;
        }

        public void Initialise( object host )
        {
            this.Host = ( Host ) host;

            if(this.Host.Client.cls.state == cactive_t.ca_dedicated )
                return;// -1;

            this._IsInitialised = true;
        }

        public void Listen( bool state )
        {
            // nothig to do
        }

        public void SearchForHosts( bool xmit )
        {
            if( !this.Host.Server.sv.active )
                return;

            this.Host.Network.HostCacheCount = 1;
            if(this.Host.Network.HostName == "UNNAMED" )
                this.Host.Network.HostCache[0].name = "local";
            else
                this.Host.Network.HostCache[0].name = this.Host.Network.HostName;

            this.Host.Network.HostCache[0].map = this.Host.Server.sv.name;
            this.Host.Network.HostCache[0].users = this.Host.Network.ActiveConnections;
            this.Host.Network.HostCache[0].maxusers = this.Host.Server.svs.maxclients;
            this.Host.Network.HostCache[0].driver = this.Host.Network.DriverLevel;
            this.Host.Network.HostCache[0].cname = "local";
        }

        public qsocket_t Connect( string host )
        {
            if( host != "local" )
                return null;

            this._LocalConnectPending = true;

            if(this._Client == null )
            {
                this._Client = this.Host.Network.NewSocket();
                if(this._Client == null )
                {
                    this.Host.Console.Print( "Loop_Connect: no qsocket available\n" );
                    return null;
                }

                this._Client.address = "localhost";
            }

            this._Client.ClearBuffers();
            this._Client.canSend = true;

            if(this._Server == null )
            {
                this._Server = this.Host.Network.NewSocket();
                if(this._Server == null )
                {
                    this.Host.Console.Print( "Loop_Connect: no qsocket available\n" );
                    return null;
                }

                this._Server.address = "LOCAL";
            }

            this._Server.ClearBuffers();
            this._Server.canSend = true;

            this._Client.driverdata = this._Server;
            this._Server.driverdata = this._Client;

            return this._Client;
        }

        public qsocket_t CheckNewConnections()
        {
            if( !this._LocalConnectPending )
                return null;

            this._LocalConnectPending = false;
            this._Server.ClearBuffers();
            this._Server.canSend = true;
            this._Client.ClearBuffers();
            this._Client.canSend = true;
            return this._Server;
        }

        public int GetMessage( qsocket_t sock )
        {
            if( sock.receiveMessageLength == 0 )
                return 0;

            int ret = sock.receiveMessage[0];
            var length = sock.receiveMessage[1] + ( sock.receiveMessage[2] << 8 );

            // alignment byte skipped here
            this.Host.Network.Message.Clear();
            this.Host.Network.Message.FillFrom( sock.receiveMessage, 4, length );

            length = this.IntAlign( length + 4 );
            sock.receiveMessageLength -= length;

            if( sock.receiveMessageLength > 0 )
                Array.Copy( sock.receiveMessage, length, sock.receiveMessage, 0, sock.receiveMessageLength );

            if( sock.driverdata != null && ret == 1 )
                ( (qsocket_t)sock.driverdata ).canSend = true;

            return ret;
        }

        public int SendMessage( qsocket_t sock, MessageWriter data )
        {
            if( sock.driverdata == null )
                return -1;

            var sock2 = (qsocket_t)sock.driverdata;

            if( sock2.receiveMessageLength + data.Length + 4 > NetworkDef.NET_MAXMESSAGE )
                Utilities.Error( "Loop_SendMessage: overflow\n" );

            // message type
            var offset = sock2.receiveMessageLength;
            sock2.receiveMessage[offset++] = 1;

            // length
            sock2.receiveMessage[offset++] = ( byte ) ( data.Length & 0xff );
            sock2.receiveMessage[offset++] = ( byte ) ( data.Length >> 8 );

            // align
            offset++;

            // message
            Buffer.BlockCopy( data.Data, 0, sock2.receiveMessage, offset, data.Length );
            sock2.receiveMessageLength = this.IntAlign( sock2.receiveMessageLength + data.Length + 4 );

            sock.canSend = false;
            return 1;
        }

        public int SendUnreliableMessage( qsocket_t sock, MessageWriter data )
        {
            if( sock.driverdata == null )
                return -1;

            var sock2 = (qsocket_t)sock.driverdata;

            if( sock2.receiveMessageLength + data.Length + sizeof( byte ) + sizeof( short ) > NetworkDef.NET_MAXMESSAGE )
                return 0;

            var offset = sock2.receiveMessageLength;

            // message type
            sock2.receiveMessage[offset++] = 2;

            // length
            sock2.receiveMessage[offset++] = ( byte ) ( data.Length & 0xff );
            sock2.receiveMessage[offset++] = ( byte ) ( data.Length >> 8 );

            // align
            offset++;

            // message
            Buffer.BlockCopy( data.Data, 0, sock2.receiveMessage, offset, data.Length );
            sock2.receiveMessageLength = this.IntAlign( sock2.receiveMessageLength + data.Length + 4 );

            return 1;
        }

        public bool CanSendMessage( qsocket_t sock )
        {
            if( sock.driverdata == null )
                return false;
            return sock.canSend;
        }

        public bool CanSendUnreliableMessage( qsocket_t sock )
        {
            return true;
        }

        public void Close( qsocket_t sock )
        {
            if( sock.driverdata != null )
                ( (qsocket_t)sock.driverdata ).driverdata = null;

            sock.ClearBuffers();
            sock.canSend = true;
            if( sock == this._Client )
                this._Client = null;
            else
                this._Server = null;
        }

        public void Shutdown()
        {
            this._IsInitialised = false;
        }

        private int IntAlign( int value )
        {
            return ( value + ( sizeof( int ) - 1 ) ) & ~( sizeof( int ) - 1 );
        }

        #endregion INetDriver Members
    }
}
