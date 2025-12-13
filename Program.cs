namespace Tiler.Editor;

using Serilog;
using Raylib_cs;
using rlImGui_cs;
using ImGuiNET;

using Tiler.Editor.Managed;
using System.Linq;
using System;

public class Program {
	public static void Main(string[] args) {

		var paths = new AppDirectories();

		Log.Logger = new LoggerConfiguration()
			.WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")
			.CreateLogger();

		Log.Information("---------------------------------- Starting program");

		Log.Information("Loading configuration");

		AppConfiguration config;

		try
		{
			config = AppConfiguration.FromFile(paths.Files.Config);
		}
		catch (Exception e)
		{
			Log.Error("Failed to load configuration file\n{Exception}", e);

			config = new AppConfiguration();
		}

		Log.Information("Initializing window");

		Raylib.SetTargetFPS(45);
		Raylib.InitWindow(1000, 800, "Tiler Editor");
		Raylib.SetWindowState(ConfigFlags.ResizableWindow);
		Raylib.SetWindowMinSize(1200, 800);

		Rlgl.DisableBackfaceCulling();

		// Raylib.SetExitKey(KeyboardKey.Null);

        rlImGui.Setup(true, true);

        ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.DockingEnable;
        ImGui.GetIO().ConfigDockingWithShift = true;
        ImGui.GetIO().ConfigDockingTransparentPayload = true;

		Log.Information("Loading assets");

		var fonts = new Fonts(paths.Fonts);

		var printer = new DebugPrinter
        {
            Font = fonts.List.Single(f => f.name == "FiraCode-Regular" && f.size == 20).font,
			Size = 20,
			Ancor = new(0, 30)
        };

		Log.Information("Loading resources");

		var tiledex = TileDex.FromTilesDir(paths.Tiles);
		var propdex = PropDex.FromPropsDir(paths.Props);

		Log.Information("Loading context");

		var context = new Context()
        {
			Dirs = paths,
			Viewports = new(70 * 20, 40 * 20, 3),
			Tiles = tiledex,
			Props = propdex,
			Config = config,
			DebugPrinter = printer
        };

		Log.Information("Loading views");

		var viewer = new Viewer(context);
		context.Viewer = viewer;

		// --------------------------------------------------------------
		// -------------------------- LOOP ------------------------------
		while (!Raylib.WindowShouldClose()) {

			// -------------------------------------------------------------
			// ------------------------ PROCESS ----------------------------

			printer.Reset();
			Unloader.Dequeue(20);

			viewer.SelectedView.Process();

			Raylib.BeginDrawing();
			Raylib.ClearBackground(Color.Gray);

			viewer.SelectedView.Draw();

			// -------------------------------------------------------------
			// -------------------------- GUI ------------------------------
			rlImGui.Begin();
			
			ImGui.BeginMainMenuBar();
			if (viewer.SelectedView.GetType() != typeof(Views.Start))
            {
                if (ImGui.BeginMenu("Project")) {
					ImGui.MenuItem("Save", "CTRL + S", false, false);
					ImGui.MenuItem("Save As", "CTRL + SHIFT + S", false, false);
					if (ImGui.MenuItem("Open", "CTRL + O")) viewer.Select<Views.Start>();
					ImGui.MenuItem("Create", "CTRL + N", false, false);
					ImGui.EndMenu();
				}
				if (ImGui.MenuItem("Geometry", "", viewer.SelectedView.GetType() == typeof(Views.Geos))) {
					viewer.Select<Views.Geos>();
				}
				if (ImGui.MenuItem("Tiles", "", viewer.SelectedView.GetType() == typeof(Views.Tiles))) {
					viewer.Select<Views.Tiles>();
				}
				if (ImGui.MenuItem("Cameras", "", viewer.SelectedView.GetType() == typeof(Views.Cameras))) {
					viewer.Select<Views.Cameras>();
				}
				if (ImGui.MenuItem("Light", "",  viewer.SelectedView.GetType() == typeof(Views.Light))) {
					viewer.Select<Views.Light>();
				}
				if (ImGui.MenuItem("Dimensions", null, false, false)) {
				}
				if (ImGui.MenuItem("Effects", null, false, false)) {
				}
				if (ImGui.MenuItem("Props", "", viewer.SelectedView.GetType() == typeof(Views.Props)))
				{
					viewer.Select<Views.Props>();
				}
				if (ImGui.MenuItem("Render", "", viewer.SelectedView.GetType() == typeof(Views.Render))) {
					viewer.Select<Views.Render>();
				}
            }
			ImGui.EndMainMenuBar();

			viewer.SelectedView.GUI();
			rlImGui.End();

			// -------------------------------------------------------------
			// ------------------------- DEBUG -----------------------------
			
			printer.Print("Tiler | Debug |" );
			printer.PrintlnLabel("FPS", Raylib.GetFPS(), new Color4(20, 255, 20));
			printer.PrintlnLabel("View", viewer.SelectedView.GetType().Name ?? "NULL", new Color4(245, 70, 110));

			viewer.SelectedView.Debug();
			
			Raylib.EndDrawing();
		}

		rlImGui.Shutdown();

		Raylib.CloseWindow();

		Log.Information("---------------------------------- Program terminated");

		Log.CloseAndFlush();
	}
}
