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

namespace SharpQuake.Networking.Server
{
	using Client;
	using Desktop;
	using Engine.Host;
	using Framework.Definitions;
	using Framework.Definitions.Bsp;
	using Framework.Engine;
	using Framework.IO;
	using Framework.IO.BSP.Q1;
	using Framework.Mathematics;
	using Framework.Networking;
	using Game.Data.Models;
	using Game.Networking.Server;
	using Game.Rendering;
	using Game.Rendering.Memory;
	using Sound;
	using System;
	using System.Numerics;

	partial class server
	{
		private int _FatBytes; // fatbytes
		private byte[] _FatPvs = new byte[BspDef.MAX_MAP_LEAFS / 8]; // fatpvs

		// Instances
		private Host Host
		{
			get;
			set;
		}

		// SV_Init
		public void Initialise( )
		{
			for ( var i = 0; i < this._BoxClipNodes.Length; i++ )
				this._BoxClipNodes[i].children = new short[2];

			for ( var i = 0; i < this._BoxPlanes.Length; i++ )
				this._BoxPlanes[i] = new();

			for ( var i = 0; i < this._AreaNodes.Length; i++ )
				this._AreaNodes[i] = new();

			if (this.Host.Cvars.Friction == null )
			{
				this.Host.Cvars.Friction = this.Host.CVars.Add( "sv_friction", 4f, ClientVariableFlags.Server );
				this.Host.Cvars.EdgeFriction = this.Host.CVars.Add( "edgefriction", 2f );
				this.Host.Cvars.StopSpeed = this.Host.CVars.Add( "sv_stopspeed", 100f );
				this.Host.Cvars.Gravity = this.Host.CVars.Add( "sv_gravity", 800f, ClientVariableFlags.Server );
				this.Host.Cvars.MaxVelocity = this.Host.CVars.Add( "sv_maxvelocity", 2000f );
				this.Host.Cvars.NoStep = this.Host.CVars.Add( "sv_nostep", false );
				this.Host.Cvars.MaxSpeed = this.Host.CVars.Add( "sv_maxspeed", 320f, ClientVariableFlags.Server );
				this.Host.Cvars.Accelerate = this.Host.CVars.Add( "sv_accelerate", 10f );
				this.Host.Cvars.Aim = this.Host.CVars.Add( "sv_aim", 0.93f );
				this.Host.Cvars.IdealPitchScale = this.Host.CVars.Add( "sv_idealpitchscale", 0.8f );
			}

			for ( var i = 0; i < QDef.MAX_MODELS; i++ )
				this._LocalModels[i] = "*" + i.ToString();
		}

		/// <summary>
		/// SV_StartParticle
		/// Make sure the event gets sent to all clients
		/// </summary>
		public void StartParticle( ref Vector3 org, ref Vector3 dir, int color, int count )
		{
			if (this.sv.datagram.Length > QDef.MAX_DATAGRAM - 16 )
				return;

			this.sv.datagram.WriteByte( ProtocolDef.svc_particle );
			this.sv.datagram.WriteCoord( org.X );
			this.sv.datagram.WriteCoord( org.Y );
			this.sv.datagram.WriteCoord( org.Z );

			var max = Vector3.One * 127;
			var min = Vector3.One * -128;
			var v = Vector3.Clamp( dir * 16, min, max );
			this.sv.datagram.WriteChar( ( int ) v.X );
			this.sv.datagram.WriteChar( ( int ) v.Y );
			this.sv.datagram.WriteChar( ( int ) v.Z );
			this.sv.datagram.WriteByte( count );
			this.sv.datagram.WriteByte( color );
		}

		/// <summary>
		/// SV_StartSound
		/// Each entity can have eight independant sound sources, like voice,
		/// weapon, feet, etc.
		///
		/// Channel 0 is an auto-allocate channel, the others override anything
		/// allready running on that entity/channel pair.
		///
		/// An attenuation of 0 will play full volume everywhere in the level.
		/// Larger attenuations will drop off.  (max 4 attenuation)
		/// </summary>
		public void StartSound( MemoryEdict entity, int channel, string sample, int volume, float attenuation )
		{
			if ( volume < 0 || volume > 255 )
				Utilities.Error( "SV_StartSound: volume = {0}", volume );

			if ( attenuation < 0 || attenuation > 4 )
				Utilities.Error( "SV_StartSound: attenuation = {0}", attenuation );

			if ( channel < 0 || channel > 7 )
				Utilities.Error( "SV_StartSound: channel = {0}", channel );

			if (this.sv.datagram.Length > QDef.MAX_DATAGRAM - 16 )
				return;

			// find precache number for sound
			int sound_num;
			for ( sound_num = 1; sound_num < QDef.MAX_SOUNDS && this.sv.sound_precache[sound_num] != null; sound_num++ )
			{
				if ( sample == this.sv.sound_precache[sound_num] )
					break;
			}

			if ( sound_num == QDef.MAX_SOUNDS || string.IsNullOrEmpty(this.sv.sound_precache[sound_num] ) )
			{
				this.Host.Console.Print( "SV_StartSound: {0} not precacheed\n", sample );
				return;
			}

			var ent = this.NumForEdict( entity );

			channel = ( ent << 3 ) | channel;

			var field_mask = 0;
			if ( volume != snd.DEFAULT_SOUND_PACKET_VOLUME )
				field_mask |= ProtocolDef.SND_VOLUME;
			if ( attenuation != snd.DEFAULT_SOUND_PACKET_ATTENUATION )
				field_mask |= ProtocolDef.SND_ATTENUATION;

			// directed messages go only to the entity the are targeted on
			this.sv.datagram.WriteByte( ProtocolDef.svc_sound );
			this.sv.datagram.WriteByte( field_mask );
			if ( ( field_mask & ProtocolDef.SND_VOLUME ) != 0 )
				this.sv.datagram.WriteByte( volume );
			if ( ( field_mask & ProtocolDef.SND_ATTENUATION ) != 0 )
				this.sv.datagram.WriteByte( ( int ) ( attenuation * 64 ) );

			this.sv.datagram.WriteShort( channel );
			this.sv.datagram.WriteByte( sound_num );
			Vector3 v;
			MathLib.VectorAdd( ref entity.v.mins, ref entity.v.maxs, out v );
			MathLib.VectorMA( ref entity.v.origin, 0.5f, ref v, out v );
			this.sv.datagram.WriteCoord( v.X );
			this.sv.datagram.WriteCoord( v.Y );
			this.sv.datagram.WriteCoord( v.Z );
		}

