#version 330 core

// Input vertex data
layout(location = 0) in vec2 aPosition;
layout(location = 1) in vec3 aColor;

// Output data that will be passed to the fragment shader
out vec3 vertexColor;

void main()
{
    // Set the vertex position
    gl_Position = vec4(aPosition, 0.0, 1.0);
    
    // Pass the color to the fragment shader
    vertexColor = aColor;
}