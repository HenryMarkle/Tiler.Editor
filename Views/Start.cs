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

public class Start : BaseView
{
    private byte[] urlBuffer = [];

    private readonly string basePath = "";
    private string currentPath = "";

    private List<string> projectDirs = [];
    private List<string> projectNames = [];

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

        projectDirs = [ ..Directory.GetDirectories(path).Where(f => File.Exists(Path.Combine(f, "tile.ini"))) ];
        projectNames = [ ..projectDirs.Select((f) =>Path.GetFileNameWithoutExtension(f)!) ];
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
                for (int i = 0; i < projectDirs.Count; i++)
                {
                    Selectable(projectNames[i]);
                }

                EndListBox();
            }
        }

        End();
    }
}