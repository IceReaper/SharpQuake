namespace SharpQuake.Framework.IO.Sprite
{
	using System.Runtime.InteropServices;

	[StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public struct dspritegroup_t
    {
        public int numframes;

        public static int SizeInBytes = Marshal.SizeOf( typeof( dspritegroup_t ) );
    } // dspritegroup_t;
}
