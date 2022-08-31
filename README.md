# Async Image Library

Load Image asynchronously from external environment without blocking Main-Thread. And powerful SkiaSharp Library implemented and wrapped for better and convenient usage.

- SkiaSharp Version - 2.80.3
- Unity Version - 2019.3.x or newer
- Build Support (Tested Only) -
  - [x] Windows (IL2CPP)
  - [x] Windows (Mono)
  - [x] Android (IL2CPP)
  - [x] Android (Mono)
  - [x] iOS (IL2CPP)
  - [ ] Mac
  - [ ] Linux Distribution
- [Sample Project](https://github.com/SrejonKhan/AsyncImageLibrary-Sample)

# Installation

Open Package Manager in Unity and Click on Plus Icon -> Add package from git URL, paste following link

```
https://github.com/SrejonKhan/AsyncImageLibrary.git
```

and click Add.

Other methods (Asset Store, UPM, Release Page) will be added later after a stable release.

# Manual

To use this library, `AsyncImageLibrary` namespace should be defined, like this `using AsyncImageLibrary;`

## AsyncImage (Class)

AsyncImage specifies an Image. It provides methods and properties for Loading, Saving and Processing an Image.

#### Constructor

| Name                                                                                  | Details                                                                                                                                               |
| ------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------- |
| AsyncImage()                                                                          | Default Constructor. Every property sets to default value. Path or Buffer should be set before loading.                                               |
| AsyncImage(string path)                                                               | For creating Bitmap from Local File Path.                                                                                                             |
| AsyncImage(string path, bool shouldGenerateTexture)                                   | For creating Bitmap from Local File Path and define if to Generate Texture upon Loading Bitmap.                                                       |
| AsyncImage(string path, bool shouldGenerateTexture, bool shouldQueueTextureProcess)   | For creating Bitmap from Local File Path and define if to Generate Texture when Bitmap is loaded and Queue Texture Generation Process in Main Thread. |
| AsyncImage(byte[] buffer)                                                             | For creating Bitmap from Buffer. Suitable for creating Bitmap from Remote Image Buffer.                                                               |
| AsyncImage(byte[] buffer, bool shouldGenerateTexture)                                 | For creating Bitmap from Buffer. Define if to generate Texture when Bitmap is loaded.                                                                 |
| AsyncImage(byte[] buffer, bool shouldGenerateTexture, bool shouldQueueTextureProcess) | For creating Bitmap from Buffer. Define if to generate Texture when Bitmap is loaded and queue Texture Generation Process in Main Thread.             |

#### Properties

| Name                      | Details                                                               |
| ------------------------- | --------------------------------------------------------------------- |
| Width\*                   | Width of Image                                                        |
| Height\*                  | Height of Image                                                       |
| Path                      | Local file path of Image                                              |
| Bitmap\*                  | SKBitmap reference                                                    |
| Texture\*\*               | Texture2D of Image                                                    |
| ShouldGenerateTexture     | Generate Texture along with Bitmap, if true.                          |
| ShouldQueueTextureProcess | Queue Texture Generation Process in MainThreadQueue to process later. |

\*Available when Bitmap is Loaded.

\*\*Available when Texture is generated.

#### Methods

| Name                                                                                                                                | Details                                                                                                                               |
| ----------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------- |
| Load()                                                                                                                              | Load Bitmap from given path.                                                                                                          |
| Load(Action cb)                                                                                                                     | Load Bitmap from given path and callback upon completion.                                                                             |
| GenerateTexture()                                                                                                                   | Generate Texture2D from Bitmap. If Bitmap is not loaded, **this will be queued for execute after Bitmaps load**.                      |
| GenerateTexture(Action cb)                                                                                                          | Generate Texture2D from Bitmap. Get queued when Bitmap is not loaded. Callback when texture is generated. (Same as `OnLoad` delegate) |
| Save(string path, SKEncodedImageFormat format, int quality)                                                                         | Save Image to specified format.                                                                                                       |
| Save(string path, SKEncodedImageFormat format, int quality, Action<bool> onComplete)                                                | Save Image to specified format.                                                                                                       |
| Crop(Vector2 position, Vector2 targetDimension)        | Crop Bitmap to specified size in specified rect position.    |
| Resize(int divideBy, ResizeQuality quality)                                                                                         | Resize to (Actual Dimension / divideBy).                                                                                              |
| Resize(int divideBy, ResizeQuality quality, Action onComplete)                                                                      | Resize to (Actual Dimension / divideBy). Callback when completed.                                                                     |
| Resize(Vector2 targetDimensions, ResizeQuality quality)                                                                             | Resize to given dimension.                                                                                                            |
| Resize(Vector2 targetDimensions, ResizeQuality quality, Action onComplete)                                                          | Resize to given dimension. Callback when completed.                                                                                   |
| DrawText(string text, Vector2 position, TextAlign textAlign, Color color, float textSize)                                           | Draw Text on Bitmap.                                                                                                                  |
| DrawText(string text, Vector2 position, TextAlign textAlign, Color color, float textSize, string fontFamilyName)                    | Draw Text on Bitmap.                                                                                                                  |
| DrawText(string text, Vector2 position, TextAlign textAlign, Color color, float textSize, string fontFamilyName, Action onComplete) | Draw Text on Bitmap.                                                                                                                  |
| DrawText(string text, Vector2 position, SKPaint paint)                                                                              | Draw Text on Bitmap, proving SKPaint for styling.                                                                                     |
| DrawText(string text, Vector2 position, SKPaint paint, Action onComplete)                                                           | Draw Text on Bitmap, proving SKPaint for styling. Callback upon completion.                                                           |

#### Delegates

| Name          | Details                            |
| ------------- | ---------------------------------- |
| OnLoad        | Callback when Bitmap is loaded.    |
| OnSave        | Callback when Bitmap is saved.     |
| OnTextureLoad | Callback when Texture2D is loaded. |

# Usage

## Loading Image from Local

```csharp
AsyncImage image = new AsyncImage(path);
image.Load();
```

## Loading Image from Remote

```csharp
string remoteImageUrl = "https://example.com/image.png";

AsyncImage image = new AsyncImage(remoteImageUrl);
image.Load();
```

## Loading Texture

To load texture, the Bitmap must be loaded. Calling `image.GenerateTexture()` will throw error if the Bitmap is not loaded. Also, calling `image.GenerateTexture()` just after `image.Load()` won't work most cases as it's an asynchronous call. So, it's better to load a texture like this -

```csharp
AsyncImage image = new AsyncImage(path);

image.OnTextureLoad += () =>
{
    Texture2D loadedTexture = image.Texture;
};

image.Load();
```

If AsyncImage is constructed by the following way -

```csharp
AsyncImage image = new AsyncImage(path, false);
```

or, defined not to generate texture -

```csharp
AsyncImage image = new AsyncImage(path);
image.ShouldGenerateTexture = false; // don't generate texture on Load
```

It won't **Generate Texture Automatically** after bitmap is loaded. So, in that circumstances, we have to generate Texture manually. we have to keep in mind that, we can only generate Texture when bitmap is loaded. Please check next section for how to deal with that.

## Generate `Texture2D` on demand

By default, `Texture2D` automatically generated on main thread after bitmap is loadeed. After it is generated, it can be accessible by `image.Texture`.

Whatever, in some scenerio, it's not necessery to generate `Texture2D` along with loading Bitmap. In that case, we can define **_not to load_** it by simply passing another argument in constructor -

```csharp
AsyncImage image = new AsyncImage(path, false);
```

For generating Texture2D -

```csharp
image.GenerateTexture(() =>
{
    Debug.Log("Texture2D loaded!");
});
```

Alternatively -

```csharp
image.OnTextureLoad += () =>
{
    Debug.Log("Texture2D loaded!");
};
// generate Texture2D
image.GenerateTexture();
```

## Saving Bitmap

For saving, Bitmap must be loaded. To save a Bitmap -

```csharp
AsyncImage image = new AsyncImage(path);

image.Save(@"E:\image.png", SkiaSharp.SKEncodedImageFormat.Png, 100);
```

## Listen to Events

There are 2 important events where you can subscribe.

### `OnLoad`

`OnLoad` will be called as soon as the Bitmap is loaded. It's ideal to use when you would like to know when Bitmap is loaded and do other works e.g calling `GenerateTexture()`.

```csharp
AsyncImage image = new AsyncImage(path);
image.OnLoad += () =>
{
   Debug.Log("Bitmap loaded!");
   // do your works
};
image.Load();
```

### `OnSave`

`OnSave` will be called as soon as the Bitmap is saved to local file. It returns bool in callback as result. `true = successfully saved`, `false = error encountered while saving`

```csharp
AsyncImage image = new AsyncImage(path);
image.OnSave += (result) =>
{
   // result is boolean
   Debug.Log("Saved - " + result);
};
image.Save(@"E:\image.png", SkiaSharp.SKEncodedImageFormat.Png, 100);
```

### `OnTextureLoad`

`OnTextureLoad` will be called when `Texture2D` is generated from the Bitmap. By default, whenever `Load()` is called, it generates `Texture2D` after Bitmap is loaded. It can be defined either to generate `Texture2D` afterward or call `GenerateTexture()` later on-demand.

```csharp
AsyncImage image = new AsyncImage(path);
image.OnTextureLoad += () =>
{
    Debug.Log("Texture2D loaded!");
    // do your works
};
image.Load();
```

## Load Image Info Only

To load Image info only -

```csharp
AsyncImage image = new AsyncImage(path);

var (info, format) = image.GetInfo();
```

Learn more about info [(SKImageInfo)](https://docs.microsoft.com/en-us/dotnet/api/skiasharp.skimageinfo?view=skiasharp-2.80.2) and format [(SKEncodedImageFormat)](https://docs.microsoft.com/en-us/dotnet/api/skiasharp.skencodedimageformat?view=skiasharp-2.80.2).

Note that, it's not possible to get Info from Image that is generated from Buffer.

# Image Process

## Resizing

There are two ways to resize an Image.

### Divide By

Let's say we have a image of 1000px _ 1000px dimension. To resize it down to half of it's resolution, 500px _ 500px, we need to divide it by 2 (1000px/2 = 500px). So, we can simply -

```csharp
AsyncImage image = new AsyncImage(path);

image.Resize(2, ResizeQuality.Medium); // resize

image.OnTextureLoad += MethodA;
image.Load();
```

### Target Dimensions

To resize an Image to a defined dimension -

```csharp
AsyncImage image = new AsyncImage(path);

// x of Vector2 is Width, y of Vector2 is Height
image.Resize(new Vector2(200,200), ResizeQuality.Medium);

image.OnTextureLoad += MethodA;
image.Load();
```

## Draw Text

### Simple

Simply draw a text by passing straight-forward parameters.

```csharp
public void DrawText(string text, Vector2 position, TextAlign textAlign, Color color, float textSize, string fontFamilyName = "Arial", Action onComplete = null)
```

| Parameters       | Details                                                    |
| ---------------- | ---------------------------------------------------------- |
| text             | Text to draw on Image                                      |
| position         | The x & y coordinate of the origin of the text being drawn |
| textAlign        | Text Align (Left, Center, Right)                           |
| color            | Color of Text (RGB)                                        |
| textSize         | Text Height in Pixel                                       |
| fontFamilyName\* | Font family name for typeface                              |
| onComplete\*     | Callback on completion                                     |

\*Optional Parameters.

#### Example

```csharp
AsyncImage image = new AsyncImage(path);

var (info, format) = image.GetInfo();

// drawing text at the center (vertically and horizontally)
image.DrawText("Hello from the other side!", new Vector2(info.Width/2, info.Height / 2), TextAlign.Center, Color.white);

image.OnTextureLoad += MethodA;
image.Load();
```

### Advanced (SKPaint)

Draw text with [SKPaint](https://docs.microsoft.com/en-us/dotnet/api/skiasharp.skpaint?view=skiasharp-2.80.2), more flexibility to use SkiaSharp directly. SKPaint gives a lot of Properties and Methods to apply any filters or whatsoever.

```csharp
public void DrawText(string text, Vector2 position, SKPaint paint, Action onComplete = null)
```

| Parameters   | Details                                                    |
| ------------ | ---------------------------------------------------------- |
| text         | Text to draw on Image                                      |
| position     | The x & y coordinate of the origin of the text being drawn |
| paint        | SKPaint object reference, for styling text.                |
| onComplete\* | Callback on completion                                     |

\*Optional Parameters.

#### Example

```csharp
AsyncImage image = new AsyncImage(path);

var (info, format) = image.GetInfo();

string text = "Leaves are on the ground, fall has come.";

var paint = new SKPaint();
// Convert RGB color to HSV color
Color.RGBToHSV(Color.white, out float h, out float s, out float v);
paint.Color = SKColor.FromHsv(h * 360, s * 100, v * 100);
// Text Align
paint.TextAlign = SKTextAlign.Center;
// Loading Font from file
paint.Typeface = SKTypeface.FromFile(@"E:\Font\Hack-Italic.ttf", 0);
// Adjust TextSize property so text is 90% of screen width
float textWidth = paint.MeasureText(text);
paint.TextSize = 0.9f * info.Width * paint.TextSize / textWidth;

// drawing text at the center (vertically and horizontally)
image.DrawText(text, new Vector2(info.Width / 2, info.Height/2), paint);

image.OnTextureLoad += MethodA;
image.Load();
```

**Note:** MeasureText() should be called after a certain typeface loads. Or, it will lead to miscalculation. **`Arial`** is the fallback font for `DrawText()`. In case of font was not loaded, it will try to load Arial.
