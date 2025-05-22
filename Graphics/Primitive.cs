using OpenTK.Mathematics;
using System;
using System.Collections.Generic;

namespace MiniRenderer.Graphics
{
    /// <summary>
    /// Utility class for generating primitive 3D shapes
    /// </summary>
    public static class Primitive
    {
        /// <summary>
        /// Create a cube with the specified size
        /// </summary>
        /// <param name="size">Size of the cube</param>
        /// <returns>Vertices and indices for the cube</returns>
        public static (float[] vertices, uint[] indices) CreateCube(float size = 1.0f)
        {
            // Half size for centered cube
            float halfSize = size / 2.0f;

            // Vertex format: position (x,y,z), texture coordinates (u,v), normal (nx,ny,nz), color (r,g,b,a)
            float[] vertices = {
                // Front face (positive Z)
                -halfSize, -halfSize,  halfSize,  0.0f, 0.0f,  0.0f, 0.0f, 1.0f,  1.0f, 1.0f, 1.0f, 1.0f, // 0
                 halfSize, -halfSize,  halfSize,  1.0f, 0.0f,  0.0f, 0.0f, 1.0f,  1.0f, 1.0f, 1.0f, 1.0f, // 1
                 halfSize,  halfSize,  halfSize,  1.0f, 1.0f,  0.0f, 0.0f, 1.0f,  1.0f, 1.0f, 1.0f, 1.0f, // 2
                -halfSize,  halfSize,  halfSize,  0.0f, 1.0f,  0.0f, 0.0f, 1.0f,  1.0f, 1.0f, 1.0f, 1.0f, // 3
                
                // Back face (negative Z)
                -halfSize, -halfSize, -halfSize,  1.0f, 0.0f,  0.0f, 0.0f, -1.0f,  1.0f, 1.0f, 1.0f, 1.0f, // 4
                -halfSize,  halfSize, -halfSize,  1.0f, 1.0f,  0.0f, 0.0f, -1.0f,  1.0f, 1.0f, 1.0f, 1.0f, // 5
                 halfSize,  halfSize, -halfSize,  0.0f, 1.0f,  0.0f, 0.0f, -1.0f,  1.0f, 1.0f, 1.0f, 1.0f, // 6
                 halfSize, -halfSize, -halfSize,  0.0f, 0.0f,  0.0f, 0.0f, -1.0f,  1.0f, 1.0f, 1.0f, 1.0f, // 7
                
                // Top face (positive Y)
                -halfSize,  halfSize, -halfSize,  0.0f, 0.0f,  0.0f, 1.0f, 0.0f,  1.0f, 1.0f, 1.0f, 1.0f, // 8
                -halfSize,  halfSize,  halfSize,  0.0f, 1.0f,  0.0f, 1.0f, 0.0f,  1.0f, 1.0f, 1.0f, 1.0f, // 9
                 halfSize,  halfSize,  halfSize,  1.0f, 1.0f,  0.0f, 1.0f, 0.0f,  1.0f, 1.0f, 1.0f, 1.0f, // 10
                 halfSize,  halfSize, -halfSize,  1.0f, 0.0f,  0.0f, 1.0f, 0.0f,  1.0f, 1.0f, 1.0f, 1.0f, // 11
                
                // Bottom face (negative Y)
                -halfSize, -halfSize, -halfSize,  0.0f, 0.0f,  0.0f, -1.0f, 0.0f,  1.0f, 1.0f, 1.0f, 1.0f, // 12
                 halfSize, -halfSize, -halfSize,  1.0f, 0.0f,  0.0f, -1.0f, 0.0f,  1.0f, 1.0f, 1.0f, 1.0f, // 13
                 halfSize, -halfSize,  halfSize,  1.0f, 1.0f,  0.0f, -1.0f, 0.0f,  1.0f, 1.0f, 1.0f, 1.0f, // 14
                -halfSize, -halfSize,  halfSize,  0.0f, 1.0f,  0.0f, -1.0f, 0.0f,  1.0f, 1.0f, 1.0f, 1.0f, // 15
                
                // Right face (positive X)
                 halfSize, -halfSize, -halfSize,  0.0f, 0.0f,  1.0f, 0.0f, 0.0f,  1.0f, 1.0f, 1.0f, 1.0f, // 16
                 halfSize,  halfSize, -halfSize,  0.0f, 1.0f,  1.0f, 0.0f, 0.0f,  1.0f, 1.0f, 1.0f, 1.0f, // 17
                 halfSize,  halfSize,  halfSize,  1.0f, 1.0f,  1.0f, 0.0f, 0.0f,  1.0f, 1.0f, 1.0f, 1.0f, // 18
                 halfSize, -halfSize,  halfSize,  1.0f, 0.0f,  1.0f, 0.0f, 0.0f,  1.0f, 1.0f, 1.0f, 1.0f, // 19
                
                // Left face (negative X)
                -halfSize, -halfSize, -halfSize,  1.0f, 0.0f,  -1.0f, 0.0f, 0.0f,  1.0f, 1.0f, 1.0f, 1.0f, // 20
                -halfSize, -halfSize,  halfSize,  0.0f, 0.0f,  -1.0f, 0.0f, 0.0f,  1.0f, 1.0f, 1.0f, 1.0f, // 21
                -halfSize,  halfSize,  halfSize,  0.0f, 1.0f,  -1.0f, 0.0f, 0.0f,  1.0f, 1.0f, 1.0f, 1.0f, // 22
                -halfSize,  halfSize, -halfSize,  1.0f, 1.0f,  -1.0f, 0.0f, 0.0f,  1.0f, 1.0f, 1.0f, 1.0f  // 23
            };

            // Indices for the cube (6 faces * 2 triangles * 3 vertices = 36 indices)
            uint[] indices = {
                // Front face
                0, 1, 2,
                2, 3, 0,
                
                // Back face
                4, 5, 6,
                6, 7, 4,
                
                // Top face
                8, 9, 10,
                10, 11, 8,
                
                // Bottom face
                12, 13, 14,
                14, 15, 12,
                
                // Right face
                16, 17, 18,
                18, 19, 16,
                
                // Left face
                20, 21, 22,
                22, 23, 20
            };

            return (vertices, indices);
        }

