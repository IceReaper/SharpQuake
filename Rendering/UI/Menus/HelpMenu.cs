﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpQuake
{
    public class HelpMenu : MenuBase
    {
        private const Int32 NUM_HELP_PAGES = 6;

        private Int32 _Page;

        public override void Show( )
        {
            _Page = 0;
            base.Show( );
        }

        public override void KeyEvent( Int32 key )
        {
            switch ( key )
            {
                case Key.K_ESCAPE:
                    MenuBase.MainMenu.Show( );
                    break;

                case Key.K_UPARROW:
                case Key.K_RIGHTARROW:
                    Menu.EnterSound = true;
                    if ( ++_Page >= NUM_HELP_PAGES )
                        _Page = 0;
                    break;

                case Key.K_DOWNARROW:
                case Key.K_LEFTARROW:
                    Menu.EnterSound = true;
                    if ( --_Page < 0 )
                        _Page = NUM_HELP_PAGES - 1;
                    break;
            }
        }

        public override void Draw( )
        {
            Menu.DrawPic( 0, 0, Drawer.CachePic( String.Format( "gfx/help{0}.lmp", _Page ) ) );
        }
    }
}
