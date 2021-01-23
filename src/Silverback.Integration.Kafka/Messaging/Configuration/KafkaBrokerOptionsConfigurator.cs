// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Kafka;
using Silverback.Messaging.Outbound;
using Silverback.Messaging.Outbound.Routing;
using Silverback.Util;

namespace Silverback.Messaging.Configuration
{
    /// <summary>
    ///     This class will be located via assembly scanning and invoked when a <see cref="KafkaBroker" /> is
    ///     added to the <see cref="IServiceCollection" />.
    /// </summary>
    public class KafkaBrokerOptionsConfigurator : IBrokerOptionsConfigurator<KafkaBroker>
    {
        /// <inheritdoc cref="IBrokerOptionsConfigurator{TBroker}.Configure" />
        public void Configure(IBrokerOptionsBuilder brokerOptionsBuilder)
        {
            Check.NotNull(brokerOptionsBuilder, nameof(brokerOptionsBuilder));

            brokerOptionsBuilder.SilverbackBuilder
                .AddSingletonBrokerBehavior<KafkaMessageKeyInitializerProducerBehavior>()
                .AddSingletonBrokerBehavior<PartitionResolverProducerBehavior>()
                .Services
                .AddTransient<IConfluentProducerBuilder, ConfluentProducerBuilder>()
                .AddTransient<IConfluentConsumerBuilder, ConfluentConsumerBuilder>()
                .AddSingleton<IConfluentProducersCache, ConfluentProducersCache>();

            brokerOptionsBuilder.LogTemplates
                .ConfigureAdditionalData<KafkaConsumerEndpoint>("offset", "kafkaKey")
                .ConfigureAdditionalData<KafkaProducerEndpoint>("offset", "kafkaKey");
        }
    }
}
