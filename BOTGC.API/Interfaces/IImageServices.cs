﻿namespace Services.Interfaces
{
    public interface IImageServices
    {
        Task<Stream?> CropAndCentreFacesAsync(Stream imageStream);
    }
}