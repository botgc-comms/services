using HtmlAgilityPack;

namespace Services.Interfaces
{
    public interface IReportParser<T>
    {
        List<T> ParseReport(HtmlDocument document);
    }
}
