#version 330 core

// Input from the vertex shader
in vec3 vertexColor;

// Uniform variables - can be set from C# code
uniform float uTime;    // Time in seconds for animation
uniform vec3 uTint;     // Color tint

// Output color
out vec4 FragColor;

void main()
{
    // Animate the colors using sine waves and time
    vec3 color = vertexColor;
    color.r = color.r * (sin(uTime) * 0.5 + 0.5);        // Red oscillates with time
    color.g = color.g * (sin(uTime + 2.0) * 0.5 + 0.5);  // Green oscillates with offset
    color.b = color.b * (sin(uTime + 4.0) * 0.5 + 0.5);  // Blue oscillates with different offset
    
    // Apply the tint color
    color = color * uTint;
    
    // Set the pixel color
    FragColor = vec4(color, 1.0);
}