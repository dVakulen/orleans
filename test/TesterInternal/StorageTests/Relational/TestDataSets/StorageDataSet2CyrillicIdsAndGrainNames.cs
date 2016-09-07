using Orleans;
using System.Collections;
using System.Collections.Generic;


namespace UnitTests.StorageTests.Relational.TestDataSets
{
    /// <summary>
    /// A data set for grains with Cyrillic letters and IDs.
    /// </summary>
    /// <typeparam name="TStateData">The type of <see cref="TestStateGeneric1{T}"/>.</typeparam>
    public class StorageDataSet2CyrillicIdsAndGrainNames<TStateData>: IEnumerable<object[]>
    {
        /// <summary>
        /// The symbol set this data set uses.
        /// </summary>
        private static SymbolSet Symbols { get; } = new SymbolSet(SymbolSet.Cyrillic);

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
