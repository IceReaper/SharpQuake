namespace SharpQuake.Framework.IO.Alias
{
	using System.Runtime.InteropServices;

	[StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public struct daliasinterval_t
    {
        public float interval;

        public static int SizeInBytes = Marshal.SizeOf( typeof( daliasinterval_t ) );
    } // daliasinterval_t;
}
