namespace SharpQuake.Renderer
{
	using Framework;
	using Framework.Definitions;
	using Framework.Engine;
	using Framework.Mathematics;
	using Framework.Rendering.Particles;
	using System;
	using System.Numerics;
	using Textures;

	public class ParticleSystem
	{
		private int _NumParticles;

		// r_numparticles
		private Particle[] _Particles;

		private BaseTexture ParticleTexture;

		private Particle _ActiveParticles;

		// active_particles
		private Particle _FreeParticles;

		// free_particles
		private int _TracerCount;

		// tracercount from RocketTrail()
		private Vector3[] _AVelocities = new Vector3[ParticleDef.NUMVERTEXNORMALS];

		// avelocities
		private float _BeamLength = 16;

		private BaseDevice Device
		{
			get;
			set;
		}

		public ParticleSystem( BaseDevice device )
		{
			this.Device = device;
		}

		/// <summary>
		/// R_RocketTrail
		/// </summary>
		public void RocketTrail( double time, ref Vector3 start, ref Vector3 end, int type )
		{
			var vec = end - start;
			var len = MathLib.Normalize( ref vec );
			int dec;
			if ( type < 128 )
				dec = 3;
			else
			{
				dec = 1;
				type -= 128;
			}

			while ( len > 0 )
			{
				len -= dec;

				var p = this.AllocParticle( );
				if ( p == null )
					return;

				p.vel = Vector3.Zero;
				p.die = ( float ) /*Host.Client.cl.time*/ time + 2;

				switch ( type )
				{
					case 0: // rocket trail
						p.ramp = MathLib.Random( ) & 3;
						p.color = ParticleDef._Ramp3[( int ) p.ramp];
						p.type = ParticleType.Fire;
						p.org = new( start.X + ( MathLib.Random( ) % 6 - 3 ),
							start.Y + ( MathLib.Random( ) % 6 - 3 ), start.Z + ( MathLib.Random( ) % 6 - 3 ) );
						break;

					case 1: // smoke smoke
						p.ramp = ( MathLib.Random( ) & 3 ) + 2;
						p.color = ParticleDef._Ramp3[( int ) p.ramp];
						p.type = ParticleType.Fire;
						p.org = new( start.X + ( MathLib.Random( ) % 6 - 3 ),
							start.Y + ( MathLib.Random( ) % 6 - 3 ), start.Z + ( MathLib.Random( ) % 6 - 3 ) );
						break;

					case 2: // blood
						p.type = ParticleType.Gravity;
						p.color = 67 + ( MathLib.Random( ) & 3 );
						p.org = new( start.X + ( MathLib.Random( ) % 6 - 3 ),
							start.Y + ( MathLib.Random( ) % 6 - 3 ), start.Z + ( MathLib.Random( ) % 6 - 3 ) );
						break;

					case 3:
					case 5: // tracer
						p.die = ( float ) time + 0.5f;
						p.type = ParticleType.Static;
						if ( type == 3 )
							p.color = 52 + ( (this._TracerCount & 4 ) << 1 );
						else
							p.color = 230 + ( (this._TracerCount & 4 ) << 1 );

						this._TracerCount++;

						p.org = start;
						if ( (this._TracerCount & 1 ) != 0 )
						{
							p.vel.X = 30 * vec.Y; // Uze: why???
							p.vel.Y = 30 * -vec.X;
						}
						else
						{
							p.vel.X = 30 * -vec.Y;
							p.vel.Y = 30 * vec.X;
						}
						break;

					case 4: // slight blood
						p.type = ParticleType.Gravity;
						p.color = 67 + ( MathLib.Random( ) & 3 );
						p.org = new( start.X + ( MathLib.Random( ) % 6 - 3 ),
							start.Y + ( MathLib.Random( ) % 6 - 3 ), start.Z + ( MathLib.Random( ) % 6 - 3 ) );
						len -= 3;
						break;

					case 6: // voor trail
						p.color = 9 * 16 + 8 + ( MathLib.Random( ) & 3 );
						p.type = ParticleType.Static;
						p.die = ( float ) time + 0.3f;
						p.org = new( start.X + ( MathLib.Random( ) % 15 - 8 ),
							start.Y + ( MathLib.Random( ) % 15 - 8 ), start.Z + ( MathLib.Random( ) % 15 - 8 ) );
						break;
				}

				start += vec;
			}
		}

