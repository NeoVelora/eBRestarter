using NSubstitute;
using Shouldly;
using System;
using Xunit;
using eBRestarter.Core.Application.Ports.Outbound.Config;
using eBRestarter.Core.Application.UseCases.Credentials;
using eBRestarter.Core.Domain.ValueObjects;
using NSubstitute.ExceptionExtensions;

namespace eBRestarter.Tests.Core.Application.UseCases.RemoveApiCredentials
{
    public class RemoveApiCredentialsUseCaseTests
    {
        private readonly IOutboundPortEVisitorConfigRepository _mockConfigService;
        private readonly RemoveApiCredentialsUseCase _sut;

        public RemoveApiCredentialsUseCaseTests()
        {
            _mockConfigService = Substitute.For<IOutboundPortEVisitorConfigRepository>();
            _sut = new RemoveApiCredentialsUseCase(_mockConfigService);
        }

        [Fact]
        public void Execute_ShouldClearCredentials_AndKeepOtherSettingsIntact()
        {
            var existingConfig = new AppConfig
            {
                Username = "HauptUser",
                Settings = new SettingsConfig
                {
                    ApiUsername = "MeinApiUser123",
                    ApiKey = "GeheimerSchluessel",
                    Theme = "Dark",
Language = 1
                }
            };

            _mockConfigService.LoadConfig().Returns(existingConfig);

            AppConfig? savedConfig = null;
            _mockConfigService.When(s => s.SaveConfig(Arg.Any<AppConfig>()))
                .Do(ci => savedConfig = ci.Arg<AppConfig>());

            _sut.Execute();

            _mockConfigService.Received(1).LoadConfig();
            _mockConfigService.Received(1).SaveConfig(Arg.Any<AppConfig>());

            savedConfig.ShouldNotBeNull();

            savedConfig.Settings.ApiUsername.ShouldBe(string.Empty);
            savedConfig.Settings.ApiKey.ShouldBe(string.Empty);

            savedConfig.Username.ShouldBe("HauptUser");
            savedConfig.Settings.Theme.ShouldBe("Dark");
            savedConfig.Settings.Language.ShouldBe(1);
        }

        [Fact]
        public void Execute_ShouldBubbleUpException_WhenLoadConfigFails()
        {
            _mockConfigService
                .LoadConfig()
                .Throws(new InvalidOperationException("Datei ist blockiert."));

            var exception = Should.Throw<InvalidOperationException>(() => _sut.Execute());
            exception.Message.ShouldBe("Datei ist blockiert.");

            _mockConfigService.DidNotReceive().SaveConfig(Arg.Any<AppConfig>());
        }

        [Fact]
        public void Execute_ShouldBubbleUpException_WhenSaveConfigFails()
        {
            _mockConfigService.LoadConfig().Returns(new AppConfig());
            _mockConfigService
                .When(s => s.SaveConfig(Arg.Any<AppConfig>()))
                .Do(_ => throw new UnauthorizedAccessException("Keine Schreibrechte."));

            var exception = Should.Throw<UnauthorizedAccessException>(() => _sut.Execute());
            exception.Message.ShouldBe("Keine Schreibrechte.");
        }
    }
}

