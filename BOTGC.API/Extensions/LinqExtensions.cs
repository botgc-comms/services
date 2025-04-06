namespace BOTGC.API.Extensions
{
    public static class LinqExtensions
    {
        public static IEnumerable<IEnumerable<T>> Chunk<T>(this IEnumerable<T> source, int size)
        {
            return source.Select((x, i) => new { Index = i, Value = x })
                            .GroupBy(x => x.Index / size)
                            .Select(g => g.Select(x => x.Value));
        }
    }
}
