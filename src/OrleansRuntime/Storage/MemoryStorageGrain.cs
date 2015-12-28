using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Orleans.Runtime;

namespace Orleans.Storage
{

    /// <summary>
    /// Implementaiton class for the Storage Grain used by In-memory Storage Provider
    /// </summary>
    /// <seealso cref="MemoryStorage"/>
    /// <seealso cref="IMemoryStorageGrain"/>
    internal class MemoryStorageGrain : Grain, IMemoryStorageGrain
    {
        private IDictionary<string, GrainStateStore> grainStore;
        private Logger logger;

        public override Task OnActivateAsync()
        {
            grainStore = new Dictionary<string, GrainStateStore>();
            base.DelayDeactivation(TimeSpan.FromDays(10 * 365)); // Delay Deactivation for MemoryStorageGrain virtually indefinitely.
            logger = GetLogger(GetType().Name);
            logger.Info("OnActivateAsync");
            return TaskDone.Done;
        }

        public override Task OnDeactivateAsync()
        {
            logger.Info("OnDeactivateAsync");
            grainStore = null;
            return TaskDone.Done;
        }

        public Task<IGrainState> ReadStateAsync(string stateStore, string grainStoreKey)
        {
            if (logger.IsVerbose) logger.Verbose("ReadStateAsync for {0} grain: {1}", stateStore, grainStoreKey);
            GrainStateStore storage = GetStoreForGrain(stateStore);
            var stateTuple = storage.GetGrainState(grainStoreKey);
            return Task.FromResult(stateTuple);
        }
        
        public Task<string> WriteStateAsync(string stateStore, string grainStoreKey, IGrainState grainState)
        {
            if (logger.IsVerbose) logger.Verbose("WriteStateAsync for {0} grain: {1} eTag: {2}", stateStore, grainStoreKey, grainState.ETag);
            GrainStateStore storage = GetStoreForGrain(stateStore);
            if (logger.IsVerbose) logger.Verbose("Done WriteStateAsync for {0} grain: {1} eTag: {2}", stateStore, grainStoreKey, grainState.ETag);
            return Task.FromResult(storage.UpdateGrainState(grainStoreKey, grainState));
        }

        public Task DeleteStateAsync(string grainType, string grainId, string etag)
        {
            GrainStateStore storage = GetStoreForGrain(grainType);
            return Task.FromResult(storage.DeleteGrainState(grainId, etag));
        }

        private GrainStateStore GetStoreForGrain(string grainType)
        {
            GrainStateStore storage;
            if (!grainStore.TryGetValue(grainType, out storage))
            {
                storage = new GrainStateStore(logger);
                grainStore.Add(grainType, storage);
            }

            return storage;
        }

        private class GrainStateStore
        {
            private Logger logger;
            public GrainStateStore(Logger logger)
            {
                this.logger = logger;
            }
            private readonly IDictionary<string, IGrainState> grainStateStorage = new Dictionary<string, IGrainState>();

            public IGrainState GetGrainState(string grainId)
            {
                IGrainState entry;
                grainStateStorage.TryGetValue(grainId, out entry);
                return entry;
            }

            public string UpdateGrainState(string grainId, IGrainState grainState)
            {
                IGrainState entry;
                grainStateStorage.TryGetValue(grainId, out entry);
                if (entry == null)
                {
                    grainStateStorage[grainId] = grainState;
                    return grainState.ETag;
                }

                ValidateEtag(grainState.ETag, entry.ETag, grainId, "Update");

                grainState.ETag = NewEtag();
                grainStateStorage[grainId] = grainState;
                return grainState.ETag;
            }

            public string DeleteGrainState(string grainId, string eTag)
            {
                IGrainState entry;
                grainStateStorage.TryGetValue(grainId, out entry);
                if (entry == null)
                {
                    return eTag;
                }

                ValidateEtag(eTag, entry.ETag, grainId, "Delete");
                grainStateStorage.Remove(grainId);
                return NewEtag();
            }

            private static string NewEtag()
            {
                return DateTime.UtcNow.ToString(CultureInfo.InvariantCulture);
            }

            private void ValidateEtag(string currentETag, string receivedEtag, string grainStoreKey, string operation)
            {
                if (receivedEtag == null) // first write
                {
                    if (currentETag != null)
                    {
                        string error = string.Format("Etag mismatch during {0} for grain {1}: Expected = {2} Received = null", operation, grainStoreKey, currentETag.ToString());
                        logger.Warn(0, error);
                        new InconsistentStateException(error);
                    }
                }
                else // non first write
                {
                    if (receivedEtag != currentETag.ToString())
                    {
                        string error = string.Format("Etag mismatch during {0} for grain {1}: Expected = {2} Received = {3}", operation, grainStoreKey, currentETag.ToString(), receivedEtag);
                        logger.Warn(0, error);
                        throw new InconsistentStateException(error);
                    }
                }
            }
        }
    }
}
