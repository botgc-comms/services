using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Services;
using Services.Dto;
using Services.Interfaces;
using Services.Services.CompetitionProcessors;
using Xunit;

namespace BOTGC.API.Tests.Services.CompetitionProcessors
{
    public class JuniorEclecticCompetitionProcessorTests
    {
        private readonly IDataService _mockReportService;
        private readonly ILogger<JuniorEclecticCompetitionProcessor> _mockLogger;
        private readonly JuniorEclecticCompetitionProcessor _processor;

        public JuniorEclecticCompetitionProcessorTests()
        {
            _mockReportService = A.Fake<IDataService>();
            _mockLogger = A.Fake<ILogger<JuniorEclecticCompetitionProcessor>>();

            var mockSettings = A.Fake<IOptions<AppSettings>>();
            A.CallTo(() => mockSettings.Value).Returns(new AppSettings());

            _processor = new JuniorEclecticCompetitionProcessor(mockSettings, _mockLogger, _mockReportService, null);
        }

        public static IEnumerable<ITheoryDataRow> GetTestData()
        {
            var testFiles = Directory.GetFiles(Path.Combine("Services", "CompetitionProcessors", "TestData", "JuniorEclecticCompetitionProcessor"), "*.json");

            foreach (var file in testFiles)
            {
                if (Regex.IsMatch(file, "X[^.]+[.]json$")) continue;

                var json = File.ReadAllText(file);
                var testCase = JsonSerializer.Deserialize<JuniorEclecticCompetitionProcessorTestCase>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                yield return new TheoryDataRow(testCase)
                    .WithTestDisplayName(testCase.TestName ?? Path.GetFileNameWithoutExtension(file));
            }
        }

        [Theory]
        [MemberData(nameof(GetTestData))]
        public async Task ProcessCompetitionAsync_ProducesCorrectEclecticScorecard(JuniorEclecticCompetitionProcessorTestCase testCase)
        {
            // Mock `GetJuniorMembersAsync`
            A.CallTo(() => _mockReportService.GetJuniorMembersAsync())
                .Returns(Task.FromResult(testCase.Mocks.GetJuniorMembersAsync.Output));

            // Mock `GetRoundsByMemberIdAsync`
            foreach (var roundMock in testCase.Mocks.GetRoundsByMemberIdAsync)
            {
                var memberId = roundMock.Input["MemberId"];

                A.CallTo(() => _mockReportService.GetRoundsByMemberIdAsync(memberId))
                    .Returns(Task.FromResult(roundMock.Output));
            }

            // Mock `GetScorecardForRoundAsync`
            foreach (var scorecardMock in testCase.Mocks.GetScorecardForRoundAsync)
            {
                var roundId = scorecardMock.Input["RoundId"];

                A.CallTo(() => _mockReportService.GetScorecardForRoundAsync(roundId))
                    .Returns(Task.FromResult(scorecardMock.Output));
            }

            // ✅ Act: Run the competition processing
            var results = await _processor.GetCompetitionResultAsync(testCase.Input.FromDate, testCase.Input.ToDate, CancellationToken.None);

            Assert.NotNull(results);
            Assert.NotNull(results.Scores);
            Assert.Single(results.Scores);

            var result = results.Scores.First();
            var expectedScorecard = testCase.Expected.Scores.First().Scorecard;
            var expectedExclusions = testCase.Expected.Scores.First().ExcludedRounds;

            // ✅ Assert: Compare expected EclecticScorecardDto
            Assert.Equal(expectedScorecard.PlayerName, result.Scorecard.PlayerName);
            Assert.Equal(expectedScorecard.TotalStablefordScore, result.Scorecard.TotalStablefordScore);

            foreach (var expectedHole in result.Scorecard.Holes)
            {
                var actualHole = result.Scorecard.Holes.FirstOrDefault(h => h.HoleNumber == expectedHole.HoleNumber);
                Assert.NotNull(actualHole);
                Assert.Equal(expectedHole.StablefordScore, actualHole.StablefordScore);
                Assert.Equal(expectedHole.RoundId, actualHole.RoundId);
                Assert.Equal(expectedHole.RoundDate, actualHole.RoundDate);

                // Check uncounted scores
                Assert.Equal(expectedHole.UncountedScores.Count, actualHole.UncountedScores.Count);
                foreach (var expectedUncounted in expectedHole.UncountedScores)
                {
                    var actualUncounted = actualHole.UncountedScores.FirstOrDefault(u => u.RoundId == expectedUncounted.RoundId);
                    Assert.NotNull(actualUncounted);
                    Assert.Equal(expectedUncounted.StablefordScore, actualUncounted.StablefordScore);
                }
            }

            Assert.Equal(expectedExclusions.Count, result.ExcludedRounds.Count);

            foreach (var eer in expectedExclusions)
            {
                var r = result.ExcludedRounds.Where(er => er.MemberId == eer.MemberId && er.RoundId == eer.RoundId && eer.Type == er.Type).FirstOrDefault();
                Assert.NotNull(r);

                Assert.Equal(eer.DatePlayed, r.DatePlayed);
                Assert.Equal(eer.ExclusionReason, r.ExclusionReason);
            }
        }
    }

    // ✅ Wrapper for test cases
    public class JuniorEclecticCompetitionProcessorTestCase
    {
        public string? TestName { get; set; } = default;
        public JuniorEclecticCompetitionProcessorTestInput Input { get; set; } = default!;
        public JuniorEclecticCompetitionProcessorTestMocks Mocks { get; set; } = default!;
        public EclecticCompetitionResultsDto Expected { get; set; } = default!;
    }

    public class JuniorEclecticCompetitionProcessorTestInput
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
    }

    public class JuniorEclecticCompetitionProcessorTestMocks
    {
        public ResponseWrapper<List<MemberDto>> GetJuniorMembersAsync { get; set; } = default!;
        public List<ResponseWrapper<List<RoundDto>>> GetRoundsByMemberIdAsync { get; set; } = new();
        public List<ResponseWrapper<ScorecardDto>> GetScorecardForRoundAsync { get; set; } = new();
    }

    public class ResponseWrapper<T>
    {
        public Dictionary<string, string> Input { get; set; } = new();
        public T Output { get; set; } = default!;
    }
}
