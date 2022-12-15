namespace LaplaceDiffusion
{
    public interface IPixel
    {
        public float this[int component] { get; set; }
        public int Components { get; }
        public IPixel Copy();
    }

    public class Image
    {
        public readonly IPixel[] pixels;
        public readonly int width;
        public readonly int height;

        public IPixel this[int x, int y]
        {
            get { return pixels[GetPixelIndex(x, y)]; }
            set { pixels[GetPixelIndex(x, y)] = value; }
        }

        public Image(int width, int height)
        {
            pixels = new IPixel[width * height];
            this.width = width;
            this.height = height;
        }

        public int GetPixelIndex(int x, int y)
        {
            return y * width + x;
        }

        public Image Copy()
        {
            Image other = new Image(width, height);

            for (int i = 0; i < pixels.Length; ++i)
            {
                other.pixels[i] = pixels[i].Copy();
            }

            return other;
        }
    }
}
