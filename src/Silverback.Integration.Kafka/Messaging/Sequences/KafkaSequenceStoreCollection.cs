// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections;
using System.Collections.Generic;
using Confluent.Kafka;
using Silverback.Messaging.Broker;
using Silverback.Util;

namespace Silverback.Messaging.Sequences
{
    /// <summary>
    ///     This sequence store collection will create a sequence store per each partition.
    /// </summary>
    internal class KafkaSequenceStoreCollection : ISequenceStoreCollection
    {
        private readonly Dictionary<TopicPartition, ISequenceStore> _sequenceStores { get; } = new();

        public IEnumerator<ISequenceStore> GetEnumerator() => _sequenceStores.Values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public int Count => _sequenceStores.Count;

        public void Dispose()
        {
            AsyncHelper.RunSynchronously(
                () => _sequenceStores.DisposeAllAsync(SequenceAbortReason.ConsumerAborted));
        }

        public ISequenceStore GetSequenceStore(IBrokerMessageIdentifier brokerMessageIdentifier)
        {
            throw new System.NotImplementedException();
        }
    }
}
