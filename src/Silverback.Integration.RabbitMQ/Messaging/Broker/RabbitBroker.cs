﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Broker
{
    /// <summary>
    ///     An <see cref="IBroker" /> implementation for RabbitMQ.
    /// </summary>
    /// <inheritdoc cref="Broker{TProducerEndpoint,TConsumerEndpoint}" />
    public class RabbitBroker : Broker<RabbitProducerEndpoint, RabbitConsumerEndpoint>
    {
        private readonly IRabbitConnectionFactory _connectionFactory;
        private readonly ILoggerFactory _loggerFactory;

        public RabbitBroker(
            IEnumerable<IBrokerBehavior> behaviors,
            IRabbitConnectionFactory connectionFactory,
            ILoggerFactory loggerFactory,
            IServiceProvider serviceProvider)
            : base(behaviors, loggerFactory, serviceProvider)
        {
            _loggerFactory = loggerFactory;
            _connectionFactory = connectionFactory;
        }

        protected override IProducer InstantiateProducer(
            RabbitProducerEndpoint endpoint,
            IReadOnlyCollection<IProducerBehavior> behaviors,
            IServiceProvider serviceProvider) =>
            new RabbitProducer(
                this,
                endpoint,
                behaviors,
                _connectionFactory,
                _loggerFactory.CreateLogger<RabbitProducer>());

        protected override IConsumer InstantiateConsumer(
            RabbitConsumerEndpoint endpoint,
            MessagesReceivedAsyncCallback callback,
            IReadOnlyCollection<IConsumerBehavior> behaviors,
            IServiceProvider serviceProvider) =>
            new RabbitConsumer(
                this,
                endpoint,
                callback,
                behaviors,
                _connectionFactory,
                serviceProvider,
                _loggerFactory.CreateLogger<RabbitConsumer>());

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing)
                return;

            _connectionFactory?.Dispose();
        }
    }
}