        /// <summary>
        /// Create a wireframe cube with the specified size
        /// </summary>
        /// <param name="size">Size of the cube</param>
        /// <returns>Vertices and indices for the wireframe cube</returns>
        public static (float[] vertices, uint[] indices) CreateWireframeCube(float size = 1.0f)
        {
            // Half size for centered cube
            float halfSize = size / 2.0f;

            // Vertex format: position (x,y,z), color (r,g,b,a)
            float[] vertices = {
                // Front bottom left
                -halfSize, -halfSize,  halfSize,  1.0f, 1.0f, 1.0f, 1.0f, // 0
                // Front bottom right
                 halfSize, -halfSize,  halfSize,  1.0f, 1.0f, 1.0f, 1.0f, // 1
                // Front top right
                 halfSize,  halfSize,  halfSize,  1.0f, 1.0f, 1.0f, 1.0f, // 2
                // Front top left
                -halfSize,  halfSize,  halfSize,  1.0f, 1.0f, 1.0f, 1.0f, // 3
                // Back bottom left
                -halfSize, -halfSize, -halfSize,  1.0f, 1.0f, 1.0f, 1.0f, // 4
                // Back bottom right
                 halfSize, -halfSize, -halfSize,  1.0f, 1.0f, 1.0f, 1.0f, // 5
                // Back top right
                 halfSize,  halfSize, -halfSize,  1.0f, 1.0f, 1.0f, 1.0f, // 6
                // Back top left
                -halfSize,  halfSize, -halfSize,  1.0f, 1.0f, 1.0f, 1.0f  // 7
            };

            // Indices for the 12 edges of the cube
            uint[] indices = {
                // Front face edges
                0, 1,
                1, 2,
                2, 3,
                3, 0,
                
                // Back face edges
                4, 5,
                5, 6,
                6, 7,
                7, 4,
                
                // Connecting edges
                0, 4,
                1, 5,
                2, 6,
                3, 7
            };

            return (vertices, indices);
        }

        /// <summary>
        /// Create a grid on the XZ plane
        /// </summary>
        /// <param name="width">Width of the grid</param>
        /// <param name="depth">Depth of the grid</param>
        /// <param name="segments">Number of segments in each direction</param>
        /// <returns>Vertices and indices for the grid</returns>
        public static (float[] vertices, uint[] indices) CreateGrid(float width = 10.0f, float depth = 10.0f, int segments = 10)
        {
            // Number of vertices (segments + 1) in each direction
            int verticesX = segments + 1;
            int verticesZ = segments + 1;

            // Calculate step size
            float stepX = width / segments;
            float stepZ = depth / segments;

            // Offset to center the grid
            float offsetX = width / 2.0f;
            float offsetZ = depth / 2.0f;

            // Create vertex array
            float[] vertices = new float[verticesX * verticesZ * 8]; // 8 floats per vertex (position, texcoord, normal)

            // Generate vertices
            for (int z = 0; z < verticesZ; z++)
            {
                for (int x = 0; x < verticesX; x++)
                {
                    // Calculate position
                    float posX = x * stepX - offsetX;
                    float posZ = z * stepZ - offsetZ;

                    // Calculate texture coordinates
                    float u = (float)x / segments;
                    float v = (float)z / segments;

                    // Calculate index in the vertex array
                    int index = (z * verticesX + x) * 8;

                    // Position
                    vertices[index + 0] = posX;
                    vertices[index + 1] = 0.0f;
                    vertices[index + 2] = posZ;

                    // Texture coordinates
                    vertices[index + 3] = u;
                    vertices[index + 4] = v;

                    // Normal (pointing up)
                    vertices[index + 5] = 0.0f;
                    vertices[index + 6] = 1.0f;
                    vertices[index + 7] = 0.0f;
                }
            }

            // Create index array (2 triangles per grid cell, 3 indices per triangle)
            int triangleCount = segments * segments * 2;
            uint[] indices = new uint[triangleCount * 3];

            int indexCount = 0;
            for (int z = 0; z < segments; z++)
            {
                for (int x = 0; x < segments; x++)
                {
                    // Calculate vertex indices
                    uint topLeft = (uint)(z * verticesX + x);
                    uint topRight = topLeft + 1;
                    uint bottomLeft = (uint)((z + 1) * verticesX + x);
                    uint bottomRight = bottomLeft + 1;

                    // First triangle
                    indices[indexCount++] = topLeft;
                    indices[indexCount++] = bottomLeft;
                    indices[indexCount++] = topRight;

                    // Second triangle
                    indices[indexCount++] = topRight;
                    indices[indexCount++] = bottomLeft;
                    indices[indexCount++] = bottomRight;
                }
            }

            return (vertices, indices);
        }
    }
}