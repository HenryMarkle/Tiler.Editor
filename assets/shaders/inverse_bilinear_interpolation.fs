// Inverse-Bilinear Interpolation fragment shader.
// This took me forever.

#version 330

in vec2 fragTexCoord;
in vec4 fragColor;

uniform vec2 vertex_pos[4];
uniform sampler2D texture0;

out vec4 finalColor;

float cross2d(vec2 a, vec2 b) {
    return a.x * b.y - a.y * b.x;
}

// https://people.csail.mit.edu/bkph/articles/Quadratics.pdf
vec2 invbilinear_robust(vec2 p, vec2 a, vec2 b, vec2 c, vec2 d) {
    vec2 k1 = c - d + a - b;
    float k2 = -4.0 * cross2d(k1, d - a);
    float k3 = cross2d(a - d, b - a);
    vec2 k4 = b - a;
    vec2 k5 = d - a;
    
    // Compute quadratic coefficients
    float b_coef = k3 - cross2d(k1, p - a);
    float c_coef = cross2d(p - a, k4);
    
    // Handle near-zero discriminant
    const float EPSILON = 1e-8;
    float discriminant = b_coef * b_coef + k2 * c_coef;
    
    if (discriminant < 0.0) {
        return vec2(-1.0);
    }
    
    float rad = sqrt(discriminant);
    
    // Avoid division by zero with numerically stable form
    float denom1 = b_coef + rad;
    float denom2 = b_coef - rad;
    
    // Choose the more stable solution
    float v1, v2;
    if (abs(denom1) > abs(denom2)) {
        v1 = -2.0 * c_coef / denom1;
        v2 = -2.0 * c_coef / denom2;
    } else {
        v2 = -2.0 * c_coef / denom2;
        v1 = -2.0 * c_coef / denom1;
    }
    
    // Compute u for both solutions using the stable dot product method
    vec2 e1 = p - a - k5 * v1;
    vec2 denom_vec1 = k4 + k1 * v1;
    float dot_e1 = dot(e1, e1);
    float dot_denom1 = dot(denom_vec1, e1);
    float u1 = (abs(dot_denom1) > EPSILON) ? dot_e1 / dot_denom1 : -1.0;
    
    vec2 e2 = p - a - k5 * v2;
    vec2 denom_vec2 = k4 + k1 * v2;
    float dot_e2 = dot(e2, e2);
    float dot_denom2 = dot(denom_vec2, e2);
    float u2 = (abs(dot_denom2) > EPSILON) ? dot_e2 / dot_denom2 : -1.0;
    
    // Choose the solution that's closer to [0,1] range
    float dist1 = max(abs(u1 - 0.5), abs(v1 - 0.5));
    float dist2 = max(abs(u2 - 0.5), abs(v2 - 0.5));
    
    return (dist1 <= dist2) ? vec2(u1, v1) : vec2(u2, v2);
}

// Alternative: Fallback iterative method for extreme cases
vec2 invbilinear_iterative(vec2 p, vec2 a, vec2 b, vec2 c, vec2 d) {
    vec2 uv = vec2(0.5, 0.5);
    
    const int MAX_ITERATIONS = 10;
    const float TOLERANCE = 1e-6;
    
    for (int i = 0; i < MAX_ITERATIONS; i++) {
        vec2 e = b - a;
        vec2 f = d - a;
        vec2 g = a - b + c - d;
        
        vec2 current = a + uv.x * e + uv.y * f + uv.x * uv.y * g;
        vec2 error = current - p;
        
        if (dot(error, error) < TOLERANCE * TOLERANCE) break;
        
        vec2 du = e + uv.y * g;
        vec2 dv = f + uv.x * g;
        
        float det = du.x * dv.y - du.y * dv.x;
        if (abs(det) < 1e-10) break;
        
        vec2 delta = vec2(
            (-error.x * dv.y + error.y * dv.x) / det,
            (error.x * du.y - error.y * du.x) / det
        );
        
        uv -= delta;
        uv = clamp(uv, vec2(0.0), vec2(1.0));
    }
    
    return uv;
}

void main()
{
    vec2 va = vertex_pos[0]; // top left
    vec2 vb = vertex_pos[1]; // top right
    vec2 vc = vertex_pos[2]; // bottom right
    vec2 vd = vertex_pos[3]; // bottom left

    vec2 uv = invbilinear_robust(fragTexCoord, va, vb, vc, vd);
    
    // Fallback to iterative if robust method fails
    const float TOLERANCE = 0.52; // Slightly more than 0.5 to account for boundary pixels
    if (max(abs(uv.x - 0.5), abs(uv.y - 0.5)) > TOLERANCE) {
        uv = invbilinear_iterative(fragTexCoord, va, vb, vc, vd);
    }
    
    // Final validation
    if (uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0) {
        discard;
    }
    
    vec4 c = texture(texture0, uv);
    finalColor = c * fragColor;
}