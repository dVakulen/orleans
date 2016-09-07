using Orleans;
using System.Collections;
using System.Collections.Generic;


namespace UnitTests.StorageTests.Relational.TestDataSets
{
    /// <summary>
    /// A set of simple test data set wit and without extension keys.
    /// </summary>
    /// <typeparam name="TGrainKey">The grain type (integer, guid or string)</typeparam>.
    public sealed class StorageDataSetPlain<TGrainKey>: IEnumerable<object[]>
    {
        /// <summary>
        /// The symbol set this data set uses.
        /// </summary>
        private static SymbolSet Symbols { get; } = new SymbolSet(SymbolSet.Latin1);

        /// <summary>
        /// The length of random string drawn form <see cref="Symbols"/>.
        /// </summary>
        private const long StringLength = 15L;


        private IEnumerable<object[]> DataSet { get; } = new[]
        {
            new object[]
            {
              
            },
        };

        public IEnumerator<object[]> GetEnumerator()
        {
            return DataSet.GetEnumerator();
        }


        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
