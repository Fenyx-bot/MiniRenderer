using OpenTK.Mathematics;
using MiniRenderer.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MiniRenderer.Lighting
{
    /// <summary>
    /// Manages multiple lights and lighting components for a scene
    /// </summary>
    public class LightingManager
    {
        // Light collection
        private List<Light> _lights = new List<Light>();
        private Light _primaryLight; // The main light for simple scenarios

        // Global lighting settings
        public bool EnableAmbient { get; set; } = true;
        public bool EnableDiffuse { get; set; } = true;
        public bool EnableSpecular { get; set; } = true;

        // Component strengths
        public float AmbientStrength { get; set; } = 0.1f;
        public float DiffuseStrength { get; set; } = 1.0f;
        public float SpecularStrength { get; set; } = 0.5f;

        // Global ambient color (affects all objects even without direct lighting)
        public Vector3 AmbientColor { get; set; } = new Vector3(0.1f, 0.1f, 0.1f);

        // Material presets
        public enum MaterialPreset
        {
            Default,
            Shiny,
            Rough,
            Metal,
            Plastic,
            Rubber
        }

        private MaterialPreset _currentMaterialPreset = MaterialPreset.Default;

        // Light visualization
        public bool ShowLightPositions { get; set; } = true;
        private Mesh _lightVisualizationMesh;
        private Material _lightVisualizationMaterial;

        // Performance settings
        public int MaxLightsPerObject { get; set; } = 8; // Limit for shader performance

        // Events for when lighting changes (useful for UI updates)
        public event Action<LightingManager> LightingChanged;

        /// <summary>
        /// Create a new lighting manager with default settings
        /// </summary>
        public LightingManager()
        {
            // Create default point light
            _primaryLight = Light.CreatePoint(
                new Vector3(2.0f, 3.0f, 2.0f),
                new Vector3(1.0f, 1.0f, 1.0f),
                1.0f
            );
            _primaryLight.Name = "Primary Light";
            _primaryLight.AutoRotate = true;
            _lights.Add(_primaryLight);

            CreateLightVisualization();
        }

        /// <summary>
        /// Get the primary light (main light source)
        /// </summary>
        public Light PrimaryLight => _primaryLight;

        /// <summary>
        /// Get all lights in the scene
        /// </summary>
        public IReadOnlyList<Light> Lights => _lights.AsReadOnly();

        /// <summary>
        /// Get the current material preset
        /// </summary>
        public MaterialPreset CurrentMaterialPreset => _currentMaterialPreset;

        /// <summary>
        /// Add a light to the scene
        /// </summary>
        /// <param name="light">Light to add</param>
        public void AddLight(Light light)
        {
            if (light == null) return;

            _lights.Add(light);
            OnLightingChanged();
        }

        /// <summary>
        /// Remove a light from the scene
        /// </summary>
        /// <param name="light">Light to remove</param>
        /// <returns>True if the light was removed</returns>
        public bool RemoveLight(Light light)
        {
            if (light == null || light == _primaryLight) return false; // Can't remove primary light

            bool removed = _lights.Remove(light);
            if (removed)
            {
                OnLightingChanged();
            }
            return removed;
        }

        /// <summary>
        /// Remove all lights except the primary light
        /// </summary>
        public void ClearSecondaryLights()
        {
            _lights.Clear();
            _lights.Add(_primaryLight);
            OnLightingChanged();
        }

        /// <summary>
        /// Update all lights (animations, etc.)
        /// </summary>
        /// <param name="deltaTime">Time since last update</param>
        public void Update(float deltaTime)
        {
            foreach (var light in _lights)
            {
                light.Update(deltaTime);
            }

            // Update light visualization positions
            if (_lightVisualizationMesh != null && _primaryLight != null)
            {
                _lightVisualizationMesh.Position = _primaryLight.Position;
            }
        }

        /// <summary>
        /// Apply lighting to a shader (single light - for simple scenarios)
        /// </summary>
        /// <param name="shader">Shader to apply lighting to</param>
        /// <param name="cameraPosition">Camera position for specular calculations</param>
        public void ApplyToShader(Shader shader, Vector3 cameraPosition)
        {
            shader.Use();

            // Apply primary light
            if (_primaryLight != null)
            {
                _primaryLight.ApplyToShader(shader, "light");
            }

            // Apply global lighting settings
            shader.SetVector3("viewPos", cameraPosition);
            shader.SetBool("enableAmbient", EnableAmbient);
            shader.SetBool("enableDiffuse", EnableDiffuse);
            shader.SetBool("enableSpecular", EnableSpecular);
            shader.SetFloat("ambientStrength", AmbientStrength);
            shader.SetFloat("diffuseStrength", DiffuseStrength);
            shader.SetFloat("specularStrength", SpecularStrength);
            shader.SetVector3("ambientColor", AmbientColor);
        }

        /// <summary>
        /// Apply multiple lights to a shader (for advanced scenarios)
        /// </summary>
        /// <param name="shader">Shader to apply lighting to</param>
        /// <param name="cameraPosition">Camera position for specular calculations</param>
        public void ApplyMultipleLightsToShader(Shader shader, Vector3 cameraPosition)
        {
            shader.Use();

            // Apply up to MaxLightsPerObject lights
            int lightCount = Math.Min(_lights.Count, MaxLightsPerObject);
            shader.SetInt("numLights", lightCount);

            for (int i = 0; i < lightCount; i++)
            {
                _lights[i].ApplyToShader(shader, $"lights[{i}]");
            }

            // Apply global settings
            shader.SetVector3("viewPos", cameraPosition);
            shader.SetBool("enableAmbient", EnableAmbient);
            shader.SetBool("enableDiffuse", EnableDiffuse);
            shader.SetBool("enableSpecular", EnableSpecular);
            shader.SetFloat("ambientStrength", AmbientStrength);
            shader.SetFloat("diffuseStrength", DiffuseStrength);
            shader.SetFloat("specularStrength", SpecularStrength);
            shader.SetVector3("ambientColor", AmbientColor);
        }

        /// <summary>
        /// Render light visualizations
        /// </summary>
        /// <param name="shader">Shader for rendering</param>
        public void RenderLightVisualization(Shader shader)
        {
            if (!ShowLightPositions || _lightVisualizationMesh == null) return;

            // Temporarily modify lighting for visualization
            var originalAmbient = EnableAmbient;
            var originalDiffuse = EnableDiffuse;
            var originalSpecular = EnableSpecular;
            var originalAmbientStrength = AmbientStrength;

            shader.SetBool("enableAmbient", true);
            shader.SetBool("enableDiffuse", false);
            shader.SetBool("enableSpecular", false);
            shader.SetFloat("ambientStrength", 1.0f);

            // Render visualization for each light
            foreach (var light in _lights)
            {
                if (light.Type != LightType.Directional) // Don't visualize directional lights
                {
                    _lightVisualizationMesh.Position = light.Position;

                    // Set the visualization color to match the light color
                    if (_lightVisualizationMaterial != null)
                    {
                        _lightVisualizationMaterial.DiffuseColor = new Vector4(light.Color, 1.0f);
                    }

                    _lightVisualizationMesh.Render(shader);
                }
            }

            // Restore original lighting settings
            shader.SetBool("enableAmbient", originalAmbient);
            shader.SetBool("enableDiffuse", originalDiffuse);
            shader.SetBool("enableSpecular", originalSpecular);
            shader.SetFloat("ambientStrength", originalAmbientStrength);
        }

        /// <summary>
        /// Toggle a lighting component on/off
        /// </summary>
        /// <param name="component">Component to toggle</param>
        public void ToggleLightingComponent(string component)
        {
            switch (component.ToLower())
            {
                case "ambient":
                    EnableAmbient = !EnableAmbient;
                    break;
                case "diffuse":
                    EnableDiffuse = !EnableDiffuse;
                    break;
                case "specular":
                    EnableSpecular = !EnableSpecular;
                    break;
            }
            OnLightingChanged();
        }

        /// <summary>
        /// Adjust the strength of a lighting component
        /// </summary>
        /// <param name="component">Component to adjust</param>
        /// <param name="delta">Amount to change (positive or negative)</param>
        public void AdjustLightingStrength(string component, float delta)
        {
            switch (component.ToLower())
            {
                case "ambient":
                    AmbientStrength = Math.Clamp(AmbientStrength + delta, 0.0f, 2.0f);
                    break;
                case "diffuse":
                    DiffuseStrength = Math.Clamp(DiffuseStrength + delta, 0.0f, 2.0f);
                    break;
                case "specular":
                    SpecularStrength = Math.Clamp(SpecularStrength + delta, 0.0f, 2.0f);
                    break;
                case "intensity":
                    if (_primaryLight != null)
                    {
                        _primaryLight.Intensity = Math.Clamp(_primaryLight.Intensity + delta, 0.0f, 5.0f);
                    }
                    break;
            }
            OnLightingChanged();
        }

        /// <summary>
        /// Set light color by preset name
        /// </summary>
        /// <param name="colorName">Name of the color preset</param>
        public void SetLightColor(string colorName)
        {
            _primaryLight?.SetColorPreset(colorName);
            OnLightingChanged();
        }

        /// <summary>
        /// Cycle through light types for the primary light
        /// </summary>
        public void CyclePrimaryLightType()
        {
            if (_primaryLight == null) return;

            switch (_primaryLight.Type)
            {
                case LightType.Directional:
                    _primaryLight.Type = LightType.Point;
                    _primaryLight.Position = new Vector3(2.0f, 3.0f, 2.0f);
                    break;
                case LightType.Point:
                    _primaryLight.Type = LightType.Spot;
                    _primaryLight.Direction = new Vector3(0.0f, -1.0f, 0.0f);
                    break;
                case LightType.Spot:
                    _primaryLight.Type = LightType.Directional;
                    _primaryLight.Direction = new Vector3(-0.2f, -1.0f, -0.3f);
                    break;
            }
            OnLightingChanged();
        }

        /// <summary>
        /// Apply material preset to a collection of materials
        /// </summary>
        /// <param name="materials">Materials to modify</param>
        public void ApplyMaterialPreset(MaterialPreset preset, params Material[] materials)
        {
            _currentMaterialPreset = preset;

            foreach (var material in materials)
            {
                if (material == null) continue;

                switch (preset)
                {
                    case MaterialPreset.Shiny:
                        material.Shininess = 128.0f;
                        material.SpecularIntensity = 1.5f;
                        material.AmbientStrength = 0.05f;
                        break;

                    case MaterialPreset.Rough:
                        material.Shininess = 4.0f;
                        material.SpecularIntensity = 0.1f;
                        material.AmbientStrength = 0.3f;
                        break;

                    case MaterialPreset.Metal:
                        material.Shininess = 256.0f;
                        material.SpecularIntensity = 2.0f;
                        material.AmbientStrength = 0.02f;
                        break;

                    case MaterialPreset.Plastic:
                        material.Shininess = 64.0f;
                        material.SpecularIntensity = 0.8f;
                        material.AmbientStrength = 0.1f;
                        break;

                    case MaterialPreset.Rubber:
                        material.Shininess = 2.0f;
                        material.SpecularIntensity = 0.05f;
                        material.AmbientStrength = 0.4f;
                        break;

                    case MaterialPreset.Default:
                    default:
                        material.Shininess = 32.0f;
                        material.SpecularIntensity = 0.5f;
                        material.AmbientStrength = 0.1f;
                        break;
                }
            }
            OnLightingChanged();
        }

        /// <summary>
        /// Create common lighting scenarios for educational purposes
        /// </summary>
        /// <param name="scenario">Name of the scenario</param>
        public void LoadLightingScenario(string scenario)
        {
            ClearSecondaryLights();

            switch (scenario.ToLower())
            {
                case "sunrise":
                    _primaryLight.Type = LightType.Directional;
                    _primaryLight.Direction = new Vector3(0.5f, -0.3f, -0.8f);
                    _primaryLight.SetColorPreset("warm");
                    _primaryLight.Intensity = 0.8f;
                    AmbientStrength = 0.3f;
                    break;

                case "noon":
                    _primaryLight.Type = LightType.Directional;
                    _primaryLight.Direction = new Vector3(0.0f, -1.0f, 0.0f);
                    _primaryLight.SetColorPreset("white");
                    _primaryLight.Intensity = 1.2f;
                    AmbientStrength = 0.4f;
                    break;

                case "sunset":
                    _primaryLight.Type = LightType.Directional;
                    _primaryLight.Direction = new Vector3(-0.5f, -0.3f, 0.8f);
                    _primaryLight.SetColorPreset("orange");
                    _primaryLight.Intensity = 0.7f;
                    AmbientStrength = 0.2f;
                    break;

                case "night":
                    _primaryLight.Type = LightType.Point;
                    _primaryLight.Position = new Vector3(0.0f, 5.0f, 0.0f);
                    _primaryLight.SetColorPreset("cool");
                    _primaryLight.Intensity = 0.4f;
                    AmbientStrength = 0.05f;
                    break;

                case "studio":
                    // Primary key light
                    _primaryLight.Type = LightType.Point;
                    _primaryLight.Position = new Vector3(3.0f, 4.0f, 3.0f);
                    _primaryLight.SetColorPreset("white");
                    _primaryLight.Intensity = 1.0f;

                    // Add fill light
                    var fillLight = Light.CreatePoint(
                        new Vector3(-2.0f, 2.0f, 2.0f),
                        new Vector3(0.8f, 0.8f, 1.0f),
                        0.5f
                    );
                    fillLight.Name = "Fill Light";
                    AddLight(fillLight);

                    AmbientStrength = 0.1f;
                    break;

                case "candlelight":
                    _primaryLight.Type = LightType.Point;
                    _primaryLight.Position = new Vector3(0.0f, 1.5f, 0.0f);
                    _primaryLight.SetColorPreset("orange");
                    _primaryLight.Intensity = 0.6f;
                    _primaryLight.Constant = 1.0f;
                    _primaryLight.Linear = 0.35f;
                    _primaryLight.Quadratic = 0.44f; // Strong attenuation for intimate lighting
                    AmbientStrength = 0.02f;
                    break;

                default:
                    // Reset to default
                    _primaryLight.Type = LightType.Point;
                    _primaryLight.Position = new Vector3(2.0f, 3.0f, 2.0f);
                    _primaryLight.SetColorPreset("white");
                    _primaryLight.Intensity = 1.0f;
                    _primaryLight.Constant = 1.0f;
                    _primaryLight.Linear = 0.09f;
                    _primaryLight.Quadratic = 0.032f;
                    AmbientStrength = 0.1f;
                    break;
            }
            OnLightingChanged();
        }

        /// <summary>
        /// Get lighting information for debugging/UI display
        /// </summary>
        /// <returns>String with current lighting state</returns>
        public string GetLightingInfo()
        {
            var info = $"Lighting Manager Status:\n";
            info += $"  Lights: {_lights.Count}\n";
            info += $"  Primary Light: {_primaryLight?.Type} at {_primaryLight?.Position}\n";
            info += $"  Ambient: {(EnableAmbient ? "ON" : "OFF")} ({AmbientStrength:F2})\n";
            info += $"  Diffuse: {(EnableDiffuse ? "ON" : "OFF")} ({DiffuseStrength:F2})\n";
            info += $"  Specular: {(EnableSpecular ? "ON" : "OFF")} ({SpecularStrength:F2})\n";
            info += $"  Material Preset: {_currentMaterialPreset}\n";
            return info;
        }

        /// <summary>
        /// Create light visualization mesh
        /// </summary>
        private void CreateLightVisualization()
        {
            try
            {
                _lightVisualizationMesh = Mesh.CreateCube(0.3f);
                _lightVisualizationMaterial = Material.CreateColored(new Vector4(1.0f, 1.0f, 0.8f, 1.0f));
                _lightVisualizationMaterial.AmbientStrength = 1.0f; // Make it always visible
                _lightVisualizationMesh.SetMaterial(_lightVisualizationMaterial, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not create light visualization: {ex.Message}");
            }
        }

        /// <summary>
        /// Trigger lighting changed event
        /// </summary>
        private void OnLightingChanged()
        {
            LightingChanged?.Invoke(this);
        }

        /// <summary>
        /// Reset all lighting to default values
        /// </summary>
        public void ResetToDefaults()
        {
            EnableAmbient = true;
            EnableDiffuse = true;
            EnableSpecular = true;
            AmbientStrength = 0.1f;
            DiffuseStrength = 1.0f;
            SpecularStrength = 0.5f;
            AmbientColor = new Vector3(0.1f, 0.1f, 0.1f);
            ShowLightPositions = true;

            ClearSecondaryLights();

            if (_primaryLight != null)
            {
                _primaryLight.Type = LightType.Point;
                _primaryLight.Position = new Vector3(2.0f, 3.0f, 2.0f);
                _primaryLight.SetColorPreset("white");
                _primaryLight.Intensity = 1.0f;
                _primaryLight.AutoRotate = true;
            }

            OnLightingChanged();
        }

        /// <summary>
        /// Dispose of resources
        /// </summary>
        public void Dispose()
        {
            _lightVisualizationMesh?.Dispose();
            _lightVisualizationMaterial?.Dispose();
        }

        /// <summary>
        /// Finalizer
        /// </summary>
        ~LightingManager()
        {
            Console.WriteLine("WARNING: LightingManager was not disposed properly.");
        }
    }
}