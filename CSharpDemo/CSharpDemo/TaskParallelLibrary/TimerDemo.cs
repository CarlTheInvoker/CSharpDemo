namespace CSharpDemo.TaskParallelLibrary
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Timer provides executing a method on a thread pool thread at specific intervals
    /// Constructor parameters:
    ///     1. callback: the work to run
    ///     2. state: an object containing information to be used by the callback method
    ///     3. dueTime: time to delay before first round.
    ///     4. period: The time interval between invocations of callback. -1 means disable periodic signaling
    /// </summary>
    public class TimerDemo : IDemo
    {
        public void RunDemo()
        {
            AsyncRetryByTimerDemo();
        }

        private void AsyncRetryByTimerDemo()
        {
            SingleActionRetryByTimer singleActionRetryByTimer = new SingleActionRetryByTimer(
                () => { Console.WriteLine("action"); throw new Exception();},
                5,
                retry => retry * 1000);

            singleActionRetryByTimer.Start();
        }

        private async Task<int> AsyncRetryByTask(Action action, int maxRetryCount, Func<int, TimeSpan> getRetryInterval)
        {
            int retry = 0;
            while (retry < maxRetryCount)
            {
                try
                {
                    action();
                }
                catch (Exception)
                {
                    await Task.Delay(getRetryInterval(retry));
                    retry++;
                }
            }

            return retry;
        }

        private class SingleActionRetryByTimer
        {
            private Timer _timer;
            private readonly int _maxRetryCount;
            private readonly Action _action;
            private readonly Func<int, int> _getRetryInterval;

            public SingleActionRetryByTimer(Action action, int maxRetryCount, Func<int, int> getRetryInterval)
            {
                this._action = action;
                this._maxRetryCount = maxRetryCount;
                this._getRetryInterval = getRetryInterval;
                this._timer = null;
                
                this.RetryCount = 0;
            }

            public int RetryCount { get; private set; }

            public void Start()
            {
                try
                {
                    this.RetryCount++;
                    this._action();
                }
                catch (Exception)
                {
                    if (this._timer == null)
                    {
                        this._timer = new Timer(state =>
                        {
                            SingleActionRetryByTimer rc = state as SingleActionRetryByTimer;

                            if (rc.RetryCount < rc._maxRetryCount)
                            {
                                this.Start();
                            }
                        }, this, this._getRetryInterval(this.RetryCount), -1);
                    }
                    else
                    {
                        if (this.RetryCount == this._maxRetryCount)
                        {
                            Console.WriteLine("Retried for MaxRetryCount times but still failed.");
                            return;
                        }

                        this._timer.Change(this._getRetryInterval(this.RetryCount), -1);
                    }
                }
            }
        }
    }
}
