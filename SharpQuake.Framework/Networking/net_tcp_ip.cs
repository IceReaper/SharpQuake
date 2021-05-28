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
    using System;
    using System.Net;
    using System.Net.Sockets;

    public class net_tcp_ip : INetLanDriver, IDisposable
    {
        public static net_tcp_ip Instance => net_tcp_ip._Singletone;

        private const int WSAEWOULDBLOCK = 10035;
        private const int WSAECONNREFUSED = 10061;

        private static net_tcp_ip _Singletone = new();

        private bool _IsInitialised;
        private IPAddress _MyAddress; // unsigned long myAddr
        private Socket _ControlSocket; // int net_controlsocket;
        private Socket _BroadcastSocket; // net_broadcastsocket
        private EndPoint _BroadcastAddress; // qsockaddr broadcastaddr
        private Socket _AcceptSocket; // net_acceptsocket

        private net_tcp_ip()
        {
        }

        #region INetLanDriver Members

        public string Name => "TCP/IP";

        public bool IsInitialised => this._IsInitialised;

        public Socket ControlSocket => this._ControlSocket;

        public string MachineName
        {
            get;
            private set;
        }

        public string HostName
        {
            get;
            set;
        }

        public string HostAddress
        {
            get;
            private set;
        }

        public int HostPort
        {
            get;
            set;
        }

        /// <summary>
        /// UDP_Init
        /// </summary>
        public bool Initialise( )
        {
            this._IsInitialised = false;

            if( CommandLine.HasParam( "-noudp" ) )
                return false;

            try
            {
                this.MachineName = Dns.GetHostName();
            }
            catch( SocketException se )
            {
                ConsoleWrapper.DPrint( "Cannot get host name: {0}\n", se.Message );
                return false;
            }

            // if the quake hostname isn't set, set it to the machine name
            if(this.HostName == "UNNAMED" )
            {
                IPAddress addr;
                if( !IPAddress.TryParse(this.MachineName, out addr ) )
                {
                    var i = this.MachineName.IndexOf( '.' );
                    if ( i != -1 )
                        this.HostName = this.MachineName.Substring( 0, i );
                    else
                        this.HostName = this.MachineName;
                }
                //CVar.Set( "hostname", MachineName );
            }

            var i2 = CommandLine.CheckParm( "-ip" );
            if( i2 > 0 )
            {
                if( i2 < CommandLine.Argc - 1 )
                {
                    var ipaddr = CommandLine.Argv( i2 + 1 );
                    if( !IPAddress.TryParse( ipaddr, out this._MyAddress ) )
                        Utilities.Error( "{0} is not a valid IP address!", ipaddr );

                    this.HostAddress = ipaddr;
                }
                else
                    Utilities.Error( "Net.Init: you must specify an IP address after -ip" );
            }
            else
            {
                this._MyAddress = IPAddress.Any;
                this.HostAddress = "INADDR_ANY";
                //Host.Network.MyTcpIpAddress = "INADDR_ANY";
            }

            this._ControlSocket = this.OpenSocket( 0 );

            if(this._ControlSocket == null )
            {
                ConsoleWrapper.Print( "TCP/IP: Unable to open control socket\n" );
                return false;
            }

            this._BroadcastAddress = new IPEndPoint( IPAddress.Broadcast, this.HostPort );

            this._IsInitialised = true;
            ConsoleWrapper.Print( "TCP/IP Initialized\n" );
            return true;
        }

        public void Dispose()
        {
            this.Listen( false );
            this.CloseSocket(this._ControlSocket );
        }

        /// <summary>
        /// UDP_Listen
        /// </summary>
        public void Listen( bool state )
        {
            // enable listening
            if( state )
            {
                if(this._AcceptSocket == null )
                {
                    this._AcceptSocket = this.OpenSocket(this.HostPort );
                    if(this._AcceptSocket == null )
                        Utilities.Error( "UDP_Listen: Unable to open accept socket\n" );

                }
            }
            else
            {
                // disable listening
                if(this._AcceptSocket != null )
                {
                    this.CloseSocket(this._AcceptSocket );
                    this._AcceptSocket = null;
                }
            }
        }

        public Socket OpenSocket( int port )
        {
            Socket result = null;
            try
            {
                result = new( AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp );
                result.Blocking = false;
                result.SetIPProtectionLevel( IPProtectionLevel.Unrestricted );

                EndPoint ep = new IPEndPoint(this._MyAddress, port );
                result.Bind( ep );
            }
            catch( Exception ex )
            {
                if( result != null )
                {
                    result.Close();
                    result = null;
                }
                ConsoleWrapper.Print( "Unable to create socket: " + ex.Message );
            }

            return result;
        }

        public int CloseSocket( Socket socket )
        {
            if( socket == this._BroadcastSocket )
                this._BroadcastSocket = null;

            socket.Close();
            return 0;
        }

        public int Connect( Socket socket, EndPoint addr )
        {
            return 0;
        }

        public string GetNameFromAddr( EndPoint addr )
        {
            try
            {
                var entry = Dns.GetHostEntry( ( (IPEndPoint)addr ).Address );
                return entry.HostName;
            }
            catch( SocketException )
            {
            }
            return string.Empty;
        }

        public EndPoint GetAddrFromName( string name )
        {
            try
            {
                IPAddress addr;
                var i = name.IndexOf( ':' );
                string saddr;
                var port = this.HostPort;
                if( i != -1 )
                {
                    saddr = name.Substring( 0, i );
                    int p;
                    if( int.TryParse( name.Substring( i + 1 ), out p ) )
                        port = p;
                }
                else
                    saddr = name;

                if( IPAddress.TryParse( saddr, out addr ) )
                    return new IPEndPoint( addr, port );

                var entry = Dns.GetHostEntry( name );
                foreach( var addr2 in entry.AddressList )
                    return new IPEndPoint( addr2, port );
            }
            catch( SocketException )
            {
            }
            return null;
        }

        public int AddrCompare( EndPoint addr1, EndPoint addr2 )
        {
            if( addr1.AddressFamily != addr2.AddressFamily )
                return -1;

            var ep1 = addr1 as IPEndPoint;
            var ep2 = addr2 as IPEndPoint;

            if( ep1 == null || ep2 == null )
                return -1;

            if( !ep1.Address.Equals( ep2.Address ) )
                return -1;

            if( ep1.Port != ep2.Port )
                return 1;

            return 0;
        }

        public int GetSocketPort( EndPoint addr )
        {
            return ( (IPEndPoint)addr ).Port;
        }

        public int SetSocketPort( EndPoint addr, int port )
        {
            ( (IPEndPoint)addr ).Port = port;
            return 0;
        }

        public Socket CheckNewConnections()
        {
            if(this._AcceptSocket == null )
                return null;

            if(this._AcceptSocket.Available > 0 )
                return this._AcceptSocket;

            return null;
        }

        public int Read( Socket socket, byte[] buf, int len, ref EndPoint ep )
        {
            var ret = 0;
            try
            {
                ret = socket.ReceiveFrom( buf, len, SocketFlags.None, ref ep );
            }
            catch( SocketException se )
            {
                if( se.ErrorCode == net_tcp_ip.WSAEWOULDBLOCK || se.ErrorCode == net_tcp_ip.WSAECONNREFUSED )
                    ret = 0;
                else
                    ret = -1;
            }
            return ret;
        }

        public int Write( Socket socket, byte[] buf, int len, EndPoint ep )
        {
            var ret = 0;
            try
            {
                ret = socket.SendTo( buf, len, SocketFlags.None, ep );
            }
            catch( SocketException se )
            {
                if( se.ErrorCode == net_tcp_ip.WSAEWOULDBLOCK )
                    ret = 0;
                else
                    ret = -1;
            }
            return ret;
        }

        public int Broadcast( Socket socket, byte[] buf, int len )
        {
            if( socket != this._BroadcastSocket )
            {
                if(this._BroadcastSocket != null )
                    Utilities.Error( "Attempted to use multiple broadcasts sockets\n" );
                try
                {
                    socket.EnableBroadcast = true;
                }
                catch( SocketException se )
                {
                    ConsoleWrapper.Print( "Unable to make socket broadcast capable: {0}\n", se.Message );
                    return -1;
                }
            }

            return this.Write( socket, buf, len, this._BroadcastAddress );
        }

        #endregion INetLanDriver Members
    }
}
