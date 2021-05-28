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

namespace SharpQuake.Rendering.UI.Menus
{
    using Engine.Host;
    using Framework.IO.Input;

    public abstract class MenuBase
    {
        public static MenuBase CurrentMenu => MenuBase._CurrentMenu;

        public int Cursor => this._Cursor;

        // Top level menu items
        public static readonly MenuBase MainMenuInstance = new MainMenu( );

        public static readonly MenuBase SinglePlayerMenuInstance = new SinglePlayerMenu( );
        public static readonly MenuBase MultiPlayerMenuInstance = new MultiplayerMenu( );
        public static readonly MenuBase OptionsMenuInstance = new OptionsMenu( );
        public static readonly MenuBase HelpMenuInstance = new HelpMenu( );
        public static readonly MenuBase QuitMenuInstance = new QuitMenu( );
        public static readonly MenuBase LoadMenuInstance = new LoadMenu( );
        public static readonly MenuBase SaveMenuInstance = new SaveMenu( );

        // Submenus
        public static readonly MenuBase KeysMenuInstance = new KeysMenu( );

        public static readonly MenuBase LanConfigMenuInstance = new LanConfigMenu( );
        public static readonly MenuBase SetupMenuInstance = new SetupMenu( );
        public static readonly MenuBase GameOptionsMenuInstance = new GameOptionsMenu( );
        public static readonly MenuBase SearchMenuInstance = new SearchMenu( );
        public static readonly MenuBase ServerListMenuInstance = new ServerListMenu( );
        public static readonly MenuBase VideoMenuInstance = new VideoMenu( );
        protected int _Cursor;
        private static MenuBase _CurrentMenu;

        // CHANGE 
        protected Host Host
        {
            get;
            set;
        }

        public void Hide( )
        {
            this.Host.Keyboard.Destination = KeyDestination.key_game;
            MenuBase._CurrentMenu = null;
        }

        public virtual void Show( Host host )
        {
            this.Host = host;

            this.Host.Menu.EnterSound = true;
            this.Host.Keyboard.Destination = KeyDestination.key_menu;
            MenuBase._CurrentMenu = this;
        }

        public abstract void KeyEvent( int key );

        public abstract void Draw( );
    }
}
