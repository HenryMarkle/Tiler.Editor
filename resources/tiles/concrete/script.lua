local T = require('TextureTiles')
local DG = require('DualGridTiles')

local texture = Image('texture.png')
local borders = Image('borders.png')

function Render(x, y, z)
    T.Render(texture, x, y, z)
    DG.Render(borders, x, y, z)
end