		/// <summary>
		/// SV_DropClient
		/// Called when the player is getting totally kicked off the host
		/// if (crash = true), don't bother sending signofs
		/// </summary>
		public void DropClient( bool crash )
		{
			var client = this.Host.HostClient;

			if ( !crash )
			{
				// send any final messages (don't check for errors)
				if (this.Host.Network.CanSendMessage( client.netconnection ) )
				{
					var msg = client.message;
					msg.WriteByte( ProtocolDef.svc_disconnect );
					this.Host.Network.SendMessage( client.netconnection, msg );
				}

				if ( client.edict != null && client.spawned )
				{
					// call the prog function for removing a client
					// this will set the body to a dead frame, among other things
					var saveSelf = this.Host.Programs.GlobalStruct.self;
					this.Host.Programs.GlobalStruct.self = this.EdictToProg( client.edict );
					this.Host.Programs.Execute(this.Host.Programs.GlobalStruct.ClientDisconnect );
					this.Host.Programs.GlobalStruct.self = saveSelf;
				}

				this.Host.Console.DPrint( "Client {0} removed\n", client.name );
			}

			// break the net connection
			this.Host.Network.Close( client.netconnection );
			client.netconnection = null;

			// free the client (the body stays around)
			client.active = false;
			client.name = null;
			client.old_frags = -999999;
			this.Host.Network.ActiveConnections--;

			// send notification to all clients
			for ( var i = 0; i < this.svs.maxclients; i++ )
			{
				var cl = this.svs.clients[i];
				if ( !cl.active )
					continue;

				cl.message.WriteByte( ProtocolDef.svc_updatename );
				cl.message.WriteByte(this.Host.ClientNum );
				cl.message.WriteString( "" );
				cl.message.WriteByte( ProtocolDef.svc_updatefrags );
				cl.message.WriteByte(this.Host.ClientNum );
				cl.message.WriteShort( 0 );
				cl.message.WriteByte( ProtocolDef.svc_updatecolors );
				cl.message.WriteByte(this.Host.ClientNum );
				cl.message.WriteByte( 0 );
			}
		}

		/// <summary>
		/// SV_SendClientMessages
		/// </summary>
		public void SendClientMessages( )
		{
			// update frags, names, etc
			this.UpdateToReliableMessages();

			// build individual updates
			for ( var i = 0; i < this.svs.maxclients; i++ )
			{
				this.Host.HostClient = this.svs.clients[i];

				if ( !this.Host.HostClient.active )
					continue;

				if (this.Host.HostClient.spawned )
				{
					if ( !this.SendClientDatagram(this.Host.HostClient ) )
						continue;
				}
				else
				{
					// the player isn't totally in the game yet
					// send small keepalive messages if too much time has passed
					// send a full message when the next signon stage has been requested
					// some other message data (name changes, etc) may accumulate
					// between signon stages
					if ( !this.Host.HostClient.sendsignon )
					{
						if (this.Host.RealTime - this.Host.HostClient.last_message > 5 )
							this.SendNop(this.Host.HostClient );
						continue;   // don't send out non-signon messages
					}
				}

				// check for an overflowed message.  Should only happen
				// on a very fucked up connection that backs up a lot, then
				// changes level
				if (this.Host.HostClient.message.IsOveflowed )
				{
					this.DropClient( true );
					this.Host.HostClient.message.IsOveflowed = false;
					continue;
				}

				if (this.Host.HostClient.message.Length > 0 || this.Host.HostClient.dropasap )
				{
					if ( !this.Host.Network.CanSendMessage(this.Host.HostClient.netconnection ) )
						continue;

					if (this.Host.HostClient.dropasap )
						this.DropClient( false );    // went to another level
					else
					{
						if (this.Host.Network.SendMessage(this.Host.HostClient.netconnection, this.Host.HostClient.message ) == -1 )
							this.DropClient( true ); // if the message couldn't send, kick off

						this.Host.HostClient.message.Clear();
						this.Host.HostClient.last_message = this.Host.RealTime;
						this.Host.HostClient.sendsignon = false;
					}
				}
			}

			// clear muzzle flashes
			this.CleanupEnts();
		}

		/// <summary>
		/// SV_ClearDatagram
		/// </summary>
		public void ClearDatagram( )
		{
			this.sv.datagram.Clear();
		}

