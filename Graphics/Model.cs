using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.IO;

namespace MiniRenderer.Graphics
{
    /// <summary>
    /// Represents a 3D model loaded from an OBJ file
    /// </summary>
    public class Model : IDisposable
    {
        // The mesh that contains the geometry data
        public Mesh Mesh { get; private set; }

        // Model transformation properties
        public Vector3 Position { get; set; } = Vector3.Zero;
        public Vector3 Rotation { get; set; } = Vector3.Zero;
        public Vector3 Scale { get; set; } = Vector3.One;

        // Model information
        public string Name { get; set; }
        public string FilePath { get; private set; }

        // Bounding box information (useful for culling and positioning)
        public Vector3 BoundingBoxMin { get; private set; }
        public Vector3 BoundingBoxMax { get; private set; }
        public Vector3 BoundingBoxCenter => (BoundingBoxMin + BoundingBoxMax) * 0.5f;
        public Vector3 BoundingBoxSize => BoundingBoxMax - BoundingBoxMin;

        // Flag for proper resource disposal
        private bool _disposed = false;

        /// <summary>
        /// Create a model from an OBJ file
        /// </summary>
        /// <param name="filePath">Path to the OBJ file</param>
        /// <param name="defaultMaterial">Optional default material to apply if no texture found</param>
        public Model(string filePath, Material defaultMaterial = null)
        {
            FilePath = filePath;
            Name = Path.GetFileNameWithoutExtension(filePath);

            LoadFromObj(filePath, defaultMaterial);
        }

        /// <summary>
        /// Create a model from existing mesh data
        /// </summary>
        /// <param name="mesh">The mesh containing geometry data</param>
        /// <param name="name">Name for the model</param>
        public Model(Mesh mesh, string name = "CustomModel")
        {
            Mesh = mesh;
            Name = name;
            FilePath = "";

            // Calculate bounding box from mesh (this is a simplified version)
            CalculateBoundingBox();
        }

