-- Warning: second uniform textures do not work (for some reason)

local shader = Shader('shader.frag')

local layer = RenderTexture(26, 26)

function Render(x, y)
    local strength = Effect.Matrix:At(x, y)
    
    if strength > 0 then
        for l = 0, 49 do
            layer:Clear()
            
            DrawOn({
                rt = layer,
                texture = Layers[l].Texture,
                dest = Rect(0, 0, 26, 26),
                source = Rect(x * 20 + Margin - Camera.Position.X - 3, y * 20 + Margin - Camera.Position.Y - 3, 26, 26)
            })
    
            Draw({
                layer = l,
                texture = layer.Texture,
                source = Rect(0, 0, 26, 26),
                dest = Rect(x * 20 - 3, y * 20 - 3, 26, 26),
                shader = shader,
                shaderValues = {
                    texture0 = layer,
                    strength = strength,
                    a = math.random() * 20 - 10,
                    b = math.random() * 20 - 10,
                    c = math.random() * 20 - 10,
                    d = math.random() * 20 - 10
                },
                alphaBlend = false
            })
        end
    end
end