		/// <summary>
		/// SV_ModelIndex
		/// </summary>
		public int ModelIndex( string name )
		{
			if ( string.IsNullOrEmpty( name ) )
				return 0;

			int i;
			for ( i = 0; i < QDef.MAX_MODELS && this.sv.model_precache[i] != null; i++ )
			{
				if (this.sv.model_precache[i] == name )
					return i;
			}

			if ( i == QDef.MAX_MODELS || string.IsNullOrEmpty(this.sv.model_precache[i] ) )
				Utilities.Error( "SV_ModelIndex: model {0} not precached", name );
			return i;
		}

		/// <summary>
		/// SV_ClientPrintf
		/// Sends text across to be displayed
		/// FIXME: make this just a stuffed echo?
		/// </summary>
		public void ClientPrint( string fmt, params object[] args )
		{
			var tmp = string.Format( fmt, args );
			this.Host.HostClient.message.WriteByte( ProtocolDef.svc_print );
			this.Host.HostClient.message.WriteString( tmp );
		}

		/// <summary>
		/// SV_BroadcastPrint
		/// </summary>
		public void BroadcastPrint( string fmt, params object[] args )
		{
			var tmp = args.Length > 0 ? string.Format( fmt, args ) : fmt;
			for ( var i = 0; i < this.svs.maxclients; i++ )
			{
				if (this.svs.clients[i].active && this.svs.clients[i].spawned )
				{
					var msg = this.svs.clients[i].message;
					msg.WriteByte( ProtocolDef.svc_print );
					msg.WriteString( tmp );
				}
			}
		}

		private void WriteClientDamageMessage( MemoryEdict ent, MessageWriter msg )
		{
			if ( ent.v.dmg_take != 0 || ent.v.dmg_save != 0 )
			{
				var other = this.ProgToEdict( ent.v.dmg_inflictor );
				msg.WriteByte( ProtocolDef.svc_damage );
				msg.WriteByte( ( int ) ent.v.dmg_save );
				msg.WriteByte( ( int ) ent.v.dmg_take );
				msg.WriteCoord( other.v.origin.X + 0.5f * ( other.v.mins.X + other.v.maxs.X ) );
				msg.WriteCoord( other.v.origin.Y + 0.5f * ( other.v.mins.Y + other.v.maxs.Y ) );
				msg.WriteCoord( other.v.origin.Z + 0.5f * ( other.v.mins.Z + other.v.maxs.Z ) );

				ent.v.dmg_take = 0;
				ent.v.dmg_save = 0;
			}
		}

		private void WriteClientWeapons( MemoryEdict ent, MessageWriter msg )
		{
			if ( MainWindow.Common.GameKind == GameKind.StandardQuake )
				msg.WriteByte( ( int ) ent.v.weapon );
			else
			{
				for ( var i = 0; i < 32; i++ )
				{
					if ( ( ( int ) ent.v.weapon & ( 1 << i ) ) != 0 )
					{
						msg.WriteByte( i );
						break;
					}
				}
			}
		}

		private void WriteClientHeader( MessageWriter msg, int bits )
		{
			msg.WriteByte( ProtocolDef.svc_clientdata );
			msg.WriteShort( bits );
		}

		private void WriteClientAmmo( MemoryEdict ent, MessageWriter msg )
		{
			msg.WriteByte( ( int ) ent.v.currentammo );
			msg.WriteByte( ( int ) ent.v.ammo_shells );
			msg.WriteByte( ( int ) ent.v.ammo_nails );
			msg.WriteByte( ( int ) ent.v.ammo_rockets );
			msg.WriteByte( ( int ) ent.v.ammo_cells );
		}

		private void WriteClientFixAngle( MemoryEdict ent, MessageWriter msg )
		{
			if ( ent.v.fixangle != 0 )
			{
				msg.WriteByte( ProtocolDef.svc_setangle );
				msg.WriteAngle( ent.v.angles.X );
				msg.WriteAngle( ent.v.angles.Y );
				msg.WriteAngle( ent.v.angles.Z );
				ent.v.fixangle = 0;
			}
		}

		private void WriteClientView( MemoryEdict ent, MessageWriter msg, int bits )
		{
			if ( ( bits & ProtocolDef.SU_VIEWHEIGHT ) != 0 )
				msg.WriteChar( ( int ) ent.v.view_ofs.Z );

			if ( ( bits & ProtocolDef.SU_IDEALPITCH ) != 0 )
				msg.WriteChar( ( int ) ent.v.idealpitch );
		}

		private void WriteClientPunches( MemoryEdict ent, MessageWriter msg, int bits )
		{
			if ( ( bits & ProtocolDef.SU_PUNCH1 ) != 0 )
				msg.WriteChar( ( int ) ent.v.punchangle.X );
			if ( ( bits & ProtocolDef.SU_VELOCITY1 ) != 0 )
				msg.WriteChar( ( int ) ( ent.v.velocity.X / 16 ) );

			if ( ( bits & ProtocolDef.SU_PUNCH2 ) != 0 )
				msg.WriteChar( ( int ) ent.v.punchangle.Y );
			if ( ( bits & ProtocolDef.SU_VELOCITY2 ) != 0 )
				msg.WriteChar( ( int ) ( ent.v.velocity.Y / 16 ) );

			if ( ( bits & ProtocolDef.SU_PUNCH3 ) != 0 )
				msg.WriteChar( ( int ) ent.v.punchangle.Z );
			if ( ( bits & ProtocolDef.SU_VELOCITY3 ) != 0 )
				msg.WriteChar( ( int ) ( ent.v.velocity.Z / 16 ) );
		}

