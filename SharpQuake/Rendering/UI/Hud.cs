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



// sbar.h

// the status bar is only redrawn if something has changed, but if anything
// does, the entire thing will be redrawn for the next vid.numpages frames.

namespace SharpQuake.Rendering.UI
{
    using Desktop;
    using Engine.Host;
    using Framework;
    using Framework.Definitions;
    using Framework.Engine;
    using Framework.IO;
    using Renderer.Textures;
    using System.Text;

    /// <summary>
    /// Sbar_functions
    /// </summary>
    public class Hud
    {
        public int Lines
        {
            get; set;
        }

        public const int SBAR_HEIGHT = 24;

        private const int STAT_MINUS = 10;
        private int _Updates; // sb_updates		// if >= vid.numpages, no update needed
        private bool _ShowScores; // sb_showscores

        // num frame for '-' stats digit

        private BasePicture[,] Numbers = new BasePicture[2, 11];

        private BasePicture Colon;
        private BasePicture Slash;
        private BasePicture IBar;
        private BasePicture SBar;
        private BasePicture ScoreBar;

        private BasePicture[,] Weapons = new BasePicture[7, 8];   // 0 is active, 1 is owned, 2-5 are flashes
        private BasePicture[] Ammo = new BasePicture[4];
        private BasePicture[] Sigil = new BasePicture[4];
        private BasePicture[] Armour = new BasePicture[3];
        private BasePicture[] Items = new BasePicture[32];

        private BasePicture[,] Faces = new BasePicture[7, 2];        // 0 is gibbed, 1 is dead, 2-6 are alive

        // 0 is static, 1 is temporary animation

        private BasePicture FaceInvis;

        private BasePicture FaceQuad;
        private BasePicture FaceInvuln;
        private BasePicture FaceInvisInvuln;

        private BasePicture[] RInvBar = new BasePicture[2];
        private BasePicture[] RWeapons = new BasePicture[5];
        private BasePicture[] RItems = new BasePicture[2];
        private BasePicture[] RAmmo = new BasePicture[3];
        private BasePicture RTeamBord;      // PGM 01/19/97 - team color border

        //MED 01/04/97 added two more weapons + 3 alternates for grenade launcher
        private BasePicture[,] HWeapons = new BasePicture[7, 5];   // 0 is active, 1 is owned, 2-5 are flashes

        //MED 01/04/97 added array to simplify weapon parsing
        private int[] _HipWeapons = new int[]
        {
            QItemsDef.HIT_LASER_CANNON_BIT, QItemsDef.HIT_MJOLNIR_BIT, 4, QItemsDef.HIT_PROXIMITY_GUN_BIT
        };

        //MED 01/04/97 added hipnotic items array
        private BasePicture[] HItems = new BasePicture[2];

        private int[] _FragSort = new int[QDef.MAX_SCOREBOARD];
        private string[] _ScoreBoardText = new string[QDef.MAX_SCOREBOARD];
        private int[] _ScoreBoardTop = new int[QDef.MAX_SCOREBOARD];
        private int[] _ScoreBoardBottom = new int[QDef.MAX_SCOREBOARD];
        private int[] _ScoreBoardCount = new int[QDef.MAX_SCOREBOARD];
        private int _ScoreBoardLines;

        // CHANGE
        private Host Host
        {
            get;
            set;
        }
        // sb_lines scan lines to draw

        public Hud( Host host )
        {
            this.Host = host;
        }

        // Sbar_Init
        public void Initialise( )
        {
            for ( var i = 0; i < 10; i++ )
            {
                var str = i.ToString( );

                this.Numbers[0, i] = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, "num_" + str, "GL_NEAREST" );
                this.Numbers[1, i] = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, "anum_" + str, "GL_NEAREST" );
            }

