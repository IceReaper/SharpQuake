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
    using Definitions;
    using System.Net;
    using System.Net.Sockets;

    // qsocket_t
    public class qsocket_t
    {
        public INetLanDriver LanDriver => NetworkWrapper.GetLanDriver(this.landriver );

        public double connecttime;
        public double lastMessageTime;
        public double lastSendTime;

        public bool disconnected;
        public bool canSend;
        public bool sendNext;

        public int driver;
        public int landriver;
        public Socket socket; // int	socket
        public object driverdata; // void *driverdata

        public uint ackSequence;
        public uint sendSequence;
        public uint unreliableSendSequence;

        public int sendMessageLength;
        public byte[] sendMessage; // byte sendMessage [NET_MAXMESSAGE]

        public uint receiveSequence;
        public uint unreliableReceiveSequence;

        public int receiveMessageLength;
        public byte[] receiveMessage; // byte receiveMessage [NET_MAXMESSAGE]

        public EndPoint addr; // qsockaddr	addr
        public string address; // char address[NET_NAMELEN]

        public void ClearBuffers( )
        {
            this.sendMessageLength = 0;
            this.receiveMessageLength = 0;
        }

        public int Read( byte[] buf, int len, ref EndPoint ep )
        {
            return this.LanDriver.Read(this.socket, buf, len, ref ep );
        }

        public int Write( byte[] buf, int len, EndPoint ep )
        {
            return this.LanDriver.Write(this.socket, buf, len, ep );
        }

        public qsocket_t( )
        {
            this.sendMessage = new byte[NetworkDef.NET_MAXMESSAGE];
            this.receiveMessage = new byte[NetworkDef.NET_MAXMESSAGE];
            this.disconnected = true;
        }
    }
}
