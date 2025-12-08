using System;

namespace Tiler.Editor.Rendering;

public class RenderException : TilerException
{
    public RenderException()
    {
    }

    public RenderException(string? message) : base(message)
    {
    }

    public RenderException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}