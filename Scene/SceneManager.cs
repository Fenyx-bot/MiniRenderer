using OpenTK.Mathematics;
using MiniRenderer.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MiniRenderer.Scene
{
    /// <summary>
    /// MODULE 8 NEW: SceneManager - Manages multiple objects efficiently
    /// This is the main addition for Module 8 - it helps us organize and render many objects
    /// </summary>
    public class SceneManager : IDisposable
    {
        // MODULE 8 NEW: Collection of all objects in our scene
        private List<SceneObject> _objects = new List<SceneObject>();

        // MODULE 8 NEW: Performance settings that students can experiment with
        public bool EnableDistanceCulling { get; set; } = true;
        public float MaxRenderDistance { get; set; } = 50.0f;

        // MODULE 8 NEW: Statistics for learning about performance
        public int TotalObjects => _objects.Count;
        public int RenderedObjects { get; private set; }
        public int CulledObjects { get; private set; }

        // For cleanup
        private bool _disposed = false;

        /// <summary>
        /// Create a new scene manager
        /// </summary>
        public SceneManager()
        {
            Console.WriteLine("MODULE 8: Scene Manager initialized");
            Console.WriteLine("This helps us manage multiple objects efficiently!");
        }

        /// <summary>
        /// MODULE 8 NEW: Add an object to our scene
        /// </summary>
        public void AddObject(SceneObject obj)
        {
            if (obj != null && !_objects.Contains(obj))
            {
                _objects.Add(obj);
                Console.WriteLine($"Added object: {obj.Name} (Total: {_objects.Count})");
            }
        }

        /// <summary>
        /// MODULE 8 NEW: Remove an object from our scene
        /// </summary>
        public bool RemoveObject(SceneObject obj)
        {
            bool removed = _objects.Remove(obj);
            if (removed)
            {
                Console.WriteLine($"Removed object: {obj.Name} (Total: {_objects.Count})");
            }
            return removed;
        }

        /// <summary>
        /// MODULE 8 NEW: Find an object by name
        /// </summary>
        public SceneObject FindObject(string name)
        {
            return _objects.FirstOrDefault(obj => obj.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// MODULE 8 NEW: Get all objects (read-only)
        /// </summary>
        public IReadOnlyList<SceneObject> GetAllObjects()
        {
            return _objects.AsReadOnly();
        }

        /// <summary>
        /// MODULE 8 NEW: Update all objects in the scene
        /// This calls Update() on each object, which handles animations
        /// </summary>
        public void Update(float deltaTime)
        {
            foreach (var obj in _objects)
            {
                obj.Update(deltaTime);
            }
        }

        /// <summary>
        /// MODULE 8 NEW: Render all objects with basic performance optimizations
        /// This is where we see the benefit of scene management
        /// </summary>
        public void Render(Shader shader, Vector3 cameraPosition)
        {
            RenderedObjects = 0;
            CulledObjects = 0;

            foreach (var obj in _objects)
            {
                // MODULE 8 NEW: Simple distance culling for performance
                // Only render objects that are close enough to see
                if (EnableDistanceCulling && !obj.ShouldRender(cameraPosition, MaxRenderDistance))
                {
                    CulledObjects++;
                    continue; // Skip this object
                }

                // Render the object
                obj.Render(shader);
                RenderedObjects++;
            }
        }

        /// <summary>
        /// MODULE 8 NEW: Create a demonstration scene with multiple objects
        /// This shows how we can easily manage many objects
        /// </summary>
        public void CreateDemoScene(Model carModel = null)
        {
            Console.WriteLine("\n=== Creating Module 8 Demo Scene ===");

            // Add the main car in the center (if we have one)
            if (carModel != null)
            {
                var centerCar = new SceneObject(carModel, "Center Car");
                centerCar.Position = new Vector3(0, 0, 0);
                centerCar.AutoRotate = true;
                centerCar.RotationSpeed = new Vector3(0, 45, 0); // Rotate around Y axis
                AddObject(centerCar);
            }

            // MODULE 8 NEW: Create a grid of cubes to show off our scene management
            CreateCubeGrid();

            // MODULE 8 NEW: Create some orbiting objects
            CreateOrbitingObjects();

            Console.WriteLine($"Demo scene complete! Total objects: {TotalObjects}");
        }

        /// <summary>
        /// MODULE 8 NEW: Create a grid of cube objects
        /// This demonstrates how easy it is to manage multiple similar objects
        /// </summary>
        private void CreateCubeGrid()
        {
            Console.WriteLine("Creating cube grid...");

            int gridSize = 5;
            float spacing = 4.0f;

            for (int x = 0; x < gridSize; x++)
            {
                for (int z = 0; z < gridSize; z++)
                {
                    // Create a cube mesh for each grid position
                    var cube = Mesh.CreateCube(0.8f);

                    // Create different colored materials for variety
                    var material = Material.CreateColored(new Vector4(
                        (float)x / (gridSize - 1), // Red varies with X
                        0.5f,                       // Green is constant
                        (float)z / (gridSize - 1), // Blue varies with Z
                        1.0f                        // Full opacity
                    ));
                    cube.SetMaterial(material, true);

                    // Create a scene object from the mesh
                    var sceneObj = new SceneObject(cube, $"GridCube_{x}_{z}");

                    // Position it in the grid
                    sceneObj.Position = new Vector3(
                        (x - gridSize / 2) * spacing,
                        1.0f,
                        (z - gridSize / 2) * spacing + 10.0f // Offset from center
                    );

                    // Give each cube different rotation for visual interest
                    sceneObj.AutoRotate = true;
                    sceneObj.RotationSpeed = new Vector3(
                        30 + x * 10,  // Vary rotation speed
                        45 + z * 15,
                        0
                    );

                    AddObject(sceneObj);
                }
            }
        }

        /// <summary>
        /// MODULE 8 NEW: Create some objects that orbit around the center
        /// This shows off animation and demonstrates why scene management is useful
        /// </summary>
        private void CreateOrbitingObjects()
        {
            Console.WriteLine("Creating orbiting objects...");

            int numOrbiters = 6;
            float orbitRadius = 8.0f;

            for (int i = 0; i < numOrbiters; i++)
            {
                // Create a small cube
                var cube = Mesh.CreateCube(0.5f);

                // Give it a unique color
                var hue = (float)i / numOrbiters; // 0 to 1
                var material = Material.CreateColored(new Vector4(
                    (float)Math.Sin(hue * Math.PI * 2) * 0.5f + 0.5f,
                    (float)Math.Sin(hue * Math.PI * 2 + Math.PI * 2 / 3) * 0.5f + 0.5f,
                    (float)Math.Sin(hue * Math.PI * 2 + Math.PI * 4 / 3) * 0.5f + 0.5f,
                    1.0f
                ));
                cube.SetMaterial(material, true);

                var orbiter = new SceneObject(cube, $"Orbiter_{i}");

                // Position in a circle around the center
                float angle = (float)(2 * Math.PI * i / numOrbiters);
                orbiter.Position = new Vector3(
                    (float)Math.Sin(angle) * orbitRadius,
                    2.0f + (float)Math.Sin(i) * 1.0f, // Vary height slightly
                    (float)Math.Cos(angle) * orbitRadius
                );

                // Make them spin
                orbiter.AutoRotate = true;
                orbiter.RotationSpeed = new Vector3(
                    90 + i * 30,   // Fast spinning
                    180 + i * 45,
                    45
                );

                AddObject(orbiter);
            }
        }

        /// <summary>
        /// MODULE 8 NEW: Toggle distance culling on/off
        /// This lets students see the performance difference
        /// </summary>
        public void ToggleDistanceCulling()
        {
            EnableDistanceCulling = !EnableDistanceCulling;
            Console.WriteLine($"Distance culling: {EnableDistanceCulling}");
        }

        /// <summary>
        /// MODULE 8 NEW: Adjust the maximum render distance
        /// Students can experiment with this to see culling in action
        /// </summary>
        public void AdjustRenderDistance(float delta)
        {
            MaxRenderDistance = Math.Max(5.0f, MaxRenderDistance + delta);
            Console.WriteLine($"Max render distance: {MaxRenderDistance:F1}");
        }

        /// <summary>
        /// MODULE 8 NEW: Get performance information
        /// This helps students understand what's happening
        /// </summary>
        public string GetPerformanceInfo()
        {
            return $"Objects: {TotalObjects} | Rendered: {RenderedObjects} | Culled: {CulledObjects} | Distance Culling: {EnableDistanceCulling} | Max Distance: {MaxRenderDistance:F0}";
        }

        /// <summary>
        /// MODULE 8 NEW: Clear all objects
        /// </summary>
        public void Clear()
        {
            foreach (var obj in _objects)
            {
                obj.Dispose();
            }
            _objects.Clear();
            Console.WriteLine("Scene cleared");
        }

        /// <summary>
        /// MODULE 8 NEW: Print scene information for debugging
        /// </summary>
        public void PrintSceneInfo()
        {
            Console.WriteLine($"\n=== Scene Info ===");
            Console.WriteLine($"Total Objects: {TotalObjects}");
            Console.WriteLine($"Distance Culling: {EnableDistanceCulling}");
            Console.WriteLine($"Max Render Distance: {MaxRenderDistance:F1}");

            if (_objects.Count > 0)
            {
                Console.WriteLine("Objects:");
                foreach (var obj in _objects)
                {
                    Console.WriteLine($"  - {obj.Name} at {obj.Position}");
                }
            }
            Console.WriteLine("==================");
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                Clear();
                _disposed = true;
                GC.SuppressFinalize(this);
            }
        }
    }
}