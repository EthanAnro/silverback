﻿// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Silverback.Core.Tests.TestTypes.Messages;
using Silverback.Core.Tests.TestTypes.Subscribers;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Subscribers;
using Silverback.Messaging.Subscribers.Subscriptions;
using Xunit;

namespace Silverback.Core.Tests.Messaging.Publishing
{
    public class PublisherTests
    {
        private readonly TestSubscriber _syncSubscriber;
        private readonly TestAsyncSubscriber _asyncSubscriber;
        private readonly TestEnumerableSubscriber _syncEnumerableSubscriber;
        private readonly TestAsyncEnumerableSubscriber _asyncEnumerableSubscriber;

        public PublisherTests()
        {
            _syncSubscriber = new TestSubscriber();
            _asyncSubscriber = new TestAsyncSubscriber();
            _syncEnumerableSubscriber = new TestEnumerableSubscriber();
            _asyncEnumerableSubscriber = new TestAsyncEnumerableSubscriber();
        }

        private IPublisher GetPublisher(params ISubscriber[] subscribers) => GetPublisher(null, subscribers);

        private IPublisher GetPublisher(Action<BusConfigurator> configAction, params ISubscriber[] subscribers)
        {
            var services = new ServiceCollection();
            services.AddBus();

            services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
            services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

            foreach (var sub in subscribers)
                services.AddSingleton<ISubscriber>(sub);

            var serviceProvider = services.BuildServiceProvider();

            configAction?.Invoke(serviceProvider.GetRequiredService<BusConfigurator>());

            return serviceProvider.GetRequiredService<IPublisher>();
        }

        [Fact]
        public void Publish_SomeMessages_Received()
        {
            var publisher = GetPublisher(_syncSubscriber);

            publisher.Publish(new TestCommandOne());
            publisher.Publish(new TestCommandTwo());

            _syncSubscriber.ReceivedMessagesCount.Should().Be(2, "2 messages have been published");
        }

        [Fact]
        public async Task PublishAsync_SomeMessages_Received()
        {
            var publisher = GetPublisher(_syncSubscriber);

            await publisher.PublishAsync(new TestCommandOne());
            await publisher.PublishAsync(new TestCommandTwo());

            _syncSubscriber.ReceivedMessagesCount.Should().Be(2, "2 messages have been published");
        }

        [Fact]
        public void Publish_SomeMessages_ReceivedByDelegateSubscription()
        {
            int count = 0;
            var publisher = GetPublisher(config => config
                .Subscribe((object _) => count++));

            publisher.Publish(new TestCommandOne());
            publisher.Publish(new TestCommandTwo());

            count.Should().Be(2, "2 messages have been published");
        }

        [Fact]
        public async Task PublishAsync_SomeMessages_ReceivedByDelegateSubscription()
        {
            int count = 0;
            var publisher = GetPublisher(config => config
                .Subscribe((object _) => count++));

            await publisher.PublishAsync(new TestCommandOne());
            await publisher.PublishAsync(new TestCommandTwo());

            count.Should().Be(2, "2 messages have been published");
        }

        [Fact]
        public void Publish_SomeMessages_ReceivedByDelegateSubscriptionWithAdditionalParameters()
        {
            int count = 0;
            var publisher = GetPublisher(config => config
                .Subscribe<object>((msg, sp) => sp != null ? count++ : count = 0));

            publisher.Publish(new TestCommandOne());
            publisher.Publish(new TestCommandTwo());

            count.Should().Be(2, "2 messages have been published");
        }

        [Fact]
        public async Task PublishAsync_SomeMessages_ReceivedByDelegateSubscriptionWithAdditionalParameters()
        {
            int count = 0;
            var publisher = GetPublisher(config => config
                .Subscribe<object>((msg, sp) => sp != null ? count++ : count = 0));

            await publisher.PublishAsync(new TestCommandOne());
            await publisher.PublishAsync(new TestCommandTwo());

            count.Should().Be(2, "2 messages have been published");
        }

