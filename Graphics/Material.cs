using OpenTK.Mathematics;
using System;

namespace MiniRenderer.Graphics
{
    /// <summary>
    /// Represents a material with various properties for rendering
    /// </summary>
    public class Material : IDisposable
    {
        // Diffuse texture and color
        public Texture DiffuseMap { get; set; }
        public Vector4 DiffuseColor { get; set; } = new Vector4(1.0f);

        // Specular texture and intensity
        public Texture SpecularMap { get; set; }
        public float SpecularIntensity { get; set; } = 0.5f;
        public float Shininess { get; set; } = 32.0f;

        // Ambient lighting factor
        public float AmbientStrength { get; set; } = 0.1f;

        // Alpha transparency
        public float Alpha { get; set; } = 1.0f;

        // Whether to use textures or flat colors
        public bool UseTextures { get; set; } = true;

        // Dispose flags
        private bool _disposed = false;
        private bool _ownsTextures = false;

        /// <summary>
        /// Create a new material with default properties
        /// </summary>
        public Material()
        {
            // Default texture is white
            DiffuseMap = Texture.CreateWhiteTexture();
            SpecularMap = null; // No specular by default
        }

        /// <summary>
        /// Create a new material with the specified diffuse texture
        /// </summary>
        /// <param name="diffuseMap">Diffuse texture</param>
        /// <param name="ownsTextures">Whether this material owns and should dispose the textures</param>
        public Material(Texture diffuseMap, bool ownsTextures = false)
        {
            DiffuseMap = diffuseMap;
            SpecularMap = null;
            _ownsTextures = ownsTextures;
        }

        /// <summary>
        /// Create a new material with the specified diffuse and specular textures
        /// </summary>
        /// <param name="diffuseMap">Diffuse texture</param>
        /// <param name="specularMap">Specular map</param>
        /// <param name="ownsTextures">Whether this material owns and should dispose the textures</param>
        public Material(Texture diffuseMap, Texture specularMap, bool ownsTextures = false)
        {
            DiffuseMap = diffuseMap;
            SpecularMap = specularMap;
            _ownsTextures = ownsTextures;
        }

        /// <summary>
        /// Create a simple colored material without textures
        /// </summary>
        /// <param name="diffuseColor">Diffuse color</param>
        /// <returns>A new material with the specified color</returns>
        public static Material CreateColored(Vector4 diffuseColor)
        {
            Material material = new Material();
            material.DiffuseColor = diffuseColor;
            material.UseTextures = false;
            return material;
        }

        /// <summary>
        /// Apply this material to a shader
        /// </summary>
        /// <param name="shader">Shader to apply the material to</param>
        public void Apply(Shader shader)
        {
            // Use the shader
            shader.Use();

            // Set material properties
            shader.SetBool("material.useTextures", UseTextures);
            shader.SetVector4("material.diffuseColor", DiffuseColor);
            shader.SetFloat("material.specularIntensity", SpecularIntensity);
            shader.SetFloat("material.shininess", Shininess);
            shader.SetFloat("material.ambientStrength", AmbientStrength);
            shader.SetFloat("material.alpha", Alpha);

            // Bind textures
            if (UseTextures && DiffuseMap != null)
            {
                // Diffuse texture (always present)
                DiffuseMap.Use(OpenTK.Graphics.OpenGL4.TextureUnit.Texture0);
                shader.SetInt("material.diffuseMap", 0);

                // Specular texture (if available)
                if (SpecularMap != null)
                {
                    SpecularMap.Use(OpenTK.Graphics.OpenGL4.TextureUnit.Texture1);
                    shader.SetInt("material.specularMap", 1);
                    shader.SetBool("material.hasSpecularMap", true);
                }
                else
                {
                    shader.SetBool("material.hasSpecularMap", false);
                    // Set a default specular intensity when no specular map is available
                    shader.SetInt("material.specularMap", 0); // Use texture unit 0 as fallback
                }
            }
            else
            {
                shader.SetBool("material.hasSpecularMap", false);
                shader.SetInt("material.diffuseMap", 0);
                shader.SetInt("material.specularMap", 0);
            }
        }

        /// <summary>
        /// Dispose of the material resources
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                // If we own the textures, dispose them
                if (_ownsTextures)
                {
                    DiffuseMap?.Dispose();
                    SpecularMap?.Dispose();
                }

                _disposed = true;
                GC.SuppressFinalize(this);
            }
        }

        /// <summary>
        /// Finalizer
        /// </summary>
        ~Material()
        {
            if (!_disposed)
            {
                Console.WriteLine("WARNING: Material was not disposed properly.");
            }
        }
    }
}