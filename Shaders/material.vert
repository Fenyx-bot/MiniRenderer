#version 330 core
layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec2 aTexCoord;
layout(location = 2) in vec3 aNormal;
layout(location = 3) in vec4 aColor;

// Output to fragment shader
out vec2 texCoord;
out vec3 normal;
out vec3 fragPos;
out vec4 vertexColor;

// Transformation matrices
uniform mat4 uModel;
uniform mat4 uView;
uniform mat4 uProjection;

// Texture tiling parameters
uniform vec2 uTextureScale = vec2(1.0, 1.0);
uniform vec2 uTextureOffset = vec2(0.0, 0.0);

void main()
{
    // Apply the full MVP transformation
    gl_Position = uProjection * uView * uModel * vec4(aPosition, 1.0);
    
    // Calculate world space fragment position (for lighting)
    fragPos = vec3(uModel * vec4(aPosition, 1.0));
    
    // Transform the normal to world space
    normal = mat3(transpose(inverse(uModel))) * aNormal;
    
    // Apply tiling to texture coordinates
    texCoord = (aTexCoord * uTextureScale) + uTextureOffset;
    
    // Pass the vertex color to the fragment shader
    vertexColor = aColor;
}