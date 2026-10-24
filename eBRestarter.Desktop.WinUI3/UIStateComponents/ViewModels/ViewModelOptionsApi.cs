using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Threading.Tasks;

using eBRestarter.Core.Application.Ports.Inbound.Providers;
using eBRestarter.Core.Application.Ports.Inbound.UseCases;
using eBRestarter.Core.Application.Ports.Outbound.Config;
using eBRestarter.Core.Domain.ValueObjects;
using eBRestarter.Desktop.WinUI3.BehavioralComponents.Services.Interfaces;
using eBRestarter.Desktop.WinUI3.ObjectArchetypes.DTOs.SignalDTO.Messages;
using eBRestarter.Desktop.WinUI3.ObjectArchetypes.Enums;

namespace eBRestarter.Desktop.WinUI3.ViewModels;

public sealed partial class ViewModelOptionsApi : ObservableObject
{
    private const string RemoveCredentialsMessageResourceKey = "Options_RemoveCreds_Message";
    private const string RemoveCredentialsTitleResourceKey = "Options_RemoveCreds_Title";

    private readonly IDialogService _dialogService;
    private readonly IOutboundPortEVisitorConfigRepository _evRestarterConfigRepository;
    private readonly IInboundPortLocalizationProvider _localizationService;
    private readonly IUseCaseRemoveApiCredentials _removeApiCredentialsUseCase;

    private readonly AppConfig? _currentConfig;

    public ViewModelOptionsApi(
        IDialogService dialogService,
        IOutboundPortEVisitorConfigRepository evRestarterConfigRepository,
        IInboundPortLocalizationProvider localizationService,
        IUseCaseRemoveApiCredentials removeApiCredentialsUseCase)
    {
        ArgumentNullException.ThrowIfNull(dialogService);
        ArgumentNullException.ThrowIfNull(evRestarterConfigRepository);
        ArgumentNullException.ThrowIfNull(localizationService);
        ArgumentNullException.ThrowIfNull(removeApiCredentialsUseCase);

        _dialogService = dialogService;
        _evRestarterConfigRepository = evRestarterConfigRepository;
        _localizationService = localizationService;
        _removeApiCredentialsUseCase = removeApiCredentialsUseCase;

        _currentConfig = _evRestarterConfigRepository.LoadConfig();
    }

    [RelayCommand]
    private async Task RemoveApiCredentialsAsync()
    {
        _removeApiCredentialsUseCase.Execute();

        if (_currentConfig?.Settings is not null)
        {
            _currentConfig.Settings.ApiUsername = string.Empty;
            _currentConfig.Settings.ApiKey = string.Empty;
        }

        WeakReferenceMessenger.Default.Send(new ApiCredentialsRemovedMessage());

        await _dialogService.ShowMessageAsync(
            _localizationService.RetrieveString(RemoveCredentialsTitleResourceKey),
            _localizationService.RetrieveString(RemoveCredentialsMessageResourceKey),
            DialogIcon.Success);
    }

    [RelayCommand]
    private Task ShowActivateApiDialogAsync() => _dialogService.ShowActivateApiDialogAsync();
}
