namespace SharpQuake.Game.Data.Models
{
    using Framework;
    using Framework.Data;
    using Framework.Definitions.Bsp;
    using Framework.Engine;
    using Framework.IO.Alias;
    using Framework.Rendering;
    using Framework.Rendering.Alias;
    using Framework.World;
    using Rendering.Textures;
    using System;
    using System.Linq;
    using System.Numerics;

    public class AliasModelData : ModelData
    {
        public aliashdr_t Header
        {
            get;
            private set;
        }

        private int PoseNum
        {
            get;
            set;
        }

        public trivertx_t[][] PoseVerts => this._PoseVerts;

        public stvert_t[] STVerts => this._STVerts;

        public dtriangle_t[] Triangles => this._Triangles;

        private stvert_t[] _STVerts = new stvert_t[ModelDef.MAXALIASVERTS]; // stverts
        private dtriangle_t[] _Triangles = new dtriangle_t[ModelDef.MAXALIASTRIS]; // triangles
        private trivertx_t[][] _PoseVerts = new trivertx_t[ModelDef.MAXALIASFRAMES][]; // poseverts

        public AliasModelData( ModelTexture noTexture ) : base( noTexture )
        {

        }

        public void Load( uint[] table8to24, string name, byte[] buffer, Func<string, ByteArraySegment, aliashdr_t, int> onLoadSkinTexture, Action<AliasModelData, aliashdr_t> onMakeAliasModelDisplayList )
        {
            this.Name = name;
            this.Buffer = buffer;

            var pinmodel = Utilities.BytesToStructure<mdl_t>(this.Buffer, 0 );

            var version = EndianHelper.LittleLong( pinmodel.version );

            if ( version != ModelDef.ALIAS_VERSION )
                Utilities.Error( "{0} has wrong version number ({1} should be {2})", this.Name, version, ModelDef.ALIAS_VERSION );

            //
            // allocate space for a working header, plus all the data except the frames,
            // skin and group info
            //
            this.Header = new( );

            this.Flags = ( EntityFlags ) EndianHelper.LittleLong( pinmodel.flags );

            //
            // endian-adjust and copy the data, starting with the alias model header
            //
            this.Header.boundingradius = EndianHelper.LittleFloat( pinmodel.boundingradius );
            this.Header.numskins = EndianHelper.LittleLong( pinmodel.numskins );
            this.Header.skinwidth = EndianHelper.LittleLong( pinmodel.skinwidth );
            this.Header.skinheight = EndianHelper.LittleLong( pinmodel.skinheight );

            if (this.Header.skinheight > ModelDef.MAX_LBM_HEIGHT )
                Utilities.Error( "model {0} has a skin taller than {1}", this.Name, ModelDef.MAX_LBM_HEIGHT );

            this.Header.numverts = EndianHelper.LittleLong( pinmodel.numverts );

            if (this.Header.numverts <= 0 )
                Utilities.Error( "model {0} has no vertices", this.Name );

            if (this.Header.numverts > ModelDef.MAXALIASVERTS )
                Utilities.Error( "model {0} has too many vertices", this.Name );

            this.Header.numtris = EndianHelper.LittleLong( pinmodel.numtris );

            if (this.Header.numtris <= 0 )
                Utilities.Error( "model {0} has no triangles", this.Name );

            this.Header.numframes = EndianHelper.LittleLong( pinmodel.numframes );
            var numframes = this.Header.numframes;
            if ( numframes < 1 )
                Utilities.Error( "Mod_LoadAliasModel: Invalid # of frames: {0}\n", numframes );

            this.Header.size = EndianHelper.LittleFloat( pinmodel.size ) * ModelDef.ALIAS_BASE_SIZE_RATIO;
            this.SyncType = ( SyncType ) EndianHelper.LittleLong( ( int ) pinmodel.synctype );
            this.FrameCount = this.Header.numframes;

            this.Header.scale = EndianHelper.LittleVector( Utilities.ToVector( ref pinmodel.scale ) );
            this.Header.scale_origin = EndianHelper.LittleVector( Utilities.ToVector( ref pinmodel.scale_origin ) );
            this.Header.eyeposition = EndianHelper.LittleVector( Utilities.ToVector( ref pinmodel.eyeposition ) );

            //
            // load the skins
            //
            var offset = this.LoadAllSkins( table8to24, this.Header.numskins, new( buffer, mdl_t.SizeInBytes ), onLoadSkinTexture );

            //
            // load base s and t vertices
            //
            var stvOffset = offset; // in bytes
            for ( var i = 0; i < this.Header.numverts; i++, offset += stvert_t.SizeInBytes )
            {
                this._STVerts[i] = Utilities.BytesToStructure<stvert_t>( buffer, offset );

                this._STVerts[i].onseam = EndianHelper.LittleLong(this._STVerts[i].onseam );
                this._STVerts[i].s = EndianHelper.LittleLong(this._STVerts[i].s );
                this._STVerts[i].t = EndianHelper.LittleLong(this._STVerts[i].t );
            }

            //
            // load triangle lists
            //
            var triOffset = stvOffset + this.Header.numverts * stvert_t.SizeInBytes;
            offset = triOffset;
            for ( var i = 0; i < this.Header.numtris; i++, offset += dtriangle_t.SizeInBytes )
            {
                this._Triangles[i] = Utilities.BytesToStructure<dtriangle_t>( buffer, offset );
                this._Triangles[i].facesfront = EndianHelper.LittleLong(this._Triangles[i].facesfront );

                for ( var j = 0; j < 3; j++ )
                    this._Triangles[i].vertindex[j] = EndianHelper.LittleLong(this._Triangles[i].vertindex[j] );
            }

            //
            // load the frames
            //
            this.PoseNum = 0;
            var framesOffset = triOffset + this.Header.numtris * dtriangle_t.SizeInBytes;

            this.Header.frames = new maliasframedesc_t[this.Header.numframes];

            for ( var i = 0; i < numframes; i++ )
            {
                var frametype = ( aliasframetype_t ) BitConverter.ToInt32( buffer, framesOffset );
                framesOffset += 4;

                if ( frametype == aliasframetype_t.ALIAS_SINGLE )
                    framesOffset = this.LoadAliasFrame( new( buffer, framesOffset ), ref this.Header.frames[i] );
                else
                    framesOffset = this.LoadAliasGroup( new( buffer, framesOffset ), ref this.Header.frames[i] );
            }

            this.Header.numposes = this.PoseNum;

            this.Type = ModelType.mod_alias;

            // FIXME: do this right
            this.BoundsMin = -Vector3.One * 16.0f;
            this.BoundsMax = -this.BoundsMin;

            //
            // build the draw lists
            //
            onMakeAliasModelDisplayList( this, this.Header );
            //mesh.MakeAliasModelDisplayLists( mod, Header );

            //
            // move the complete, relocatable alias model to the cache
            //
            //cache = Host.Cache.Alloc( aliashdr_t.SizeInBytes * Header.frames.Length * maliasframedesc_t.SizeInBytes, null );

            //if ( cache == null )
            //    return;

            //cache.data = Header;
        }

