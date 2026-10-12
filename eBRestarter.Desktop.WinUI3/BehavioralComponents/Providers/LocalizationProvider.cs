using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

using Microsoft.Extensions.Logging;
using Microsoft.Windows.ApplicationModel.Resources;

using eBRestarter.Core.Application.ObjectArchetypes.Constants;
using eBRestarter.Core.Application.Ports.Inbound.Interfaces.Providers;
using eBRestarter.Desktop.WinUI3.BehavioralComponents.Providers.Interfaces;
using eBRestarter.Desktop.WinUI3.ObjectArchetypes.DTOs.UIOptionDTO;

namespace eBRestarter.Desktop.WinUI3.BehavioralComponents.Providers;

public sealed class LocalizationProvider : IInboundPortLocalizationProvider, IUIOptionsProvider
{
    private const int CacheEvictionBatchSize = 250;
    private const int MaxCacheEntries = 500;

    private const char FallbackBracket = '[';
    private const string FallbackDeutsch = "Deutsch";
    private const string FallbackEnglish = "English";

    private const string KeyBrowserCacheOption0 = "BrowserCacheOption_0";
    private const string KeyBrowserCacheOption1 = "BrowserCacheOption_1";
    private const string KeyBrowserCacheOption3 = "BrowserCacheOption_3";
    private const string KeyBrowserCacheOption7 = "BrowserCacheOption_7";
    private const string KeyBrowserCacheOption14 = "BrowserCacheOption_14";

    private const string KeyLangEnglish = "Lang_English";
    private const string KeyLangGerman = "Lang_German";

    private const string KeyQualifierLanguage = "Language";

    private const string KeyRestartOption0 = "RestartOption_0";
    private const string KeyRestartOption1 = "RestartOption_1";
    private const string KeyRestartOption3 = "RestartOption_3";
    private const string KeyRestartOption7 = "RestartOption_7";
    private const string KeyRestartOption14 = "RestartOption_14";

    private const string ResourcePathPrefix = "Resources/";

    private static readonly TimeSpan CacheEntryTtl = TimeSpan.FromHours(1);
    private static readonly TimeSpan MissingKeyTtl = TimeSpan.FromMinutes(5);

    private readonly ILogger<LocalizationProvider> _logger;

    private readonly Lock _cacheLock = new();
    private readonly ResourceContext _resourceContext;
    private readonly ResourceMap _resourceMap;

    private readonly ConcurrentDictionary<string, CacheEntry> _resourceStringCache = new(StringComparer.Ordinal);
    private string _currentLanguage = string.Empty;

    private long _cacheHits;
    private long _cacheMisses;
    private long _cacheEvictions;

    private readonly record struct CacheEntry(string Value, long ExpirationTimestamp);

    public LocalizationProvider(ILogger<LocalizationProvider> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;

        var resourceManager = new ResourceManager();
        _resourceContext = resourceManager.CreateResourceContext();

        string currentLanguage = Microsoft.Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride;
        if (string.IsNullOrEmpty(currentLanguage))
        {
            currentLanguage = System.Globalization.CultureInfo.CurrentUICulture.Name;
        }

        _currentLanguage = currentLanguage;
        _resourceContext.QualifierValues[KeyQualifierLanguage] = currentLanguage;
        _resourceMap = resourceManager.MainResourceMap;
    }

    internal int CachedEntriesCount => _resourceStringCache.Count;

    internal long CacheHits => Volatile.Read(ref _cacheHits);

    internal long CacheMisses => Volatile.Read(ref _cacheMisses);

    internal long CacheEvictions => Volatile.Read(ref _cacheEvictions);

    public string RetrieveString(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return string.Empty;
        }

        EnsureLanguageSynchronization();

        long now = Stopwatch.GetTimestamp();

        if (_resourceStringCache.TryGetValue(key, out CacheEntry entry))
        {
            if (now < entry.ExpirationTimestamp)
            {
                Interlocked.Increment(ref _cacheHits);
                return entry.Value;
            }

            _resourceStringCache.TryRemove(key, out _);
            Interlocked.Increment(ref _cacheEvictions);
        }

        Interlocked.Increment(ref _cacheMisses);

        EnsureCacheCapacity();

        string resolvedValue = ResolveResourceString(key);
        TimeSpan ttl = resolvedValue.StartsWith(FallbackBracket) ? MissingKeyTtl : CacheEntryTtl;
        long expirationTimestamp = now + (long)(ttl.TotalSeconds * Stopwatch.Frequency);

