using System;
using System.Collections.Generic;
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

<<<<<<< d473c48bf1777b788f1d34e76b3d4939ecbcb17a
<<<<<<< 505e746beb0edcc9916fd9128de4b3402f618eb6
        public Task<Tuple<IDictionary<string, object>, string>> ReadStateAsync(string stateStore, string grainStoreKey)
=======
        public Task<IGrainState> ReadStateAsync(string grainType, string grainId)
>>>>>>> Moved state and eTag to the Grain<TState>
=======
        public Task<IGrainState> ReadStateAsync(string stateStore, string grainStoreKey)
>>>>>>> Fixed migrated tests
        {
            if (logger.IsVerbose) logger.Verbose("ReadStateAsync for {0} grain: {1}", stateStore, grainStoreKey);
            GrainStateStore storage = GetStoreForGrain(stateStore);
            var stateTuple = storage.GetGrainState(grainStoreKey);
            return Task.FromResult(stateTuple);
<<<<<<< d473c48bf1777b788f1d34e76b3d4939ecbcb17a
        }

<<<<<<< 505e746beb0edcc9916fd9128de4b3402f618eb6
        public Task<string> WriteStateAsync(string stateStore, string grainStoreKey, IDictionary<string, object> grainState, string eTag)
        {
            if (logger.IsVerbose) logger.Verbose("WriteStateAsync for {0} grain: {1} eTag: {2}", stateStore, grainStoreKey, eTag);
            GrainStateStore storage = GetStoreForGrain(stateStore);
            string newETag = storage.UpdateGrainState(grainStoreKey, grainState, eTag);
            if (logger.IsVerbose) logger.Verbose("Done WriteStateAsync for {0} grain: {1} eTag: {2}", stateStore, grainStoreKey, eTag);
            return Task.FromResult(newETag);
        }

        public Task DeleteStateAsync(string stateStore, string grainStoreKey, string eTag)
        {
            if (logger.IsVerbose) logger.Verbose("DeleteStateAsync for {0} grain: {1} eTag: {2}", stateStore, grainStoreKey, eTag);
            GrainStateStore storage = GetStoreForGrain(stateStore);
            storage.DeleteGrainState(grainStoreKey, eTag);
            if (logger.IsVerbose) logger.Verbose("Done DeleteStateAsync for {0} grain: {1} eTag: {2}", stateStore, grainStoreKey, eTag);
            return TaskDone.Done;
=======
        public Task<string> WriteStateAsync(string grainType, string grainId, IGrainState grainState)
=======
        }
        
        public Task<string> WriteStateAsync(string stateStore, string grainStoreKey, IGrainState grainState)
>>>>>>> Fixed migrated tests
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
>>>>>>> Moved state and eTag to the Grain<TState>
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
<<<<<<< d473c48bf1777b788f1d34e76b3d4939ecbcb17a
<<<<<<< 505e746beb0edcc9916fd9128de4b3402f618eb6
            private Logger logger;
            private readonly IDictionary<string, Tuple<IDictionary<string, object>, long>> grainStateStorage = new Dictionary<string, Tuple<IDictionary<string, object>, long>>();
            
=======
            private Logger logger;
>>>>>>> Fixed migrated tests
            public GrainStateStore(Logger logger)
            {
                this.logger = logger;
            }
<<<<<<< d473c48bf1777b788f1d34e76b3d4939ecbcb17a

            public Tuple<IDictionary<string, object>, string> GetGrainState(string grainId)
            {
                Tuple<IDictionary<string, object>, long> state;
                if(grainStateStorage.TryGetValue(grainId, out state))
                    return Tuple.Create(state.Item1, state.Item2.ToString());
                else
                    return Tuple.Create<IDictionary<string, object>, string>(null, null); // upon first read, return null/invalid etag, to mimic Azure Storage.
            }

            public string UpdateGrainState(string grainStoreKey, IDictionary<string, object> state, string receivedEtag)
            {
                long currentETag = 0;
                Tuple<IDictionary<string, object>, long> oldState;
                if (grainStateStorage.TryGetValue(grainStoreKey, out oldState)) {
                    currentETag = oldState.Item2;
                }
                ValidateEtag(currentETag, receivedEtag, grainStoreKey, "Update");
                currentETag++;
                grainStateStorage[grainStoreKey] = Tuple.Create(state, currentETag);
                return currentETag.ToString();
=======
=======
>>>>>>> Fixed migrated tests
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
>>>>>>> Moved state and eTag to the Grain<TState>
            }

            public void DeleteGrainState(string grainStoreKey, string receivedEtag)
            {
                long currentETag = 0;
                Tuple<IDictionary<string, object>, long> oldState;
                if (grainStateStorage.TryGetValue(grainStoreKey, out oldState)){
                    currentETag = oldState.Item2;
                }
                ValidateEtag(currentETag, receivedEtag, grainStoreKey, "Delete");
                grainStateStorage.Remove(grainStoreKey);
            }
<<<<<<< 505e746beb0edcc9916fd9128de4b3402f618eb6

            private void ValidateEtag(long currentETag, string receivedEtag, string grainStoreKey, string operation)
            {
                if (receivedEtag == null) // first write
                {
                    if (currentETag > 0)
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
<<<<<<< d473c48bf1777b788f1d34e76b3d4939ecbcb17a
=======
>>>>>>> Moved state and eTag to the Grain<TState>
=======

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
>>>>>>> Fixed migrated tests
        }
    }
}
