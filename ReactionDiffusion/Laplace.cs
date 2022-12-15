namespace LaplaceDiffusion
{
    public class ReactionDiffusion
    {
        private Image current;
        private Image next;
        Tuple<int, int>[] positionArray;

        public float diffusionA = 1.0f;
        public float diffusionB = 0.5f;
        public float feedrateA = 0.055f;
        public float killrateB = 0.062f;
        public float timestep = 1.0f;

        public ReactionDiffusion(Image image)
        {
            current = image;
            next = new(image.width, image.height);
            positionArray = new Tuple<int, int>[image.width * image.height];
            for (int y = 0; y < image.height; ++y)
            {
                for (int x = 0; x < image.width; ++x)
                {
                    int index = image.GetPixelIndex(x, y);
                    next.pixels[index] = new ABPixel(0, 0);
                    positionArray[index] = new Tuple<int, int>(x, y);
                }
            }
        }

        public void Step()
        {
            Parallel.ForEach(positionArray, StepPixel);
            //swap buffers
            var tmp = next;
            next = current;
            current = tmp;
        }

        private void StepPixel(Tuple<int, int> pos)
        {
            int x = pos.Item1;
            int y = pos.Item2;
            var pixel = current[x, y];
            var nextPixel = next[x, y];
            float pA = pixel[0];
            float pB = pixel[1];


            //float A = pA + (Da * LaplaceA(x, y) * pA - pA * MathF.Pow(pB, 2) + f * (1 - pA)) * timestep;
            //float B = pB + (Db * LaplaceB(x, y) * pB + pA * MathF.Pow(pB, 2) - (k + f) * pB) * timestep;

            float A = pA + ((diffusionA * LaplaceA(x, y)) - (pA * pB * pB) + (feedrateA * (1 - pA))) * timestep;
            float B = pB + ((diffusionB * LaplaceB(x, y)) + (pA * pB * pB) - ((killrateB + feedrateA) * pB)) * timestep;
            //Console.WriteLine($"x|y:{x}|{y} pA: {pA} A: {A} pB: {pB} B: {B}");

            nextPixel[0] = A;
            nextPixel[1] = B;
        }

        readonly float[,] laplaceGrid = new float[3, 3]
        {
            { 0.05f, 0.2f, 0.05f },
            { 0.2f, -1.0f, 0.2f },
            { 0.05f, 0.2f, 0.05f },
        };

        private float LaplaceTransform(int x, int y, Func<IPixel, float> getter)
        {
            float sum = 0;
            int count = 0;
            for (int j = 0; j < 3; j++)
            {
                int y_ = y + j - 1;
                for (int i = 0; i < 3; i++)
                {
                    int x_ = x + i - 1;
                    if (x_ >= 0 && x_ < current.width && y_ >= 0 && y_ < current.height)
                    {
                        sum += getter(current[x_, y_]) * laplaceGrid[j, i];
                        count++;
                    }
                    else
                    {
                        if (count > 0)
                        {
                            sum += sum / count;
                        }
                    }
                }
            }
            return sum;
        }

        float GetPixelA(IPixel pixel)
        {
            return pixel[0];
        }

        float GetPixelB(IPixel pixel)
        {
            return pixel[1];
        }

        private float LaplaceA(int x, int y)
        {
            return LaplaceTransform(x, y, GetPixelA);
        }

        private float LaplaceB(int x, int y)
        {
            return LaplaceTransform(x, y, GetPixelB);
        }
    }
}
