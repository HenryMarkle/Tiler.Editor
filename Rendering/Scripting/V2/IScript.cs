namespace Tiler.Editor.Rendering.Scripting.V2;

public interface IScript
{
    bool IsInitialized { get; }
    bool IsDone { get; }
    
    void Initialize(Viewports viewports);
    void Render();
}