        /// <summary>
        /// Mod_LoadAllSkins
        /// </summary>
        /// <returns>Offset of next data block in source byte array</returns>
        private int LoadAllSkins( uint[] table8to24, int numskins, ByteArraySegment data, Func<string, ByteArraySegment, aliashdr_t, int> onLoadSkinTexture )
        {
            if ( numskins < 1 || numskins > ModelDef.MAX_SKINS )
                Utilities.Error( "Mod_LoadAliasModel: Invalid # of skins: {0}\n", numskins );

            var offset = data.StartIndex;
            var skinOffset = data.StartIndex + daliasskintype_t.SizeInBytes; //  skin = (byte*)(pskintype + 1);
            var s = this.Header.skinwidth * this.Header.skinheight;

            var pskintype = Utilities.BytesToStructure<daliasskintype_t>( data.Data, offset );

            for ( var i = 0; i < numskins; i++ )
            {
                if ( pskintype.type == aliasskintype_t.ALIAS_SKIN_SINGLE )
                {
                    this.FloodFillSkin( table8to24, new( data.Data, skinOffset ), this.Header.skinwidth, this.Header.skinheight );

                    // save 8 bit texels for the player model to remap
                    var texels = new byte[s]; // Hunk_AllocName(s, loadname);
                    this.Header.texels[i] = texels;// -(byte*)pheader;
                    System.Buffer.BlockCopy( data.Data, offset + daliasskintype_t.SizeInBytes, texels, 0, s );

                    // set offset to pixel data after daliasskintype_t block...
                    offset += daliasskintype_t.SizeInBytes;

                    var name = this.Name + "_" + i.ToString( );

                    var index = onLoadSkinTexture( name, new( data.Data, offset ), this.Header );

                    this.Header.gl_texturenum[i, 0] = this.Header.gl_texturenum[i, 1] = this.Header.gl_texturenum[i, 2] = this.Header.gl_texturenum[i, 3] = index;
                    // Host.DrawingContext.LoadTexture( name, Header.skinwidth,
                    //Header.skinheight, new ByteArraySegment( data.Data, offset ), true, false ); // (byte*)(pskintype + 1)

                    // set offset to next daliasskintype_t block...
                    offset += s;
                    pskintype = Utilities.BytesToStructure<daliasskintype_t>( data.Data, offset );
                }
                else
                {
                    // animating skin group.  yuck.
                    offset += daliasskintype_t.SizeInBytes;
                    var pinskingroup = Utilities.BytesToStructure<daliasskingroup_t>( data.Data, offset );
                    var groupskins = EndianHelper.LittleLong( pinskingroup.numskins );
                    offset += daliasskingroup_t.SizeInBytes;
                    var pinskinintervals = Utilities.BytesToStructure<daliasskininterval_t>( data.Data, offset );

                    offset += daliasskininterval_t.SizeInBytes * groupskins;

                    pskintype = Utilities.BytesToStructure<daliasskintype_t>( data.Data, offset );
                    int j;
                    for ( j = 0; j < groupskins; j++ )
                    {
                        this.FloodFillSkin( table8to24, new( data.Data, skinOffset ), this.Header.skinwidth, this.Header.skinheight );
                        if ( j == 0 )
                        {
                            var texels = new byte[s]; // Hunk_AllocName(s, loadname);
                            this.Header.texels[i] = texels;// -(byte*)pheader;
                            System.Buffer.BlockCopy( data.Data, offset, texels, 0, s );
                        }

                        var name = string.Format( "{0}_{1}_{2}", this.Name, i, j );

                        var index = onLoadSkinTexture( name, new( data.Data, offset ), this.Header );

                        this.Header.gl_texturenum[i, j & 3] = index;// //  (byte*)(pskintype)

                        offset += s;

                        pskintype = Utilities.BytesToStructure<daliasskintype_t>( data.Data, offset );
                    }
                    var k = j;
                    for ( ; j < 4; j++ )
                        this.Header.gl_texturenum[i, j & 3] = this.Header.gl_texturenum[i, j - k];
                }
            }

            return offset;// (void*)pskintype;
        }

