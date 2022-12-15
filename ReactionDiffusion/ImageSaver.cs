using SixLabors.ImageSharp.PixelFormats;
using System.Collections.Concurrent;

namespace LaplaceDiffusion
{
    public class ImageSaver
    {
        readonly ConcurrentQueue<Tuple<string, Image>> imagesToSave = new();
        readonly Semaphore queueSize = new(0, 100);
        readonly Thread thread;
        readonly SixLabors.ImageSharp.Image<Rgb24> imageSharp;

        public ImageSaver(int width, int height)
        {
            thread = new Thread(SaverLoop);
            thread.IsBackground = true;
            imageSharp = new SixLabors.ImageSharp.Image<Rgb24>(width, height);
        }

        public void Start()
        {
            thread.Start();
        }

        public void Enqueue(string path, Image image)
        {
            imagesToSave.Enqueue(new Tuple<string, Image>(path, image));
            while (true)
            {
                try
                {
                    queueSize.Release();
                    break;
                }
                catch (SemaphoreFullException)
                {
                    Thread.Sleep(10);
                }
            }
        }

        static Rgb24 GetColor(IPixel pixel)
        {
            byte val = (byte)(Math.Clamp(pixel[0] - pixel[1], 0, 1) * 255);
            return new Rgb24(val, val, val);
        }

        static void ImageToImageSharp(Image image, SixLabors.ImageSharp.Image<Rgb24> target, Func<IPixel, Rgb24> getColor)
        {
            for (int y = 0; y < image.height; ++y)
            {
                for (int x = 0; x < image.width; ++x)
                {
                    target[x, y] = getColor(image[x, y]);
                }
            }
        }

        void SaverLoop()
        {
            var pngEncoder = new SixLabors.ImageSharp.Formats.Png.PngEncoder();
            while (true)
            {
                try
                {
                    queueSize.WaitOne();
                    if (imagesToSave.TryDequeue(out var image))
                    {
                        ImageToImageSharp(image.Item2, imageSharp, GetColor);
                        using (var fs = new FileStream(image.Item1, FileMode.Create, FileAccess.Write))
                        {
                            imageSharp.Save(fs, pngEncoder);
                        }
                    }
                    else
                    {
                        break;
                    }
                }
                catch (ThreadInterruptedException)
                {
                    break;
                }
            }
        }

        public void Abort()
        {
            thread.Interrupt();
        }

        public void Stop()
        {
            while (true)
            {
                try
                {
                    queueSize.Release();
                    break;
                }
                catch (SemaphoreFullException)
                {
                    Thread.Sleep(10);
                }
            }
        }

        public void Join()
        {
            thread.Join();
        }
    }
}
