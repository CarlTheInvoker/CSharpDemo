namespace CSharpDemo
{
    using System;
    using System.Linq;

    public class RandomDemo : IDemo
    {
        private static readonly Random Rand = new Random();

        public void RunDemo()
        {
            int[] count = new int[16];
            for (int i = 0; i < 16; ++i)
            {
                count[i] = 0;
            }

            for (int i = 0; i < 1000000; ++i)
            {
                int next = Rand.Next(0, 16);
                count[next]++;

                if (i % 10000 == 0)
                {
                    Console.WriteLine($"index = {i}, max count is {count.Max()}, min count is {count.Min()}, average is {i / 16 }");
                }
            }
        }
    }
}
