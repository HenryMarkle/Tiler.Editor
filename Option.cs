using System;

namespace Tiler.Editor;

public readonly struct Option<T>
{
    private readonly T? value;

    public bool HasValue { get; init; }

    public T Value => HasValue 
        ? (value ?? throw new InvalidOperationException("Option had no value")) 
        : throw new InvalidOperationException("Option had no value");

    public Option()
    {
        value = default;
        HasValue = false;
    }

    public Option(T value)
    {
        this.value = value;
        HasValue = true;
    }

    public static Option<T> Some(T value) => new(value);
    public static Option<T> None() => new();

    public override string ToString() => $"Option({(HasValue ? value!.ToString() : "None")})";
}