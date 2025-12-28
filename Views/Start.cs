namespace Tiler.Editor.Views;

using System.Numerics;

using Raylib_cs;
using ImGuiNET;
using static ImGuiNET.ImGui;

using Tiler.Editor;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System;
using Serilog;

public class Start : BaseView
{
    private byte[] urlBuffer = [];

    private readonly string basePath = "";
    private string currentPath = "";

    private List<string> entries = [];
    private List<bool> isDir = [];
    private List<string> projectNames = [];

    private int selectedEntryIndex;

    public Start(Context context) : base(context)
    {
        GoTo(context.Dirs.Projects);
    }

    public void GoTo(string path)
    {
        if (File.Exists(path)) path = Directory.GetParent(path)!.FullName;

        if (!Directory.Exists(path)) return;

        urlBuffer = Encoding.UTF8.GetBytes(path);
        currentPath = path;

        entries = [ ..Directory.GetDirectories(path).OrderBy((f) => !File.Exists(Path.Combine(f, "level.ini"))) ];
        isDir = [ ..entries.Select(d => !File.Exists(Path.Combine(d, "level.ini"))) ];
        projectNames = [ ..entries.Select((f) =>Path.GetFileNameWithoutExtension(f)!) ];
    }

    public void GoHome() => GoTo(basePath);
    public void GoUp() => GoTo(Directory.GetParent(currentPath)!.FullName);

    public override void Process()
    {
        if (Raylib.IsKeyPressed(KeyboardKey.N))
        {
            var level = new Level
            {
                Directory = Context.Dirs.Projects,
            };

            for (int z = 0; z < 2; z++)
            {
                for (int y = 0; y < level.Height; y++)
                {
                    for (int x = 0; x < level.Width; x++)
                    {
                        level.Geos[x, y, z] = Geo.Solid;
                    }
                }
            }

            Context.AddLevel(level);

            Context.SelectLevel(level);
            Context.Viewer.Select<Geos>();
        }
    }

    public override void GUI()
    {
        if (ImGui.IsKeyPressed(ImGuiKey.UpArrow))
        {
            selectedEntryIndex--;

            if (selectedEntryIndex < 0) 
                selectedEntryIndex = entries.Count - 1;
        }
        else if (ImGui.IsKeyPressed(ImGuiKey.DownArrow))
        {
            selectedEntryIndex++;

            if (selectedEntryIndex >= entries.Count) 
                selectedEntryIndex = 0;
        }

        Exception? loadExcep = null;

        if (Begin(
            "Project Explorer", 
            ImGuiWindowFlags.NoMove | 
            ImGuiWindowFlags.NoCollapse | 
            ImGuiWindowFlags.NoResize
            )
        )
        {
            SetWindowPos(new Vector2(30, 60));
            SetWindowSize(new Vector2(Raylib.GetScreenWidth() - 60, Raylib.GetScreenHeight() - 120));

            SetNextItemWidth(GetContentRegionAvail().X);
            InputText("##URL", urlBuffer, 256);

            if (BeginListBox("##Projects", GetContentRegionAvail()))
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    var selected = Selectable(
                        projectNames[i], 
                        selectedEntryIndex == i, 
                        ImGuiSelectableFlags.None, 
                        GetContentRegionAvail() with { Y = 20 }
                    );

                    if (selected)
                    {
                        if (selectedEntryIndex == i)
                        {
                            if (isDir[i])
                            {
                                GoTo(entries[i]);
                            }
                            else
                            {
                                Log.Information("Loading level {Name}", projectNames[i]);

                                try
                                {
                                    var level = Level.FromDir(entries[i], Context.Tiles, Context.Props, Context.Effects);
                                    Context.AddLevel(level);
                                    Context.SelectLevel(level);
                                    Context.Viewer.Select<Geos>();

                                    Log.Information("Level loaded successfully");
                                }
                                catch (Exception e)
                                {
                                    Log.Error("Failed to load level at {Dir}\n{Exception}", entries[i], e);
                                    loadExcep = e;
                                }
                            }
                        }
                        else
                        {
                            selectedEntryIndex = i;
                        }
                    }
                }

                EndListBox();
            }
        }

        End();

        if (loadExcep is not null) OpenPopup("Error##LevelLoadError");

        if (BeginPopupModal("Error##LevelLoadError", ImGuiWindowFlags.NoCollapse))
        {
            Text("Failed to load level. View logs for more information");
            if (Button("Ok"))
            {
                loadExcep = null; // Ineffective
                CloseCurrentPopup();
            }
            EndPopup();
        }
    }
}