        /// <summary>
        /// Mod_LoadAliasFrame
        /// </summary>
        /// <returns>Offset of next data block in source byte array</returns>
        private int LoadAliasFrame( ByteArraySegment pin, ref maliasframedesc_t frame )
        {
            var pdaliasframe = Utilities.BytesToStructure<daliasframe_t>( pin.Data, pin.StartIndex );

            frame.name = Utilities.GetString( pdaliasframe.name );
            frame.firstpose = this.PoseNum;
            frame.numposes = 1;
            frame.bboxmin.Init( );
            frame.bboxmax.Init( );

            for ( var i = 0; i < 3; i++ )
            {
                // these are byte values, so we don't have to worry about
                // endianness
                frame.bboxmin.v[i] = pdaliasframe.bboxmin.v[i];
                frame.bboxmax.v[i] = pdaliasframe.bboxmax.v[i];
            }

            var verts = new trivertx_t[this.Header.numverts];
            var offset = pin.StartIndex + daliasframe_t.SizeInBytes; //pinframe = (trivertx_t*)(pdaliasframe + 1);
            for ( var i = 0; i < verts.Length; i++, offset += trivertx_t.SizeInBytes )
                verts[i] = Utilities.BytesToStructure<trivertx_t>( pin.Data, offset );

            this._PoseVerts[this.PoseNum] = verts;
            this.PoseNum++;

            return offset;
        }

