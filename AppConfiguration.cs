using System;
using System.IO;
using System.Linq;
using IniParser;

namespace Tiler.Editor;

public enum GeometryLayerColoring
{
    RGB, Gray, Purple
}

public class AppConfiguration
{
    public GeometryLayerColoring GeoColoring = GeometryLayerColoring.RGB;

    public static AppConfiguration FromFile(string path)
    {
        var parser = new FileIniDataParser();

        var ini = parser.ReadFile(path);

        var view = ini["view"];


        if (!Enum.TryParse(view["GeoColoring"], false, out GeometryLayerColoring geoColoring)) 
            geoColoring = GeometryLayerColoring.RGB;

        return new()
        {
            GeoColoring = geoColoring
        };
    }
}