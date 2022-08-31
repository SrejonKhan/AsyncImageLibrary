using System;
using UnityEngine;
using SkiaSharp;

namespace AsyncImageLibrary
{
    internal class ImageProcess
    {
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
            SKFilterQuality filterQuality = (SKFilterQuality)((int)quality);
            asyncImage.Bitmap = asyncImage.Bitmap.Resize(resizeInfo, filterQuality);

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
            SKFilterQuality filterQuality = (SKFilterQuality)((int)quality);
            asyncImage.Bitmap = asyncImage.Bitmap.Resize(resizeInfo, filterQuality);

            // callback
            if (onComplete != null) UnityMainThread.Execute(onComplete);
        }

        internal void DrawText(AsyncImage asyncImage, string text, Vector2 position, SKPaint paint, string fontFamilyName, Action onComplete)
        {
            // If image isn't loaded yet, we will queue it for later
            if (asyncImage.Bitmap == null)
            {
                asyncImage.queuedProcess += () => DrawText(asyncImage, text, position, paint, fontFamilyName, onComplete);
                return;
            }
            SKCanvas canvas = new SKCanvas(asyncImage.Bitmap);

            // Fallback font
            if (paint.Typeface == null)
            {
                paint.Typeface = SKTypeface.FromFamilyName(fontFamilyName);
            }
            // Draw Text
            canvas.DrawText(text, position.x, position.y, paint);

            onComplete?.Invoke();

            canvas.Dispose();
        }

        internal void GenerateTexture(AsyncImage asyncImage, Action onComplete)
        {
            TextureFormat textureFormat = asyncImage.Bitmap.Info.ColorType == SKColorType.Rgba8888 ?
                TextureFormat.RGBA32 : TextureFormat.BGRA32;
            Texture2D texture = new Texture2D(asyncImage.Bitmap.Width, asyncImage.Bitmap.Height, textureFormat, false);
            texture.LoadRawTextureData(asyncImage.Bitmap.GetPixels(), asyncImage.Bitmap.RowBytes * asyncImage.Bitmap.Height);
            texture.Apply(false, true);
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