		private void WriteClientItems( MemoryEdict ent, MessageWriter msg, int items, int bits )
		{
			msg.WriteLong( items );

			if ( ( bits & ProtocolDef.SU_WEAPONFRAME ) != 0 )
				msg.WriteByte( ( int ) ent.v.weaponframe );
			if ( ( bits & ProtocolDef.SU_ARMOR ) != 0 )
				msg.WriteByte( ( int ) ent.v.armorvalue );
			if ( ( bits & ProtocolDef.SU_WEAPON ) != 0 )
				msg.WriteByte(this.ModelIndex(this.Host.Programs.GetString( ent.v.weaponmodel ) ) );
		}

		private void WriteClientHealth( MemoryEdict ent, MessageWriter msg )
		{
			msg.WriteShort( ( int ) ent.v.health );
		}

		private int GenerateClientBits( MemoryEdict ent, out int items )
		{
			var bits = 0;

			if ( ent.v.view_ofs.Z != ProtocolDef.DEFAULT_VIEWHEIGHT )
				bits |= ProtocolDef.SU_VIEWHEIGHT;

			if ( ent.v.idealpitch != 0 )
				bits |= ProtocolDef.SU_IDEALPITCH;

			// stuff the sigil bits into the high bits of items for sbar, or else
			// mix in items2
			var val = this.Host.Programs.GetEdictFieldFloat( ent, "items2", 0 );

			if ( val != 0 )
				items = ( int ) ent.v.items | ( ( int ) val << 23 );
			else
				items = ( int ) ent.v.items | ( ( int )this.Host.Programs.GlobalStruct.serverflags << 28 );

			bits |= ProtocolDef.SU_ITEMS;

			if ( ( ( int ) ent.v.flags & EdictFlags.FL_ONGROUND ) != 0 )
				bits |= ProtocolDef.SU_ONGROUND;

			if ( ent.v.waterlevel >= 2 )
				bits |= ProtocolDef.SU_INWATER;

			if ( ent.v.punchangle.X != 0 )
				bits |= ProtocolDef.SU_PUNCH1;
			if ( ent.v.punchangle.Y != 0 )
				bits |= ProtocolDef.SU_PUNCH2;
			if ( ent.v.punchangle.Z != 0 )
				bits |= ProtocolDef.SU_PUNCH3;

			if ( ent.v.velocity.X != 0 )
				bits |= ProtocolDef.SU_VELOCITY1;
			if ( ent.v.velocity.Y != 0 )
				bits |= ProtocolDef.SU_VELOCITY2;
			if ( ent.v.velocity.Z != 0 )
				bits |= ProtocolDef.SU_VELOCITY3;

			if ( ent.v.weaponframe != 0 )
				bits |= ProtocolDef.SU_WEAPONFRAME;

			if ( ent.v.armorvalue != 0 )
				bits |= ProtocolDef.SU_ARMOR;

			//	if (ent.v.weapon)
			bits |= ProtocolDef.SU_WEAPON;

			return bits;
		}

		/// <summary>
		/// SV_WriteClientdataToMessage
		/// </summary>
		public void WriteClientDataToMessage( MemoryEdict ent, MessageWriter msg )
		{
			//
			// send a damage message
			//
			this.WriteClientDamageMessage( ent, msg );

			//
			// send the current viewpos offset from the view entity
			//
			this.SetIdealPitch();        // how much to look up / down ideally

			// a fixangle might get lost in a dropped packet.  Oh well.
			this.WriteClientFixAngle( ent, msg );

			var bits = this.GenerateClientBits( ent, out var items );

			// send the data
			this.WriteClientHeader( msg, bits );
			this.WriteClientView( ent, msg, bits );
			this.WriteClientPunches( ent, msg, bits );

			// always sent
			this.WriteClientItems( ent, msg, items, bits );
			this.WriteClientHealth( ent, msg );
			this.WriteClientAmmo( ent, msg );
			this.WriteClientWeapons( ent, msg );
		}

		/// <summary>
		/// SV_CheckForNewClients
		/// </summary>
		public void CheckForNewClients( )
		{
			//
			// check for new connections
			//
			while ( true )
			{
				var ret = this.Host.Network.CheckNewConnections();
				if ( ret == null )
					break;

				//
				// init a new client structure
				//
				int i;
				for ( i = 0; i < this.svs.maxclients; i++ )
				{
					if ( !this.svs.clients[i].active )
						break;
				}

				if ( i == this.svs.maxclients )
					Utilities.Error( "Host_CheckForNewClients: no free clients" );

				this.svs.clients[i].netconnection = ret;
				this.ConnectClient( i );

				this.Host.Network.ActiveConnections++;
			}
		}

		/// <summary>
		/// SV_SaveSpawnparms
		/// Grabs the current state of each client for saving across the
		/// transition to another level
		/// </summary>
		public void SaveSpawnparms( )
		{
			this.svs.serverflags = ( int )this.Host.Programs.GlobalStruct.serverflags;

			for ( var i = 0; i < this.svs.maxclients; i++ )
			{
				this.Host.HostClient = this.svs.clients[i];
				if ( !this.Host.HostClient.active )
					continue;

				// call the progs to get default spawn parms for the new client
				this.Host.Programs.GlobalStruct.self = this.EdictToProg(this.Host.HostClient.edict );
				this.Host.Programs.Execute(this.Host.Programs.GlobalStruct.SetChangeParms );
				this.AssignGlobalSpawnparams(this.Host.HostClient );
			}
		}

