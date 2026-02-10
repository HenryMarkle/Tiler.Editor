using System.Runtime.CompilerServices;

namespace Tiler.Editor;

using Tiler.Editor.Views;

public class Viewer
{
    public readonly Start Start;
    public readonly Create Create;
    public readonly Geos Geos;
    public readonly Tiles Tiles;
    public readonly Connections Connections;
    public readonly Cameras Cameras;
    public readonly Light Light;
    public readonly Effects Effects;
    public readonly Props Props;
    public readonly Render Render;
    public readonly Palettes Palettes;

    public BaseView SelectedView;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Select(BaseView view)
    {
        SelectedView = view;
        SelectedView.OnViewSelected();
    }

    public Viewer(Context context)
    {
        Start = new Start(context);
        Create = new Create(context);
        Geos = new Geos(context);
        Tiles = new Tiles(context);
        Connections = new Connections(context);
        Cameras = new Cameras(context);
        Light = new Light(context);
        Effects = new Effects(context);
        Props = new Props(context);
        Render = new Render(context);
        Palettes = new Palettes(context);
        
        // Load keybinds

        var parser = new IniParser.FileIniDataParser();
        var data = parser.ReadFile(context.Dirs.Files.Keybinds);
        
        // TODO: Find a way to dynamically do this

        if (data.Sections.ContainsSection("Geos"))
            Geos.Keybinds.FromIni(data["Geos"]);

        if (data.Sections.ContainsSection("Tiles"))
            Tiles.Keybinds.FromIni(data["Tiles"]);
        
        // TODO: Complete this

        SelectedView = Start;
    } 
}