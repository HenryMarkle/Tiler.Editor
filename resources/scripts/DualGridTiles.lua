local M = {}

local topleft = 1
local topright = 2
local bottomright = 3
local bottomleft = 4

local quarters = { Point(0, 0), Point(1, 0), Point(1, 1), Point(0, 1) }
local offsets = { Point(-1, -1), Point(0, -1), Point(0, 0), Point(-1, 0) }
local primes = { 2, 3, 5, 7 }

local solidMap = {
    { -- topleft
        ['21'] = 0,
        [ '7'] = 1,
        ['14'] = 1,
        [ '3'] = 2,
        [ '6'] = 2,
        [ '1'] = 3,
        [ '2'] = 3,
        ['42'] = 4
    },
    { -- topright
        ['10'] = 0,
        [ '5'] = 1,
        ['15'] = 1,
        [ '2'] = 2,
        [ '6'] = 2,
        [ '1'] = 3,
        [ '3'] = 3,
        ['42'] = 4,
        ['30'] = 4
    },
    { -- bottomright
        [ '21'] = 0,
        [ '35'] = 1,
        [  '7'] = 1,
        [  '3'] = 2,
        [ '15'] = 2,
        [  '5'] = 3,
        [  '1'] = 3,
        ['105'] = 4
    },
    { -- bottomleft
        ['10'] = 0,
        ['35'] = 1,
        [ '5'] = 1,
        [ '2'] = 2,
        ['14'] = 2,
        [ '1'] = 3,
        [ '7'] = 3,
        ['70'] = 4
    },
}

local yCornerOrder = { 0, 1, 3, 2 }

---@param tileset RLImage
---@param x integer
---@param y integer
---@param z integer
function M.Render(tileset, x, y, z)
    local geo = Level.Geos:StrAt(x, y, z)

    if geo == Solid then
        local pos = Point(x, y)

        for q = topleft, bottomleft do
            local product = 1

            for p = topleft, bottomleft do
                if p == bottomright and q == topleft then
                elseif p == bottomleft and q == topright then
                elseif p == topleft and q == bottomright then
                elseif p == topright and q == bottomleft then
                else
                    local offsetPos = pos + quarters[q] + offsets[p]

                    if Level.Geos:IsInBounds(offsetPos) and
                        Level.Geos:StrAt(offsetPos.X, offsetPos.Y, z) == Solid and
                        Level.Tiles:At(offsetPos.X, offsetPos.Y, z) == Tile then

                        product = product * primes[p]
                    end
                end
            end

            local ycorner = yCornerOrder[q]
            local xcorner = solidMap[q][tostring(product)]

            local destination = Rect((x * 20) + (quarters[q].X * 10), (y * 20) + (quarters[q].Y * 10), 10, 10)
            local source = Rect(ycorner * 10, xcorner * 10, 10, 10)

            Draw(
                tileset,
                z * 10,
                destination,
                source
            )
        end
    elseif geo == SlopeNW then
        Draw(
            tileset,
            z * 10,
            Rect(x * 20 + 10, y * 20, 10, 10),
            Rect(50, 0, 10, 10)
        )
        Draw(
            tileset,
            z * 10,
            Rect(x * 20, y * 20 + 10, 10, 10),
            Rect(50, 0, 10, 10)
        )
        Draw(
            tileset,
            z * 10,
            Rect(x * 20 + 10, y * 20 + 10, 10, 10),
            Rect(50, 40, 10, 10)
        )
    elseif geo == SlopeNE then
        Draw(
            tileset,
            z * 10,
            Rect(x * 20, y * 20, 10, 10),
            Rect(50, 10, 10, 10)
        )
        Draw(
            tileset,
            z * 10,
            Rect(x * 20 + 10, y * 20 + 10, 10, 10),
            Rect(50, 10, 10, 10)
        )
        Draw(
            tileset,
            z * 10,
            Rect(x * 20, y * 20 + 10, 10, 10),
            Rect(50, 40, 10, 10)
        )
    elseif geo == SlopeSE then
        Draw(
            tileset,
            z * 10,
            Rect(x * 20 + 10, y * 20, 10, 10),
            Rect(50, 20, 10, 10)
        )
        Draw(
            tileset,
            z * 10,
            Rect(x * 20, y * 20 + 10, 10, 10),
            Rect(50, 20, 10, 10)
        )
        Draw(
            tileset,
            z * 10,
            Rect(x * 20, y * 20, 10, 10),
            Rect(50, 40, 10, 10)
        )
    elseif geo == SlopeSW then
        Draw(
            tileset,
            z * 10,
            Rect(x * 20, y * 20, 10, 10),
            Rect(50, 30, 10, 10)
        )
        Draw(
            tileset,
            z * 10,
            Rect(x * 20 + 10, y * 20 + 10, 10, 10),
            Rect(50, 30, 10, 10)
        )
        Draw(
            tileset,
            z * 10,
            Rect(x * 20 + 10, y * 20, 10, 10),
            Rect(50, 40, 10, 10)
        )
    elseif geo == Platform then
        Draw(
            tileset,
            z * 10,
            Rect(x * 20, y * 20, 10, 10),
            Rect(40, 0, 10, 10)
        )
        Draw(
            tileset,
            z * 10,
            Rect(x * 20 + 10, y * 20, 10, 10),
            Rect(40, 10, 10, 10)
        )
    elseif geo == Slab then
        Draw(
            tileset,
            z * 10,
            Rect(x * 20, y * 20 + 10, 10, 10),
            Rect(40, 0, 10, 10)
        )
        Draw(
            tileset,
            z * 10,
            Rect(x * 20 + 10, y * 20 + 10, 10, 10),
            Rect(40, 10, 10, 10)
        )
    end
end

return M