        [Fact]
        public void Publish_SomeMessages_ReceivedByAllSubscribers()
        {
            var publisher = GetPublisher(_syncSubscriber, _asyncSubscriber);

            publisher.Publish(new TestCommandOne());
            publisher.Publish(new TestCommandTwo());
            publisher.Publish(new TestCommandOne());
            publisher.Publish(new TestCommandTwo());
            publisher.Publish(new TestCommandTwo());

            _syncSubscriber.ReceivedMessagesCount.Should().Be(5, "5 messages have been published");
            _asyncSubscriber.ReceivedMessagesCount.Should().Be(5, "5 messages have been published");
        }

        [Fact]
        public async Task PublishAsync_SomeMessages_ReceivedByAllSubscribers()
        {
            var publisher = GetPublisher(_syncSubscriber, _asyncSubscriber);

            await publisher.PublishAsync(new TestCommandOne());
            await publisher.PublishAsync(new TestCommandTwo());
            await publisher.PublishAsync(new TestCommandOne());
            await publisher.PublishAsync(new TestCommandTwo());
            await publisher.PublishAsync(new TestCommandTwo());

            _syncSubscriber.ReceivedMessagesCount.Should().Be(5, "5 messages have been published");
            _asyncSubscriber.ReceivedMessagesCount.Should().Be(5, "5 messages have been published");
        }

        [Fact]
        public async Task PublishSyncAndAsync_SomeMessages_ReceivedByAllSubscribers()
        {
            var publisher = GetPublisher(_syncSubscriber, _asyncSubscriber);

            await publisher.PublishAsync(new TestCommandOne());
            await publisher.PublishAsync(new TestCommandTwo());
            publisher.Publish(new TestCommandOne());
            await publisher.PublishAsync(new TestCommandTwo());
            publisher.Publish(new TestCommandTwo());

            _syncSubscriber.ReceivedMessagesCount.Should().Be(5, "5 messages have been published");
            _asyncSubscriber.ReceivedMessagesCount.Should().Be(5, "5 messages have been published");
        }

        [Fact]
        public async Task Publish_SomeMessages_ReceivedByAllSubscribedMethods()
        {
            var service1 = new TestServiceOne();
            var service2 = new TestServiceTwo();
            var publisher = GetPublisher(service1, service2);

            await publisher.PublishAsync(new TestCommandOne());         // service1 +2
            await publisher.PublishAsync(new TestCommandTwo());         // service2 +2
            publisher.Publish(new TestCommandOne());                    // service1 +2
            await publisher.PublishAsync(new TransactionCompleteEvent()); // service1/2 +1
            publisher.Publish(new TransactionAbortedEvent());          // service1/2 +1

            service1.ReceivedMessagesCount.Should().Be(6);
            service2.ReceivedMessagesCount.Should().Be(4);
        }

        [Theory, ClassData(typeof(Publish_SubscribedMessage_ReceivedRepublishedMessages_TestData))]
        public void Publish_SubscribedMessage_ReceivedRepublishedMessages(IEvent message, int expectedEventOne, int expectedEventTwo)
        {
            var service1 = new TestServiceOne();
            var service2 = new TestServiceTwo();
            var publisher = GetPublisher(new RepublishMessagesTestService(), service1, service2);

            publisher.Publish(message);

            service1.ReceivedMessagesCount.Should().Be(expectedEventOne * 2);
            service2.ReceivedMessagesCount.Should().Be(expectedEventTwo * 2);
        }

        [Theory, ClassData(typeof(Publish_SubscribedMessage_ReceivedRepublishedMessages_TestData))]
        public async Task PublishAsync_SubscribedMessage_ReceivedRepublishedMessages(IEvent message, int expectedEventOne, int expectedEventTwo)
        {
            var service1 = new TestServiceOne();
            var service2 = new TestServiceTwo();
            var publisher = GetPublisher(new RepublishMessagesTestService(), service1, service2);

            await publisher.PublishAsync(message);

            service1.ReceivedMessagesCount.Should().Be(expectedEventOne * 2);
            service2.ReceivedMessagesCount.Should().Be(expectedEventTwo * 2);
        }

