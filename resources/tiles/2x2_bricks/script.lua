graphic = Image('layers.png')

function Render(x, y, z)
    for l = 0, 3 do
        Draw(
            graphic,
            l + z,
            Rect(x * 20, y * 20, 20, 20),
            Rect((x % 2) * 20, l * 40 + (y % 2) * 20, 20, 20)
        )

        if l == 3 then
            for rep = 1, 7 do
                Draw(
                    graphic,
                    l + z + rep,
                    Rect(x * 20, y * 20, 20, 20),
                    Rect((x % 2) * 20, l * 40 + (y % 2) * 20, 20, 20)
                )
            end
        end
    end
end