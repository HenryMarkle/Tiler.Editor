using System;

namespace Tiler.Editor.Rendering.Scripting;

public class ScriptingException : TilerException
{
    public ScriptingException()
    {
    }

    public ScriptingException(string? message) : base(message)
    {
    }

    public ScriptingException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}