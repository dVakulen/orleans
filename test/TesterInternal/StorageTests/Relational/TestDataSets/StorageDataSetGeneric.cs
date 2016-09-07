using Orleans;
using System.Collections;
using System.Collections.Generic;


namespace UnitTests.StorageTests.Relational.TestDataSets
{
    public sealed class StorageDataSetGeneric<TGrainKey, TStateData>: IEnumerable<object[]>
    {
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
