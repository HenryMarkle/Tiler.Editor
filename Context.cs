using System.Collections.Generic;
using System.Collections.Immutable;
using System.Numerics;
using Raylib_cs;

namespace Tiler.Editor;

public class Context
{
    private readonly List<Level> levels = [];
    public Level? SelectedLevel { get; set; }

    public delegate void LevelSelectedEventHandler(Level level);
    public event LevelSelectedEventHandler? LevelSelected;

    public ImmutableList<Level> Levels => [ ..levels ];
    public void AddLevel(Level l) => levels.Add(l);
    public void RemoveLevel(Level l)
    {
        var index = levels.IndexOf(l);
        levels.Remove(l);

        if (levels is { Count: 0 }) SelectedLevel = null;
        else if (index >= levels.Count) SelectedLevel = levels[^1];
        else SelectedLevel = levels[index];

        if (SelectedLevel is not null) LevelSelected?.Invoke(SelectedLevel);
    }
    public void SelectLevel(int index)
    {
        if (index < 0 || index >= levels.Count) return;

        var level = levels[index];

        SelectedLevel = level;

        Viewports.Resize(level.Width * 20, level.Height * 20);

        LevelSelected?.Invoke(level);
    }
    public void SelectLevel(Level level)
    {
        if (!levels.Contains(level)) return;

        Viewports.Resize(level.Width * 20, level.Height * 20);

        SelectedLevel = level;
        LevelSelected?.Invoke(level);
    }

    //

    public required Viewports Viewports { get; init; }
    public required AppDirectories Dirs { get; init; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public Viewer Viewer { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

    public TileDex Tiles { get; set; } = new();

    public Camera2D Camera = new() { Zoom = 1, Target = Vector2.Zero };
    public int Layer { get; set; } = 0;
    public required AppConfiguration Config { get; set; }
    public required DebugPrinter DebugPrinter { get; set; }
}