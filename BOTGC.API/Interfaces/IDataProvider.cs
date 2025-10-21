using BOTGC.API.Dto;

namespace BOTGC.API.Interfaces
{
    public interface IDataProvider
    {
        Task<List<T>> GetData<T, TMetadata>(string reportUrl, IReportParserWithMetadata<T, TMetadata> parser, TMetadata metadata, string? cacheKey = null, TimeSpan? cacheTTL = null, Func<T, List<HateoasLink>>? linkBuilder = null) where T : HateoasResource, new();
        Task<List<T>> GetData<T>(string reportUrl, IReportParser<T> parser, string? cacheKey = null, TimeSpan? cacheTTL = null, Func<T, List<HateoasLink>>? linkBuilder = null) where T : HateoasResource, new();
        Task<string> GetData(string reportUrl, string? cacheKey = null, TimeSpan? cacheTTL = null);
        Task<string?> PostData(string reportUrl, Dictionary<string, string> data);
        Task<List<T>> PostData<T>(string reportUrl, Dictionary<string, string> data, IReportParser<T> parser, string? cacheKey = null, TimeSpan? cacheTTL = null, Func<T, List<HateoasLink>>? linkBuilder = null) where T : HateoasResource, new();
    }

}
