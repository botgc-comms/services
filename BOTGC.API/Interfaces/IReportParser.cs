using HtmlAgilityPack;

namespace BOTGC.API.Interfaces
{
    public interface IReportParser<T>
    {
        List<T> ParseReport(HtmlDocument document);
    }
}
