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
    Draw(graphic, z, Rect(x * 20, y * 20, 20, 20))
    
    for l = 1, 9 do
        Draw(base, (z*10)+l, Rect(x * 20, y * 20, 20, 20), Rect(mapping[Level.Geos:StrAt(x, y, z)] * 20, 0, 20, 20))
    end
end