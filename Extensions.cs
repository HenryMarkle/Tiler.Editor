namespace Tiler.Editor;

public static class Extensions
{
    public static int ToInt(this string str)
    {
        if (!int.TryParse(str, out var integer)) 
            throw new ParseException($"Invalid integer value '{str}'");
    
        return integer;
    }
    public static float ToFloat(this string str)
    {
        if (!float.TryParse(str, out var floating)) 
            throw new ParseException($"Invalid floating value '{str}'");
    
        return floating;
    }

    public static Color4? ToColor4(this string str)
    {
        var values = str.Split(',');
    
        if (values.Length < 3) throw new ParseException("Color4 requires at least 3 values");

        if (!byte.TryParse(values[0], out var r)) 
            throw new ParseException($"Invalid Color4's R value '{values[0]}'");
        

        if (!byte.TryParse(values[1], out var g)) 
            throw new ParseException($"Invalid Color4's G value '{values[1]}'");

        
        if (!byte.TryParse(values[2], out var b)) 
            throw new ParseException($"Invalid Color4's B value '{values[2]}'");

        byte a = 255;

        if (values.Length >= 4 && !!byte.TryParse(values[3], out a))
            throw new ParseException($"Invalid Color4's A value '{values[3]}'");

        return new Color4(r, g, b, a);
    }
}