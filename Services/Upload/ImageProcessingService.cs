using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace PsikologProje_Void.Services.Upload
{
    public class ImageProcessingService : IImageProcessingService
    {
        private readonly UploadPolicyOptions _options;

        public ImageProcessingService(IOptions<UploadPolicyOptions> options)
        {
            _options = options.Value;
        }

        public async Task<byte[]> NormalizeProfilePhotoAsync(IFormFile file, CancellationToken cancellationToken = default)
        {
            await using var input = file.OpenReadStream();
            using var image = await Image.LoadAsync(input, cancellationToken);

            var targetSize = _options.ProfilePhotoSizePx > 0 ? _options.ProfilePhotoSizePx : 512;
            var side = Math.Min(image.Width, image.Height);
            var cropX = (image.Width - side) / 2;
            var cropY = (image.Height - side) / 2;

            image.Mutate(ctx => ctx
                .Crop(new Rectangle(cropX, cropY, side, side))
                .Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Crop,
                    Size = new Size(targetSize, targetSize)
                }));

            await using var output = new MemoryStream();
            await image.SaveAsJpegAsync(output, new JpegEncoder
            {
                Quality = 85
            }, cancellationToken);

            return output.ToArray();
        }
    }
}
