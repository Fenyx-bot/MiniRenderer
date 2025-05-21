#version 330 core

// Input from the vertex shader
in vec3 vertexColor;

// Output color
out vec4 FragColor;

void main()
{
    // Set the pixel color directly from the interpolated vertex color
    FragColor = vec4(vertexColor, 1.0);
}