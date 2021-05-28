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
    using Engine.Host;
    using Framework.Data;
    using Framework.Definitions;
    using Framework.Engine;
    using Framework.IO;
    using Framework.Networking;
    using System;
    using System.Net;

    internal class net_datagram : INetDriver
    {
        public static net_datagram Instance => net_datagram._Singletone;

        private static net_datagram _Singletone = new( );

        private int _DriverLevel;
        private bool _IsInitialised;
        private byte[] _PacketBuffer;

        // statistic counters
        private int packetsSent;

        private int packetsReSent;
        private int packetsReceived;
        private int receivedDuplicateCount;
        private int shortPacketCount;
        private int droppedDatagrams;
        //

        private static string StrAddr( EndPoint ep )
        {
            return ep.ToString( );
        }

        // NET_Stats_f
        private void Stats_f( CommandMessage msg )
        {
            if ( msg.Parameters == null || msg.Parameters.Length == 0 )
            {
                this.Host.Console.Print( "unreliable messages sent   = %i\n", this.Host.Network.UnreliableMessagesSent );
                this.Host.Console.Print( "unreliable messages recv   = %i\n", this.Host.Network.UnreliableMessagesReceived );
                this.Host.Console.Print( "reliable messages sent     = %i\n", this.Host.Network.MessagesSent );
                this.Host.Console.Print( "reliable messages received = %i\n", this.Host.Network.MessagesReceived );
                this.Host.Console.Print( "packetsSent                = %i\n", this.packetsSent );
                this.Host.Console.Print( "packetsReSent              = %i\n", this.packetsReSent );
                this.Host.Console.Print( "packetsReceived            = %i\n", this.packetsReceived );
                this.Host.Console.Print( "receivedDuplicateCount     = %i\n", this.receivedDuplicateCount );
                this.Host.Console.Print( "shortPacketCount           = %i\n", this.shortPacketCount );
                this.Host.Console.Print( "droppedDatagrams           = %i\n", this.droppedDatagrams );
            }
            else if ( msg.Parameters[0] == "*" )
            {
                foreach ( var s in this.Host.Network.ActiveSockets )
                    this.PrintStats( s );

                foreach ( var s in this.Host.Network.FreeSockets )
                    this.PrintStats( s );
            }
            else
            {
                qsocket_t sock = null;
                var cmdAddr = msg.Parameters[0];

                foreach ( var s in this.Host.Network.ActiveSockets )
                {
                    if ( Utilities.SameText( s.address, cmdAddr ) )
                    {
                        sock = s;
                        break;
                    }
                }

                if ( sock == null )
                {
                    foreach ( var s in this.Host.Network.FreeSockets )
                    {
                        if ( Utilities.SameText( s.address, cmdAddr ) )
                        {
                            sock = s;
                            break;
                        }
                    }
                }

                if ( sock == null )
                    return;

                this.PrintStats( sock );
            }
        }

        // PrintStats(qsocket_t* s)
        private void PrintStats( qsocket_t s )
        {
            this.Host.Console.Print( "canSend = {0:4}   \n", s.canSend );
            this.Host.Console.Print( "sendSeq = {0:4}   ", s.sendSequence );
            this.Host.Console.Print( "recvSeq = {0:4}   \n", s.receiveSequence );
            this.Host.Console.Print( "\n" );
        }

        private net_datagram( )
        {
            this._PacketBuffer = new byte[NetworkDef.NET_DATAGRAMSIZE];
        }

        #region INetDriver Members

        public string Name => "Datagram";

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

            this._DriverLevel = Array.IndexOf(this.Host.Network.Drivers, this );
            this.Host.Commands.Add( "net_stats", this.Stats_f );

            if ( CommandLine.HasParam( "-nolan" ) )
                return;

            foreach ( var driver in this.Host.Network.LanDrivers )
            {
                if ( driver is net_tcp_ip )
                {
                    var tcpIP = ( net_tcp_ip ) driver;

                    tcpIP.HostName = this.Host.CVars.Get( "hostname" ).Get<string>( );
                    tcpIP.HostPort = this.Host.Network.HostPort;
                }

                driver.Initialise( );

                if ( driver is net_tcp_ip )
                {
                    var tcpIP = ( net_tcp_ip ) driver;

                    this.Host.Network.MyTcpIpAddress = tcpIP.HostAddress;

                    this.Host.CVars.Set( "hostname", tcpIP.HostName );
                }
            }

#if BAN_TEST
	        Cmd_AddCommand ("ban", NET_Ban_f);
#endif
            //Cmd.Add("test", Test_f);
            //Cmd.Add("test2", Test2_f);

            this._IsInitialised = true;
        }

        /// <summary>
        /// Datagram_Listen
        /// </summary>
        public void Listen( bool state )
        {
            foreach ( var drv in this.Host.Network.LanDrivers )
            {
                if ( drv.IsInitialised )
                    drv.Listen( state );
            }
        }

        /// <summary>
        /// Datagram_SearchForHosts
        /// </summary>
        public void SearchForHosts( bool xmit )
        {
            for (this.Host.Network.LanDriverLevel = 0; this.Host.Network.LanDriverLevel < this.Host.Network.LanDrivers.Length; this.Host.Network.LanDriverLevel++ )
            {
                if (this.Host.Network.HostCacheCount == NetworkDef.HOSTCACHESIZE )
                    break;
                if (this.Host.Network.LanDrivers[this.Host.Network.LanDriverLevel].IsInitialised )
                    this.InternalSearchForHosts( xmit );
            }
        }

        /// <summary>
        /// Datagram_Connect
        /// </summary>
        public qsocket_t Connect( string host )
        {
            qsocket_t ret = null;

            for (this.Host.Network.LanDriverLevel = 0; this.Host.Network.LanDriverLevel < this.Host.Network.LanDrivers.Length; this.Host.Network.LanDriverLevel++ )
            {
                if (this.Host.Network.LanDrivers[this.Host.Network.LanDriverLevel].IsInitialised )
                {
                    ret = this.InternalConnect( host );
                    if ( ret != null )
                        break;
                }
            }

            return ret;
        }

        /// <summary>
        /// Datagram_CheckNewConnections
        /// </summary>
        public qsocket_t CheckNewConnections( )
        {
            qsocket_t ret = null;

            for (this.Host.Network.LanDriverLevel = 0; this.Host.Network.LanDriverLevel < this.Host.Network.LanDrivers.Length; this.Host.Network.LanDriverLevel++ )
            {
                if (this.Host.Network.LanDriver.IsInitialised )
                {
                    ret = this.InternalCheckNewConnections( );
                    if ( ret != null )
                        break;
                }
            }

            return ret;
        }

        /// <summary>
        /// _Datagram_CheckNewConnections
        /// </summary>
        public qsocket_t InternalCheckNewConnections( )
        {
            var acceptsock = this.Host.Network.LanDriver.CheckNewConnections( );
            if ( acceptsock == null )
                return null;

            EndPoint clientaddr = new IPEndPoint( IPAddress.Any, 0 );
            this.Host.Network.Message.FillFrom(this.Host.Network, acceptsock, ref clientaddr );

            if (this.Host.Network.Message.Length < sizeof( int ) )
                return null;

            this.Host.Network.Reader.Reset( );
            var control = EndianHelper.BigLong(this.Host.Network.Reader.ReadLong( ) );
            if ( control == -1 )
                return null;
            if ( ( control & ~NetFlags.NETFLAG_LENGTH_MASK ) != NetFlags.NETFLAG_CTL )
                return null;
            if ( ( control & NetFlags.NETFLAG_LENGTH_MASK ) != this.Host.Network.Message.Length )
                return null;

            var command = this.Host.Network.Reader.ReadByte( );
            if ( command == CCReq.CCREQ_SERVER_INFO )
            {
                var tmp = this.Host.Network.Reader.ReadString( );
                if ( tmp != "QUAKE" )
                    return null;

                this.Host.Network.Message.Clear( );

                // save space for the header, filled in later
                this.Host.Network.Message.WriteLong( 0 );
                this.Host.Network.Message.WriteByte( CCRep.CCREP_SERVER_INFO );
                var newaddr = acceptsock.LocalEndPoint; //dfunc.GetSocketAddr(acceptsock, &newaddr);
                this.Host.Network.Message.WriteString( newaddr.ToString( ) ); // dfunc.AddrToString(&newaddr));
                this.Host.Network.Message.WriteString(this.Host.Network.HostName );
                this.Host.Network.Message.WriteString(this.Host.Server.sv.name );
                this.Host.Network.Message.WriteByte(this.Host.Network.ActiveConnections );
                this.Host.Network.Message.WriteByte(this.Host.Server.svs.maxclients );
                this.Host.Network.Message.WriteByte( NetworkDef.NET_PROTOCOL_VERSION );
                Utilities.WriteInt(
                    this.Host.Network.Message.Data, 0, EndianHelper.BigLong( NetFlags.NETFLAG_CTL |
                    (this.Host.Network.Message.Length & NetFlags.NETFLAG_LENGTH_MASK ) ) );

                this.Host.Network.LanDriver.Write( acceptsock, this.Host.Network.Message.Data, this.Host.Network.Message.Length, clientaddr );
                this.Host.Network.Message.Clear( );
                return null;
            }

            if ( command == CCReq.CCREQ_PLAYER_INFO )
            {
                var playerNumber = this.Host.Network.Reader.ReadByte( );
                int clientNumber, activeNumber = -1;
                client_t client = null;
                for ( clientNumber = 0; clientNumber < this.Host.Server.svs.maxclients; clientNumber++ )
                {
                    client = this.Host.Server.svs.clients[clientNumber];
                    if ( client.active )
                    {
                        activeNumber++;
                        if ( activeNumber == playerNumber )
                            break;
                    }
                }
                if ( clientNumber == this.Host.Server.svs.maxclients )
                    return null;

                this.Host.Network.Message.Clear( );
                // save space for the header, filled in later
                this.Host.Network.Message.WriteLong( 0 );
                this.Host.Network.Message.WriteByte( CCRep.CCREP_PLAYER_INFO );
                this.Host.Network.Message.WriteByte( playerNumber );
                this.Host.Network.Message.WriteString( client.name );
                this.Host.Network.Message.WriteLong( client.colors );
                this.Host.Network.Message.WriteLong( ( int ) client.edict.v.frags );
                this.Host.Network.Message.WriteLong( ( int ) (this.Host.Network.Time - client.netconnection.connecttime ) );
                this.Host.Network.Message.WriteString( client.netconnection.address );
                Utilities.WriteInt(
                    this.Host.Network.Message.Data, 0, EndianHelper.BigLong( NetFlags.NETFLAG_CTL |
                    (this.Host.Network.Message.Length & NetFlags.NETFLAG_LENGTH_MASK ) ) );

                this.Host.Network.LanDriver.Write( acceptsock, this.Host.Network.Message.Data, this.Host.Network.Message.Length, clientaddr );
                this.Host.Network.Message.Clear( );

                return null;
            }

            if ( command == CCReq.CCREQ_RULE_INFO )
            {
                // find the search start location
                var prevCvarName = this.Host.Network.Reader.ReadString( );
                ClientVariable var;
                if ( !string.IsNullOrEmpty( prevCvarName ) )
                {
                    var = this.Host.CVars.Get( prevCvarName );

                    if ( var == null )
                        return null;

                    var index = this.Host.CVars.IndexOf( var.Name );

                    var = this.Host.CVars.GetByIndex( index + 1 );
                }
                else
                    var = this.Host.CVars.GetByIndex( 0 );

                // search for the next server cvar
                while ( var != null )
                {
                    if ( var.IsServer )
                        break;

                    var index = this.Host.CVars.IndexOf( var.Name );

                    var = this.Host.CVars.GetByIndex( index + 1 );
                }

                // send the response
                this.Host.Network.Message.Clear( );

                // save space for the header, filled in later
                this.Host.Network.Message.WriteLong( 0 );
                this.Host.Network.Message.WriteByte( CCRep.CCREP_RULE_INFO );
                if ( var != null )
                {
                    this.Host.Network.Message.WriteString( var.Name );
                    this.Host.Network.Message.WriteString( var.Get( ).ToString( ) );
                }
                Utilities.WriteInt(
                    this.Host.Network.Message.Data, 0, EndianHelper.BigLong( NetFlags.NETFLAG_CTL |
                    (this.Host.Network.Message.Length & NetFlags.NETFLAG_LENGTH_MASK ) ) );

                this.Host.Network.LanDriver.Write( acceptsock, this.Host.Network.Message.Data, this.Host.Network.Message.Length, clientaddr );
                this.Host.Network.Message.Clear( );

                return null;
            }

            if ( command != CCReq.CCREQ_CONNECT )
                return null;

            if (this.Host.Network.Reader.ReadString( ) != "QUAKE" )
                return null;

            if (this.Host.Network.Reader.ReadByte( ) != NetworkDef.NET_PROTOCOL_VERSION )
            {
                this.Host.Network.Message.Clear( );
                // save space for the header, filled in later
                this.Host.Network.Message.WriteLong( 0 );
                this.Host.Network.Message.WriteByte( CCRep.CCREP_REJECT );
                this.Host.Network.Message.WriteString( "Incompatible version.\n" );
                Utilities.WriteInt(
                    this.Host.Network.Message.Data, 0, EndianHelper.BigLong( NetFlags.NETFLAG_CTL |
                    (this.Host.Network.Message.Length & NetFlags.NETFLAG_LENGTH_MASK ) ) );

                this.Host.Network.LanDriver.Write( acceptsock, this.Host.Network.Message.Data, this.Host.Network.Message.Length, clientaddr );
                this.Host.Network.Message.Clear( );
                return null;
            }

#if BAN_TEST
            // check for a ban
            if (clientaddr.sa_family == AF_INET)
            {
                unsigned long testAddr;
                testAddr = ((struct sockaddr_in *)&clientaddr)->sin_addr.s_addr;
                if ((testAddr & banMask) == banAddr)
                {
                    SZ_Clear(&net_message);
                    // save space for the header, filled in later
                    MSG_WriteLong(&net_message, 0);
                    MSG_WriteByte(&net_message, CCREP_REJECT);
                    MSG_WriteString(&net_message, "You have been banned.\n");
                    *((int *)net_message.data) = BigLong(NETFLAG_CTL | (net_message.cursize & NETFLAG_LENGTH_MASK));
                    dfunc.Write (acceptsock, net_message.data, net_message.cursize, &clientaddr);
                    SZ_Clear(&net_message);
                    return NULL;
                }
            }
#endif

            // see if this guy is already connected
            foreach ( var s in this.Host.Network.ActiveSockets )
            {
                if ( s.driver != this.Host.Network.DriverLevel )
                    continue;

                var ret = this.Host.Network.LanDriver.AddrCompare( clientaddr, s.addr );
                if ( ret >= 0 )
                {
                    // is this a duplicate connection reqeust?
                    if ( ret == 0 && this.Host.Network.Time - s.connecttime < 2.0 )
                    {
                        // yes, so send a duplicate reply
                        this.Host.Network.Message.Clear( );
                        // save space for the header, filled in later
                        this.Host.Network.Message.WriteLong( 0 );
                        this.Host.Network.Message.WriteByte( CCRep.CCREP_ACCEPT );
                        var newaddr = s.socket.LocalEndPoint; //dfunc.GetSocketAddr(s.socket, &newaddr);
                        this.Host.Network.Message.WriteLong(this.Host.Network.LanDriver.GetSocketPort( newaddr ) );
                        Utilities.WriteInt(
                            this.Host.Network.Message.Data, 0, EndianHelper.BigLong( NetFlags.NETFLAG_CTL |
                            (this.Host.Network.Message.Length & NetFlags.NETFLAG_LENGTH_MASK ) ) );

                        this.Host.Network.LanDriver.Write( acceptsock, this.Host.Network.Message.Data, this.Host.Network.Message.Length, clientaddr );
                        this.Host.Network.Message.Clear( );
                        return null;
                    }
                    // it's somebody coming back in from a crash/disconnect
                    // so close the old qsocket and let their retry get them back in
                    this.Host.Network.Close( s );
                    return null;
                }
            }

            // allocate a QSocket
            var sock = this.Host.Network.NewSocket( );
            if ( sock == null )
            {
                // no room; try to let him know
                this.Host.Network.Message.Clear( );
                // save space for the header, filled in later
                this.Host.Network.Message.WriteLong( 0 );
                this.Host.Network.Message.WriteByte( CCRep.CCREP_REJECT );
                this.Host.Network.Message.WriteString( "Server is full.\n" );
                Utilities.WriteInt(
                    this.Host.Network.Message.Data, 0, EndianHelper.BigLong( NetFlags.NETFLAG_CTL |
                    (this.Host.Network.Message.Length & NetFlags.NETFLAG_LENGTH_MASK ) ) );

                this.Host.Network.LanDriver.Write( acceptsock, this.Host.Network.Message.Data, this.Host.Network.Message.Length, clientaddr );
                this.Host.Network.Message.Clear( );
                return null;
            }

            // allocate a network socket
            var newsock = this.Host.Network.LanDriver.OpenSocket( 0 );
            if ( newsock == null )
            {
                this.Host.Network.FreeSocket( sock );
                return null;
            }

            // connect to the client
            if (this.Host.Network.LanDriver.Connect( newsock, clientaddr ) == -1 )
            {
                this.Host.Network.LanDriver.CloseSocket( newsock );
                this.Host.Network.FreeSocket( sock );
                return null;
            }

            // everything is allocated, just fill in the details
            sock.socket = newsock;
            sock.landriver = this.Host.Network.LanDriverLevel;
            sock.addr = clientaddr;
            sock.address = clientaddr.ToString( );

            // send him back the info about the server connection he has been allocated
            this.Host.Network.Message.Clear( );
            // save space for the header, filled in later
            this.Host.Network.Message.WriteLong( 0 );
            this.Host.Network.Message.WriteByte( CCRep.CCREP_ACCEPT );
            var newaddr2 = newsock.LocalEndPoint;// dfunc.GetSocketAddr(newsock, &newaddr);
            this.Host.Network.Message.WriteLong(this.Host.Network.LanDriver.GetSocketPort( newaddr2 ) );
            Utilities.WriteInt(
                this.Host.Network.Message.Data, 0, EndianHelper.BigLong( NetFlags.NETFLAG_CTL |
                (this.Host.Network.Message.Length & NetFlags.NETFLAG_LENGTH_MASK ) ) );

            this.Host.Network.LanDriver.Write( acceptsock, this.Host.Network.Message.Data, this.Host.Network.Message.Length, clientaddr );
            this.Host.Network.Message.Clear( );

            return sock;
        }

        public int GetMessage( qsocket_t sock )
        {
            if ( !sock.canSend )
            {
                if ( this.Host.Network.Time - sock.lastSendTime > 1.0 )
                    this.ReSendMessage( sock );
            }

            var ret = 0;
            EndPoint readaddr = new IPEndPoint( IPAddress.Any, 0 );
            while ( true )
            {
                var length = sock.Read(this._PacketBuffer, NetworkDef.NET_DATAGRAMSIZE, ref readaddr );
                if ( length == 0 )
                    break;

                if ( length == -1 )
                {
                    this.Host.Console.Print( "Read error\n" );
                    return -1;
                }

                if ( sock.LanDriver.AddrCompare( readaddr, sock.addr ) != 0 )
                {
#if DEBUG
                    this.Host.Console.DPrint( "Forged packet received\n" );
                    this.Host.Console.DPrint( "Expected: {0}\n", net_datagram.StrAddr( sock.addr ) );
                    this.Host.Console.DPrint( "Received: {0}\n", net_datagram.StrAddr( readaddr ) );
#endif
                    continue;
                }

                if ( length < NetworkDef.NET_HEADERSIZE )
                {
                    this.shortPacketCount++;
                    continue;
                }

                var header = Utilities.BytesToStructure<PacketHeader>(this._PacketBuffer, 0 );

                length = EndianHelper.BigLong( header.length );
                var flags = length & ~NetFlags.NETFLAG_LENGTH_MASK;
                length &= NetFlags.NETFLAG_LENGTH_MASK;

                if ( ( flags & NetFlags.NETFLAG_CTL ) != 0 )
                    continue;

                var sequence = ( uint ) EndianHelper.BigLong( header.sequence );
                this.packetsReceived++;

                if ( ( flags & NetFlags.NETFLAG_UNRELIABLE ) != 0 )
                {
                    if ( sequence < sock.unreliableReceiveSequence )
                    {
                        this.Host.Console.DPrint( "Got a stale datagram\n" );
                        ret = 0;
                        break;
                    }
                    if ( sequence != sock.unreliableReceiveSequence )
                    {
                        var count = ( int ) ( sequence - sock.unreliableReceiveSequence );
                        this.droppedDatagrams += count;
                        this.Host.Console.DPrint( "Dropped {0} datagram(s)\n", count );
                    }
                    sock.unreliableReceiveSequence = sequence + 1;

                    length -= NetworkDef.NET_HEADERSIZE;

                    this.Host.Network.Message.FillFrom(this._PacketBuffer, PacketHeader.SizeInBytes, length );

                    ret = 2;
                    break;
                }

                if ( ( flags & NetFlags.NETFLAG_ACK ) != 0 )
                {
                    if ( sequence != sock.sendSequence - 1 )
                    {
                        this.Host.Console.DPrint( "Stale ACK received\n" );
                        continue;
                    }
                    if ( sequence == sock.ackSequence )
                    {
                        sock.ackSequence++;
                        if ( sock.ackSequence != sock.sendSequence )
                            this.Host.Console.DPrint( "ack sequencing error\n" );
                    }
                    else
                    {
                        this.Host.Console.DPrint( "Duplicate ACK received\n" );
                        continue;
                    }
                    sock.sendMessageLength -= QDef.MAX_DATAGRAM;
                    if ( sock.sendMessageLength > 0 )
                    {
                        Buffer.BlockCopy( sock.sendMessage, QDef.MAX_DATAGRAM, sock.sendMessage, 0, sock.sendMessageLength );
                        sock.sendNext = true;
                    }
                    else
                    {
                        sock.sendMessageLength = 0;
                        sock.canSend = true;
                    }
                    continue;
                }

                if ( ( flags & NetFlags.NETFLAG_DATA ) != 0 )
                {
                    header.length = EndianHelper.BigLong( NetworkDef.NET_HEADERSIZE | NetFlags.NETFLAG_ACK );
                    header.sequence = EndianHelper.BigLong( ( int ) sequence );

                    Utilities.StructureToBytes( ref header, this._PacketBuffer, 0 );
                    sock.Write(this._PacketBuffer, NetworkDef.NET_HEADERSIZE, readaddr );

                    if ( sequence != sock.receiveSequence )
                    {
                        this.receivedDuplicateCount++;
                        continue;
                    }
                    sock.receiveSequence++;

                    length -= NetworkDef.NET_HEADERSIZE;

                    if ( ( flags & NetFlags.NETFLAG_EOM ) != 0 )
                    {
                        this.Host.Network.Message.Clear( );
                        this.Host.Network.Message.FillFrom( sock.receiveMessage, 0, sock.receiveMessageLength );
                        this.Host.Network.Message.AppendFrom(this._PacketBuffer, PacketHeader.SizeInBytes, length );
                        sock.receiveMessageLength = 0;

                        ret = 1;
                        break;
                    }

                    Buffer.BlockCopy(this._PacketBuffer, PacketHeader.SizeInBytes, sock.receiveMessage, sock.receiveMessageLength, length );
                    sock.receiveMessageLength += length;
                    continue;
                }
            }

            if ( sock.sendNext )
                this.SendMessageNext( sock );

            return ret;
        }

        /// <summary>
        /// Datagram_SendMessage
        /// </summary>
        public int SendMessage( qsocket_t sock, MessageWriter data )
        {
#if DEBUG
            if ( data.IsEmpty )
                Utilities.Error( "Datagram_SendMessage: zero length message\n" );

            if ( data.Length > NetworkDef.NET_MAXMESSAGE )
                Utilities.Error( "Datagram_SendMessage: message too big {0}\n", data.Length );

            if ( !sock.canSend )
                Utilities.Error( "SendMessage: called with canSend == false\n" );
#endif
            Buffer.BlockCopy( data.Data, 0, sock.sendMessage, 0, data.Length );
            sock.sendMessageLength = data.Length;

            int dataLen, eom;
            if ( data.Length <= QDef.MAX_DATAGRAM )
            {
                dataLen = data.Length;
                eom = NetFlags.NETFLAG_EOM;
            }
            else
            {
                dataLen = QDef.MAX_DATAGRAM;
                eom = 0;
            }
            var packetLen = NetworkDef.NET_HEADERSIZE + dataLen;

            PacketHeader header;
            header.length = EndianHelper.BigLong( packetLen | NetFlags.NETFLAG_DATA | eom );
            header.sequence = EndianHelper.BigLong( ( int ) sock.sendSequence++ );
            Utilities.StructureToBytes( ref header, this._PacketBuffer, 0 );
            Buffer.BlockCopy( data.Data, 0, this._PacketBuffer, PacketHeader.SizeInBytes, dataLen );

            sock.canSend = false;

            if ( sock.Write(this._PacketBuffer, packetLen, sock.addr ) == -1 )
                return -1;

            sock.lastSendTime = this.Host.Network.Time;
            this.packetsSent++;
            return 1;
        }

        /// <summary>
        /// Datagram_SendUnreliableMessage
        /// </summary>
        public int SendUnreliableMessage( qsocket_t sock, MessageWriter data )
        {
            int packetLen;

#if DEBUG
            if ( data.IsEmpty )
                Utilities.Error( "Datagram_SendUnreliableMessage: zero length message\n" );

            if ( data.Length > QDef.MAX_DATAGRAM )
                Utilities.Error( "Datagram_SendUnreliableMessage: message too big {0}\n", data.Length );
#endif

            packetLen = NetworkDef.NET_HEADERSIZE + data.Length;

            PacketHeader header;
            header.length = EndianHelper.BigLong( packetLen | NetFlags.NETFLAG_UNRELIABLE );
            header.sequence = EndianHelper.BigLong( ( int ) sock.unreliableSendSequence++ );
            Utilities.StructureToBytes( ref header, this._PacketBuffer, 0 );
            Buffer.BlockCopy( data.Data, 0, this._PacketBuffer, PacketHeader.SizeInBytes, data.Length );

            if ( sock.Write(this._PacketBuffer, packetLen, sock.addr ) == -1 )
                return -1;

            this.packetsSent++;
            return 1;
        }

        /// <summary>
        /// Datagram_CanSendMessage
        /// </summary>
        public bool CanSendMessage( qsocket_t sock )
        {
            if ( sock.sendNext )
                this.SendMessageNext( sock );

            return sock.canSend;
        }

        /// <summary>
        /// Datagram_CanSendUnreliableMessage
        /// </summary>
        public bool CanSendUnreliableMessage( qsocket_t sock )
        {
            return true;
        }

        /// <summary>
        /// Datagram_Close
        /// </summary>
        public void Close( qsocket_t sock )
        {
            sock.LanDriver.CloseSocket( sock.socket );
        }

        /// <summary>
        /// Datagram_Shutdown
        /// </summary>
        public void Shutdown( )
        {
            //
            // shutdown the lan drivers
            //
            foreach ( var driver in this.Host.Network.LanDrivers )
            {
                if ( driver.IsInitialised )
                    driver.Dispose( );
            }

            this._IsInitialised = false;
        }

        /// <summary>
        /// _Datagram_SearchForHosts
        /// </summary>
        private void InternalSearchForHosts( bool xmit )
        {
            var myaddr = this.Host.Network.LanDriver.ControlSocket.LocalEndPoint;
            if ( xmit )
            {
                this.Host.Network.Message.Clear( );
                // save space for the header, filled in later
                this.Host.Network.Message.WriteLong( 0 );
                this.Host.Network.Message.WriteByte( CCReq.CCREQ_SERVER_INFO );
                this.Host.Network.Message.WriteString( "QUAKE" );
                this.Host.Network.Message.WriteByte( NetworkDef.NET_PROTOCOL_VERSION );
                Utilities.WriteInt(
                    this.Host.Network.Message.Data, 0, EndianHelper.BigLong( NetFlags.NETFLAG_CTL |
                    (this.Host.Network.Message.Length & NetFlags.NETFLAG_LENGTH_MASK ) ) );

                this.Host.Network.LanDriver.Broadcast(this.Host.Network.LanDriver.ControlSocket, this.Host.Network.Message.Data, this.Host.Network.Message.Length );
                this.Host.Network.Message.Clear( );
            }

            EndPoint readaddr = new IPEndPoint( IPAddress.Any, 0 );
            while ( true )
            {
                this.Host.Network.Message.FillFrom(this.Host.Network, this.Host.Network.LanDriver.ControlSocket, ref readaddr );
                if (this.Host.Network.Message.IsEmpty )
                    break;
                if (this.Host.Network.Message.Length < sizeof( int ) )
                    continue;

                // don't answer our own query
                if (this.Host.Network.LanDriver.AddrCompare( readaddr, myaddr ) >= 0 )
                    continue;

                // is the cache full?
                if (this.Host.Network.HostCacheCount == NetworkDef.HOSTCACHESIZE )
                    continue;

                this.Host.Network.Reader.Reset( );
                var control = EndianHelper.BigLong(this.Host.Network.Reader.ReadLong( ) );// BigLong(*((int *)net_message.data));
                //MSG_ReadLong();
                if ( control == -1 )
                    continue;
                if ( ( control & ~NetFlags.NETFLAG_LENGTH_MASK ) != NetFlags.NETFLAG_CTL )
                    continue;
                if ( ( control & NetFlags.NETFLAG_LENGTH_MASK ) != this.Host.Network.Message.Length )
                    continue;

                if (this.Host.Network.Reader.ReadByte( ) != CCRep.CCREP_SERVER_INFO )
                    continue;

                var _hostIP = readaddr;

                readaddr = this.Host.Network.LanDriver.GetAddrFromName(this.Host.Network.Reader.ReadString( ) );
                int n;
                // search the cache for this server
                for ( n = 0; n < this.Host.Network.HostCacheCount; n++ )
                {
                    if (this.Host.Network.LanDriver.AddrCompare( readaddr, this.Host.Network.HostCache[n].addr ) == 0 )
                        break;
                }

                // is it already there?
                if ( n < this.Host.Network.HostCacheCount )
                    continue;

                // add it
                this.Host.Network.HostCacheCount++;
                var hc = this.Host.Network.HostCache[n];
                hc.name = this.Host.Network.Reader.ReadString( );
                hc.map = this.Host.Network.Reader.ReadString( );
                hc.users = this.Host.Network.Reader.ReadByte( );
                hc.maxusers = this.Host.Network.Reader.ReadByte( );
                if (this.Host.Network.Reader.ReadByte( ) != NetworkDef.NET_PROTOCOL_VERSION )
                {
                    hc.cname = hc.name;
                    hc.name = "*" + hc.name;
                }
                //IPEndPoint ep = (IPEndPoint)readaddr;
                //hc.addr = new IPEndPoint( ep.Address, ep.Port );
                var ip = readaddr.ToString( ).Split( ':' ); //readaddr.ToString()
                IPAddress _ipAddress;
                int _port;
                IPAddress.TryParse( ip[0].ToString( ), out _ipAddress );
                int.TryParse( ip[1].ToString( ), out _port );
                hc.addr = new IPEndPoint( _ipAddress, _port );
                hc.driver = this.Host.Network.DriverLevel;
                hc.ldriver = this.Host.Network.LanDriverLevel;
                hc.cname = _hostIP.ToString( ); //readaddr.ToString();

                // check for a name conflict
                for ( var i = 0; i < this.Host.Network.HostCacheCount; i++ )
                {
                    if ( i == n )
                        continue;
                    var hc2 = this.Host.Network.HostCache[i];
                    if ( hc.name == hc2.name )
                    {
                        i = hc.name.Length;
                        if ( i < 15 && hc.name[i - 1] > '8' )
                            hc.name = hc.name.Substring( 0, i ) + '0';
                        else
                            hc.name = hc.name.Substring( 0, i - 1 ) + ( char ) ( hc.name[i - 1] + 1 );
                        i = 0;// -1;
                    }
                }
            }
        }

        /// <summary>
        /// _Datagram_Connect
        /// </summary>
        private qsocket_t InternalConnect( string host )
        {
            // see if we can resolve the host name
            var sendaddr = this.Host.Network.LanDriver.GetAddrFromName( host );
            if ( sendaddr == null )
                return null;

            var newsock = this.Host.Network.LanDriver.OpenSocket( 0 );
            if ( newsock == null )
                return null;

            var sock = this.Host.Network.NewSocket( );
            if ( sock == null )
                goto ErrorReturn2;
            sock.socket = newsock;
            sock.landriver = this.Host.Network.LanDriverLevel;

            // connect to the host
            if (this.Host.Network.LanDriver.Connect( newsock, sendaddr ) == -1 )
                goto ErrorReturn;

            // send the connection request
            this.Host.Console.Print( "Connecting to " + sendaddr + "\n" );
            this.Host.Console.Print( "trying...\n" );
            this.Host.Screen.UpdateScreen( );
            var start_time = this.Host.Network.Time;
            var ret = 0;
            for ( var reps = 0; reps < 3; reps++ )
            {
                this.Host.Network.Message.Clear( );
                // save space for the header, filled in later
                this.Host.Network.Message.WriteLong( 0 );
                this.Host.Network.Message.WriteByte( CCReq.CCREQ_CONNECT );
                this.Host.Network.Message.WriteString( "QUAKE" );
                this.Host.Network.Message.WriteByte( NetworkDef.NET_PROTOCOL_VERSION );
                Utilities.WriteInt(
                    this.Host.Network.Message.Data, 0, EndianHelper.BigLong( NetFlags.NETFLAG_CTL |
                    (this.Host.Network.Message.Length & NetFlags.NETFLAG_LENGTH_MASK ) ) );
                //*((int *)net_message.data) = BigLong(NETFLAG_CTL | (net_message.cursize & NETFLAG_LENGTH_MASK));
                this.Host.Network.LanDriver.Write( newsock, this.Host.Network.Message.Data, this.Host.Network.Message.Length, sendaddr );
                this.Host.Network.Message.Clear( );
                EndPoint readaddr = new IPEndPoint( IPAddress.Any, 0 );
                do
                {
                    ret = this.Host.Network.Message.FillFrom(this.Host.Network, newsock, ref readaddr );
                    // if we got something, validate it
                    if ( ret > 0 )
                    {
                        // is it from the right place?
                        if ( sock.LanDriver.AddrCompare( readaddr, sendaddr ) != 0 )
                        {
#if DEBUG
                            this.Host.Console.Print( "wrong reply address\n" );
                            this.Host.Console.Print( "Expected: {0}\n", net_datagram.StrAddr( sendaddr ) );
                            this.Host.Console.Print( "Received: {0}\n", net_datagram.StrAddr( readaddr ) );
                            this.Host.Screen.UpdateScreen( );
#endif
                            ret = 0;
                            continue;
                        }

                        if ( ret < sizeof( int ) )
                        {
                            ret = 0;
                            continue;
                        }

                        this.Host.Network.Reader.Reset( );

                        var control = EndianHelper.BigLong(this.Host.Network.Reader.ReadLong( ) );// BigLong(*((int *)net_message.data));
                        //MSG_ReadLong();
                        if ( control == -1 )
                        {
                            ret = 0;
                            continue;
                        }
                        if ( ( control & ~NetFlags.NETFLAG_LENGTH_MASK ) != NetFlags.NETFLAG_CTL )
                        {
                            ret = 0;
                            continue;
                        }
                        if ( ( control & NetFlags.NETFLAG_LENGTH_MASK ) != ret )
                        {
                            ret = 0;
                            continue;
                        }
                    }
                }
                while ( ret == 0 && this.Host.Network.SetNetTime( ) - start_time < 2.5 );
                if ( ret > 0 )
                    break;

                this.Host.Console.Print( "still trying...\n" );
                this.Host.Screen.UpdateScreen( );
                start_time = this.Host.Network.SetNetTime( );
            }

            var reason = string.Empty;
            if ( ret == 0 )
            {
                reason = "No Response";
                this.Host.Console.Print( "{0}\n", reason );
                this.Host.Menu.ReturnReason = reason;
                goto ErrorReturn;
            }

            if ( ret == -1 )
            {
                reason = "Network Error";
                this.Host.Console.Print( "{0}\n", reason );
                this.Host.Menu.ReturnReason = reason;
                goto ErrorReturn;
            }

            ret = this.Host.Network.Reader.ReadByte( );
            if ( ret == CCRep.CCREP_REJECT )
            {
                reason = this.Host.Network.Reader.ReadString( );
                this.Host.Console.Print( reason );
                this.Host.Menu.ReturnReason = reason;
                goto ErrorReturn;
            }

            if ( ret == CCRep.CCREP_ACCEPT )
            {
                var ep = ( IPEndPoint ) sendaddr;
                sock.addr = new IPEndPoint( ep.Address, ep.Port );
                this.Host.Network.LanDriver.SetSocketPort( sock.addr, this.Host.Network.Reader.ReadLong( ) );
            }
            else
            {
                reason = "Bad Response";
                this.Host.Console.Print( "{0}\n", reason );
                this.Host.Menu.ReturnReason = reason;
                goto ErrorReturn;
            }

            sock.address = this.Host.Network.LanDriver.GetNameFromAddr( sendaddr );

            this.Host.Console.Print( "Connection accepted\n" );
            sock.lastMessageTime = this.Host.Network.SetNetTime( );

            // switch the connection to the specified address
            if (this.Host.Network.LanDriver.Connect( newsock, sock.addr ) == -1 )
            {
                reason = "Connect to Game failed";
                this.Host.Console.Print( "{0}\n", reason );
                this.Host.Menu.ReturnReason = reason;
                goto ErrorReturn;
            }

            this.Host.Menu.ReturnOnError = false;
            return sock;

        ErrorReturn:
            this.Host.Network.FreeSocket( sock );
        ErrorReturn2:
            this.Host.Network.LanDriver.CloseSocket( newsock );
            if (this.Host.Menu.ReturnOnError && this.Host.Menu.ReturnMenu != null )
            {
                this.Host.Menu.ReturnMenu.Show(this.Host );
                this.Host.Menu.ReturnOnError = false;
            }
            return null;
        }

        /// <summary>
        /// SendMessageNext
        /// </summary>
        private int SendMessageNext( qsocket_t sock )
        {
            int dataLen;
            int eom;
            if ( sock.sendMessageLength <= QDef.MAX_DATAGRAM )
            {
                dataLen = sock.sendMessageLength;
                eom = NetFlags.NETFLAG_EOM;
            }
            else
            {
                dataLen = QDef.MAX_DATAGRAM;
                eom = 0;
            }
            var packetLen = NetworkDef.NET_HEADERSIZE + dataLen;

            PacketHeader header;
            header.length = EndianHelper.BigLong( packetLen | NetFlags.NETFLAG_DATA | eom );
            header.sequence = EndianHelper.BigLong( ( int ) sock.sendSequence++ );
            Utilities.StructureToBytes( ref header, this._PacketBuffer, 0 );
            Buffer.BlockCopy( sock.sendMessage, 0, this._PacketBuffer, PacketHeader.SizeInBytes, dataLen );

            sock.sendNext = false;

            if ( sock.Write(this._PacketBuffer, packetLen, sock.addr ) == -1 )
                return -1;

            sock.lastSendTime = this.Host.Network.Time;
            this.packetsSent++;
            return 1;
        }

        /// <summary>
        /// ReSendMessage
        /// </summary>
        private int ReSendMessage( qsocket_t sock )
        {
            int dataLen, eom;
            if ( sock.sendMessageLength <= QDef.MAX_DATAGRAM )
            {
                dataLen = sock.sendMessageLength;
                eom = NetFlags.NETFLAG_EOM;
            }
            else
            {
                dataLen = QDef.MAX_DATAGRAM;
                eom = 0;
            }
            var packetLen = NetworkDef.NET_HEADERSIZE + dataLen;

            PacketHeader header;
            header.length = EndianHelper.BigLong( packetLen | NetFlags.NETFLAG_DATA | eom );
            header.sequence = EndianHelper.BigLong( ( int ) ( sock.sendSequence - 1 ) );
            Utilities.StructureToBytes( ref header, this._PacketBuffer, 0 );
            Buffer.BlockCopy( sock.sendMessage, 0, this._PacketBuffer, PacketHeader.SizeInBytes, dataLen );

            sock.sendNext = false;

            if ( sock.Write(this._PacketBuffer, packetLen, sock.addr ) == -1 )
                return -1;

            sock.lastSendTime = this.Host.Network.Time;
            this.packetsReSent++;
            return 1;
        }

        #endregion INetDriver Members
    }
}
