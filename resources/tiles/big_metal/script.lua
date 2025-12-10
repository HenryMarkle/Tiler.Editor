local DG = require('DualGridTiles')
local B = require('BasicTiles')

local tileset = Image('tileset.png')
local base = Image('base.png')

function Render(x, y, z)
    DG.Render(tileset, x, y, z)

    for l = 1, 9 do
        B.Render(base, x, y, z, z * 10 + l)
    end
end