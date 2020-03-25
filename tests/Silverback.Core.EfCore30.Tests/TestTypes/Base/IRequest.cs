// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using Silverback.Messaging.Messages;

namespace Silverback.Tests.Core.EFCore30.TestTypes.Base
{
    [SuppressMessage("ReSharper", "UnusedTypeParameter")]
    public interface IRequest<out TResponse> : IMessage
    {
    }
}