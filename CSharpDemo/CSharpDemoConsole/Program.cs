namespace CSharpDemoConsole
{
    using CSharpDemo;
    using CSharpDemo.AzureLibrary;
    using CSharpDemo.TaskParallelLibrary;
    using CSharpDemo.HttpClientDemo;
    using System;

    internal class Program
    {
        private static void Main(string[] args)
        {
            IDemo demo = new SendingRequestConcurrentlyDemo();
            Logger.LogInfo($"Demo {demo.GetType().Name} started.");

            try
            {
                demo.RunDemo();
            }
            catch (Exception e)
            {
                Logger.LogError($"Run {demo.GetType().Name} failed with exception {e.ToString()}");
            }

            Logger.LogInfo($"Demo {demo.GetType().Name} finished.");
        }
    }
}
