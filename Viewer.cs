namespace Tiler.Editor;

using System.Linq;
using Tiler.Editor.Views;

public class Viewer
{
    private readonly Context context;

    public Start Start;
    public Create Create;
    public Geos Geos;
    public Tiles Tiles;
    public Connections Connections;
    public Cameras Cameras;
    public Light Light;
    public Effects Effects;
    public Props Props;
    public Render Render;
    public Palettes Palettes;

    public BaseView SelectedView;

    public void Select(BaseView view)
    {
        SelectedView = view;
        SelectedView.OnViewSelected();
    }

    public Viewer(Context context)
    {
        this.context = context;

        Start = new(context);
        Create = new(context);
        Geos = new(context);
        Tiles = new(context);
        Connections = new(context);
        Cameras = new(context);
        Light = new(context);
        Effects = new(context);
        Props = new(context);
        Render = new(context);
        Palettes = new(context);

        context.LevelSelected += Start.OnLevelSelected;
        context.LevelSelected += Create.OnLevelSelected;
        context.LevelSelected += Geos.OnLevelSelected;
        context.LevelSelected += Tiles.OnLevelSelected;
        context.LevelSelected += Connections.OnLevelSelected;
        context.LevelSelected += Cameras.OnLevelSelected;
        context.LevelSelected += Light.OnLevelSelected;
        context.LevelSelected += Effects.OnLevelSelected;
        context.LevelSelected += Props.OnLevelSelected;
        context.LevelSelected += Render.OnLevelSelected;
        context.LevelSelected += Palettes.OnLevelSelected;

        SelectedView = Start;
    } 

    ~Viewer()
    {
        context.LevelSelected -= Start.OnLevelSelected;
        context.LevelSelected -= Create.OnLevelSelected;
        context.LevelSelected -= Geos.OnLevelSelected;
        context.LevelSelected -= Tiles.OnLevelSelected;
        context.LevelSelected -= Connections.OnLevelSelected;
        context.LevelSelected -= Cameras.OnLevelSelected;
        context.LevelSelected -= Light.OnLevelSelected;
        context.LevelSelected -= Effects.OnLevelSelected;
        context.LevelSelected -= Props.OnLevelSelected;
        context.LevelSelected -= Render.OnLevelSelected;
        context.LevelSelected -= Palettes.OnLevelSelected;
    }
}