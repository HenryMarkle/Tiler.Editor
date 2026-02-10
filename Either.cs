using System;

namespace Tiler.Editor;

/// <summary>
/// Represents a state that can be either <see cref="TLeft"/> or <see cref="TRight"/>.
/// </summary>
public readonly struct Either<TLeft, TRight>
{
    private readonly TLeft? left;
    private readonly TRight? right;
    
    public enum StateStatus { Left, Right }
    public StateStatus Status { get; }

    public TLeft Left => Status is StateStatus.Left 
        ? left ?? throw new InvalidOperationException("Either was right")
        : throw new InvalidOperationException("Either was right");
    public TRight Right => Status is StateStatus.Right 
        ? right ?? throw new InvalidOperationException("Either was left")
        : throw new InvalidOperationException("Either was left");

    public bool IsLeft => Status is StateStatus.Left;
    public bool IsRight => Status is StateStatus.Right;

    public Either(TLeft left)
    {
        Status = StateStatus.Left;
        
        this.left = left;
    }

    public Either(TRight right)
    {
        Status = StateStatus.Right;

        this.right = right;
    }

    public static Either<TLeft, TRight> FromLeft(TLeft left) => new(left);
    public static Either<TLeft, TRight> FromRight(TRight right) => new(right);

    public override string ToString() => 
        $"Result({Status switch { StateStatus.Left => left?.ToString(), StateStatus.Right => right?.ToString(), _ => throw new InvalidOperationException() }})";
}