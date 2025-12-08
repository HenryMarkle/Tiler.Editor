local v = Point(0, 0)
local q = Quad(Point(0, 0), Point(10, 0), Point(10, 10), Point(0, 10))

print(q:Rotate(100))

function Render(x, y, z)
    print('' .. x .. ' ' .. y .. ' ' .. z)
end
