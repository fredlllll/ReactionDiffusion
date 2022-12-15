namespace LaplaceDiffusion
{
    public class ABPixel : IPixel
    {
        float a, b;

        public int Components => 2;

        public float this[int component]
        {
            get
            {
                if (component == 0)
                {
                    return a;
                }
                else
                {
                    return b;
                }
            }
            set
            {
                if (component == 0)
                {
                    a = Math.Clamp(value, 0, 1);
                }
                else
                {
                    b = Math.Clamp(value, 0, 1);
                }
            }
        }

        public ABPixel(float a, float b)
        {
            this.a = a;
            this.b = b;
        }

        public ABPixel(Random r)
        {
            a = 0.5f + 0.1f * r.NextSingle();
            b = 0.5f + 0.1f * r.NextSingle();
        }

        public IPixel Copy()
        {
            return new ABPixel(a, b);
        }
    }
}