        /// <summary>
        /// Mod_LoadAliasGroup
        /// </summary>
        /// <returns>Offset of next data block in source byte array</returns>
        private int LoadAliasGroup( ByteArraySegment pin, ref maliasframedesc_t frame )
        {
            var offset = pin.StartIndex;
            var pingroup = Utilities.BytesToStructure<daliasgroup_t>( pin.Data, offset );
            var numframes = EndianHelper.LittleLong( pingroup.numframes );

            frame.Init( );
            frame.firstpose = this.PoseNum;
            frame.numposes = numframes;

            for ( var i = 0; i < 3; i++ )
            {
                // these are byte values, so we don't have to worry about endianness
                frame.bboxmin.v[i] = pingroup.bboxmin.v[i];
                frame.bboxmin.v[i] = pingroup.bboxmax.v[i];
            }

            offset += daliasgroup_t.SizeInBytes;
            var pin_intervals = Utilities.BytesToStructure<daliasinterval_t>( pin.Data, offset ); // (daliasinterval_t*)(pingroup + 1);

            frame.interval = EndianHelper.LittleFloat( pin_intervals.interval );

            offset += numframes * daliasinterval_t.SizeInBytes;

            for ( var i = 0; i < numframes; i++ )
            {
                var tris = new trivertx_t[this.Header.numverts];
                var offset1 = offset + daliasframe_t.SizeInBytes;
                for ( var j = 0; j < this.Header.numverts; j++, offset1 += trivertx_t.SizeInBytes )
                    tris[j] = Utilities.BytesToStructure<trivertx_t>( pin.Data, offset1 );

                this._PoseVerts[this.PoseNum] = tris;
                this.PoseNum++;

                offset += daliasframe_t.SizeInBytes + this.Header.numverts * trivertx_t.SizeInBytes;
            }

            return offset;
        }

        /// <summary>
        /// Mod_FloodFillSkin
        /// Fill background pixels so mipmapping doesn't have haloes - Ed
        /// </summary>
        private void FloodFillSkin( uint[] table8To24, ByteArraySegment skin, int skinwidth, int skinheight )
        {
            var filler = new FloodFiller( skin, skinwidth, skinheight );
            filler.Perform( table8To24 );
        }

        public override void Clear( )
        {
            base.Clear( );

            this.Header = null;
            this.PoseNum = 0;
            this._PoseVerts = null;
            this._STVerts = null;
            this._Triangles = null;
        }

        public override void CopyFrom( ModelData src )
        {
            base.CopyFrom( src );

            this.Type = ModelType.mod_alias;

            if ( ! ( src is AliasModelData ) )
                return;
            
            var aliasSrc = ( AliasModelData ) src;

            this.Header = aliasSrc.Header;
            this.PoseNum = aliasSrc.PoseNum;
            this._PoseVerts = aliasSrc.PoseVerts.ToArray( );
            this._STVerts = aliasSrc.STVerts.ToArray( );
            this._Triangles = aliasSrc.Triangles.ToArray( );
        }
    }
}
