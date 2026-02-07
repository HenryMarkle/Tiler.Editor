namespace Tiler.Editor;

using System;
using System.Linq;
using Raylib_cs;

public class LevelPalette
{
    public Color3[] ShadowLayers { get; init; } = new Color3[50];
    public Color3[] BaseLayers { get; init; } = new Color3[50];
    public Color3[] HighlightLayers { get; init; } = new Color3[50];

    public Color3[] SunlitShadowLayers { get; init; } = new Color3[50];
    public Color3[] SunlitBaseLayers { get; init; } = new Color3[50];
    public Color3[] SunlitHighlightLayers { get; init; } = new Color3[50];

    public Color3 Darkness = new(10, 10, 10);
    public Color3 Sky = new(200, 200, 200);
    public Color3 Fog = new(200, 200, 200);
    public float FogIntensity { get => field; set => field = Math.Clamp(value, 0, 1); }

    public Raylib_cs.Image ToImage()
    {
        var image = Raylib.GenImageColor(50, 8, Color.White);
        
        Raylib.ImageFormat(ref image, PixelFormat.UncompressedR8G8B8);
        
        for (var layer = 0; layer < 50; layer++)
        {
            Raylib.ImageDrawPixel(ref image, posX: layer, posY: 1, ShadowLayers[layer]);
            Raylib.ImageDrawPixel(ref image, posX: layer, posY: 2, BaseLayers[layer]);
            Raylib.ImageDrawPixel(ref image, posX: layer, posY: 3, HighlightLayers[layer]);

            Raylib.ImageDrawPixel(ref image, posX: layer, posY: 1 + 3, SunlitShadowLayers[layer]);
            Raylib.ImageDrawPixel(ref image, posX: layer, posY: 2 + 3, SunlitBaseLayers[layer]);
            Raylib.ImageDrawPixel(ref image, posX: layer, posY: 3 + 3, SunlitHighlightLayers[layer]);
        }
        
        Raylib.ImageDrawPixel(ref image, posX: 0, posY: 0, Darkness);
        Raylib.ImageDrawPixel(ref image, posX: 1, posY: 0, Sky);
        Raylib.ImageDrawPixel(ref image, posX: 2, posY: 0, Fog);
        Raylib.ImageDrawPixel(ref image, posX: 3, posY: 0, new Color((int)(FogIntensity * 256), 0, 0, 255));

        return image;
    }

    public static LevelPalette FromImage(Raylib_cs.Image image)
    {
        if ( image is not
            {
                Width: 50, 
                Height: 8
            }) throw new ArgumentException("Incorrect image size");

        return new LevelPalette
        {
            ShadowLayers = Enumerable
                .Range(0, 50)
                .Select(layer => new Color3(Raylib.GetImageColor(image, x: layer, y: 1)))
                .ToArray(),
            
            BaseLayers = Enumerable
                .Range(0, 50)
                .Select(layer => new Color3(Raylib.GetImageColor(image, x: layer, y: 2)))
                .ToArray(),
            
            HighlightLayers = Enumerable
                .Range(0, 50)
                .Select(layer => new Color3(Raylib.GetImageColor(image, x: layer, y: 3)))
                .ToArray(),

            SunlitShadowLayers = Enumerable
                .Range(0, 50)
                .Select(layer => new Color3(Raylib.GetImageColor(image, x: layer, y: 4)))
                .ToArray(),
            
            SunlitBaseLayers = Enumerable
                .Range(0, 50)
                .Select(layer => new Color3(Raylib.GetImageColor(image, x: layer, y: 5)))
                .ToArray(),
            
            SunlitHighlightLayers = Enumerable
                .Range(0, 50)
                .Select(layer => new Color3(Raylib.GetImageColor(image, x: layer, y: 6)))
                .ToArray(),
            
            Darkness = Raylib.GetImageColor(image, x: 0, y: 0),
            Sky = Raylib.GetImageColor(image, x: 1, y: 0),
            Fog = Raylib.GetImageColor(image, x: 2, y: 0),
            FogIntensity = Raylib.GetImageColor(image, x: 3, y: 0).R / 256f
        };
    }
}