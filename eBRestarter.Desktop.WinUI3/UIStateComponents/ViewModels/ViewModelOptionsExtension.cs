using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

using eBRestarter.Core.Application.Common.Attributes;
using eBRestarter.Core.Application.Ports.Inbound.Providers;
using eBRestarter.Core.Application.Ports.Outbound.Browser;
using eBRestarter.Core.Application.Ports.Outbound.Config;
using eBRestarter.Core.Application.Ports.Outbound.OperatingSystem;
using eBRestarter.Core.Domain.ValueObjects;
using eBRestarter.Desktop.WinUI3.BehavioralComponents.Providers.Interfaces;
using eBRestarter.Desktop.WinUI3.BehavioralComponents.Services.Interfaces;
using eBRestarter.Desktop.WinUI3.ObjectArchetypes.DTOs.UIOptionDTO;
using eBRestarter.Desktop.WinUI3.ObjectArchetypes.Enums;
using eBRestarter.Infrastructure.ObjectArchetypes.Constants;
using eBRestarter.Infrastructure.ObjectArchetypes.DTOs;
using Microsoft.Extensions.Logging;
using eBRestarter.Core.Application.ObjectArchetypes.Constants;

namespace eBRestarter.Desktop.WinUI3.ViewModels;

public sealed partial class ViewModelOptionsExtension : ObservableObject
{
    private const string ExtensionConfigErrorMessageResourceKey = "BrowserExtension_Config_Error_Message";
    private const string ExtensionConfigFileName = "tab_restarter_config.json";
    private const string ExtensionFolderOpenErrorMessageResourceKey = "Options_ExtensionFolderOpenError_Message";
    private const string ExtensionFolderOpenErrorTitleResourceKey = "Options_ExtensionFolderOpenError_Title";
    private const string ExtensionLanguageCodeEnglish = "EN";
    private const string ExtensionLanguageCodeGerman = "DE";
    private const int ExtensionSaveStatusDisplayDurationMilliseconds = 3000;
    private const string GeneralErrorKey = "General_Error";
    private const int MillisecondsPerMinute = 60000;
    private const string OptionsSavedSuccessfullyResourceKey = "Options_SavedSuccessfully";