        [Fact]
        public void Publish_ExceptionInSubscriber_ExceptionReturned()
        {
            var publisher = GetPublisher(new TestExceptionSubscriber());

            publisher.Invoking(x => x.Publish(new TestEventOne())).Should().Throw<AggregateException>();
            publisher.Invoking(x => x.Publish(new TestEventTwo())).Should().Throw<AggregateException>();
        }

        [Fact]
        public void PublishAsync_ExceptionInSubscriber_ExceptionReturned()
        {
            var publisher = GetPublisher(new TestExceptionSubscriber());

            Func<Task> act1 = async () => await publisher.PublishAsync(new TestEventOne());
            Func<Task> act2 = async () => await publisher.PublishAsync(new TestEventTwo());

            act1.Should().Throw<AggregateException>();
            act2.Should().Throw<AggregateException>();
        }

        [Fact]
        public void Publish_NewMessageReturnedBySubscriber_MessageRepublished()
        {
            var subscriber = new TestSubscriber();
            var publisher = GetPublisher(new TestRepublisher(), subscriber);

            publisher.Publish(new TestCommandOne());

            subscriber.ReceivedMessagesCount.Should().Be(2);
        }

        [Fact]
        public async Task PublishAsync_NewMessageReturnedBySubscriber_MessageRepublished()
        {
            var subscriber = new TestSubscriber();
            var publisher = GetPublisher(new TestRepublisher(), subscriber);

            await publisher.PublishAsync(new TestCommandOne());

            subscriber.ReceivedMessagesCount.Should().Be(2);
        }

        [Fact]
        public void Publish_NewMessagesReturnedBySubscriber_MessagesRepublished()
        {
            var subscriber = new TestSubscriber();
            var publisher = GetPublisher(new TestRepublisher(), subscriber);

            publisher.Publish(new TestCommandTwo());

            subscriber.ReceivedMessagesCount.Should().Be(3);
        }

        [Fact]
        public async Task PublishAsync_NewMessagesReturnedBySubscriber_MessagesRepublished()
        {
            var subscriber = new TestSubscriber();
            var publisher = GetPublisher(new TestRepublisher(), subscriber);

            await publisher.PublishAsync(new TestCommandTwo());

            subscriber.ReceivedMessagesCount.Should().Be(3);
        }

        [Fact]
        public void Publish_HandlersReturnValue_ResultsReturned()
        {
            var publisher = GetPublisher(new TestRequestReplier());

            var results = publisher.Publish<string>(new TestRequestCommandOne());

            results.Should().Equal("response", "response2");
        }

        [Fact]
        public async Task PublishAsync_HandlersReturnValue_ResultsReturned()
        {
            var publisher = GetPublisher(new TestRequestReplier());

            var results = await publisher.PublishAsync<string>(new TestRequestCommandOne());

            results.Should().Equal("response", "response2");
        }

        [Fact]
        public void Publish_SomeMessages_ReceivedAsEnumerable()
        {
            var publisher = GetPublisher(_syncEnumerableSubscriber, _asyncEnumerableSubscriber);

            publisher.Publish(new TestCommandOne());
            publisher.Publish(new TestCommandTwo());

            _syncEnumerableSubscriber.ReceivedBatchesCount.Should().Be(2);
            _syncEnumerableSubscriber.ReceivedMessagesCount.Should().Be(2);
            _asyncEnumerableSubscriber.ReceivedBatchesCount.Should().Be(2);
            _asyncEnumerableSubscriber.ReceivedMessagesCount.Should().Be(2);
        }

        [Fact]
        public async Task PublishAsync_SomeMessages_ReceivedAsEnumerable()
        {
            var publisher = GetPublisher(_syncEnumerableSubscriber, _asyncEnumerableSubscriber);

            await publisher.PublishAsync(new TestCommandOne());
            await publisher.PublishAsync(new TestCommandTwo());

            _syncEnumerableSubscriber.ReceivedBatchesCount.Should().Be(2);
            _syncEnumerableSubscriber.ReceivedMessagesCount.Should().Be(2);
            _asyncEnumerableSubscriber.ReceivedBatchesCount.Should().Be(2);
            _asyncEnumerableSubscriber.ReceivedMessagesCount.Should().Be(2);
        }

