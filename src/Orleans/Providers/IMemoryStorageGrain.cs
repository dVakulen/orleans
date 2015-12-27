<<<<<<< d473c48bf1777b788f1d34e76b3d4939ecbcb17a
<<<<<<< 505e746beb0edcc9916fd9128de4b3402f618eb6
using System;
using System.Collections.Generic;
=======
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

>>>>>>> Moved state and eTag to the Grain<TState>
=======
>>>>>>> Fixed migrated tests
using System.Threading.Tasks;

namespace Orleans.Storage
{
    /// <summary>
    /// Grain interface for internal memory storage grain used by Orleans in-memory storage provider.
    /// </summary>
    public interface IMemoryStorageGrain : IGrainWithIntegerKey
    {
        /// <summary>
        /// Async method to cause retrieval of the specified grain state data from memory store.
        /// </summary>
<<<<<<< d473c48bf1777b788f1d34e76b3d4939ecbcb17a
<<<<<<< 505e746beb0edcc9916fd9128de4b3402f618eb6
        /// <param name="stateStore">The name of the store that is used to store this grain state</param>
        /// <param name="grainStoreKey">Store key for this grain.</param>
        /// <returns>Value promise for the currently stored grain state for the specified grain and the etag of this data.</returns>
        Task<Tuple<IDictionary<string, object>, string>> ReadStateAsync(string stateStore, string grainStoreKey);
=======
        /// <param name="grainType">Type of this grain [fully qualified class name]</param>
        /// <param name="grainId">Grain id for this grain.</param>
        /// <returns>Value promise for the currently stored grain state for the specified grain.</returns>
        Task<IGrainState> ReadStateAsync(string stateStore, string grainStoreKey);
>>>>>>> Fixed migrated tests

=======
        /// <param name="grainType">Type of this grain [fully qualified class name]</param>
        /// <param name="grainId">Grain id for this grain.</param>
        /// <returns>Value promise for the currently stored grain state for the specified grain.</returns>
        Task<IGrainState> ReadStateAsync(string grainType, string grainId);
        
>>>>>>> Moved state and eTag to the Grain<TState>
        /// <summary>
        /// Async method to cause update of the specified grain state data into memory store.
        /// </summary>
        /// <param name="stateStore">The name of the store that is used to store this grain state</param>
        /// <param name="grainStoreKey">Store key for this grain.</param>
        /// <param name="grainState">New state data to be stored for this grain.</param>
<<<<<<< d473c48bf1777b788f1d34e76b3d4939ecbcb17a
<<<<<<< 505e746beb0edcc9916fd9128de4b3402f618eb6
        /// <param name="eTag">The previous etag that was read.</param>
        /// <returns>Value promise of the etag of the update operation for stored grain state for the specified grain.</returns>
        Task<string> WriteStateAsync(string stateStore, string grainStoreKey, IDictionary<string, object> grainState, string eTag);
=======
        /// <returns>Completion promise with new eTag for the update operation for stored grain state for the specified grain.</returns>
        Task<string> WriteStateAsync(string grainType, string grainId, IGrainState grainState);
>>>>>>> Moved state and eTag to the Grain<TState>

        /// <summary>
        /// Async method to cause deletion of the specified grain state data from memory store.
        /// </summary>
<<<<<<< 505e746beb0edcc9916fd9128de4b3402f618eb6
=======
        /// <returns>Completion promise with new eTag for the update operation for stored grain state for the specified grain.</returns>
        Task<string> WriteStateAsync(string grainType, string grainId, IGrainState grainState);
        
>>>>>>> Fixed migrated tests
        /// <param name="stateStore">The name of the store that is used to store this grain state</param>
        /// <param name="grainStoreKey">Store key for this grain.</param>
        /// <param name="eTag">The previous etag that was read.</param>
        /// <returns>Completion promise for the update operation for stored grain state for the specified grain.</returns>
        Task DeleteStateAsync(string stateStore, string grainStoreKey, string eTag);
=======
        /// <param name="grainType">Type of this grain [fully qualified class name]</param>
        /// <param name="grainId">Grain id for this grain.</param>
        /// <returns>Completion promise with new eTag for the update operation for stored grain state for the specified grain.</returns>
        Task<string> DeleteStateAsync(string grainType, string grainId, string etag);
>>>>>>> Moved state and eTag to the Grain<TState>
    }
}
