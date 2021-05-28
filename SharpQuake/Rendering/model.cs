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



// gl_model.c -- model loading and caching

// models are the only shared resource between a client and server running
// on the same machine.

namespace SharpQuake.Rendering
{
    using Engine.Host;
    using Framework.Data;
    using Framework.Definitions.Bsp;
    using Framework.Engine;
    using Framework.IO;
    using Framework.Rendering.Alias;
    using Game.Data.Models;
    using Renderer.OpenGL.Textures;
    using Renderer.Textures;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
	/// Mod_functions
	/// </summary>
	public class Mod
    {
        public float SubdivideSize => this._glSubDivideSize.Get<int>( );

        public Host Host
        {
            get;
            private set;
        }

        public List<BaseTexture> SkinTextures
        {
            get;
            private set;
        }

        public List<BaseTexture> SpriteTextures
        {
            get;
            private set;
        }

        private ClientVariable _glSubDivideSize
        {
            get;
            set;
        }

        private List<ModelData> ModelCache
        {
            get;
            set;
        }

        private ModelData CurrentModel
        {
            get;
            set;
        }

        public Mod( Host host )
        {
            this.Host = host;
        }

        /// <summary>
        /// Mod_Init
        /// </summary>
        public void Initialise( )
        {
            this.SkinTextures = new( );
            this.SpriteTextures = new( );
            this.ModelCache = new( ModelDef.MAX_MOD_KNOWN );

            if (this._glSubDivideSize == null )
                this._glSubDivideSize = this.Host.CVars.Add( "gl_subdivide_size", 128, ClientVariableFlags.Archive );
        }

        /// <summary>
        /// Mod_ClearAll
        /// </summary>
        public void ClearAll( )
        {
            for ( var i = 0; i < this.ModelCache.Count; i++ )
            {
                var mod = this.ModelCache[i];

                if ( mod.Type != ModelType.mod_alias )
                    mod.IsLoadRequired = true;
            }
        }

        /// <summary>
        /// Mod_ForName
        /// Loads in a model for the given name
        /// </summary>
        public ModelData ForName( string name, bool crash, ModelType type )
        {
            var mod = this.FindName( name, type );

            return this.LoadModel( mod, crash, type );
        }

        /// <summary>
        /// Mod_Extradata
        /// handles caching
        /// </summary>
        public aliashdr_t GetExtraData( ModelData mod )
        {
            var r = this.Host.Cache.Check( mod.cache );

            if ( r != null )
                return ( aliashdr_t ) r;

            this.LoadModel( mod, true, ModelType.mod_alias );

            if ( mod.cache.data == null )
                Utilities.Error( "Mod_Extradata: caching failed" );

            return ( aliashdr_t ) mod.cache.data;
        }

        /// <summary>
        /// Mod_TouchModel
        /// </summary>
        public void TouchModel( string name )
        {
            ModelType type;

            var n = name.ToLower( );

            if ( (n.StartsWith( "*" ) && !n.Contains( ".mdl" )) || n.Contains( ".bsp" ) )
                type = ModelType.mod_brush;
            else if ( n.Contains( ".mdl" ) )
                type = ModelType.mod_alias;
            else
                type = ModelType.mod_sprite;

            var mod = this.FindName( name, type );

            if ( !mod.IsLoadRequired )
            {
                if ( mod.Type == ModelType.mod_alias )
                    this.Host.Cache.Check( mod.cache );
            }
        } 

        // Mod_Print
        public void Print( CommandMessage msg )
        {
            var names = string.Join( "\n", this.ModelCache.Select( m => m.Name ) );
            ConsoleWrapper.Print( $"Cached models:\n{names}\n" );
        }

        /// <summary>
        /// Mod_FindName
        /// </summary>
        public ModelData FindName( string name, ModelType type )
        {
            if ( string.IsNullOrEmpty( name ) )
                Utilities.Error( "Mod_ForName: NULL name" );

            var mod = this.ModelCache.Where( m => m.Name == name ).FirstOrDefault( );

            if ( mod == null )
            {
                if (this.ModelCache.Count == ModelDef.MAX_MOD_KNOWN )
                    Utilities.Error( "mod_numknown == MAX_MOD_KNOWN" );

                switch ( type )
                {
                    case ModelType.mod_brush:
                        mod = new BrushModelData(this.Host.Model.SubdivideSize, this.Host.RenderContext.NoTextureMip );
                        break;

                    case ModelType.mod_sprite:
                        mod = new AliasModelData(this.Host.RenderContext.NoTextureMip );
                        break;

                    case ModelType.mod_alias:
                        mod = new SpriteModelData(this.Host.RenderContext.NoTextureMip );
                        break;
                }

                mod.Name = name;
                mod.IsLoadRequired = true;
                this.ModelCache.Add( mod );
            }

            return mod;
        }

