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
    public class IGClubChampionshipsLeaderboardReportParserTests
    {
        private async Task<(HtmlDocument doc, CompetitionSettingsDto settings, IGClubChampionshipLeaderboardReportParser parser)> LoadTestDataAsync(string htmlFile, string jsonFile)
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

            var fakeLogger = FakeItEasy.A.Fake<ILogger<IGClubChampionshipLeaderboardReportParser>>();
            var parser = new IGClubChampionshipLeaderboardReportParser(fakeLogger);

            return (doc, competitionSettings, parser);
        }

        [Fact]
        public async Task ParseReport_Championship_R1_ReturnsExpectedResults()
        {
            // Arrange
            var (doc, settings, parser) = await LoadTestDataAsync("championship_r1.html", "championship.json");

            // Act
            var results = await parser.ParseReport(doc, settings);

            // Assert
            Assert.NotNull(results);
            Assert.NotEmpty(results);
            Assert.Equal(5, results.Count());

            var player1 = results[0];
            Assert.Equal(1, player1.Position);
            Assert.Equal("Philip Gill", player1.PlayerName);
            Assert.Equal("Back 9 - 22.5000, Back 6 - 15.6670, Back 3 - 5.8330, Back 1 - 1.9440", player1.Countback);
            Assert.Equal(80486, player1.PlayerId);
            Assert.Equal("-3", player1.Par);
            Assert.Equal("18", player1.Thru);
            Assert.Equal("68", player1.Score);

            var player2 = results[1];
            Assert.Equal(2, player2.Position);
            Assert.Equal("Boyd Brimsted", player2.PlayerName);
            Assert.Equal("Back 9 - 25.5000, Back 6 - 21.6670, Back 3 - 11.8330, Back 1 - 3.2780", player2.Countback);
            Assert.Equal(92851, player2.PlayerId);
            Assert.Equal("-1", player2.Par);
            Assert.Equal("18", player2.Thru);
            Assert.Equal("70", player2.Score);

            var player3 = results[2];
            Assert.Equal(3, player3.Position);
            Assert.Equal("Simon Parsons", player3.PlayerName);
            Assert.Equal("Back 9 - 30.5000, Back 6 - 20.6670, Back 3 - 7.8330, Back 1 - 1.9440", player3.Countback);
            Assert.Equal(83642, player3.PlayerId);
            Assert.Equal("+13", player3.Par);
            Assert.Equal("18", player3.Thru);
            Assert.Equal("84", player3.Score);

            var player4 = results[3];
            Assert.Equal(4, player4.Position);
            Assert.Equal("Seth Parsons", player4.PlayerName);
            Assert.Equal("Back 9 - 24.0000, Back 6 - 14.0000, Back 3 - 7.0000, Back 1 - 0.6670", player4.Countback);
            Assert.Equal(86207, player4.PlayerId);
            Assert.Equal("+37", player4.Par);
            Assert.Equal("18", player4.Thru);
            Assert.Equal("108", player4.Score);

            var player5 = results[4];
            Assert.Equal(5, player5.Position);
            Assert.Equal("Scott Someone", player5.PlayerName);
            Assert.Equal("Back 9 - 24.0000, Back 6 - 14.0000, Back 3 - 7.0000, Back 1 - 0.6670", player5.Countback);
            Assert.Equal(86207, player5.PlayerId);
            Assert.Equal("+37", player5.Par);
            Assert.Equal("18", player5.Thru);
            Assert.Equal("108", player5.Score);
        }

        [Fact]
        public async Task ParseReport_Championship_R2_ReturnsExpectedResults()
        {
            // Arrange
            var (doc, settings, parser) = await LoadTestDataAsync("championship_r2.html", "championship.json");

            // Act
            var results = await parser.ParseReport(doc, settings);

            // Assert
            Assert.NotNull(results);
            Assert.NotEmpty(results);
            Assert.Equal(4, results.Count);

            Assert.Equal(1, results[0].Position);
            Assert.Equal("Simon Parsons", results[0].PlayerName);
            Assert.Equal("-14", results[0].Par);
            Assert.Equal("18", results[0].Thru);
            Assert.Equal("57", results[0].Score);

            Assert.Equal(2, results[1].Position);
            Assert.Equal("Seth Parsons", results[1].PlayerName);
            Assert.Equal("-13", results[1].Par);
            Assert.Equal("11", results[1].Thru);
            Assert.Equal("58", results[1].Score);

            Assert.Equal(3, results[2].Position);
            Assert.Equal("Philip Gill", results[2].PlayerName);
            Assert.Equal("LEVEL", results[2].Par);
            Assert.Equal("8", results[2].Thru);
            Assert.Equal("71", results[2].Score);

            Assert.Equal(4, results[3].Position);
            Assert.Equal("Boyd Brimsted", results[3].PlayerName);
            Assert.Equal("+26", results[3].Par);
            Assert.Equal("18", results[3].Thru);
            Assert.Equal("97", results[3].Score);
        }

        [Fact]
        public async Task ParseReport_Championship_R1_Finalised_ReturnsExpectedResults()
        {
            // Arrange
            var (doc, settings, parser) = await LoadTestDataAsync("championship_r1_finalised.html", "championship.json");

            // Act
            var results = await parser.ParseReport(doc, settings);

            // Assert
            Assert.NotNull(results);
            Assert.NotEmpty(results);
            Assert.Equal(62, results.Count());

            var player1 = results[0];
            Assert.Equal(1, player1.Position);
            Assert.Equal("Joe Clarke", player1.PlayerName);
            Assert.Equal(910, player1.PlayerId);
            Assert.Equal("-2", player1.Par);
            Assert.Equal("18", player1.Thru);
            Assert.Equal("Back 9 - 36.0000, Back 6 - 24.0000, Back 3 - 12.0000, Back 1 - 3.0000", player1.Countback);
            Assert.Equal("69", player1.Score);

            var player2 = results[1];
            Assert.Equal(2, player2.Position);
            Assert.Equal("Mark Parker", player2.PlayerName);
            Assert.Equal(34, player2.PlayerId);
            Assert.Equal("+2", player2.Par);
            Assert.Equal("18", player2.Thru);
            Assert.Equal("Back 9 - 34.0000, Back 6 - 24.0000, Back 3 - 12.0000, Back 1 - 3.0000", player2.Countback);
            Assert.Equal("73", player2.Score);

            var player3 = results[2];
            Assert.Equal(3, player3.Position);
            Assert.Equal("Graham Flockett", player3.PlayerName);
            Assert.Equal(209, player3.PlayerId);
            Assert.Equal("+4", player3.Par);
            Assert.Equal("18", player3.Thru);
            Assert.Equal("Back 9 - 36.0000, Back 6 - 26.0000, Back 3 - 12.0000, Back 1 - 4.0000", player3.Countback);
            Assert.Equal("75", player3.Score);

            var player4 = results[3];
            Assert.Equal(4, player4.Position);
            Assert.Equal("Matthew Lisle", player4.PlayerName);
            Assert.Equal(94739, player4.PlayerId);
            Assert.Equal("+6", player4.Par);
            Assert.Equal("18", player4.Thru);
            Assert.Equal("Back 9 - 42.0000, Back 6 - 32.0000, Back 3 - 15.0000, Back 1 - 5.0000", player4.Countback);
            Assert.Equal("77", player4.Score);

            var player5 = results[4];
            Assert.Equal(5, player5.Position);
            Assert.Equal("Joseph Bateman", player5.PlayerName);
            Assert.Equal(83090, player5.PlayerId);
            Assert.Equal("+7", player5.Par);
            Assert.Equal("18", player5.Thru);
            Assert.Equal("Back 9 - 39.0000, Back 6 - 28.0000, Back 3 - 14.0000, Back 1 - 4.0000", player5.Countback);
            Assert.Equal("78", player5.Score);


        }
    }
}
