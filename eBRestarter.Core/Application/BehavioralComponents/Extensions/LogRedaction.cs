using System;

namespace eBRestarter.Core.Application.BehavioralComponents.Extensions;

public static class LogRedaction
{
    private const string EmptyMarker = "(empty)";
    private const string MaskSuffix = "***";
    private const string RedactedSegmentMarker = "***";

    public static string MaskIdentifier(string? identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            return EmptyMarker;
        }

        return string.Concat(identifier.AsSpan(0, 1), MaskSuffix);
    }

    public static string MaskUrlUserSegment(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return EmptyMarker;
        }

        if (!Uri.TryCreate(url.Trim('"'), UriKind.Absolute, out Uri? parsedUrl))
        {
            return RedactedSegmentMarker;
        }

        var path = parsedUrl.AbsolutePath;
        var lastSeparatorIndex = path.LastIndexOf('/');

        if (lastSeparatorIndex < 0 || lastSeparatorIndex == path.Length - 1)
        {
            return $"{parsedUrl.GetLeftPart(UriPartial.Authority)}{path}";
        }

        var pathWithoutLastSegment = path[..(lastSeparatorIndex + 1)];

        return $"{parsedUrl.GetLeftPart(UriPartial.Authority)}{pathWithoutLastSegment}{RedactedSegmentMarker}";
    }
}
