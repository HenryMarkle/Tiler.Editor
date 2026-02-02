namespace Tiler.Editor.Views;


using Raylib_cs;
using ImGuiNET;
using static ImGuiNET.ImGui;

using Tiler.Editor;

using System.Numerics;
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

    private Managed.RenderTexture levelPreview = new(1, 1, clearColor: Color.LightGray, clear: true);
    private Matrix<Geo> previewMatrix = new(0, 0, 0);
    private int previewScale = 4;
    private int previewX;
    private int previewY;

    private void SetPreview(string levelDir)
    {
        var iniPath = Path.Combine(levelDir, "level.ini");
        var geoPath = Path.Combine(levelDir, "geometry.txt");
        
        if (!File.Exists(iniPath) || !File.Exists(geoPath)) return;

        var levelInfo = new IniParser.FileIniDataParser().ReadFile(iniPath)["level"];
        var (width, height) = (levelInfo["width"].ToInt(), levelInfo["height"].ToInt());

        var text = File.ReadAllText(geoPath);

        {
            var matrix = new Matrix<Geo>(width, height, 3);

            var cells = File
                .ReadAllText(geoPath)
                .Split('|')
                .Select((c, i) => 
                    (
                        c is "" ? Geo.Air : Enum.TryParse<Geo>(c, out var cell) ? cell : Geo.Solid,
                        i % width,                          // x
                        (i % (width * height)) / width,     // y
                        i / (width * height)                // z
                    )
                )
                .Where(tuple => tuple.Item4 < 3);

            foreach (var (cell, x, y, z) in cells) matrix[x, y, z] = cell;
            
            previewMatrix = matrix;
        }

        if (levelPreview.Width != width || levelPreview.Height != height)
        {
            levelPreview = new Managed.RenderTexture(
                width * previewScale, 
                height * previewScale, 
                
                clearColor: Color.LightGray, 
                clear:      true
            );
        }

        levelPreview.Clear();

        previewX = previewY = 0;
    }

    private void AdvancePreview(int threashold = 500)
    {
        // Duplicate; I know
        if (previewY >= previewMatrix.Height) return;
        if (previewX >= previewMatrix.Width)
        {
            previewX = 0;
            previewY++;
            return;
        }

        var progress = 0;

        Raylib.BeginTextureMode(levelPreview);

        while (progress++ < threashold)
        {
            if (previewY >= previewMatrix.Height) break;
            if (previewX >= previewMatrix.Width)
            {
                previewX = 0;
                previewY++;
                continue;
            }

            for (var z = 2; z >= 0; z--)
            switch (previewMatrix[previewX, previewY, z])
            {
                case Geo.Solid:
                case Geo.Exit:
                Raylib.DrawRectangle(
                    previewX * previewScale, 
                    previewY * previewScale, 
                    previewScale, 
                    previewScale, 
                    new Color(z*100, z*100, z*100, 255)
                );
                break;

                case Geo.Platform:
                Raylib.DrawRectangle(
                    previewX * previewScale, 
                    previewY * previewScale, 
                    previewScale, 
                    previewScale/2, 
                    new Color(z*100, z*100, z*100, 255)
                );
                break;

                case Geo.Slab:
                Raylib.DrawRectangle(
                    previewX * previewScale, 
                    previewY * previewScale + previewScale/2, 
                    previewScale, 
                    previewScale/2, 
                    new Color(z*100, z*100, z*100, 255)
                );
                break;

                case Geo.VerticalPole:
                Raylib.DrawRectangle(
                    previewX * previewScale + previewScale/2, 
                    previewY * previewScale, 
                    previewScale/4, 
                    previewScale, 
                    new Color(z*100, z*100, z*100, 255)
                );
                break;

                case Geo.HorizontalPole:
                Raylib.DrawRectangle(
                    previewX * previewScale, 
                    previewY * previewScale + previewScale/2, 
                    previewScale, 
                    previewScale/4, 
                    new Color(z*100, z*100, z*100, 255)
                );
                break;

                case Geo.CrossPole:
                Raylib.DrawRectangle(
                    previewX * previewScale + previewScale/2, 
                    previewY * previewScale, 
                    previewScale/4, 
                    previewScale, 
                    new Color(z*100, z*100, z*100, 255)
                );
                Raylib.DrawRectangle(
                    previewX * previewScale, 
                    previewY * previewScale + previewScale/2, 
                    previewScale, 
                    previewScale/4, 
                    new Color(z*100, z*100, z*100, 255)
                );
                break;

                case Geo.SlopeNW:
                Raylib.DrawTriangle(
                    new(previewX * previewScale + previewScale, previewY * previewScale),
                    new(previewX * previewScale, previewY * previewScale + previewScale),
                    new(previewX * previewScale + previewScale, previewY * previewScale + previewScale),
                    new Color(z*100, z*100, z*100, 255)
                );
                break;

                // TODO: Complete the rest of slopes
            }

            previewX++;
        }

        Raylib.EndTextureMode();
    }

    public Start(Context context) : base(context)
    {
        GoTo(context.Dirs.Projects);

        if (selectedEntryIndex >= 0 && selectedEntryIndex < entries.Count && !isDir[selectedEntryIndex])
        {
            SetPreview(entries[selectedEntryIndex]);
        }
    }

    public void GoTo(string path)
    {
        if (!Directory.Exists(path)) path = Directory.GetParent(path)!.FullName;
        if (!Directory.Exists(path)) return;

        urlBuffer = Encoding.UTF8.GetBytes(path);
        currentPath = path;

        entries = [ ..Directory
            .GetDirectories(path)
            .Where(d => !Path.GetFileName(d).StartsWith('.'))
            .OrderBy((f) => !File.Exists(Path.Combine(f, "level.ini"))) 
        ];
        isDir = [ ..entries.Select(d => !File.Exists(Path.Combine(d, "level.ini"))) ];
        projectNames = [ ..entries.Select(f => Path.GetFileNameWithoutExtension(f)!) ];

        if (entries.Count > 0)
        {
            selectedEntryIndex = 0;
            if (!isDir[0]) SetPreview(entries[0]);
        }
    }

    public void GoHome() => GoTo(basePath);
    public void GoUp() => GoTo(Directory.GetParent(currentPath)!.FullName);

    public override void Process()
    {
        if (Raylib.IsKeyPressed(KeyboardKey.N))
        {
            Context.Viewer.Select(Context.Viewer.Create);
        }

        AdvancePreview();
    }

    public override void GUI()
    {
        Exception? loadExcep = null;

        if (IsKeyPressed(ImGuiKey.UpArrow))
        {
            selectedEntryIndex--;

            if (selectedEntryIndex < 0) 
                selectedEntryIndex = entries.Count - 1;
        }
        else if (IsKeyPressed(ImGuiKey.DownArrow))
        {
            selectedEntryIndex++;

            if (selectedEntryIndex >= entries.Count) 
                selectedEntryIndex = 0;
        }

        if (IsKeyPressed(ImGuiKey.Enter) && selectedEntryIndex >= 0 && selectedEntryIndex < entries.Count)
        {
            if (isDir[selectedEntryIndex])
            {
                GoTo(entries[selectedEntryIndex]);
            }
            else
            {
                Log.Information("Loading level {Name}", projectNames[selectedEntryIndex]);

                try
                {
                    var level = Level.FromDir(entries[selectedEntryIndex], Context.Tiles, Context.Props, Context.Effects);
                    Context.AddLevel(level);
                    Context.SelectLevel(level);
                    Context.Viewer.Select(Context.Viewer.Geos);

                    Log.Information("Level loaded successfully");
                }
                catch (Exception e)
                {
                    Log.Error("Failed to load level at {Dir}\n{Exception}", entries[selectedEntryIndex], e);
                    loadExcep = e;
                }
            }
        }

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

            if (Button("Projects")) GoHome();
            SameLine();
            if (Button("Up")) GoUp();
            SameLine();

            SetNextItemWidth(GetContentRegionAvail().X);
            InputText("##URL", urlBuffer, 256);

            Columns(2);

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
                                    Context.Viewer.Select(Context.Viewer.Geos);

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

                            if (!isDir[i]) SetPreview(entries[i]);
                        }
                    }
                }

                EndListBox();
            }
        
            NextColumn();
            
            rlImGui_cs.rlImGui.ImageRenderTextureFit(levelPreview, false);
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