namespace Tiler.Editor.Rendering.Scripting.V2;

using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.CSharp.Scripting;

using Serilog;

public interface ITilesScript : IScript { }

public static class Tiles
{
    public static ITilesScript CreateFromFile(string file)
    {
        Log.Debug("Loading tiles script file '{FILE}'", file);

        try
        {
            var script = CSharpScript.Create<ITilesScript>(
                file,
                ScriptOptions.Default.AddReferences(
                        typeof(object).Assembly,
                        typeof(System.Numerics.Vector2).Assembly,
                        typeof(System.Collections.Generic.List<>).Assembly,
                        typeof(System.Collections.Generic.Dictionary<,>).Assembly,
                        typeof(ITilesScript).Assembly,
                        typeof(Raylib_cs.Raylib).Assembly,
                        typeof(Raylib_cs.Raymath).Assembly,
                        typeof(Matrix<>).Assembly,
                        typeof(Geo).Assembly,
                        typeof(Tile.TileDef).Assembly,
                        typeof(Quad).Assembly,
                        typeof(Triangle).Assembly,
                        typeof(Viewports).Assembly,
                        typeof(Managed.Shader).Assembly,
                        typeof(Managed.Texture).Assembly,
                        typeof(Managed.Image).Assembly,
                        typeof(Managed.RenderTexture).Assembly
                    )
                    .WithImports(
                        "System.Collections.Generic",
                        "System.Collections.Numerics",
                        "Raylib_cs",
                        "Raymath = Raylib_cs.Raymath",
                        "Image = Tiler.Managed.Image")
            );

            // TODO: Log diagnostics
            var diagnostics = script.Compile();

            if (diagnostics.Any(d => d.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Warning))
            {
                Log.Information("Script compilation finished with warnings:");

                foreach (var error in diagnostics)
                {
                    Log.Information("\t[{LEVEL}] {MESSAGE} | at {LOCATION}",
                        error.Severity,
                        error.GetMessage(),
                        error.Location
                    );
                }
            }

            var state = script.RunAsync().GetAwaiter().GetResult();

            Log.Debug("Script compiled successfully");

            return state.ReturnValue;
        }
        catch (CompilationErrorException ce)
        {
            throw new ScriptCompilationException(
                file,
                ce);
        }
        catch (System.Exception e)
        {
            throw new ScriptInitializationException(file, e);
        }
    }
}
