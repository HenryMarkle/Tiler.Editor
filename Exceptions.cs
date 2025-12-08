using System;
using System.Runtime.Serialization;

namespace Tiler.Editor;

public class TilerException : Exception
{
    public TilerException()
    {
    }

    public TilerException(string? message) : base(message)
    {
    }

    public TilerException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}

public class ParseException : TilerException
{
    public ParseException()
    {
    }

    public ParseException(string? message) : base(message)
    {
    }

    public ParseException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}

public class TileParseException : ParseException
{
    public TileParseException()
    {
    }

    public TileParseException(string? message) : base(message)
    {
    }

    public TileParseException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}

public class LevelParseException : ParseException
{
    public LevelParseException()
    {
    }

    public LevelParseException(string? message) : base(message)
    {
    }

    public LevelParseException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}

public sealed class DuplicateTileException(string tileId) : TilerException($"Duplicate tile '{tileId}'")
{
    public readonly string TileID = tileId;
}