		/// <summary>
		/// SV_SpawnServer
		/// </summary>
		public void SpawnServer( string server )
		{
			// let's not have any servers with no name
			if ( string.IsNullOrEmpty(this.Host.Network.HostName ) )
				this.Host.CVars.Set( "hostname", "UNNAMED" );

			this.Host.Screen.CenterTimeOff = 0;

			this.Host.Console.DPrint( "SpawnServer: {0}\n", server );
			this.svs.changelevel_issued = false;     // now safe to issue another

			//
			// tell all connected clients that we are going to a new level
			//
			if (this.sv.active )
				this.SendReconnect();

			//
			// make cvars consistant
			//
			if (this.Host.Cvars.Coop.Get<bool>() )
				this.Host.CVars.Set( "deathmatch", 0 );

			this.Host.CurrentSkill = ( int ) (this.Host.Cvars.Skill.Get<int>() + 0.5 );
			if (this.Host.CurrentSkill < 0 )
				this.Host.CurrentSkill = 0;
			if (this.Host.CurrentSkill > 3 )
				this.Host.CurrentSkill = 3;

			this.Host.CVars.Set( "skill", this.Host.CurrentSkill );

			//
			// set up the new server
			//
			this.Host.ClearMemory();

			this.sv.Clear();

			this.sv.name = server;

			// load progs to get entity field count
			this.Host.Programs.LoadProgs();

			// allocate server memory
			this.sv.max_edicts = QDef.MAX_EDICTS;

			this.sv.edicts = new MemoryEdict[this.sv.max_edicts];
			for ( var i = 0; i < this.sv.edicts.Length; i++ )
				this.sv.edicts[i] = new();

			// leave slots at start for clients only
			this.sv.num_edicts = this.svs.maxclients + 1;
			MemoryEdict ent;
			for ( var i = 0; i < this.svs.maxclients; i++ )
			{
				ent = this.EdictNum( i + 1 );
				this.svs.clients[i].edict = ent;
			}

			this.sv.state = server_state_t.Loading;
			this.sv.paused = false;
			this.sv.time = 1.0;
			this.sv.modelname = string.Format( "maps/{0}.bsp", server );
			this.sv.worldmodel = ( BrushModelData )this.Host.Model.ForName(this.sv.modelname, false, ModelType.mod_brush );
			if (this.sv.worldmodel == null )
			{
				this.Host.Console.Print( "Couldn't spawn server {0}\n", this.sv.modelname );
				this.sv.active = false;
				return;
			}

			this.sv.models[1] = this.sv.worldmodel;

			//
			// clear world interaction links
			//
			this.ClearWorld();

			this.sv.sound_precache[0] = string.Empty;
			this.sv.model_precache[0] = string.Empty;

			this.sv.model_precache[1] = this.sv.modelname;
			for ( var i = 1; i < this.sv.worldmodel.NumSubModels; i++ )
			{
				this.sv.model_precache[1 + i] = this._LocalModels[i];
				this.sv.models[i + 1] = this.Host.Model.ForName(this._LocalModels[i], false, ModelType.mod_brush );
			}

			//
			// load the rest of the entities
			//
			ent = this.EdictNum( 0 );
			ent.Clear();
			ent.v.model = this.Host.Programs.StringOffset(this.sv.worldmodel.Name );
			if ( ent.v.model == -1 )
				ent.v.model = this.Host.Programs.NewString(this.sv.worldmodel.Name );

			ent.v.modelindex = 1;       // world model
			ent.v.solid = Solids.SOLID_BSP;
			ent.v.movetype = Movetypes.MOVETYPE_PUSH;

			if (this.Host.Cvars.Coop.Get<bool>() )
				this.Host.Programs.GlobalStruct.coop = 1; //coop.value;
			else
				this.Host.Programs.GlobalStruct.deathmatch = this.Host.Cvars.Deathmatch.Get<int>();

			var offset = this.Host.Programs.NewString(this.sv.name );
			this.Host.Programs.GlobalStruct.mapname = offset;

			// serverflags are for cross level information (sigils)
			this.Host.Programs.GlobalStruct.serverflags = this.svs.serverflags;

			this.Host.Programs.LoadFromFile(this.sv.worldmodel.Entities );

			this.sv.active = true;

			// all setup is completed, any further precache statements are errors
			this.sv.state = server_state_t.Active;

			// run two frames to allow everything to settle
			this.Host.FrameTime = 0.1;
			this.Physics();
			this.Physics();

			// create a baseline for more efficient communications
			this.CreateBaseline();

			// send serverinfo to all connected clients
			for ( var i = 0; i < this.svs.maxclients; i++ )
			{
				this.Host.HostClient = this.svs.clients[i];
				if (this.Host.HostClient.active )
					this.SendServerInfo(this.Host.HostClient );
			}

			GC.Collect();
			this.Host.Console.DPrint( "Server spawned.\n" );
		}

		/// <summary>
		/// SV_CleanupEnts
		/// </summary>
		private void CleanupEnts( )
		{
			for ( var i = 1; i < this.sv.num_edicts; i++ )
			{
				var ent = this.sv.edicts[i];
				ent.v.effects = ( int ) ent.v.effects & ~EntityEffects.EF_MUZZLEFLASH;
			}
		}

		/// <summary>
		/// SV_SendNop
		/// Send a nop message without trashing or sending the accumulated client
		/// message buffer
		/// </summary>
		private void SendNop( client_t client )
		{
			var msg = new MessageWriter( 4 );
			msg.WriteChar( ProtocolDef.svc_nop );

			if (this.Host.Network.SendUnreliableMessage( client.netconnection, msg ) == -1 )
				this.DropClient( true ); // if the message couldn't send, kick off
			client.last_message = this.Host.RealTime;
		}

