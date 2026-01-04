local blob = Image('blob.png')

local function connects(x, y)
    return x >= 0 and
        x < Level.Width and
        y >= 0 and
        y < Level.Height and
        Level.Geos:StrAt(x, y, 0) == Solid and 
        Effect.Matrix:At(x, y, 0) > 0
end

local random = math.random
local source = Rect(0, 0, blob.Width, blob.Height)

function Render()
    for x = StartX, StartX + Columns do
        for y = StartY, StartY + Rows do
            if connects(x, y) then
                local addX = random(4) - 2
                local addY = random(4) - 2

                local back = Rect(x * 20 + 5, y * 20 + 5, 10, 10)

                if (connects(x - 1, y)) then
                    back.X = back.X - 13
                    back.Width = back.Width + 13
                end

                if (connects(x, y - 1)) then
                    back.Y = back.Y - 13
                    back.Height = back.Height + 13
                end

                if (connects(x + 1, y)) then
                    back.Width = back.Width + 13
                end

                if (connects(x, y + 1)) then
                    back.Height = back.Height + 13
                end
            
                Draw({
                    layer = 0,
                    texture = blob,
                    source = source,
                    dest = back,
                    tint = Color(0, 255, 0, 255)
                })
            end
        end
    end

    for x = StartX, StartX + Columns do
        for y = StartY, StartY + Rows do
            if connects(x, y) then
                local addX
                local addY

                local front = Rect(x * 20 + 8, y * 20 + 8, 4, 4)
                
                if (connects(x - 1, y)) then
                    front.X = front.X - 13
                    front.Width = front.Width + 13
                else
                    addX = random(6) - 0
                    front.X = front.X + addX
                    front.Width = front.Width - addX
                end

                if (connects(x, y - 1)) then
                    front.Y = front.Y - 13
                    front.Height = front.Height + 13
                else
                    addY = random(6) - 0
                    front.Y = front.Y + addY
                    front.Height = front.Height - addY
                end

                if (connects(x + 1, y)) then
                    front.Width = front.Width + 13
                else
                    addX = random(6) - 0
                    front.Width = front.Width - addX
                end

                if (connects(x, y + 1)) then
                    front.Height = front.Height + 13
                else
                    addY = random(6) - 0
                    front.Height = front.Height - addY
                end
                
                Draw({
                    texture = blob,
                    layer = 0,
                    dest = front,
                    source = source,
                    tint = Color(255, 0, 0, 255)
                })
            end
        end
    end
end