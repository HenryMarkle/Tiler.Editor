namespace Tiler.Editor;

using Serilog;
using Raylib_cs;
using rlImGui_cs;
using ImGuiNET;

using System;
using System.Linq;
using System.Threading;
using System.Globalization;

using Tiler.Editor.Managed;
using Serilog.Core;
using System.IO;

public class Program {
	public static void Main(string[] args) {
		Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("en-US");

		var paths = new AppDirectories();

		#if DEBUG
		Log.Logger = new LoggerConfiguration()
			.MinimumLevel.Debug()
			.WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")
			.CreateLogger();
		#else
		Log.Logger = new LoggerConfiguration()
			.WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")
			.CreateLogger();
		#endif


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

		#if DEBUG
		Raylib.SetTraceLogLevel(TraceLogLevel.Warning);
		#else
		Raylib.SetTraceLogLevel(TraceLogLevel.Error);
		#endif

		Raylib.SetTargetFPS(45);
		Raylib.InitWindow(1400, 800, "Tiler Editor");
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
		var effcdex = EffectDex.FromEffectsDir(paths.Effects);

		Log.Information("Loading context");

		var context = new Context()
        {
			Dirs = paths,
			Viewports = new(70 * 20, 40 * 20, 5),
			Tiles = tiledex,
			Props = propdex,
			Effects = effcdex,
			Config = config,
			DebugPrinter = printer
        };

		if (Directory.Exists(paths.Palettes))
		{
			Log.Information("Loading palettes");
			
			foreach (var paletteFile in Directory.GetFiles(paths.Palettes).Where(f => f.EndsWith(".png")))
			{
				context.Palettes[Path.GetFileNameWithoutExtension(paletteFile)] = new HybridImage(Raylib.LoadImage(paletteFile));
			}
		}

		Log.Information("Loading views");

		var viewer = new Viewer(context);
		context.Viewer = viewer;

		// Stored exceptions

		Exception? levelSaveExcep = null;

		// --------------------------------------------------------------
		// -------------------------- LOOP ------------------------------
		while (!Raylib.WindowShouldClose()) {

			// -------------------------------------------------------------
			// ------------------------ PROCESS ----------------------------

			printer.Reset();
			Unloader.Dequeue(20);

			if (viewer.SelectedView is not Views.Start and not Views.Create)
			{
				if (Raylib.IsKeyPressed(KeyboardKey.One)) viewer.Select(viewer.Geos);
				else if (Raylib.IsKeyPressed(KeyboardKey.Two)) viewer.Select(viewer.Tiles);
				else if (Raylib.IsKeyPressed(KeyboardKey.Three)) viewer.Select(viewer.Connections);
				else if (Raylib.IsKeyPressed(KeyboardKey.Four)) viewer.Select(viewer.Cameras);
				else if (Raylib.IsKeyPressed(KeyboardKey.Five)) viewer.Select(viewer.Light);
				else if (Raylib.IsKeyPressed(KeyboardKey.Six)) viewer.Select(viewer.Light);
				else if (Raylib.IsKeyPressed(KeyboardKey.Seven)) viewer.Select(viewer.Effects);
				else if (Raylib.IsKeyPressed(KeyboardKey.Eight)) viewer.Select(viewer.Props);
			}

			viewer.SelectedView.Process();

			Raylib.BeginDrawing();
			Raylib.ClearBackground(Color.Gray);

			viewer.SelectedView.Draw();

			// -------------------------------------------------------------
			// -------------------------- GUI ------------------------------
			rlImGui.Begin();
			
			ImGui.BeginMainMenuBar();
			if (viewer.SelectedView is not Views.Start and not Views.Create)
            {
                if (ImGui.BeginMenu("Project")) {
					if (ImGui.MenuItem("Save", "CTRL + S", false, context.SelectedLevel is not null))
					{
						context.SelectedLevel!.Lightmap = new Managed.Image(
							Raylib.LoadImageFromTexture(context.Viewports.Lightmap.Raw.Texture)
						);
						
						try
						{
							context.SelectedLevel!.Save(paths.Projects);
						}
						catch (Exception e)
						{
							Log.Error("Failed to save level '{Name}'\n{Exception}", context.SelectedLevel!.Name, e);
							levelSaveExcep = e;
						}
					}

					ImGui.MenuItem("Save As", "CTRL + SHIFT + S", false, false);
					if (ImGui.MenuItem("Open", "CTRL + O")) viewer.Select(viewer.Start);
					if (ImGui.MenuItem("Create", "CTRL + N")) viewer.Select(viewer.Create);
					ImGui.EndMenu();
				}
				
				if (ImGui.MenuItem("Geometry", "", viewer.SelectedView is Views.Geos)) 
					viewer.Select(viewer.Geos);
				
				if (ImGui.MenuItem("Tiles", "", viewer.SelectedView is Views.Tiles)) 
					viewer.Select(viewer.Tiles);
				
				if (ImGui.MenuItem("Connections", "", viewer.SelectedView is Views.Connections)) 
					viewer.Select(viewer.Connections);
				
				if (ImGui.MenuItem("Cameras", "", viewer.SelectedView is Views.Cameras)) 
					viewer.Select(viewer.Cameras);
				
				if (ImGui.MenuItem("Light", "",  viewer.SelectedView is Views.Light)) 
					viewer.Select(viewer.Light);
				
				if (ImGui.MenuItem("Dimensions", null, false, false)) 
				
				if (ImGui.MenuItem("Effects", "", viewer.SelectedView is Views.Effects))
					viewer.Select(viewer.Effects);
				
				if (ImGui.MenuItem("Props", "", viewer.SelectedView is Views.Props))
					viewer.Select(viewer.Props);
				
				if (ImGui.MenuItem("Render", "", viewer.SelectedView is Views.Render)) 
					viewer.Select(viewer.Render);
				
            }
			ImGui.EndMainMenuBar();

			viewer.SelectedView.GUI();

			{ // Error message
				if (levelSaveExcep is not null) ImGui.OpenPopup("Error##LevelSaveError");
				
				if (ImGui.BeginPopupModal("Error##LevelSaveError", ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.AlwaysAutoResize))
				{
					ImGui.Text("Failed to save level. View logs for more information");
					if (ImGui.Button("Ok"))
					{
						levelSaveExcep = null;
						ImGui.CloseCurrentPopup();
					}
					
					ImGui.EndPopup();
				}
			}

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
