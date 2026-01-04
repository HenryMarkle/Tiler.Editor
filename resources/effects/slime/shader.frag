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
    
    // Calculate number of drips based on strength (0-1)
    int maxDrips = int(strength * 20.0); // 0 to 20 drips
    
    // Generate multiple drips
    for (int i = 0; i < maxDrips; i++) {
        // Unique seed for each drip
        vec2 dripSeed = tileSeed + vec2(float(i) * 0.137, float(i) * 0.491);
        
        // Random source pixel for this drip
        float randX = random(dripSeed + vec2(0.123, 0.456));
        float randY = random(dripSeed + vec2(0.789, 0.321));
        ivec2 sourcePixel = ivec2(randX * 26.0, randY * 26.0);
        
        // Random drip length (3-8 pixels)
        int dripLength = int(3.0 + random(dripSeed + vec2(0.999, 0.111)) * 5.0);
        
        // Check if current pixel is part of this drip
        if (currentPixel.x == sourcePixel.x && 
            currentPixel.y >= sourcePixel.y && 
            currentPixel.y < sourcePixel.y + dripLength) {
            shouldDuplicate = true;
            sourceCoord = (vec2(sourcePixel) + 0.5) * onePixel;
            break; // Found a drip, no need to check more
        }
    }
    
    if (shouldDuplicate) {
        vec4 duplicatedColor = texture(texture0, sourceCoord);
        finalColor = duplicatedColor;
    } else {
        finalColor = baseColor;
    }
}