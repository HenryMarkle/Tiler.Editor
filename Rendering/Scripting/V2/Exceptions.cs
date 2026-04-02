namespace Tiler.Editor.Rendering.Scripting.V2;

using Microsoft.CodeAnalysis.Scripting;

using System;

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

public class ScriptCompilationException : ScriptingException
{
    private string ScriptFile { get; init; }
    
    public ScriptCompilationException(string scriptFile) 
        : base($"Failed to compile script file '{scriptFile}'")
    {
        ScriptFile = scriptFile;
    }

    public ScriptCompilationException(string scriptFile, CompilationErrorException exception) 
        : base(
            $"Failed to compile script file '{scriptFile}'" +
            $"\n\t{string.Join(Environment.NewLine, exception.Diagnostics)}", 
            exception)
    {
        ScriptFile = scriptFile;
    }
}

public class ScriptInitializationException : ScriptingException
{
    public ScriptInitializationException(string scriptFile) 
        : base($"Failed to initialize script file '{scriptFile}'")
    {
    }

    public ScriptInitializationException(string scriptFile, Exception? innerException) 
        : base($"Failed to initialize script file '{scriptFile}'", innerException)
    {
    }
}