#version 330 core
// Input from vertex shader
in vec2 texCoord;
in vec3 normal;
in vec3 fragPos;
in vec4 vertexColor;

// Output
out vec4 FragColor;

// Material properties
struct Material {
    bool useTextures;
    vec4 diffuseColor;
    float specularIntensity;
    float shininess;
    float ambientStrength;
    float alpha;
    sampler2D diffuseMap;
    sampler2D specularMap;
    bool hasSpecularMap;
};

// Light properties
struct Light {
    int type; // 0=Directional, 1=Point, 2=Spot
    vec3 position;
    vec3 direction;
    vec3 color;
    float intensity;
    
    // Attenuation (for point/spot lights)
    float constant;
    float linear;
    float quadratic;
    
    // Spot light specific
    float cutOff;
    float outerCutOff;
};

// Uniforms
uniform Material material;
uniform Light light;
uniform vec3 viewPos;

// Lighting component toggles
uniform bool enableAmbient;
uniform bool enableDiffuse;
uniform bool enableSpecular;
uniform float ambientStrength;
uniform float diffuseStrength;
uniform float specularStrength;
uniform vec3 ambientColor;

vec3 calculateDirectionalLight(Light light, vec3 normal, vec3 viewDir, vec3 baseColor)
{
    vec3 lightDir = normalize(-light.direction);
    
    // Ambient
    vec3 ambient = vec3(0.0);
    if (enableAmbient) {
        ambient = ambientStrength * ambientColor;
    }
    
    // Diffuse
    vec3 diffuse = vec3(0.0);
    if (enableDiffuse) {
        float diff = max(dot(normal, lightDir), 0.0);
        diffuse = diff * light.color * light.intensity * diffuseStrength;
    }
    
    // Specular
    vec3 specular = vec3(0.0);
    if (enableSpecular) {
        vec3 reflectDir = reflect(-lightDir, normal);
        float spec = pow(max(dot(viewDir, reflectDir), 0.0), material.shininess);
        
        float specularComponent = material.specularIntensity;
        if (material.hasSpecularMap && material.useTextures) {
            specularComponent *= texture(material.specularMap, texCoord).r;
        }
        
        specular = spec * specularComponent * light.color * light.intensity * specularStrength;
    }
    
    return (ambient + diffuse + specular) * baseColor;
}

vec3 calculatePointLight(Light light, vec3 normal, vec3 fragPos, vec3 viewDir, vec3 baseColor)
{
    vec3 lightDir = normalize(light.position - fragPos);
    float distance = length(light.position - fragPos);
    
    // Attenuation
    float attenuation = 1.0 / (light.constant + light.linear * distance + light.quadratic * (distance * distance));
    
    // Ambient
    vec3 ambient = vec3(0.0);
    if (enableAmbient) {
        ambient = ambientStrength * ambientColor;
    }
    
    // Diffuse
    vec3 diffuse = vec3(0.0);
    if (enableDiffuse) {
        float diff = max(dot(normal, lightDir), 0.0);
        diffuse = diff * light.color * light.intensity * diffuseStrength;
    }
    
    // Specular
    vec3 specular = vec3(0.0);
    if (enableSpecular) {
        vec3 reflectDir = reflect(-lightDir, normal);
        float spec = pow(max(dot(viewDir, reflectDir), 0.0), material.shininess);
        
        float specularComponent = material.specularIntensity;
        if (material.hasSpecularMap && material.useTextures) {
            specularComponent *= texture(material.specularMap, texCoord).r;
        }
        
        specular = spec * specularComponent * light.color * light.intensity * specularStrength;
    }
    
    // Apply attenuation to diffuse and specular (not ambient)
    diffuse *= attenuation;
    specular *= attenuation;
    
    return (ambient + diffuse + specular) * baseColor;
}

void main()
{
    // Normalize normal
    vec3 norm = normalize(normal);
    vec3 viewDir = normalize(viewPos - fragPos);
    
    // Get base color
    vec3 baseColor;
    if (material.useTextures) {
        baseColor = texture(material.diffuseMap, texCoord).rgb * vertexColor.rgb;
    } else {
        baseColor = material.diffuseColor.rgb * vertexColor.rgb;
    }
    
    // Calculate lighting based on light type
    vec3 result;
    if (light.type == 0) { // Directional
        result = calculateDirectionalLight(light, norm, viewDir, baseColor);
    } else if (light.type == 1) { // Point
        result = calculatePointLight(light, norm, fragPos, viewDir, baseColor);
    } else { // Default to point light
        result = calculatePointLight(light, norm, fragPos, viewDir, baseColor);
    }
    
    // Get alpha
    float alpha = material.useTextures ? texture(material.diffuseMap, texCoord).a * vertexColor.a : material.diffuseColor.a * vertexColor.a;
    
    FragColor = vec4(result, alpha);
}