        [Fact]
        public void Publish_MessagesBatch_EachMessageReceived()
        {
            var publisher = GetPublisher(_syncSubscriber, _asyncSubscriber);

            publisher.Publish(new ICommand[] { new TestCommandOne(), new TestCommandTwo(), new TestCommandOne() });

            _syncSubscriber.ReceivedMessagesCount.Should().Be(3);
            _asyncSubscriber.ReceivedMessagesCount.Should().Be(3);
        }

        [Fact]
        public async Task PublishAsync_MessagesBatch_EachMessageReceived()
        {
            var publisher = GetPublisher(_syncSubscriber, _asyncSubscriber);

            await publisher.PublishAsync(new ICommand[] { new TestCommandOne(), new TestCommandTwo(), new TestCommandOne() });

            _syncSubscriber.ReceivedMessagesCount.Should().Be(3);
            _asyncSubscriber.ReceivedMessagesCount.Should().Be(3);
        }

        [Fact]
        public void Publish_MessagesBatch_BatchReceived()
        {
            var publisher = GetPublisher(_syncEnumerableSubscriber, _asyncEnumerableSubscriber);

            publisher.Publish(new ICommand[] { new TestCommandOne(), new TestCommandTwo(), new TestCommandOne() });

            _syncEnumerableSubscriber.ReceivedBatchesCount.Should().Be(1);
            _syncEnumerableSubscriber.ReceivedMessagesCount.Should().Be(3);
            _asyncEnumerableSubscriber.ReceivedBatchesCount.Should().Be(1);
            _asyncEnumerableSubscriber.ReceivedMessagesCount.Should().Be(3);
        }

        [Fact]
        public async Task PublishAsync_MessagesBatch_BatchReceived()
        {
            var publisher = GetPublisher(_syncEnumerableSubscriber, _asyncEnumerableSubscriber);

            await publisher.PublishAsync(new ICommand[] { new TestCommandOne(), new TestCommandTwo(), new TestCommandOne() });

            _syncEnumerableSubscriber.ReceivedBatchesCount.Should().Be(1);
            _syncEnumerableSubscriber.ReceivedMessagesCount.Should().Be(3);
            _asyncEnumerableSubscriber.ReceivedBatchesCount.Should().Be(1);
            _asyncEnumerableSubscriber.ReceivedMessagesCount.Should().Be(3);
        }
        
        [Fact]
        public void Publish_MessagesBatch_EachMessageReceivedByDelegateSubscription()
        {
            int receivedMessagesCount = 0, asyncReceivedMessagesCount = 0;

            var publisher = GetPublisher(config =>
                config
                    .Subscribe<ITestMessage>((ITestMessage msg) => receivedMessagesCount++)
                    .Subscribe<ITestMessage>(async (ITestMessage msg) =>
                    {
                        await Task.Delay(1);
                        asyncReceivedMessagesCount++;
                    }));

            publisher.Publish(new ICommand[] { new TestCommandOne(), new TestCommandTwo(), new TestCommandOne() });

            receivedMessagesCount.Should().Be(3);
            asyncReceivedMessagesCount.Should().Be(3);
        }

        [Fact]
        public async Task PublishAsync_MessagesBatch_EachMessageReceivedByDelegateSubscription()
        {
            int receivedMessagesCount = 0, asyncReceivedMessagesCount = 0;

            var publisher = GetPublisher(config =>
                config
                    .Subscribe<ITestMessage>((ITestMessage msg) => receivedMessagesCount++)
                    .Subscribe<ITestMessage>(async (ITestMessage msg) =>
                    {
                        await Task.Delay(1);
                        asyncReceivedMessagesCount++;
                    }));

            await publisher.PublishAsync(new ICommand[] { new TestCommandOne(), new TestCommandTwo(), new TestCommandOne() });

            receivedMessagesCount.Should().Be(3);
            asyncReceivedMessagesCount.Should().Be(3);
        }

