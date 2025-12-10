local B = require('BasicTiles')

local tileset = Image('tileset.png')

function Render(x, y, z)
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
end