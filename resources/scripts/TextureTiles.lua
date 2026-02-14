local M = {}

-- Creates the shape appropriate to the geo tile 
local function triTile(geo, x, y)
    if geo == Solid then
        return Rect(x * 20, y * 20, 20, 20)
    elseif geo == Platform then
        return Rect(x * 20, y * 20, 20, 10)
    elseif geo == Slab then
        return Rect(x * 20, y * 20 + 10, 20, 10)
    elseif geo == SlopeNW then
        return Triangle(
            Point(x * 20 + 20, y * 20), 
            Point(x * 20, y * 20 + 20),
            Point(x * 20 + 20, y * 20 + 20))
    elseif geo == SlopeNE then
        return Triangle(
            Point(x * 20, y * 20), 
            Point(x * 20, y * 20 + 20),
            Point(x * 20 + 20, y * 20 + 20))
    elseif geo == SlopeSW then
        return Triangle(
            Point(x * 20 + 20, y * 20 + 20), 
            Point(x * 20 + 20, y * 20),
            Point(x * 20, y * 20))

    elseif geo == SlopeSE then
        return Triangle(
            Point(x * 20, y * 20), 
            Point(x * 20, y * 20 + 20),
            Point(x * 20 + 20, y * 20))      
    else
        return nil
    end
end

function M.Render(texture, x, y, z, opts)
    local geo = Level.Geos:StrAt(x, y, z)
    
    local dest = triTile(geo, x, y)
    local source = triTile(geo, x, y)
    
    if dest == nil or source == nil then
        return
    end

    if not opts then
        for l = 0, 9 do
            Draw(
                texture,
                z*10 + l,
                dest,
                source
            )
        end
    else
        for l = opts.startLayer, opts.endLayer do
            Draw(
                texture,
                l,
                dest,
                source
            )
        end
    end
end

return M