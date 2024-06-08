using SkiaSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;

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
            ThreadPool.SetMaxThreads(Environment.ProcessorCount, Environment.ProcessorCount);
            // Load Bitmap
            if (!string.IsNullOrEmpty(asyncImage.Path))
            {
                // android / remote file
                if (asyncImage.Path.Contains("://") || asyncImage.Path.Contains(":///"))
                    StaticCoroutine.StartCoroutine(LoadBufferUwr(asyncImage));
                else
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
            // validate the file path
            if (asyncImage.IsPathValidated == null) 
                ValidatePath(asyncImage);
            if (!(bool)asyncImage.IsPathValidated)
                return;

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

        IEnumerator LoadBufferUwr(AsyncImage asyncImage)
        {
            // check if remote image exist by sending a HEAD req
            yield return DryUwrValidatePath(asyncImage);
            if (!(bool)asyncImage.IsPathValidated)
                yield break;

            using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(asyncImage.Path))
            {
                yield return uwr.SendWebRequest();

                if (uwr.result == UnityWebRequest.Result.ConnectionError || uwr.result == UnityWebRequest.Result.DataProcessingError)
                {
                    Debug.LogError(uwr.error);
                    yield break;
                }

                asyncImage.Buffer = uwr.downloadHandler.data;
                ThreadPool.QueueUserWorkItem(cb => LoadBitmapFromBuffer(asyncImage));
            }
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
            SKBitmap flippedBitmap = FlipBitmap(asyncImage.Bitmap);

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

            if (asyncImage.OnLoad != null)
                UnityMainThread.Execute(asyncImage.OnLoad);
        }

        SKBitmap FlipBitmap(SKBitmap bitmap)
        {
            SKBitmap flippedBitmap = new SKBitmap(bitmap.Width, bitmap.Height);

            using (SKCanvas c = new SKCanvas(flippedBitmap))
            {
                c.Clear();
                c.Scale(1, -1, 0, bitmap.Height / 2);
                c.DrawBitmap(bitmap, new SKPoint());
            }
            return flippedBitmap;
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

        internal (SKImageInfo?, SKEncodedImageFormat?) GetImageInfoFromBitmap(AsyncImage asyncImage)
        {
            if(asyncImage.Bitmap != null) 
            {
                SKImageInfo info = new SKImageInfo(asyncImage.Bitmap.Width, asyncImage.Bitmap.Height);
                return (info, null);
            }
            return (null, null);
        }   

        internal (SKImageInfo? info, SKEncodedImageFormat? encodedFormat) GetImageInfoFromFile(AsyncImage asyncImage)
        {
            // validate the file path
            if (asyncImage.IsPathValidated == null)
                ValidatePath(asyncImage);
            if (!(bool)asyncImage.IsPathValidated)
                return (null, null);

            using (var codec = SKCodec.Create(asyncImage.Path))
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

        internal void TrySave(AsyncImage asyncImage, string path, SKEncodedImageFormat format, int quality)
        {
            try
            {
                using (MemoryStream memStream = new MemoryStream())
                {
                    using (SKManagedWStream wstream = new SKManagedWStream(memStream))
                    {
                        SKBitmap flippedBitmap = FlipBitmap(asyncImage.Bitmap);

                        flippedBitmap.Encode(wstream, format, quality);
                        byte[] data = memStream.ToArray();

                        // save file
                        File.WriteAllBytes(path, data);
                    }
                }
                asyncImage.OnSave?.Invoke(true);
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.Message);
                asyncImage.OnSave?.Invoke(false);
            }
        }

        internal byte[] GetEncodedBuffer(AsyncImage asyncImage, SKEncodedImageFormat format, int quality)
        {
            byte[] buffer = null;
            try
            {
                using (MemoryStream memStream = new MemoryStream())
                    using (SKManagedWStream wstream = new SKManagedWStream(memStream))
                    {
                        SKBitmap flippedBitmap = FlipBitmap(asyncImage.Bitmap);

                        flippedBitmap.Encode(wstream, format, quality);
                        buffer = memStream.ToArray();
                    }
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.Message);
            }

            return buffer;
        }

        internal void ValidatePath(AsyncImage asyncImage)
        {
            if (string.IsNullOrEmpty(asyncImage.Path))
            {
                asyncImage.IsPathValidated = false;
                asyncImage.OnPathValidation.Invoke(false);
                return;
            }

            // android / remote file
            if (asyncImage.Path.Contains("://") || asyncImage.Path.Contains(":///"))
            {
                StaticCoroutine.StartCoroutine(DryUwrValidatePath(asyncImage));
                return;
            }

            if (!File.Exists(asyncImage.Path))
            {
                asyncImage.IsPathValidated = false;
                asyncImage.OnPathValidation.Invoke(false);
                return;
            }
            asyncImage.IsPathValidated = true;
            asyncImage.OnPathValidation.Invoke(true);
        }

        private IEnumerator DryUwrValidatePath(AsyncImage asyncImage)
        {
            // check if remote image exist by sending a HEAD req
            using (UnityWebRequest headReq = UnityWebRequest.Head(asyncImage.Path))
            {
                yield return headReq.SendWebRequest();

                if (!(headReq.responseCode >= 200 && headReq.responseCode < 300))
                {
                    asyncImage.IsPathValidated = false;
                    asyncImage.OnPathValidation?.Invoke(false);
                    yield break;
                }
            }
            asyncImage.IsPathValidated = true;
            asyncImage.OnPathValidation?.Invoke(true);
        }
    }
}