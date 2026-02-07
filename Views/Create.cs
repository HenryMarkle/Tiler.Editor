namespace Tiler.Editor.Views;

using System.Numerics;
using System.IO;
using System.Linq;
using System;

using Raylib_cs;
using ImGuiNET;
using static ImGuiNET.ImGui;

using Tiler.Editor;

public class Create : BaseView
{
    private int columns;
    private int rows;
    private int layers;
    private bool cameras;
    private string name;

    private bool nameExists;

    private readonly Managed.RenderTexture preview;

    private void Reset()
    {
        columns = 1;
        rows = 1;
        layers = 5;
        cameras = true;
        name = "New Level";
    }

    private bool DoesNameExist() => Directory.Exists(Path.Combine(Context.Dirs.Projects, name));

    private void UpdatePreview()
    {
        var size = new Vector2(Level.DefaultWidth * 4, Level.DefaultHeight * 4);
        var space = new Vector2(size.X / columns, size.Y / rows);
        var ratio = new Vector2(space.X / size.X, space.Y / size.Y);
        var min = MathF.Min(ratio.X, ratio.Y);

        Raylib.BeginTextureMode(preview);
        Raylib.ClearBackground(new Color(0, 0, 0, 0));
        for (var row = 0; row < rows; row++)
        {
            for (var column = 0; column < columns; column++)
            {
                Raylib.DrawRectangleLinesEx(
                    rec:       new Rectangle(position: new Vector2(column, row) * size * min, size: size * min),
                    lineThick: 1f,
                    color:     Color.White
                );

                if (cameras) 
                    Raylib.DrawCircleLinesV(
                        center: new Vector2(column, row) * size * min + (size * min / 2), 
                        radius: 10, 
                        color:  Color.White
                    );
            }             
        }
        Raylib.EndTextureMode();
    }

    public Create(Context context) : base(context)
    {
        name = "";
        Reset();
        nameExists = DoesNameExist();

        preview = new Managed.RenderTexture(
            width: Level.DefaultWidth * 4, 
            height: Level.DefaultHeight * 4, 
            clearColor: new Color4(0, 0, 0, 0), 
            clear: true
        );

        UpdatePreview();
    }

    public override void GUI()
    {
        if (Begin(
            name: "Create New Level##CreateNewLevel", 
            flags: ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse
        )) {
            SetWindowPos(new Vector2(30, 60));
            SetWindowSize(new Vector2(Raylib.GetScreenWidth() - 60, Raylib.GetScreenHeight() - 120));

            if (BeginTable("Table", columns: 2))
            {
                TableNextRow();

                TableSetColumnIndex(0);

                if (nameExists) TextColored(new Vector4(1f, 0.1f, 0.1f, 1f), "Name already exists");
                if (InputText(label: "Name", input: ref name, maxLength: 256)) nameExists = DoesNameExist();
                if (InputInt(label: "Rows", ref rows)) { rows = Math.Clamp(rows, 1, 6); UpdatePreview(); }
                if (InputInt(label: "Columns", ref columns)) { columns = Math.Clamp(columns, 1, 6); UpdatePreview(); }
                if (InputInt(label: "Layers", ref layers)) layers = Math.Clamp(layers, 0, 5);
                if (Checkbox(label: "Cameras", ref cameras)) UpdatePreview();

                TableSetColumnIndex(1);

                rlImGui_cs.rlImGui.ImageRenderTextureFit(preview, center: false);

                EndTable();
            }

            if (nameExists) BeginDisabled();
            if (Button(label: "Create"))
            {
                var level = new Level(
                    width: columns*Level.DefaultWidth + Viewports.LightmapMargin*2/20, 
                    height: rows*Level.DefaultHeight + Viewports.LightmapMargin*2/20
                ) {
                    Name = name,
                    Directory = Context.Dirs.Projects,
                };

                for (var z = 0; z < 2; z++)
                {
                    for (var y = 0; y < level.Height; y++)
                    {
                        for (var x = 0; x < level.Width; x++)
                        {
                            level.Geos[x, y, z] = Geo.Solid;
                        }
                    }
                }

                if (cameras)
                {
                    level.Cameras = [
                        ..Enumerable
                            .Range(0, rows)
                            .SelectMany(r =>
                                Enumerable
                                    .Range(0, columns)
                                    .Select(c => 
                                        new LevelCamera(
                                            new Vector2(
                                                x: c * Level.DefaultWidth * 20 + Viewports.LightmapMargin,
                                                y: r * Level.DefaultHeight * 20 + Viewports.LightmapMargin
                                            )
                                        )
                                    )
                            )
                    ];
                }

                Context.AddLevel(level);

                Context.SelectLevel(level);
                Context.Viewer.Select(Context.Viewer.Geos);
            }
            if (nameExists) EndDisabled();

            if (Button(label: "Reset to default"))
                Reset();

            if (Button(label: "Cancel"))
                Context.Viewer.Select(Context.Viewer.Start);
        }

        End();
    }
}
