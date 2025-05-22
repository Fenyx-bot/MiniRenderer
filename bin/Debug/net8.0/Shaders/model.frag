#version 330 core
in vec2 texCoord;
in vec3 normal;
in vec4 vertexColor;

out vec4 FragColor;

uniform sampler2D uTexture;
uniform bool uUseTexture;
uniform vec4 uColor;

void main()
{
    // Normalize the normal
    vec3 norm = normalize(normal);
    
    // Basic lighting calculation
    vec3 lightDir = normalize(vec3(0.5, 1.0, 0.3)); // Light direction
    float diff = max(dot(norm, lightDir), 0.0); // Diffuse factor
    
    // Ambient lighting
    float ambientStrength = 0.3;
    vec3 ambient = ambientStrength * vec3(1.0);
    
    // Combine ambient and diffuse
    vec3 lighting = ambient + diff * vec3(0.7);
    
    if (uUseTexture)
    {
        // Sample the texture
        vec4 texColor = texture(uTexture, texCoord);
        
        // Apply lighting to the texture color
        FragColor = vec4(texColor.rgb * lighting, texColor.a) * uColor;
    }
    else
    {
        // Apply lighting to the vertex color
        FragColor = vec4(vertexColor.rgb * lighting, vertexColor.a) * uColor;
    }
}