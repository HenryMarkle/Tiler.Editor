using System;
using System.IO;
using static System.IO.Path;

namespace Tiler.Editor;

public class AppFiles(AppDirectories appDirectories)
{
    public string ImGui { get; }            = Combine(appDirectories.Executable, "imgui.ini");
    public string Config { get; }           = Combine(appDirectories.Executable, "config.ini");
    public string Keybinds { get; }         = Combine(appDirectories.Executable, "keybinds.ini");
    public string DefaultFont { get; }      = Combine(appDirectories.Fonts, "default.ttf");
    public string CameraSprite { get; }     = Combine(appDirectories.Textures, "camera_sprite.png");
    public string ConnectionsAtlas { get; } = Combine(appDirectories.Textures, "geometry", "connections.png");
}

public class AppDirectories
{
    /// <summary>
    /// The directory of the currently running executable (duh).
    /// </summary>
    public string Executable { get; }
    
    /// <summary>
    /// The directory containing program log file(s).
    /// </summary>
    public string Logs { get; }
    
    /// <summary>
    /// The directory containing editable projects.
    /// </summary>
    public string Projects { get; }
    
    /// <summary>
    /// The directory containing rendered projects and ready for game usage.
    /// </summary>
    public string Levels { get; }
    
    /// <summary>
    /// The directory containing persistent, immutable resources required for proper functionality.
    /// </summary>
    public string Assets { get; }
    
    /// <summary>
    /// The directory containing user-extensible, sharable resources for the editor.
    /// </summary>
    public string Resources { get; }
    
    /// <summary>
    /// The directory containing tile resources.
    /// </summary>
    public string Tiles { get; }
    
    /// <summary>
    /// The directory containing prop resources.
    /// </summary>
    public string Props { get; }
    
    /// <summary>
    /// The directory containing effect resources.
    /// </summary>
    public string Effects { get; }
    
    /// <summary>
    /// The directory containing script libraries that can be included by other resource scripts.
    /// </summary>
    public string Scripts { get; }
    
    /// <summary>
    /// The directory containing light bruhes.
    /// </summary>
    public string Light { get; }
    
    /// <summary>
    /// The directory containing palettes.
    /// </summary>
    public string Palettes { get; }
    
    /// <summary>
    /// The directory containing sprite assets.
    /// </summary>
    public string Textures { get; }
    
    /// <summary>
    /// The directory for font assets.
    /// </summary>
    public string Fonts { get; }
    
    /// <summary>
    /// The directory containing shader assets.
    /// </summary>
    public string Shaders { get; }

    public AppFiles Files { get; }

    public AppDirectories()
    {
        var currentPath = AppDomain.CurrentDomain.BaseDirectory;
        
        // This is done so we don't have to repeatedly copy resources and assets to the binary's folder. 

        #if DEBUG
        Executable = GetFullPath(Combine(currentPath, "..", "..", "..", ".."));
        #else
        Executable = currentPath;
        #endif

        Logs      = Combine(Executable, "logs");
        Projects  = Combine(Executable, "projects");
        Levels    = Combine(Executable, "levels");
        Assets    = Combine(Executable, "assets");
        Resources = Combine(Executable, "resources");

        Tiles    = Combine(Resources, "tiles");
        Props    = Combine(Resources, "props");
        Effects  = Combine(Resources, "effects");
        Scripts  = Combine(Resources, "scripts");
        Light    = Combine(Resources, "light");
        Palettes = Combine(Resources, "palettes");

        Textures = Combine(Assets, "textures");
        Fonts    = Combine(Assets, "fonts");
        Shaders  = Combine(Assets, "shaders");

        Files = new AppFiles(appDirectories: this);
    }

    public override string ToString()
    {
        return "App Directories:"
               + $"\n├{"/logs", -15}: {DoesExist(Logs), -7}"
               + $"\n├{"/projects", -15}: {DoesExist(Projects), -7}"
               + $"\n├{"/levels", -15}: {DoesExist(Levels), -7}"
               + $"\n├{"/assets", -15}: {DoesExist(Assets), -7}"
               + $"\n│{"├────/textures", -15}: {DoesExist(Resources), -7}"
               + $"\n│{"├────/fonts", -15}: {DoesExist(Fonts), -7}"
               + $"\n│{"└────/shaders", -15}: {DoesExist(Shaders), -7}"
               + $"\n└{"/resources", -15}: {DoesExist(Resources), -7}"
               + $"\n {"├────/tiles", -15}: {DoesExist(Tiles), -7}"
               + $"\n {"├────/props", -15}: {DoesExist(Props), -7}"
               + $"\n {"├────/effects", -15}: {DoesExist(Effects), -7}"
               + $"\n {"├────/scripts", -15}: {DoesExist(Scripts), -7}"
               + $"\n {"├────/light", -15}: {DoesExist(Light), -7}"
               + $"\n {"└────/palettes", -15}: {DoesExist(Palettes), -7}";

        string DoesExist(string path) => Directory.Exists(path) ? "OK" : "MISSING";
    }
}
