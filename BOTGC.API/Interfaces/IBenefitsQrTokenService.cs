using BOTGC.API.Dto;

namespace BOTGC.API.Interfaces;

public interface IBenefitsQrTokenService
{
    string CreateToken(BenefitsQrPayloadDto payload);
    BenefitsQrPayloadDto? TryDecrypt(string token);
}