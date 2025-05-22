#version 330 core

// Input from vertex shader
in vec2 texCoord;
in vec4 vertexColor;

// Output color
out vec4 FragColor;

// Texture sampler
uniform sampler2D uTexture;
uniform bool uUseTexture;

void main()
{
    if (uUseTexture)
    {
        // Sample the texture
        vec4 texColor = texture(uTexture, texCoord);
        
        // Combine texture color with vertex color
        FragColor = texColor * vertexColor;
    }
    else
    {
        // If not using a texture, just use the vertex color
        FragColor = vertexColor;
    }
}