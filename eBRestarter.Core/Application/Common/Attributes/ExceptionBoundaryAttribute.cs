using System;

namespace eBRestarter.Core.Application.Common.Attributes;

[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method |
    AttributeTargets.Constructor | AttributeTargets.Property,
    AllowMultiple = false,
    Inherited = false)]
public sealed class ExceptionBoundaryAttribute(ExceptionBoundaryKind kind) : Attribute
{
    public ExceptionBoundaryKind Kind { get; } = kind;

    public string? Justification { get; init; }
}
