using BOTGC.API.Common;
using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using BOTGC.API.Services.Queries;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Extensions;

namespace BOTGC.API.Services.QueryHandlers
{
    public class SetMemberPropertiesHandler(IOptions<AppSettings> settings,
                                            ILogger<SetMemberPropertiesHandler> logger,
                                            IDataProvider dataProvider) : QueryHandlerBase<SetMemberPropertiesQuery, bool>
    {
        private readonly AppSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        private readonly ILogger<SetMemberPropertiesHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IDataProvider _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));

        public async override Task<bool> Handle(SetMemberPropertiesQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var url = $"{_settings.IG.BaseUrl}{_settings.IG.Urls.UpdateMemberPropertiesUrl}".Replace("{memberid}", request.MemberId.ToString());

                var content = new StringContent($"paramid=1&user_id={request.MemberId}&param_value={request.Value}");

                var data = new Dictionary<string, string>
                {
                    { "paramid", ((int)request.Property).ToString() },
                    { "user_id", request.MemberId.ToString() },
                    { "param_value", request.Value }
                };

                var result = await _dataProvider.PostData(url, data);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to update property {request.Property.GetDisplayName()} for member {request.MemberId}", ex.Message);
                return false;
            }

            return true;
        }
    }
}
