graphic = Image('texture.png')
base = Image('base.png')

local mapping = {
    ['Solid'] = 0,
    ['Platform'] = 1,
    ['Slab'] = 2,
    ['SlopeNW'] = 3,
    ['SlopeNE'] = 4,
    ['SlopeSE'] = 5,
    ['SlopeSW'] = 6,
}

function Render(x, y, z)
    local geo = Level.Geos:StrAt(x, y, z)

    local dest
    local source

    if geo == Solid then
        dest = Rect(x * 20, y * 20, 20, 20)
        source = Rect(x * 20, y * 20, 20, 20)
    elseif geo == Platform then
        dest = Rect(x * 20, y * 20, 20, 10)
        source = Rect(x * 20, y * 20, 20, 10)
    elseif geo == Slab then
        dest = Rect(x * 20, y * 20 + 10, 20, 10)
        source = Rect(x * 20, y * 20 + 10, 20, 10)
    end

    Draw(graphic, z * 10, dest, source)

    local geo = Level.Geos:StrAt(x, y, z)
    local posx = mapping[geo]

    if not posx then return end
    
    for l = 1, 9 do
        Draw(base, (z*10)+l, Rect(x * 20, y * 20, 20, 20), Rect(posx * 20, 0, 20, 20))
    end
end