using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Image=SixLabors.ImageSharp.Image;

namespace tuffCards.Services;

public static class BleedService {
	public static void AddBleed(string path, int bleedPx) {
		using var image = Image.Load<Rgba32>(path);

		var newWidth = image.Width + 2 * bleedPx;
		var newHeight = image.Height + 2 * bleedPx;
		using var extendedImage = new Image<Rgba32>(newWidth, newHeight);

		// ReSharper disable once AccessToDisposedClosure
		extendedImage.Mutate(ctx => ctx.DrawImage(image, new Point(bleedPx, bleedPx), 1));

		extendedImage.ProcessPixelRows(pixels => {
			var topRow = pixels.GetRowSpan(bleedPx)[bleedPx..^bleedPx];
			foreach (var row in Enumerable.Range(0, bleedPx))
				topRow.CopyTo(pixels.GetRowSpan(row)[bleedPx..^bleedPx]);
			var bottomRow = pixels.GetRowSpan(newHeight - bleedPx - 1)[bleedPx..^bleedPx];
			foreach (var row in Enumerable.Range(newHeight - bleedPx, bleedPx))
				bottomRow.CopyTo(pixels.GetRowSpan(row)[bleedPx..^bleedPx]);
		});
		extendedImage.Mutate(ctx => ctx.ProcessPixelRowsAsVector4(row => {
			row[..bleedPx].Fill(row[bleedPx]);
			row[^bleedPx..].Fill(row[^(bleedPx + 1)]);
		}));

		extendedImage.Save(path);
	}
}