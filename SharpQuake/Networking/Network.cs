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
	using Framework.IO;
	using Framework.Mathematics;
	using Framework.Networking;
	using System;
	using System.Collections.Generic;
	using System.Net;
	using System.Net.Sockets;
	using System.Runtime.InteropServices;
	using System.Text;

	internal delegate void PollHandler( object arg );

	public class Network
	{
		public INetDriver[] Drivers => this._Drivers;

		public INetLanDriver[] LanDrivers => this._LanDrivers;

		public IEnumerable<qsocket_t> ActiveSockets => this._ActiveSockets;

		public IEnumerable<qsocket_t> FreeSockets => this._ActiveSockets;

		public int MessagesSent => this._MessagesSent;

		public int MessagesReceived => this._MessagesReceived;

		public int UnreliableMessagesSent => this._UnreliableMessagesSent;

		public int UnreliableMessagesReceived => this._UnreliableMessagesReceived;

		public string HostName => this.Host.Cvars.HostName.Get<string>();

		public string MyTcpIpAddress
		{
			get => this._MyTcpIpAddress;
			set => this._MyTcpIpAddress = value;
		}

		public int DefaultHostPort => this._DefHostPort;

		public bool TcpIpAvailable => net_tcp_ip.Instance.IsInitialised;

		public hostcache_t[] HostCache => this._HostCache;

		public int DriverLevel => this._DriverLevel;

		public INetLanDriver LanDriver => this._LanDrivers[this.LanDriverLevel];

		public INetDriver Driver => this._Drivers[this._DriverLevel];

		public bool SlistInProgress => this._SlistInProgress;

		public double Time => this._Time;

		public int HostPort;

		public int ActiveConnections;

		public MessageWriter Message;

		// sizebuf_t net_message
		public MessageReader Reader;

		public int HostCacheCount;

		public bool SlistSilent;

		// slistSilent
		public bool SlistLocal = true;

		public int LanDriverLevel;

		private PollProcedure _SlistSendProcedure;
		private PollProcedure _SlistPollProcedure;

		private INetDriver[] _Drivers;

		// net_driver_t net_drivers[MAX_NET_DRIVERS];
		private INetLanDriver[] _LanDrivers;

		// net_landriver_t	net_landrivers[MAX_NET_DRIVERS]
		private bool _IsRecording;

		// recording
		private int _DefHostPort = 26000;

		// int	DEFAULTnet_hostport = 26000;
		// net_hostport;
		private bool _IsListening;

		// qboolean	listening = false;
		private List<qsocket_t> _FreeSockets;

		// net_freeSockets
		private List<qsocket_t> _ActiveSockets;

		// net_activeSockets
		// net_activeconnections
		private double _Time;

		private string _MyTcpIpAddress;

		// char my_tcpip_address[NET_NAMELEN];
		private int _MessagesSent = 0;

		// reads from net_message
		private int _MessagesReceived = 0;

		// net_time
		private int _UnreliableMessagesSent = 0;

		private int _UnreliableMessagesReceived = 0;

		private PollProcedure _PollProcedureList;

		private hostcache_t[] _HostCache = new hostcache_t[NetworkDef.HOSTCACHESIZE];

		private bool _SlistInProgress;

		// slistInProgress
		// slistLocal
		private int _SlistLastShown;

		// slistLastShown
		private double _SlistStartTime;

		private int _DriverLevel;

		private VcrRecord _VcrConnect = new();

		// vcrConnect
		private VcrRecord2 _VcrGetMessage = new();

		// vcrGetMessage
		private VcrRecord2 _VcrSendMessage = new();

		public Network( Host host )
		{
			this.Host = host;

			this._SlistSendProcedure = new( null, 0.0, this.SlistSend, null );
			this._SlistPollProcedure = new( null, 0.0, this.SlistPoll, null );

			// Temporary workaround will sort out soon
			NetworkWrapper.OnGetLanDriver += ( index ) =>
			{
				return this.LanDrivers[index];
			};
		}

		// CHANGE
		private Host Host
		{
			get;
			set;
		}

		// vcrSendMessage
		// NET_Init (void)
		public void Initialise( )
		{
			for ( var i2 = 0; i2 < this._HostCache.Length; i2++ )
				this._HostCache[i2] = new();

			if (this._Drivers == null )
			{
				if ( CommandLine.HasParam( "-playback" ) )
				{
					this._Drivers = new INetDriver[]
					{
						new net_vcr()
					};
				}
				else
				{
					this._Drivers = new INetDriver[]
					{
						new net_loop(),
						net_datagram.Instance
					};
				}
			}

			if (this._LanDrivers == null )
			{
				this._LanDrivers = new INetLanDriver[]
				{
					net_tcp_ip.Instance
				};
			}

			if ( CommandLine.HasParam( "-record" ) )
				this._IsRecording = true;

			var i = CommandLine.CheckParm( "-port" );
			if ( i == 0 )
				i = CommandLine.CheckParm( "-udpport" );
			if ( i == 0 )
				i = CommandLine.CheckParm( "-ipxport" );

			if ( i > 0 )
			{
				if ( i < CommandLine.Argc - 1 )
					this._DefHostPort = MathLib.atoi( CommandLine.Argv( i + 1 ) );
				else
					Utilities.Error( "Net.Init: you must specify a number after -port!" );
			}

			this.HostPort = this._DefHostPort;

			if ( CommandLine.HasParam( "-listen" ) || this.Host.Client.cls.state == cactive_t.ca_dedicated )
				this._IsListening = true;
			var numsockets = this.Host.Server.svs.maxclientslimit;
			if (this.Host.Client.cls.state != cactive_t.ca_dedicated )
				numsockets++;

			this._FreeSockets = new( numsockets );
			this._ActiveSockets = new( numsockets );

			for ( i = 0; i < numsockets; i++ )
				this._FreeSockets.Add( new() );

			this.SetNetTime();

			// allocate space for network message buffer
			this.Message = new( NetworkDef.NET_MAXMESSAGE ); // SZ_Alloc (&net_message, NET_MAXMESSAGE);
			this.Reader = new(this.Message );

			if (this.Host.Cvars.MessageTimeout == null )
			{
				this.Host.Cvars.MessageTimeout = this.Host.CVars.Add( "net_messagetimeout", 300 );
				this.Host.Cvars.HostName = this.Host.CVars.Add( "hostname", "UNNAMED" );
			}

			this.Host.Commands.Add( "slist", this.Slist_f );
			this.Host.Commands.Add( "listen", this.Listen_f );
			this.Host.Commands.Add( "maxplayers", this.MaxPlayers_f );
			this.Host.Commands.Add( "port", this.Port_f );

			// initialize all the drivers
			this._DriverLevel = 0;
			foreach ( var driver in this._Drivers )
			{
				driver.Initialise(this.Host );
				if ( driver.IsInitialised && this._IsListening )
					driver.Listen( true );

				this._DriverLevel++;
			}

			//if (*my_ipx_address)
			//    Con_DPrintf("IPX address %s\n", my_ipx_address);
			if ( !string.IsNullOrEmpty(this._MyTcpIpAddress ) )
				this.Host.Console.DPrint( "TCP/IP address {0}\n", this._MyTcpIpAddress );
		}

		// net_driverlevel
		// net_landriverlevel
		/// <summary>
		/// NET_Shutdown
		/// </summary>
		public void Shutdown( )
		{
			this.SetNetTime();

			if (this._ActiveSockets != null )
			{
				var tmp = this._ActiveSockets.ToArray();
				foreach ( var sock in tmp )
					this.Close( sock );
			}

			//
			// shutdown the drivers
			//
			if (this._Drivers != null )
			{
				for (this._DriverLevel = 0; this._DriverLevel < this._Drivers.Length; this._DriverLevel++ )
				{
					if (this._Drivers[this._DriverLevel].IsInitialised )
						this._Drivers[this._DriverLevel].Shutdown();
				}
			}
		}

		// slistStartTime
		/// <summary>
		/// NET_CheckNewConnections
		/// </summary>
		/// <returns></returns>
		public qsocket_t CheckNewConnections( )
		{
			this.SetNetTime();

			for (this._DriverLevel = 0; this._DriverLevel < this._Drivers.Length; this._DriverLevel++ )
			{
				if ( !this._Drivers[this._DriverLevel].IsInitialised )
					continue;

				if (this._DriverLevel > 0 && !this._IsListening )
					continue;

				var ret = this.Driver.CheckNewConnections();
				if ( ret != null )
				{
					if (this._IsRecording )
					{
						this._VcrConnect.time = this.Host.Time;
						this._VcrConnect.op = VcrOp.VCR_OP_CONNECT;
						this._VcrConnect.session = 1; // (long)ret; // Uze: todo: make it work on 64bit systems
						var buf = Utilities.StructureToBytes( ref this._VcrConnect );
						this.Host.VcrWriter.Write( buf, 0, buf.Length );
						buf = Encoding.ASCII.GetBytes( ret.address );
						var count = Math.Min( buf.Length, NetworkDef.NET_NAMELEN );
						var extra = NetworkDef.NET_NAMELEN - count;
						this.Host.VcrWriter.Write( buf, 0, count );
						for ( var i = 0; i < extra; i++ )
							this.Host.VcrWriter.Write( ( byte ) 0 );
					}
					return ret;
				}
			}

			if (this._IsRecording )
			{
				this._VcrConnect.time = this.Host.Time;
				this._VcrConnect.op = VcrOp.VCR_OP_CONNECT;
				this._VcrConnect.session = 0;
				var buf = Utilities.StructureToBytes( ref this._VcrConnect );
				this.Host.VcrWriter.Write( buf, 0, buf.Length );
			}

			return null;
		}

		// hostcache
		// hostCacheCount
		/// <summary>
		/// NET_Connect
		/// called by client to connect to a host.  Returns -1 if not able to connect
		/// </summary>
		public qsocket_t Connect( string host )
		{
			var numdrivers = this._Drivers.Length;// net_numdrivers;

			this.SetNetTime();

			if ( string.IsNullOrEmpty( host ) )
				host = null;

			if ( host != null )
			{
				if ( Utilities.SameText( host, "local" ) )
				{
					numdrivers = 1;
					goto JustDoIt;
				}

				if (this.HostCacheCount > 0 )
				{
					foreach ( var hc in this._HostCache )
					{
						if ( Utilities.SameText( hc.name, host ) )
						{
							host = hc.cname;
							goto JustDoIt;
						}
					}
				}
			}

			this.SlistSilent = host != null;
			this.Slist_f( null );

			while (this._SlistInProgress )
				this.Poll();

			if ( host == null )
			{
				if (this.HostCacheCount != 1 )
					return null;
				host = this._HostCache[0].cname;
				this.Host.Console.Print( "Connecting to...\n{0} @ {1}\n\n", this._HostCache[0].name, host );
			}

			this._DriverLevel = 0;
			foreach ( var hc in this._HostCache )
			{
				if ( Utilities.SameText( host, hc.name ) )
				{
					host = hc.cname;
					break;
				}

				this._DriverLevel++;
			}

		JustDoIt:
			this._DriverLevel = 0;
			foreach ( var drv in this._Drivers )
			{
				if ( !drv.IsInitialised )
					continue;
				var ret = drv.Connect( host );
				if ( ret != null )
					return ret;

				this._DriverLevel++;
			}

			if ( host != null )
			{
				this.Host.Console.Print( "\n" );
				this.PrintSlistHeader();
				this.PrintSlist();
				this.PrintSlistTrailer();
			}

			return null;
		}

		/// <summary>
		/// NET_CanSendMessage
		/// Returns true or false if the given qsocket can currently accept a
		/// message to be transmitted.
		/// </summary>
		public bool CanSendMessage( qsocket_t sock )
		{
			if ( sock == null )
				return false;

			if ( sock.disconnected )
				return false;

			this.SetNetTime();

			var r = this._Drivers[sock.driver].CanSendMessage( sock );

			if (this._IsRecording )
			{
				this._VcrSendMessage.time = this.Host.Time;
				this._VcrSendMessage.op = VcrOp.VCR_OP_CANSENDMESSAGE;
				this._VcrSendMessage.session = 1; // (long)sock; Uze: todo: do something?
				this._VcrSendMessage.ret = r ? 1 : 0;
				var buf = Utilities.StructureToBytes( ref this._VcrSendMessage );
				this.Host.VcrWriter.Write( buf, 0, buf.Length );
			}

			return r;
		}

		/// <summary>
		/// NET_GetMessage
		/// returns data in net_message sizebuf
		/// returns 0 if no data is waiting
		/// returns 1 if a message was received
		/// returns 2 if an unreliable message was received
		/// returns -1 if the connection died
		/// </summary>
		public int GetMessage( qsocket_t sock )
		{
			//int ret;

			if ( sock == null )
				return -1;

			if ( sock.disconnected )
			{
				this.Host.Console.Print( "NET_GetMessage: disconnected socket\n" );
				return -1;
			}

			this.SetNetTime();

			var ret = this._Drivers[sock.driver].GetMessage( sock );

			// see if this connection has timed out
			if ( ret == 0 && sock.driver != 0 )
			{
				if (this._Time - sock.lastMessageTime > this.Host.Cvars.MessageTimeout.Get<int>() )
				{
					this.Close( sock );
					return -1;
				}
			}

			if ( ret > 0 )
			{
				if ( sock.driver != 0 )
				{
					sock.lastMessageTime = this._Time;
					if ( ret == 1 )
						this._MessagesReceived++;
					else if ( ret == 2 )
						this._UnreliableMessagesReceived++;
				}

				if (this._IsRecording )
				{
					this._VcrGetMessage.time = this.Host.Time;
					this._VcrGetMessage.op = VcrOp.VCR_OP_GETMESSAGE;
					this._VcrGetMessage.session = 1;// (long)sock; Uze todo: write somethisng meaningful
					this._VcrGetMessage.ret = ret;
					var buf = Utilities.StructureToBytes( ref this._VcrGetMessage );
					this.Host.VcrWriter.Write( buf, 0, buf.Length );
					this.Host.VcrWriter.Write(this.Message.Length );
					this.Host.VcrWriter.Write(this.Message.Data, 0, this.Message.Length );
				}
			}
			else
			{
				if (this._IsRecording )
				{
					this._VcrGetMessage.time = this.Host.Time;
					this._VcrGetMessage.op = VcrOp.VCR_OP_GETMESSAGE;
					this._VcrGetMessage.session = 1; // (long)sock; Uze todo: fix this
					this._VcrGetMessage.ret = ret;
					var buf = Utilities.StructureToBytes( ref this._VcrGetMessage );
					this.Host.VcrWriter.Write( buf, 0, buf.Length );
				}
			}

			return ret;
		}

		/// <summary>
		/// NET_SendMessage
		/// Try to send a complete length+message unit over the reliable stream.
		/// returns 0 if the message cannot be delivered reliably, but the connection
		/// is still considered valid
		/// returns 1 if the message was sent properly
		/// returns -1 if the connection died
		/// </summary>
		public int SendMessage( qsocket_t sock, MessageWriter data )
		{
			if ( sock == null )
				return -1;

			if ( sock.disconnected )
			{
				this.Host.Console.Print( "NET_SendMessage: disconnected socket\n" );
				return -1;
			}

			this.SetNetTime();

			var r = this._Drivers[sock.driver].SendMessage( sock, data );
			if ( r == 1 && sock.driver != 0 )
				this._MessagesSent++;

			if (this._IsRecording )
			{
				this._VcrSendMessage.time = this.Host.Time;
				this._VcrSendMessage.op = VcrOp.VCR_OP_SENDMESSAGE;
				this._VcrSendMessage.session = 1; // (long)sock; Uze: todo: do something?
				this._VcrSendMessage.ret = r;
				var buf = Utilities.StructureToBytes( ref this._VcrSendMessage );
				this.Host.VcrWriter.Write( buf, 0, buf.Length );
			}

			return r;
		}

		/// <summary>
		/// NET_SendUnreliableMessage
		/// returns 0 if the message connot be delivered reliably, but the connection
		///		is still considered valid
		/// returns 1 if the message was sent properly
		/// returns -1 if the connection died
		/// </summary>
		public int SendUnreliableMessage( qsocket_t sock, MessageWriter data )
		{
			if ( sock == null )
				return -1;

			if ( sock.disconnected )
			{
				this.Host.Console.Print( "NET_SendMessage: disconnected socket\n" );
				return -1;
			}

			this.SetNetTime();

			var r = this._Drivers[sock.driver].SendUnreliableMessage( sock, data );
			if ( r == 1 && sock.driver != 0 )
				this._UnreliableMessagesSent++;

			if (this._IsRecording )
			{
				this._VcrSendMessage.time = this.Host.Time;
				this._VcrSendMessage.op = VcrOp.VCR_OP_SENDMESSAGE;
				this._VcrSendMessage.session = 1;// (long)sock; Uze todo: ???????
				this._VcrSendMessage.ret = r;
				var buf = Utilities.StructureToBytes( ref this._VcrSendMessage );
				this.Host.VcrWriter.Write( buf );
			}

			return r;
		}

		/// <summary>
		/// NET_SendToAll
		/// This is a reliable *blocking* send to all attached clients.
		/// </summary>
		public int SendToAll( MessageWriter data, int blocktime )
		{
			var state1 = new bool[QDef.MAX_SCOREBOARD];
			var state2 = new bool[QDef.MAX_SCOREBOARD];

			var count = 0;
			for ( var i = 0; i < this.Host.Server.svs.maxclients; i++ )
			{
				this.Host.HostClient = this.Host.Server.svs.clients[i];
				if (this.Host.HostClient.netconnection == null )
					continue;

				if (this.Host.HostClient.active )
				{
					if (this.Host.HostClient.netconnection.driver == 0 )
					{
						this.SendMessage(this.Host.HostClient.netconnection, data );
						state1[i] = true;
						state2[i] = true;
						continue;
					}
					count++;
					state1[i] = false;
					state2[i] = false;
				}
				else
				{
					state1[i] = true;
					state2[i] = true;
				}
			}

			var start = Timer.GetFloatTime();
			while ( count > 0 )
			{
				count = 0;
				for ( var i = 0; i < this.Host.Server.svs.maxclients; i++ )
				{
					this.Host.HostClient = this.Host.Server.svs.clients[i];
					if ( !state1[i] )
					{
						if (this.CanSendMessage(this.Host.HostClient.netconnection ) )
						{
							state1[i] = true;
							this.SendMessage(this.Host.HostClient.netconnection, data );
						}
						else
							this.GetMessage(this.Host.HostClient.netconnection );

						count++;
						continue;
					}

					if ( !state2[i] )
					{
						if (this.CanSendMessage(this.Host.HostClient.netconnection ) )
							state2[i] = true;
						else
							this.GetMessage(this.Host.HostClient.netconnection );

						count++;
						continue;
					}
				}
				if ( Timer.GetFloatTime() - start > blocktime )
					break;
			}
			return count;
		}

		/// <summary>
		/// NET_Close
		/// </summary>
		public void Close( qsocket_t sock )
		{
			if ( sock == null )
				return;

			if ( sock.disconnected )
				return;

			this.SetNetTime();

			// call the driver_Close function
			this._Drivers[sock.driver].Close( sock );

			this.FreeSocket( sock );
		}

		/// <summary>
		/// NET_FreeQSocket
		/// </summary>
		public void FreeSocket( qsocket_t sock )
		{
			// remove it from active list
			if ( !this._ActiveSockets.Remove( sock ) )
				Utilities.Error( "NET_FreeQSocket: not active\n" );

			// add it to free list
			this._FreeSockets.Add( sock );
			sock.disconnected = true;
		}

		/// <summary>
		/// NET_Poll
		/// </summary>
		public void Poll( )
		{
			this.SetNetTime();

			for ( var pp = this._PollProcedureList; pp != null; pp = pp.next )
			{
				if ( pp.nextTime > this._Time )
					break;

				this._PollProcedureList = pp.next;
				pp.procedure( pp.arg );
			}
		}

		// double SetNetTime
		public double SetNetTime( )
		{
			this._Time = Timer.GetFloatTime();
			return this._Time;
		}

		/// <summary>
		/// NET_Slist_f
		/// </summary>
		public void Slist_f( CommandMessage msg )
		{
			if (this._SlistInProgress )
				return;

			if ( !this.SlistSilent )
			{
				this.Host.Console.Print( "Looking for Quake servers...\n" );
				this.PrintSlistHeader();
			}

			this._SlistInProgress = true;
			this._SlistStartTime = Timer.GetFloatTime();

			this.SchedulePollProcedure(this._SlistSendProcedure, 0.0 );
			this.SchedulePollProcedure(this._SlistPollProcedure, 0.1 );

			this.HostCacheCount = 0;
		}

		/// <summary>
		/// NET_NewQSocket
		/// Called by drivers when a new communications endpoint is required
		/// The sequence and buffer fields will be filled in properly
		/// </summary>
		public qsocket_t NewSocket( )
		{
			if (this._FreeSockets.Count == 0 )
				return null;

			if (this.ActiveConnections >= this.Host.Server.svs.maxclients )
				return null;

			// get one from free list
			var i = this._FreeSockets.Count - 1;
			var sock = this._FreeSockets[i];
			this._FreeSockets.RemoveAt( i );

			// add it to active list
			this._ActiveSockets.Add( sock );

			sock.disconnected = false;
			sock.connecttime = this._Time;
			sock.address = "UNSET ADDRESS";
			sock.driver = this._DriverLevel;
			sock.socket = null;
			sock.driverdata = null;
			sock.canSend = true;
			sock.sendNext = false;
			sock.lastMessageTime = this._Time;
			sock.ackSequence = 0;
			sock.sendSequence = 0;
			sock.unreliableSendSequence = 0;
			sock.sendMessageLength = 0;
			sock.receiveSequence = 0;
			sock.unreliableReceiveSequence = 0;
			sock.receiveMessageLength = 0;

			return sock;
		}

		// pollProcedureList
		private void PrintSlistHeader( )
		{
			this.Host.Console.Print( "Server          Map             Users\n" );
			this.Host.Console.Print( "--------------- --------------- -----\n" );
			this._SlistLastShown = 0;
		}

		// = { "hostname", "UNNAMED" };
		private void PrintSlist( )
		{
			int i;
			for ( i = this._SlistLastShown; i < this.HostCacheCount; i++ )
			{
				var hc = this._HostCache[i];
				if ( hc.maxusers != 0 )
					this.Host.Console.Print( "{0,-15} {1,-15}\n {2,2}/{3,2}\n", Utilities.Copy( hc.name, 15 ), Utilities.Copy( hc.map, 15 ), hc.users, hc.maxusers );
				else
					this.Host.Console.Print( "{0,-15} {1,-15}\n", Utilities.Copy( hc.name, 15 ), Utilities.Copy( hc.map, 15 ) );
			}

			this._SlistLastShown = i;
		}

		private void PrintSlistTrailer( )
		{
			if (this.HostCacheCount != 0 )
				this.Host.Console.Print( "== end list ==\n\n" );
			else
				this.Host.Console.Print( "No Quake servers found.\n\n" );
		}

		/// <summary>
		/// SchedulePollProcedure
		/// </summary>
		private void SchedulePollProcedure( PollProcedure proc, double timeOffset )
		{
			proc.nextTime = Timer.GetFloatTime() + timeOffset;
			PollProcedure pp, prev;
			for ( pp = this._PollProcedureList, prev = null; pp != null; pp = pp.next )
			{
				if ( pp.nextTime >= proc.nextTime )
					break;
				prev = pp;
			}

			if ( prev == null )
			{
				proc.next = this._PollProcedureList;
				this._PollProcedureList = proc;
				return;
			}

			proc.next = pp;
			prev.next = proc;
		}

		// NET_Listen_f
		private void Listen_f( CommandMessage msg )
		{
			if ( msg.Parameters == null || msg.Parameters.Length != 1 )
			{
				this.Host.Console.Print( "\"listen\" is \"{0}\"\n", this._IsListening ? 1 : 0 );
				return;
			}

			this._IsListening = MathLib.atoi( msg.Parameters[0] ) != 0;

			foreach ( var driver in this._Drivers )
			{
				if ( driver.IsInitialised )
					driver.Listen(this._IsListening );
			}
		}

		// MaxPlayers_f
		private void MaxPlayers_f( CommandMessage msg )
		{
			if ( msg.Parameters == null || msg.Parameters.Length != 1 )
			{
				this.Host.Console.Print( $"\"maxplayers\" is \"{this.Host.Server.svs.maxclients}\"\n" );
				return;
			}

			if (this.Host.Server.sv.active )
			{
				this.Host.Console.Print( "maxplayers can not be changed while a server is running.\n" );
				return;
			}

			var n = MathLib.atoi( msg.Parameters[0] );
			if ( n < 1 )
				n = 1;
			if ( n > this.Host.Server.svs.maxclientslimit )
			{
				n = this.Host.Server.svs.maxclientslimit;
				this.Host.Console.Print( "\"maxplayers\" set to \"{0}\"\n", n );
			}

			if ( n == 1 && this._IsListening )
				this.Host.Commands.Buffer.Append( "listen 0\n" );

			if ( n > 1 && !this._IsListening )
				this.Host.Commands.Buffer.Append( "listen 1\n" );

			this.Host.Server.svs.maxclients = n;
			if ( n == 1 )
				this.Host.CVars.Set( "deathmatch", 0 );
			else
				this.Host.CVars.Set( "deathmatch", 1 );
		}

		// NET_Port_f
		private void Port_f( CommandMessage msg )
		{
			if ( msg.Parameters == null || msg.Parameters.Length != 1 )
			{
				this.Host.Console.Print( $"\"port\" is \"{this.HostPort}\"\n" );
				return;
			}

			var n = MathLib.atoi( msg.Parameters[0] );
			if ( n < 1 || n > 65534 )
			{
				this.Host.Console.Print( "Bad value, must be between 1 and 65534\n" );
				return;
			}

			this._DefHostPort = n;
			this.HostPort = n;

			if (this._IsListening )
			{
				// force a change to the new port
				this.Host.Commands.Buffer.Append( "listen 0\n" );
				this.Host.Commands.Buffer.Append( "listen 1\n" );
			}
		}

		/// <summary>
		/// Slist_Send
		/// </summary>
		private void SlistSend( object arg )
		{
			for (this._DriverLevel = 0; this._DriverLevel < this._Drivers.Length; this._DriverLevel++ )
			{
				if ( !this.SlistLocal && this._DriverLevel == 0 )
					continue;
				if ( !this._Drivers[this._DriverLevel].IsInitialised )
					continue;

				this._Drivers[this._DriverLevel].SearchForHosts( true );
			}

			if ( Timer.GetFloatTime() - this._SlistStartTime < 0.5 )
				this.SchedulePollProcedure(this._SlistSendProcedure, 0.75 );
		}

		/// <summary>
		/// Slist_Poll
		/// </summary>
		private void SlistPoll( object arg )
		{
			for (this._DriverLevel = 0; this._DriverLevel < this._Drivers.Length; this._DriverLevel++ )
			{
				if ( !this.SlistLocal && this._DriverLevel == 0 )
					continue;
				if ( !this._Drivers[this._DriverLevel].IsInitialised )
					continue;

				this._Drivers[this._DriverLevel].SearchForHosts( false );
			}

			if ( !this.SlistSilent )
				this.PrintSlist();

			if ( Timer.GetFloatTime() - this._SlistStartTime < 1.5 )
			{
				this.SchedulePollProcedure(this._SlistPollProcedure, 0.1 );
				return;
			}

			if ( !this.SlistSilent )
				this.PrintSlistTrailer();

			this._SlistInProgress = false;
			this.SlistSilent = false;
			this.SlistLocal = true;
		}

		[StructLayout( LayoutKind.Sequential, Pack = 1 )]
		private class VcrRecord2 : VcrRecord
		{
			public int ret;
			// Uze: int len - removed
		} //vcrGetMessage;

		// Temporary fix to support pulling messagereader/writer from main code


	}

	public static class MessageWriterExtensions
	{
		public static int FillFrom( this MessageWriter writer, Network network, Socket socket, ref EndPoint ep )
		{
			writer.Clear();
			var result = network.LanDriver.Read( socket, writer._Buffer, writer._Buffer.Length, ref ep );
			if ( result >= 0 )
				writer._Count = result;
			return result;
		}
	}
	/// <summary>
	/// NetHeader flags
	/// </summary>
	internal static class NetFlags
	{
		public const int NETFLAG_LENGTH_MASK = 0x0000ffff;
		public const int NETFLAG_DATA = 0x00010000;
		public const int NETFLAG_ACK = 0x00020000;
		public const int NETFLAG_NAK = 0x00040000;
		public const int NETFLAG_EOM = 0x00080000;
		public const int NETFLAG_UNRELIABLE = 0x00100000;
		public const int NETFLAG_CTL = -2147483648;// 0x80000000;
	}

	internal static class CCReq
	{
		public const int CCREQ_CONNECT = 0x01;
		public const int CCREQ_SERVER_INFO = 0x02;
		public const int CCREQ_PLAYER_INFO = 0x03;
		public const int CCREQ_RULE_INFO = 0x04;
	}

	//	note:
	//		There are two address forms used above.  The short form is just a
	//		port number.  The address that goes along with the port is defined as
	//		"whatever address you receive this reponse from".  This lets us use
	//		the host OS to solve the problem of multiple host addresses (possibly
	//		with no routing between them); the host will use the right address
	//		when we reply to the inbound connection request.  The long from is
	//		a full address and port in a string.  It is used for returning the
	//		address of a server that is not running locally.
	internal static class CCRep
	{
		public const int CCREP_ACCEPT = 0x81;
		public const int CCREP_REJECT = 0x82;
		public const int CCREP_SERVER_INFO = 0x83;
		public const int CCREP_PLAYER_INFO = 0x84;
		public const int CCREP_RULE_INFO = 0x85;
	}



	internal class PollProcedure
	{
		public PollProcedure next;
		public double nextTime;
		public PollHandler procedure; // void (*procedure)();
		public object arg; // void *arg

		public PollProcedure( PollProcedure next, double nextTime, PollHandler handler, object arg )
		{
			this.next = next;
			this.nextTime = nextTime;
			this.procedure = handler;
			this.arg = arg;
		}
	}






	// PollProcedure;

	//hostcache_t;
	// This is the network info/connection protocol.  It is used to find Quake
	// servers, get info about them, and connect to them.  Once connected, the
	// Quake game protocol (documented elsewhere) is used.
	//
	//
	// General notes:
	//	game_name is currently always "QUAKE", but is there so this same protocol
	//		can be used for future games as well; can you say Quake2?
	//
	// CCREQ_CONNECT
	//		string	game_name				"QUAKE"
	//		byte	net_protocol_version	NET_PROTOCOL_VERSION
	//
	// CCREQ_SERVER_INFO
	//		string	game_name				"QUAKE"
	//		byte	net_protocol_version	NET_PROTOCOL_VERSION
	//
	// CCREQ_PLAYER_INFO
	//		byte	player_number
	//
	// CCREQ_RULE_INFO
	//		string	rule
	//
	//
	//
	// CCREP_ACCEPT
	//		long	port
	//
	// CCREP_REJECT
	//		string	reason
	//
	// CCREP_SERVER_INFO
	//		string	server_address
	//		string	host_name
	//		string	level_name
	//		byte	current_players
	//		byte	max_players
	//		byte	protocol_version	NET_PROTOCOL_VERSION
	//
	// CCREP_PLAYER_INFO
	//		byte	player_number
	//		string	name
	//		long	colors
	//		long	frags
	//		long	connect_time
	//		string	address
	//
	// CCREP_RULE_INFO
	//		string	rule
	//		string	value
}
