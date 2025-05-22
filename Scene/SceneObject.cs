using OpenTK.Mathematics;
using MiniRenderer.Graphics;
using System;

namespace MiniRenderer.Scene
{
    /// <summary>
    /// MODULE 8 NEW: A wrapper around Model/Mesh that adds scene management features
    /// This is our basic building block for managing multiple objects in a scene
    /// </summary>
    public class SceneObject : IDisposable
    {
        // Basic properties - same as before but now organized in a scene object
        public string Name { get; set; } = "SceneObject";
        public bool IsVisible { get; set; } = true;

        // The actual 3D content - we can have either a Model OR a Mesh
        public Model Model { get; set; }
        public Mesh Mesh { get; set; }

        // Transform properties - these override the Model/Mesh transforms when rendering
        public Vector3 Position { get; set; } = Vector3.Zero;
        public Vector3 Rotation { get; set; } = Vector3.Zero;
        public Vector3 Scale { get; set; } = Vector3.One;

        // MODULE 8 NEW: Simple animation support
        public bool AutoRotate { get; set; } = false;
        public Vector3 RotationSpeed { get; set; } = new Vector3(0, 30, 0); // degrees per second

        // For cleanup
        private bool _disposed = false;

        /// <summary>
        /// Create a scene object from a Model
        /// </summary>
        public SceneObject(Model model, string name = null)
        {
            Model = model;
            Name = name ?? model?.Name ?? "SceneObject";

            // Copy initial transform from the model
            if (model != null)
            {
                Position = model.Position;
                Rotation = model.Rotation;
                Scale = model.Scale;
            }
        }

        /// <summary>
        /// Create a scene object from a Mesh
        /// </summary>
        public SceneObject(Mesh mesh, string name = null)
        {
            Mesh = mesh;
            Name = name ?? "SceneObject";

            // Copy initial transform from the mesh
            if (mesh != null)
            {
                Position = mesh.Position;
                Rotation = mesh.Rotation;
                Scale = mesh.Scale;
            }
        }

        /// <summary>
        /// MODULE 8 NEW: Update the object (handle animations)
        /// This is called every frame by the SceneManager
        /// </summary>
        public void Update(float deltaTime)
        {
            // Simple auto-rotation animation
            if (AutoRotate)
            {
                Rotation += RotationSpeed * deltaTime;

                // Keep rotation values reasonable (0-360 degrees)
                Rotation = new Vector3(
                    Rotation.X % 360.0f,
                    Rotation.Y % 360.0f,
                    Rotation.Z % 360.0f
                );
            }
        }

        /// <summary>
        /// Render this scene object
        /// The SceneObject manages the transform, then delegates to Model or Mesh for actual rendering
        /// </summary>
        public void Render(Shader shader)
        {
            if (!IsVisible) return;

            // Render either the Model or Mesh, whichever we have
            if (Model != null)
            {
                // Apply our SceneObject transforms to the Model
                Model.Position = Position;
                Model.Rotation = Rotation;
                Model.Scale = Scale;

                Model.Render(shader);
            }
            else if (Mesh != null)
            {
                // Apply our SceneObject transforms to the Mesh
                Mesh.Position = Position;
                Mesh.Rotation = Rotation;
                Mesh.Scale = Scale;

                Mesh.Render(shader);
            }
        }

        /// <summary>
        /// MODULE 8 NEW: Simple distance-based culling
        /// Returns true if this object should be rendered based on distance from camera
        /// </summary>
        public bool ShouldRender(Vector3 cameraPosition, float maxDistance)
        {
            if (!IsVisible) return false;

            float distance = Vector3.Distance(Position, cameraPosition);
            return distance <= maxDistance;
        }

        /// <summary>
        /// Create a copy of this SceneObject (useful for creating multiple similar objects)
        /// </summary>
        public SceneObject Clone()
        {
            SceneObject clone;

            if (Model != null)
            {
                clone = new SceneObject(Model, Name + "_Clone");
            }
            else
            {
                clone = new SceneObject(Mesh, Name + "_Clone");
            }

            // Copy all properties
            clone.Position = Position;
            clone.Rotation = Rotation;
            clone.Scale = Scale;
            clone.AutoRotate = AutoRotate;
            clone.RotationSpeed = RotationSpeed;
            clone.IsVisible = IsVisible;

            return clone;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                // Note: We don't dispose Model/Mesh here because they might be shared
                // The SceneManager will handle disposal of shared resources
                _disposed = true;
                GC.SuppressFinalize(this);
            }
        }

        public override string ToString()
        {
            return $"SceneObject: {Name} at {Position}";
        }
    }
}