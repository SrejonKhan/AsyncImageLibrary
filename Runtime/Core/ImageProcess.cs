using System;
using UnityEngine;
using SkiaSharp;

namespace AsyncImageLibrary
{
    internal class ImageProcess
    {
        internal SKSamplingOptions GetSamplingOptions(ResizeQuality quality)
        {
            // Old SKFilterQuality values were: None, Low, Medium, High
            switch (quality)
            {
                case ResizeQuality.None:
                    return new SKSamplingOptions(SKFilterMode.Nearest, SKMipmapMode.None);
                case ResizeQuality.Low:
                    return new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.None);
                case ResizeQuality.Medium:
                    return new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear);
                case ResizeQuality.High:
                    // High quality used to imply bicubic-like resampling
                    return new SKSamplingOptions(SKCubicResampler.Mitchell);
                default:
                    return SKSamplingOptions.Default;
            }
        }
        
        public void Resize(AsyncImage asyncImage, int divideBy, ResizeQuality quality, Action onComplete)
        {
            if (divideBy <= 0)
                throw new ArgumentException("DivideBy should not be less than Zero (0) or equal to Zero (0).");

            // If image isn't loaded yet, we will queue it for later
            if (asyncImage.Bitmap == null)
            {
                asyncImage.queuedProcess += () => Resize(asyncImage, divideBy, quality, onComplete);
                return;
            }
            var resizeInfo = new SKImageInfo(asyncImage.Bitmap.Width / divideBy, asyncImage.Bitmap.Height / divideBy);
            var samplingOption = GetSamplingOptions(quality);
            asyncImage.Bitmap = asyncImage.Bitmap.Resize(resizeInfo, samplingOption);

            // callback
            if (onComplete != null) UnityMainThread.Execute(onComplete);
        }

        internal void Resize(AsyncImage asyncImage, Vector2 targetDimensions, ResizeQuality quality, Action onComplete)
        {
            if (targetDimensions.x == 0 || targetDimensions.y == 0)
                throw new ArgumentException("Target Dimensions should not be equal to Zero (0).");

            // If image isn't loaded yet, we will queue it for later
            if (asyncImage.Bitmap == null)
            {
                asyncImage.queuedProcess += () => Resize(asyncImage, targetDimensions, quality, onComplete);
                return;
            }
            var resizeInfo = new SKImageInfo((int)targetDimensions.x, (int)targetDimensions.y);
            var samplingOption = GetSamplingOptions(quality);
            asyncImage.Bitmap = asyncImage.Bitmap.Resize(resizeInfo, samplingOption);

            // callback
            if (onComplete != null) UnityMainThread.Execute(onComplete);
        }

        internal void DrawText(AsyncImage asyncImage, string text, Vector2 position, SKPaint paint, SKFont font, SKTextAlign textAlign, string fontFamilyName, Action onComplete)
        {
            if (asyncImage.Bitmap == null)
            {
                asyncImage.queuedProcess += () => DrawText(asyncImage, text, position, paint, font, textAlign, fontFamilyName, onComplete);
                return;
            }
            SKCanvas canvas = new SKCanvas(asyncImage.Bitmap);

            if (font.Typeface == null)
            {
                font.Typeface = SKTypeface.FromFamilyName(fontFamilyName);
            }

            canvas.DrawText(text, position.x, position.y, textAlign, font, paint);

            onComplete?.Invoke();

            canvas.Dispose();
        }

        internal void GenerateTexture(AsyncImage asyncImage, Action onComplete)
        {
            TextureFormat textureFormat = asyncImage.Bitmap.Info.ColorType == SKColorType.Rgba8888 ?
                TextureFormat.RGBA32 : TextureFormat.BGRA32;
            Texture2D texture = new Texture2D(asyncImage.Bitmap.Width, asyncImage.Bitmap.Height, textureFormat, false);
            texture.LoadRawTextureData(asyncImage.Bitmap.GetPixels(), asyncImage.Bitmap.RowBytes * asyncImage.Bitmap.Height);
            texture.Apply(false, !asyncImage.ShouldTextureBeReadable);
            asyncImage.Texture = texture;
            onComplete?.Invoke();
        }

        internal void Crop(AsyncImage asyncImage, Vector2 position, Vector2 targetDimension)
        {
            SKRectI cropRect = SKRectI.Create((int)position.x, (int)position.y, (int)targetDimension.x, (int)targetDimension.y);
            SKBitmap newBitmap = new SKBitmap(cropRect.Width, cropRect.Height);
            asyncImage.Bitmap.ExtractSubset(newBitmap, cropRect);
            asyncImage.Bitmap = newBitmap;

        }
    }
}