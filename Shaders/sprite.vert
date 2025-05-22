#version 330 core

// Input vertex attributes
layout(location = 0) in vec2 aPosition;
layout(location = 1) in vec2 aTexCoord;
layout(location = 2) in vec4 aColor;

// Output to fragment shader
out vec2 texCoord;
out vec4 vertexColor;

// Transformation matrices
uniform mat4 uModel;      // Model matrix for the sprite
uniform mat4 uView;       // View matrix from the 2D camera
uniform mat4 uProjection; // Projection matrix from the 2D camera

void main()
{
    // Apply the model-view-projection matrix transformation
    gl_Position = uProjection * uView * uModel * vec4(aPosition, 0.0, 1.0);
    
    // Pass the texture coordinates to the fragment shader
    texCoord = aTexCoord;
    
    // Pass the vertex color to the fragment shader
    vertexColor = aColor;
}