﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Types;
using Silverback.Util;

namespace Silverback.Tests.Integration.TestTypes
{
    public class TestConsumer : Consumer<TestBroker, TestConsumerEndpoint, TestOffset>
    {
        public TestConsumer(
            TestBroker broker,
            TestConsumerEndpoint endpoint,
            IBrokerBehaviorsProvider<IConsumerBehavior> behaviorsProvider,
            IServiceProvider serviceProvider)
            : base(
                broker,
                endpoint,
                behaviorsProvider,
                serviceProvider,
                serviceProvider.GetRequiredService<ISilverbackIntegrationLogger<TestConsumer>>())
        {
        }

        public int AcknowledgeCount { get; set; }

        public Task TestHandleMessage(
            object message,
            IEnumerable<MessageHeader>? headers = null,
            IOffset? offset = null,
            IMessageSerializer? serializer = null) =>
            TestHandleMessage(message, new MessageHeaderCollection(headers), offset, serializer);

        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        public Task TestConsume(
            byte[]? rawMessage,
            IEnumerable<MessageHeader>? headers = null,
            IOffset? offset = null) =>
            TestHandleMessage(rawMessage, new MessageHeaderCollection(headers), offset);

        public async Task TestHandleMessage(
            object message,
            MessageHeaderCollection? headers,
            IOffset? offset = null,
            IMessageSerializer? serializer = null)
        {
            if (serializer == null)
                serializer = new JsonMessageSerializer();

            headers ??= new MessageHeaderCollection();

            var stream = await serializer.SerializeAsync(message, headers, MessageSerializationContext.Empty);
            var buffer = await stream.ReadAllAsync();

            await TestHandleMessage(buffer, headers, offset);
        }

        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        public async Task TestHandleMessage(byte[]? rawMessage, MessageHeaderCollection headers, IOffset? offset = null)
        {
            if (!Broker.IsConnected)
                throw new InvalidOperationException("The broker is not connected.");

            if (!IsConnected)
                throw new InvalidOperationException("The consumer is not ready.");

            await HandleMessage(rawMessage, headers, "test-topic", offset, null);
        }

        protected override void ConnectCore()
        {
        }

        protected override void DisconnectCore()
        {
        }

        protected override Task CommitCore(IReadOnlyCollection<TestOffset> offsets)
        {
            AcknowledgeCount += offsets.Count;
            return Task.CompletedTask;
        }

        protected override Task RollbackCore(IReadOnlyCollection<TestOffset> offsets)
        {
            // Nothing to do
            return Task.CompletedTask;
        }
    }
}
