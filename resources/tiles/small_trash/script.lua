local ST = require('SixteenTiles')

local tileset = Image('tileset.png')
local img = Image('variations.png')

local colors = { Color(255, 0, 0), Color(0, 255, 0), Color(0, 0, 255) }

function Render(x, y, z)
    

    if y - 1 >= 0 and Level.Geos:StrAt(x, y-1, z) == Air then
        for l = 0, 9 do
            local quad = Quad(
                Point(x * 20, y * 20),
                Point(x * 20 + 20, y * 20),
                Point(x * 20 + 20, y * 20 + 20),
                Point(x * 20, y * 20 + 20)
            )
    
            local magnitude = math.random(13)
            local degree = math.random(360)
            local offset = Point(
                magnitude * math.cos(degree * math.pi / 180),
                magnitude * math.sin(degree * math.pi / 180)
            )
    
            local rotation = math.random(360)
    
            quad = quad:Rotate(rotation)
            quad = quad + offset
    
            local variation = math.random(48) - 1
    
            local colorIndex = math.random(3)
    
            Draw(img, z * 10 + l, quad, Rect(0, variation * 50, 50, 50), colors[colorIndex])
        end
    else
        for l = 0, 9 do
            ST.Render(tileset, x, y, z, z * 10 + l, { variations = 4, buffer = 1, solidConnectChance = 20 })
        end
    end
end