    private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        WriteIndented = true,
        TypeInfoResolver = ExtensionConfigJsonContext.Default
    };

    private readonly IOutboundPortBrowserExtensionDeployment _browserExtensionDeploymentService;
    private readonly ILogger<ViewModelOptionsExtension> _logger;
    private readonly IOutboundPortBrowserExtensionPathProvider _browserExtensionPathProvider;
    private readonly IDialogService _dialogService;
    private readonly IOutboundPortEVisitorConfigRepository _evRestarterConfigRepository;
    private readonly IInboundPortLocalizationProvider _localizationService;
    private readonly IOutboundPortOsProcessControl _osProcessControlPort;
    private readonly IUIOptionsProvider _uiOptionsService;

    private readonly AppConfig? _currentConfig;

    [ObservableProperty] public partial string ExtensionSaveStatus { get; set; } = string.Empty;
    [ObservableProperty] public partial string ExtensionUrl { get; set; } = string.Empty;
    [ObservableProperty] public partial double ExtensionWaitTimeMinutes { get; set; } = 3;

    [ObservableProperty] public partial LanguageOption SelectedExtensionLanguage { get; set; }


    public ReadOnlyCollection<LanguageOption> ExtensionLanguages { get; }

    public ViewModelOptionsExtension(
        IOutboundPortBrowserExtensionDeployment browserExtensionDeploymentService,
        IOutboundPortBrowserExtensionPathProvider browserExtensionPathProvider,
        IDialogService dialogService,
        IOutboundPortEVisitorConfigRepository evRestarterConfigRepository,
        IInboundPortLocalizationProvider localizationService,
        ILogger<ViewModelOptionsExtension> logger,
        IOutboundPortOsProcessControl osProcessControlPort,
        IUIOptionsProvider uiOptionsService)
    {
        ArgumentNullException.ThrowIfNull(browserExtensionDeploymentService);
        ArgumentNullException.ThrowIfNull(browserExtensionPathProvider);
        ArgumentNullException.ThrowIfNull(dialogService);
        ArgumentNullException.ThrowIfNull(evRestarterConfigRepository);
        ArgumentNullException.ThrowIfNull(localizationService);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(osProcessControlPort);
        ArgumentNullException.ThrowIfNull(uiOptionsService);

        _browserExtensionDeploymentService = browserExtensionDeploymentService;
        _browserExtensionPathProvider = browserExtensionPathProvider;
        _dialogService = dialogService;
        _evRestarterConfigRepository = evRestarterConfigRepository;
        _localizationService = localizationService;
        _logger = logger;
        _osProcessControlPort = osProcessControlPort;
        _uiOptionsService = uiOptionsService;

        _currentConfig = _evRestarterConfigRepository.LoadConfig();

        _browserExtensionDeploymentService.EnsureExtensionIsDeployed();

        ExtensionLanguages = new ReadOnlyCollection<LanguageOption>([.. _uiOptionsService.GetAvailableLanguages()]);
        if (ExtensionLanguages.Count == 0)
        {
            throw new InvalidOperationException("Requires at least one language entry.");
        }

        SelectedExtensionLanguage = ExtensionLanguages[0];

        LoadExtensionConfig();
    }

    [RelayCommand]
    [ExceptionBoundary(ExceptionBoundaryKind.ThreadRoot, Justification = "UI command boundary for opening extension folder in file browser.")]
    private async Task OpenExtensionFolderAsync()
    {
        try
        {
            string extensionPath = _browserExtensionPathProvider.RetrieveExtensionFolderPath();

            if (!Directory.Exists(extensionPath))
            {
                Directory.CreateDirectory(extensionPath);
            }

            _osProcessControlPort.OpenDirectoryInFileBrowser(extensionPath);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                LogEventIds.UserInterface.ViewModelOperationFailed,
                exception,
                "Opening the browser extension folder failed.");

            await _dialogService.ShowMessageAsync(
                _localizationService.RetrieveString(ExtensionFolderOpenErrorTitleResourceKey),
                _localizationService.RetrieveString(ExtensionFolderOpenErrorMessageResourceKey),
                DialogIcon.Error);
        }
    }

    [RelayCommand]
    [ExceptionBoundary(ExceptionBoundaryKind.ThreadRoot, Justification = "UI command boundary for saving browser extension configuration.")]
    private async Task SaveExtensionConfigAsync()
    {
        try
        {
            string extensionPath = _browserExtensionPathProvider.RetrieveExtensionFolderPath();
            string configPath = Path.Combine(extensionPath, ExtensionConfigFileName);

            var targetUsername = Uri.EscapeDataString(_currentConfig?.Username ?? string.Empty);

            var extensionConfigDto = new ExtensionConfig
            {
                LANGUAGE = SelectedExtensionLanguage?.Index == 0 ? ExtensionLanguageCodeGerman : ExtensionLanguageCodeEnglish,
                ZIEL_URL = $"{WebLinks.EVisitorSurflink}{targetUsername}",
                WARTEZEIT_MS = (int)(ExtensionWaitTimeMinutes * MillisecondsPerMinute)
            };

            string jsonString = JsonSerializer.Serialize(extensionConfigDto, _jsonSerializerOptions);

            if (!Directory.Exists(extensionPath))
            {
                Directory.CreateDirectory(extensionPath);
            }

            await File.WriteAllTextAsync(configPath, jsonString);

            ExtensionSaveStatus = _localizationService.RetrieveString(OptionsSavedSuccessfullyResourceKey) ?? string.Empty;
            await Task.Delay(ExtensionSaveStatusDisplayDurationMilliseconds);
            ExtensionSaveStatus = string.Empty;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                LogEventIds.UserInterface.ExtensionConfigWriteFailed,
                exception,
                "Writing the browser extension configuration failed.");

            await _dialogService.ShowMessageAsync(
                _localizationService.RetrieveString(GeneralErrorKey),
                _localizationService.RetrieveString(ExtensionConfigErrorMessageResourceKey),
                DialogIcon.Error);
        }
    }

    [ExceptionBoundary(ExceptionBoundaryKind.DegradationBoundary, Justification = "Degradation fallback to default configuration when reading existing extension config fails.")]
    private void LoadExtensionConfig()
    {
        try
        {
            string configPath = Path.Combine(_browserExtensionPathProvider.RetrieveExtensionFolderPath(), ExtensionConfigFileName);

            if (!File.Exists(configPath))
            {
                return;
            }

            string jsonString = File.ReadAllText(configPath);
            var extensionConfigDto = JsonSerializer.Deserialize(jsonString, ExtensionConfigJsonContext.Default.ExtensionConfig);

            if (extensionConfigDto is null)
            {
                return;
            }

            ExtensionUrl = extensionConfigDto.ZIEL_URL;
            ExtensionWaitTimeMinutes = extensionConfigDto.WARTEZEIT_MS / (double)MillisecondsPerMinute;

            LanguageOption? matchingExtensionLanguage = extensionConfigDto.LANGUAGE == ExtensionLanguageCodeEnglish
                ? ExtensionLanguages.FirstOrDefault(languageOption => languageOption.Index == 1)
                : ExtensionLanguages.FirstOrDefault(languageOption => languageOption.Index == 0);

            if (matchingExtensionLanguage is not null)
            {
                SelectedExtensionLanguage = matchingExtensionLanguage;
            }
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                LogEventIds.UserInterface.ExtensionConfigReadFailed,
                exception,
                "The existing browser extension configuration could not be read; defaults are used.");
        }
    }
}
