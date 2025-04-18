using BOTGC.API.Models;
using System.Drawing;

namespace BOTGC.API.Interfaces
{
    public interface ICognitiveServices
    {
        Task<List<FaceRectangle>> DetectPrimaryFacesInImage(Image image);
    }
}