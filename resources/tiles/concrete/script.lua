local DG = require('DualGridTiles')

local texture = Image('texture.png')
local borders = Image('borders.png')

function Render(x, y, z)
    for l = 0, 9 do
        Draw(
            texture,
            z*10 + l,
            Rect(x * 20, y * 20, 20, 20),
            Rect((x * 20 % texture.Width), (y * 20 % texture.Height), 20, 20)
        )
    end

    DG.Render(borders, x, y, z)
end