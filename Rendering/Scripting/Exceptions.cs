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

public class InvalidScriptFunctionArgumentException : ScriptingException
{
    public object? Arguments { get; init; }
    
    public InvalidScriptFunctionArgumentException()
    {
    }

    public InvalidScriptFunctionArgumentException(string? message) : base(message)
    {
    }
    
    public InvalidScriptFunctionArgumentException(string? message, object? arguments) : base(message)
    {
        Arguments = arguments;
    }

    public InvalidScriptFunctionArgumentException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    public InvalidScriptFunctionArgumentException(string? message, object? arguments, Exception? innerException) : base(message, innerException)
    {
        Arguments = arguments;
    }
}