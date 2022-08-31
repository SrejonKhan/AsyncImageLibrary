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
        public int Width { get => bitmap != null ? bitmap.Width : width; internal set => width = value; }

        private int height;
        public int Height { get => bitmap != null ? bitmap.Width : width; internal set => height = value; }

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

        private Action<bool> onSave;
        public Action<bool> OnSave { get => onSave; set => onSave = value; }

        internal Action queuedProcess;
        internal bool isExecutingQueuedProcess = false;

        private bool constructedFromBuffer = false;

        /// <summary>
        /// Default Constructor. Every property sets to default value. Path or Buffer should be set before loading.
        /// </summary>
        public AsyncImage() { }

        /// <summary>
        /// Constructor for creating Bitmap from Local File Path.
        /// </summary>
        /// <param name="path">Path of Local File</param>
        public AsyncImage(string path)
        {
            this.path = path;
        }

        /// <summary>
        /// Constructor for creating Bitmap from Local File Path.     
        /// </summary>
        /// <param name="path">Local File Path</param>
        /// <param name="shouldGenerateTexture">Generate Texture2D when Bitmap is loaded.</param>
        /// <param name="shouldQueueTextureProcess">Queue Texture Generation Progress in Main Thread without executing.</param>
        public AsyncImage(string path, bool shouldGenerateTexture = true, bool shouldQueueTextureProcess = false)
        {
            this.path = path;
            this.shouldGenerateTexture = shouldGenerateTexture;
            this.shouldQueueTextureProcess = shouldQueueTextureProcess;
        }

        /// <summary>
        /// Constuctor for creating Bitmap from Buffer. Suitable for creating Bitmap from Remote Image Buffer.
        /// </summary>
        /// <param name="buffer">File Buffer</param>
        /// <param name="shouldGenerateTexture">Generate Texture2D when Bitmap is loaded.</param>
        /// <param name="shouldQueueTextureProcess">Queue Texture Generation Progress in Main Thread without executing.</param>
        public AsyncImage(byte[] buffer, bool shouldGenerateTexture = true, bool shouldQueueTextureProcess = false)
        {
            constructedFromBuffer = true;
            this.buffer = buffer;
            this.shouldGenerateTexture = shouldGenerateTexture;
            this.shouldQueueTextureProcess = shouldQueueTextureProcess;
        }

        /// <summary>
        /// Get Info of from loaded Bitmap. Works when Image is loaded.     
        /// </summary>
        /// <returns>SKImageInfo & SKEncodedImageFormat</returns>
        public (SKImageInfo?, SKEncodedImageFormat?) GetInfoFromBitmap()
        {
            if (bitmap == null)
                throw new NullReferenceException("Image has not loaded yet. Please load image by calling Load().");

            return new ImageLoadSave().GetImageInfoFromBitmap(this);
        }

        /// <summary>
        /// Get Info of Image without loading it. Applicable for only local file.     
        /// </summary>
        /// <returns>SKImageInfo & SKEncodedImageFormat</returns>
        public (SKImageInfo?, SKEncodedImageFormat?) GetInfoFromFile()
        {
            if (string.IsNullOrEmpty(path))
                throw new NullReferenceException("Could not get info for AsyncImage when Path is not present.");

            return new ImageLoadSave().GetImageInfoFromFile(this);
        }

        /// <summary>
        /// Load Bitmap from given path
        /// </summary>
        /// <param name="cb">Callback upon loaded. Overwrites OnLoad delegates.</param>
        public void Load(Action cb = null)
        {
            onLoad = cb != null ? cb : onLoad;
            new ImageLoadSave().Load(this);
        }

        /// <summary>
        /// Generate Texture2D from Bitmap. If Bitmap is not loaded, this will be queued for execute after Bitmaps load.
        /// </summary>
        /// <param name="cb">Callback upon loaded. Overwrites OnTextureLoad delegates.</param>
        public void GenerateTexture(Action cb = null)
        {
            if (bitmap == null)
                throw new NullReferenceException("Image has not loaded yet. Please load image by calling Load().");

            onTextureLoad = cb != null ? cb : onTextureLoad;
            new ImageProcess().GenerateTexture(this, onTextureLoad);
        }

        /// <summary>
        /// Resize Bitmap to (Actual Dimension / divideBy)
        /// </summary>
        /// <param name="divideBy">Integer to divide actual dimension.</param>
        /// <param name="quality">Resize Quality</param>
        /// <param name="onComplete">Callback upon Resize Completes.</param>
        public void Resize(int divideBy, ResizeQuality quality, Action onComplete = null)
        {
            ThreadPool.QueueUserWorkItem(cb =>
                new ImageProcess().Resize(this, divideBy, quality, onComplete)
            );
        }

        /// <summary>
        /// Resize Bitmap to Target Dimensions.
        /// </summary>
        /// <param name="targetDimensions">X Axis is Width, Y Axis is Height</param>
        /// <param name="quality">Resize Quality</param>
        /// <param name="onComplete">Callback upon Resize Completes.</param>
        public void Resize(Vector2 targetDimensions, ResizeQuality quality, Action onComplete = null)
        {
            ThreadPool.QueueUserWorkItem(cb =>
                new ImageProcess().Resize(this, targetDimensions, quality, onComplete) 
            );
        }

        /// <summary>
        /// Draw Text on Bitmap
        /// </summary>
        /// <param name="text">Text to Draw</param>
        /// <param name="position">Position in Bitmap</param>
        /// <param name="paint">SKPaint for Styling</param>
        /// <param name="onComplete">Callback upon DrawText completes.</param>
        public void DrawText(string text, Vector2 position, SKPaint paint, Action onComplete = null)
        {
            if(paint == null)
            {
                throw new ArgumentNullException("SKPaint can not be null.");
            }
            new ImageProcess().DrawText(this, text, position, paint, "Arial", onComplete);
        }

        /// <summary>
        /// Draw Text on Bitmap
        /// </summary>
        /// <param name="text">Text to Draw</param>
        /// <param name="position">Position in Bitmap</param>
        /// <param name="textAlign">TextAlign of Text</param>
        /// <param name="color">Text Color</param>
        /// <param name="textSize">Text Size in Pixel Format.</param>
        /// <param name="fontFamilyName">Font Family Name. Default is Arial.</param>
        /// <param name="onComplete">Callback upon DrawText completes.</param>
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
            throw new NotImplementedException();
        }

        /// <summary>
        /// Crop bitmap
        /// </summary>
        /// <param name="position">From where to start rect</param>
        /// <param name="targetDimension">Rect size</param>
        public void Crop(Vector2 position, Vector2 targetDimension)
        {
            if (bitmap == null)
            {
                queuedProcess += () => Crop(position, targetDimension);
                return;
            }

            new ImageProcess().Crop(this, position, targetDimension);
        }

        /// <summary>
        /// Save Bitmap to Local File
        /// </summary>
        /// <param name="path">Save Path with Filename and Extension</param>
        /// <param name="format">Image Format</param>
        /// <param name="quality">Save Quality</param>
        /// <param name="onComplete">Callback upon save. Overwrites OnSave delegates.</param>
        public void Save(string path, SKEncodedImageFormat format, int quality, Action<bool> onComplete = null)
        {
            onSave = onComplete != null ? onComplete : onSave;

            ThreadPool.QueueUserWorkItem(cb => 
                new ImageLoadSave().TrySave(this, path, format, quality));
        }
    }
}