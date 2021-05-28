namespace SharpQuake.Framework.IO.Sprite
{
	using System.Runtime.InteropServices;

	[StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public struct dspriteinterval_t
    {
        public float interval;

        public static int SizeInBytes = Marshal.SizeOf( typeof( dspriteinterval_t ) );
    } // dspriteinterval_t;
}
