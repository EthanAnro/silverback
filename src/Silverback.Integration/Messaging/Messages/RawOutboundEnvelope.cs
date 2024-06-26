﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.IO;
using Silverback.Messaging.Broker;

namespace Silverback.Messaging.Messages
{
    /// <inheritdoc cref="IRawOutboundEnvelope" />
    internal class RawOutboundEnvelope : RawBrokerEnvelope, IRawOutboundEnvelope
    {
        private string _actualEndpointName;

        private string? _actualEndpointDisplayName;

        public RawOutboundEnvelope(
            IReadOnlyCollection<MessageHeader>? headers,
            IProducerEndpoint endpoint,
            IBrokerMessageIdentifier? brokerMessageIdentifier = null)
            : this(null, headers, endpoint, brokerMessageIdentifier)
        {
        }

        public RawOutboundEnvelope(
            Stream? rawMessage,
            IReadOnlyCollection<MessageHeader>? headers,
            IProducerEndpoint endpoint,
            IBrokerMessageIdentifier? brokerMessageIdentifier = null)
            : base(rawMessage, headers, endpoint)
        {
            BrokerMessageIdentifier = brokerMessageIdentifier;
            _actualEndpointName = endpoint.Name;
            UpdateActualEndpointDisplayName();
        }

        public new IProducerEndpoint Endpoint => (IProducerEndpoint)base.Endpoint;

        public IBrokerMessageIdentifier? BrokerMessageIdentifier { get; internal set; }

        public string ActualEndpointName
        {
            get => _actualEndpointName;
            internal set
            {
                _actualEndpointName = value;
                UpdateActualEndpointDisplayName();
            }
        }

        public string ActualEndpointDisplayName => _actualEndpointDisplayName ?? ActualEndpointName;

        private void UpdateActualEndpointDisplayName()
        {
            _actualEndpointDisplayName = Endpoint.FriendlyName != null
                ? $"{Endpoint.FriendlyName} ({_actualEndpointName})"
                : null;
        }
    }
}
