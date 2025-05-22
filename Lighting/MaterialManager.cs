using OpenTK.Mathematics;
using MiniRenderer.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MiniRenderer.Lighting
{
    /// <summary>
    /// Manages material properties and presets for educational lighting demonstrations
    /// </summary>
    public class MaterialManager : IDisposable
    {
        // Material collections
        private List<Material> _managedMaterials = new List<Material>();
        private Dictionary<string, MaterialPreset> _presets = new Dictionary<string, MaterialPreset>();

        // Current state
        private string _currentPresetName = "Default";
        private bool _disposed = false;

        /// <summary>
        /// Represents a material preset with all properties
        /// </summary>
        public class MaterialPreset
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public float Shininess { get; set; }
            public float SpecularIntensity { get; set; }
            public float AmbientStrength { get; set; }
            public Vector4 DiffuseColor { get; set; }
            public bool UseTextures { get; set; }

            public MaterialPreset(string name, string description, float shininess, float specularIntensity,
                                float ambientStrength, Vector4 diffuseColor, bool useTextures = true)
            {
                Name = name;
                Description = description;
                Shininess = shininess;
                SpecularIntensity = specularIntensity;
                AmbientStrength = ambientStrength;
                DiffuseColor = diffuseColor;
                UseTextures = useTextures;
            }
        }

        /// <summary>
        /// Create a new material manager with default presets
        /// </summary>
        public MaterialManager()
        {
            CreateDefaultPresets();
        }

        /// <summary>
        /// Get the current preset name
        /// </summary>
        public string CurrentPresetName => _currentPresetName;

        /// <summary>
        /// Get all available preset names
        /// </summary>
        public IEnumerable<string> PresetNames => _presets.Keys;

        /// <summary>
        /// Get all managed materials
        /// </summary>
        public IReadOnlyList<Material> ManagedMaterials => _managedMaterials.AsReadOnly();

        /// <summary>
        /// Add a material to be managed by this manager
        /// </summary>
        /// <param name="material">Material to add</param>
        /// <param name="name">Optional name for the material</param>
        public void AddMaterial(Material material, string name = null)
        {
            if (material == null) return;

            if (!_managedMaterials.Contains(material))
            {
                _managedMaterials.Add(material);
            }
        }

        /// <summary>
        /// Remove a material from management
        /// </summary>
        /// <param name="material">Material to remove</param>
        public void RemoveMaterial(Material material)
        {
            _managedMaterials.Remove(material);
        }

        /// <summary>
        /// Clear all managed materials
        /// </summary>
        public void ClearMaterials()
        {
            _managedMaterials.Clear();
        }

        /// <summary>
        /// Apply a preset to all managed materials
        /// </summary>
        /// <param name="presetName">Name of the preset to apply</param>
        /// <returns>True if the preset was found and applied</returns>
        public bool ApplyPreset(string presetName)
        {
            if (!_presets.TryGetValue(presetName, out MaterialPreset preset))
            {
                Console.WriteLine($"Warning: Material preset '{presetName}' not found");
                return false;
            }

            _currentPresetName = presetName;

            foreach (var material in _managedMaterials)
            {
                ApplyPresetToMaterial(material, preset);
            }

            Console.WriteLine($"Applied material preset: {preset.Name}");
            Console.WriteLine($"  Description: {preset.Description}");
            Console.WriteLine($"  Shininess: {preset.Shininess:F0}");
            Console.WriteLine($"  Specular Intensity: {preset.SpecularIntensity:F2}");
            Console.WriteLine($"  Ambient Strength: {preset.AmbientStrength:F2}");

            return true;
        }

        /// <summary>
        /// Apply a preset to a specific material
        /// </summary>
        /// <param name="material">Material to modify</param>
        /// <param name="preset">Preset to apply</param>
        private void ApplyPresetToMaterial(Material material, MaterialPreset preset)
        {
            if (material == null || preset == null) return;

            material.Shininess = preset.Shininess;
            material.SpecularIntensity = preset.SpecularIntensity;
            material.AmbientStrength = preset.AmbientStrength;

            // Only change color if not using textures or if preset specifies to override
            if (!material.UseTextures || !preset.UseTextures)
            {
                material.DiffuseColor = preset.DiffuseColor;
                material.UseTextures = preset.UseTextures;
            }
        }

        /// <summary>
        /// Cycle through presets in order
        /// </summary>
        /// <returns>Name of the new preset</returns>
        public string CyclePreset()
        {
            var presetNames = _presets.Keys.ToList();
            if (presetNames.Count == 0) return _currentPresetName;

            int currentIndex = presetNames.IndexOf(_currentPresetName);
            int nextIndex = (currentIndex + 1) % presetNames.Count;
            string nextPreset = presetNames[nextIndex];

            ApplyPreset(nextPreset);
            return nextPreset;
        }

        /// <summary>
        /// Adjust shininess of all managed materials
        /// </summary>
        /// <param name="delta">Amount to change shininess by</param>
        public void AdjustShininess(float delta)
        {
            foreach (var material in _managedMaterials)
            {
                material.Shininess = Math.Clamp(material.Shininess + delta, 1.0f, 512.0f);
            }

            if (_managedMaterials.Count > 0)
            {
                Console.WriteLine($"Material shininess: {_managedMaterials[0].Shininess:F0}");
            }
        }

        /// <summary>
        /// Adjust specular intensity of all managed materials
        /// </summary>
        /// <param name="delta">Amount to change specular intensity by</param>
        public void AdjustSpecularIntensity(float delta)
        {
            foreach (var material in _managedMaterials)
            {
                material.SpecularIntensity = Math.Clamp(material.SpecularIntensity + delta, 0.0f, 3.0f);
            }

            if (_managedMaterials.Count > 0)
            {
                Console.WriteLine($"Specular intensity: {_managedMaterials[0].SpecularIntensity:F2}");
            }
        }

        /// <summary>
        /// Adjust ambient strength of all managed materials
        /// </summary>
        /// <param name="delta">Amount to change ambient strength by</param>
        public void AdjustAmbientStrength(float delta)
        {
            foreach (var material in _managedMaterials)
            {
                material.AmbientStrength = Math.Clamp(material.AmbientStrength + delta, 0.0f, 1.0f);
            }

            if (_managedMaterials.Count > 0)
            {
                Console.WriteLine($"Ambient strength: {_managedMaterials[0].AmbientStrength:F2}");
            }
        }

        /// <summary>
        /// Set material color for all managed materials (only affects non-textured materials)
        /// </summary>
        /// <param name="color">New diffuse color</param>
        public void SetMaterialColor(Vector4 color)
        {
            foreach (var material in _managedMaterials)
            {
                if (!material.UseTextures)
                {
                    material.DiffuseColor = color;
                }
            }
        }

        /// <summary>
        /// Create educational material comparison set
        /// </summary>
        /// <param name="baseTexture">Base texture to use for all materials</param>
        /// <returns>List of materials with different properties for comparison</returns>
        public List<Material> CreateComparisonSet(Texture baseTexture = null)
        {
            var materials = new List<Material>();

            // Very shiny material
            var shiny = new Material(baseTexture);
            shiny.Shininess = 256.0f;
            shiny.SpecularIntensity = 2.0f;
            shiny.AmbientStrength = 0.02f;
            materials.Add(shiny);

            // Medium shiny material
            var medium = new Material(baseTexture);
            medium.Shininess = 64.0f;
            medium.SpecularIntensity = 0.8f;
            medium.AmbientStrength = 0.1f;
            materials.Add(medium);

            // Rough material
            var rough = new Material(baseTexture);
            rough.Shininess = 4.0f;
            rough.SpecularIntensity = 0.1f;
            rough.AmbientStrength = 0.3f;
            materials.Add(rough);

            // Add to managed materials
            foreach (var material in materials)
            {
                AddMaterial(material);
            }

            return materials;
        }

        /// <summary>
        /// Create default material presets
        /// </summary>
        private void CreateDefaultPresets()
        {
            // Educational presets with clear differences
            _presets["Default"] = new MaterialPreset(
                "Default",
                "Balanced material suitable for general use",
                32.0f, 0.5f, 0.1f, new Vector4(0.8f, 0.8f, 0.8f, 1.0f), true
            );

            _presets["Shiny"] = new MaterialPreset(
                "Shiny",
                "High-gloss surface like polished metal or plastic",
                128.0f, 1.5f, 0.05f, new Vector4(0.9f, 0.9f, 0.9f, 1.0f), true
            );

            _presets["Rough"] = new MaterialPreset(
                "Rough",
                "Matte surface with no reflections like concrete or fabric",
                4.0f, 0.1f, 0.3f, new Vector4(0.6f, 0.6f, 0.6f, 1.0f), true
            );

            _presets["Metal"] = new MaterialPreset(
                "Metal",
                "Highly reflective metallic surface",
                256.0f, 2.0f, 0.02f, new Vector4(0.8f, 0.8f, 0.9f, 1.0f), true
            );

            _presets["Plastic"] = new MaterialPreset(
                "Plastic",
                "Smooth plastic surface with moderate reflection",
                64.0f, 0.8f, 0.08f, new Vector4(0.7f, 0.7f, 0.8f, 1.0f), true
            );

            _presets["Rubber"] = new MaterialPreset(
                "Rubber",
                "Soft, non-reflective rubber material",
                2.0f, 0.05f, 0.4f, new Vector4(0.4f, 0.4f, 0.4f, 1.0f), true
            );

            _presets["Glass"] = new MaterialPreset(
                "Glass",
                "Transparent, highly reflective glass surface",
                512.0f, 3.0f, 0.01f, new Vector4(0.9f, 0.9f, 1.0f, 0.3f), true
            );

            _presets["Ceramic"] = new MaterialPreset(
                "Ceramic",
                "Smooth ceramic with subtle reflections",
                96.0f, 0.6f, 0.12f, new Vector4(0.95f, 0.95f, 0.9f, 1.0f), true
            );

            _presets["Wood"] = new MaterialPreset(
                "Wood",
                "Natural wood surface with low reflectivity",
                8.0f, 0.2f, 0.25f, new Vector4(0.6f, 0.4f, 0.2f, 1.0f), true
            );

            _presets["Stone"] = new MaterialPreset(
                "Stone",
                "Rough stone surface with minimal reflection",
                6.0f, 0.15f, 0.35f, new Vector4(0.5f, 0.5f, 0.4f, 1.0f), true
            );

            // Educational comparison presets
            _presets["No Specular"] = new MaterialPreset(
                "No Specular",
                "Material with no specular reflection for comparison",
                1.0f, 0.0f, 0.3f, new Vector4(0.7f, 0.7f, 0.7f, 1.0f), true
            );

            _presets["Only Specular"] = new MaterialPreset(
                "Only Specular",
                "Material with very high specular, low ambient for demonstration",
                64.0f, 2.0f, 0.01f, new Vector4(0.1f, 0.1f, 0.1f, 1.0f), true
            );

            _presets["High Ambient"] = new MaterialPreset(
                "High Ambient",
                "Material with very high ambient lighting",
                32.0f, 0.5f, 0.8f, new Vector4(0.8f, 0.8f, 0.8f, 1.0f), true
            );
        }

        /// <summary>
        /// Get information about a specific preset
        /// </summary>
        /// <param name="presetName">Name of the preset</param>
        /// <returns>Preset information or null if not found</returns>
        public MaterialPreset GetPresetInfo(string presetName)
        {
            _presets.TryGetValue(presetName, out MaterialPreset preset);
            return preset;
        }

        /// <summary>
        /// Get a summary of current material properties
        /// </summary>
        /// <returns>Formatted string with material information</returns>
        public string GetMaterialSummary()
        {
            if (_managedMaterials.Count == 0)
            {
                return "No materials managed";
            }

            var firstMaterial = _managedMaterials[0];
            return $"Materials: {_managedMaterials.Count} | " +
                   $"Preset: {_currentPresetName} | " +
                   $"Shininess: {firstMaterial.Shininess:F0} | " +
                   $"Specular: {firstMaterial.SpecularIntensity:F2} | " +
                   $"Ambient: {firstMaterial.AmbientStrength:F2}";
        }

        /// <summary>
        /// Create a material with educational properties for demonstrating specific concepts
        /// </summary>
        /// <param name="concept">Lighting concept to demonstrate</param>
        /// <param name="baseTexture">Base texture to use</param>
        /// <returns>Material configured for the concept</returns>
        public Material CreateEducationalMaterial(string concept, Texture baseTexture = null)
        {
            Material material = new Material(baseTexture);

            switch (concept.ToLower())
            {
                case "ambient":
                    // High ambient, no specular - shows ambient lighting only
                    material.Shininess = 1.0f;
                    material.SpecularIntensity = 0.0f;
                    material.AmbientStrength = 0.8f;
                    material.DiffuseColor = new Vector4(0.7f, 0.7f, 0.7f, 1.0f);
                    break;

                case "diffuse":
                    // Balanced diffuse, low ambient, no specular - shows diffuse shading
                    material.Shininess = 1.0f;
                    material.SpecularIntensity = 0.0f;
                    material.AmbientStrength = 0.05f;
                    material.DiffuseColor = new Vector4(0.8f, 0.8f, 0.8f, 1.0f);
                    break;

                case "specular":
                    // High specular, low ambient - shows specular highlights clearly
                    material.Shininess = 128.0f;
                    material.SpecularIntensity = 2.0f;
                    material.AmbientStrength = 0.02f;
                    material.DiffuseColor = new Vector4(0.3f, 0.3f, 0.3f, 1.0f);
                    break;

                case "shininess":
                    // High shininess for tight specular highlights
                    material.Shininess = 256.0f;
                    material.SpecularIntensity = 1.0f;
                    material.AmbientStrength = 0.1f;
                    material.DiffuseColor = new Vector4(0.7f, 0.7f, 0.7f, 1.0f);
                    break;

                default:
                    // Default educational material
                    ApplyPresetToMaterial(material, _presets["Default"]);
                    break;
            }

            AddMaterial(material);
            return material;
        }

        /// <summary>
        /// Apply different materials to a collection of objects for comparison
        /// </summary>
        /// <param name="materials">Materials to modify</param>
        public void ApplyComparisonMaterials(List<Material> materials)
        {
            if (materials == null || materials.Count == 0) return;

            string[] comparisonPresets = { "Rough", "Default", "Shiny", "Metal" };

            for (int i = 0; i < materials.Count && i < comparisonPresets.Length; i++)
            {
                if (_presets.TryGetValue(comparisonPresets[i], out MaterialPreset preset))
                {
                    ApplyPresetToMaterial(materials[i], preset);
                    AddMaterial(materials[i]);
                }
            }

            Console.WriteLine("Applied comparison materials:");
            for (int i = 0; i < Math.Min(materials.Count, comparisonPresets.Length); i++)
            {
                Console.WriteLine($"  Object {i + 1}: {comparisonPresets[i]}");
            }
        }

        /// <summary>
        /// Reset all managed materials to default values
        /// </summary>
        public void ResetToDefaults()
        {
            ApplyPreset("Default");
        }

        /// <summary>
        /// Save current material properties as a custom preset
        /// </summary>
        /// <param name="name">Name for the custom preset</param>
        /// <param name="description">Description of the preset</param>
        public void SaveCustomPreset(string name, string description = "")
        {
            if (_managedMaterials.Count == 0) return;

            var referenceMaterial = _managedMaterials[0];
            var customPreset = new MaterialPreset(
                name,
                description.Length > 0 ? description : $"Custom preset: {name}",
                referenceMaterial.Shininess,
                referenceMaterial.SpecularIntensity,
                referenceMaterial.AmbientStrength,
                referenceMaterial.DiffuseColor,
                referenceMaterial.UseTextures
            );

            _presets[name] = customPreset;
            _currentPresetName = name;

            Console.WriteLine($"Saved custom preset: {name}");
        }

        /// <summary>
        /// Remove a custom preset
        /// </summary>
        /// <param name="name">Name of the preset to remove</param>
        /// <returns>True if the preset was removed</returns>
        public bool RemovePreset(string name)
        {
            // Don't allow removal of default presets
            string[] protectedPresets = { "Default", "Shiny", "Rough", "Metal", "Plastic", "Rubber" };
            if (protectedPresets.Contains(name))
            {
                Console.WriteLine($"Cannot remove protected preset: {name}");
                return false;
            }

            bool removed = _presets.Remove(name);
            if (removed)
            {
                Console.WriteLine($"Removed preset: {name}");
                if (_currentPresetName == name)
                {
                    _currentPresetName = "Default";
                    ApplyPreset("Default");
                }
            }

            return removed;
        }

        /// <summary>
        /// Dispose of managed resources
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _managedMaterials.Clear();
                _presets.Clear();
                _disposed = true;
                GC.SuppressFinalize(this);
            }
        }

        /// <summary>
        /// Finalizer
        /// </summary>
        ~MaterialManager()
        {
            if (!_disposed)
            {
                Console.WriteLine("WARNING: MaterialManager was not disposed properly.");
            }
        }
    }
}