using Serilog.Core;
using Serilog.Events;

namespace Tiler.Editor;

using Serilog;
using Raylib_cs;
using rlImGui_cs;
using ImGuiNET;

using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Globalization;

using Tiler.Editor.Managed;

public static class Program {
	public static void Main(string[] args)
	{
		var logLevelIndex = Array.FindIndex(args, str => str == "-l") + 1;
		var logLevel = args.Length > logLevelIndex 
			? args[logLevelIndex] 
			: null;

		var loggingLevelSwitch = new LoggingLevelSwitch
		{
			MinimumLevel = logLevel switch
			{
				"d" or "debug" => LogEventLevel.Debug,
				"e" or "error" => LogEventLevel.Error,
				"f" or "fatal" => LogEventLevel.Fatal,
				"v" or "verbose" => LogEventLevel.Verbose,
				"i" or "information" => LogEventLevel.Information,
				"w" or "warning" => LogEventLevel.Warning,
			
				_ => LogEventLevel.Debug
			}
		};

		Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("en-US");

		var paths = new AppDirectories();

		#if DEBUG
		Log.Logger = new LoggerConfiguration()
			.MinimumLevel.ControlledBy(loggingLevelSwitch)
			.WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")
			.CreateLogger();
		#else
		Log.Logger = new LoggerConfiguration()
			.WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")
			.CreateLogger();
		#endif


		Log.Information("---------------------------------- Starting program");
		
		Log.Information("Inspecting directories:\n{DIRS}", paths);

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

		// --------------------------------------------------------------
		// -------------------------- WINDOW ----------------------------

		Raylib.SetTargetFPS(45);
		Raylib.InitWindow(width: 1400, height: 800, title: "Tiler Editor");
		Raylib.SetWindowState(flag: ConfigFlags.ResizableWindow);
		Raylib.SetWindowMinSize(width: 1200, height: 800);
		Raylib.SetWindowIcon(Raylib.LoadImage(fileName: Path.Combine(paths.Executable, "icon.png")));

		Raylib.SetGesturesEnabled(flags: Gesture.PinchIn | Gesture.PinchOut);
		Rlgl.DisableBackfaceCulling();

		// Raylib.SetExitKey(KeyboardKey.Null);

        rlImGui.Setup(darkTheme: true, enableDocking: true);

        ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.DockingEnable;
        ImGui.GetIO().ConfigDockingWithShift = true;
        ImGui.GetIO().ConfigDockingTransparentPayload = true;

		Log.Information("Loading assets");

		var fonts = new Fonts(paths.Fonts);

		var printer = new DebugPrinter
        {
            Font = fonts.List.Single(f => f is { name: "FiraCode-Regular", size: 20 }).font,
			Size = 20,
			Anchor = new Vector2(0, 30)
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

			if (Raylib.IsKeyPressed(KeyboardKey.F3)) 
				config.ShowDebugScreen = !config.ShowDebugScreen;

			if (config.ShowDebugScreen)
				printer.Reset();

			Unloader.Dequeue(cap: 20);

			if (viewer.SelectedView is not Views.Start and not Views.Create && Raylib.IsKeyDown(KeyboardKey.LeftAlt))
			{
				if (Raylib.IsKeyPressed(KeyboardKey.One)) viewer.Select(viewer.Geos);
				else if (Raylib.IsKeyPressed(KeyboardKey.Two)) viewer.Select(viewer.Tiles);
				else if (Raylib.IsKeyPressed(KeyboardKey.Three)) viewer.Select(viewer.Connections);
				else if (Raylib.IsKeyPressed(KeyboardKey.Four)) viewer.Select(viewer.Cameras);
				else if (Raylib.IsKeyPressed(KeyboardKey.Five)) viewer.Select(viewer.Light);
				else if (Raylib.IsKeyPressed(KeyboardKey.Six)) viewer.Select(viewer.Light); // TODO: Fix duplicate
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
			if (ImGui.BeginMenu(label: "Project")) {
				if (ImGui.BeginMenu(label: "Projects"))
				{
					foreach (var project in context.Levels)
					{
						if (ImGui.MenuItem(
							    label: $"{project.Name}##{project.GetHashCode()}", 
							    shortcut: "", 
							    selected: context.SelectedLevel == project))
							context.SelectLevel(project);
					}
					
					ImGui.EndMenu();
				}
				
				if (ImGui.MenuItem(
					label: "Save", 
					shortcut: "CTRL + S", 
					selected: false, 
					enabled: viewer.SelectedView is not Views.Start and not Views.Create && context.SelectedLevel is not null
					)
				) {
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

				ImGui.MenuItem(
					label: "Save As", 
					shortcut: "CTRL + SHIFT + S", 
					selected: false, 
					enabled: viewer.SelectedView is not Views.Start and not Views.Create && context.SelectedLevel is not null
				);
				if (ImGui.MenuItem(label: "Open", shortcut: "CTRL + O")) viewer.Select(viewer.Start);
				if (ImGui.MenuItem(label: "Create", shortcut: "CTRL + N")) viewer.Select(viewer.Create);
				if (ImGui.MenuItem(
					    label: "Close", 
					    shortcut: "", 
					    selected: false, 
					    enabled: viewer.SelectedView is not Views.Start and not Views.Create && context.SelectedLevel is not null))
				{
					context.RemoveLevel(context.SelectedLevel!);
					if (context.Levels.Count == 0) viewer.Select(viewer.Start);
				}
				ImGui.EndMenu();
			}
			
			if (viewer.SelectedView is not Views.Start and not Views.Create)
            {
				if (ImGui.MenuItem(label: "Geometry", shortcut: "", viewer.SelectedView is Views.Geos)) 
					viewer.Select(viewer.Geos);
				
				if (ImGui.MenuItem(label: "Tiles", shortcut: "", viewer.SelectedView is Views.Tiles)) 
					viewer.Select(viewer.Tiles);
				
				if (ImGui.MenuItem(label: "Connections", shortcut: "", viewer.SelectedView is Views.Connections)) 
					viewer.Select(viewer.Connections);
				
				if (ImGui.MenuItem(label: "Cameras", shortcut: "", viewer.SelectedView is Views.Cameras)) 
					viewer.Select(viewer.Cameras);
				
				if (ImGui.MenuItem(label: "Light", shortcut: "",  viewer.SelectedView is Views.Light)) 
					viewer.Select(viewer.Light);
				
				if (ImGui.MenuItem(label: "Dimensions", shortcut: null, selected: false, enabled: false)) {}
				
				if (ImGui.MenuItem(label: "Effects", shortcut: "", viewer.SelectedView is Views.Effects))
					viewer.Select(viewer.Effects);
				
				if (ImGui.MenuItem(label: "Props", shortcut: "", viewer.SelectedView is Views.Props))
					viewer.Select(viewer.Props);
				
				if (ImGui.MenuItem(label: "Render", shortcut: "", viewer.SelectedView is Views.Render)) 
					viewer.Select(viewer.Render);
            }

			if (ImGui.BeginMenu(label: "Misc"))
			{
				if (ImGui.MenuItem(label: "Debug Screen", shortcut: "F3", selected: config.ShowDebugScreen))
					config.ShowDebugScreen = !config.ShowDebugScreen;
				
				if (ImGui.MenuItem(label: "Palettes", shortcut: "", viewer.SelectedView is Views.Palettes))
					viewer.Select(viewer.Palettes);
			
				ImGui.EndMenu();
			}
			ImGui.EndMainMenuBar();

			viewer.SelectedView.GUI();

			{ // Error message
				if (levelSaveExcep is not null) ImGui.OpenPopup("Error##LevelSaveError");
				
				if (ImGui.BeginPopupModal(name: "Error##LevelSaveError", flags: ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.AlwaysAutoResize))
				{
					ImGui.Text("Failed to save level. View logs for more information");
					if (ImGui.Button(label: "Ok"))
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

			if (config.ShowDebugScreen)
			{
				printer.Print("Tiler | Debug |" );
				printer.PrintlnLabel("FPS", Raylib.GetFPS(), new Color4(20, 255, 20));
	
				var totalMemUsed = System.Diagnostics.Process.GetCurrentProcess().PrivateMemorySize64 / 1024f / 1024f;
				
				printer.PrintlnLabel(
					"Memory", 
					$"{(totalMemUsed > 1001 ? totalMemUsed / 1024f : totalMemUsed) :F2} {(totalMemUsed > 1001 ? "GB" : "MB")}", 
					Color.SkyBlue
					);
				
				printer.PrintlnLabel("View", viewer.SelectedView.GetType().Name, new Color4(245, 70, 110));
	
				viewer.SelectedView.Debug();
			}
			
			Raylib.EndDrawing();
		}

		rlImGui.Shutdown();

		Raylib.CloseWindow();

		Log.Information("---------------------------------- Program terminated");

		Log.CloseAndFlush();
	}

	public readonly struct ProgramArguments
	{
		public string? LogLevel { get; init; }
		public int FPS { get; init; }

		public ProgramArguments()
		{
			LogLevel = null;
			FPS = 45;
		}

		public static ProgramArguments Parse(string[] args)
		{
			return new ProgramArguments
			{
				LogLevel = GetFlag("-l"),
				FPS = int.Parse(GetFlag("--fps") ?? "45"),
			};

			string? GetFlag(string name)
			{
				var index = Array.FindIndex(args, a => a == name);

				if (index >= 0 && index < args.Length - 1)
					return args[index + 1];
				
				return null;
			}
		}
	}
}
