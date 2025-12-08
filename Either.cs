using System;

namespace Tiler.Editor;

public readonly struct Either<TLeft, TRight>
{
    private readonly TLeft? left;
    private readonly TRight? right;

    /// <summary>
    /// -1: left
    ///  1: right
    /// </summary>
    private readonly int status;

    public TLeft Left => left!;
    public TRight Right => right!;

    public bool IsLeft => status is -1;
    public bool IsRight => status is 1;

    public Either(TLeft left)
    {
        status = -1;
        
        this.left = left;
    }

    public Either(TRight right)
    {
        status = 1;

        this.right = right;
    }

    public override string ToString() => 
        $"Result({status switch { -1 => left?.ToString(), 1 => right?.ToString(), _ => throw new InvalidOperationException() }})";
}