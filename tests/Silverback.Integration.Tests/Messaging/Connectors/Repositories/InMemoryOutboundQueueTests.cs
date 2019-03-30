﻿// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Silverback.Messaging.Connectors.Repositories;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Integration.TestTypes.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Connectors.Repositories
{
    [Collection("StaticInMemory")]
    public class InMemoryOutboundQueueTests
    {
        private readonly InMemoryOutboundQueue _queue;
        private readonly IOutboundMessage _sampleOutboundMessage = new OutboundMessage<TestEventOne>
        {
            Message = new TestEventOne { Content = "Test" },
            Endpoint = TestEndpoint.Default
        };


        public InMemoryOutboundQueueTests()
        {
            _queue = new InMemoryOutboundQueue();
            InMemoryOutboundQueue.Clear();
        }

        [Fact]
        public void EnqueueTest()
        {
            Parallel.For(0, 3, _ =>
            {
                _queue.Enqueue(_sampleOutboundMessage);
            });

            _queue.Length.Should().Be(0);
        }

        [Fact]
        public void EnqueueCommitTest()
        {
            Parallel.For(0, 3, _ =>
            {
                _queue.Enqueue(_sampleOutboundMessage);
            });

            _queue.Commit();

            _queue.Length.Should().Be(3);
        }

        [Fact]
        public void EnqueueRollbackTest()
        {
            Parallel.For(0, 3, _ =>
            {
                _queue.Enqueue(_sampleOutboundMessage);
            });

            _queue.Rollback();

            _queue.Length.Should().Be(0);
        }

        [Fact]
        public void EnqueueCommitRollbackCommitTest()
        {
            _queue.Enqueue(_sampleOutboundMessage);
            _queue.Commit();
            _queue.Enqueue(_sampleOutboundMessage);
            _queue.Rollback();
            _queue.Enqueue(_sampleOutboundMessage);
            _queue.Commit();

            _queue.Length.Should().Be(2);
        }

        [Theory]
        [InlineData(3, 3)]
        [InlineData(5, 5)]
        [InlineData(10, 5)]
        public void DequeueTest(int count, int expected)
        {
            for (var i = 0; i < 5; i++)
            {
                _queue.Enqueue(_sampleOutboundMessage);
            }

            _queue.Commit();

            var result = _queue.Dequeue(count);

            result.Count().Should().Be(expected);
        }

        [Fact]
        public void AcknowledgeRetryTest()
        {
            for (var i = 0; i < 5; i++)
            {
                _queue.Enqueue(_sampleOutboundMessage);
            }

            _queue.Commit();

            var result = _queue.Dequeue(5).ToArray();

            _queue.Acknowledge(result[0]);
            _queue.Retry(result[1]);
            _queue.Acknowledge(result[2]);
            _queue.Retry(result[3]);
            _queue.Acknowledge(result[4]);

            _queue.Length.Should().Be(2);
        }
    }
}