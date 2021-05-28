namespace SharpQuake.Framework.IO.Alias
{
	using Rendering;
	using System.Runtime.InteropServices;

	[StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public struct daliasgroup_t
    {
        public int numframes;
        public trivertx_t bboxmin;	// lightnormal isn't used
        public trivertx_t bboxmax;	// lightnormal isn't used

        public static int SizeInBytes = Marshal.SizeOf( typeof( daliasgroup_t ) );
    } // daliasgroup_t;
}
