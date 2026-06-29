using eBRestarter.Core.Application.Extensions;
using Shouldly;
using System.Globalization;
using Xunit;

namespace eBRestarter.XUnit.Test.Core.Domain.Extension.Logic
{
        public class FormatExtensionsTestsTests
        {

        [Theory]
        [InlineData(0, "0 B")]
        [InlineData(512, "512 B")]
        [InlineData(1023, "1023 B")]
        [InlineData(1024, "1.0 KB")]
        [InlineData(1536, "1.5 KB")]
        [InlineData(1048576, "1.0 MB")]
        [InlineData(1073741824, "1.0 GB")]
        public void ToSizeSuffix_ShouldFormatCorrectly_WithDefaultSettings(long inputBytes, string expectedResult)
        {
            var result = inputBytes.ToSizeSuffix(1, CultureInfo.InvariantCulture);

            result.ShouldBe(expectedResult);
        }

         [Theory]
         [InlineData(-512, "-512 B")]
         [InlineData(-1048576, "-1.0 MB")]
         public void ToSizeSuffix_ShouldHandleNegativeNumbers(long inputBytes, string expectedResult)
         {
             var result = inputBytes.ToSizeSuffix(1, CultureInfo.InvariantCulture);

             result.ShouldBe(expectedResult);
         }

        [Fact]
        public void ToSizeSuffix_ShouldRespectDecimalPlaces()
        {
        const long inputBytes = 1310720;

        var oneDecimal = inputBytes.ToSizeSuffix(1, CultureInfo.InvariantCulture);
            var twoDecimals = inputBytes.ToSizeSuffix(2, CultureInfo.InvariantCulture);
            var zeroDecimals = inputBytes.ToSizeSuffix(0, CultureInfo.InvariantCulture);

            oneDecimal.ShouldBe("1.3 MB");
            twoDecimals.ShouldBe("1.25 MB");
            zeroDecimals.ShouldBe("1 MB");
        }

        [Fact]
        public void ToSizeSuffix_ShouldRespectCultureInfo()
        {
            const long inputBytes = 1536;

            var germanCulture = new CultureInfo("de-DE");
            var englishCulture = new CultureInfo("en-US");

            var germanResult = inputBytes.ToSizeSuffix(1, germanCulture);
            var englishResult = inputBytes.ToSizeSuffix(1, englishCulture);

            germanResult.ShouldBe("1,5 KB");
            englishResult.ShouldBe("1.5 KB");
        }

        [Fact]
        public void ToSizeSuffix_ShouldNotThrowOnExtremelyLargeNumbers()
        {
        const long maxLong = long.MaxValue;

        var result = maxLong.ToSizeSuffix(2, CultureInfo.InvariantCulture);

            result.ShouldEndWith("EB");

            result.ShouldBe("8.00 EB");
        }
        }
    }