        [Fact]
        public void Publish_MessagesBatch_BatchReceivedByDelegateSubscription()
        {
            int receivedMessagesCount = 0, asyncReceivedMessagesCount = 0;

            var publisher = GetPublisher(config =>
                config
                    .Subscribe<ITestMessage>((IEnumerable<ITestMessage> msg) => receivedMessagesCount += msg.Count())
                    .Subscribe<ITestMessage>(async (IEnumerable<ITestMessage> msg) =>
                    {
                        await Task.Delay(1);
                        asyncReceivedMessagesCount += msg.Count();
                    }));

            publisher.Publish(new ICommand[] { new TestCommandOne(), new TestCommandTwo(), new TestCommandOne() });

            receivedMessagesCount.Should().Be(3);
            asyncReceivedMessagesCount.Should().Be(3);
        }

        [Fact]
        public async Task PublishAsync_MessagesBatch_BatchReceivedByDelegateSubscription()
        {
            int receivedMessagesCount = 0, asyncReceivedMessagesCount = 0;

            var publisher = GetPublisher(config =>
                config
                    .Subscribe<ITestMessage>((IEnumerable<ITestMessage> msg) => receivedMessagesCount += msg.Count())
                    .Subscribe<ITestMessage>(async (IEnumerable<ITestMessage> msg) =>
                    {
                        await Task.Delay(1);
                        asyncReceivedMessagesCount += msg.Count();
                    }));

            await publisher.PublishAsync(new ICommand[] { new TestCommandOne(), new TestCommandTwo(), new TestCommandOne() });

            receivedMessagesCount.Should().Be(3);
            asyncReceivedMessagesCount.Should().Be(3);
        }

        [Fact]
        public void Publish_NewMessagesReturnedByDelegateSubscription_MessagesRepublished()
        {
            var subscriber = new TestSubscriber();
            var publisher = GetPublisher(config =>
                    config
                        .Subscribe<TestCommandTwo>((TestCommandTwo msg) => new TestCommandOne())
                        .Subscribe<TestCommandTwo>(async (TestCommandTwo msg) =>
                        {
                            await Task.Delay(1);
                            return new TestCommandOne();
                        }),
                subscriber);

            publisher.Publish(new TestCommandTwo());

            subscriber.ReceivedMessagesCount.Should().Be(3);
        }

        [Fact]
        public void Publish_ExclusiveSubscribers_SequentiallyInvoked()
        {
            var subscriber = new ExclusiveSubscriberTestService();
            var publisher = GetPublisher(subscriber);

            publisher.Publish(new TestCommandOne());

            subscriber.Timestamps.Should().Match(times => times.Max() - times.Min() > TimeSpan.FromMilliseconds(20));
        }

        [Fact]
        public void Publish_NonExclusiveSubscribers_InvokedInParallel()
        {
            var subscriber = new NonExclusiveSubscriberTestService();
            var publisher = GetPublisher(subscriber);

            publisher.Publish(new TestCommandOne());

            subscriber.Timestamps.Should().Match(times => times.Max() - times.Min() < TimeSpan.FromMilliseconds(20));
        }

        [Fact]
        public async Task PublishAsync_ExclusiveSubscribers_SequentiallyInvoked()
        {
            var subscriber = new ExclusiveSubscriberTestService();
            var publisher = GetPublisher(subscriber);

            await publisher.PublishAsync(new TestCommandOne());

            subscriber.Timestamps.Should().Match(times => times.Max() - times.Min() > TimeSpan.FromMilliseconds(20));
        }

        [Fact]
        public async Task PublishAsync_NonExclusiveSubscribers_InvokedInParallel()
        {
            var subscriber = new NonExclusiveSubscriberTestService();
            var publisher = GetPublisher(subscriber);

            await publisher.PublishAsync(new TestCommandOne());

            subscriber.Timestamps.Should().Match(times => times.Max() - times.Min() < TimeSpan.FromMilliseconds(20));
        }

