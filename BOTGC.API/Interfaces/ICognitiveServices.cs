using Services.Models;
using System.Drawing;

namespace Services.Interfaces
{
    public interface ICognitiveServices
    {
        Task<List<FaceRectangle>> DetectPrimaryFacesInImage(Image image);
    }
}