		/// <summary>
		/// R_ParticleExplosion
		/// </summary>
		public void ParticleExplosion( double time, ref Vector3 org )
		{
			for ( var i = 0; i < 1024; i++ ) // Uze: Why 1024 if MAX_PARTICLES = 2048?
			{
				var p = this.AllocParticle( );
				if ( p == null )
					return;

				p.die = ( float ) time + 5;
				p.color = ParticleDef._Ramp1[0];
				p.ramp = MathLib.Random( ) & 3;
				if ( ( i & 1 ) != 0 )
					p.type = ParticleType.Explode;
				else
					p.type = ParticleType.Explode2;
				p.org = org + new Vector3( MathLib.Random( ) % 32 - 16, MathLib.Random( ) % 32 - 16, MathLib.Random( ) % 32 - 16 );
				p.vel = new( MathLib.Random( ) % 512 - 256, MathLib.Random( ) % 512 - 256, MathLib.Random( ) % 512 - 256 );
			}
		}

		/// <summary>
		/// R_RunParticleEffect
		/// </summary>
		public void RunParticleEffect( double time, ref Vector3 org, ref Vector3 dir, int color, int count )
		{
			for ( var i = 0; i < count; i++ )
			{
				var p = this.AllocParticle( );
				if ( p == null )
					return;

				if ( count == 1024 )
				{   // rocket explosion
					p.die = ( float ) time + 5;
					p.color = ParticleDef._Ramp1[0];
					p.ramp = MathLib.Random( ) & 3;
					if ( ( i & 1 ) != 0 )
						p.type = ParticleType.Explode;
					else
						p.type = ParticleType.Explode2;
					p.org = org + new Vector3( MathLib.Random( ) % 32 - 16, MathLib.Random( ) % 32 - 16, MathLib.Random( ) % 32 - 16 );
					p.vel = new( MathLib.Random( ) % 512 - 256, MathLib.Random( ) % 512 - 256, MathLib.Random( ) % 512 - 256 );
				}
				else
				{
					p.die = ( float ) time + 0.1f * ( MathLib.Random( ) % 5 );
					p.color = ( color & ~7 ) + ( MathLib.Random( ) & 7 );
					p.type = ParticleType.SlowGravity;
					p.org = org + new Vector3( ( MathLib.Random( ) & 15 ) - 8, ( MathLib.Random( ) & 15 ) - 8, ( MathLib.Random( ) & 15 ) - 8 );
					p.vel = dir * 15.0f;
				}
			}
		}

		/// <summary>
		/// R_ParseParticleEffect
		/// Parse an effect out of the server message
		/// </summary>
		public void ParseParticleEffect( double time, MessageReader reader )
		{
			var org = reader.ReadCoords( );
			var dir = new Vector3( reader.ReadChar( ) * RenderDef.ONE_OVER_16,
				reader.ReadChar( ) * RenderDef.ONE_OVER_16,
				reader.ReadChar( ) * RenderDef.ONE_OVER_16 );
			var count = reader.ReadByte( );
			var color = reader.ReadByte( );

			if ( count == 255 )
				count = 1024;

			this.RunParticleEffect( time, ref org, ref dir, color, count );
		}

		/// <summary>
		/// R_TeleportSplash
		/// </summary>
		public void TeleportSplash( double time, ref Vector3 org )
		{
			for ( var i = -16; i < 16; i += 4 )
				for ( var j = -16; j < 16; j += 4 )
					for ( var k = -24; k < 32; k += 4 )
					{
						var p = this.AllocParticle( );
						if ( p == null )
							return;

						p.die = ( float ) ( time + 0.2 + ( MathLib.Random( ) & 7 ) * 0.02 );
						p.color = 7 + ( MathLib.Random( ) & 7 );
						p.type = ParticleType.SlowGravity;

						var dir = new Vector3( j * 8, i * 8, k * 8 );

						p.org = org + new Vector3( i + ( MathLib.Random( ) & 3 ), j + ( MathLib.Random( ) & 3 ), k + ( MathLib.Random( ) & 3 ) );

						MathLib.Normalize( ref dir );
						float vel = 50 + ( MathLib.Random( ) & 63 );
						p.vel = dir * vel;
					}
		}

