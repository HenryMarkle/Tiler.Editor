namespace Tiler.Editor.Views;

using Tiler.Editor;

public abstract class BaseView(Context context)
{
    protected Context Context = context;

    public virtual void Process() {}
    public virtual void Draw() {}
    public virtual void GUI() {}
    public virtual void Debug() {}

    public virtual void OnLevelSelected(Level level) {}
    public virtual void OnViewSelected() {}
}