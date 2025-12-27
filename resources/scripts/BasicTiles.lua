local M = {}

local mapping = {
    [Solid] = 0,
    [Platform] = 1,
    [Slab] = 2,
    [SlopeNW] = 3,
    [SlopeNE] = 4,
    [SlopeSE] = 5,
    [SlopeSW] = 6,
}

---@param tileset RLImage
---@param x integer
---@param y integer
---@param z integer
---@param layer integer?
---@param options table?
function M.Render(tileset, x, y, z, layer, options)
    local geo = Level.Geos:StrAt(x, y, z)

    if not mapping[geo] then return end

    local buffer = (options or {}).buffer or 0
    local variations = (options or {}).variations or 1

    Draw(
        tileset,
        layer or z * 10,
        Rect((x - buffer) * 20, (y - buffer) * 20, 20 + (buffer*2*20), 20 + (buffer*2*20)),
        Rect((mapping[geo] * buffer * 3) * 20, (math.random(variations)-1) * (buffer * 3 * 20), 20 + buffer*2*20, 20 + buffer*2*20)
    )
end

return M