namespace SharpQuake.Renderer.Models
{
	using Framework;
	using Framework.Engine;
	using Framework.Rendering.Alias;
	using System;
	using System.Numerics;
	using Textures;

	public class BaseAliasModel : BaseModel
	{
		public BaseAliasModelDesc AliasDesc
		{
			get;
			private set;
		}

		public BaseAliasModel( BaseDevice device, BaseAliasModelDesc desc )
			: base( device, desc )
		{
			this.AliasDesc = desc;
		}

		/// <summary>
		/// R_DrawAliasModel
		/// </summary>
		public virtual void DrawAliasModel( float shadeLight, Vector3 shadeVector, float[] shadeDots, float lightSpotZ, aliashdr_t paliashdr, double realTime, double time, ref int poseNum, ref int poseNum2, ref float frameStartTime, ref float frameInterval, ref Vector3 origin1, ref Vector3 origin2, ref float translateStartTime, ref Vector3 angles1, ref Vector3 angles2, ref float rotateStartTime, bool shadows = true, bool smoothModels = true, bool affineModels = false, bool noColours = false, bool isEyes = false, bool useInterpolation = true )
		{
			throw new NotImplementedException( );
		}

		/// <summary>
		/// GL_DrawAliasShadow
		/// </summary>
		protected virtual void DrawAliasShadow( aliashdr_t paliashdr, int posenum, float lightSpotZ, Vector3 shadeVector )
		{
			throw new NotImplementedException( );
		}

		protected virtual void DrawAliasBlendedFrame( float shadeLight, float[] shadeDots, aliashdr_t paliashdr, int posenum, int posenum2, float blend )
		{
			throw new NotImplementedException( );
		}
		/*
		=================
		R_SetupAliasBlendedFrame

		fenix@io.com: model animation interpolation
		=================
		*/
		protected virtual void SetupAliasBlendedFrame( float shadeLight, int frame, double realTime, double time, aliashdr_t paliashdr, float[] shadeDots, ref int poseNum, ref int poseNum2, ref float frameStartTime, ref float frameInterval )
		{
			if ( frame >= paliashdr.numframes || frame < 0 )
			{
				ConsoleWrapper.Print( "R_AliasSetupFrame: no such frame {0}\n", frame );
				frame = 0;
			}

			var pose = paliashdr.frames[frame].firstpose;
			var numposes = paliashdr.frames[frame].numposes;

			if ( numposes > 1 )
			{
				var interval = paliashdr.frames[frame].interval;
				pose += ( int ) ( time / interval ) % numposes;
				frameInterval = interval;
			}
			else
			{
				/* One tenth of a second is a good for most Quake animations.
				If the nextthink is longer then the animation is usually meant to pause
				( e.g.check out the shambler magic animation in shambler.qc).  If its
				shorter then things will still be smoothed partly, and the jumps will be
				less noticable because of the shorter time.So, this is probably a good
				assumption. */
				frameInterval = 0.1f;
			}

			var blend = 0f;

			var e = paliashdr.frames[frame];

			if ( poseNum2 != pose )
			{
				frameStartTime = ( float ) realTime;
				poseNum = poseNum2;
				poseNum2 = pose;
				blend = 0;
			}
			else
				blend = ( float ) ( ( realTime - frameStartTime ) / frameInterval );

			// wierd things start happening if blend passes 1
			if ( /*cl.paused || */ blend > 1 )
				blend = 1;

			this.DrawAliasBlendedFrame( shadeLight, shadeDots, paliashdr, poseNum, poseNum2, blend );
		}

		/// <summary>
		/// R_SetupAliasFrame
		/// </summary>
		protected virtual void SetupAliasFrame( float shadeLight, int frame, double time, aliashdr_t paliashdr, float[] shadeDots )
		{
			if ( frame >= paliashdr.numframes || frame < 0 )
			{
				ConsoleWrapper.Print( "R_AliasSetupFrame: no such frame {0}\n", frame );
				frame = 0;
			}

			var pose = paliashdr.frames[frame].firstpose;
			var numposes = paliashdr.frames[frame].numposes;

			if ( numposes > 1 )
			{
				var interval = paliashdr.frames[frame].interval;
				pose += ( int ) ( time / interval ) % numposes;
			}

			this.DrawAliasFrame( shadeLight, shadeDots, paliashdr, pose );
		}

		/// <summary>
		/// GL_DrawAliasFrame
		/// </summary>
		protected virtual void DrawAliasFrame( float shadeLight, float[] shadeDots, aliashdr_t paliashdr, int posenum )
		{
			throw new NotImplementedException( );
		}

		public static BaseAliasModel Create( BaseDevice device, string identifier, BaseTexture texture )
		{
			return ( BaseAliasModel ) BaseModel.Create( device, identifier, texture, device.AliasModelType, device.AliasModelDescType );
		}
	}
}
