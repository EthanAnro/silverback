﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Encryption;
using Silverback.Messaging.Serialization;
using Silverback.Messaging.Validation;
using Silverback.Util;

namespace Silverback.Messaging.Configuration
{
    /// <summary>
    ///     The base class for the builders of the types inheriting from <see cref="Endpoint" />.
    /// </summary>
    /// <typeparam name="TEndpoint">
    ///     The type of the endpoint being built.
    /// </typeparam>
    /// <typeparam name="TBuilder">
    ///     The actual builder type.
    /// </typeparam>
    public abstract class EndpointBuilder<TEndpoint, TBuilder> : IEndpointBuilder<TBuilder>
        where TEndpoint : Endpoint
        where TBuilder : IEndpointBuilder<TBuilder>
    {
        private string? _friendlyName;

        private IMessageSerializer? _serializer;

        private EncryptionSettings? _encryptionSettings;

        private MessageValidationMode _messageValidationMode = MessageValidationMode.LogWarning;

        /// <summary>
        ///     Initializes a new instance of the <see cref="EndpointBuilder{TEndpoint,TBuilder}" /> class.
        /// </summary>
        /// <param name="messageType">
        ///     The type of the message being produced or consumed.
        /// </param>
        /// <param name="endpointsConfigurationBuilder">
        ///     The optional reference to the <see cref="IEndpointsConfigurationBuilder" /> that instantiated the
        ///     builder.
        /// </param>
        protected EndpointBuilder(
            Type? messageType,
            IEndpointsConfigurationBuilder? endpointsConfigurationBuilder = null)
        {
            MessageType = messageType;
            EndpointsConfigurationBuilder = endpointsConfigurationBuilder;
        }

        /// <inheritdoc cref="IEndpointBuilder{TBuilder}.MessageType" />
        public Type? MessageType { get; }

        /// <summary>
        ///     Gets the <see cref="IEndpointsConfigurationBuilder" /> that instantiated the builder.
        /// </summary>
        public IEndpointsConfigurationBuilder? EndpointsConfigurationBuilder { get; }

        /// <summary>
        ///     Gets this instance.
        /// </summary>
        /// <remarks>
        ///     This is necessary to work around casting in the base classes.
        /// </remarks>
        protected abstract TBuilder This { get; }

        /// <inheritdoc cref="IEndpointBuilder{TBuilder}.WithName" />
        public TBuilder WithName(string friendlyName)
        {
            _friendlyName = friendlyName;
            return This;
        }

        /// <inheritdoc cref="IEndpointBuilder{TBuilder}.UseSerializer" />
        public TBuilder UseSerializer(IMessageSerializer serializer)
        {
            _serializer = Check.NotNull(serializer, nameof(serializer));
            return This;
        }

        /// <inheritdoc cref="IEndpointBuilder{TBuilder}.WithEncryption" />
        public TBuilder WithEncryption(EncryptionSettings? encryptionSettings)
        {
            _encryptionSettings = encryptionSettings;
            return This;
        }

        /// <inheritdoc cref="IEndpointBuilder{TBuilder}.ValidateMessage" />
        public TBuilder ValidateMessage(bool throwException)
        {
            _messageValidationMode = throwException
                ? MessageValidationMode.ThrowException
                : MessageValidationMode.LogWarning;
            return This;
        }

        /// <inheritdoc cref="IEndpointBuilder{TBuilder}.DisableMessageValidation" />
        public TBuilder DisableMessageValidation()
        {
            _messageValidationMode = MessageValidationMode.None;
            return This;
        }

        /// <summary>
        ///     Builds the endpoint instance.
        /// </summary>
        /// <returns>
        ///     The endpoint.
        /// </returns>
        public virtual TEndpoint Build()
        {
            var endpoint = CreateEndpoint();

            if (_friendlyName != null)
                endpoint.FriendlyName = _friendlyName;

            if (_serializer != null)
                endpoint.Serializer = _serializer;

            endpoint.Encryption = _encryptionSettings;

            endpoint.MessageValidationMode = _messageValidationMode;

            endpoint.Validate();

            return endpoint;
        }

        /// <summary>
        ///     Creates the endpoint to be configured according to the options stored in the builder.
        /// </summary>
        /// <returns>
        ///     The endpoint.
        /// </returns>
        protected abstract TEndpoint CreateEndpoint();
    }
}
