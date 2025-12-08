---@class Image
---@field width number
---@field height number
Image = {}

---@class Point
---@field x number
---@field y number
Point = {}

---@class Rectangle
---@field x number
---@field y number
---@field width number
---@field height number
Rectangle = {}

---@class Quad
---@field topleft Point
---@field topright Point
---@field bottomright Point
---@field bottomleft Point
Rectangle = {}

---@class Color
---@field r number
---@field g number
---@field b number
Color = {}

---@param path string
---@return Image
local function image(path) return {} end

---@param x number
---@param y number
---@return Point
local function point(x, y) return {} end

---@param x number
---@param y number
---@param width number
---@param height number
---@return Rectangle
local function rect(x, y, width, height) return {} end

---@param topleft Point
---@param topright Point
---@param bottomright Point
---@param bottomleft Point
---@return Quad
local function quad(topleft, topright, bottomright, bottomleft) return {} end

---@param rectOrQuad Rectangle|Quad
---@param degree number
---@return Quad
local function rotate(rectOrQuad, degree) return {} end

---@param text string
local function log(text) end

---@param image Image
---@param layer number
---@param source Rectangle?
---@param dest Rectangle|Quad|Point
local function drawi(image, layer, dest, source) end

---@param color Color
---@param layer number
---@param dest Rectangle
local function drawr(color, layer, dest) end

---@class Level
---@field width number
---@field height number
---@field depth number
---@field geos number[][][]
---@field tiles string[][][]
Level = {}

Air = 0
Solid = 1
Wall = 2
SlopeNW = 3
SlopeNE = 4
SlopeSE = 5
SlopeSW = 6
Platform = 7
Slab = 8
Glass = 9
Exit = 10
VerticalPole = 11
HorizontalPole = 12
CrossPole = 13

local mapping = {}

table.insert(mapping,   1, rect(0      , 0, 20, 20))
table.insert(mapping,  21, rect(1  * 20, 0, 20, 20))
table.insert(mapping,  10, rect(2  * 20, 0, 20, 20))
table.insert(mapping, 210, rect(3  * 20, 0, 20, 20))
table.insert(mapping, 105, rect(4  * 20, 0, 20, 20))
table.insert(mapping,  70, rect(5  * 20, 0, 20, 20))
table.insert(mapping,  42, rect(6  * 20, 0, 20, 20))
table.insert(mapping,  30, rect(7  * 20, 0, 20, 20))
table.insert(mapping,  35, rect(8  * 20, 0, 20, 20))
table.insert(mapping,  14, rect(9  * 20, 0, 20, 20))
table.insert(mapping,   6, rect(10 * 20, 0, 20, 20))
table.insert(mapping,  15, rect(11 * 20, 0, 20, 20))
table.insert(mapping,   2, rect(12 * 20, 0, 20, 20))
table.insert(mapping,   3, rect(13 * 20, 0, 20, 20))
table.insert(mapping,   5, rect(14 * 20, 0, 20, 20))
table.insert(mapping,   7, rect(15 * 20, 0, 20, 20))

table.insert(mapping, Platform * 1000, 16)
table.insert(mapping, Slab     * 1000, 17)
table.insert(mapping, SlopeNW  * 1000, 18)
table.insert(mapping, SlopeNE  * 1000, 19)
table.insert(mapping, SlopeSE  * 1000, 20)
table.insert(mapping, SlopeSW  * 1000, 21)

local tileset = image("tileset.png")

---@param x number
---@param y number
---@param layer number
---@param level Level
function Render(x, y, layer)
  -- [    ][    ][    ]
  -- [    ][HERE][    ]
  -- [    ][    ][    ]

  local cell = level.geos[2][2][layer]

  if cell == Solid then
    local left   = level.geos[1][2][layer] == cell -- 2
    local top    = level.geos[2][1][layer] == cell -- 3
    local right  = level.geos[3][2][layer] == cell -- 5
    local bottom = level.geos[2][3][layer] == cell -- 7

    local product = 1

    if left   then product = product * 2 end
    if top    then product = product * 3 end
    if right  then product = product * 5 end
    if bottom then product = product * 7 end

    -- products mapped to indices: 1 21 10 210 105 70 42 30 35 14 6 15 2 3 5 7

    local source = mapping[product]

    local variation = math.random(4)

    source.y = (variation - 1) * 20

    drawi(tileset, layer, point(x * 20, y * 20), source)
  else
    drawi(tileset, layer, point(x * 20, y * 20), rect(mapping[cell * 1000] * 20, (math.random(4) - 1) * 20, 20, 20))
  end
end

