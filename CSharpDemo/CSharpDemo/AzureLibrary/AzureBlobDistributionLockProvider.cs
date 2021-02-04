namespace CSharpDemo.AzureLibrary
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Net;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure;
    using Azure.Storage.Blobs;
    using Azure.Storage.Blobs.Specialized;

    public class AzureBlobDistributionLockProvider
    {
        private static AzureBlobDistributionLockProvider _instance;
        private static readonly object InstanceLock = new object();

        private readonly byte[] _byteArray = Encoding.ASCII.GetBytes("lock");
        private readonly BlobContainerClient _blobContainerClient;

        private AzureBlobDistributionLockProvider()
        {
            try
            {
                var blobServiceClient = new BlobServiceClient(Constants.AzureBlobForDistributedLockConnectionString);
                this._blobContainerClient = blobServiceClient.GetBlobContainerClient(Constants.AzureBlobForDistributedLockContainerName);
                this._blobContainerClient.CreateIfNotExists();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public static AzureBlobDistributionLockProvider Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (_instance == null)
                        {
                            _instance = new AzureBlobDistributionLockProvider();
                        }
                    }
                }

                return _instance;
            }
        }

        public async Task<BlobLeaseClient> AcquireLockAsync(
            string lockName, 
            string operationName, 
            TimeSpan retryInterval, 
            TimeSpan leaseInterval,
            CancellationToken cancellationToken)
        {
            Stopwatch sw = Stopwatch.StartNew();

            BlobClient blobClient = AzureBlobDistributionLockProvider.Instance.GetBlobClient($"lock-{lockName}");
            BlobLeaseClient blobLeaseClient = blobClient.GetBlobLeaseClient();

            bool needStop = false;
            while (!needStop && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    Logger.LogInfo($"{operationName}: acquiring the lock.");
                    var res = await blobLeaseClient.AcquireAsync(leaseInterval, cancellationToken: cancellationToken);
                    Logger.LogInfo($"{operationName}: acquired the lock");
                    return blobLeaseClient;
                }
                catch (RequestFailedException ex)
                {
                    HttpStatusCode statusCode = (HttpStatusCode)ex.Status;
                    switch (statusCode)
                    {
                        case HttpStatusCode.Conflict:
                            // Another process owns the lock, wait some time and retry
                            await Task.Delay(retryInterval, cancellationToken);
                            break;
                        case HttpStatusCode.NotFound:
                            // The blob doesn't exist, need to create first
                            needStop = true;
                            break;
                        default:
                            // Other response is not expected
                            throw;
                    }
                }
            }

            if (cancellationToken.IsCancellationRequested)
            {
                Logger.LogInfo($"{operationName} being cancelled");
                return null;
            }

            // If goes there, means the blob doesn't exist, check it again
            if (!blobClient.Exists())
            {
                try
                {
                    using (MemoryStream ms = new MemoryStream(this._byteArray))
                    {
                        await blobClient.UploadAsync(ms, cancellationToken);
                    }
                }
                catch (RequestFailedException)
                {
                    // May created by other process and get exception here, just ignore the exception
                }
            }

            return await this.AcquireLockAsync(lockName, operationName, retryInterval, leaseInterval, cancellationToken);
        }

        public async Task ReleaseLockAsync(string operationName, BlobLeaseClient blobLeaseClient)
        {
            Logger.LogInfo($"{operationName}: releasing the lock");
            await blobLeaseClient.ReleaseAsync();
            Logger.LogInfo($"{operationName}: released the lock");
        }

        private BlobClient GetBlobClient(string blobName)
        {
            return this._blobContainerClient.GetBlobClient(blobName);
        }
    }
}