		/// <summary>
		/// R_LavaSplash
		/// </summary>
		public void LavaSplash( double time, ref Vector3 org )
		{
			Vector3 dir;

			for ( var i = -16; i < 16; i++ )
				for ( var j = -16; j < 16; j++ )
					for ( var k = 0; k < 1; k++ )
					{
						var p = this.AllocParticle( );
						if ( p == null )
							return;

						p.die = ( float ) ( time + 2 + ( MathLib.Random( ) & 31 ) * 0.02 );
						p.color = 224 + ( MathLib.Random( ) & 7 );
						p.type = ParticleType.SlowGravity;

						dir.X = j * 8 + ( MathLib.Random( ) & 7 );
						dir.Y = i * 8 + ( MathLib.Random( ) & 7 );
						dir.Z = 256;

						p.org = org + dir;
						p.org.Z += MathLib.Random( ) & 63;

						MathLib.Normalize( ref dir );
						float vel = 50 + ( MathLib.Random( ) & 63 );
						p.vel = dir * vel;
					}
		}

		/// <summary>
		/// R_ParticleExplosion2
		/// </summary>
		public void ParticleExplosion( double time, ref Vector3 org, int colorStart, int colorLength )
		{
			var colorMod = 0;

			for ( var i = 0; i < 512; i++ )
			{
				var p = this.AllocParticle( );
				if ( p == null )
					return;

				p.die = ( float ) ( time + 0.3 );
				p.color = colorStart + colorMod % colorLength;
				colorMod++;

				p.type = ParticleType.Blob;
				p.org = org + new Vector3( MathLib.Random( ) % 32 - 16, MathLib.Random( ) % 32 - 16, MathLib.Random( ) % 32 - 16 );
				p.vel = new( MathLib.Random( ) % 512 - 256, MathLib.Random( ) % 512 - 256, MathLib.Random( ) % 512 - 256 );
			}
		}

		/// <summary>
		/// R_BlobExplosion
		/// </summary>
		public void BlobExplosion( double time, ref Vector3 org )
		{
			for ( var i = 0; i < 1024; i++ )
			{
				var p = this.AllocParticle( );
				if ( p == null )
					return;

				p.die = ( float ) ( time + 1 + ( MathLib.Random( ) & 8 ) * 0.05 );

				if ( ( i & 1 ) != 0 )
				{
					p.type = ParticleType.Blob;
					p.color = 66 + MathLib.Random( ) % 6;
				}
				else
				{
					p.type = ParticleType.Blob2;
					p.color = 150 + MathLib.Random( ) % 6;
				}
				p.org = org + new Vector3( MathLib.Random( ) % 32 - 16, MathLib.Random( ) % 32 - 16, MathLib.Random( ) % 32 - 16 );
				p.vel = new( MathLib.Random( ) % 512 - 256, MathLib.Random( ) % 512 - 256, MathLib.Random( ) % 512 - 256 );
			}
		}

		/// <summary>
		/// R_EntityParticles
		/// </summary>
		public void EntityParticles( double time, Vector3 entityOrigin )
		{
			float dist = 64;

			if (this._AVelocities[0].X == 0 )
			{
				for ( var i = 0; i < ParticleDef.NUMVERTEXNORMALS; i++ )
				{
					this._AVelocities[i].X = ( MathLib.Random( ) & 255 ) * 0.01f;
					this._AVelocities[i].Y = ( MathLib.Random( ) & 255 ) * 0.01f;
					this._AVelocities[i].Z = ( MathLib.Random( ) & 255 ) * 0.01f;
				}
			}

			for ( var i = 0; i < ParticleDef.NUMVERTEXNORMALS; i++ )
			{
				var angle = time * this._AVelocities[i].X;
				var sy = Math.Sin( angle );
				var cy = Math.Cos( angle );
				angle = time * this._AVelocities[i].Y;
				var sp = Math.Sin( angle );
				var cp = Math.Cos( angle );
				angle = time * this._AVelocities[i].Z;
				var sr = Math.Sin( angle );
				var cr = Math.Cos( angle );

				var forward = new Vector3( ( float ) ( cp * cy ), ( float ) ( cp * sy ), -( float ) sp );
				var p = this.AllocParticle( );
				if ( p == null )
					return;

				p.die = ( float ) ( time + 0.01 );
				p.color = 0x6f;
				p.type = ParticleType.Explode;

				p.org = entityOrigin + anorms.Values[i] * dist + forward * this._BeamLength;
			}
		}

		// R_InitParticles
		public void InitParticles( )
		{
			var i = CommandLine.CheckParm( "-particles" );
			if ( i > 0 && i < CommandLine.Argc - 1 )
			{
				this._NumParticles = int.Parse( CommandLine.Argv( i + 1 ) );
				if (this._NumParticles < ParticleDef.ABSOLUTE_MIN_PARTICLES )
					this._NumParticles = ParticleDef.ABSOLUTE_MIN_PARTICLES;
			}
			else
				this._NumParticles = ParticleDef.MAX_PARTICLES;

			this._Particles = new Particle[this._NumParticles];
			for ( i = 0; i < this._NumParticles; i++ )
				this._Particles[i] = new( );
		}

