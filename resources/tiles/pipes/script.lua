tileset = Image('tileset.png')
frame = Image('frame.png')

local mapping = {
    ["1"]   = Rect(0  * 20, 0, 20, 20),
    ["21"]  = Rect(1  * 20, 0, 20, 20),
    ["10"]  = Rect(2  * 20, 0, 20, 20),
    ["210"] = Rect(3  * 20, 0, 20, 20),
    ["105"] = Rect(4  * 20, 0, 20, 20),
    ["70"]  = Rect(5  * 20, 0, 20, 20),
    ["42"]  = Rect(6  * 20, 0, 20, 20),
    ["30"]  = Rect(7  * 20, 0, 20, 20),
    ["35"]  = Rect(8  * 20, 0, 20, 20),
    ["14"]  = Rect(9  * 20, 0, 20, 20),
    ["6"]   = Rect(10 * 20, 0, 20, 20),
    ["15"]  = Rect(11 * 20, 0, 20, 20),
    ["2"]   = Rect(12 * 20, 0, 20, 20),
    ["3"]   = Rect(13 * 20, 0, 20, 20),
    ["5"]   = Rect(14 * 20, 0, 20, 20),
    ["7"]   = Rect(15 * 20, 0, 20, 20),

    ["Platform"] = 16,
    ["Slab"] = 17,
    ["SlopeNW"] = 18,
    ["SlopeNE"] = 19,
    ["SlopeSE"] = 20,
    ["SlopeSW"] = 21
}

function Render(x, y, z)
    if Level.Geos:StrAt(x, y, z) == Solid then
        local left
        local top
        local right
        local bottom

        if x - 1 < 0 then 
            left = true 
        else
            left = Level.Geos:StrAt(x - 1, y, z) == Solid and Level.Tiles:At(x - 1, y, z) == Tile;
        end
        if y - 1 < 0 then
            top = true
        else
            top = Level.Geos:StrAt(x, y - 1, z) == Solid and Level.Tiles:At(x, y - 1, z) == Tile;
        end
        if x + 1 >= Level.Width then
            right = true
        else
            right = Level.Geos:StrAt(x + 1, y, z) == Solid and Level.Tiles:At(x + 1, y, z) == Tile;
        end
        if y + 1 >= Level.Height then
            bottom = true
        else
            bottom = Level.Geos:StrAt(x, y + 1, z) == Solid and Level.Tiles:At(x, y + 1, z) == Tile;
        end

        local product = 1

        if left   then product = product * 2 end
        if top    then product = product * 3 end
        if right  then product = product * 5 end
        if bottom then product = product * 7 end
        
        ---@type RLRect|integer
        local source = mapping[tostring(product)]

        for l = 0, 4 do
            if l == 2 then
                Draw(frame, l, Rect(x * 20, y * 20, 20, 20))
            else
                local variation = math.random(4)
        
                source.Y = (variation - 1) * 20
        
                Draw(tileset, l, Rect(x * 20, y * 20, 20, 20), source)
            end
        end
    else
        ---@type RLRect|integer
        local source = mapping[Level.Geos:StrAt(x, y, z)]
        
        for l = 0, 4 do
            if l == 2 then
                Draw(frame, l, Rect(x * 20, y * 20, 20, 20))
            else
                local variation = math.random(4)
        
                source.Y = (variation - 1) * 20
        
                Draw(tileset, l, Rect(x * 20, y * 20, 20, 20), source)
            end
        end
    end
end
