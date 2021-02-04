namespace CSharpDemo.AzureLibrary
{
    using System;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Azure.Cosmos;
    public class CosmosDbDistributionLockProvider
    {
        private static CosmosDbDistributionLockProvider _instance;
        private static readonly object InstanceLock = new object();
        private static readonly string DatabaseName = "SrpLockDatabase";
        private static readonly string ContainerName = "LockContainer";
        private static readonly string PartitionKeyValue = "Lock";
        private static readonly PartitionKey PartitionKey = new PartitionKey(PartitionKeyValue);

        private readonly Container _container;
        private CosmosDbDistributionLockProvider()
        {
            try
            {
                CosmosClient cosmosClient = new CosmosClient(Constants.AzureCosmosDbForDistributedLockConnectionString);
                cosmosClient.CreateDatabaseIfNotExistsAsync(DatabaseName).GetAwaiter().GetResult();
                Database database = cosmosClient.GetDatabase(DatabaseName);
                database.CreateContainerIfNotExistsAsync(ContainerName, PartitionKeyValue).GetAwaiter().GetResult();
                this._container = database.GetContainer(ContainerName);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public static CosmosDbDistributionLockProvider Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (_instance == null)
                        {
                            _instance = new CosmosDbDistributionLockProvider();
                        }
                    }
                }

                return _instance;
            }
        }

        public async Task<string> AcquireLockAsync(
            string lockName,
            string operationName,
            TimeSpan retryInterval,
            TimeSpan leaseInterval,
            CancellationToken cancellationToken)
        {
            // Document is already there, check if it has an expired lease
            bool needStop = false;

            ItemResponse<CosmosDbLease> readResponse = null;
            while (!needStop && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    readResponse = await this._container.ReadItemAsync<CosmosDbLease>(lockName, PartitionKey, cancellationToken: cancellationToken);
                    var existingLease = readResponse.Resource;
                    if (existingLease.LeasedUntil >= DateTime.UtcNow)
                    {
                        // Other process hold the lock
                        await Task.Delay(retryInterval, cancellationToken);
                        continue;
                    }

                    var updatedLease = new CosmosDbLease()
                    {
                        Id = lockName,
                        LeasedUntil = DateTime.UtcNow.Add(leaseInterval),
                    };

                    Logger.LogInfo($"{operationName}: acquiring the lock");
                    var updateLeaseResponse = await this._container.ReplaceItemAsync<CosmosDbLease>(
                        updatedLease,
                        lockName,
                        PartitionKey,
                        new ItemRequestOptions()
                        {
                            IfMatchEtag = readResponse.ETag,
                        }, 
                        cancellationToken);

                    Logger.LogInfo($"{operationName}: acquired the lock");
                    return readResponse.ETag;
                }
                catch (CosmosException ex)
                {
                    switch (ex.StatusCode)
                    {
                        case HttpStatusCode.Conflict:
                            // Another process owns the lease, wait some time and retry
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
                return null;
            }

            if (readResponse?.Resource == null)
            {
                var newLease = new CosmosDbLease()
                {
                    Id = lockName,
                    LeasedUntil = DateTime.UtcNow.Add(leaseInterval),
                };

                try
                {
                    var createResponse = await this._container.CreateItemAsync<CosmosDbLease>(
                        newLease,
                        PartitionKey, 
                        cancellationToken: cancellationToken);
                }
                catch (CosmosException)
                {
                    // If failed means another process owns this doc
                }
            }
         
            return await this.AcquireLockAsync(lockName, operationName, retryInterval, leaseInterval, cancellationToken);
        }

        public async Task ReleaseLockAsync(string lockName, string operationName, string eTag)
        {
            Logger.LogInfo($"{operationName}: releasing the lock");
            var updatedLease = new CosmosDbLease()
            {
                Id = lockName,
                LeasedUntil = DateTime.UtcNow.AddSeconds(-1),
            };

            var updateLeaseResponse = await this._container.ReplaceItemAsync<CosmosDbLease>(
                updatedLease,
                lockName,
                PartitionKey,
                new ItemRequestOptions()
                {
                    IfMatchEtag = eTag,
                });

            Logger.LogInfo($"{operationName}: released the lock");
        }

        private class CosmosDbLease
        {
            public string Id { get; set; }
            public DateTime LeasedUntil { get; set; }
        }
    }
}
