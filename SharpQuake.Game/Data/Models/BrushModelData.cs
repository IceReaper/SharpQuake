namespace SharpQuake.Game.Data.Models
{
    using Framework;
    using Framework.Data;
    using Framework.Definitions.Bsp;
    using Framework.Engine;
    using Framework.IO.BSP;
    using Framework.IO.BSP.Q1;
    using Framework.IO.BSP.Q2;
    using Framework.IO.BSP.Q3;
    using Framework.IO.Wad;
    using Framework.Mathematics;
    using Framework.Rendering;
    using Rendering.Memory;
    using Rendering.Textures;
    using System;
    using System.Drawing;
    using System.Linq;
    using System.Numerics;
    using System.Text;
    using Plane = Framework.Mathematics.Plane;

    public class BrushModelData : ModelData
    {
        private int Version
        {
            get;
            set;
        }

        private int BaseOffset
        {
            get;
            set;
        }

        private Q1Header Q1Header
        {
            get;
            set;
        }

        private Q2Header Q2Header
        {
            get;
            set;
        }

        private Q3Header Q3Header
        {
            get;
            set;
        }

        //
        // brush model
        //
        public int FirstModelSurface
        {
            get;
            set;
        }
        
        public int NumModelSurfaces
        {
            get;
            set;
        }

        public Q1Model[] SubModels
        {
            get;
            set;
        }

        public int NumSubModels
        {
            get;
            set;
        }

        public Plane[] Planes // mplane_t*
        {
            get;
            set;
        }

        public int NumPlanes
        {
            get;
            set;
        }

        public int NumLeafs      // number of visible leafs, not counting 0
        {
            get;
            set;
        }

        public MemoryLeaf[] Leaves // mleaf_t*
        {
            get;
            set;
        }

        public int NumVertices
        {
            get;
            set;
        }

        public MemoryVertex[] Vertices // mvertex_t*
        {
            get;
            set;
        }

        public int NumEdges
        {
            get;
            set;
        }

        public MemoryEdge[] Edges // medge_t*
        {
            get;
            set;
        }

        public int NumNodes
        {
            get;
            set;
        }

        public MemoryNode[] Nodes // mnode_t *nodes;
        {
            get;
            set;
        }

        public int NumTexInfo
        {
            get;
            set;
        }

        public MemoryTextureInfo[] TexInfo
        {
            get;
            set;
        }

        public int NumSurfaces
        {
            get;
            set;
        }

        public MemorySurface[] Surfaces
        {
            get;
            set;
        }

        public int NumSurfEdges
        {
            get;
            set;
        }

        public int[] SurfEdges // int *surfedges;
        {
            get;
            set;
        }

        public int NumClipNodes
        {
            get;
            set;
        }

        public BspClipNode[] ClipNodes // public dclipnode_t* clipnodes;
        {
            get;
            set;
        }

        public int NumMarkSurfaces
        {
            get;
            set;
        }

        public MemorySurface[] MarkSurfaces // msurface_t **marksurfaces;
        {
            get;
            set;
        }

        public BspHull[] Hulls // [MAX_MAP_HULLS];
        {
            get;
            set;
        }

        public int NumTextures
        {
            get;
            set;
        }

        public ModelTexture[] Textures // texture_t	**textures;
        {
            get;
            set;
        }

        public byte[] VisData // byte *visdata;
        {
            get;
            set;
        }

        public byte[] LightData // byte		*lightdata;
        {
            get;
            set;
        }

        public string Entities // char		*entities
        {
            get;
            set;
        }

        private MemorySurface WarpFace
        {
            get;
            set;
        }

        private float SubdivideSize
        {
            get;
            set;
        }

        private byte[] _NoVis = new byte[BspDef.MAX_MAP_LEAFS / 8]; // byte mod_novis[MAX_MAP_LEAFS/8]
        private byte[] _Decompressed = new byte[BspDef.MAX_MAP_LEAFS / 8]; // static byte decompressed[] from Mod_DecompressVis()
        
        public BrushModelData( float subdivideSize, ModelTexture noTexture ) : base( noTexture )
        {
            this.Type = ModelType.mod_brush;

            this.SubdivideSize = subdivideSize;

            this.Hulls = new BspHull[BspDef.MAX_MAP_HULLS];

            for ( var i = 0; i < this.Hulls.Length; i++ )
                this.Hulls[i] = new( );

            Utilities.FillArray(this._NoVis, ( byte ) 0xff );
        }

        public override void Clear( )
        {
            base.Clear( );

            this.FirstModelSurface = 0;
            this.NumModelSurfaces = 0;

            this.NumSubModels = 0;
            this.SubModels = null;

            this.NumPlanes = 0;
            this.Planes = null;

            this.NumLeafs = 0;
            this.Leaves = null;

            this.NumVertices = 0;
            this.Vertices = null;

            this.NumEdges = 0;
            this.Edges = null;

            this.NumNodes = 0;
            this.Nodes = null;

            this.NumTexInfo = 0;
            this.TexInfo = null;

            this.NumSurfaces = 0;
            this.Surfaces = null;

            this.NumSurfEdges = 0;
            this.SurfEdges = null;

            this.NumClipNodes = 0;
            this.ClipNodes = null;

            this.NumMarkSurfaces = 0;
            this.MarkSurfaces = null;

            foreach ( var h in this.Hulls )
                h.Clear( );

            this.NumTextures = 0;
            this.Textures = null;

            this.VisData = null;
            this.LightData = null;
            this.Entities = null;
        }

        public override void CopyFrom( ModelData src )
        {
            base.CopyFrom( src );

            this.Type = ModelType.mod_brush;

            if ( !( src is BrushModelData ) )
                return;

            var brushSrc = ( BrushModelData ) src;

            this.FirstModelSurface = brushSrc.FirstModelSurface;
            this.NumModelSurfaces = brushSrc.NumModelSurfaces;

            this.NumSubModels = brushSrc.NumSubModels;
            this.SubModels = brushSrc.SubModels;

            this.NumPlanes = brushSrc.NumPlanes;
            this.Planes = brushSrc.Planes;

            this.NumLeafs = brushSrc.NumLeafs;
            this.Leaves = brushSrc.Leaves;

            this.NumVertices = brushSrc.NumVertices;
            this.Vertices = brushSrc.Vertices;

            this.NumEdges = brushSrc.NumEdges;
            this.Edges = brushSrc.Edges;

            this.NumNodes = brushSrc.NumNodes;
            this.Nodes = brushSrc.Nodes;

            this.NumTexInfo = brushSrc.NumTexInfo;
            this.TexInfo = brushSrc.TexInfo;

            this.NumSurfaces = brushSrc.NumSurfaces;
            this.Surfaces = brushSrc.Surfaces;

            this.NumSurfEdges = brushSrc.NumSurfEdges;
            this.SurfEdges = brushSrc.SurfEdges;

            this.NumClipNodes = brushSrc.NumClipNodes;
            this.ClipNodes = brushSrc.ClipNodes;

            this.NumMarkSurfaces = brushSrc.NumMarkSurfaces;
            this.MarkSurfaces = brushSrc.MarkSurfaces;

            for ( var i = 0; i < brushSrc.Hulls.Length; i++ )
                this.Hulls[i].CopyFrom( brushSrc.Hulls[i] );

            this.NumTextures = brushSrc.NumTextures;
            this.Textures = brushSrc.Textures;

            this.VisData = brushSrc.VisData;
            this.LightData = brushSrc.LightData;
            this.Entities = brushSrc.Entities;
        }

        public void Load( string name, byte[] buffer, Action<ModelTexture> onCheckInitSkyTexture, Func<string, Tuple<byte[], Size, byte[]>> onCheckForTexture )
        {
            this.Name = name;
            this.Buffer = buffer;

            this.LoadHeader( );
            this.SwapLumps( );

            // load into heap
            if (this.Version == BspDef.Q1_BSPVERSION || this.Version == BspDef.HL_BSPVERSION )
            {
                var lumps = this.Q1Header.lumps;
                this.LoadVertices( ref lumps[( int ) Q1Lumps.Vertices] );
                this.LoadEdges( ref lumps[( int ) Q1Lumps.Edges] );
                this.LoadSurfEdges( ref lumps[( int ) Q1Lumps.SurfaceEdges] );
                this.LoadTextures( ref lumps[( int ) Q1Lumps.Textures], onCheckInitSkyTexture, onCheckForTexture );
                this.LoadLighting( ref lumps[( int ) Q1Lumps.Lighting] );
                this.LoadPlanes( ref lumps[( int ) Q1Lumps.Planes] );
                this.LoadTexInfo( ref lumps[( int ) Q1Lumps.TextureInfo] );
                this.LoadFaces( ref lumps[( int ) Q1Lumps.Faces] );
                this.LoadMarkSurfaces( ref lumps[( int ) Q1Lumps.MarkSurfaces] );
                this.LoadVisibility( ref lumps[( int ) Q1Lumps.Visibility] );
                this.LoadLeafs( ref lumps[( int ) Q1Lumps.Leaves] );
                this.LoadNodes( ref lumps[( int ) Q1Lumps.Nodes] );
                this.LoadClipNodes( ref lumps[( int ) Q1Lumps.ClipNodes] );
                this.LoadEntities( ref lumps[( int ) Q1Lumps.Entities] );
                this.LoadSubModels( ref lumps[( int ) Q1Lumps.Models] );
                this.MakeHull0( );
            }
            else if (this.Version == BspDef.Q2_BSPVERSION )
            {
                var lumps = this.Q2Header.lumps;
                this.LoadEntities( ref lumps[( int ) Q2Lumps.Entities] );
                this.LoadPlanes( ref lumps[( int ) Q2Lumps.Planes] );
                this.LoadVertices( ref lumps[( int ) Q2Lumps.Vertices] );
                this.LoadVisibility( ref lumps[( int ) Q2Lumps.Visibility] );
                this.LoadNodes( ref lumps[( int ) Q2Lumps.Nodes] );
                this.LoadTexInfo( ref lumps[( int ) Q2Lumps.TextureInfo] );
                this.LoadFaces( ref lumps[( int ) Q2Lumps.Faces] );
                this.LoadLighting( ref lumps[( int ) Q2Lumps.Lighting] );
                this.LoadLeafs( ref lumps[( int ) Q2Lumps.Leaves] );
                // LeafFaces
                // LeafBrushes
                this.LoadEdges( ref lumps[( int ) Q2Lumps.Edges] );
                this.LoadSurfEdges( ref lumps[( int ) Q2Lumps.SurfaceEdges] );
                this.LoadSubModels( ref lumps[( int ) Q2Lumps.Models] );
                // Brushes
                // BrushSides
                // Pop
                // Areas
                // AreaPortals
                this.MakeHull0( );
            }
            else if (this.Version == BspDef.Q3_BSPVERSION )
            {
                this.BaseOffset += Q3Header.SizeInBytes;

                var lumps = this.Q3Header.lumps;
                this.LoadEntities( ref lumps[( int ) Q3Lumps.Entities] );
                this.LoadTextures( ref lumps[( int ) Q3Lumps.Textures], onCheckInitSkyTexture, onCheckForTexture );
                //LoadPlanes( ref lumps[( Int32 ) Q3Lumps.Planes] );
               // LoadNodes( ref lumps[( Int32 ) Q3Lumps.Nodes] );
                //LoadLeafs( ref lumps[( Int32 ) Q3Lumps.Leaves] );
                // LeafFaces
                // LeafBrushes
                //LoadSubModels( ref lumps[( Int32 ) Q3Lumps.Models] );
                // Brushes
                // BrushSides
                //LoadVertices( ref lumps[( Int32 ) Q3Lumps.Vertices] );
                // Triangles
                // Effects
                //LoadFaces( ref lumps[( Int32 ) Q3Lumps.Faces] );
                // LightMaps
                // LightGrid
                // PVS
               // MakeHull0( );
            }

            this.FrameCount = 2;	// regular and alternate animation
        }

        private void LoadHeader( )
        {
            var v = BitConverter.ToInt32(this.Buffer.ToList( ).GetRange( 0, 4 ).ToArray( ), 0 );
            var bspVersion = EndianHelper.LittleLong( v );

            if ( v < 0 || v > 1000 ) // Hack for detecting quake 3
            {
                v = BitConverter.ToInt32(this.Buffer.ToList( ).GetRange( 4, 4 ).ToArray( ), 0 );
                bspVersion = EndianHelper.LittleLong( v );
            }

            if ( !BspDef.SUPPORTED_BSPS.Contains( bspVersion ) )
            {
                Utilities.Error( $"Mod_LoadBrushModel: {this.Name} has wrong version number ({bspVersion})" );
                return;
            }

            if ( bspVersion == BspDef.Q1_BSPVERSION || bspVersion == BspDef.HL_BSPVERSION )
            {
                var header = Utilities.BytesToStructure<Q1Header>(this.Buffer, 0 );
                header.version = EndianHelper.LittleLong( header.version );
                this.Q1Header = header;
            }
            else if ( bspVersion == BspDef.Q2_BSPVERSION )
            {
                var header = Utilities.BytesToStructure<Q2Header>(this.Buffer, 0 );
                header.version = EndianHelper.LittleLong( header.version );
                this.Q2Header = header;
            }
            else if ( bspVersion == BspDef.Q3_BSPVERSION )
            {
                var header = Utilities.BytesToStructure<Q3Header>(this.Buffer, 0 );
                header.version = EndianHelper.LittleLong( header.version );
                this.Q3Header = header;
            }

            this.Version = bspVersion;
        }

        private void SwapLumps( )
        {
            BspLump[] lumps = null;

            switch (this.Version )
            {
                case BspDef.HL_BSPVERSION:
                case BspDef.Q1_BSPVERSION:
                    lumps = this.Q1Header.lumps;
                    break;

                case BspDef.Q2_BSPVERSION:
                    lumps = this.Q2Header.lumps;
                    break;

                case BspDef.Q3_BSPVERSION:
                    lumps = this.Q3Header.lumps;
                    break;
            }

            if ( lumps == null )
                return;

            for ( var i = 0; i < lumps.Length; i++ )
            {
                lumps[i].Length = EndianHelper.LittleLong( lumps[i].Length );
                lumps[i].Position = EndianHelper.LittleLong( lumps[i].Position );
            }
        }

        /// <summary>
        /// Mod_LoadVertexes
        /// </summary>
        private void LoadVertices( ref BspLump l )
        {
            var count = 0;

            if (this.Version == BspDef.Q1_BSPVERSION || this.Version == BspDef.HL_BSPVERSION )
            {
                if ( l.Length % BspVertex.SizeInBytes != 0 )
                    Utilities.Error( $"MOD_LoadBmodel: funny lump size in {this.Name}" );

                count = l.Length / BspVertex.SizeInBytes;
            }
            else
            {
                var cc = ( float ) l.Length / Q3Vertex.SizeInBytes;

                if ( (this.BaseOffset + l.Length ) % Q3Vertex.SizeInBytes != 0 )
                    Utilities.Error( $"MOD_LoadBmodel: funny lump size in {this.Name}" );

                count = l.Length / Q3Vertex.SizeInBytes;
            }

            var verts = new MemoryVertex[count];

            this.Vertices = verts;
            this.NumVertices = count;

            for ( int i = 0, offset = this.BaseOffset + l.Position; i < count; i++, offset += BspVertex.SizeInBytes )
            {
                if (this.Version == BspDef.Q1_BSPVERSION || this.Version == BspDef.HL_BSPVERSION )
                {
                    var src = Utilities.BytesToStructure<BspVertex>(this.Buffer, offset );
                    verts[i].position = EndianHelper.LittleVector3( src.point );
                }
                else
                {
                    var src = Utilities.BytesToStructure<Q3Vertex>(this.Buffer, offset );
                    verts[i].position = EndianHelper.LittleVector3( src.origin );
                }
            }
        }

        /// <summary>
        /// Mod_LoadEdges
        /// </summary>
        private void LoadEdges( ref BspLump l )
        {
            if ( l.Length % BspEdge.SizeInBytes != 0 )
                Utilities.Error( $"MOD_LoadBmodel: funny lump size in {this.Name}" );

            var count = l.Length / BspEdge.SizeInBytes;

            // Uze: Why count + 1 ?????
            var e = new MemoryEdge[count]; // out = Hunk_AllocName ( (count + 1) * sizeof(*out), loadname);
            this.Edges = e;
            this.NumEdges = count;

            for ( int i = 0, offset = l.Position; i < count; i++, offset += BspEdge.SizeInBytes )
            {
                var src = Utilities.BytesToStructure<BspEdge>(this.Buffer, offset );
                e[i].v = new ushort[] {
                    (ushort)EndianHelper.LittleShort((short)src.v[0]),
                    (ushort)EndianHelper.LittleShort((short)src.v[1])
                };
            }
        }

        /// <summary>
        /// Mod_LoadSurfedges
        /// </summary>
        private void LoadSurfEdges( ref BspLump l )
        {
            if ( l.Length % sizeof( int ) != 0 )
                Utilities.Error( $"MOD_LoadBmodel: funny lump size in {this.Name}" );

            var count = l.Length / sizeof( int );
            var e = new int[count];

            this.SurfEdges = e;
            this.NumSurfEdges = count;

            for ( int i = 0, offset = l.Position; i < count; i++, offset += 4 )
            {
                var src = BitConverter.ToInt32(this.Buffer, offset );
                e[i] = src; // EndianHelper.LittleLong(in[i]);
            }
        }

        /// <summary>
        /// Mod_LoadTextures
        /// </summary>
        private void LoadTextures( ref BspLump l, Action<ModelTexture> onCheckInitSkyTexture, Func<string, Tuple<byte[], Size, byte[]>> onCheckForTexture )
        {
            if ( l.Length == 0 )
            {
                this.Textures = null;
                return;
            }

            if (this.Version == BspDef.Q3_BSPVERSION )
            {
                var count = l.Length / Q3Texture.SizeInBytes;
                var offset = this.BaseOffset + l.Position;
                for ( var i = 0; i < count; i++ )
                {
                    var tex = Utilities.BytesToStructure<Q3Texture>(this.Buffer, offset );
                    
                    offset += Q3Texture.SizeInBytes;
                }
            }
            else
            {
                var m = Utilities.BytesToStructure<BspMipTexLump>(this.Buffer, l.Position );// (dmiptexlump_t *)(mod_base + l.fileofs);

                m.nummiptex = EndianHelper.LittleLong( m.nummiptex );

                var dataofs = new int[m.nummiptex];

                System.Buffer.BlockCopy(this.Buffer, l.Position + BspMipTexLump.SizeInBytes, dataofs, 0, dataofs.Length * sizeof( int ) );

                this.NumTextures = m.nummiptex;
                this.Textures = new ModelTexture[m.nummiptex]; // Hunk_AllocName (m->nummiptex * sizeof(*loadmodel->textures) , loadname);

                for ( var i = 0; i < m.nummiptex; i++ )
                {
                    dataofs[i] = EndianHelper.LittleLong( dataofs[i] );
                    if ( dataofs[i] == -1 )
                        continue;

                    var mtOffset = l.Position + dataofs[i];
					var mt = Utilities.BytesToStructure<WadMipTex>(this.Buffer, mtOffset ); //mt = (miptex_t *)((byte *)m + m.dataofs[i]);
					mt.width = ( uint ) EndianHelper.LittleLong( ( int ) mt.width );
					mt.height = ( uint ) EndianHelper.LittleLong( ( int ) mt.height );

					var tx = new ModelTexture( );// Hunk_AllocName(sizeof(texture_t) + pixels, loadname);
					tx.name = Utilities.GetString( mt.name );

					var texResult = onCheckForTexture( tx.name );

					if ( texResult?.Item1 != null )
					{
						var overrideTex = texResult.Item1;
						var size = texResult.Item2;

						mt.width = ( uint ) size.Width;
						mt.height = ( uint ) size.Height;
						tx.scaleX = 1f;
						tx.scaleY = 1f;

						tx.pixels = overrideTex;

						tx.width = mt.width;
						tx.height = mt.height;
						tx.localPalette = texResult.Item3;
					}
					else if (this.Version == BspDef.Q1_BSPVERSION )
					{
						tx.scaleX = 1f;
						tx.scaleY = 1f;

						tx.width = mt.width;
						tx.height = mt.height;
						var pixels = ( int ) ( mt.width * mt.height / 64 * 85 );

						// the pixels immediately follow the structures
						tx.pixels = new byte[pixels];
#warning BlockCopy tries to copy data over the bounds of _ModBase if certain mods are loaded. Needs proof fix!
						if ( mtOffset + WadMipTex.SizeInBytes + pixels <= this.Buffer.Length )
							System.Buffer.BlockCopy(this.Buffer, mtOffset + WadMipTex.SizeInBytes, tx.pixels, 0, pixels );
						else
						{
							System.Buffer.BlockCopy(this.Buffer, mtOffset + WadMipTex.SizeInBytes, tx.pixels, 0, pixels );
							ConsoleWrapper.Print( $"Texture info of {this.Name} truncated to fit in bounds of _ModBase\n" );
						}
					}
					else
						continue;

					for ( var j = 0; j < BspDef.MIPLEVELS; j++ )
						mt.offsets[j] = ( uint ) EndianHelper.LittleLong( ( int ) mt.offsets[j] );

                    this.Textures[i] = tx;

					if (this.Version == BspDef.Q1_BSPVERSION && mt.offsets[0] == 0 )
						continue;

					for ( var j = 0; j < BspDef.MIPLEVELS; j++ )
						tx.offsets[j] = ( int ) mt.offsets[j] - WadMipTex.SizeInBytes;

					onCheckInitSkyTexture( tx );

                    //if ( tx.name != null && tx.name.StartsWith( "sky" ) )// !Q_strncmp(mt->name,"sky",3))
                    //    Host.RenderContext.InitSky( tx );
                    //else
                    //    tx.texture = BaseTexture.FromBuffer( Host.Video.Device, tx.name, new ByteArraySegment( tx.pixels ),
                    //        ( Int32 ) tx.width, ( Int32 ) tx.height, true, false );
                }

                //
                // sequence the animations
                //
                var anims = new ModelTexture[10];
                var altanims = new ModelTexture[10];

                for ( var i = 0; i < m.nummiptex; i++ )
                {
                    var tx = this.Textures[i];
                    if ( tx == null || !tx.name.StartsWith( "+" ) )// [0] != '+')
                        continue;
                    if ( tx.anim_next != null )
                        continue;   // allready sequenced

                    // find the number of frames in the animation
                    Array.Clear( anims, 0, anims.Length );
                    Array.Clear( altanims, 0, altanims.Length );

                    int max = tx.name[1];
                    var altmax = 0;
                    if ( max >= 'a' && max <= 'z' )
                        max -= 'a' - 'A';
                    if ( max >= '0' && max <= '9' )
                    {
                        max -= '0';
                        altmax = 0;
                        anims[max] = tx;
                        max++;
                    }
                    else if ( max >= 'A' && max <= 'J' )
                    {
                        altmax = max - 'A';
                        max = 0;
                        altanims[altmax] = tx;
                        altmax++;
                    }
                    else
                        Utilities.Error( "Bad animating texture {0}", tx.name );

                    for ( var j = i + 1; j < m.nummiptex; j++ )
                    {
                        var tx2 = this.Textures[j];
                        if ( tx2 == null || !tx2.name.StartsWith( "+" ) )// tx2->name[0] != '+')
                            continue;
                        if ( string.Compare( tx2.name, 2, tx.name, 2, Math.Min( tx.name.Length, tx2.name.Length ) ) != 0 )// strcmp (tx2->name+2, tx->name+2))
                            continue;

                        int num = tx2.name[1];

                        if ( num >= 'a' && num <= 'z' )
                            num -= 'a' - 'A';

                        if ( num >= '0' && num <= '9' )
                        {
                            num -= '0';
                            anims[num] = tx2;
                            if ( num + 1 > max )
                                max = num + 1;
                        }
                        else if ( num >= 'A' && num <= 'J' )
                        {
                            num = num - 'A';
                            altanims[num] = tx2;
                            if ( num + 1 > altmax )
                                altmax = num + 1;
                        }
                        else
                            Utilities.Error( "Bad animating texture {0}", tx2.name );
                    }

                    // link them all together
                    for ( var j = 0; j < max; j++ )
                    {
                        var tx2 = anims[j];

                        if ( tx2 == null )
                            Utilities.Error( "Missing frame {0} of {1}", j, tx.name );

                        tx2.anim_total = max * ModelDef.ANIM_CYCLE;
                        tx2.anim_min = j * ModelDef.ANIM_CYCLE;
                        tx2.anim_max = ( j + 1 ) * ModelDef.ANIM_CYCLE;
                        tx2.anim_next = anims[( j + 1 ) % max];

                        if ( altmax != 0 )
                            tx2.alternate_anims = altanims[0];
                    }
                    for ( var j = 0; j < altmax; j++ )
                    {
                        var tx2 = altanims[j];

                        if ( tx2 == null )
                            Utilities.Error( "Missing frame {0} of {1}", j, tx2.name );

                        tx2.anim_total = altmax * ModelDef.ANIM_CYCLE;
                        tx2.anim_min = j * ModelDef.ANIM_CYCLE;
                        tx2.anim_max = ( j + 1 ) * ModelDef.ANIM_CYCLE;
                        tx2.anim_next = altanims[( j + 1 ) % altmax];

                        if ( max != 0 )
                            tx2.alternate_anims = anims[0];
                    }
                }
            }
        }

        /// <summary>
        /// Mod_LoadLighting
        /// </summary>
        private void LoadLighting( ref BspLump l )
        {
            if ( l.Length == 0 )
            {
                this.LightData = null;
                return;
            }

            this.LightData = new byte[l.Length]; // Hunk_AllocName(l->filelen, loadname);
            System.Buffer.BlockCopy(this.Buffer, l.Position, this.LightData, 0, l.Length );
        }

        /// <summary>
        /// Mod_LoadPlanes
        /// </summary>
        private void LoadPlanes( ref BspLump l )
        {
            if ( l.Length % BspPlane.SizeInBytes != 0 )
                Utilities.Error( $"MOD_LoadBmodel: funny lump size in {this.Name}" );

            var count = l.Length / BspPlane.SizeInBytes;
            // Uze: Possible error! Why in original is out = Hunk_AllocName ( count*2*sizeof(*out), loadname)???
            var p = new Plane[count];

            for ( var i = 0; i < p.Length; i++ )
                p[i] = new( );

            this.Planes = p;
            this.NumPlanes = count;

            for ( var i = 0; i < count; i++ )
            {
                var src = Utilities.BytesToStructure<BspPlane>(this.Buffer, l.Position + i * BspPlane.SizeInBytes );
                var bits = 0;
                p[i].normal = EndianHelper.LittleVector3( src.normal );

                if ( p[i].normal.X < 0 )
                    bits |= 1;

                if ( p[i].normal.Y < 0 )
                    bits |= 1 << 1;

                if ( p[i].normal.Z < 0 )
                    bits |= 1 << 2;

                p[i].dist = EndianHelper.LittleFloat( src.dist );
                p[i].type = ( byte ) EndianHelper.LittleLong( src.type );
                p[i].signbits = ( byte ) bits;
            }
        }

        /// <summary>
        /// Mod_LoadTexinfo
        /// </summary>
        private void LoadTexInfo( ref BspLump l )
        {
            //in = (void *)(mod_base + l->fileofs);
            if ( l.Length % BspTextureInfo.SizeInBytes != 0 )
                Utilities.Error( $"MOD_LoadBmodel: funny lump size in {this.Name}" );

            var count = l.Length / BspTextureInfo.SizeInBytes;
            var infos = new MemoryTextureInfo[count]; // out = Hunk_AllocName ( count*sizeof(*out), loadname);

            for ( var i = 0; i < infos.Length; i++ )
                infos[i] = new( );

            this.TexInfo = infos;
            this.NumTexInfo = count;

            for ( var i = 0; i < count; i++ )//, in++, out++)
            {
                var src = Utilities.BytesToStructure<BspTextureInfo>(this.Buffer, l.Position + i * BspTextureInfo.SizeInBytes );

                for ( var j = 0; j < 2; j++ )
                    infos[i].vecs[j] = EndianHelper.LittleVector4( src.vecs, j * 4 );

                var len1 = infos[i].vecs[0].Length();
                var len2 = infos[i].vecs[1].Length();
                len1 = ( len1 + len2 ) / 2;
                if ( len1 < 0.32 )
                    infos[i].mipadjust = 4;
                else if ( len1 < 0.49 )
                    infos[i].mipadjust = 3;
                else if ( len1 < 0.99 )
                    infos[i].mipadjust = 2;
                else
                    infos[i].mipadjust = 1;

                var miptex = EndianHelper.LittleLong( src.miptex );
                infos[i].flags = EndianHelper.LittleLong( src.flags );

                if (this.Textures == null )
                {
                    infos[i].texture = this.NoTexture;//Host.RenderContext.NoTextureMip;	// checkerboard texture
                    infos[i].flags = 0;
                }
                else
                {
                    if ( miptex >= this.NumTextures )
                        Utilities.Error( "miptex >= loadmodel->numtextures" );

                    infos[i].texture = this.Textures[miptex];

                    if ( infos[i].texture == null )
                    {
                        infos[i].texture = this.NoTexture; //Host.RenderContext.NoTextureMip; // texture not found
                        infos[i].flags = 0;
                    }
                }
            }
        }

        /// <summary>
        /// Mod_LoadFaces
        /// </summary>
        private void LoadFaces( ref BspLump l )
        {
            if ( l.Length % BspFace.SizeInBytes != 0 )
                Utilities.Error( $"MOD_LoadBmodel: funny lump size in {this.Name}" );

            var count = l.Length / BspFace.SizeInBytes;
            var dest = new MemorySurface[count];

            for ( var i = 0; i < dest.Length; i++ )
                dest[i] = new( );

            this.Surfaces = dest;
            this.NumSurfaces = count;
            var offset = l.Position;

            for ( var surfnum = 0; surfnum < count; surfnum++, offset += BspFace.SizeInBytes )
            {
                var src = Utilities.BytesToStructure<BspFace>(this.Buffer, offset );

                dest[surfnum].firstedge = EndianHelper.LittleLong( src.firstedge );
                dest[surfnum].numedges = EndianHelper.LittleShort( src.numedges );
                dest[surfnum].flags = 0;

                int planenum = EndianHelper.LittleShort( src.planenum );
                int side = EndianHelper.LittleShort( src.side );

                if ( side != 0 )
                    dest[surfnum].flags |= ( int ) Q1SurfaceFlags.PlaneBack;

                dest[surfnum].plane = this.Planes[planenum];
                dest[surfnum].texinfo = this.TexInfo[EndianHelper.LittleShort( src.texinfo )];

                this.CalcSurfaceExtents( dest[surfnum] );

                // lighting info

                for ( var i = 0; i < BspDef.MAXLIGHTMAPS; i++ )
                    dest[surfnum].styles[i] = src.styles[i];

                var i2 = EndianHelper.LittleLong( src.lightofs );

                if ( i2 == -1 )
                    dest[surfnum].sample_base = null;
                else
                {
                    dest[surfnum].sample_base = this.LightData;
                    dest[surfnum].sampleofs = i2;
                }

                // set the drawing flags flag
                if ( dest[surfnum].texinfo.texture.name != null )
                {
                    if ( dest[surfnum].texinfo.texture.name.StartsWith( "sky" ) )	// sky
                    {
                        dest[surfnum].flags |= ( int ) Q1SurfaceFlags.Sky | ( int ) Q1SurfaceFlags.Tiled;
                        this.SubdivideSurface( dest[surfnum] );	// cut up polygon for warps
                        continue;
                    }

                    if ( dest[surfnum].texinfo.texture.name.StartsWith( "*" ) )		// turbulent
                    {
                        dest[surfnum].flags |= ( int ) Q1SurfaceFlags.Turbulence | ( int ) Q1SurfaceFlags.Tiled;

                        for ( var i = 0; i < 2; i++ )
                        {
                            dest[surfnum].extents[i] = 16384;
                            dest[surfnum].texturemins[i] = -8192;
                        }

                        this.SubdivideSurface( dest[surfnum] );	// cut up polygon for warps
                        continue;
                    }
                }
            }
        }

        /// <summary>
        /// Mod_LoadMarksurfaces
        /// </summary>
        private void LoadMarkSurfaces( ref BspLump l )
        {
            if ( l.Length % sizeof( short ) != 0 )
                Utilities.Error( $"MOD_LoadBmodel: funny lump size in {this.Name}" );

            var count = l.Length / sizeof( short );
            var dest = new MemorySurface[count];

            this.MarkSurfaces = dest;
            this.NumMarkSurfaces = count;

            for ( var i = 0; i < count; i++ )
            {
                int j = BitConverter.ToInt16(this.Buffer, l.Position + i * sizeof( short ) );

                if ( j >= this.NumSurfaces )
                    Utilities.Error( "Mod_ParseMarksurfaces: bad surface number" );

                dest[i] = this.Surfaces[j];
            }
        }

        /// <summary>
        /// Mod_LoadVisibility
        /// </summary>
        private void LoadVisibility( ref BspLump l )
        {
            if ( l.Length == 0 )
            {
                this.VisData = null;
                return;
            }

            this.VisData = new byte[l.Length];
            System.Buffer.BlockCopy(this.Buffer, l.Position, this.VisData, 0, l.Length );
        }

        /// <summary>
        /// Mod_LoadLeafs
        /// </summary>
        private void LoadLeafs( ref BspLump l )
        {
            if ( l.Length % BspLeaf.SizeInBytes != 0 )
                Utilities.Error( $"MOD_LoadBmodel: funny lump size in {this.Name}" );

            var count = l.Length / BspLeaf.SizeInBytes;
            var dest = new MemoryLeaf[count];

            for ( var i = 0; i < dest.Length; i++ )
                dest[i] = new( );

            this.Leaves = dest;
            this.NumLeafs = count;

            for ( int i = 0, offset = l.Position; i < count; i++, offset += BspLeaf.SizeInBytes )
            {
                var src = Utilities.BytesToStructure<BspLeaf>(this.Buffer, offset );

                dest[i].mins.X = EndianHelper.LittleShort( src.mins[0] );
                dest[i].mins.Y = EndianHelper.LittleShort( src.mins[1] );
                dest[i].mins.Z = EndianHelper.LittleShort( src.mins[2] );

                dest[i].maxs.X = EndianHelper.LittleShort( src.maxs[0] );
                dest[i].maxs.Y = EndianHelper.LittleShort( src.maxs[1] );
                dest[i].maxs.Z = EndianHelper.LittleShort( src.maxs[2] );

                var p = EndianHelper.LittleLong( src.contents );
                dest[i].contents = p;

                dest[i].marksurfaces = this.MarkSurfaces;
                dest[i].firstmarksurface = EndianHelper.LittleShort( ( short ) src.firstmarksurface );
                dest[i].nummarksurfaces = EndianHelper.LittleShort( ( short ) src.nummarksurfaces );

                p = EndianHelper.LittleLong( src.visofs );

                if ( p == -1 )
                    dest[i].compressed_vis = null;
                else
                {
                    dest[i].compressed_vis = this.VisData; // loadmodel->visdata + p;
                    dest[i].visofs = p;
                }

                dest[i].efrags = null;

                for ( var j = 0; j < 4; j++ )
                    dest[i].ambient_sound_level[j] = src.ambient_level[j];

                // gl underwater warp
                // Uze: removed underwater warp as too ugly
                //if (dest[i].contents != Contents.CONTENTS_EMPTY)
                //{
                //    for (int j = 0; j < dest[i].nummarksurfaces; j++)
                //        dest[i].marksurfaces[dest[i].firstmarksurface + j].flags |= Surf.SURF_UNDERWATER;
                //}
            }
        }

        /// <summary>
        /// Mod_LoadNodes
        /// </summary>
        private void LoadNodes( ref BspLump l )
        {
            if ( l.Length % BspNode.SizeInBytes != 0 )
                Utilities.Error( $"MOD_LoadBmodel: funny lump size in {this.Name}" );

            var count = l.Length / BspNode.SizeInBytes;
            var dest = new MemoryNode[count];

            for ( var i = 0; i < dest.Length; i++ )
                dest[i] = new( );

            this.Nodes = dest;
            this.NumNodes = count;

            for ( int i = 0, offset = l.Position; i < count; i++, offset += BspNode.SizeInBytes )
            {
                var src = Utilities.BytesToStructure<BspNode>(this.Buffer, offset );

                dest[i].mins.X = EndianHelper.LittleShort( src.mins[0] );
                dest[i].mins.Y = EndianHelper.LittleShort( src.mins[1] );
                dest[i].mins.Z = EndianHelper.LittleShort( src.mins[2] );

                dest[i].maxs.X = EndianHelper.LittleShort( src.maxs[0] );
                dest[i].maxs.Y = EndianHelper.LittleShort( src.maxs[1] );
                dest[i].maxs.Z = EndianHelper.LittleShort( src.maxs[2] );

                var p = EndianHelper.LittleLong( src.planenum );
                dest[i].plane = this.Planes[p];

                dest[i].firstsurface = ( ushort ) EndianHelper.LittleShort( ( short ) src.firstface );
                dest[i].numsurfaces = ( ushort ) EndianHelper.LittleShort( ( short ) src.numfaces );

                for ( var j = 0; j < 2; j++ )
                {
                    p = EndianHelper.LittleShort( src.children[j] );

                    if ( p >= 0 )
                        dest[i].children[j] = this.Nodes[p];
                    else
                        dest[i].children[j] = this.Leaves[-1 - p];
                }
            }

            this.SetParent(this.Nodes[0], null );	// sets nodes and leafs
        }

        /// <summary>
        /// Mod_LoadClipnodes
        /// </summary>
        private void LoadClipNodes( ref BspLump l )
        {
            if ( l.Length % BspClipNode.SizeInBytes != 0 )
                Utilities.Error( $"MOD_LoadBmodel: funny lump size in {this.Name}" );

            var count = l.Length / BspClipNode.SizeInBytes;
            var dest = new BspClipNode[count];

            this.ClipNodes = dest;
            this.NumClipNodes = count;

            var hull = this.Hulls[1];
            hull.clipnodes = dest;
            hull.firstclipnode = 0;
            hull.lastclipnode = count - 1;
            hull.planes = this.Planes;
            hull.clip_mins.X = -16;
            hull.clip_mins.Y = -16;
            hull.clip_mins.Z = -24;
            hull.clip_maxs.X = 16;
            hull.clip_maxs.Y = 16;
            hull.clip_maxs.Z = 32;

            hull = this.Hulls[2];
            hull.clipnodes = dest;
            hull.firstclipnode = 0;
            hull.lastclipnode = count - 1;
            hull.planes = this.Planes;
            hull.clip_mins.X = -32;
            hull.clip_mins.Y = -32;
            hull.clip_mins.Z = -24;
            hull.clip_maxs.X = 32;
            hull.clip_maxs.Y = 32;
            hull.clip_maxs.Z = 64;

            for ( int i = 0, offset = l.Position; i < count; i++, offset += BspClipNode.SizeInBytes )
            {
                var src = Utilities.BytesToStructure<BspClipNode>(this.Buffer, offset );

                dest[i].planenum = EndianHelper.LittleLong( src.planenum ); // Uze: changed from LittleShort
                dest[i].children = new short[2];
                dest[i].children[0] = EndianHelper.LittleShort( src.children[0] );
                dest[i].children[1] = EndianHelper.LittleShort( src.children[1] );
            }
        }

        /// <summary>
        /// Mod_LoadEntities
        /// </summary>
        private void LoadEntities( ref BspLump l )
        {
            if ( l.Length == 0 )
            {
                this.Entities = null;
                return;
            }

            this.Entities = Encoding.ASCII.GetString(this.Buffer, this.BaseOffset + l.Position, l.Length );
        }

        /// <summary>
        /// Mod_LoadSubmodels
        /// </summary>
        private void LoadSubModels( ref BspLump l )
        {
            if ( l.Length % Q1Model.SizeInBytes != 0 )
                Utilities.Error( $"MOD_LoadBmodel: funny lump size in {this.Name}" );

            var count = l.Length / Q1Model.SizeInBytes;
            var dest = new Q1Model[count];

            this.SubModels = dest;
            this.NumSubModels = count;

            for ( int i = 0, offset = l.Position; i < count; i++, offset += Q1Model.SizeInBytes )
            {
                var src = Utilities.BytesToStructure<Q1Model>(this.Buffer, offset );

                dest[i].mins = new float[3];
                dest[i].maxs = new float[3];
                dest[i].origin = new float[3];

                for ( var j = 0; j < 3; j++ )
                {
                    // spread the mins / maxs by a pixel
                    dest[i].mins[j] = EndianHelper.LittleFloat( src.mins[j] ) - 1;
                    dest[i].maxs[j] = EndianHelper.LittleFloat( src.maxs[j] ) + 1;
                    dest[i].origin[j] = EndianHelper.LittleFloat( src.origin[j] );
                }

                dest[i].headnode = new int[BspDef.MAX_MAP_HULLS];
                for ( var j = 0; j < BspDef.MAX_MAP_HULLS; j++ )
                    dest[i].headnode[j] = EndianHelper.LittleLong( src.headnode[j] );

                dest[i].visleafs = EndianHelper.LittleLong( src.visleafs );
                dest[i].firstface = EndianHelper.LittleLong( src.firstface );
                dest[i].numfaces = EndianHelper.LittleLong( src.numfaces );
            }
        }

        /// <summary>
        /// Mod_MakeHull0
        /// Deplicate the drawing hull structure as a clipping hull
        /// </summary>
        private void MakeHull0( )
        {
            var hull = this.Hulls[0];
            var src = this.Nodes;
            var count = this.NumNodes;
            var dest = new BspClipNode[count];

            hull.clipnodes = dest;
            hull.firstclipnode = 0;
            hull.lastclipnode = count - 1;
            hull.planes = this.Planes;

            for ( var i = 0; i < count; i++ )
            {
                dest[i].planenum = Array.IndexOf(this.Planes, src[i].plane ); // todo: optimize this
                dest[i].children = new short[2];

                for ( var j = 0; j < 2; j++ )
                {
                    var child = src[i].children[j];
                    if ( child.contents < 0 )
                        dest[i].children[j] = ( short ) child.contents;
                    else
                        dest[i].children[j] = ( short ) Array.IndexOf(this.Nodes, ( MemoryNode ) child ); // todo: optimize this
                }
            }
        }

        /// <summary>
        /// Mod_SetParent
        /// </summary>
        private void SetParent( MemoryNodeBase node, MemoryNode parent )
        {
            node.parent = parent;

            if ( node.contents < 0 )
                return;

            var n = ( MemoryNode ) node;
            this.SetParent( n.children[0], n );
            this.SetParent( n.children[1], n );
        }

        /// <summary>
        /// CalcSurfaceExtents
        /// Fills in s->texturemins[] and s->extents[]
        /// </summary>
        private void CalcSurfaceExtents( MemorySurface s )
        {
            var mins = new float[] { 999999, 999999 };
            var maxs = new float[] { -99999, -99999 };

            var tex = s.texinfo;
            var v = this.Vertices;

            for ( var i = 0; i < s.numedges; i++ )
            {
                int idx;
                var e = this.SurfEdges[s.firstedge + i];

                if ( e >= 0 )
                    idx = this.Edges[e].v[0];
                else
                    idx = this.Edges[-e].v[1];

                for ( var j = 0; j < 2; j++ )
                {
                    var val = v[idx].position.X * tex.vecs[j].X +
                        v[idx].position.Y * tex.vecs[j].Y +
                        v[idx].position.Z * tex.vecs[j].Z +
                        tex.vecs[j].W;
                    if ( val < mins[j] )
                        mins[j] = val;
                    if ( val > maxs[j] )
                        maxs[j] = val;
                }
            }

            var bmins = new int[2];
            var bmaxs = new int[2];

            for ( var i = 0; i < 2; i++ )
            {
                bmins[i] = ( int ) Math.Floor( mins[i] / 16 );
                bmaxs[i] = ( int ) Math.Ceiling( maxs[i] / 16 );

                s.texturemins[i] = ( short ) ( bmins[i] * 16 );
                s.extents[i] = ( short ) ( ( bmaxs[i] - bmins[i] ) * 16 );

			}

			var ssize = ( s.extents[0] >> 4 ) + 1;
			var tsize = ( s.extents[1] >> 4 ) + 1;

			if (this.Version != BspDef.Q3_BSPVERSION && ( tex?.flags & BspDef.TEX_SPECIAL ) == 0 ) //&& s.extents[i] > 512
			{
				if ( ssize > 256 || tsize > 256 )
					Utilities.Error( "Bad surface extents" );
			}
		}

        /// <summary>
        /// GL_SubdivideSurface
        /// Breaks a polygon up along axial 64 unit boundaries
        /// so that turbulent and sky warps can be done reasonably.
        /// </summary>
        protected void SubdivideSurface( MemorySurface fa )
        {
            this.WarpFace = fa;

            //
            // convert edges back to a normal polygon
            //
            var numverts = 0;
            var verts = new Vector3[fa.numedges + 1]; // + 1 for wrap case

            for ( var i = 0; i < fa.numedges; i++ )
            {
                var lindex = this.SurfEdges[fa.firstedge + i];

                if ( lindex > 0 )
                    verts[numverts] = this.Vertices[this.Edges[lindex].v[0]].position;
                else
                    verts[numverts] = this.Vertices[this.Edges[-lindex].v[1]].position;

                numverts++;
            }

            this.SubdividePolygon( numverts, verts );
        }

        /// <summary>
        /// SubdividePolygon
        /// </summary>
        protected void SubdividePolygon( int numverts, Vector3[] verts )
        {
            if ( numverts > 60 )
                Utilities.Error( "numverts = {0}", numverts );

            Vector3 mins, maxs;
            this.BoundPoly( numverts, verts, out mins, out maxs );

            var dist = new float[64];
            for ( var i = 0; i < 3; i++ )
            {
                var m = ( MathLib.Comp( ref mins, i ) + MathLib.Comp( ref maxs, i ) ) * 0.5;
                m = this.SubdivideSize * Math.Floor( m / this.SubdivideSize + 0.5 );
                if ( MathLib.Comp( ref maxs, i ) - m < 8 )
                    continue;

                if ( m - MathLib.Comp( ref mins, i ) < 8 )
                    continue;

                for ( var j = 0; j < numverts; j++ )
                    dist[j] = ( float ) ( MathLib.Comp( ref verts[j], i ) - m );

                var front = new Vector3[64];
                var back = new Vector3[64];

                // cut it

                // wrap cases
                dist[numverts] = dist[0];
                verts[numverts] = verts[0]; // Uze: source array must be at least numverts + 1 elements long

                int f = 0, b = 0;
                for ( var j = 0; j < numverts; j++ )
                {
                    if ( dist[j] >= 0 )
                    {
                        front[f] = verts[j];
                        f++;
                    }
                    if ( dist[j] <= 0 )
                    {
                        back[b] = verts[j];
                        b++;
                    }
                    if ( dist[j] == 0 || dist[j + 1] == 0 )
                        continue;
                    if ( dist[j] > 0 != dist[j + 1] > 0 )
                    {
                        // clip point
                        var frac = dist[j] / ( dist[j] - dist[j + 1] );
                        front[f] = back[b] = verts[j] + ( verts[j + 1] - verts[j] ) * frac;
                        f++;
                        b++;
                    }
                }

                this.SubdividePolygon( f, front );
                this.SubdividePolygon( b, back );
                return;
            }

            var poly = new GLPoly( );
            poly.next = this.WarpFace.polys;
            this.WarpFace.polys = poly;
            poly.AllocVerts( numverts );
            for ( var i = 0; i < numverts; i++ )
            {
                Utilities.Copy( ref verts[i], poly.verts[i] );
                var s = Vector3.Dot( verts[i], new(this.WarpFace.texinfo.vecs[0].X, this.WarpFace.texinfo.vecs[0].Y, this.WarpFace.texinfo.vecs[0].Z) );
                var t = Vector3.Dot( verts[i], new(this.WarpFace.texinfo.vecs[1].X, this.WarpFace.texinfo.vecs[1].Y, this.WarpFace.texinfo.vecs[1].Z) );
                poly.verts[i][3] = s;
                poly.verts[i][4] = t;
            }
        }

        /// <summary>
        /// BoundPoly
        /// </summary>
        protected void BoundPoly( int numverts, Vector3[] verts, out Vector3 mins, out Vector3 maxs )
        {
            mins = Vector3.One * 9999;
            maxs = Vector3.One * -9999;
            for ( var i = 0; i < numverts; i++ )
            {
                mins = Vector3.Min( verts[i], mins );
                maxs = Vector3.Max( verts[i], maxs );
            }
        }

        public void SetupSubModel( ref Q1Model submodel )
        {
            this.Hulls[0].firstclipnode = submodel.headnode[0];
            for ( var j = 1; j < BspDef.MAX_MAP_HULLS; j++ )
            {
                this.Hulls[j].firstclipnode = submodel.headnode[j];
                this.Hulls[j].lastclipnode = this.NumClipNodes - 1;
            }

            this.FirstModelSurface = submodel.firstface;
            this.NumModelSurfaces = submodel.numfaces;

            var mins = this.BoundsMin;
            var maxs = this.BoundsMax;

            Utilities.Copy( submodel.maxs, out maxs ); // mod.maxs = submodel.maxs;
            Utilities.Copy( submodel.mins, out mins ); // mod.mins = submodel.mins;
            this.Radius = this.RadiusFromBounds( ref mins, ref maxs );
            this.NumLeafs = submodel.visleafs;

            this.BoundsMin = mins;
            this.BoundsMax = maxs;
        }

        private float RadiusFromBounds( ref Vector3 mins, ref Vector3 maxs )
        {
            Vector3 corner;

            corner.X = Math.Max( Math.Abs( mins.X ), Math.Abs( maxs.X ) );
            corner.Y = Math.Max( Math.Abs( mins.Y ), Math.Abs( maxs.Y ) );
            corner.Z = Math.Max( Math.Abs( mins.Z ), Math.Abs( maxs.Z ) );

            return corner.Length();
        }

        /// <summary>
        /// Mod_DecompressVis
        /// </summary>
        private byte[] DecompressVis( byte[] p, int startIndex )
        {
            var row = (this.NumLeafs + 7 ) >> 3;
            var offset = 0;

            if ( p == null )
            {
                // no vis info, so make all visible
                while ( row != 0 )
                {
                    this._Decompressed[offset++] = 0xff;
                    row--;
                }
                return this._Decompressed;
            }
            var srcOffset = startIndex;
            do
            {
                if ( p[srcOffset] != 0 )// (*in)
                {
                    this._Decompressed[offset++] = p[srcOffset++]; //  *out++ = *in++;
                    continue;
                }

                int c = p[srcOffset + 1];// in[1];
                srcOffset += 2; // in += 2;
                while ( c != 0 )
                {
                    this._Decompressed[offset++] = 0; // *out++ = 0;
                    c--;
                }
            } while ( offset < row ); // out - decompressed < row

            return this._Decompressed;
        }

        /// <summary>
        /// Mod_LeafPVS
        /// </summary>
        public byte[] LeafPVS( MemoryLeaf leaf )
        {
            if ( leaf == this.Leaves[0] )
                return this._NoVis;

            return this.DecompressVis( leaf.compressed_vis, leaf.visofs );
        }

        /// <summary>
        /// Mod_PointInLeaf
        /// </summary>
        public MemoryLeaf PointInLeaf( ref Vector3 p )
        {
            if (this.Nodes == null )
                Utilities.Error( "Mod_PointInLeaf: bad model" );

            MemoryLeaf result = null;
            MemoryNodeBase node = this.Nodes[0];

            while ( true )
            {
                if ( node.contents < 0 )
                {
                    result = ( MemoryLeaf ) node;
                    break;
                }

                var n = ( MemoryNode ) node;
                var plane = n.plane;
                var d = Vector3.Dot( p, plane.normal ) - plane.dist;
                if ( d > 0 )
                    node = n.children[0];
                else
                    node = n.children[1];
            }

            return result;
        }
	}
}
