using System;
using System.IO;
using static System.IO.Path;

namespace Tiler.Editor;

public class AppDirectories
{
    public string Executable { get; init; }
    public string Logs { get; init; }
    public string Projects { get; init; }
    public string Assets { get; init; }
    public string Resources { get; init; }
    public string Tiles { get; init; }
    public string Scripts { get; init; }
    public string Light { get; init; }
    public string Textures { get; init; }
    public string Fonts { get; init; }

    public class AppFiles
    {
        public required string ImGui { get; init; }
        public required string Config { get; init; }
        public required string DefaultFont { get; init; }
    }

    public AppFiles Files { get; init; }

    public AppDirectories()
    {
        var currentPath = AppDomain.CurrentDomain.BaseDirectory;

        #if DEBUG
        Executable = GetFullPath(Combine(currentPath, "..", "..", "..", ".."));
        #else
        Executable = currentPath;
        #endif

        Logs      = Combine(Executable, "logs");
        Projects  = Combine(Executable, "projects");
        Assets    = Combine(Executable, "assets");
        Resources = Combine(Executable, "resources");

        Tiles = Combine(Resources, "tiles");
        Scripts = Combine(Resources, "scripts");
        Light = Combine(Resources, "light");

        Textures = Combine(Assets, "textures");
        Fonts    = Combine(Assets, "fonts");

        Files = new AppFiles()
        {
            ImGui  = Combine(Executable, "imgui.ini"),
            Config = Combine(Executable, "config.ini"),
            DefaultFont = Combine(Fonts, "default.ttf")
        };
    }
}