            this.Numbers[0, 10] = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, "num_minus", "GL_NEAREST" );
            this.Numbers[1, 10] = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, "anum_minus", "GL_NEAREST" );

            this.Colon = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, "num_colon", "GL_NEAREST" );
            this.Slash = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, "num_slash", "GL_NEAREST" );

            this.Weapons[0, 0] = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, "inv_shotgun", "GL_LINEAR" );
            this.Weapons[0, 1] = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, "inv_sshotgun", "GL_LINEAR" );
            this.Weapons[0, 2] = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, "inv_nailgun", "GL_LINEAR" );
            this.Weapons[0, 3] = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, "inv_snailgun", "GL_LINEAR" );
            this.Weapons[0, 4] = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, "inv_rlaunch", "GL_LINEAR" );
            this.Weapons[0, 5] = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, "inv_srlaunch", "GL_LINEAR" );
            this.Weapons[0, 6] = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, "inv_lightng", "GL_LINEAR" );

            this.Weapons[1, 0] = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, "inv2_shotgun", "GL_LINEAR" );
            this.Weapons[1, 1] = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, "inv2_sshotgun", "GL_LINEAR" );
            this.Weapons[1, 2] = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, "inv2_nailgun", "GL_LINEAR" );
            this.Weapons[1, 3] = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, "inv2_snailgun", "GL_LINEAR" );
            this.Weapons[1, 4] = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, "inv2_rlaunch", "GL_LINEAR" );
            this.Weapons[1, 5] = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, "inv2_srlaunch", "GL_LINEAR" );
            this.Weapons[1, 6] = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, "inv2_lightng", "GL_LINEAR" );

            for ( var i = 0; i < 5; i++ )
            {
                var s = "inva" + ( i + 1 ).ToString( );

                this.Weapons[2 + i, 0] = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, s + "_shotgun", "GL_LINEAR" );
                this.Weapons[2 + i, 1] = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, s + "_sshotgun", "GL_LINEAR" );
                this.Weapons[2 + i, 2] = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, s + "_nailgun", "GL_LINEAR" );
                this.Weapons[2 + i, 3] = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, s + "_snailgun", "GL_LINEAR" );
                this.Weapons[2 + i, 4] = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, s + "_rlaunch", "GL_LINEAR" );
                this.Weapons[2 + i, 5] = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, s + "_srlaunch", "GL_LINEAR" );
                this.Weapons[2 + i, 6] = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, s + "_lightng", "GL_LINEAR" );
            }

            this.Ammo[0] = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, "sb_shells", "GL_LINEAR" );
            this.Ammo[1] = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, "sb_nails", "GL_LINEAR" );
            this.Ammo[2] = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, "sb_rocket", "GL_LINEAR" );
            this.Ammo[3] = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, "sb_cells", "GL_LINEAR" );

            this.Armour[0] = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, "sb_armor1", "GL_LINEAR" );
            this.Armour[1] = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, "sb_armor2", "GL_LINEAR" );
            this.Armour[2] = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, "sb_armor3", "GL_LINEAR" );

            this.Items[0] = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, "sb_key1", "GL_LINEAR" );
            this.Items[1] = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, "sb_key2", "GL_LINEAR" );
            this.Items[2] = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, "sb_invis", "GL_LINEAR" );
            this.Items[3] = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, "sb_invuln", "GL_LINEAR" );
            this.Items[4] = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, "sb_suit", "GL_LINEAR" );
            this.Items[5] = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, "sb_quad", "GL_LINEAR" );

            this.Sigil[0] = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, "sb_sigil1", "GL_LINEAR" );
            this.Sigil[1] = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, "sb_sigil2", "GL_LINEAR" );
            this.Sigil[2] = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, "sb_sigil3", "GL_LINEAR" );
            this.Sigil[3] = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, "sb_sigil4", "GL_LINEAR" );

            this.Faces[4, 0] = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, "face1", "GL_NEAREST" );
            this.Faces[4, 1] = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, "face_p1", "GL_NEAREST" );
            this.Faces[3, 0] = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, "face2", "GL_NEAREST" );
            this.Faces[3, 1] = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, "face_p2", "GL_NEAREST" );
            this.Faces[2, 0] = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, "face3", "GL_NEAREST" );
            this.Faces[2, 1] = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, "face_p3", "GL_NEAREST" );
            this.Faces[1, 0] = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, "face4", "GL_NEAREST" );
            this.Faces[1, 1] = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, "face_p4", "GL_NEAREST" );
            this.Faces[0, 0] = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, "face5", "GL_NEAREST" );
            this.Faces[0, 1] = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, "face_p5", "GL_NEAREST" );

            this.FaceInvis = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, "face_invis", "GL_NEAREST" );
            this.FaceInvuln = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, "face_invul2", "GL_NEAREST" );
            this.FaceInvisInvuln = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, "face_inv2", "GL_NEAREST" );
            this.FaceQuad = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, "face_quad", "GL_NEAREST" );

            this.Host.Commands.Add( "+showscores", this.ShowScores );
            this.Host.Commands.Add( "-showscores", this.DontShowScores );

            this.SBar = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, "sbar", "GL_NEAREST" );
            this.IBar = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, "ibar", "GL_NEAREST" );
            this.ScoreBar = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, "scorebar", "GL_LINEAR" );

            //MED 01/04/97 added new hipnotic weapons
            if ( MainWindow.Common.GameKind == GameKind.Hipnotic )
            {
                this.HWeapons[0, 0] = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, "inv_laser", "GL_LINEAR" );
                this.HWeapons[0, 1] = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, "inv_mjolnir", "GL_LINEAR" );
                this.HWeapons[0, 2] = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, "inv_gren_prox", "GL_LINEAR" );
                this.HWeapons[0, 3] = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, "inv_prox_gren", "GL_LINEAR" );
                this.HWeapons[0, 4] = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, "inv_prox", "GL_LINEAR" );

                this.HWeapons[1, 0] = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, "inv2_laser", "GL_LINEAR" );
                this.HWeapons[1, 1] = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, "inv2_mjolnir", "GL_LINEAR" );
                this.HWeapons[1, 2] = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, "inv2_gren_prox", "GL_LINEAR" );
                this.HWeapons[1, 3] = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, "inv2_prox_gren", "GL_LINEAR" );
                this.HWeapons[1, 4] = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, "inv2_prox", "GL_LINEAR" );

                for ( var i = 0; i < 5; i++ )
                {
                    var s = "inva" + ( i + 1 ).ToString( );
                    this.HWeapons[2 + i, 0] = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, s + "_laser", "GL_LINEAR" );
                    this.HWeapons[2 + i, 1] = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, s + "_mjolnir", "GL_LINEAR" );
                    this.HWeapons[2 + i, 2] = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, s + "_gren_prox", "GL_LINEAR" );
                    this.HWeapons[2 + i, 3] = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, s + "_prox_gren", "GL_LINEAR" );
                    this.HWeapons[2 + i, 4] = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, s + "_prox", "GL_LINEAR" );
                }

                this.HItems[0] = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, "sb_wsuit", "GL_LINEAR" );
                this.HItems[1] = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, "sb_eshld", "GL_LINEAR" );
            }

            if ( MainWindow.Common.GameKind == GameKind.Rogue )
            {
                this.RInvBar[0] = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, "r_invbar1", "GL_LINEAR" );
                this.RInvBar[1] = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, "r_invbar2", "GL_LINEAR" );

                this.RWeapons[0] = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, "r_lava", "GL_LINEAR" );
                this.RWeapons[1] = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, "r_superlava", "GL_LINEAR" );
                this.RWeapons[2] = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, "r_gren", "GL_LINEAR" );
                this.RWeapons[3] = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, "r_multirock", "GL_LINEAR" );
                this.RWeapons[4] = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, "r_plasma", "GL_LINEAR" );

                this.RItems[0] = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, "r_shield1", "GL_LINEAR" );
                this.RItems[1] = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, "r_agrav1", "GL_LINEAR" );

                // PGM 01/19/97 - team color border
                this.RTeamBord = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, "r_teambord", "GL_LINEAR" );
                // PGM 01/19/97 - team color border

                this.RAmmo[0] = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, "r_ammolava", "GL_LINEAR" );
                this.RAmmo[1] = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, "r_ammomulti", "GL_LINEAR" );
                this.RAmmo[2] = BasePicture.FromWad(this.Host.Video.Device, this.Host.GfxWad, "r_ammoplasma", "GL_LINEAR" );
            }
        }

        // Sbar_Changed
        // call whenever any of the client stats represented on the sbar changes
        public void Changed( )
        {
            this._Updates = 0;	// update next frame
        }

        // Sbar_Draw
        // called every frame by screen
        public void Draw( )
        {
            if (this.Host == null )
                return;

            var vid = this.Host.Screen.vid;
            if (this.Host.Screen.ConCurrent == vid.height )
                return;		// console is full screen

            if (this._Updates >= vid.numpages )
                return;

            this.Host.Screen.CopyEverithing = true;

            this._Updates++;

            if (this.Lines > 0 && vid.width > 320 )
                this.Host.DrawingContext.TileClear( 0, vid.height - this.Lines, vid.width, this.Lines );

            if (this.Lines > 24 )
            {
                this.DrawInventory( );
                if (this.Host.Client.cl.maxclients != 1 )
                    this.DrawFrags( );
            }

            var cl = this.Host.Client.cl;
            if (this._ShowScores || cl.stats[QStatsDef.STAT_HEALTH] <= 0 )
            {
                this.DrawPic( 0, 0, this.ScoreBar );
                this.DrawScoreboard( );
                this._Updates = 0;
            }
            else if (this.Lines > 0 )
            {
                this.DrawPic( 0, 0, this.SBar );

                // keys (hipnotic only)
                //MED 01/04/97 moved keys here so they would not be overwritten
                if ( MainWindow.Common.GameKind == GameKind.Hipnotic )
                {
                    if ( cl.HasItems( QItemsDef.IT_KEY1 ) )
                        this.DrawPic( 209, 3, this.Items[0] );
                    if ( cl.HasItems( QItemsDef.IT_KEY2 ) )
                        this.DrawPic( 209, 12, this.Items[1] );
                }
                // armor
                if ( cl.HasItems( QItemsDef.IT_INVULNERABILITY ) )
                {
                    this.DrawNum( 24, 0, 666, 3, 1 );
                    this.Host.Video.Device.Graphics.DrawPicture(this.Host.DrawingContext.Disc, 0, 0 );
                }
                else
                {
                    if ( MainWindow.Common.GameKind == GameKind.Rogue )
                    {
                        this.DrawNum( 24, 0, cl.stats[QStatsDef.STAT_ARMOR], 3, cl.stats[QStatsDef.STAT_ARMOR] <= 25 ? 1 : 0 ); // uze: corrected color param
                        if ( cl.HasItems( QItemsDef.RIT_ARMOR3 ) )
                            this.DrawPic( 0, 0, this.Armour[2] );
                        else if ( cl.HasItems( QItemsDef.RIT_ARMOR2 ) )
                            this.DrawPic( 0, 0, this.Armour[1] );
                        else if ( cl.HasItems( QItemsDef.RIT_ARMOR1 ) )
                            this.DrawPic( 0, 0, this.Armour[0] );
                    }
                    else
                    {
                        this.DrawNum( 24, 0, cl.stats[QStatsDef.STAT_ARMOR], 3, cl.stats[QStatsDef.STAT_ARMOR] <= 25 ? 1 : 0 );
                        if ( cl.HasItems( QItemsDef.IT_ARMOR3 ) )
                            this.DrawPic( 0, 0, this.Armour[2] );
                        else if ( cl.HasItems( QItemsDef.IT_ARMOR2 ) )
                            this.DrawPic( 0, 0, this.Armour[1] );
                        else if ( cl.HasItems( QItemsDef.IT_ARMOR1 ) )
                            this.DrawPic( 0, 0, this.Armour[0] );
                    }
                }

                // face
                this.DrawFace( );

                // health
                this.DrawNum( 136, 0, cl.stats[QStatsDef.STAT_HEALTH], 3, cl.stats[QStatsDef.STAT_HEALTH] <= 25 ? 1 : 0 );

                // ammo icon
                if ( MainWindow.Common.GameKind == GameKind.Rogue )
                {
                    if ( cl.HasItems( QItemsDef.RIT_SHELLS ) )
                        this.DrawPic( 224, 0, this.Ammo[0] );
                    else if ( cl.HasItems( QItemsDef.RIT_NAILS ) )
                        this.DrawPic( 224, 0, this.Ammo[1] );
                    else if ( cl.HasItems( QItemsDef.RIT_ROCKETS ) )
                        this.DrawPic( 224, 0, this.Ammo[2] );
                    else if ( cl.HasItems( QItemsDef.RIT_CELLS ) )
                        this.DrawPic( 224, 0, this.Ammo[3] );
                    else if ( cl.HasItems( QItemsDef.RIT_LAVA_NAILS ) )
                        this.DrawPic( 224, 0, this.RAmmo[0] );
                    else if ( cl.HasItems( QItemsDef.RIT_PLASMA_AMMO ) )
                        this.DrawPic( 224, 0, this.RAmmo[1] );
                    else if ( cl.HasItems( QItemsDef.RIT_MULTI_ROCKETS ) )
                        this.DrawPic( 224, 0, this.RAmmo[2] );
                }
                else
                {
                    if ( cl.HasItems( QItemsDef.IT_SHELLS ) )
                        this.DrawPic( 224, 0, this.Ammo[0] );
                    else if ( cl.HasItems( QItemsDef.IT_NAILS ) )
                        this.DrawPic( 224, 0, this.Ammo[1] );
                    else if ( cl.HasItems( QItemsDef.IT_ROCKETS ) )
                        this.DrawPic( 224, 0, this.Ammo[2] );
                    else if ( cl.HasItems( QItemsDef.IT_CELLS ) )
                        this.DrawPic( 224, 0, this.Ammo[3] );
                }

                this.DrawNum( 248, 0, cl.stats[QStatsDef.STAT_AMMO], 3, cl.stats[QStatsDef.STAT_AMMO] <= 10 ? 1 : 0 );
            }

            if ( vid.width > 320 )
            {
                if (this.Host.Client.cl.gametype == ProtocolDef.GAME_DEATHMATCH )
                    this.MiniDeathmatchOverlay( );
            }
        }

        /// <summary>
        /// Sbar_IntermissionOverlay
        /// called each frame after the level has been completed
        /// </summary>
        public void IntermissionOverlay( )
        {
            this.Host.Screen.CopyEverithing = true;
            this.Host.Screen.FullUpdate = 0;

            if (this.Host.Client.cl.gametype == ProtocolDef.GAME_DEATHMATCH )
            {
                this.DeathmatchOverlay( );
                return;
            }

            var pic = this.Host.DrawingContext.CachePic( "gfx/complete.lmp", "GL_LINEAR" );
            this.Host.Video.Device.Graphics.DrawPicture( pic, 64, 24 );

            pic = this.Host.DrawingContext.CachePic( "gfx/inter.lmp", "GL_LINEAR" );
            this.Host.Video.Device.Graphics.DrawPicture( pic, 0, 56, hasAlpha: true );

            // time
            var dig = this.Host.Client.cl.completed_time / 60;
            this.IntermissionNumber( 160, 64, dig, 3, 0 );
            var num = this.Host.Client.cl.completed_time - dig * 60;

            this.Host.Video.Device.Graphics.DrawPicture(this.Colon, 234, 64, hasAlpha: true );

            this.Host.Video.Device.Graphics.DrawPicture(this.Numbers[0, num / 10], 246, 64, hasAlpha: true );
            this.Host.Video.Device.Graphics.DrawPicture(this.Numbers[0, num % 10], 266, 64, hasAlpha: true );

            this.IntermissionNumber( 160, 104, this.Host.Client.cl.stats[QStatsDef.STAT_SECRETS], 3, 0 );
            this.Host.Video.Device.Graphics.DrawPicture(this.Slash, 232, 104, hasAlpha: true );
            this.IntermissionNumber( 240, 104, this.Host.Client.cl.stats[QStatsDef.STAT_TOTALSECRETS], 3, 0 );

            this.IntermissionNumber( 160, 144, this.Host.Client.cl.stats[QStatsDef.STAT_MONSTERS], 3, 0 );
            this.Host.Video.Device.Graphics.DrawPicture(this.Slash, 232, 144, hasAlpha: true );
            this.IntermissionNumber( 240, 144, this.Host.Client.cl.stats[QStatsDef.STAT_TOTALMONSTERS], 3, 0 );
        }

        /// <summary>
        /// Sbar_FinaleOverlay
        /// </summary>
        public void FinaleOverlay( )
        {
            this.Host.Screen.CopyEverithing = true;

            var pic = this.Host.DrawingContext.CachePic( "gfx/finale.lmp", "GL_LINEAR" );
            this.Host.Video.Device.Graphics.DrawPicture( pic, (this.Host.Screen.vid.width - pic.Width ) / 2, 16, hasAlpha: true );
        }

        /// <summary>
        /// Sbar_IntermissionNumber
        /// </summary>
        private void IntermissionNumber( int x, int y, int num, int digits, int color )
        {
            var str = num.ToString( );
            if ( str.Length > digits )
                str = str.Remove( 0, str.Length - digits );

            if ( str.Length < digits )
                x += ( digits - str.Length ) * 24;

            for ( var i = 0; i < str.Length; i++ )
            {
                var frame = str[i] == '-' ? Hud.STAT_MINUS : str[i] - '0';

                this.Host.Video.Device.Graphics.DrawPicture(this.Numbers[color, frame], x, y, hasAlpha: true );

                //Host.DrawingContext.DrawTransPic( x, y, _Nums[color, frame] );
                x += 24;
            }
        }

        // Sbar_DrawInventory
        private void DrawInventory( )
        {
            int flashon;

            var cl = this.Host.Client.cl;
            if ( MainWindow.Common.GameKind == GameKind.Rogue )
            {
                if ( cl.stats[QStatsDef.STAT_ACTIVEWEAPON] >= QItemsDef.RIT_LAVA_NAILGUN )
                    this.DrawPic( 0, -24, this.RInvBar[0] );
                else
                    this.DrawPic( 0, -24, this.RInvBar[1] );
            }
            else
                this.DrawPic( 0, -24, this.IBar );

            // weapons
            for ( var i = 0; i < 7; i++ )
            {
                if ( cl.HasItems( QItemsDef.IT_SHOTGUN << i ) )
                {
                    var time = cl.item_gettime[i];
                    flashon = ( int ) ( ( cl.time - time ) * 10 );
                    if ( flashon >= 10 )
                    {
                        if ( cl.stats[QStatsDef.STAT_ACTIVEWEAPON] == QItemsDef.IT_SHOTGUN << i )
                            flashon = 1;
                        else
                            flashon = 0;
                    }
                    else
                        flashon = flashon % 5 + 2;

                    this.DrawPic( i * 24, -16, this.Weapons[flashon, i] );

                    if ( flashon > 1 )
                        this._Updates = 0; // force update to remove flash
                }
            }

            // MED 01/04/97
            // hipnotic weapons
            if ( MainWindow.Common.GameKind == GameKind.Hipnotic )
            {
                var grenadeflashing = 0;
                for ( var i = 0; i < 4; i++ )
                {
                    if ( cl.HasItems( 1 << this._HipWeapons[i] ) )
                    {
                        var time = cl.item_gettime[this._HipWeapons[i]];
                        flashon = ( int ) ( ( cl.time - time ) * 10 );
                        if ( flashon >= 10 )
                        {
                            if ( cl.stats[QStatsDef.STAT_ACTIVEWEAPON] == 1 << this._HipWeapons[i] )
                                flashon = 1;
                            else
                                flashon = 0;
                        }
                        else
                            flashon = flashon % 5 + 2;

                        // check grenade launcher
                        if ( i == 2 )
                        {
                            if ( cl.HasItems( QItemsDef.HIT_PROXIMITY_GUN ) )
                            {
                                if ( flashon > 0 )
                                {
                                    grenadeflashing = 1;
                                    this.DrawPic( 96, -16, this.HWeapons[flashon, 2] );
                                }
                            }
                        }
                        else if ( i == 3 )
                        {
                            if ( cl.HasItems( QItemsDef.IT_SHOTGUN << 4 ) )
                            {
                                if ( flashon > 0 && grenadeflashing == 0 )
                                    this.DrawPic( 96, -16, this.HWeapons[flashon, 3] );
                                else if ( grenadeflashing == 0 )
                                    this.DrawPic( 96, -16, this.HWeapons[0, 3] );
                            }
                            else
                                this.DrawPic( 96, -16, this.HWeapons[flashon, 4] );
                        }
                        else
                            this.DrawPic( 176 + i * 24, -16, this.HWeapons[flashon, i] );
                        if ( flashon > 1 )
                            this._Updates = 0; // force update to remove flash
                    }
                }
            }

            if ( MainWindow.Common.GameKind == GameKind.Rogue )
            {
                // check for powered up weapon.
                if ( cl.stats[QStatsDef.STAT_ACTIVEWEAPON] >= QItemsDef.RIT_LAVA_NAILGUN )
                {
                    for ( var i = 0; i < 5; i++ )
                    {
                        if ( cl.stats[QStatsDef.STAT_ACTIVEWEAPON] == QItemsDef.RIT_LAVA_NAILGUN << i )
                            this.DrawPic( ( i + 2 ) * 24, -16, this.RWeapons[i] );
                    }
                }
            }

            // ammo counts
            for ( var i = 0; i < 4; i++ )
            {
                var num = cl.stats[QStatsDef.STAT_SHELLS + i].ToString( ).PadLeft( 3 );
                //sprintf(num, "%3i", cl.stats[QStats.STAT_SHELLS + i]);
                if ( num[0] != ' ' )
                    this.DrawCharacter( ( 6 * i + 1 ) * 8 - 2, -24, 18 + num[0] - '0' );
                if ( num[1] != ' ' )
                    this.DrawCharacter( ( 6 * i + 2 ) * 8 - 2, -24, 18 + num[1] - '0' );
                if ( num[2] != ' ' )
                    this.DrawCharacter( ( 6 * i + 3 ) * 8 - 2, -24, 18 + num[2] - '0' );
            }

            flashon = 0;
            // items
            for ( var i = 0; i < 6; i++ )
            {
                if ( cl.HasItems( 1 << ( 17 + i ) ) )
                {
                    var time = cl.item_gettime[17 + i];
                    if ( time > 0 && time > cl.time - 2 && flashon > 0 )
                    {  // flash frame
                        this._Updates = 0;
                    }
                    else
                    {
                        //MED 01/04/97 changed keys
                        if ( MainWindow.Common.GameKind != GameKind.Hipnotic || i > 1 )
                            this.DrawPic( 192 + i * 16, -16, this.Items[i] );
                    }
                    if ( time > 0 && time > cl.time - 2 )
                        this._Updates = 0;
                }
            }

            //MED 01/04/97 added hipnotic items
            // hipnotic items
            if ( MainWindow.Common.GameKind == GameKind.Hipnotic )
            {
                for ( var i = 0; i < 2; i++ )
                {
                    if ( cl.HasItems( 1 << ( 24 + i ) ) )
                    {
                        var time = cl.item_gettime[24 + i];
                        if ( time > 0 && time > cl.time - 2 && flashon > 0 )
                        {  // flash frame
                            this._Updates = 0;
                        }
                        else
                            this.DrawPic( 288 + i * 16, -16, this.HItems[i] );

                        if ( time > 0 && time > cl.time - 2 )
                            this._Updates = 0;
                    }
                }
            }

            if ( MainWindow.Common.GameKind == GameKind.Rogue )
            {
                // new rogue items
                for ( var i = 0; i < 2; i++ )
                {
                    if ( cl.HasItems( 1 << ( 29 + i ) ) )
                    {
                        var time = cl.item_gettime[29 + i];

                        if ( time > 0 && time > cl.time - 2 && flashon > 0 )
                        {	// flash frame
                            this._Updates = 0;
                        }
                        else
                            this.DrawPic( 288 + i * 16, -16, this.RItems[i] );

                        if ( time > 0 && time > cl.time - 2 )
                            this._Updates = 0;
                    }
                }
            }
            else
            {
                // sigils
                for ( var i = 0; i < 4; i++ )
                {
                    if ( cl.HasItems( 1 << ( 28 + i ) ) )
                    {
                        var time = cl.item_gettime[28 + i];
                        if ( time > 0 && time > cl.time - 2 && flashon > 0 )
                        {	// flash frame
                            this._Updates = 0;
                        }
                        else
                            this.DrawPic( 320 - 32 + i * 8, -16, this.Sigil[i] );
                        if ( time > 0 && time > cl.time - 2 )
                            this._Updates = 0;
                    }
                }
            }
        }

        // Sbar_DrawFrags
        private void DrawFrags( )
        {
            this.SortFrags( );

            // draw the text
            var l = this._ScoreBoardLines <= 4 ? this._ScoreBoardLines : 4;
            int xofs, x = 23;
            var cl = this.Host.Client.cl;

            if ( cl.gametype == ProtocolDef.GAME_DEATHMATCH )
                xofs = 0;
            else
                xofs = (this.Host.Screen.vid.width - 320 ) >> 1;

            var y = this.Host.Screen.vid.height - Hud.SBAR_HEIGHT - 23;

            for ( var i = 0; i < l; i++ )
            {
                var k = this._FragSort[i];
                var s = cl.scores[k];
                if ( string.IsNullOrEmpty( s.name ) )
                    continue;

                // draw background
                var top = s.colors & 0xf0;
                var bottom = ( s.colors & 15 ) << 4;
                top = this.ColorForMap( top );
                bottom = this.ColorForMap( bottom );

                this.Host.Video.Device.Graphics.FillUsingPalette( xofs + x * 8 + 10, y, 28, 4, top );
                this.Host.Video.Device.Graphics.FillUsingPalette( xofs + x * 8 + 10, y + 4, 28, 3, bottom );

                // draw number
                var f = s.frags;
                var num = f.ToString( ).PadLeft( 3 );
                //sprintf(num, "%3i", f);

                this.DrawCharacter( ( x + 1 ) * 8, -24, num[0] );
                this.DrawCharacter( ( x + 2 ) * 8, -24, num[1] );
                this.DrawCharacter( ( x + 3 ) * 8, -24, num[2] );

                if ( k == cl.viewentity - 1 )
                {
                    this.DrawCharacter( x * 8 + 2, -24, 16 );
                    this.DrawCharacter( ( x + 4 ) * 8 - 4, -24, 17 );
                }
                x += 4;
            }
        }

        // Sbar_DrawPic
        private void DrawPic( int x, int y, BasePicture pic )
        {
            if (this.Host.Client.cl.gametype == ProtocolDef.GAME_DEATHMATCH )
                this.Host.Video.Device.Graphics.DrawPicture( pic, x, y + (this.Host.Screen.vid.height - Hud.SBAR_HEIGHT ) );
            else
                this.Host.Video.Device.Graphics.DrawPicture( pic, x + ( (this.Host.Screen.vid.width - 320 ) >> 1 ), y + (this.Host.Screen.vid.height - Hud.SBAR_HEIGHT ) );
        }

        // Sbar_DrawScoreboard
        private void DrawScoreboard( )
        {
            this.SoloScoreboard( );
            if (this.Host.Client.cl.gametype == ProtocolDef.GAME_DEATHMATCH )
                this.DeathmatchOverlay( );
        }

        // Sbar_DrawNum
        private void DrawNum( int x, int y, int num, int digits, int color )
        {
            var str = num.ToString( );// int l = Sbar_itoa(num, str);

            if ( str.Length > digits )
                str = str.Remove( str.Length - digits );
            if ( str.Length < digits )
                x += ( digits - str.Length ) * 24;

            for ( int i = 0, frame; i < str.Length; i++ )
            {
                if ( str[i] == '-' )
                    frame = Hud.STAT_MINUS;
                else
                    frame = str[i] - '0';

                this.DrawTransPic( x, y, this.Numbers[color, frame] );
                x += 24;
            }
        }

        // Sbar_DrawFace
        private void DrawFace( )
        {
            var cl = this.Host.Client.cl;

            // PGM 01/19/97 - team color drawing
            // PGM 03/02/97 - fixed so color swatch only appears in CTF modes
            if ( MainWindow.Common.GameKind == GameKind.Rogue &&
                this.Host.Client.cl.maxclients != 1 &&
                this.Host.Cvars.TeamPlay.Get<int>( ) > 3 &&
                this.Host.Cvars.TeamPlay.Get<int>( ) < 7 )
            {
                var s = cl.scores[cl.viewentity - 1];

                // draw background
                var top = s.colors & 0xf0;
                var bottom = ( s.colors & 15 ) << 4;
                top = this.ColorForMap( top );
                bottom = this.ColorForMap( bottom );

                int xofs;
                if ( cl.gametype == ProtocolDef.GAME_DEATHMATCH )
                    xofs = 113;
                else
                    xofs = ( (this.Host.Screen.vid.width - 320 ) >> 1 ) + 113;

                this.DrawPic( 112, 0, this.RTeamBord );
                this.Host.Video.Device.Graphics.FillUsingPalette( xofs, this.Host.Screen.vid.height - Hud.SBAR_HEIGHT + 3, 22, 9, top );
                this.Host.Video.Device.Graphics.FillUsingPalette( xofs, this.Host.Screen.vid.height - Hud.SBAR_HEIGHT + 12, 22, 9, bottom );

                // draw number
                var num = s.frags.ToString( ).PadLeft( 3 );
                if ( top == 8 )
                {
                    if ( num[0] != ' ' )
                        this.DrawCharacter( 109, 3, 18 + num[0] - '0' );
                    if ( num[1] != ' ' )
                        this.DrawCharacter( 116, 3, 18 + num[1] - '0' );
                    if ( num[2] != ' ' )
                        this.DrawCharacter( 123, 3, 18 + num[2] - '0' );
                }
                else
                {
                    this.DrawCharacter( 109, 3, num[0] );
                    this.DrawCharacter( 116, 3, num[1] );
                    this.DrawCharacter( 123, 3, num[2] );
                }

                return;
            }
            // PGM 01/19/97 - team color drawing

            int f, anim;

            if ( cl.HasItems( QItemsDef.IT_INVISIBILITY | QItemsDef.IT_INVULNERABILITY ) )
            {
                this.DrawPic( 112, 0, this.FaceInvisInvuln );
                return;
            }
            if ( cl.HasItems( QItemsDef.IT_QUAD ) )
            {
                this.DrawPic( 112, 0, this.FaceQuad );
                return;
            }
            if ( cl.HasItems( QItemsDef.IT_INVISIBILITY ) )
            {
                this.DrawPic( 112, 0, this.FaceInvis );
                return;
            }
            if ( cl.HasItems( QItemsDef.IT_INVULNERABILITY ) )
            {
                this.DrawPic( 112, 0, this.FaceInvuln );
                return;
            }

            if ( cl.stats[QStatsDef.STAT_HEALTH] >= 100 )
                f = 4;
            else
                f = cl.stats[QStatsDef.STAT_HEALTH] / 20;

            if ( cl.time <= cl.faceanimtime )
            {
                anim = 1;
                this._Updates = 0; // make sure the anim gets drawn over
            }
            else
                anim = 0;

            this.DrawPic( 112, 0, this.Faces[f, anim] );
        }

        // Sbar_DeathmatchOverlay
        private void MiniDeathmatchOverlay( )
        {
            if (this.Host.Screen.vid.width < 512 || this.Lines == 0 )
                return;

            this.Host.Screen.CopyEverithing = true;
            this.Host.Screen.FullUpdate = 0;

            // scores
            this.SortFrags( );

            // draw the text
            var l = this._ScoreBoardLines;
            var y = this.Host.Screen.vid.height - this.Lines;
            var numlines = this.Lines / 8;
            if ( numlines < 3 )
                return;

            //find us
            int i;
            for ( i = 0; i < this._ScoreBoardLines; i++ )
            {
                if (this._FragSort[i] == this.Host.Client.cl.viewentity - 1 )
                    break;
            }

            if ( i == this._ScoreBoardLines ) // we're not there
                i = 0;
            else // figure out start
                i = i - numlines / 2;

            if ( i > this._ScoreBoardLines - numlines )
                i = this._ScoreBoardLines - numlines;
            if ( i < 0 )
                i = 0;

            var x = 324;
            for ( ; i < this._ScoreBoardLines && y < this.Host.Screen.vid.height - 8; i++ )
            {
                var k = this._FragSort[i];
                var s = this.Host.Client.cl.scores[k];
                if ( string.IsNullOrEmpty( s.name ) )
                    continue;

                // draw background
                var top = s.colors & 0xf0;
                var bottom = ( s.colors & 15 ) << 4;
                top = this.ColorForMap( top );
                bottom = this.ColorForMap( bottom );

                this.Host.Video.Device.Graphics.FillUsingPalette( x, y + 1, 40, 3, top );
                this.Host.Video.Device.Graphics.FillUsingPalette( x, y + 4, 40, 4, bottom );

                // draw number
                var num = s.frags.ToString( ).PadLeft( 3 );
                this.Host.DrawingContext.DrawCharacter( x + 8, y, num[0] );
                this.Host.DrawingContext.DrawCharacter( x + 16, y, num[1] );
                this.Host.DrawingContext.DrawCharacter( x + 24, y, num[2] );

                if ( k == this.Host.Client.cl.viewentity - 1 )
                {
                    this.Host.DrawingContext.DrawCharacter( x, y, 16 );
                    this.Host.DrawingContext.DrawCharacter( x + 32, y, 17 );
                }

                // draw name
                this.Host.DrawingContext.DrawString( x + 48, y, s.name );

                y += 8;
            }
        }

        // Sbar_SortFrags
        private void SortFrags( )
        {
            var cl = this.Host.Client.cl;

            // sort by frags
            this._ScoreBoardLines = 0;
            for ( var i = 0; i < cl.maxclients; i++ )
            {
                if ( !string.IsNullOrEmpty( cl.scores[i].name ) )
                {
                    this._FragSort[this._ScoreBoardLines] = i;
                    this._ScoreBoardLines++;
                }
            }

            for ( var i = 0; i < this._ScoreBoardLines; i++ )
            {
                for ( var j = 0; j < this._ScoreBoardLines - 1 - i; j++ )
                {
                    if ( cl.scores[this._FragSort[j]].frags < cl.scores[this._FragSort[j + 1]].frags )
                    {
                        var k = this._FragSort[j];
                        this._FragSort[j] = this._FragSort[j + 1];
                        this._FragSort[j + 1] = k;
                    }
                }
            }
        }

        // Sbar_DrawCharacter
        //
        // Draws one solid graphics character
        private void DrawCharacter( int x, int y, int num )
        {
            if (this.Host.Client.cl.gametype == ProtocolDef.GAME_DEATHMATCH )
                this.Host.DrawingContext.DrawCharacter( x + 4, y + this.Host.Screen.vid.height - Hud.SBAR_HEIGHT, num );
            else
                this.Host.DrawingContext.DrawCharacter( x + ( (this.Host.Screen.vid.width - 320 ) >> 1 ) + 4, y + this.Host.Screen.vid.height - Hud.SBAR_HEIGHT, num );
        }

        // Sbar_ColorForMap
        private int ColorForMap( int m )
        {
            return m < 128 ? m + 8 : m + 8;
        }

        // Sbar_SoloScoreboard
        private void SoloScoreboard( )
        {
            var sb = new StringBuilder( 80 );
            var cl = this.Host.Client.cl;

            sb.AppendFormat( "Monsters:{0,3:d} /{1,3:d}", cl.stats[QStatsDef.STAT_MONSTERS], this.Host.Client.cl.stats[QStatsDef.STAT_TOTALMONSTERS] );
            this.DrawString( 8, 4, sb.ToString( ) );

            sb.Length = 0;
            sb.AppendFormat( "Secrets :{0,3:d} /{1,3:d}", cl.stats[QStatsDef.STAT_SECRETS], cl.stats[QStatsDef.STAT_TOTALSECRETS] );
            this.DrawString( 8, 12, sb.ToString( ) );

            // time
            var minutes = ( int ) ( cl.time / 60.0 );
            var seconds = ( int ) ( cl.time - 60 * minutes );
            var tens = seconds / 10;
            var units = seconds - 10 * tens;
            sb.Length = 0;
            sb.AppendFormat( "Time :{0,3}:{1}{2}", minutes, tens, units );
            this.DrawString( 184, 4, sb.ToString( ) );

            // draw level name
            var l = cl.levelname.Length;
            this.DrawString( 232 - l * 4, 12, cl.levelname );
        }

        // Sbar_DeathmatchOverlay
        private void DeathmatchOverlay( )
        {
            this.Host.Screen.CopyEverithing = true;
            this.Host.Screen.FullUpdate = 0;

            var pic = this.Host.DrawingContext.CachePic( "gfx/ranking.lmp", "GL_LINEAR" );
            this.Host.Video.Device.Graphics.DrawPicture( pic, ( 320 - pic.Width ) / 2, 8 );

            // scores
            this.SortFrags( );

            // draw the text
            var l = this._ScoreBoardLines;

            var x = 80 + ( (this.Host.Screen.vid.width - 320 ) >> 1 );
            var y = 40;
            for ( var i = 0; i < l; i++ )
            {
                var k = this._FragSort[i];
                var s = this.Host.Client.cl.scores[k];
                if ( string.IsNullOrEmpty( s.name ) )
                    continue;

                // draw background
                var top = s.colors & 0xf0;
                var bottom = ( s.colors & 15 ) << 4;
                top = this.ColorForMap( top );
                bottom = this.ColorForMap( bottom );

                this.Host.Video.Device.Graphics.FillUsingPalette( x, y, 40, 4, top );
                this.Host.Video.Device.Graphics.FillUsingPalette( x, y + 4, 40, 4, bottom );

                // draw number
                var num = s.frags.ToString( ).PadLeft( 3 );

                this.Host.DrawingContext.DrawCharacter( x + 8, y, num[0] );
                this.Host.DrawingContext.DrawCharacter( x + 16, y, num[1] );
                this.Host.DrawingContext.DrawCharacter( x + 24, y, num[2] );

                if ( k == this.Host.Client.cl.viewentity - 1 )
                    this.Host.DrawingContext.DrawCharacter( x - 8, y, 12 );

                // draw name
                this.Host.DrawingContext.DrawString( x + 64, y, s.name );

                y += 10;
            }
        }

        // Sbar_DrawTransPic
        private void DrawTransPic( int x, int y, BasePicture picture )
        {
            if (this.Host.Client.cl.gametype == ProtocolDef.GAME_DEATHMATCH )
                this.Host.Video.Device.Graphics.DrawPicture( picture, x, y + (this.Host.Screen.vid.height - Hud.SBAR_HEIGHT ), hasAlpha: true );
            else
                this.Host.Video.Device.Graphics.DrawPicture( picture, x + ( (this.Host.Screen.vid.width - 320 ) >> 1 ), y + (this.Host.Screen.vid.height - Hud.SBAR_HEIGHT ), hasAlpha: true );
        }

        // Sbar_DrawString
        private void DrawString( int x, int y, string str )
        {
            if (this.Host.Client.cl.gametype == ProtocolDef.GAME_DEATHMATCH )
                this.Host.DrawingContext.DrawString( x, y + this.Host.Screen.vid.height - Hud.SBAR_HEIGHT, str );
            else
                this.Host.DrawingContext.DrawString( x + ( (this.Host.Screen.vid.width - 320 ) >> 1 ), y + this.Host.Screen.vid.height - Hud.SBAR_HEIGHT, str );
        }

        // Sbar_ShowScores
        //
        // Tab key down
        private void ShowScores( CommandMessage msg )
        {
            if (this._ShowScores )
                return;

            this._ShowScores = true;
            this._Updates = 0;
        }

        // Sbar_DontShowScores
        //
        // Tab key up
        private void DontShowScores( CommandMessage msg )
        {
            this._ShowScores = false;
            this._Updates = 0;
        }
    }
}
