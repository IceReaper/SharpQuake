namespace SharpQuake.Game.Data.Models
{
    using Framework;
    using Framework.Data;
    using Framework.Definitions.Bsp;
    using Framework.Engine;
    using Framework.IO.Sprite;
    using Framework.Rendering;
    using Framework.Rendering.Sprite;
    using Framework.World;
    using Rendering.Textures;
    using System;

    public class SpriteModelData : ModelData
    {        
        public SpriteModelData( ModelTexture noTexture ) : base( noTexture )
        {

        }

        public void Load( string name, byte[] buffer, Func<string, ByteArraySegment, int, int, int> onLoadSpriteTexture )
        {
            this.Name = name;
            this.Buffer = buffer;

            var pin = Utilities.BytesToStructure<dsprite_t>( buffer, 0 );

            var version = EndianHelper.LittleLong( pin.version );

            if ( version != ModelDef.SPRITE_VERSION )
                Utilities.Error( "{0} has wrong version number ({1} should be {2})", this.Name, version, ModelDef.SPRITE_VERSION );

            var numframes = EndianHelper.LittleLong( pin.numframes );

            var psprite = new msprite_t( );

            // Uze: sprite models are not cached so
            this.cache = new( );
            this.cache.data = psprite;

            psprite.type = ( SpriteType ) EndianHelper.LittleLong( pin.type );
            psprite.maxwidth = EndianHelper.LittleLong( pin.width );
            psprite.maxheight = EndianHelper.LittleLong( pin.height );
            psprite.beamlength = EndianHelper.LittleFloat( pin.beamlength );
            this.SyncType = ( SyncType ) EndianHelper.LittleLong( ( int ) pin.synctype );
            psprite.numframes = numframes;

            var mins = this.BoundsMin;
            var maxs = this.BoundsMax;
            mins.X = mins.Y = -psprite.maxwidth / 2;
            maxs.X = maxs.Y = psprite.maxwidth / 2;
            mins.Z = -psprite.maxheight / 2;
            maxs.Z = psprite.maxheight / 2;
            this.BoundsMin = this.BoundsMin;

            //
            // load the frames
            //
            if ( numframes < 1 )
                Utilities.Error( "Mod_LoadSpriteModel: Invalid # of frames: {0}\n", numframes );

            this.FrameCount = numframes;

            var frameOffset = dsprite_t.SizeInBytes;

            psprite.frames = new mspriteframedesc_t[numframes];

            for ( var i = 0; i < numframes; i++ )
            {
                var frametype = ( spriteframetype_t ) BitConverter.ToInt32( buffer, frameOffset );
                frameOffset += 4;

                psprite.frames[i].type = frametype;

                if ( frametype == spriteframetype_t.SPR_SINGLE )
                    frameOffset = this.LoadSpriteFrame( new( buffer, frameOffset ), out psprite.frames[i].frameptr, i, onLoadSpriteTexture );
                else
                    frameOffset = this.LoadSpriteGroup( new( buffer, frameOffset ), out psprite.frames[i].frameptr, i, onLoadSpriteTexture );
            }

            this.Type = ModelType.mod_sprite;
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns>Offset of next data block</returns>
        private int LoadSpriteFrame( ByteArraySegment pin, out object ppframe, int framenum, Func<string, ByteArraySegment, int, int, int> onLoadSpriteTexture )
        {
            var pinframe = Utilities.BytesToStructure<dspriteframe_t>( pin.Data, pin.StartIndex );

            var width = EndianHelper.LittleLong( pinframe.width );
            var height = EndianHelper.LittleLong( pinframe.height );
            var size = width * height;

            var pspriteframe = new mspriteframe_t( );

            ppframe = pspriteframe;

            pspriteframe.width = width;
            pspriteframe.height = height;
            var orgx = EndianHelper.LittleLong( pinframe.origin[0] );
            var orgy = EndianHelper.LittleLong( pinframe.origin[1] );

            pspriteframe.up = orgy;// origin[1];
            pspriteframe.down = orgy - height;
            pspriteframe.left = orgx;// origin[0];
            pspriteframe.right = width + orgx;// origin[0];

            var name = this.Name + "_" + framenum.ToString( );

            var index = onLoadSpriteTexture( name, new( pin.Data, pin.StartIndex + dspriteframe_t.SizeInBytes ), width, height );

            pspriteframe.gl_texturenum = index;

            return pin.StartIndex + dspriteframe_t.SizeInBytes + size;
        }

        /// <summary>
        /// Mod_LoadSpriteGroup
        /// </summary>
        private int LoadSpriteGroup( ByteArraySegment pin, out object ppframe, int framenum, Func<string, ByteArraySegment, int, int, int> onLoadSpriteTexture )
        {
            var pingroup = Utilities.BytesToStructure<dspritegroup_t>( pin.Data, pin.StartIndex );

            var numframes = EndianHelper.LittleLong( pingroup.numframes );
            var pspritegroup = new mspritegroup_t( );
            pspritegroup.numframes = numframes;
            pspritegroup.frames = new mspriteframe_t[numframes];
            ppframe = pspritegroup;
            var poutintervals = new float[numframes];
            pspritegroup.intervals = poutintervals;

            var offset = pin.StartIndex + dspritegroup_t.SizeInBytes;
            for ( var i = 0; i < numframes; i++, offset += dspriteinterval_t.SizeInBytes )
            {
                var interval = Utilities.BytesToStructure<dspriteinterval_t>( pin.Data, offset );
                poutintervals[i] = EndianHelper.LittleFloat( interval.interval );
                if ( poutintervals[i] <= 0 )
                    Utilities.Error( "Mod_LoadSpriteGroup: interval<=0" );
            }

            for ( var i = 0; i < numframes; i++ )
            {
                object tmp;
                offset = this.LoadSpriteFrame( new( pin.Data, offset ), out tmp, framenum * 100 + i, onLoadSpriteTexture );
                pspritegroup.frames[i] = ( mspriteframe_t ) tmp;
            }

            return offset;
        }

        public override void Clear( )
        {
            base.Clear( );
        }

        public override void CopyFrom( ModelData src )
        {
            base.CopyFrom( src );

            this.Type = ModelType.mod_sprite;
        }
    }
}