		/// <summary>
		/// SV_SendClientDatagram
		/// </summary>
		private bool SendClientDatagram( client_t client )
		{
			var msg = new MessageWriter( QDef.MAX_DATAGRAM ); // Uze todo: make static?

			msg.WriteByte( ProtocolDef.svc_time );
			msg.WriteFloat( ( float )this.sv.time );

			// add the client specific data to the datagram
			this.WriteClientDataToMessage( client.edict, msg );

			this.WriteEntitiesToClient( client.edict, msg );

			// copy the server datagram if there is space
			if ( msg.Length + this.sv.datagram.Length < msg.Capacity )
				msg.Write(this.sv.datagram.Data, 0, this.sv.datagram.Length );

			// send the datagram
			if (this.Host.Network.SendUnreliableMessage( client.netconnection, msg ) == -1 )
			{
				this.DropClient( true );// if the message couldn't send, kick off
				return false;
			}

			return true;
		}

		/// <summary>
		/// SV_WriteEntitiesToClient
		/// </summary>
		private void WriteEntitiesToClient( MemoryEdict clent, MessageWriter msg )
		{
			// find the client's PVS
			var org = Utilities.ToVector( ref clent.v.origin ) + Utilities.ToVector( ref clent.v.view_ofs );
			var pvs = this.FatPVS( ref org );

			// send over all entities (except the client) that touch the pvs
			for ( var e = 1; e < this.sv.num_edicts; e++ )
			{
				var ent = this.sv.edicts[e];
				// ignore if not touching a PV leaf
				if ( ent != clent ) // clent is ALLWAYS sent
				{
					// ignore ents without visible models
					var mname = this.Host.Programs.GetString( ent.v.model );
					if ( string.IsNullOrEmpty( mname ) )
						continue;

					int i;
					for ( i = 0; i < ent.num_leafs; i++ )
					{
						if ( ( pvs[ent.leafnums[i] >> 3] & ( 1 << ( ent.leafnums[i] & 7 ) ) ) != 0 )
							break;
					}

					if ( i == ent.num_leafs )
						continue;       // not visible
				}

				if ( msg.Capacity - msg.Length < 16 )
				{
					this.Host.Console.Print( "packet overflow\n" );
					return;
				}

				// send an update
				var bits = 0;
				Vector3 miss;
				MathLib.VectorSubtract( ref ent.v.origin, ref ent.baseline.origin, out miss );
				if ( miss.X < -0.1f || miss.X > 0.1f )
					bits |= ProtocolDef.U_ORIGIN1;
				if ( miss.Y < -0.1f || miss.Y > 0.1f )
					bits |= ProtocolDef.U_ORIGIN2;
				if ( miss.Z < -0.1f || miss.Z > 0.1f )
					bits |= ProtocolDef.U_ORIGIN3;

				if ( ent.v.angles.X != ent.baseline.angles.X )
					bits |= ProtocolDef.U_ANGLE1;

				if ( ent.v.angles.Y != ent.baseline.angles.Y )
					bits |= ProtocolDef.U_ANGLE2;

				if ( ent.v.angles.Z != ent.baseline.angles.Z )
					bits |= ProtocolDef.U_ANGLE3;

				if ( ent.v.movetype == Movetypes.MOVETYPE_STEP )
					bits |= ProtocolDef.U_NOLERP;   // don't mess up the step animation

				if ( ent.baseline.colormap != ent.v.colormap )
					bits |= ProtocolDef.U_COLORMAP;

				if ( ent.baseline.skin != ent.v.skin )
					bits |= ProtocolDef.U_SKIN;

				if ( ent.baseline.frame != ent.v.frame )
					bits |= ProtocolDef.U_FRAME;

				if ( ent.baseline.effects != ent.v.effects )
					bits |= ProtocolDef.U_EFFECTS;

				if ( ent.baseline.modelindex != ent.v.modelindex )
					bits |= ProtocolDef.U_MODEL;

				if ( e >= 256 )
					bits |= ProtocolDef.U_LONGENTITY;

				if ( bits >= 256 )
					bits |= ProtocolDef.U_MOREBITS;

				//
				// write the message
				//
				msg.WriteByte( bits | ProtocolDef.U_SIGNAL );

				if ( ( bits & ProtocolDef.U_MOREBITS ) != 0 )
					msg.WriteByte( bits >> 8 );
				if ( ( bits & ProtocolDef.U_LONGENTITY ) != 0 )
					msg.WriteShort( e );
				else
					msg.WriteByte( e );

				if ( ( bits & ProtocolDef.U_MODEL ) != 0 )
					msg.WriteByte( ( int ) ent.v.modelindex );
				if ( ( bits & ProtocolDef.U_FRAME ) != 0 )
					msg.WriteByte( ( int ) ent.v.frame );
				if ( ( bits & ProtocolDef.U_COLORMAP ) != 0 )
					msg.WriteByte( ( int ) ent.v.colormap );
				if ( ( bits & ProtocolDef.U_SKIN ) != 0 )
					msg.WriteByte( ( int ) ent.v.skin );
				if ( ( bits & ProtocolDef.U_EFFECTS ) != 0 )
					msg.WriteByte( ( int ) ent.v.effects );
				if ( ( bits & ProtocolDef.U_ORIGIN1 ) != 0 )
					msg.WriteCoord( ent.v.origin.X );
				if ( ( bits & ProtocolDef.U_ANGLE1 ) != 0 )
					msg.WriteAngle( ent.v.angles.X );
				if ( ( bits & ProtocolDef.U_ORIGIN2 ) != 0 )
					msg.WriteCoord( ent.v.origin.Y );
				if ( ( bits & ProtocolDef.U_ANGLE2 ) != 0 )
					msg.WriteAngle( ent.v.angles.Y );
				if ( ( bits & ProtocolDef.U_ORIGIN3 ) != 0 )
					msg.WriteCoord( ent.v.origin.Z );
				if ( ( bits & ProtocolDef.U_ANGLE3 ) != 0 )
					msg.WriteAngle( ent.v.angles.Z );
			}
		}

