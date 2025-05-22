#version 330 core
in vec4 vertexColor;

out vec4 FragColor;

uniform vec4 uColor;
uniform bool uUseTexture; // Added uniform (unused) to avoid warnings
uniform sampler2D uTexture; // Added uniform (unused) to avoid warnings

void main()
{
    FragColor = vertexColor * uColor;
}