        [Fact]
        public void Publish_ExclusiveDelegateSubscription_SequentiallyInvoked()
        {
            var timestamps = new ConcurrentBag<DateTime>();

            var publisher = GetPublisher(config =>
                config
                    .Subscribe<ICommand>((ICommand _) =>
                    {
                        Thread.Sleep(20);
                        timestamps.Add(DateTime.Now);
                    })
                    .Subscribe<ICommand>(async (ICommand _) =>
                    {
                        await Task.Delay(20);
                        timestamps.Add(DateTime.Now);
                    }, new SubscriptionOptions {Exclusive = true}));

            publisher.Publish(new TestCommandOne());

            timestamps.Should().Match(times => times.Max() - times.Min() > TimeSpan.FromMilliseconds(20));
        }

        [Fact]
        public void Publish_NonExclusiveDelegateSubscription_InvokedInParallel()
        {
            var timestamps = new ConcurrentBag<DateTime>();

            var publisher = GetPublisher(config =>
                config
                    .Subscribe<ICommand>((ICommand _) =>
                    {
                        Thread.Sleep(20);
                        timestamps.Add(DateTime.Now);
                    }, new SubscriptionOptions { Exclusive = false })
                    .Subscribe<ICommand>(async (ICommand _) =>
                    {
                        await Task.Delay(20);
                        timestamps.Add(DateTime.Now);
                    }, new SubscriptionOptions { Exclusive = false }));

            publisher.Publish(new TestCommandOne());

            timestamps.Should().Match(times => times.Max() - times.Min() < TimeSpan.FromMilliseconds(20));
        }

        [Fact]
        public void Publish_NonParallelSubscriber_SequentiallyProcessing()
        {
            var subscriber = new NonParallelSubscriberTestService();
            var publisher = GetPublisher(subscriber);

            publisher.Publish(new ICommand[]
            {
                new TestCommandOne(),
                new TestCommandTwo(),
                new TestCommandOne(),
                new TestCommandTwo()
            });

            subscriber.Timestamps.Should().Match(times => times.Max() - times.Min() > TimeSpan.FromMilliseconds(160));
        }

        [Fact]
        public void Publish_ParallelSubscriber_ProcessingInParallel()
        {
            var subscriber = new ParallelSubscriberTestService();
            var publisher = GetPublisher(subscriber);

            publisher.Publish(new ICommand[]
            {
                new TestCommandOne(),
                new TestCommandTwo(),
                new TestCommandOne(),
                new TestCommandTwo()
            });

            subscriber.Timestamps.Should().Match(times => times.Max() - times.Min() < TimeSpan.FromMilliseconds(50));
        }
        
        [Fact]
        public void Publish_NonParallelDelegateSubscription_SequentiallyProcessing()
        {
            var timestamps = new ConcurrentBag<DateTime>();

            var publisher = GetPublisher(config =>
                config
                    .Subscribe<ICommand>((ICommand _) =>
                    {
                        Thread.Sleep(20);
                        timestamps.Add(DateTime.Now);
                    }, new SubscriptionOptions { Parallel = false })
                    .Subscribe<ICommand>(async (ICommand _) =>
                    {
                        await Task.Delay(20);
                        timestamps.Add(DateTime.Now);
                    }, new SubscriptionOptions { Parallel = false }));

            publisher.Publish(new ICommand[]
            {
                new TestCommandOne(),
                new TestCommandTwo(),
                new TestCommandOne(),
                new TestCommandTwo()
            });

            timestamps.Should().Match(times => times.Max() - times.Min() > TimeSpan.FromMilliseconds(160));
        }

        [Fact]
        public void Publish_ParallelDelegateSubscription_ProcessingInParallel()
        {
            var timestamps = new ConcurrentBag<DateTime>();

            var publisher = GetPublisher(config =>
                config
                    .Subscribe<ICommand>((ICommand _) =>
                    {
                        Thread.Sleep(20);
                        timestamps.Add(DateTime.Now);
                    }, new SubscriptionOptions { Parallel = true })
                    .Subscribe<ICommand>(async (ICommand _) =>
                    {
                        await Task.Delay(20);
                        timestamps.Add(DateTime.Now);
                    }, new SubscriptionOptions { Parallel = true }));

            publisher.Publish(new ICommand[]
            {
                new TestCommandOne(),
                new TestCommandTwo(),
                new TestCommandOne(),
                new TestCommandTwo()
            });

            timestamps.Should().Match(times => times.Max() - times.Min() < TimeSpan.FromMilliseconds(50));
        }

