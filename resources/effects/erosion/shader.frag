#version 330

in vec2 fragTexCoord;
in vec4 fragColor;

uniform sampler2D texture0;
uniform float strength;
uniform float a;
uniform float b;
uniform float c;
uniform float d;

out vec4 finalColor;

float randOffset() {
    return ((cos(a*fragTexCoord.x + b*sin(c*fragTexCoord.x+d)) - 1) / 2) * strength * 3;
}

void main() {
    float offset = randomOffset() / 26.0;

    finalColor = texture(texture0, vec2(fragTexCoord.x, clamp(fragTexCoord.y + offset, 0.01, 0.99)));
}