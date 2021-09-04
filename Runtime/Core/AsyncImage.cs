using SkiaSharp;
using System;
using System.Collections;
using System.Threading;
using UnityEngine;

namespace AsyncImageLibrary
{
    public class AsyncImage
    {
        private int width;
        public int Width { get => width; internal set => width = value; }

        private int height;
        public int Height { get => height; internal set => height = value; }

        private string path;
        internal string Path { get => path; set => path = value; }

        private SKBitmap bitmap;
        public SKBitmap Bitmap { get => bitmap; internal set => bitmap = value; }

        internal bool shouldGenerateTexture = true;
        internal bool shouldQueueTextureProcess = false;

        private Texture2D texture;
        public Texture2D Texture { get => texture; internal set => texture = value; }

        internal Action queuedProcess;
        internal bool isExecutingQueuedProcess = false;

        public AsyncImage() { }

        public AsyncImage(string path)
        {
            this.path = path;
        }

        public AsyncImage(string path, bool shouldGenerateTexture, bool shouldQueueTextureProcess = false)
        {
            this.path = path;
            this.shouldGenerateTexture = shouldGenerateTexture;
            this.shouldQueueTextureProcess = shouldQueueTextureProcess;
        }

        public (SKImageInfo, SKEncodedImageFormat) GetInfo()
        {
            return new ImageLoadSave().GetImageInfo(path);
        }

        public void Load(Action cb)
        {
            new ImageLoadSave().Load(this, cb);
        }

        public void GenerateTexture()
        {
            new ImageProcess().GenerateTexture(this, null);
        }

        public void Resize(int divideBy, ResizeQuality quality)
        {
            ThreadPool.QueueUserWorkItem(cb =>
                new ImageProcess().Resize(this, divideBy, quality)
            );
        }

        public void Resize(Vector2 targetDimensions, ResizeQuality quality)
        {
            ThreadPool.QueueUserWorkItem(cb =>
                new ImageProcess().Resize(this, targetDimensions, quality)
            );
        }

        public void DrawText(string text, Vector2 position, SKPaint paint, string fontFamilyName = "Arial", Action onComplete = null)
        {
            new ImageProcess().DrawText(this, text, position, paint, fontFamilyName, onComplete);
        }

        public void DrawText(string text, Vector2 position, TextAlign textAlign, Color color, float textSize = 0, string fontFamilyName = "Arial", Action onComplete = null)
        {
            if (bitmap == null)
            {
                queuedProcess += () => DrawText(text, position, textAlign, color, textSize, fontFamilyName, onComplete);
                return;
            }

            Color.RGBToHSV(color, out float h, out float s, out float v);

            var paint = new SKPaint();
            paint.Color = SKColor.FromHsv(h * 360, s * 100, v * 100);
            paint.TextSize = textSize == 0 ? bitmap.Width * 0.05f : textSize;
            paint.TextAlign = (SKTextAlign)((int)textAlign);

            // execute in same thread where loading task is ongoing
            if (isExecutingQueuedProcess)
            {
                new ImageProcess().DrawText(this, text, position, paint, fontFamilyName, onComplete);
            }
            // Called from main thread
            else
            {
                ThreadPool.QueueUserWorkItem(cb =>
                    new ImageProcess().DrawText(this, text, position, paint, fontFamilyName, onComplete));
            }
        }

        public void OverlapImage()
        {
            // TODO - Overlap Image
        }

        public void Crop(Vector2 targetDimension)
        {
            if (bitmap == null)
            {
                queuedProcess += () => Crop(new Vector2());
                return;
            }
            int horizontal = bitmap.Width - (bitmap.Height / 50 * 50);
            //SKImage image = SKImage.FromBitmap(bitmap);
            SKRectI cropRect = SKRectI.Create(1250, 1250, 2500, 2500);
            //var subset = image.Subset(SKRectI.Create(0, 0, 50, 50));
            SKBitmap newBitmap = new SKBitmap(cropRect.Width, cropRect.Height);
            bitmap.ExtractSubset(newBitmap, cropRect);
            bitmap = newBitmap;
        }

        public bool Save(string savePath)
        {
            return new ImageLoadSave().TrySave(this, savePath);
        }
    }

    public enum ResizeQuality
    {
        None, Low, Medium, High
    }

    public enum TextAlign
    {
        Left, Center, Right
    }
}