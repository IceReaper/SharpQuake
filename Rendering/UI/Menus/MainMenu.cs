﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpQuake.Framework;

namespace SharpQuake
{
    /// <summary>
    /// MainMenu
    /// </summary>
    public class MainMenu : MenuBase
    {
        private const Int32 MAIN_ITEMS = 5;
        private Int32 _SaveDemoNum;

        public override void Show( )
        {
            if ( Key.Destination != keydest_t.key_menu )
            {
                _SaveDemoNum = client.cls.demonum;
                client.cls.demonum = -1;
            }

            base.Show( );
        }

        /// <summary>
        /// M_Main_Key
        /// </summary>
        public override void KeyEvent( Int32 key )
        {
            switch ( key )
            {
                case Key.K_ESCAPE:
                    //Key.Destination = keydest_t.key_game;
                    MenuBase.Hide( );
                    client.cls.demonum = _SaveDemoNum;
                    if ( client.cls.demonum != -1 && !client.cls.demoplayback && client.cls.state != cactive_t.ca_connected )
                        client.NextDemo( );
                    break;

                case Key.K_DOWNARROW:
                    snd.LocalSound( "misc/menu1.wav" );
                    if ( ++_Cursor >= MAIN_ITEMS )
                        _Cursor = 0;
                    break;

                case Key.K_UPARROW:
                    snd.LocalSound( "misc/menu1.wav" );
                    if ( --_Cursor < 0 )
                        _Cursor = MAIN_ITEMS - 1;
                    break;

                case Key.K_ENTER:
                    Menu.EnterSound = true;

                    switch ( _Cursor )
                    {
                        case 0:
                            MenuBase.SinglePlayerMenu.Show( );
                            break;

                        case 1:
                            MenuBase.MultiPlayerMenu.Show( );
                            break;

                        case 2:
                            MenuBase.OptionsMenu.Show( );
                            break;

                        case 3:
                            MenuBase.HelpMenu.Show( );
                            break;

                        case 4:
                            MenuBase.QuitMenu.Show( );
                            break;
                    }
                    break;
            }
        }

        public override void Draw( )
        {
            Menu.DrawTransPic( 16, 4, Drawer.CachePic( "gfx/qplaque.lmp" ) );
            GLPic p = Drawer.CachePic( "gfx/ttl_main.lmp" );
            Menu.DrawPic( ( 320 - p.width ) / 2, 4, p );
            Menu.DrawTransPic( 72, 32, Drawer.CachePic( "gfx/mainmenu.lmp" ) );

            var f = ( Int32 ) ( host.Time * 10 ) % 6;

            Menu.DrawTransPic( 54, 32 + _Cursor * 20, Drawer.CachePic( String.Format( "gfx/menudot{0}.lmp", f + 1 ) ) );
        }
    }
}