        _resourceStringCache[key] = new CacheEntry(resolvedValue, expirationTimestamp);
        return resolvedValue;
    }

    private void EnsureLanguageSynchronization()
    {
        string activeLanguage = Microsoft.Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride;
        if (string.IsNullOrEmpty(activeLanguage))
        {
            activeLanguage = System.Globalization.CultureInfo.CurrentUICulture.Name;
        }

        if (!string.Equals(_currentLanguage, activeLanguage, StringComparison.Ordinal))
        {
            lock (_cacheLock)
            {
                if (!string.Equals(_currentLanguage, activeLanguage, StringComparison.Ordinal))
                {
                    _currentLanguage = activeLanguage;
                    _resourceContext.QualifierValues[KeyQualifierLanguage] = activeLanguage;
                    int evictedCount = _resourceStringCache.Count;
                    _resourceStringCache.Clear();
                    Interlocked.Add(ref _cacheEvictions, evictedCount);
                }
            }
        }
    }

    private void EnsureCacheCapacity()
    {
        if (_resourceStringCache.Count < MaxCacheEntries)
        {
            return;
        }

        lock (_cacheLock)
        {
            if (_resourceStringCache.Count < MaxCacheEntries)
            {
                return;
            }

            long now = Stopwatch.GetTimestamp();

            foreach (var kvp in _resourceStringCache)
            {
                if (now >= kvp.Value.ExpirationTimestamp)
                {
                    if (_resourceStringCache.TryRemove(kvp.Key, out _))
                    {
                        Interlocked.Increment(ref _cacheEvictions);
                    }
                }
            }

            if (_resourceStringCache.Count >= MaxCacheEntries)
            {
                int toEvict = _resourceStringCache.Count - (MaxCacheEntries - CacheEvictionBatchSize);
                foreach (var key in _resourceStringCache.Keys)
                {
                    if (toEvict <= 0)
                    {
                        break;
                    }

                    if (_resourceStringCache.TryRemove(key, out _))
                    {
                        Interlocked.Increment(ref _cacheEvictions);
                        toEvict--;
                    }
                }
            }
        }
    }

    private string ResolveResourceString(string resourceKey)
    {
        try
        {
            var result = _resourceMap.GetValue($"{ResourcePathPrefix}{resourceKey}", _resourceContext);

            if (result?.ValueAsString is { } resolvedValue)
            {
                return resolvedValue;
            }

            _logger.LogWarning(
                LogEventIds.UserInterface.LocalizationLookupFailed,
                "Resource key {ResourceKey} has no value in the current language; showing the bracketed key instead.",
                resourceKey);

            return $"[{resourceKey}]";
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                LogEventIds.UserInterface.LocalizationLookupFailed,
                exception,
                "Looking up resource key {ResourceKey} failed; showing the bracketed key instead.",
                resourceKey);

            return $"[{resourceKey}]";
        }
    }

    internal void ClearCache()
    {
        lock (_cacheLock)
        {
            _resourceStringCache.Clear();
        }
    }

    public IEnumerable<LanguageOption> GetAvailableLanguages()
    {
        string germanName = RetrieveString(KeyLangGerman);
        string englishName = RetrieveString(KeyLangEnglish);

        if (germanName.StartsWith(FallbackBracket))
        {
            germanName = FallbackDeutsch;
        }

        if (englishName.StartsWith(FallbackBracket))
        {
            englishName = FallbackEnglish;
        }

        return (LanguageOption[])[new(germanName, 0), new(englishName, 1)];
    }

    public IEnumerable<ComputerRestartOption> GetComputerRestartOptions() =>
    (ComputerRestartOption[])
    [
        new(RetrieveString(KeyRestartOption0), 0),
        new(RetrieveString(KeyRestartOption1), 1),
        new(RetrieveString(KeyRestartOption3), 3),
        new(RetrieveString(KeyRestartOption7), 7),
        new(RetrieveString(KeyRestartOption14), 14)
    ];

    public IEnumerable<BrowserCacheDeleteOption> GetBrowserCacheOptions() =>
    (BrowserCacheDeleteOption[])
    [
        new(RetrieveString(KeyBrowserCacheOption0), 0),
        new(RetrieveString(KeyBrowserCacheOption1), 1),
        new(RetrieveString(KeyBrowserCacheOption3), 3),
        new(RetrieveString(KeyBrowserCacheOption7), 7),
        new(RetrieveString(KeyBrowserCacheOption14), 14)
    ];
}
