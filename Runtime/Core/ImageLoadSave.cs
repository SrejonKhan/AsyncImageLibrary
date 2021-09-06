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

        internal void Load(AsyncImage asyncImage)
        {
            // Initialize Unity Main Thread for thread sensitive call
            UnityMainThread.Init();
            // Initialize Queuer if queueing is required
            if (asyncImage.ShouldQueueTextureProcess) MainThreadQueuer.Init();
            // Setup Threadpool for current environment
            ThreadPool.SetMaxThreads(12, 12);
            // Load Bitmap
            if (!string.IsNullOrEmpty(asyncImage.Path))
            {
                ThreadPool.QueueUserWorkItem(cb => LoadBitmapFromFileStream(asyncImage));
            }
            else if (asyncImage.Buffer != null)
            {
                ThreadPool.QueueUserWorkItem(cb => LoadBitmapFromBuffer(asyncImage));
            }
        }

        void LoadBitmapFromBuffer(AsyncImage asyncImage)
        {
            SKBitmap bitmap = SKBitmap.Decode(asyncImage.Buffer);
            // Process Bitmap
            ProcessBitmap(asyncImage, bitmap);
        }

        void LoadBitmapFromFileStream(AsyncImage asyncImage)
        {
            var input = File.OpenRead(asyncImage.Path);
            var inputStream = new SKManagedStream(input);
            var encodedOrigin = ReadOrigin(asyncImage.Path);

            // Decode Bitmap
            SKBitmap bitmap = SKBitmap.Decode(inputStream);
            bitmap = ChangeOrientation(bitmap, encodedOrigin);

            // Process Bitmap
            ProcessBitmap(asyncImage, bitmap); 

            input.Dispose();
            inputStream.Dispose();
        }

        void ProcessBitmap(AsyncImage asyncImage, SKBitmap bitmap)
        {
            asyncImage.Bitmap = bitmap;

            // Execute queued process
            asyncImage.isExecutingQueuedProcess = true;
            try
            {
                asyncImage.queuedProcess?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.Log(ex.Message);
            }
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

            if (asyncImage.ShouldGenerateTexture)
            {
                Action generateTexture = () =>
                    new ImageProcess().GenerateTexture(asyncImage, asyncImage.OnTextureLoad);

                if (asyncImage.ShouldQueueTextureProcess)
                    MainThreadQueuer.Queue(generateTexture);
                else
                    UnityMainThread.Execute(generateTexture);
            }

            if(asyncImage.OnLoad != null) 
                UnityMainThread.Execute(asyncImage.OnLoad);


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

        internal (SKImageInfo, SKEncodedImageFormat) GetImageInfo(AsyncImage asyncImage)
        {
            if (asyncImage.Bitmap != null)
            {
                IntPtr addr = asyncImage.Bitmap.GetPixels(out IntPtr length);

                using (SKData skData = SKData.Create(addr, length.ToInt32()))
                {
                    using (var codec = SKCodec.Create(skData))
                    {
                        return (codec.Info, codec.EncodedFormat);
                    }
                }
            }
            else if (!string.IsNullOrEmpty(asyncImage.Path))
            {
                using (var codec = SKCodec.Create(asyncImage.Path))
                {
                    return (codec.Info, codec.EncodedFormat);
                }
            }

            return (default (SKImageInfo), default(SKEncodedImageFormat)); 
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

        internal void TrySave(AsyncImage asyncImage, string path, SKEncodedImageFormat format, int quality, Action<bool> onComplete)
        {
            // TODO - Flip Bitmap before save
            try
            {
                using (MemoryStream memStream = new MemoryStream())
                {
                    using (SKManagedWStream wstream = new SKManagedWStream(memStream))
                    {
                        asyncImage.Bitmap.Encode(wstream, format, quality);
                        byte[] data = memStream.ToArray();

                        // save file
                        File.WriteAllBytes(path, data);
                    }
                }
                onComplete?.Invoke(true);
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.Message);
                onComplete?.Invoke(false);
            }
        }
    }
}