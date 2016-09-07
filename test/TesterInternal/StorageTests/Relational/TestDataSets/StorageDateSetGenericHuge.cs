using Orleans;
using System;
using System.Collections;
using System.Collections.Generic;


namespace UnitTests.StorageTests.Relational.TestDataSets
{
    public sealed class StorageDataSetGenericHuge<TGrainKey, TStateData>: IEnumerable<object[]>
    {
        private static Range<long> CountOfCharacters { get; } = new Range<long>(1000000, 1000000);

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