		/// <summary>
		/// SV_FatPVS
		/// Calculates a PVS that is the inclusive or of all leafs within 8 pixels of the
		/// given point.
		/// </summary>
		private byte[] FatPVS( ref Vector3 org )
		{
			this._FatBytes = (this.sv.worldmodel.NumLeafs + 31 ) >> 3;
			Array.Clear(this._FatPvs, 0, this._FatPvs.Length );
			this.AddToFatPVS( ref org, this.sv.worldmodel.Nodes[0] );
			return this._FatPvs;
		}

		/// <summary>
		/// SV_AddToFatPVS
		/// The PVS must include a small area around the client to allow head bobbing
		/// or other small motion on the client side.  Otherwise, a bob might cause an
		/// entity that should be visible to not show up, especially when the bob
		/// crosses a waterline.
		/// </summary>
		private void AddToFatPVS( ref Vector3 org, MemoryNodeBase node )
		{
			while ( true )
			{
				// if this is a leaf, accumulate the pvs bits
				if ( node.contents < 0 )
				{
					if ( node.contents != ( int ) Q1Contents.Solid )
					{
						var pvs = this.sv.worldmodel.LeafPVS( ( MemoryLeaf ) node );
						for ( var i = 0; i < this._FatBytes; i++ )
							this._FatPvs[i] |= pvs[i];
					}
					return;
				}

				var n = ( MemoryNode ) node;
				var plane = n.plane;
				var d = Vector3.Dot( org, plane.normal ) - plane.dist;
				if ( d > 8 )
					node = n.children[0];
				else if ( d < -8 )
					node = n.children[1];
				else
				{   // go down both
					this.AddToFatPVS( ref org, n.children[0] );
					node = n.children[1];
				}
			}
		}

		/// <summary>
		/// SV_UpdateToReliableMessages
		/// </summary>
		private void UpdateToReliableMessages( )
		{
			// check for changes to be sent over the reliable streams
			for ( var i = 0; i < this.svs.maxclients; i++ )
			{
				this.Host.HostClient = this.svs.clients[i];
				if (this.Host.HostClient.old_frags != this.Host.HostClient.edict.v.frags )
				{
					for ( var j = 0; j < this.svs.maxclients; j++ )
					{
						var client = this.svs.clients[j];
						if ( !client.active )
							continue;

						client.message.WriteByte( ProtocolDef.svc_updatefrags );
						client.message.WriteByte( i );
						client.message.WriteShort( ( int )this.Host.HostClient.edict.v.frags );
					}

					this.Host.HostClient.old_frags = ( int )this.Host.HostClient.edict.v.frags;
				}
			}

			for ( var j = 0; j < this.svs.maxclients; j++ )
			{
				var client = this.svs.clients[j];
				if ( !client.active )
					continue;
				client.message.Write(this.sv.reliable_datagram.Data, 0, this.sv.reliable_datagram.Length );
			}

			this.sv.reliable_datagram.Clear();
		}

		/// <summary>
		/// SV_ConnectClient
		/// Initializes a client_t for a new net connection.  This will only be called
		/// once for a player each game, not once for each level change.
		/// </summary>
		private void ConnectClient( int clientnum )
		{
			var client = this.svs.clients[clientnum];

			this.Host.Console.DPrint( "Client {0} connected\n", client.netconnection.address );

			var edictnum = clientnum + 1;
			var ent = this.EdictNum( edictnum );

			// set up the client_t
			var netconnection = client.netconnection;

			var spawn_parms = new float[ServerDef.NUM_SPAWN_PARMS];
			if (this.sv.loadgame )
				Array.Copy( client.spawn_parms, spawn_parms, spawn_parms.Length );

			client.Clear();
			client.netconnection = netconnection;
			client.name = "unconnected";
			client.active = true;
			client.spawned = false;
			client.edict = ent;
			client.message.AllowOverflow = true; // we can catch it
			client.privileged = false;

			if (this.sv.loadgame )
				Array.Copy( spawn_parms, client.spawn_parms, spawn_parms.Length );
			else
			{
				// call the progs to get default spawn parms for the new client
				this.Host.Programs.Execute(this.Host.Programs.GlobalStruct.SetNewParms );

				this.AssignGlobalSpawnparams( client );
			}

			this.SendServerInfo( client );
		}

