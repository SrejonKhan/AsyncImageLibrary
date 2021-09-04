using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;

namespace AsyncImageLibrary
{
    internal class ImageLoadSave
    {
        public ImageLoadSave() { }

        internal void Load(AsyncImage asyncImage, Action onComplete)
        {
            if (string.IsNullOrEmpty(asyncImage.Path))
                throw new ArgumentNullException("Image Path should not be null or empty.");

            // Initialize Unity Main Thread for thread sensitive call
            UnityMainThread.Init();
            // Initialize Queuer if queueing is required
            if (asyncImage.shouldQueueTextureProcess) MainThreadQueuer.Init();
            // Setup Threadpool for current environment
            ThreadPool.SetMaxThreads(12, 12);
            ThreadPool.QueueUserWorkItem(cb => LoadImageFromFile(asyncImage, onComplete));
        }

        void LoadImageFromFile(AsyncImage asyncImage, Action onComplete)
        {
            var input = File.OpenRead(asyncImage.Path);
            var inputStream = new SKManagedStream(input);
            var encodedOrigin = ReadOrigin(asyncImage.Path);

            // Decode Bitmap
            SKBitmap bitmap = SKBitmap.Decode(inputStream);
            bitmap = ChangeOrientation(bitmap, encodedOrigin);

            asyncImage.Bitmap = bitmap;

            // Execute queued process
            asyncImage.isExecutingQueuedProcess = true;
            asyncImage.queuedProcess?.Invoke();
            asyncImage.queuedProcess = null;
            asyncImage.isExecutingQueuedProcess = false;

            // Flip image
            SKBitmap flippedBitmap = new SKBitmap(asyncImage.Bitmap.Width, asyncImage.Bitmap.Height);

            using (SKCanvas c = new SKCanvas(flippedBitmap))
            {
                c.Clear();
                c.Scale(1, -1, 0, asyncImage.Bitmap.Height / 2);
                c.DrawBitmap(asyncImage.Bitmap, new SKPoint());
            }
            asyncImage.Bitmap = flippedBitmap;
            asyncImage.Width = bitmap.Width;
            asyncImage.Height = bitmap.Height;

            // callback
            Action callback = () => UnityMainThread.Execute(onComplete);

            if (asyncImage.shouldGenerateTexture && callback != null)
            {
                Action generateTexture = () =>
                    new ImageProcess().GenerateTexture(asyncImage, callback);

                if (asyncImage.shouldQueueTextureProcess)
                    MainThreadQueuer.Queue(generateTexture);
                else
                    UnityMainThread.Execute(generateTexture);
            }
            else
            {
                callback?.Invoke();
            }

            input.Dispose();
            inputStream.Dispose();
        }

        SKEncodedOrigin ReadOrigin(string path)
        {
            SKEncodedOrigin encodedOrigin;

            using (var codec = SKCodec.Create(path))
            {
                encodedOrigin = codec.EncodedOrigin;
                return encodedOrigin;
            }
        }

        internal (SKImageInfo, SKEncodedImageFormat) GetImageInfo(string path)
        {
            using (var codec = SKCodec.Create(path))
            {
                return (codec.Info, codec.EncodedFormat);
            }
        }

        SKBitmap ChangeOrientation(SKBitmap bitmap, SKEncodedOrigin orientation)
        {
            SKBitmap rotated;
            switch (orientation)
            {
                case SKEncodedOrigin.BottomRight:

                    using (var surface = new SKCanvas(bitmap))
                    {
                        surface.RotateDegrees(180, bitmap.Width / 2, bitmap.Height / 2);
                        surface.DrawBitmap(bitmap.Copy(), 0, 0);
                    }

                    return bitmap;

                case SKEncodedOrigin.RightTop:
                    rotated = new SKBitmap(bitmap.Height, bitmap.Width);

                    using (var surface = new SKCanvas(rotated))
                    {
                        surface.Translate(rotated.Width, 0);
                        surface.RotateDegrees(90);
                        surface.DrawBitmap(bitmap, 0, 0);
                    }

                    return rotated;

                case SKEncodedOrigin.LeftBottom:
                    rotated = new SKBitmap(bitmap.Height, bitmap.Width);

                    using (var surface = new SKCanvas(rotated))
                    {
                        surface.Translate(0, rotated.Height);
                        surface.RotateDegrees(270);
                        surface.DrawBitmap(bitmap, 0, 0);
                    }

                    return rotated;

                default:
                    return bitmap;
            }
        }

        internal bool TrySave(AsyncImage image, string path)
        {
            try
            {
                image.Save(path);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.Message);
                return false;
            }
        }
    }
}