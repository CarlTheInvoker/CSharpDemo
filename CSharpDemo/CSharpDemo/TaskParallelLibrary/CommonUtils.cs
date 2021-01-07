namespace CSharpDemo.TaskParallelLibrary
{
    using System.Threading;

    public static class CommonUtils
    {
        public static void PrintThreadId(string message)
        {
            Logger.LogInfo($"ThreadID = {Thread.CurrentThread.ManagedThreadId}, {message}");
        }
    }
}