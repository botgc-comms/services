using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BOTGC.API.Common;
using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using BOTGC.API.Services.Queries;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Services.QueryHandlers
{
    public sealed class GetManualCompetitionResultsHandler(IOptions<AppSettings> settings,
                                                           ILogger<GetManualCompetitionResultsHandler> logger,
                                                           IDataProvider dataProvider,
                                                           IReportParser<ManualCompetitionResultDto> reportParser)
        : QueryHandlerBase<GetManualCompetitionResultsQuery, ReadOnlyCollection<ManualCompetitionResultDto>>
    {
        private const string __CACHE_KEY = "Manual_Competition_Results:{year}";

        private readonly AppSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        private readonly ILogger<GetManualCompetitionResultsHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IDataProvider _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
        private readonly IReportParser<ManualCompetitionResultDto> _reportParser = reportParser ?? throw new ArgumentNullException(nameof(reportParser));

        public async override Task<ReadOnlyCollection<ManualCompetitionResultDto>> Handle(GetManualCompetitionResultsQuery request, CancellationToken cancellationToken)
        {
            var year = request.Year;
            var cacheKey = __CACHE_KEY.Replace("{year}", year.ToString(CultureInfo.InvariantCulture));

            var reportUrl = $"{_settings.IG.BaseUrl}{_settings.IG.Urls.TrophyCompetitionsUrl}";

            var results = await _dataProvider.GetData<ManualCompetitionResultDto>(
                reportUrl,
                _reportParser,
                cacheKey,
                TimeSpan.FromMinutes(_settings.Cache.ShortTerm_TTL_mins));

            if (results == null || results.Count == 0)
            {
                _logger.LogWarning("No manual competition results returned from trophy competitions page for {Year}.", year);
                return new ReadOnlyCollection<ManualCompetitionResultDto>(new List<ManualCompetitionResultDto>());
            }

            var filtered = results
                .Where(r => IsForYear(r, year))
                .ToList();

            _logger.LogInformation("Returning {Count} manual competition results for {Year}.", filtered.Count, year);

            return new ReadOnlyCollection<ManualCompetitionResultDto>(filtered);
        }

        private static bool IsForYear(ManualCompetitionResultDto dto, int year)
        {
            if (dto == null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(dto.Date))
            {
                return false;
            }

            if (int.TryParse(dto.Date, NumberStyles.Integer, CultureInfo.InvariantCulture, out var yearOnly))
            {
                return yearOnly == year;
            }

            if (DateTime.TryParse(dto.Date, CultureInfo.GetCultureInfo("en-GB"), DateTimeStyles.AssumeLocal, out var parsed))
            {
                return parsed.Year == year;
            }

            return false;
        }
    }
}
