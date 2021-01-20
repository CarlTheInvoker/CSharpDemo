namespace CSharpDemoConsole
{
    using CSharpDemo;
    using CSharpDemo.TaskParallelLibrary;
    using System;

    internal class Program
    {
        private static void Main(string[] args)
        {
            IDemo demo = new WaitAwaitThreadIdDemo();
            Console.WriteLine($"Demo {demo.GetType().Name} started.");

            demo.RunDemo();

            Console.WriteLine($"Demo {demo.GetType().Name} finished.");
            Console.ReadKey();
        }
    }
}
