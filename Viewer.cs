namespace Tiler.Editor;

using System.Linq;
using Tiler.Editor.Views;

public class Viewer
{
    private readonly Context context;

    public Start start;
    public Geos geos;
    public Tiles tiles;
    public Cameras cameras;
    public Light light;
    public Render render;

    public BaseView SelectedView;

    public void Select<T>() 
        where T : BaseView
    {
        var view = (BaseView?) GetType()
            .GetFields()
            .Single(v => v.FieldType == typeof(T))?
            .GetValue(this);

        if (view is null) return;

        SelectedView = view;
        SelectedView.OnViewSelected();
    }

    public Viewer(Context context)
    {
        this.context = context;

        start = new(context);
        geos = new(context);
        tiles = new(context);
        cameras = new(context);
        light = new(context);
        render = new(context);

        context.LevelSelected += start.OnLevelSelected;
        context.LevelSelected += geos.OnLevelSelected;
        context.LevelSelected += tiles.OnLevelSelected;
        context.LevelSelected += cameras.OnLevelSelected;
        context.LevelSelected += light.OnLevelSelected;
        context.LevelSelected += render.OnLevelSelected;

        SelectedView = start;
    } 

    ~Viewer()
    {
        context.LevelSelected -= start.OnLevelSelected;
        context.LevelSelected -= geos.OnLevelSelected;
        context.LevelSelected -= tiles.OnLevelSelected;
        context.LevelSelected -= cameras.OnLevelSelected;
        context.LevelSelected -= light.OnLevelSelected;
        context.LevelSelected -= render.OnLevelSelected;
    }
}