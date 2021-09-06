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

        private byte[] buffer;
        public byte[] Buffer { get => buffer; internal set => buffer = value; }

        private SKBitmap bitmap;
        public SKBitmap Bitmap { get => bitmap; internal set => bitmap = value; }


        private bool shouldGenerateTexture = true;
        public bool ShouldGenerateTexture { get => shouldGenerateTexture; set => shouldGenerateTexture = value; }

        private bool shouldQueueTextureProcess = false;
        public bool ShouldQueueTextureProcess { get => shouldQueueTextureProcess; set => shouldQueueTextureProcess = value; }
                
        private Texture2D texture;
        public Texture2D Texture { get => texture; internal set => texture = value; }

        private Action onTextureLoad;
        public Action OnTextureLoad { get => onTextureLoad; set => onTextureLoad = value; }

        private Action onLoad;
        public Action OnLoad { get => onLoad; set => onLoad = value; }

        internal Action queuedProcess;
        internal bool isExecutingQueuedProcess = false;

        private bool constructedFromBuffer = false;

        public AsyncImage() { }

        public AsyncImage(string path)
        {
            this.path = path;
        }

        public AsyncImage(string path, bool shouldGenerateTexture = true, bool shouldQueueTextureProcess = false)
        {
            this.path = path;
            this.shouldGenerateTexture = shouldGenerateTexture;
            this.shouldQueueTextureProcess = shouldQueueTextureProcess;
        }

        public AsyncImage(byte[] buffer)
        {
            constructedFromBuffer = true;
            this.buffer = buffer;   
        }

        public (SKImageInfo, SKEncodedImageFormat) GetInfo()
        {
            if (constructedFromBuffer)
                throw new NullReferenceException("Could not get info for AsyncImage that is constructed by Buffer.");

            if(bitmap == null && string.IsNullOrEmpty(path))
                throw new NullReferenceException("Could not get info for AsyncImage when Bitmap or Path is not present.");

            return new ImageLoadSave().GetImageInfo(this);
        }

        public void Load(Action cb = null)
        {
            onLoad = cb != null ? cb : onLoad;
            new ImageLoadSave().Load(this);
        }

        public void GenerateTexture(Action cb = null)
        {
            if (bitmap == null)
                throw new NullReferenceException("Image has not loaded yet. Please load image by calling Load().");

            onTextureLoad = cb != null ? cb : onTextureLoad;
            new ImageProcess().GenerateTexture(this, onTextureLoad);
        }

        public void Resize(int divideBy, ResizeQuality quality, Action onComplete = null)
        {
            ThreadPool.QueueUserWorkItem(cb =>
                new ImageProcess().Resize(this, divideBy, quality, onComplete)
            );
        }

        public void Resize(Vector2 targetDimensions, ResizeQuality quality, Action onComplete = null)
        {
            ThreadPool.QueueUserWorkItem(cb =>
                new ImageProcess().Resize(this, targetDimensions, quality, onComplete) 
            );
        }

        public void DrawText(string text, Vector2 position, SKPaint paint, Action onComplete = null)
        {
            if(paint == null)
            {
                throw new ArgumentNullException("SKPaint can not be null.");
            }
            new ImageProcess().DrawText(this, text, position, paint, "Arial", onComplete);
        }

        public void DrawText(string text, Vector2 position, TextAlign textAlign, Color color, float textSize, string fontFamilyName = "Arial", Action onComplete = null)
        {
            if (bitmap == null)
            {
                queuedProcess += () => DrawText(text, position, textAlign, color, textSize, fontFamilyName, onComplete);
                return;
            }

            Color.RGBToHSV(color, out float h, out float s, out float v);

            var paint = new SKPaint();
            paint.Color = SKColor.FromHsv(h * 360, s * 100, v * 100);
            paint.TextSize = textSize;
            paint.TextAlign = (SKTextAlign)((int)textAlign);
            paint.Typeface = SKTypeface.FromFamilyName(fontFamilyName);

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
            throw new NotImplementedException();
            //if (bitmap == null)
            //{
            //    queuedProcess += () => Crop(new Vector2());
            //    return;
            //}
        }

        public void Save(string path, SKEncodedImageFormat format, int quality, Action<bool> onComplete = null)
        {
            new ImageLoadSave().TrySave(this, path, format, quality, onComplete);
        }
    }
}