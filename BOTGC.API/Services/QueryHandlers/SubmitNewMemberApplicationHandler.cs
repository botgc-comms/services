using BOTGC.API.Common;
using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using BOTGC.API.Services.Queries;
using MediatR;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Services.QueryHandlers
{
    public class SubmitNewMemberApplicationHandler(IOptions<AppSettings> settings,
                                                   IMediator mediator,
                                                   ILogger<SubmitNewMemberApplicationHandler> logger,
                                                   IDataProvider dataProvider,
                                                   IReportParser<NewMemberResponseDto> reportParser) : QueryHandlerBase<SubmitNewMemberApplicactionCommand, NewMemberApplicationResultDto?>
    {
        private readonly AppSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        private readonly ILogger<SubmitNewMemberApplicationHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IDataProvider _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
        private readonly IReportParser<NewMemberResponseDto> _reportParser = reportParser ?? throw new ArgumentNullException(nameof(reportParser));

        public async override Task<NewMemberApplicationResultDto?> Handle(SubmitNewMemberApplicactionCommand request, CancellationToken cancellationToken)
        {
            var url = $"{_settings.IG.BaseUrl}{_settings.IG.Urls.NewMembershipApplicationUrl}";
            MemberCDHLookupDto? cdhLookup = null;

            var application = request.Application ?? throw new ArgumentNullException(nameof(request.Application), "New member application cannot be null.");

            _logger.LogInformation($"Submitting new member application for {application.Forename} {application.Surname} ({application.Email}).");

            if (!string.IsNullOrEmpty(application.CdhId))
            {
                _logger.LogInformation($"Looking up CDH ID details for {application.CdhId}.");

                var lookupMemberCDHIdDetailsQuery = new LookupMemberCDHIdDetailsQuery
                {
                    CDHId = application.CdhId
                };
                                
                cdhLookup = await _mediator.Send(lookupMemberCDHIdDetailsQuery, cancellationToken);
            }

            var data = IGMembershipApplicationMapper.MapToFormData(application, cdhLookup);

            var result = await _dataProvider.PostData(url, data, _reportParser);

            if (result != null && result.Any())
            {
                return new NewMemberApplicationResultDto
                {
                    Application = application,
                    ApplicationId = application.ApplicationId,
                    MemberId = result[0].MemberId
                };
            }

            return null;
        }
    }
}
