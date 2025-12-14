local M = {}

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

---@param tileset RLImage
---@param x integer
---@param y integer
---@param z integer
---@param layer integer?
---@param options { variations: integer?, buffer: integer?, solidConnectChance: number? }?
function M.Render(tileset, x, y, z, layer, options)
    -- TODO: compute at load time
    local buffer = (options or {}).buffer or 0
    local variations = (options or {}).variations or 1
    local solidConnectChance = (options or {}).solidConnectChance or 0

    local dest = Rect(x * 20, y * 20, 20, 20)

    if Level.Geos:StrAt(x, y, z) == Solid then
        local left
        local top
        local right
        local bottom

        if x - 1 < 0 then 
            left = true 
        else
            left = Level.Geos:StrAt(x - 1, y, z) == Solid and (Level.Tiles:At(x - 1, y, z) == Tile or math.random(100) < solidConnectChance);
        end
        if y - 1 < 0 then
            top = true
        else
            top = Level.Geos:StrAt(x, y - 1, z) == Solid and (Level.Tiles:At(x, y - 1, z) == Tile or math.random(100) < solidConnectChance);
        end
        if x + 1 >= Level.Width then
            right = true
        else
            right = Level.Geos:StrAt(x + 1, y, z) == Solid and (Level.Tiles:At(x + 1, y, z) == Tile or math.random(100) < solidConnectChance);
        end
        if y + 1 >= Level.Height then
            bottom = true
        else
            bottom = Level.Geos:StrAt(x, y + 1, z) == Solid and (Level.Tiles:At(x, y + 1, z) == Tile or math.random(100) < solidConnectChance);
        end

        local product = 1

        if left   then product = product * 2 end
        if top    then product = product * 3 end
        if right  then product = product * 5 end
        if bottom then product = product * 7 end
        
        ---@type RLRect|integer
        local source = Rect(mapping[tostring(product)])

        local variation = math.random(variations) - 1
        
        source.Y = variation * 20

        if buffer > 0 then
            source.X = source.X * (buffer * 3)
            source.Y = source.Y * (buffer * 3)
            source.Width = source.Width * (buffer * 3)
            source.Height = source.Height * (buffer * 3)
        
            dest.X = dest.X - (buffer * 20)
            dest.Y = dest.Y - (buffer * 20)
            dest.Width = dest.Width * (buffer * 3)
            dest.Height = dest.Height * (buffer * 3)
        end

        Draw(tileset, layer or z * 10, dest, source)
    else
        ---@type RLRect|integer
        local source = mapping[Level.Geos:StrAt(x, y, z)]
        
        local variation = math.random(variations)
        
        source.Y = (variation - 1) * 20

        if buffer > 0 then
            source.X = source.X * (buffer * 3)
            source.Y = source.Y * (buffer * 3)
            source.Width = source.Width * (buffer * 3)
            source.Height = source.Height * (buffer * 3)
        
            dest.X = dest.X - (buffer * 20)
            dest.Y = dest.Y - (buffer * 20)
            dest.Width = dest.Width * (buffer * 3)
            dest.Height = dest.Height * (buffer * 3)
        end
        
        Draw(tileset, layer or z * 10, dest, source)
    end
end

return M