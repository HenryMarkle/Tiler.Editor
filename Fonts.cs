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
                Raylib.LoadFontEx(entry, fontSize: 24, codepoints: [], codepointCount: 0),
                size: 24
            ));

            List.Add((
                name,
                Raylib.LoadFontEx(entry, fontSize: 20, codepoints: [], codepointCount: 0),
                size: 20
            ));

            List.Add((
                name,
                Raylib.LoadFontEx(entry, fontSize: 16, codepoints: [], codepointCount: 0),
                size: 16
            ));
        }
    }
};