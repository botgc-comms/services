using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using Microsoft.Extensions.Options;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Services.Interfaces;

using Rectangle = Services.Models.FaceRectangle;

namespace Services.Services
{
    public class AzureCognitiveServices : ICognitiveServices
    {
        private readonly AppSettings _settings;
        private readonly ILogger<AzureCognitiveServices> _logger;
        private readonly IFaceClient _faceClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureCognitiveServices"/> class.
        /// </summary>
        /// <param name="settings">Application settings.</param>
        /// <param name="logger">Logger instance.</param>
        public AzureCognitiveServices(IOptions<AppSettings> settings, ILogger<AzureCognitiveServices> logger)
        {
            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _faceClient = new FaceClient(new ApiKeyServiceClientCredentials(_settings.AzureFaceApi.SubscriptionKey))
            {
                Endpoint = _settings.AzureFaceApi.EndPoint
            };
        }

        /// <summary>
        /// Detects and returns the primary faces from the provided image.
        /// </summary>
        /// <param name="image">The image to process.</param>
        /// <returns>A list of primary face rectangles.</returns>
        public async Task<List<Rectangle>> DetectPrimaryFacesInImage(Image image)
        {
            if (image == null) throw new ArgumentNullException(nameof(image));

            using var memoryStream = new MemoryStream();
            image.Save(memoryStream, image.RawFormat);
            memoryStream.Position = 0;

            try
            {
                var detectedFaces = await _faceClient.Face.DetectWithStreamAsync(
                    memoryStream,
                    returnFaceId: false,
                    returnFaceLandmarks: false,
                    returnFaceAttributes: null,
                    recognitionModel: "recognition_04",
                    returnRecognitionModel: false,
                    detectionModel: "detection_03"
                );

                if (detectedFaces == null || !detectedFaces.Any())
                {
                    _logger.LogWarning("No faces detected in the provided image.");
                    return new List<Rectangle>();
                }

                var primaryFaces = GetPrimaryFaces(detectedFaces, image.Width, image.Height);

                return primaryFaces.Select(f => new Rectangle()
                {
                    Height = f.FaceRectangle.Height, 
                    Width = f.FaceRectangle.Width, 
                    Left = f.FaceRectangle.Left, 
                    Top = f.FaceRectangle.Top
                }).ToList();
            }
            catch (APIErrorException ex)
            {
                _logger.LogError(ex, "Azure Face API error occurred.");
                return new List<Rectangle>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while detecting faces.");
                return new List<Rectangle>();
            }
        }

        /// <summary>
        /// Identifies the primary faces based on face sizes and relative differences.
        /// </summary>
        private IList<DetectedFace> GetPrimaryFaces(IList<DetectedFace> detectedFaces, int imageWidth, int imageHeight)
        {
            if (detectedFaces == null || detectedFaces.Count < 2)
                throw new ArgumentException("At least 2 faces must be detected.");

            // Step 1: Calculate face areas
            var faceAreas = detectedFaces
                .Select(face => face.FaceRectangle.Width * face.FaceRectangle.Height)
                .ToList();

            // Step 2: Sort faces by area (largest first)
            var sortedFaces = detectedFaces
                .Zip(faceAreas, (face, area) => new { Face = face, Area = area })
                .OrderByDescending(item => item.Area)
                .ToList();

            // Step 3: Identify the relative size difference
            var sizeDifferences = sortedFaces
                .Select((item, index) => index < sortedFaces.Count - 1
                    ? (double)sortedFaces[index].Area / sortedFaces[index + 1].Area
                    : 1.0) // Last face has no next face to compare
                .ToList();

            // Step 4: Find the first large gap (where a face is much bigger than the next)
            double threshold = 1.8; // Faces must be at least 80% larger than the next face to consider a gap
            int cutoffIndex = sizeDifferences.FindIndex(diff => diff >= threshold);

            // Step 5: Apply a minimum size filter (ignore faces smaller than 60% of the largest)
            double largestFaceArea = sortedFaces.First().Area;
            double sizeThreshold = largestFaceArea * 0.6;

            var primaryFaces = sortedFaces
                .Take(cutoffIndex > 0 ? cutoffIndex + 1 : sortedFaces.Count) // Keep faces up to the cutoff
                .Where(item => item.Area >= sizeThreshold) // Remove small faces
                .Select(item => item.Face)
                .ToList();

            // Step 6: Ensure at least 2 faces are always selected
            if (primaryFaces.Count < 2)
            {
                primaryFaces = sortedFaces
                    .Take(2)
                    .Select(item => item.Face)
                    .ToList();
            }

            return primaryFaces;
        }
    }
}


//[HttpGet("winnerImage/{slug}")]
//public async Task<IActionResult> GetImageWithFaceRectangles(string slug)
//{
//    var trophy = _trophyService.GetTrophyBySlug(slug);
//    if (trophy == null || string.IsNullOrEmpty(trophy.WinnerImage))
//    {
//        return NotFound($"No winner image could be found for trophy {slug}");
//    }

