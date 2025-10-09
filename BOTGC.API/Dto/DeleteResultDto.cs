namespace BOTGC.API.Dto
{
    public class DeleteResultDto
    {
        public Guid EntryId { get; set; }
        public bool Found { get; set; }

        public DeleteResultDto(Guid entryId, bool found)
        {
            EntryId = entryId;
            Found = found;
        }

        public DeleteResultDto(bool found)
        {
            Found = found;
        }
    };
}
