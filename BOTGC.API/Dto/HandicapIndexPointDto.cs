namespace BOTGC.API.Dto
{
    /// <summary>
    /// A single handicap index datapoint.
    /// </summary>
    public class HandicapIndexPointDto: HateoasResource
    {
        public DateTime Date { get; set; }
        public double Index { get; set; }
    }
}
