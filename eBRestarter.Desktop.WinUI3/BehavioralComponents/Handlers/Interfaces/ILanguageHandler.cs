namespace eBRestarter.Desktop.WinUI3.BehavioralComponents.Handlers.Interfaces;

public interface ILanguageHandler
{
    string CurrentLanguageCode { get; }

    void SetLanguageOption(string languageCode);
}
