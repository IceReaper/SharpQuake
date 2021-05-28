namespace SharpQuake.Framework.IO.Alias
{
	using Rendering;
	using System.Runtime.InteropServices;

	[StructLayout( LayoutKind.Sequential, Pack = 1 )]
    public struct daliasframe_t
    {
        public trivertx_t bboxmin;	// lightnormal isn't used
        public trivertx_t bboxmax;	// lightnormal isn't used
        [MarshalAs( UnmanagedType.ByValArray, SizeConst = 16 )]
        public byte[] name; // char[16]	// frame name from grabbing

        public static int SizeInBytes = Marshal.SizeOf( typeof( daliasframe_t ) );
    } // daliasframe_t;
}
