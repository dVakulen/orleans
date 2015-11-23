/*
Project Orleans Cloud Service SDK ver. 1.0
 
Copyright (c) Microsoft Corporation
 
All rights reserved.
 
MIT License

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and 
associated documentation files (the ""Software""), to deal in the Software without restriction,
including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so,
subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO
THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS
OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

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

        public override Task OnActivateAsync()
        {
            grainStore = new Dictionary<string, GrainStateStore>();
            base.DelayDeactivation(TimeSpan.FromDays(10 * 365)); // Delay Deactivation for MemoryStorageGrain virtually indefinitely.
            return TaskDone.Done;
        }

        public override Task OnDeactivateAsync()
        {
            grainStore = null;
            return TaskDone.Done;
        }

        public Task<ETagged<object>> ReadStateAsync(string grainType, string grainId)
        {
            GrainStateStore storage = GetStoreForGrain(grainType);
            var state = storage.GetGrainState(grainId);
            return Task.FromResult(state);
        }

        public Task WriteStateAsync(string grainType, string grainId, ETagged<object> grainState)
        {
            GrainStateStore storage = GetStoreForGrain(grainType);
            storage.UpdateGrainState(grainId, grainState);
            return TaskDone.Done;
        }

        public Task DeleteStateAsync(string grainType, string grainId, string etag)
        {
            GrainStateStore storage = GetStoreForGrain(grainType);
            storage.DeleteGrainState(grainId, etag);
            return TaskDone.Done;
        }

        private GrainStateStore GetStoreForGrain(string grainType)
        {
            GrainStateStore storage;
            if (!grainStore.TryGetValue(grainType, out storage))
            {
                storage = new GrainStateStore();
                grainStore.Add(grainType, storage);
            }

            return storage;
        }

        private class GrainStateStore
        {
            private readonly IDictionary<string, StoreEntry> grainStateStorage = new Dictionary<string, StoreEntry>();

            public ETagged<object> GetGrainState(string grainId)
            {
                StoreEntry entry;
                grainStateStorage.TryGetValue(grainId, out entry);
                if (entry == null) return null;
                entry.Etag = NewEtag();

                return new ETagged<object>(entry.State, entry.Etag);
            }

            public void UpdateGrainState(string grainId, ETagged<object> grainState)
            {
                StoreEntry entry;
                grainStateStorage.TryGetValue(grainId, out entry);
                if (entry == null)
                {
                    entry = new StoreEntry(grainState.ETag, grainState.State);
                }
                else if (grainState.ETag != null && entry.Etag != null && grainState.ETag != entry.Etag)
                {
                    throw new InconsistentStateException(
                        string.Format("Etag mismatch during Write: Expected = {0} Received = {1}", entry.Etag,
                            grainState.ETag));
                }
                else
                {
                    entry.State = grainState.State;
                }

                grainStateStorage[grainId] = entry;
            }

            public void DeleteGrainState(string grainId, string eTag)
            {
                StoreEntry entry;
                grainStateStorage.TryGetValue(grainId, out entry);
                if (entry == null)
                {
                    return;
                }

                if (entry.Etag != null && entry.Etag != eTag)
                    throw new InconsistentStateException(string.Format("Etag mismatch during Delete: Expected = {0} Received = {1}", entry.Etag, eTag));

                grainStateStorage.Remove(grainId);
            }

            private static string NewEtag()
            {
                return DateTime.UtcNow.ToString(CultureInfo.InvariantCulture);
            }

            private class StoreEntry
            {
                public string Etag { get; set; }

                public object State { get; set; }

                public StoreEntry(string etag, object state)
                {
                    Etag = etag;
                    State = state;
                }
            }
        }
    }
}
