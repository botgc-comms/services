using Microsoft.Extensions.Logging;
using Services.Interfaces;
using Services.Models;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Resources;
using System.Threading.Tasks;

namespace Services.Services
{
    /// <summary>
    /// Service for processing images, including cropping and centering faces.
    /// </summary>
    public class ImageServices : IImageServices
    {
        private readonly ILogger<ImageServices> _logger;
        private readonly ICognitiveServices _cognitive;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageServices"/> class.
        /// </summary>
        /// <param name="logger">Logger instance.</param>
        /// <param name="cognitive">Cognitive services instance for face detection.</param>
        public ImageServices(ILogger<ImageServices> logger, ICognitiveServices cognitive)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cognitive = cognitive ?? throw new ArgumentNullException(nameof(cognitive));
        }

        /// <summary>
        /// Crops and centers faces in an image.
        /// </summary>
        /// <param name="imageStream">The input image stream.</param>
        /// <returns>The processed image stream.</returns>
        public async Task<Stream?> CropAndCentreFacesAsync(Stream imageStream)
        {
            if (imageStream == null)
            {
                _logger.LogWarning("Null image stream provided for face cropping.");
                return null;
            }

            _logger.LogInformation("Processing image to detect and center faces...");

            using var image = Image.FromStream(imageStream);

            _logger.LogInformation("Detecting faces...");
            var detectedFaces = await _cognitive.DetectPrimaryFacesInImage(image);
            if (!detectedFaces.Any())
            {
                _logger.LogWarning("No faces detected in the image.");
                return null;
            }

            //var finalImage = FixImageOrientation(image);
            var finalImage = RotateImageToCorrectOrientation(image);

            _logger.LogInformation("Calculating crop box...");
            var cropBox = CalculateCropBox(detectedFaces, finalImage.Width, finalImage.Height);

            _logger.LogInformation("Cropping image...");
            var croppedImage = CropImage(finalImage, cropBox);
            
            var memoryStream = new MemoryStream();
            croppedImage.Save(memoryStream, ImageFormat.Jpeg);
            memoryStream.Seek(0, SeekOrigin.Begin);

            _logger.LogInformation("Image processing completed.");
            return memoryStream;
        }

        private static Image CropImage(Image image, Rectangle cropBox)
        {
            var croppedBitmap = new Bitmap(cropBox.Width, cropBox.Height);
            using (var graphics = Graphics.FromImage(croppedBitmap))
            {
                graphics.DrawImage(image, new Rectangle(0, 0, cropBox.Width, cropBox.Height), cropBox, GraphicsUnit.Pixel);
            }
            return croppedBitmap;
        }

        private Image RotateImageToCorrectOrientation(Image image)
        {
            const int ExifOrientationTag = 0x112;
            var orientationProperty = image.PropertyItems.FirstOrDefault(p => p.Id == ExifOrientationTag);

            if (orientationProperty != null && orientationProperty.Value != null)
            {
                int orientation = BitConverter.ToUInt16(orientationProperty.Value, 0);
                switch (orientation)
                {
                    case 3: // 180 degrees
                        image.RotateFlip(RotateFlipType.Rotate180FlipNone);
                        break;
                    case 6: // 90 degrees clockwise (portrait mode)
                        image.RotateFlip(RotateFlipType.Rotate90FlipNone);
                        break;
                    case 8: // 90 degrees counter-clockwise
                        image.RotateFlip(RotateFlipType.Rotate270FlipNone);
                        break;
                }
            }

            return image;
        }

        private static Rectangle CalculateCropBox(IEnumerable<FaceRectangle> faces, int imageWidth, int imageHeight)
        { 
            // Aspect ratio of the crop box: 800x920
            const double aspectRatio = 800.0 / 920.0;

            // Calculate the bounding rectangle for primary faces
            int minX = faces.Min(face => face.Left);
            int maxX = faces.Max(face => face.Left + face.Width);
            int minY = faces.Min(face => face.Top);
            int maxY = faces.Max(face => face.Top + face.Height);

            // Add padding around the faces (25% of the width and height of the bounding box)
            int faceBoxWidth = maxX - minX;
            int faceBoxHeight = maxY - minY;
            int paddingX = (int)(faceBoxWidth * 0.25); // 25% horizontal padding
            int paddingY = (int)(faceBoxHeight * 0.25); // 25% vertical padding

            // Expand the bounding box with padding
            minX = Math.Max(0, minX - paddingX);
            maxX = Math.Min(imageWidth, maxX + paddingX);
            minY = Math.Max(0, minY - paddingY);
            maxY = Math.Min(imageHeight, maxY + paddingY);

            // Adjust dimensions to maintain the aspect ratio
            int cropWidth = maxX - minX;
            int cropHeight = maxY - minY;

            if (cropWidth / (double)cropHeight < aspectRatio)
            {
                // Adjust width to match aspect ratio
                int adjustedWidth = (int)(cropHeight * aspectRatio);
                int widthDiff = adjustedWidth - cropWidth;
                minX = Math.Max(0, minX - widthDiff / 2);
                maxX = Math.Min(imageWidth, maxX + widthDiff / 2);
            }
            else
            {
                // Adjust height to match aspect ratio
                int adjustedHeight = (int)(cropWidth / aspectRatio);
                int heightDiff = adjustedHeight - cropHeight;
                minY = Math.Max(0, minY - heightDiff / 2);
                maxY = Math.Min(imageHeight, maxY + heightDiff / 2);
            }

            // Final crop dimensions
            cropWidth = maxX - minX;
            cropHeight = maxY - minY;

            // Vertically align faces at 1/3 of the crop box height from the top
            int faceCenterY = minY + (maxY - minY) / 2;
            int cropTop = Math.Max(0, faceCenterY - (int)(cropHeight * (1.0 / 3.0)));
            int cropBottom = cropTop + cropHeight;

            if (cropBottom > imageHeight)
            {
                cropTop = imageHeight - cropHeight;
                cropBottom = imageHeight;
            }

            return new Rectangle(minX, cropTop, cropWidth, cropHeight);
        }
    }
}
