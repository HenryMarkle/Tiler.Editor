namespace Tiler.Editor.Views;

using Tiler.Editor;

public abstract class BaseView
{
    protected readonly Context Context;
    protected internal ViewKeybinds Keybinds;

    protected BaseView(Context context)
    {
        Context = context;
        Keybinds = new ViewKeybinds();
        
        Context.LevelSelected += OnLevelSelected;
    }

    ~BaseView()
    {
        Context.LevelSelected -= OnLevelSelected;
    }

    public virtual void Process() {}
    public virtual void Draw() {}
    public virtual void GUI() {}
    public virtual void Debug() {}

    public virtual void OnLevelSelected(Level level) {}
    public virtual void OnViewSelected() {}
}