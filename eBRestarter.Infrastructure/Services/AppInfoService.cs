using eBRestarter.Core.Application.Interfaces;
using eBRestarter.Core.Domain.Models.Records;
using System.Diagnostics;
using System.Reflection;

namespace eBRestarter.Infrastructure.Services;

public class AppInfoService : IAppInfoService
{
    public string GetAppVersion()
    {
        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        var fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
        return fvi.FileVersion ?? "1.0.0";
    }
}