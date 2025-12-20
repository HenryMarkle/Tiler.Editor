using System.Collections.Generic;

namespace Tiler.Editor;

public enum ConnectionType
{
    None,
    Exit,
    Spawn,
    Shortcut
}

public class Connection(ConnectionType type, IEnumerable<(int x, int y)> path)
{
    public ConnectionType Type = type;

    public List<(int x, int y)> Path = [.. path];
}