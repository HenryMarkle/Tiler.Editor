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
function M.Render(tileset, x, y, z, layer)
    local geo = Level.Geos:StrAt(x, y, z)

    if not mapping[geo] then return end

    Draw(
        tileset,
        layer or z * 10,
        Rect(x * 20, y * 20, 20, 20),
        Rect(mapping[geo] * 20, 0, 20, 20)
    )
end

return M