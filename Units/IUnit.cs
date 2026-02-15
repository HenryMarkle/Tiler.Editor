using System;
using System.Numerics;

namespace Tiler.Editor.Units;

public interface IUnit<TSelf, in TValue> 
    where TSelf : struct, IUnit<TSelf, TValue>
    where TValue : INumber<TValue>
{
    static abstract TSelf operator +(TSelf lhs, TSelf rhs);
    static abstract TSelf operator -(TSelf lhs, TSelf rhs);
    static abstract float operator /(TSelf lhs, TSelf rhs);
    static abstract TSelf operator *(TSelf lhs, int rhs);
    static abstract TSelf operator /(TSelf lhs, int rhs);
    static abstract TSelf operator *(TSelf lhs, float rhs);
    static abstract TSelf operator /(TSelf lhs, float rhs);
}
