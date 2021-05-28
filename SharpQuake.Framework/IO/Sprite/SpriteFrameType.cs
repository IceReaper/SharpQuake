namespace SharpQuake.Framework.IO.Sprite
{
	using Rendering.Sprite;
	using System.Runtime.InteropServices;

	[StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public struct dspriteframetype_t
    {
        public spriteframetype_t type;
    } // dspriteframetype_t;
}
