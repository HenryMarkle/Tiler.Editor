local DG = require('DualGridTiles')

local texture = Image('texture.png')
local borders = Image('borders.png')

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

    for l = 0, 9 do
        Draw(
            texture,
            z*10 + l,
            dest,
            source
        )
    end

    DG.Render(borders, x, y, z)
end