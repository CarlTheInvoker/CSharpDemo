namespace CSharpDemo.TaskParallelLibrary
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// This class will show the relation about how tasks being executed in different thread
    /// </summary>
    public class WaitAwaitThreadIdDemo : IDemo
    {
        public void RunDemo()
        {
            this.WaitTaskDemo();

            // Since RunDemo is a sync method, need to wait here
            this.AwaitTaskDemo().Wait();
        }

        /// <summary>
        /// Wait will block current thread until task finished
        /// Then continue to execute the code in current thread
        /// </summary>
        private void WaitTaskDemo()
        {
            CommonUtils.PrintThreadId("Enter WaitTaskDemo");
            
            Task task = this.WaitAsync();
            task.Wait();
            
            CommonUtils.PrintThreadId("Exit WaitTaskDemo");
        }

        /// <summary>
        /// Await will free current thread
        /// When task finished, following code will be executed in the same thread which execute the task
        /// </summary>
        /// <returns></returns>
        private async Task AwaitTaskDemo()
        {
            CommonUtils.PrintThreadId("Enter AwaitTaskDemo");
            
            Task task = this.WaitAsync();
            await task;
            
            CommonUtils.PrintThreadId("Exit AwaitTaskDemo");
        }

        private async Task WaitAsync()
        {
            CommonUtils.PrintThreadId("WaitAsync, start waiting.");

            // Await means the work in the lambda expression will actually execute in another thread
            await Task.Run(() =>
            {
                // Block the thread which is executing the task for 5 seconds
                CommonUtils.PrintThreadId("WaitAsync, within the task");
                Thread.Sleep(TimeSpan.FromSeconds(5));
            });

            CommonUtils.PrintThreadId("WaitAsync, 5 seconds later...");
        }
    }
}
