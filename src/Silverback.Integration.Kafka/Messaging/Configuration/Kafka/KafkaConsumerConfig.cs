﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using Confluent.Kafka;

namespace Silverback.Messaging.Configuration.Kafka
{
    /// <summary>
    ///     Extends the <see cref="Confluent.Kafka.ConsumerConfig" /> adding the Silverback specific settings.
    /// </summary>
    [SuppressMessage("ReSharper", "SA1623", Justification = "Comments style is in-line with Confluent.Kafka")]
    public sealed class KafkaConsumerConfig : ConfluentConsumerConfigProxy, IEquatable<KafkaConsumerConfig>
    {
        /// <summary>
        ///     If the KafkaConsumer does not have a group.id for consumer group partition assignment and offset
        ///     storage, then a fake value is set as workaround for the current limitations of confluent-kafka-dotnet and librdkafka.
        ///     See also
        ///     <see href="https://github.com/confluentinc/confluent-kafka-dotnet/issues/225" />
        ///     <see href="https://github.com/edenhill/librdkafka/issues/593" /> for more information.
        /// </summary>
        private const string GroupIdNotSet = "not-set";

        private const bool KafkaDefaultAutoCommitEnabled = true;

        /// <summary>
        ///     Initializes a new instance of the <see cref="KafkaConsumerConfig" /> class.
        /// </summary>
        /// <param name="clientConfig">
        ///     The <see cref="KafkaClientConfig" /> to be used to initialize the
        ///     <see cref="KafkaConsumerConfig" />.
        /// </param>
        public KafkaConsumerConfig(KafkaClientConfig? clientConfig = null)
            : base(clientConfig?.GetConfluentConfig())
        {
            if (string.IsNullOrWhiteSpace(GroupId))
            {
                GroupId = GroupIdNotSet;
            }
        }

        /// <summary>
        ///     Returns a boolean indicating whether group.id is set.
        /// </summary>
        public bool IsGroupIdSet { get; private set; }

        /// <summary>
        ///     Client group id string. All clients sharing the same group.id belong to the same group.
        ///     <br /><br />default: ''
        ///     <br />importance: high.
        /// </summary>
        public override string GroupId
        {
            get => ConfluentConfig.GroupId;
            set
            {
                if (string.IsNullOrWhiteSpace(value) || value == GroupIdNotSet)
                {
                    ConfluentConfig.GroupId = GroupIdNotSet;
                    IsGroupIdSet = false;
                }
                else
                {
                    ConfluentConfig.GroupId = value;
                    IsGroupIdSet = true;
                }
            }
        }

        /// <summary>
        ///     Gets a value indicating whether autocommit is enabled according to the explicit
        ///     configuration and Kafka defaults.
        /// </summary>
        public bool IsAutoCommitEnabled => EnableAutoCommit ?? KafkaDefaultAutoCommitEnabled;

        /// <summary>
        ///     Defines the number of message to be processed before committing the offset to the server. The most
        ///     reliable level is 1 but it reduces throughput.
        /// </summary>
        public int CommitOffsetEach { get; set; } = -1;

        /// <summary>
        ///     Specifies whether the consumer has to be automatically recycled when a <see cref="KafkaException" />
        ///     is thrown while polling/consuming or an issues is detected (e.g. a poll timeout is reported). The default
        ///     is <c>true</c>.
        /// </summary>
        public bool EnableAutoRecovery { get; set; } = true;

        /// <inheritdoc cref="IValidatableEndpointSettings.Validate" />
        public override void Validate()
        {
            if (string.IsNullOrEmpty(BootstrapServers))
            {
                throw new EndpointConfigurationException(
                    "BootstrapServers is required to connect with the message broker.");
            }

            if (IsAutoCommitEnabled && CommitOffsetEach >= 0)
            {
                throw new EndpointConfigurationException(
                    "CommitOffsetEach cannot be used when auto-commit is enabled. " +
                    "Explicitly disable it setting Configuration.EnableAutoCommit = false.");
            }

            if (!IsAutoCommitEnabled && CommitOffsetEach <= 0)
            {
                throw new EndpointConfigurationException(
                    "CommitOffSetEach must be greater or equal to 1 when auto-commit is disabled.");
            }

            if (EnableAutoOffsetStore == true)
            {
                throw new EndpointConfigurationException(
                    "EnableAutoOffsetStore is not supported. " +
                    "Silverback must have control over the offset storing to work properly.");
            }

            EnableAutoOffsetStore = false;
        }

        /// <inheritdoc cref="IEquatable{T}.Equals(T)" />
        public bool Equals(KafkaConsumerConfig? other)
        {
            if (other is null)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            return CommitOffsetEach == other.CommitOffsetEach &&
                   ConfluentConfigEqualityComparer.Equals(ConfluentConfig, other.ConfluentConfig);
        }

        /// <inheritdoc cref="object.Equals(object)" />
        public override bool Equals(object? obj)
        {
            if (obj is null)
                return false;

            if (ReferenceEquals(this, obj))
                return true;

            if (obj.GetType() != GetType())
                return false;

            return Equals((KafkaConsumerConfig)obj);
        }

        /// <inheritdoc cref="object.GetHashCode" />
        public override int GetHashCode() => 0;
    }
}
