---@class RLImage
---@field Width integer
---@field Height integer
RLImage = {}

---@class RLRect
---@field X number
---@field Y number
---@field Width number
---@field Height number
RLRect = {}

---@class RLColor
---@field R integer 
---@field G integer
---@field B integer
---@field A integer
RLColor = {}

---@class RLPoint
---@field X number 
---@field Y number 
RLPoint = {}

---@class RLQuad
---@field TopLeft RLPoint
---@field TopRight RLPoint
---@field BottomRight RLPoint
---@field BottomLeft RLPoint
---@field Rotate function
RLQuad = {}

---Creates an image from a file path
---@param path string
---@return RLImage
function Image(path) return {} end

---@class Matrix
---@field Width integer
---@field Height integer
---@field At function
---@field StrAt function
---@field IsInBounds function
MatrixType = {}

---@class Level
---@field Geos Matrix
---@field Tiles Matrix
---@field Width integer
---@field Height integer
---@field Layers integer
LevelType = {}

---@type Level
Level = {}

---@param r integer
---@param g integer
---@param b integer
---@param a integer?
---@return RLColor
function Color(r, g, b, a) return {} end

---@param x number?
---@param y number?
---@return RLPoint
function Point(x, y) return {} end

---@param x number
---@param y number
---@param width number
---@param height number
---@return RLRect
function Rect(x, y, width, height) return {} end

---@param image RLImage
---@param layer integer
---@param dest RLQuad|RLRect
---@param source RLRect?
---@param tint RLColor?
function Draw(image, layer, dest, source, tint) end

---@param tl RLPoint?
---@param tr RLPoint?
---@param br RLPoint?
---@param bl RLPoint?
---@return RLQuad
function Quad(tl, tr, br, bl) return {} end