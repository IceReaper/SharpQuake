namespace SharpQuake.Framework.Rendering
{
    using Data;

    public class FloodFiller
    {
        private struct floodfill_t
        {
            public short x, y;
        } // floodfill_t;

        // must be a power of 2
        private const int FLOODFILL_FIFO_SIZE = 0x1000;

        private const int FLOODFILL_FIFO_MASK = FloodFiller.FLOODFILL_FIFO_SIZE - 1;

        private ByteArraySegment _Skin;
        private floodfill_t[] _Fifo;
        private int _Width;
        private int _Height;

        //int _Offset;
        private int _X;

        private int _Y;
        private int _Fdc;
        private byte _FillColor;
        private int _Inpt;

        public void Perform( uint[] table8to24 )
        {
            var filledcolor = 0;
            // attempt to find opaque black
            var t8to24 = table8to24;
            for ( var i = 0; i < 256; ++i )
            {
                if ( t8to24[i] == 255 << 0 ) // alpha 1.0
                {
                    filledcolor = i;
                    break;
                }
            }

            // can't fill to filled color or to transparent color (used as visited marker)
            if ( this._FillColor == filledcolor || this._FillColor == 255 )
                return;

            var outpt = 0;
            this._Inpt = 0;
            this._Fifo[this._Inpt].x = 0;
            this._Fifo[this._Inpt].y = 0;
            this._Inpt = (this._Inpt + 1 ) & FloodFiller.FLOODFILL_FIFO_MASK;

            while ( outpt != this._Inpt )
            {
                this._X = this._Fifo[outpt].x;
                this._Y = this._Fifo[outpt].y;
                this._Fdc = filledcolor;
                var offset = this._X + this._Width * this._Y;

                outpt = ( outpt + 1 ) & FloodFiller.FLOODFILL_FIFO_MASK;

                if (this._X > 0 )
                    this.Step( offset - 1, -1, 0 );
                if (this._X < this._Width - 1 )
                    this.Step( offset + 1, 1, 0 );
                if (this._Y > 0 )
                    this.Step( offset - this._Width, 0, -1 );
                if (this._Y < this._Height - 1 )
                    this.Step( offset + this._Width, 0, 1 );

                this._Skin.Data[this._Skin.StartIndex + offset] = ( byte )this._Fdc;
            }
        }

        private void Step( int offset, int dx, int dy )
        {
            var pos = this._Skin.Data;
            var off = this._Skin.StartIndex + offset;

            if ( pos[off] == this._FillColor )
            {
                pos[off] = 255;
                this._Fifo[this._Inpt].x = ( short ) (this._X + dx );
                this._Fifo[this._Inpt].y = ( short ) (this._Y + dy );
                this._Inpt = (this._Inpt + 1 ) & FloodFiller.FLOODFILL_FIFO_MASK;
            }
            else if ( pos[off] != 255 )
                this._Fdc = pos[off];
        }

        public FloodFiller( ByteArraySegment skin, int skinwidth, int skinheight )
        {
            this._Skin = skin;
            this._Width = skinwidth;
            this._Height = skinheight;
            this._Fifo = new floodfill_t[FloodFiller.FLOODFILL_FIFO_SIZE];
            this._FillColor = this._Skin.Data[this._Skin.StartIndex]; // *skin; // assume this is the pixel to fill
        }
    }
}
