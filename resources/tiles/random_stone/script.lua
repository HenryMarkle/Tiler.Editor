local B = require('BasicTiles')

local tileset = Image('tileset.png')
local largeVars = Image('large_variants.png')

local auxMtx = {}

for x = 1, Level.Width do
    table.insert(auxMtx, {})

    for y = 1, Level.Height do
        table.insert(auxMtx[x], {})

        for z = 1, 5 do
            table.insert(
                auxMtx[x][y],
                (Level.Geos:StrAt(x-1,y-1,z-1) == Solid or Level.Geos:StrAt(x-1,y-1,z-1) ~= Air)
                and (Level.Tiles:At(x-1,y-1,z-1) == Tile or Level.DefaultTile == Tile)
            )
        end
    end
end

local function fits(x, y, z, size)
    if size == 0 then return false end

    for w = 0, size - 1 do
        for h = 0, size - 1 do
            if (not auxMtx[x + 1 + w][y + 1 + h][z + 1]) then
                return false

            end
        end
    end

    return true
end

local function set(x, y, z, size)
    if size == 0 then return end

    for w = 1, size do
        for h = 1, size do
            auxMtx[x + w][y + h][z + 1] = false
        end
    end
end

function Render(x, y, z)
    if fits(x, y, z, 2) and math.random(2) == 2 then
        for l = 0, 9 do
            Draw(
                largeVars,
                z * 10 + l,
                Rect(x*20 - 20, y*20 - 20, 80, 80),
                Rect(0, (math.random(3) - 1) * 80, 80, 80)
            )
        end

        set(x, y, z, 2)
    elseif fits(x, y, z, 1) then
        for l = 0, 9 do
            B.Render(
                tileset,
                x,
                y,
                z,
                z * 10 + l,
                {
                    buffer = 1,
                    variations = 4,
                }
            )
        end

        set(x, y, z, 1)
    end
end