#version 330 core
// Input from vertex shader
in vec2 texCoord;
in vec3 normal;
in vec3 fragPos;
in vec4 vertexColor;

// Output color
out vec4 FragColor;

// Material structure
struct Material {
    bool useTextures;
    vec4 diffuseColor;
    float specularIntensity;
    float shininess;
    float ambientStrength;
    float alpha;
    bool hasSpecularMap;
    sampler2D diffuseMap;
    sampler2D specularMap;
};

// Light structure
struct Light {
    vec3 position;
    vec3 direction;
    vec3 color;
    float intensity;
    float constant;
    float linear;
    float quadratic;
    bool isDirectional;
};

// Uniforms
uniform Material material;
uniform Light light;
uniform vec3 viewPos; // Camera position for specular calculation
uniform bool uSpecularEnabled; // Global specular toggle

void main()
{
    // Normalize the normal
    vec3 norm = normalize(normal);
    
    // =====================
    // Lighting calculations
    // =====================
    
    vec3 lightDir;
    float attenuation = 1.0;
    
    if (light.isDirectional) {
        // For directional lights, use the negative direction
        lightDir = normalize(-light.direction);
    } else {
        // For point lights, calculate direction and attenuation
        lightDir = normalize(light.position - fragPos);
        
        // Calculate attenuation based on distance
        float distance = length(light.position - fragPos);
        attenuation = 1.0 / (light.constant + light.linear * distance + light.quadratic * (distance * distance));
    }
    
    // Calculate diffuse factor
    float diff = max(dot(norm, lightDir), 0.0);
    
    // Calculate specular factor (only if enabled)
    vec3 viewDir = normalize(viewPos - fragPos);
    vec3 reflectDir = reflect(-lightDir, norm);
    float spec = 0.0;
    
    if (uSpecularEnabled && diff > 0.0) {
        // Use Blinn-Phong for better specular highlights
        vec3 halfwayDir = normalize(lightDir + viewDir);
        spec = pow(max(dot(norm, halfwayDir), 0.0), material.shininess);
    }
    
    // Determine specular multiplier (from texture or uniform)
    float specMultiplier = material.specularIntensity;
    if (material.hasSpecularMap && material.useTextures && uSpecularEnabled) {
        vec4 specularTexture = texture(material.specularMap, texCoord);
        specMultiplier *= specularTexture.r; // Use red channel for specular intensity
    }
    
    // =====================
    // Final color calculation
    // =====================
    
    // Calculate ambient component
    vec3 ambient = material.ambientStrength * light.color;
    
    // Calculate diffuse component
    vec3 diffuse = diff * light.color * light.intensity;
    
    // Calculate specular component (only if enabled)
    vec3 specular = vec3(0.0);
    if (uSpecularEnabled) {
        specular = spec * specMultiplier * light.color * light.intensity;
    }
    
    // Apply attenuation to all components
    ambient *= attenuation;
    diffuse *= attenuation;
    specular *= attenuation;
    
    // Base color (from texture or material color)
    vec4 baseColor;
    if (material.useTextures) {
        baseColor = texture(material.diffuseMap, texCoord) * vertexColor;
    } else {
        baseColor = material.diffuseColor * vertexColor;
    }
    
    // Final color with lighting
    vec3 result = (ambient + diffuse + specular) * baseColor.rgb;
    
    // Set final color with alpha
    FragColor = vec4(result, baseColor.a * material.alpha);
}