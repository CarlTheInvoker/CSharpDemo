namespace CSharpDemo.AzureLibrary
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    public class DistributedLockDemo : IDemo
    {
        public void RunDemo()
        {
            this.LockDemo("locklock").GetAwaiter().GetResult();
        }

        private async Task LockDemo(string lockName)
        {
            List<Task> tasks = new List<Task>();
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            
            for (int i = 0; i < 10; ++i)
            {
                var waitSeconds = i;
                tasks.Add(this.LockByAzureBlob(
                    lockName,
                    $"Task {i}",
                    () => { Thread.Sleep(waitSeconds * 1000); },
                    TimeSpan.FromMilliseconds(500),
                    TimeSpan.FromSeconds(15),
                    cancellationTokenSource.Token));
            }

            await Task.WhenAll(tasks);
        }

        private async Task LockByAzureBlob(
            string lockName, 
            string operationName, 
            Action action, 
            TimeSpan retryInterval, 
            TimeSpan leaseInterval, 
            CancellationToken cancellationToken)
        {
            var lease = await AzureBlobDistributionLockProvider.Instance.AcquireLockAsync(lockName, operationName, retryInterval, leaseInterval, cancellationToken);
            if (lease == null)
            {
                // Means the task being cancelled.
                return;
            }

            action();
            await AzureBlobDistributionLockProvider.Instance.ReleaseLock(operationName, lease);
        }

        private async Task LockByCosmosDb(
            string lockName,
            string operationName,
            Action action,
            TimeSpan retryInterval,
            TimeSpan leaseInterval,
            CancellationToken cancellationToken)
        {
            var etag = await CosmosDbDistributionLockProvider.Instance.AcquireLockAsync(lockName, operationName, retryInterval, leaseInterval, cancellationToken);
            if (etag == null)
            {
                // Means the task being cancelled.
                return;
            }

            action();
            await CosmosDbDistributionLockProvider.Instance.ReleaseLock(lockName, operationName, etag);
        }
    }
}
