local ST = require('SixteenTiles')

local tileset = Image('tileset.png')
local frame = Image('frame.png')

function Render(x, y, z)
    for l = 0, 4 do
        if l == 2 then
            Draw(frame, z * 10 + l, Rect(x * 20, y * 20, 20, 20))
        else
            ST.Render(tileset, x, y, z, z * 10 + l, { variations = 4, solidConnectChance = 30 })
        end
    end
end
