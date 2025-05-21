#version 330 core
layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec2 aTexCoord;
layout(location = 2) in vec3 aNormal;
layout(location = 3) in vec4 aColor;

out vec2 texCoord;
out vec3 normal;
out vec4 vertexColor;

uniform mat4 uModel;
uniform mat4 uView;
uniform mat4 uProjection;
uniform bool uUseTexture; // Keep same uniforms in all shaders

void main()
{
    gl_Position = uProjection * uView * uModel * vec4(aPosition, 1.0);
    texCoord = aTexCoord;
    
    // Transform the normal to world space
    normal = mat3(transpose(inverse(uModel))) * aNormal;
    
    vertexColor = aColor;
}