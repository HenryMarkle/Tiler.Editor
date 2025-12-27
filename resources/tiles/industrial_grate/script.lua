local texture = Image('texture.png')

function Render(x, y, z)
    local geo = Level.Geos:StrAt(x, y, z)

    if geo == Solid then
        Draw(texture, z * 10 + 1, Rect(x * 20, y * 20, 20, 20), Rect(x * 20, y * 20, 20, 20))
    elseif geo == Platform then
        Draw(texture, z * 10 + 1, Rect(x * 20, y * 20, 20, 10), Rect(x * 20, y * 20, 20, 10))
    end
end