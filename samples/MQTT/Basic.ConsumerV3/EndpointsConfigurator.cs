﻿using MQTTnet.Formatter;
using MQTTnet.Protocol;
using Silverback.Messaging.Configuration;
using Silverback.Samples.Mqtt.Basic.Common;

namespace Silverback.Samples.Mqtt.Basic.ConsumerV3
{
    public class EndpointsConfigurator : IEndpointsConfigurator
    {
        public void Configure(IEndpointsConfigurationBuilder builder)
        {
            builder
                .AddMqttEndpoints(
                    endpoints => endpoints

                        // Configure the client options
                        .Configure(
                            config => config
                                .WithClientId("samples.basic.consumer")
                                .ConnectViaTcp("localhost")
                                .UseProtocolVersion(MqttProtocolVersion.V310)

                                // Send last will message if disconnecting
                                // ungracefully
                                .SendLastWillMessage(
                                    lastWill => lastWill
                                        .Message(new TestamentMessage())
                                        .ProduceTo("samples/testaments")))

                        // Consume the samples/basic topic
                        // Note: It is mandatory to specify the message type, since
                        // MQTT 3 doesn't support message headers (aka user
                        // properties)
                        .AddInbound<SampleMessage>(
                            endpoint => endpoint
                                .ConsumeFrom("samples/basic")
                                .WithQualityOfServiceLevel(
                                    MqttQualityOfServiceLevel.AtLeastOnce)

                                // Silently skip the messages in case of exception
                                .OnError(policy => policy.Skip())));
        }
    }
}
