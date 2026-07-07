using System;

namespace eBRestarter.Core.Application.Ports.Outbound.Application;

[Obsolete("Use IAppInfoProviderOutboundPort instead. This port has been unified to eliminate redundancy.")]
public interface IAppVersionInfoProviderOutboundPort : IAppInfoProviderOutboundPort
{
}