		// beamlength
		// R_InitParticleTexture
		public void InitParticleTexture( )
		{
			var data = new byte[8 * 8 * 4];
			var i = 0;

			for ( var x = 0; x < 8; x++ )
			{
				for ( var y = 0; y < 8; y++, i += 4 )
				{
					data[i] = 255;
					data[i + 1] = 255;
					data[i + 2] = 255;
					data[i + 3] = ( byte ) ( ParticleDef._DotTexture[x, y] * 255 );
				}
			}

			var uintData = new uint[data.Length / 4];
			Buffer.BlockCopy( data, 0, uintData, 0, data.Length );

			this.ParticleTexture = BaseTexture.FromBuffer(this.Device, "_Particles", uintData, 8, 8, false, true, "GL_LINEAR", "GL_MODULATE" );
		}

		// particletexture	// little dot for particles
		/// <summary>
		/// R_ClearParticles
		/// </summary>
		public void ClearParticles( )
		{
			this._FreeParticles = this._Particles[0];
			this._ActiveParticles = null;

			for ( var i = 0; i < this._NumParticles - 1; i++ )
				this._Particles[i].next = this._Particles[i + 1];

			this._Particles[this._NumParticles - 1].next = null;
		}

		/// <summary>
		/// R_DrawParticles
		/// </summary>
		public void DrawParticles( double time, double oldTime, float gravity, Vector3 origin, Vector3 viewUp, Vector3 viewRight, Vector3 viewPn )
		{
			this.Device.Graphics.BeginParticles(this.ParticleTexture );

			var up = viewUp * 1.5f;
			var right = viewRight * 1.5f;
			var frametime = ( float ) ( time - oldTime );
			var time3 = frametime * 15;
			var time2 = frametime * 10;
			var time1 = frametime * 5;
			var grav = frametime * gravity * 0.05f;
			var dvel = 4 * frametime;

			while ( true )
			{
				var kill = this._ActiveParticles;
				if ( kill != null && kill.die < time )
				{
					this._ActiveParticles = kill.next;
					kill.next = this._FreeParticles;
					this._FreeParticles = kill;
					continue;
				}
				break;
			}

			for ( var p = this._ActiveParticles; p != null; p = p.next )
			{
				while ( true )
				{
					var kill = p.next;
					if ( kill != null && kill.die < time )
					{
						p.next = kill.next;
						kill.next = this._FreeParticles;
						this._FreeParticles = kill;
						continue;
					}
					break;
				}

				// hack a scale up to keep particles from disapearing
				var scale = Vector3.Dot( p.org - origin, viewPn );
				if ( scale < 20 )
					scale = 1;
				else
					scale = 1 + scale * 0.004f;

				this.Device.Graphics.DrawParticle( p.color, up, right, p.org, scale );

				p.org += p.vel * frametime;

				switch ( p.type )
				{
					case ParticleType.Static:
						break;

					case ParticleType.Fire:
						p.ramp += time1;
						if ( p.ramp >= 6 )
							p.die = -1;
						else
							p.color = ParticleDef._Ramp3[( int ) p.ramp];
						p.vel.Z += grav;
						break;

					case ParticleType.Explode:
						p.ramp += time2;
						if ( p.ramp >= 8 )
							p.die = -1;
						else
							p.color = ParticleDef._Ramp1[( int ) p.ramp];
						p.vel += p.vel * dvel;
						p.vel.Z -= grav;
						break;

					case ParticleType.Explode2:
						p.ramp += time3;
						if ( p.ramp >= 8 )
							p.die = -1;
						else
							p.color = ParticleDef._Ramp2[( int ) p.ramp];
						p.vel -= p.vel * frametime;
						p.vel.Z -= grav;
						break;

					case ParticleType.Blob:
						p.vel += p.vel * dvel;
						p.vel.Z -= grav;
						break;

					case ParticleType.Blob2:
						p.vel -= p.vel * dvel;
						p.vel.Z -= grav;
						break;

					case ParticleType.Gravity:
					case ParticleType.SlowGravity:
						p.vel.Z -= grav;
						break;
				}
			}

			this.Device.Graphics.EndParticles( );
		}

		private Particle AllocParticle( )
		{
			if (this._FreeParticles == null )
				return null;

			var p = this._FreeParticles;
			this._FreeParticles = p.next;
			p.next = this._ActiveParticles;
			this._ActiveParticles = p;

			return p;
		}

	}
}