//    var imagePath = Path.Combine(_env.WebRootPath, trophy.WinnerImage.TrimStart('\\', '/'));

//    if (!System.IO.File.Exists(imagePath))
//    {
//        return NotFound($"The winner image file {imagePath} was not found");
//    }


//}

//private Image RestoreOriginalOrientation(Image image, Image originalImage)
//{
//    const int ExifOrientationTag = 0x112; // Orientation tag
//    var orientationProperty = originalImage.PropertyItems.FirstOrDefault(p => p.Id == ExifOrientationTag);

//    if (orientationProperty != null)
//    {
//        int orientation = BitConverter.ToUInt16(orientationProperty.Value, 0);
//        switch (orientation)
//        {
//            case 3: // Rotated 180 degrees
//                image.RotateFlip(RotateFlipType.Rotate180FlipNone);
//                break;
//            case 6: // Rotated 90 degrees counter-clockwise
//                image.RotateFlip(RotateFlipType.Rotate270FlipNone);
//                break;
//            case 8: // Rotated 90 degrees clockwise
//                image.RotateFlip(RotateFlipType.Rotate90FlipNone);
//                break;
//        }
//    }

//    return image;
//}



//// Helper method to draw a rectangle on the image
//private void DrawRectangle(Graphics graphics, FaceRectangle faceRectangle, Color color, int thickness)
//{
//    var rect = new Rectangle(faceRectangle.Left, faceRectangle.Top, faceRectangle.Width, faceRectangle.Height);
//    using var pen = new Pen(color, thickness);
//    graphics.DrawRectangle(pen, rect);
//}

//private void DrawRectangle(Graphics graphics, Rectangle rectangle, Color color, int thickness)
//{
//    using var pen = new Pen(color, thickness);
//    graphics.DrawRectangle(pen, rectangle);
//}

//private Rectangle CalculateCropBox(IList<DetectedFace> primaryFaces, int imageWidth, int imageHeight)
//{
//    // Aspect ratio of the crop box: 800x920
//    const double aspectRatio = 800.0 / 920.0;

//    // Calculate the bounding rectangle for primary faces
//    int minX = primaryFaces.Min(face => face.FaceRectangle.Left);
//    int maxX = primaryFaces.Max(face => face.FaceRectangle.Left + face.FaceRectangle.Width);
//    int minY = primaryFaces.Min(face => face.FaceRectangle.Top);
//    int maxY = primaryFaces.Max(face => face.FaceRectangle.Top + face.FaceRectangle.Height);

//    // Add padding around the faces (25% of the width and height of the bounding box)
//    int faceBoxWidth = maxX - minX;
//    int faceBoxHeight = maxY - minY;
//    int paddingX = (int)(faceBoxWidth * 0.25); // 25% horizontal padding
//    int paddingY = (int)(faceBoxHeight * 0.25); // 25% vertical padding

//    // Expand the bounding box with padding
//    minX = Math.Max(0, minX - paddingX);
//    maxX = Math.Min(imageWidth, maxX + paddingX);
//    minY = Math.Max(0, minY - paddingY);
//    maxY = Math.Min(imageHeight, maxY + paddingY);

//    // Adjust dimensions to maintain the aspect ratio
//    int cropWidth = maxX - minX;
//    int cropHeight = maxY - minY;

//    if (cropWidth / (double)cropHeight < aspectRatio)
//    {
//        // Adjust width to match aspect ratio
//        int adjustedWidth = (int)(cropHeight * aspectRatio);
//        int widthDiff = adjustedWidth - cropWidth;
//        minX = Math.Max(0, minX - widthDiff / 2);
//        maxX = Math.Min(imageWidth, maxX + widthDiff / 2);
//    }
//    else
//    {
//        // Adjust height to match aspect ratio
//        int adjustedHeight = (int)(cropWidth / aspectRatio);
//        int heightDiff = adjustedHeight - cropHeight;
//        minY = Math.Max(0, minY - heightDiff / 2);
//        maxY = Math.Min(imageHeight, maxY + heightDiff / 2);
//    }

//    // Final crop dimensions
//    cropWidth = maxX - minX;
//    cropHeight = maxY - minY;

//    // Vertically align faces at 1/3 of the crop box height from the top
//    int faceCenterY = minY + (maxY - minY) / 2;
//    int cropTop = Math.Max(0, faceCenterY - (int)(cropHeight * (1.0 / 3.0)));
//    int cropBottom = cropTop + cropHeight;

//    if (cropBottom > imageHeight)
//    {
//        cropTop = imageHeight - cropHeight;
//        cropBottom = imageHeight;
//    }

//    return new Rectangle(minX, cropTop, cropWidth, cropHeight);
//}



