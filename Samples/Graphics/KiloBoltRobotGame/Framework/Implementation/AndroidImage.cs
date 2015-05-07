using Android.Graphics;

namespace KiloBoltRobotGame.Framework
{
    public class AndroidImage : IImage
    {
        internal Bitmap bitmap;
        private ImageFormat format;

        public AndroidImage(Bitmap bitmap, ImageFormat format)
        {
            this.bitmap = bitmap;
            this.format = format;
        }

        public int getWidth()
        {
            return bitmap.Width;
        }

        public int getHeight()
        {
            return bitmap.Height;
        }

        public ImageFormat getFormat()
        {
            return format;
        }

        public void dispose()
        {
            bitmap.Recycle();
        }
    }
}