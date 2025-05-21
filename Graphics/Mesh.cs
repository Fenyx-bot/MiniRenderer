using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;

namespace MiniRenderer.Graphics
{
    /// <summary>
    /// Represents a 3D mesh with vertices, indices, materials, and transformation properties
    /// </summary>
    public class Mesh : IDisposable
    {
        // OpenGL objects
        private VertexArray _vao;
        private VertexBuffer _vbo;
        private ElementBuffer _ebo;

        // Mesh data
        private readonly int _vertexCount;
        private readonly int _indexCount;

        // Render mode (triangles, lines, etc.)
        public PrimitiveType RenderMode { get; set; }

        // Transformation properties
        public Vector3 Position { get; set; }
        public Vector3 Rotation { get; set; }
        public Vector3 Scale { get; set; }

        // Material properties
        public Material Material { get; set; }

        // Texture coordinates scaling for tiling textures
        public Vector2 TextureScale { get; set; } = Vector2.One;
        public Vector2 TextureOffset { get; set; } = Vector2.Zero;

        // Flag for proper resource disposal
        private bool _disposed = false;
        private bool _ownsMaterial = false;

        /// <summary>
        /// Create a new mesh from vertex and index data
        /// </summary>
        /// <param name="vertices">Array of vertex data</param>
        /// <param name="indices">Array of indices</param>
        /// <param name="vertexSize">Number of floats per vertex</param>
        /// <param name="hasTextureCoords">Whether vertices include texture coordinates</param>
        /// <param name="hasNormals">Whether vertices include normals</param>
        /// <param name="hasColors">Whether vertices include colors</param>
        /// <param name="renderMode">Rendering mode (triangles, lines, etc.)</param>
        public Mesh(float[] vertices, uint[] indices, int vertexSize,
                    bool hasTextureCoords = true, bool hasNormals = true, bool hasColors = true,
                    PrimitiveType renderMode = PrimitiveType.Triangles)
        {
            // Store counts
            _vertexCount = vertices.Length / vertexSize;
            _indexCount = indices.Length;

            // Set render mode
            RenderMode = renderMode;

            // Default transformation properties
            Position = Vector3.Zero;
            Rotation = Vector3.Zero;
            Scale = Vector3.One;

            // Default material - white diffuse
            Material = new Material();
            _ownsMaterial = true;

            // Create VAO
            _vao = new VertexArray();
            _vao.Bind();

            // Create VBO and upload vertex data
            _vbo = new VertexBuffer();
            _vbo.Bind();
            _vbo.SetData(vertices);

            // Create EBO and upload index data
            _ebo = new ElementBuffer();
            _ebo.Bind();
            _ebo.SetData(indices);

            // Set up vertex attributes
            int stride = vertexSize * sizeof(float);
            int offset = 0;
            int index = 0;

            // Position attribute (always present, 3 floats)
            GL.VertexAttribPointer(index, 3, VertexAttribPointerType.Float, false, stride, offset);
            GL.EnableVertexAttribArray(index);
            offset += 3 * sizeof(float);
            index++;

            // Texture coordinate attribute (2 floats)
            if (hasTextureCoords)
            {
                GL.VertexAttribPointer(index, 2, VertexAttribPointerType.Float, false, stride, offset);
                GL.EnableVertexAttribArray(index);
                offset += 2 * sizeof(float);
                index++;
            }

            // Normal attribute (3 floats)
            if (hasNormals)
            {
                GL.VertexAttribPointer(index, 3, VertexAttribPointerType.Float, false, stride, offset);
                GL.EnableVertexAttribArray(index);
                offset += 3 * sizeof(float);
                index++;
            }

            // Color attribute (4 floats)
            if (hasColors)
            {
                GL.VertexAttribPointer(index, 4, VertexAttribPointerType.Float, false, stride, offset);
                GL.EnableVertexAttribArray(index);
                offset += 4 * sizeof(float);
                index++;
            }

            // Unbind VAO
            _vao.Unbind();
        }

        /// <summary>
        /// Set the material for this mesh
        /// </summary>
        /// <param name="material">The material to use</param>
        /// <param name="ownsMaterial">Whether this mesh owns and should dispose the material</param>
        public void SetMaterial(Material material, bool ownsMaterial = false)
        {
            // Clean up existing material if we own it
            if (_ownsMaterial)
            {
                Material?.Dispose();
            }

            Material = material;
            _ownsMaterial = ownsMaterial;
        }

        /// <summary>
        /// Create a cube mesh
        /// </summary>
        /// <param name="size">Size of the cube</param>
        /// <param name="wireframe">Whether to create a wireframe cube</param>
        /// <returns>A new mesh representing a cube</returns>
        public static Mesh CreateCube(float size = 1.0f, bool wireframe = false)
        {
            if (wireframe)
            {
                // Create a wireframe cube
                var (vertices, indices) = Primitive.CreateWireframeCube(size);
                return new Mesh(vertices, indices, 7, false, false, true, PrimitiveType.Lines);
            }
            else
            {
                // Create a solid cube
                var (vertices, indices) = Primitive.CreateCube(size);
                return new Mesh(vertices, indices, 12);
            }
        }

        /// <summary>
        /// Create a grid mesh
        /// </summary>
        /// <param name="width">Width of the grid</param>
        /// <param name="depth">Depth of the grid</param>
        /// <param name="segments">Number of segments in each direction</param>
        /// <returns>A new mesh representing a grid</returns>
        public static Mesh CreateGrid(float width = 10.0f, float depth = 10.0f, int segments = 10)
        {
            var (vertices, indices) = Primitive.CreateGrid(width, depth, segments);
            return new Mesh(vertices, indices, 8, true, true, false);
        }

        /// <summary>
        /// Render the mesh using the specified shader
        /// </summary>
        /// <param name="shader">Shader to use for rendering</param>
        public void Render(Shader shader)
        {
            // Use the shader
            shader.Use();

            // Set model matrix
            shader.SetMatrix4("uModel", GetModelMatrix());

            // Set texture tiling parameters
            shader.SetVector2("uTextureScale", TextureScale);
            shader.SetVector2("uTextureOffset", TextureOffset);

            // Apply material (this sets up textures and material properties)
            Material?.Apply(shader);

            // Bind VAO and draw
            _vao.Bind();
            GL.DrawElements(RenderMode, _indexCount, DrawElementsType.UnsignedInt, 0);
            _vao.Unbind();
        }

        /// <summary>
        /// Get the model matrix based on position, rotation, and scale
        /// </summary>
        /// <returns>Model matrix</returns>
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
        /// Dispose of mesh resources
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _vao?.Dispose();
                _vbo?.Dispose();
                _ebo?.Dispose();

                // Dispose of material if we own it
                if (_ownsMaterial)
                {
                    Material?.Dispose();
                }

                _disposed = true;
                GC.SuppressFinalize(this);
            }
        }

        /// <summary>
        /// Finalizer
        /// </summary>
        ~Mesh()
        {
            if (!_disposed)
            {
                Console.WriteLine("WARNING: Mesh was not disposed properly.");
            }
        }
    }
}