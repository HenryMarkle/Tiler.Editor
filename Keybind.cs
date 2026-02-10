using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace Tiler.Editor;

using System;
using System.Runtime.CompilerServices;

using Raylib_cs;

public class Keybind(KeyboardKey key, bool ctrl = false, bool shift = false, bool alt = false) 
    : IEquatable<Keybind>
{
    public string? Name;
    public string? Description;
    public string? Group;
    
    public bool Ctrl = ctrl;
    public bool Shift = shift;
    public bool Alt  = alt;

    public KeyboardKey Key = key;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Check(bool hold = false) => 
        Ctrl && Raylib.IsKeyDown(KeyboardKey.LeftControl)
        && Shift && Raylib.IsKeyDown(KeyboardKey.LeftShift)
        && Alt && Raylib.IsKeyDown(KeyboardKey.LeftAlt)
        && ((hold && Raylib.IsKeyDown(Key)) || Raylib.IsKeyPressed(Key));
    
    public static implicit operator Keybind(KeyboardKey key) => new(key); 
    public static implicit operator KeyboardKey(Keybind k) => k.Key;
    
    public void Deserialize(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression)) return;
        
        foreach (var segment in expression.Split('+').Select(s => s.Trim().ToLower()))
        {
            switch (segment)
            {
                case "ctrl": Ctrl = true; break;
                case "shift": Shift = true; break;
                case "alt": Alt = true; break;
                default:
                    if (Enum.TryParse<KeyboardKey>(segment, out var key))
                        Key = key;
                    break;
            }
        }
    }

    /// <summary>
    /// Deserialized a string to a keybind;
    /// does not throw at failure and returns an incomplete keybind instead.
    /// </summary>
    /// <param name="expression">Examples: "Ctrl+Shift+Z", "Shift+ A", "Alt + Three"</param>
    public static Keybind FromString(string expression)
    {
        var keybind = new Keybind(KeyboardKey.Null);

        if (string.IsNullOrWhiteSpace(expression)) return keybind;
        
        foreach (var segment in expression.Split('+').Select(s => s.Trim().ToLower()))
        {
            switch (segment)
            {
                case "ctrl": keybind.Ctrl = true; break;
                case "shift": keybind.Shift = true; break;
                case "alt": keybind.Alt = true; break;
                default:
                    if (Enum.TryParse<KeyboardKey>(segment, out var key))
                        keybind.Key = key;
                    break;
            }
        }
        
        return keybind;
    }

    public override string ToString() => "Keybind"
        + (Name is null ? "" : $"({Name})")
        + '{'
        + (Ctrl ? "Ctrl + " : "")
        + (Shift ? "Shift + " : "")
        + (Alt ? "Alt + " : "")
        + $"{Key}" 
        + '}';

    public bool Equals(Keybind? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Ctrl == other.Ctrl && Shift == other.Shift && Alt == other.Alt && Key == other.Key;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == GetType() && Equals((Keybind)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Ctrl, Shift, Alt, (int)Key);
    }
}

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
public class ViewKeybinds
{
    // public readonly Keybind NavigateToStartView = new(KeyboardKey.Zero)
    // {
    //     Name = "Navigate to start view",
    //     Group = "Navigation"
    // };
    // public readonly Keybind NavigateToGeosView = new(KeyboardKey.One)
    // {
    //     Name = "Navigate to geos view",
    //     Group = "Navigation"
    // };
    // public readonly Keybind NavigateToTilesView = new(KeyboardKey.Two)
    // {
    //     Name = "Navigate to tiles view",
    //     Group = "Navigation"
    // };
    // public readonly Keybind NavigateToConnectionsView = new(KeyboardKey.Three)
    // {
    //     Name = "Navigate to connections view",
    //     Group = "Navigation"
    // };
    // public readonly Keybind NavigateToCamerasView = new(KeyboardKey.Four)
    // {
    //     Name = "Navigate to cameras view",
    //     Group = "Navigation"
    // };
    // public readonly Keybind NavigateToLightView = new(KeyboardKey.Five)
    // {
    //     Name = "Navigate to lights view",
    //     Group = "Navigation"
    // };
    // public readonly Keybind NavigateToDimensionsView = new(KeyboardKey.Six)
    // {
    //     Name = "Navigate to dimensions view",
    //     Group = "Navigation"
    // };
    // public readonly Keybind NavigateToEffectsView = new(KeyboardKey.Seven)
    // {
    //     Name = "Navigate to effects view",
    //     Group = "Navigation"
    // };
    // public readonly Keybind NavigateToPropsView = new(KeyboardKey.Eight)
    // {
    //     Name = "Navigate to props view",
    //     Group = "Navigation"
    // };
    // public readonly Keybind NavigateToRenderView = new(KeyboardKey.Nine)
    // {
    //     Name = "Navigate to render view",
    //     Group = "Navigation"
    // };

    public virtual void FromIni(IniParser.Model.KeyDataCollection data)
    {
        foreach (var keybindProp in 
                 GetType()
                     .GetFields()
                     .Where(p => p.DeclaringType == typeof(Keybind))
                ) {
            var name = keybindProp.Name;
            if (!data.ContainsKey(name)) continue;
            var keybind = (Keybind)keybindProp.GetValue(this)!;
            keybind.Deserialize(data[name]);
        }
    }
    
    public ViewKeybinds() {}
    public ViewKeybinds(IniParser.Model.KeyDataCollection data)
    {
        foreach (var keybindProp in 
                 GetType()
                     .GetFields()
                     .Where(p => p.DeclaringType == typeof(Keybind))
                 ) {
            var name = keybindProp.Name;
            if (!data.ContainsKey(name)) continue;
            var keybind = (Keybind)keybindProp.GetValue(this)!;
            keybind.Deserialize(data[name]);
        }
    }
}

public class GeosKeybinds : ViewKeybinds
{
    public GeosKeybinds() : base() {}
    public GeosKeybinds(IniParser.Model.KeyDataCollection data) : base(data) {}
}

public class TilesKeybinds : ViewKeybinds
{
    public TilesKeybinds() : base() {}
    public TilesKeybinds(IniParser.Model.KeyDataCollection data) : base(data) {}
}