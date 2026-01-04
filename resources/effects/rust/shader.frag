// To be honest, I used Claude AI to write this.
// Ain't wasting my time with shader code.

#version 330
in vec2 fragTexCoord;
in vec4 fragColor;
uniform sampler2D texture0;
uniform float strength;
uniform int x;
uniform int y;
out vec4 finalColor;

float random(vec2 seed) {
    return fract(sin(dot(seed, vec2(12.9898, 78.233))) * 43758.5453123);
}

void main() {
    vec2 onePixel = 1 / vec2(26.0, 26.0);
    
    // Convert current fragment's texture coordinate to pixel coordinates (0-25)
    ivec2 currentPixel = ivec2(fragTexCoord * 26.0);
    
    // Sample the base color at current pixel
    vec4 baseColor = texture(texture0, fragTexCoord);
    
    // Use tile coordinates (x, y) as random seed
    vec2 tileSeed = vec2(float(x), float(y));
    
    bool shouldDuplicate = false;
    vec2 sourceCoord;
    
    // Calculate number of puddles based on strength (0-1)
    int maxPuddles = int(strength * 15.0); // 0 to 15 puddles
    
    // Generate multiple puddles
    for (int i = 0; i < maxPuddles; i++) {
        // Unique seed for each puddle
        vec2 puddleSeed = tileSeed + vec2(float(i) * 0.137, float(i) * 0.491);
        
        // Random center pixel for this puddle
        float randX = random(puddleSeed + vec2(0.123, 0.456));
        float randY = random(puddleSeed + vec2(0.789, 0.321));
        vec2 puddleCenter = vec2(randX * 26.0, randY * 26.0);
        
        // Puddle radius based on strength: 0.5 to 3.0 pixels
        float puddleRadius = 0.5 + random(puddleSeed + vec2(0.555, 0.777)) * (0.5 + strength * 2.5);
        puddleRadius = clamp(puddleRadius, 0.5, 3.0);
        
        // Calculate distance from current pixel to puddle center
        float dist = distance(vec2(currentPixel), puddleCenter);
        
        // Check if current pixel is within the circular puddle
        if (dist <= puddleRadius) {
            shouldDuplicate = true;
            // Use the puddle center as source
            sourceCoord = (puddleCenter + 0.5) / 26.0;
            break; // Found a puddle, no need to check more
        }
    }
    
    if (shouldDuplicate) {
        vec4 duplicatedColor = texture(texture0, sourceCoord);
        finalColor = duplicatedColor;
    } else {
        finalColor = baseColor;
    }
}