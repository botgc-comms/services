using BOTGC.API.Interfaces;
using BOTGC.API.Services.Queries;
using System.Text.RegularExpressions;

namespace BOTGC.API.Services
{
    public class DefaultCacheKeyProvider<TRequest> : ICacheKeyProvider<TRequest>
        where TRequest : QueryBase<TRequest>
    {
        public string GetCacheKey(TRequest request)
        {
            var typeName = typeof(TRequest).Name.Replace("Query", "");
            var snakeTypeName = Regex.Replace(typeName, @"(?<=[a-z0-9])([A-Z])", "_$1");
            var props = typeof(TRequest).GetProperties();
            var keyParts = props.Select(p => $"{p.Name}_{p.GetValue(request)}");
            return $"{snakeTypeName}_{string.Join("_", keyParts)}";
        }
    }
}
