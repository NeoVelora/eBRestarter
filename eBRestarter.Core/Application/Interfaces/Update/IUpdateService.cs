using eBRestarter.Core.Domain.Models.Records;
using System;
using System.Collections.Generic;
using System.Text;

namespace eBRestarter.Core.Application.Interfaces.Update
{
    public interface IUpdateService
    {
        Task<UpdateInfo> CheckForUpdateAsync();

        Task DownloadAndInstallAsync(UpdateInfo updateInfo);
    }
}
