using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using HtmlAgilityPack;
using BOTGC.API.Dto;
using BOTGC.API.Common;
using Microsoft.Extensions.Logging;
using FakeItEasy;

namespace BOTGC.API.Tests.Services.CompetitionLeaderBoard
{
    public class IGLeaderboardReportParserTests
    {
        private async Task<(HtmlDocument doc, CompetitionSettingsDto settings, IGLeaderboardReportParser parser)> LoadTestDataAsync(string htmlFile, string jsonFile)
        {
            // Path relative to output directory
            var testDataDir = Path.Combine(AppContext.BaseDirectory, "Services", "CompetitionLeaderBoard", "TestData");

            var htmlPath = Path.Combine(testDataDir, htmlFile);
            var jsonPath = Path.Combine(testDataDir, jsonFile);

            if (!File.Exists(htmlPath))
                throw new FileNotFoundException($"HTML test data file not found: {htmlPath}");
            if (!File.Exists(jsonPath))
                throw new FileNotFoundException($"JSON test data file not found: {jsonPath}");

            var htmlContent = await File.ReadAllTextAsync(htmlPath);
            var jsonContent = await File.ReadAllTextAsync(jsonPath);

            var doc = new HtmlDocument();
            doc.LoadHtml(htmlContent);

            var competitionSettings = JsonSerializer.Deserialize<CompetitionSettingsDto>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var fakeLogger = FakeItEasy.A.Fake<ILogger<IGLeaderboardReportParser>>();
            var parser = new IGLeaderboardReportParser(fakeLogger);

            return (doc, competitionSettings, parser);
        }

        [Fact]
        public async Task ParseReport_MedalLeaderboardFinalised_ReturnsExpectedResults()
        {
            // Arrange
            var (doc, settings, parser) = await LoadTestDataAsync("medal_leaderboard_finalised.html", "medal_leaderboard_finalised_settings.json");

            // Act
            var results = await parser.ParseReport(doc, settings);

            // Assert
            Assert.NotNull(results);
            Assert.NotEmpty(results);
            var player = results[0].Players[0];

            Assert.Equal(1, player.Position);
            Assert.Equal("David Milsom", player.PlayerName);
            Assert.Equal(98768, player.PlayerId);
            Assert.Equal("26", player.PlayingHandicap);
            Assert.Equal("62", player.NetScore);
            Assert.Null(player.StablefordScore);
            Assert.Null(player.Thru);
            Assert.Equal("Back 9 - 32.0000, Back 6 - 25.3330, Back 3 - 10.6670, Back 1 - 2.5560", player.Countback);
        }

        [Fact]
        public async Task ParseReport_StablefordLeaderboardFinalised_ReturnsExpectedResults()
        {
            // Arrange
            var (doc, settings, parser) = await LoadTestDataAsync("stableford_leaderboard_finalised.html", "stableford_leaderboard_finalised_settings.json");

            // Act
            var results = await parser.ParseReport(doc, settings);

            // Assert
            Assert.NotNull(results);
            Assert.NotEmpty(results);
            var player = results[0].Players[0];

            Assert.Equal(1, player.Position);
            Assert.Equal("James Elrick", player.PlayerName);
            Assert.Equal(153, player.PlayerId);
            Assert.Equal("17", player.PlayingHandicap);
            Assert.Null(player.NetScore);
            Assert.Equal("46", player.StablefordScore);
            Assert.Null(player.Thru);
            Assert.Equal("Back 9 - 22, Back 6 - 16, Back 3 - 8, Back 1 - 2", player.Countback);
        }

        [Fact]
        public async Task ParseReport_MedalLeaderboardInProgress_ReturnsExpectedResults()
        {
            // Arrange
            var (doc, settings, parser) = await LoadTestDataAsync("medal_leaderboard_inprogress.html", "medal_leaderboard_inprogress_settings.json");

            // Act
            var results = await parser.ParseReport(doc, settings);

            // Assert
            Assert.NotNull(results);
            Assert.NotEmpty(results);
            var player = results[0].Players[0];

            // Replace these with the actual expected values from your test data
            Assert.Equal(1, player.Position);
            Assert.Equal("Daniel Clarke", player.PlayerName);
            Assert.Equal(638, player.PlayerId);
            Assert.Equal("8", player.PlayingHandicap);
            Assert.Equal("65", player.NetScore);
            Assert.Null(player.StablefordScore);
            Assert.Equal("18", player.Thru);
            Assert.Equal("Back 9 - 31.0000, Back 6 - 24.3330, Back 3 - 12.6670, Back 1 - 3.5560", player.Countback);

        }
    }
}
