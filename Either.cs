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

    public TLeft Left => left ?? throw new InvalidOperationException("Either was right");
    public TRight Right => right ?? throw new InvalidOperationException("Either was left");

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

    public static Either<TLeft, TRight> FromLeft(TLeft left) => new(left);
    public static Either<TLeft, TRight> FromRight(TRight right) => new(right);

    public override string ToString() => 
        $"Result({status switch { -1 => left?.ToString(), 1 => right?.ToString(), _ => throw new InvalidOperationException() }})";
}