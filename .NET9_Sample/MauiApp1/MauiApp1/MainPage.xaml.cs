using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Platform;
using Microsoft.Maui.Graphics.Platform;
using RectF = Microsoft.Maui.Graphics.RectF;
using System.Reflection;
#if ANDROID
using Android.Graphics;
using NativeAndroid = Android;
#endif

namespace MauiApp1
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();

#if ANDROID
            Content = new GraphicsView() { Drawable = new CustomDrawable() };
#endif

        }
#if ANDROID
        public class CustomDrawable : IDrawable
        {
            public void Draw(ICanvas canvas, RectF dirtyRect)
            {
                //canvas.StrokeColor = Colors.Red;
                //canvas.StrokeSize = 5;
                //canvas.DrawLine(10, 10, 50, 50);
                // Load an image from a stream
                using (Stream stream = GetImageStreamFromAssembly())
                {
                    var image = PlatformImage.FromStream(stream);

                    // Draw the image onto the canvas
                    canvas.DrawImage(image, 0, 0, 200, 200); // Specify the x, y, width, height
                }
            }

            private Stream GetImageStreamFromAssembly()
            {
                ImageSource imageSource = ImageSource.FromFile(CopyImageToCache());
                return ConvertImageSourceToBitmapAsync(imageSource).Result;
            }
        }

        public static string CopyImageToCache()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "MauiApp1.Resources.Images.dotnet_bot.png";

            using Stream resourceStream = assembly.GetManifestResourceStream(resourceName);
            if (resourceStream == null)
            {
                throw new FileNotFoundException("Resource not found", resourceName);
            }

            var cacheDir = FileSystem.CacheDirectory;
            var filePath = System.IO.Path.Combine(cacheDir, "dotnet_bot.png");//"dotnet_bot.png");

            //File.Delete(filePath);

            using FileStream fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            resourceStream.CopyTo(fileStream);

            return filePath;
        }

        /// <summary>
        /// Converts the <see cref="ImageSource"/> to <see cref="Bitmap"/>.
        /// </summary>
        /// <param name="imageSource">The <see cref="ImageSource"/> to convert.</param>
        /// <returns>A <see cref="Bitmap"/> representation of the image.</returns>
        public static async Task<Stream> ConvertImageSourceToBitmapAsync(ImageSource imageSource)
        {
            var bitMap = await GetBitmapFromImageSource(imageSource);
            byte[] bitmapData;
            using (var stream = new MemoryStream())
            {
                if (imageSource.ToString()!.EndsWith("png"))
                {
                    bitMap?.Compress(Bitmap.CompressFormat.Png, 0, stream);
                }
                else
                {
                    bitMap?.Compress(Bitmap.CompressFormat.Jpeg, 0, stream);
                }

                bitmapData = stream.ToArray();
            }

            Stream iOStream = new MemoryStream(bitmapData);
            return iOStream;
        }

        private static async Task<Bitmap?> GetBitmapFromImageSource(ImageSource imageSource)
        {
            Bitmap? bitmap = null;
            CancellationToken cancellationToken = default;

            if (imageSource is UriImageSource uriImageSource)
            {
                // Load image from URL
                using var httpClient = new System.Net.Http.HttpClient();
                using var stream = await httpClient.GetStreamAsync(uriImageSource.Uri);
                bitmap = await BitmapFactory.DecodeStreamAsync(stream);
            }
            else if (imageSource is FileImageSource fileImageSource)
            {
                var context = NativeAndroid.App.Application.Context;
                string file = fileImageSource.File;

                if (File.Exists(file))
                {
                    bitmap = await BitmapFactory.DecodeFileAsync(file);
                }
                else
                {
                    int resourceId = context.Resources.GetIdentifier(file, "drawable", context.PackageName);
                    if (resourceId != 0)
                    {
                        bitmap = BitmapFactory.DecodeResource(context.Resources, resourceId);
                    }
                }
            }
            else if (imageSource is StreamImageSource streamImageSource)
            {
                if (streamImageSource.Stream != null)
                {
                    using Stream stream = await streamImageSource.Stream(cancellationToken);
                    bitmap = await BitmapFactory.DecodeStreamAsync(stream);
                }
            }

            return bitmap;
        }
#endif
    }
}