        [Fact]
        public void Publish_LimitedParallelSubscriber_ProcessingInParallel()
        {
            var subscriber = new LimitedParallelSubscriberTestService();
            var publisher = GetPublisher(subscriber);

            publisher.Publish(new ICommand[]
            {
                new TestCommandOne(),
                new TestCommandTwo(),
                new TestCommandOne(),
                new TestCommandTwo()
            });

            subscriber.Timestamps.Should().Match(times => times.Max() - times.Min() > TimeSpan.FromMilliseconds(50));
            subscriber.Timestamps.Should().Match(times => times.Max() - times.Min() < TimeSpan.FromMilliseconds(100));
        }

        [Fact]
        public async Task PublishAsync_LimitedParallelSubscriber_ProcessingInParallel()
        {
            var subscriber = new LimitedParallelSubscriberTestService();
            var publisher = GetPublisher(subscriber);

            await publisher.PublishAsync(new ICommand[]
            {
                new TestCommandOne(),
                new TestCommandTwo(),
                new TestCommandOne(),
                new TestCommandTwo()
            });

            subscriber.Timestamps.Should().Match(times => times.Max() - times.Min() > TimeSpan.FromMilliseconds(50));
            subscriber.Timestamps.Should().Match(times => times.Max() - times.Min() < TimeSpan.FromMilliseconds(100));
        }

        [Fact]
        public void Publish_LimitedParallelDelegateSubscription_ProcessingInParallel()
        {
            var timestamps = new ConcurrentBag<DateTime>();

            var publisher = GetPublisher(config =>
                config
                    .Subscribe<ICommand>((ICommand _) =>
                    {
                        Thread.Sleep(20);
                        timestamps.Add(DateTime.Now);
                    }, new SubscriptionOptions { Parallel = true, MaxDegreeOfParallelism = 2 })
                    .Subscribe<ICommand>(async (ICommand _) =>
                    {
                        await Task.Delay(20);
                        timestamps.Add(DateTime.Now);
                    }, new SubscriptionOptions { Parallel = true, MaxDegreeOfParallelism = 2 }));

            publisher.Publish(new ICommand[]
            {
                new TestCommandOne(),
                new TestCommandTwo(),
                new TestCommandOne(),
                new TestCommandTwo()
            });

            timestamps.Should().Match(times => times.Max() - times.Min() > TimeSpan.FromMilliseconds(50));
            timestamps.Should().Match(times => times.Max() - times.Min() < TimeSpan.FromMilliseconds(100));
        }

        [Fact]
        public async Task PublishAsync_LimitedParallelDelegateSubscription_ProcessingInParallel()
        {
            var timestamps = new ConcurrentBag<DateTime>();

            var publisher = GetPublisher(config =>
                config
                    .Subscribe<ICommand>((ICommand _) =>
                    {
                        Thread.Sleep(20);
                        timestamps.Add(DateTime.Now);
                    }, new SubscriptionOptions { Parallel = true, MaxDegreeOfParallelism = 2 })
                    .Subscribe<ICommand>(async (ICommand _) =>
                    {
                        await Task.Delay(20);
                        timestamps.Add(DateTime.Now);
                    }, new SubscriptionOptions { Parallel = true, MaxDegreeOfParallelism = 2 }));

            await publisher.PublishAsync(new ICommand[]
            {
                new TestCommandOne(),
                new TestCommandTwo(),
                new TestCommandOne(),
                new TestCommandTwo()
            });

            timestamps.Should().Match(times => times.Max() - times.Min() > TimeSpan.FromMilliseconds(50));
            timestamps.Should().Match(times => times.Max() - times.Min() < TimeSpan.FromMilliseconds(100));
        }

        /* TODO: Implement following tests:
         * - Parallel and Exclusive
         * - Republish messages not implementing IMessage */
    }
}
