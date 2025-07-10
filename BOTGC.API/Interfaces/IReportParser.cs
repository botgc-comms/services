using HtmlAgilityPack;

namespace BOTGC.API.Interfaces
{
    public interface IReportParser<T>
    {
        Task<List<T>> ParseReport(HtmlDocument document);
    }

    public interface IReportParserWithMetadata<T, TMetadata> : IReportParser<T>
    {
        Task<List<T>> ParseReport(HtmlDocument document, TMetadata metadata);
    }
}
