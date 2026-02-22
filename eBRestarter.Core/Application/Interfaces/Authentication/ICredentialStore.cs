using eBRestarter.Core.Domain.Models.Records;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace eBRestarter.Core.Application.Interfaces.Authentication;

public interface ICredentialStore
{
    void SaveCredentials(ApiCredentials credentials);

    ApiCredentials? LoadCredentials();

    void ClearCredentials();

    ApiCredentials? ImportFromLegacyFile(string filePath);
}