		private void AssignGlobalSpawnparams( client_t client )
		{
			client.spawn_parms[0] = this.Host.Programs.GlobalStruct.parm1;
			client.spawn_parms[1] = this.Host.Programs.GlobalStruct.parm2;
			client.spawn_parms[2] = this.Host.Programs.GlobalStruct.parm3;
			client.spawn_parms[3] = this.Host.Programs.GlobalStruct.parm4;

			client.spawn_parms[4] = this.Host.Programs.GlobalStruct.parm5;
			client.spawn_parms[5] = this.Host.Programs.GlobalStruct.parm6;
			client.spawn_parms[6] = this.Host.Programs.GlobalStruct.parm7;
			client.spawn_parms[7] = this.Host.Programs.GlobalStruct.parm8;

			client.spawn_parms[8] = this.Host.Programs.GlobalStruct.parm9;
			client.spawn_parms[9] = this.Host.Programs.GlobalStruct.parm10;
			client.spawn_parms[10] = this.Host.Programs.GlobalStruct.parm11;
			client.spawn_parms[11] = this.Host.Programs.GlobalStruct.parm12;

			client.spawn_parms[12] = this.Host.Programs.GlobalStruct.parm13;
			client.spawn_parms[13] = this.Host.Programs.GlobalStruct.parm14;
			client.spawn_parms[14] = this.Host.Programs.GlobalStruct.parm15;
			client.spawn_parms[15] = this.Host.Programs.GlobalStruct.parm16;
		}

		/// <summary>
		/// SV_SendServerinfo
		/// Sends the first message from the server to a connected client.
		/// This will be sent on the initial connection and upon each server load.
		/// </summary>
		private void SendServerInfo( client_t client )
		{
			var writer = client.message;

			writer.WriteByte( ProtocolDef.svc_print );
			writer.WriteString( string.Format( "{0}\nVERSION {1,4:F2} SERVER ({2} CRC)", ( char ) 2, QDef.VERSION, this.Host.Programs.Crc ) );

			writer.WriteByte( ProtocolDef.svc_serverinfo );
			writer.WriteLong( ProtocolDef.PROTOCOL_VERSION );
			writer.WriteByte(this.svs.maxclients );

			if ( !this.Host.Cvars.Coop.Get<bool>() && this.Host.Cvars.Deathmatch.Get<int>() != 0 )
				writer.WriteByte( ProtocolDef.GAME_DEATHMATCH );
			else
				writer.WriteByte( ProtocolDef.GAME_COOP );

			var message = this.Host.Programs.GetString(this.sv.edicts[0].v.message );

			writer.WriteString( message );

			for ( var i = 1; i < this.sv.model_precache.Length; i++ )
			{
				var tmp = this.sv.model_precache[i];
				if ( string.IsNullOrEmpty( tmp ) )
					break;
				writer.WriteString( tmp );
			}
			writer.WriteByte( 0 );

			for ( var i = 1; i < this.sv.sound_precache.Length; i++ )
			{
				var tmp = this.sv.sound_precache[i];
				if ( tmp == null )
					break;
				writer.WriteString( tmp );
			}
			writer.WriteByte( 0 );

			// send music
			writer.WriteByte( ProtocolDef.svc_cdtrack );
			writer.WriteByte( ( int )this.sv.edicts[0].v.sounds );
			writer.WriteByte( ( int )this.sv.edicts[0].v.sounds );

			// set view
			writer.WriteByte( ProtocolDef.svc_setview );
			writer.WriteShort(this.NumForEdict( client.edict ) );

			writer.WriteByte( ProtocolDef.svc_signonnum );
			writer.WriteByte( 1 );

			client.sendsignon = true;
			client.spawned = false;     // need prespawn, spawn, etc
		}

		/// <summary>
		/// SV_SendReconnect
		/// Tell all the clients that the server is changing levels
		/// </summary>
		private void SendReconnect( )
		{
			var msg = new MessageWriter( 128 );

			msg.WriteChar( ProtocolDef.svc_stufftext );
			msg.WriteString( "reconnect\n" );
			this.Host.Network.SendToAll( msg, 5 );

			if (this.Host.Client.cls.state != cactive_t.ca_dedicated )
				this.Host.Commands.ExecuteString( "reconnect\n", CommandSource.Command );
		}

		/// <summary>
		/// SV_CreateBaseline
		/// </summary>
		private void CreateBaseline( )
		{
			for ( var entnum = 0; entnum < this.sv.num_edicts; entnum++ )
			{
				// get the current server version
				var svent = this.EdictNum( entnum );
				if ( svent.free )
					continue;
				if ( entnum > this.svs.maxclients && svent.v.modelindex == 0 )
					continue;

				//
				// create entity baseline
				//
				svent.baseline.origin = svent.v.origin;
				svent.baseline.angles = svent.v.angles;
				svent.baseline.frame = ( int ) svent.v.frame;
				svent.baseline.skin = ( int ) svent.v.skin;
				if ( entnum > 0 && entnum <= this.svs.maxclients )
				{
					svent.baseline.colormap = entnum;
					svent.baseline.modelindex = this.ModelIndex( "progs/player.mdl" );
				}
				else
				{
					svent.baseline.colormap = 0;
					svent.baseline.modelindex = this.ModelIndex(this.Host.Programs.GetString( svent.v.model ) );
				}

				//
				// add to the message
				//
				this.sv.signon.WriteByte( ProtocolDef.svc_spawnbaseline );
				this.sv.signon.WriteShort( entnum );

				this.sv.signon.WriteByte( svent.baseline.modelindex );
				this.sv.signon.WriteByte( svent.baseline.frame );
				this.sv.signon.WriteByte( svent.baseline.colormap );
				this.sv.signon.WriteByte( svent.baseline.skin );

				this.sv.signon.WriteCoord( svent.baseline.origin.X );
				this.sv.signon.WriteAngle( svent.baseline.angles.X );
				this.sv.signon.WriteCoord( svent.baseline.origin.Y );
				this.sv.signon.WriteAngle( svent.baseline.angles.Y );
				this.sv.signon.WriteCoord( svent.baseline.origin.Z );
				this.sv.signon.WriteAngle( svent.baseline.angles.Z );
			}
		}
	}
}
