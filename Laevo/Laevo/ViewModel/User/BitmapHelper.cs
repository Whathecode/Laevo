using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;


namespace Laevo.ViewModel.User
{
	class BitmapHelper
	{
		/// <summary>
		/// Resizes a bitmap to a given size keeping the aspect ratio.
		/// </summary>
		/// <param name="bitmap">Bitmap to resize.</param>
		/// <param name="size">Desired size.</param>
		/// <returns></returns>
		public static TransformedBitmap ResizeBitmap( BitmapSource bitmap, Size size )
		{
			double scale = bitmap.PixelWidth <= bitmap.PixelHeight ? size.Width / bitmap.PixelWidth : size.Height / bitmap.PixelHeight;
			var transformedBitmap = new TransformedBitmap( bitmap, new ScaleTransform( scale, scale ) );
			return transformedBitmap;
		}

		/// <summary>
		/// Crops the center part of a bitmap according to given width and height.
		/// </summary>
		/// <param name="bitmap">Bitmap to crop.</param>
		/// <param name="size">Desired size of cropped rectangle.</param>
		/// <returns></returns>
		public static CroppedBitmap CroppBitmap( BitmapSource bitmap, Size size )
		{
			// Calculate x coordinate and width of cropped rectangle. If image is narrower than given width use 0 and image's width.
			var startX = bitmap.PixelWidth > size.Height ? bitmap.Width / 2 - size.Width / 2 : 0;
			var bitmapWidth = bitmap.PixelWidth > size.Width ? size.Width : bitmap.Width;

			// Calculate y coordinate and height of cropped rectangle. If image is lower than given height use 0 and image's height.
			var startY = bitmap.PixelHeight > size.Height ? bitmap.Height / 2 - size.Height / 2 : 0;
			var bitmapHeight = bitmap.PixelHeight > size.Height ? size.Height : bitmap.Height;

			var croppedImage = new CroppedBitmap( bitmap, new Int32Rect( (int)startX, (int)startY, (int)bitmapWidth, (int)bitmapHeight ) );
			return croppedImage;
		}

		/// <summary>
		/// Changes bitmap DPI.
		/// </summary>
		/// <param name="bitmap">Bitmap to change DPI.</param>
		/// <param name="dpi">Desired DPI value (default 96).</param>
		/// <returns></returns>
		public static BitmapImage ChangeBitmapDpi( BitmapSource bitmap, int dpi = 96 )
		{
			var width = bitmap.PixelWidth;
			var height = bitmap.PixelHeight;

			var stride = width * bitmap.Format.BitsPerPixel;
			var pixelData = new byte[stride * height];
			bitmap.CopyPixels( pixelData, stride, 0 );

			var bitmapSource = BitmapSource.Create( width, height, dpi, dpi, bitmap.Format, null, pixelData, stride );
			var encoder = new PngBitmapEncoder();
			var bitmapImage96 = new BitmapImage();

			using ( var memoryStream = new MemoryStream() )
			{
				encoder.Frames.Add( BitmapFrame.Create( bitmapSource ) );
				encoder.Save( memoryStream );
				bitmapImage96.BeginInit();
				bitmapImage96.StreamSource = new MemoryStream( memoryStream.ToArray() );
				bitmapImage96.EndInit();
			}

			return bitmapImage96;
		}
	}
}