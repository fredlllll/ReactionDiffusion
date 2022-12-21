using FFMpegCore;
using FFMpegCore.Arguments;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Diagnostics;
using System.Numerics;

namespace ReactionDiffusion
{
    public static class Program
    {
        static readonly int width = 256;
        static readonly int height = 256;
        static readonly int frames = 1000;
        static readonly int stepsPerFrame = 10;

        static readonly float diffusionA = 1.0f;
        static readonly float diffusionB = 0.5f;
        static readonly float feedrateA = 0.0345f;
        static readonly float killrateB = 0.062f;
        static readonly float timestep = 1.0f;

        static ImageSaver imageSaver = new(width, height);

        public static void Main(string[] args)
        {
            imageSaver.Start();
            Image img = new(width, height);
            //InitFred(img);
            InitImage(img);

            Directory.CreateDirectory("output");
            foreach (var file in Directory.EnumerateFiles("output"))
            {
                File.Delete(file);
            }

            imageSaver.Enqueue("output\\000000.png", img.Copy());

            ReactionDiffusion reactionDiffusion = new(img)
            {
                diffusionA = diffusionA,
                diffusionB = diffusionB,
                feedrateA = feedrateA,
                killrateB = killrateB,
                timestep = timestep,
            };
            Stopwatch stopwatch = new();
            for (int i = 0; i < frames; ++i)
            {
                if (i % 10 == 0)
                {
                    Console.Write("Step " + i);
                }
                stopwatch.Restart();
                var spf = i > 50 ? stepsPerFrame : 1;
                for (int j = 0; j < spf; ++j)
                {
                    reactionDiffusion.Step();
                }
                stopwatch.Stop();
                if (i % 10 == 0)
                {
                    Console.WriteLine($". Time: {stopwatch.Elapsed}");
                }
                imageSaver.Enqueue($"output\\{i + 1:000000}.png", img.Copy());
            }
            imageSaver.Stop();
            imageSaver.Join();
            MakeVideo();
        }

        class StringArgument : IArgument
        {
            public string Text { get; set; }

            public StringArgument(string str)
            {
                this.Text = str;
            }
        }

        static void MakeVideo()
        {
            float framerate = 60;
            string input = Path.Combine(Path.GetFullPath("output"), "%06d.png");
            FFMpegArguments.FromFileInput(input, verifyExists: false, (o) => o.WithFramerate(framerate))
                .OutputToFile("output\\video.mp4", overwrite: true, delegate (FFMpegArgumentOptions options)
        {
            options.WithFramerate(framerate).WithVideoCodec("libx264").ForcePixelFormat("yuv420p").WithArgument(new StringArgument("-profile:v baseline -level 3 -f mp4"));
        }).ProcessSynchronously();
        }

        static void InitImage(Image img)
        {
            using (SixLabors.ImageSharp.Image<Rgb24> image = SixLabors.ImageSharp.Image.Load<Rgb24>("input2.png"))
            {
                image.Mutate(x => x.Resize(img.width, img.height));
                for (int y = 0; y < img.height; y++)
                {
                    for (int x = 0; x < img.width; x++)
                    {
                        var pixel = image[x, y];
                        img[x, y] = new ABPixel(pixel.R / 255f, pixel.G / 255f);
                    }
                }
            }
        }

        static void InitFred(Image img)
        {
            Random random = new((int)DateTime.Now.Ticks);
            Vector2 center = new(img.width / 2, img.height / 2);
            Vector2 pos = new();
            float avgSize = (img.width + img.height) / 2;

            for (int y = 0; y < img.height; ++y)
            {
                pos.Y = y;
                for (int x = 0; x < img.width; ++x)
                {
                    img[x, y] = new ABPixel(1 - random.NextSingle() * 0.1f, 0);

                    pos.X = x;
                    float distance = Vector2.Distance(center, pos) / avgSize;
                    if (distance < 0.025f)
                    {
                        img[x, y][1] = 1 - random.NextSingle() * 0.1f;
                    }
                }
            }
        }
    }
}