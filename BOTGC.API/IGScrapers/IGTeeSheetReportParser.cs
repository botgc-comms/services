using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using HtmlAgilityPack;
using System.Globalization;

namespace BOTGC.API.IGScrapers
{
    public class IGTeeSheetReportParser : IReportParser<TeeSheetDto>
    {
        private readonly ILogger<IGTeeSheetReportParser> _logger;

        public IGTeeSheetReportParser(ILogger<IGTeeSheetReportParser> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<TeeSheetDto>> ParseReport(HtmlDocument document)
        {
            var teeSheets = new List<TeeSheetDto>();
            var playerMap = new Dictionary<string, PlayerTeeTimesDto>();
            var teeTimePlayers = new List<TeeTimePlayersDto>();

            var rows = document.DocumentNode.SelectNodes("//tr[contains(@class, 'teetime-mins')]");
            if (rows == null || rows.Count == 0)
            {
                _logger.LogWarning("No tee times found in the report.");
                return teeSheets;
            }

            foreach (var row in rows)
            {
                try
                {
                    var timeCell = row.SelectSingleNode("./th[contains(@class, 'slot-time')]");
                    if (timeCell == null) continue;

                    string timeText = timeCell.InnerText.Trim();
                    DateTime teeTime;
                    if (!DateTime.TryParseExact(timeText, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out teeTime))
                    {
                        _logger.LogWarning("Skipping row due to invalid time format: {TimeText}", timeText);
                        continue;
                    }

                    var playerCells = row.SelectNodes("./td");
                    if (playerCells == null) continue;

                    var teeTimeDto = new TeeTimePlayersDto
                    {
                        Time = teeTime,
                        Players = new List<PlayerDto>()
                    };

                    teeTimePlayers.Add(teeTimeDto);

                    foreach (var playerCell in playerCells)
                    {
                        var bookedForCompetition = playerCell.OuterHtml.Contains("Booked for competition");

                        var playerDiv = playerCell.SelectSingleNode(".//div[contains(@class, 'player-tee')]");
                        if (playerDiv == null) playerDiv = playerCell.SelectSingleNode(".//span[contains(@class, 'player-name')]");

                        if (playerDiv != null)
                        {
                            string playerName = playerDiv.InnerText.Trim();
                            if (!string.IsNullOrEmpty(playerName))
                            {
                                if (!playerMap.TryGetValue(playerName, out var playerDto))
                                {
                                    playerDto = new PlayerTeeTimesDto
                                    {
                                        FullName = playerName,
                                        TeeTimes = new List<TeeTimeBookingDto>()
                                    };
                                    playerMap[playerName] = playerDto;

                                    teeTimeDto.Players.Add(playerDto);
                                }

                                var playerTeeTimeBooking = new TeeTimeBookingDto()
                                {
                                    Time = teeTimeDto.Time,
                                    IsCompetitionBooking = bookedForCompetition
                                };

                                playerDto.TeeTimes.Add(playerTeeTimeBooking);
                            }
                        }
                    }

                    var dateAttr = row.SelectSingleNode(".//td[@data-date]")?.GetAttributeValue("data-date", null);
                    if (dateAttr == null || !DateTime.TryParseExact(dateAttr, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate))
                    {
                        _logger.LogWarning("Skipping row due to missing or invalid date.");
                        continue;
                    }

                    var teeSheetDto = teeSheets.FirstOrDefault(ts => ts.Date == parsedDate);
                    if (teeSheetDto == null)
                    {
                        teeSheetDto = new TeeSheetDto { Date = parsedDate, TeeTimes = new List<TeeTimePlayersDto>(), Players = new List<PlayerTeeTimesDto>() };
                        teeSheets.Add(teeSheetDto);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing tee time row.");
                }
            }

            foreach (var teeSheet in teeSheets)
            {
                teeSheet.Players = playerMap.Values.ToList();
                teeSheet.TeeTimes = teeTimePlayers.ToList();
            }

            _logger.LogInformation("Successfully parsed {Count} tee sheets.", teeSheets.Count);
            return teeSheets;
        }
    }
}
