using BOTGC.API.Dto;

namespace BOTGC.API.Interfaces
{
    public interface IMemberApplicationFormPdfGeneratorService
    {
        byte[] GeneratePdf(NewMemberApplicationDto model);
    }
}
