namespace Tiler.Editor;

public class Palette
{
    public Color3[] ShadowLayers { get; init; } = new Color3[50];
    public Color3[] BaseLayers { get; init; } = new Color3[50];
    public Color3[] HighlightLayers { get; init; } = new Color3[50];

    public Color3[] SunlitShadowLayers { get; init; } = new Color3[50];
    public Color3[] SunlitBaseLayers { get; init; } = new Color3[50];
    public Color3[] SunlitHighlightLayers { get; init; } = new Color3[50];

    public Color3 Sky = new(200, 200, 200);
    public Color3 Fog = new(200, 200, 200);
    public float FogIntensity = 0.1f;
}