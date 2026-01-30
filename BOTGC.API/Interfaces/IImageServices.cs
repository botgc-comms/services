namespace BOTGC.API.Interfaces;

public interface IImageServices
{
    Task<Stream?> CropAndCentreFacesAsync(Stream imageStream);
}
