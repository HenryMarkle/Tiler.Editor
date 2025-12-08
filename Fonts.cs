using System.Collections.Generic;
using Raylib_cs;

namespace Tiler.Editor;

public class Fonts
{
    public List<(string name, Font font, int size)> List { get; init; }

    public Fonts()
    {
        List = [];
    }

    public Fonts(string fontsDir)
    {
        List = [];
        
        foreach (var entry in System.IO.Directory.GetFiles(fontsDir))
        {
            if (!entry.EndsWith(".ttf")) continue;

            var name = System.IO.Path.GetFileNameWithoutExtension(entry)!;

            List.Add((
                name,
                Raylib.LoadFontEx(entry, 24, [], 0),
                24
            ));

            List.Add((
                name,
                Raylib.LoadFontEx(entry, 20, [], 0),
                20
            ));

            List.Add((
                name,
                Raylib.LoadFontEx(entry, 16, [], 0),
                16
            ));
        }
    }
};