        /// <summary>
        /// Load model data from an OBJ file and auto-detect PNG textures
        /// </summary>
        private void LoadFromObj(string filePath, Material defaultMaterial)
        {
            try
            {
                Console.WriteLine($"Loading model: {filePath}");

                // Load OBJ data (no MTL support)
                var objData = ObjLoader.LoadObj(filePath);

                // Calculate normals if they don't exist
                if (objData.Normals.Count == 0)
                {
                    Console.WriteLine("No normals found in OBJ file, calculating them...");
                    ObjLoader.CalculateNormals(objData);
                }

                // Convert to mesh format
                var (vertices, indices) = ObjLoader.ConvertToMeshData(objData);

                // Create mesh
                Mesh = new Mesh(vertices, indices, 12, true, true, true); // 12 floats per vertex: pos(3) + texcoord(2) + normal(3) + color(4)

                // Auto-detect and load PNG textures from the same directory
                Material appliedMaterial = AutoDetectTextures(filePath);

                // Apply the material or use default
                if (appliedMaterial != null)
                {
                    Mesh.SetMaterial(appliedMaterial, true); // true = mesh owns the material
                }
                else if (defaultMaterial != null)
                {
                    Mesh.SetMaterial(defaultMaterial);
                    Console.WriteLine("Applied default material (no PNG textures found)");
                }

                // Calculate bounding box
                CalculateBoundingBoxFromObjData(objData);

                Console.WriteLine($"Model loaded successfully: {Name}");
                Console.WriteLine($"  Vertices: {vertices.Length / 12}");
                Console.WriteLine($"  Triangles: {indices.Length / 3}");
                Console.WriteLine($"  Bounding box: ({BoundingBoxMin.X:F2}, {BoundingBoxMin.Y:F2}, {BoundingBoxMin.Z:F2}) to ({BoundingBoxMax.X:F2}, {BoundingBoxMax.Y:F2}, {BoundingBoxMax.Z:F2})");
                Console.WriteLine($"  Size: {BoundingBoxSize.X:F2} x {BoundingBoxSize.Y:F2} x {BoundingBoxSize.Z:F2}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading model {filePath}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Auto-detect and load PNG textures from the same directory as the OBJ file
        /// </summary>
        private Material AutoDetectTextures(string objFilePath)
        {
            string directory = Path.GetDirectoryName(objFilePath);

            Console.WriteLine($"Auto-detecting textures in: {directory}");

            // Look for PNG files in the same directory
            string[] pngFiles = Directory.GetFiles(directory, "*.png");

            if (pngFiles.Length == 0)
            {
                // Also try JPG files
                string[] jpgFiles = Directory.GetFiles(directory, "*.jpg");
                if (jpgFiles.Length > 0)
                {
                    pngFiles = jpgFiles;
                    Console.WriteLine($"Found {jpgFiles.Length} JPG texture(s)");
                }
                else
                {
                    Console.WriteLine("No PNG or JPG texture files found");
                    return null;
                }
            }
            else
            {
                Console.WriteLine($"Found {pngFiles.Length} PNG texture(s)");
            }

            // Try to find the main texture file (look for common naming patterns)
            string[] preferredNames = {
                "car.png", "car.jpg",
                "texture.png", "texture.jpg",
                "diffuse.png", "diffuse.jpg",
                "color.png", "color.jpg"
            };

            string mainTexture = null;

            // First, try preferred names
            foreach (string preferredName in preferredNames)
            {
                string fullPath = Path.Combine(directory, preferredName);
                if (File.Exists(fullPath))
                {
                    mainTexture = fullPath;
                    break;
                }
            }

            // If no preferred name found, use the first PNG/JPG file
            if (mainTexture == null && pngFiles.Length > 0)
            {
                mainTexture = pngFiles[0];
            }

            if (mainTexture != null)
            {
                try
                {
                    Console.WriteLine($"✓ Loading texture: {Path.GetFileName(mainTexture)}");

                    var texture = new Texture(mainTexture);
                    var material = new Material(texture, null, true); // true = material owns texture

                    // Set reasonable material properties
                    material.Shininess = 32.0f;
                    material.SpecularIntensity = 0.5f;
                    material.AmbientStrength = 0.1f;
                    material.UseTextures = true;

                    Console.WriteLine($"✓ Created material with texture: {Path.GetFileName(mainTexture)}");
                    return material;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"✗ Error loading texture {mainTexture}: {ex.Message}");
                    return null;
                }
            }

            return null;
        }

        /// <summary>
        /// Calculate bounding box from OBJ data
        /// </summary>
        private void CalculateBoundingBoxFromObjData(ObjLoader.ObjData objData)
        {
            if (objData.Vertices.Count == 0)
            {
                BoundingBoxMin = Vector3.Zero;
                BoundingBoxMax = Vector3.Zero;
                return;
            }

            BoundingBoxMin = objData.Vertices[0];
            BoundingBoxMax = objData.Vertices[0];

            foreach (var vertex in objData.Vertices)
            {
                BoundingBoxMin = Vector3.ComponentMin(BoundingBoxMin, vertex);
                BoundingBoxMax = Vector3.ComponentMax(BoundingBoxMax, vertex);
            }
        }

        /// <summary>
        /// Calculate bounding box (simplified version for when we don't have OBJ data)
        /// </summary>
        private void CalculateBoundingBox()
        {
            // This is a simplified version - in a real implementation you'd extract
            // vertex positions from the mesh data
            BoundingBoxMin = new Vector3(-1.0f);
            BoundingBoxMax = new Vector3(1.0f);
        }

        /// <summary>
        /// Render the model using the specified shader
        /// </summary>
        /// <param name="shader">Shader to use for rendering</param>
        public void Render(Shader shader)
        {
            // Set model transformation matrix
            shader.SetMatrix4("uModel", GetModelMatrix());

            // Render the mesh
            Mesh?.Render(shader);
        }

        /// <summary>
        /// Get the model transformation matrix
        /// </summary>
        /// <returns>The model transformation matrix</returns>
        public Matrix4 GetModelMatrix()
        {
            // Create transformation matrices
            Matrix4 translation = Matrix4.CreateTranslation(Position);
            Matrix4 rotationX = Matrix4.CreateRotationX(MathHelper.DegreesToRadians(Rotation.X));
            Matrix4 rotationY = Matrix4.CreateRotationY(MathHelper.DegreesToRadians(Rotation.Y));
            Matrix4 rotationZ = Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(Rotation.Z));
            Matrix4 scale = Matrix4.CreateScale(Scale);

            // Combine transformations (scale first, then rotate, then translate)
            return scale * rotationX * rotationY * rotationZ * translation;
        }

        /// <summary>
        /// Set the model's material
        /// </summary>
        /// <param name="material">Material to apply</param>
        /// <param name="ownsMaterial">Whether the model should own and dispose the material</param>
        public void SetMaterial(Material material, bool ownsMaterial = false)
        {
            Mesh?.SetMaterial(material, ownsMaterial);
        }

        /// <summary>
        /// Center the model at the origin by adjusting its position
        /// </summary>
        public void CenterAtOrigin()
        {
            Position = -BoundingBoxCenter;
        }

        /// <summary>
        /// Scale the model to fit within the specified size
        /// </summary>
        /// <param name="targetSize">Target size (the model will fit within a cube of this size)</param>
        public void ScaleToFit(float targetSize)
        {
            float maxDimension = Math.Max(Math.Max(BoundingBoxSize.X, BoundingBoxSize.Y), BoundingBoxSize.Z);
            if (maxDimension > 0)
            {
                float scaleFactor = targetSize / maxDimension;
                Scale = new Vector3(scaleFactor);
            }
        }

        /// <summary>
        /// Create a simple cube model
        /// </summary>
        /// <param name="size">Size of the cube</param>
        /// <param name="material">Material to apply</param>
        /// <returns>A cube model</returns>
        public static Model CreateCube(float size = 1.0f, Material material = null)
        {
            var cubeMesh = Mesh.CreateCube(size);
            if (material != null)
            {
                cubeMesh.SetMaterial(material);
            }

            var model = new Model(cubeMesh, "Cube");
            model.BoundingBoxMin = new Vector3(-size * 0.5f);
            model.BoundingBoxMax = new Vector3(size * 0.5f);

            return model;
        }

        /// <summary>
        /// Dispose of model resources
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                Mesh?.Dispose();
                _disposed = true;
                GC.SuppressFinalize(this);
            }
        }

        /// <summary>
        /// Finalizer
        /// </summary>
        ~Model()
        {
            if (!_disposed)
            {
                Console.WriteLine($"WARNING: Model '{Name}' was not disposed properly.");
            }
        }
    }
}