        /// <summary>
        /// Mod_LoadModel
        /// Loads a model into the cache
        /// </summary>
        public ModelData LoadModel( ModelData mod, bool crash, ModelType type )
        {
            var name = mod.Name;

            if ( mod.Type != type )
            {
                ModelData newMod = null;

                switch ( type )
                {
                    case ModelType.mod_brush:
                        newMod = new BrushModelData(this.Host.Model.SubdivideSize, this.Host.RenderContext.NoTextureMip );
                        newMod.CopyFrom( mod );
                        break;

                    case ModelType.mod_alias:
                        newMod = new AliasModelData(this.Host.RenderContext.NoTextureMip );
                        newMod.CopyFrom( mod );
                        break;

                    case ModelType.mod_sprite:
                        newMod = new SpriteModelData(this.Host.RenderContext.NoTextureMip );
                        newMod.CopyFrom( mod );
                        break;
                }

                newMod.Name = mod.Name;

                this.ModelCache.RemoveAll( k => k.Name == name );

                mod = newMod;

                this.ModelCache.Add( mod );
            }

            if ( !mod.IsLoadRequired )
            {
                if ( mod.Type == ModelType.mod_alias )
                {
                    if (this.Host.Cache.Check( mod.cache ) != null )
                        return mod;
                }
                else
                    return mod;		// not cached at all
            }

            //
            // load the file
            //
            var buf = FileSystem.LoadFile( mod.Name );
            if ( buf == null )
            {
                if ( crash )
                    Utilities.Error( "Mod_NumForName: {0} not found", mod.Name );
                return null;
            }

            //
            // allocate a new model
            //
            this.CurrentModel = mod;

            mod.IsLoadRequired = false;

            switch ( BitConverter.ToUInt32( buf, 0 ) )// LittleLong(*(unsigned *)buf))
            {
                case ModelDef.IDPOLYHEADER:
                    this.LoadAliasModel( ( AliasModelData ) mod, buf );
                    break;

                case ModelDef.IDSPRITEHEADER:
                    this.LoadSpriteModel( ( SpriteModelData ) mod, buf );
                    break;

                default:
                    this.LoadBrushModel( ( BrushModelData ) mod, buf );
                    break;
            }

            return mod;
        }

        /// <summary>
        /// Mod_LoadAliasModel
        /// </summary>
        public void LoadAliasModel( AliasModelData mod, byte[] buffer )
        {
            mod.Load(
                this.Host.Video.Device.Palette.Table8to24, mod.Name, buffer, ( n, b, h ) => 
            {
                var texture = ( GLTexture ) BaseTexture.FromBuffer(
                    this.Host.Video.Device, n,
                        b, h.skinwidth, h.skinheight, true, false );

                this.SkinTextures.Add( texture );

                return texture.GLDesc.TextureNumber;
            }, ( m, h ) => 
            {
                //
                // build the draw lists
                //
                mesh.MakeAliasModelDisplayLists( m );

                //
                // move the complete, relocatable alias model to the cache
                //
                mod.cache = this.Host.Cache.Alloc( aliashdr_t.SizeInBytes * h.frames.Length * maliasframedesc_t.SizeInBytes, null );
                if ( mod.cache == null )
                    return;
                mod.cache.data = h;
            } );
        }

        /// <summary>
        /// Mod_LoadSpriteModel
        /// </summary>
        public void LoadSpriteModel( SpriteModelData mod, byte[] buffer )
        {
            mod.Load( mod.Name, buffer, ( n, b, w, h ) =>
            {
                var texture = ( GLTexture ) BaseTexture.FromBuffer(
                    this.Host.Video.Device, n,
                        b, w, h, true, true );

                this.SpriteTextures.Add( texture );

                return texture.GLDesc.TextureNumber;
            } );
        }

        /// <summary>
        /// Mod_LoadBrushModel
        /// </summary>
        public void LoadBrushModel( BrushModelData mod, byte[] buffer )
        {
            mod.Load( mod.Name, buffer, ( tx ) => 
            {
                if ( tx.name != null && tx.name.StartsWith( "sky" ) )// !Q_strncmp(mt->name,"sky",3))
                    this.Host.RenderContext.WarpableTextures.InitSky( tx );
                else
                {   
                    tx.texture = BaseTexture.FromBuffer(
                        this.Host.Video.Device, tx.name, new ByteArraySegment( tx.pixels ),
                     ( int ) tx.width, ( int ) tx.height, true, true );
                }
            },
            ( textureFile ) =>             
            {
				var lowerName = textureFile.ToLower( );

				if (this.Host.WadTextures.ContainsKey( lowerName ) )
				{
					var wadFile = this.Host.WadTextures[lowerName];
					var wad = this.Host.WadFiles[wadFile];

					return wad.GetLumpBuffer( textureFile );
				}

				return null;
			} );

            //
            // set up the submodels (FIXME: this is confusing)
            //
            for ( var i = 0; i < mod.NumSubModels; i++ )
            {
                mod.SetupSubModel( ref mod.SubModels[i] );

                if ( i < mod.NumSubModels - 1 )
                {
                    // duplicate the basic information
                    var name = "*" + ( i + 1 ).ToString( );
                    this.CurrentModel = this.FindName( name, ModelType.mod_brush );
                    this.CurrentModel.CopyFrom( mod ); // *loadmodel = *mod;
                    this.CurrentModel.Name = name; //strcpy (loadmodel->name, name);
                    mod = ( BrushModelData )this.CurrentModel; //mod = loadmodel;
                }
            }
        }
    }
}
