using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace MiniRenderer.Graphics
{
    /// <summary>
    /// Simple OBJ file loader for loading 3D models
    /// </summary>
    public static class ObjLoader
    {
        /// <summary>
        /// Represents the raw data from an OBJ file
        /// </summary>
        public class ObjData
        {
            public List<Vector3> Vertices { get; set; } = new List<Vector3>();
            public List<Vector2> TextureCoords { get; set; } = new List<Vector2>();
            public List<Vector3> Normals { get; set; } = new List<Vector3>();
            public List<Face> Faces { get; set; } = new List<Face>();
            public List<string> MaterialLibraries { get; set; } = new List<string>();
            public Dictionary<string, List<Face>> Groups { get; set; } = new Dictionary<string, List<Face>>();
        }

        /// <summary>
        /// Represents a face in the OBJ file
        /// </summary>
        public class Face
        {
            public List<FaceVertex> Vertices { get; set; } = new List<FaceVertex>();
            public string MaterialName { get; set; } = "";
        }

        /// <summary>
        /// Represents a vertex reference in a face
        /// </summary>
        public struct FaceVertex
        {
            public int VertexIndex;
            public int TextureIndex;
            public int NormalIndex;

            public FaceVertex(int vertexIndex, int textureIndex = -1, int normalIndex = -1)
            {
                VertexIndex = vertexIndex;
                TextureIndex = textureIndex;
                NormalIndex = normalIndex;
            }
        }

        /// <summary>
        /// Load an OBJ file and return the raw data
        /// </summary>
        /// <param name="filePath">Path to the OBJ file</param>
        /// <returns>OBJ data structure</returns>
        public static ObjData LoadObj(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"OBJ file not found: {filePath}");
            }

            var objData = new ObjData();
            string currentMaterial = "";
            string currentGroup = "default";

            Console.WriteLine($"Loading OBJ file: {filePath}");

            string[] lines = File.ReadAllLines(filePath);
            int lineNumber = 0;

            foreach (string line in lines)
            {
                lineNumber++;
                string trimmedLine = line.Trim();

                // Skip empty lines and comments
                if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("#"))
                    continue;

                string[] parts = trimmedLine.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0)
                    continue;

                try
                {
                    switch (parts[0].ToLower())
                    {
                        case "v": // Vertex position
                            if (parts.Length >= 4)
                            {
                                float x = ParseFloat(parts[1]);
                                float y = ParseFloat(parts[2]);
                                float z = ParseFloat(parts[3]);
                                objData.Vertices.Add(new Vector3(x, y, z));
                            }
                            break;

                        case "vt": // Texture coordinate
                            if (parts.Length >= 3)
                            {
                                float u = ParseFloat(parts[1]);
                                float v = ParseFloat(parts[2]);
                                // DON'T flip V coordinate here - let the texture loader handle it
                                objData.TextureCoords.Add(new Vector2(u, v));
                            }
                            break;

                        case "vn": // Normal vector
                            if (parts.Length >= 4)
                            {
                                float x = ParseFloat(parts[1]);
                                float y = ParseFloat(parts[2]);
                                float z = ParseFloat(parts[3]);
                                objData.Normals.Add(new Vector3(x, y, z));
                            }
                            break;

                        case "f": // Face
                            var face = ParseFace(parts);
                            face.MaterialName = currentMaterial;
                            objData.Faces.Add(face);

                            // Add to current group
                            if (!objData.Groups.ContainsKey(currentGroup))
                            {
                                objData.Groups[currentGroup] = new List<Face>();
                            }
                            objData.Groups[currentGroup].Add(face);
                            break;

                        case "mtllib": // Material library (ignore but log)
                            if (parts.Length >= 2)
                            {
                                objData.MaterialLibraries.Add(parts[1]);
                                Console.WriteLine($"Ignoring MTL reference: {parts[1]} (will auto-detect PNG instead)");
                            }
                            break;

                        case "usemtl": // Use material (ignore but log)
                            if (parts.Length >= 2)
                            {
                                currentMaterial = parts[1];
                                Console.WriteLine($"Ignoring material reference: {parts[1]}");
                            }
                            break;

                        case "g": // Group
                        case "o": // Object
                            if (parts.Length >= 2)
                            {
                                currentGroup = parts[1];
                            }
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Error parsing line {lineNumber} in {filePath}: {ex.Message}");
                    Console.WriteLine($"Line content: {trimmedLine}");
                }
            }

            Console.WriteLine($"Loaded OBJ: {objData.Vertices.Count} vertices, {objData.TextureCoords.Count} texture coords, {objData.Normals.Count} normals, {objData.Faces.Count} faces");

            return objData;
        }

        /// <summary>
        /// Parse a float value using invariant culture
        /// </summary>
        private static float ParseFloat(string value)
        {
            return float.Parse(value, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Parse a face definition
        /// </summary>
        private static Face ParseFace(string[] parts)
        {
            var face = new Face();

            // Process each vertex in the face (skip the 'f' command)
            for (int i = 1; i < parts.Length; i++)
            {
                string vertexDef = parts[i];
                var faceVertex = ParseFaceVertex(vertexDef);
                face.Vertices.Add(faceVertex);
            }

            return face;
        }

        /// <summary>
        /// Parse a face vertex definition (v/vt/vn format)
        /// </summary>
        private static FaceVertex ParseFaceVertex(string vertexDef)
        {
            string[] indices = vertexDef.Split('/');

            int vertexIndex = -1;
            int textureIndex = -1;
            int normalIndex = -1;

            // Parse vertex index (always present)
            if (indices.Length >= 1 && !string.IsNullOrEmpty(indices[0]))
            {
                vertexIndex = int.Parse(indices[0]) - 1; // OBJ uses 1-based indexing
            }

            // Parse texture coordinate index (optional)
            if (indices.Length >= 2 && !string.IsNullOrEmpty(indices[1]))
            {
                textureIndex = int.Parse(indices[1]) - 1; // OBJ uses 1-based indexing
            }

            // Parse normal index (optional)
            if (indices.Length >= 3 && !string.IsNullOrEmpty(indices[2]))
            {
                normalIndex = int.Parse(indices[2]) - 1; // OBJ uses 1-based indexing
            }

            return new FaceVertex(vertexIndex, textureIndex, normalIndex);
        }

        /// <summary>
        /// Convert OBJ data to mesh-friendly format (vertices and indices)
        /// </summary>
        /// <param name="objData">The loaded OBJ data</param>
        /// <returns>Tuple of vertex array and index array</returns>
        public static (float[] vertices, uint[] indices) ConvertToMeshData(ObjData objData)
        {
            var vertices = new List<float>();
            var indices = new List<uint>();
            var vertexMap = new Dictionary<string, uint>();
            uint currentIndex = 0;

            foreach (var face in objData.Faces)
            {
                // Triangulate the face (assumes convex faces)
                for (int i = 1; i < face.Vertices.Count - 1; i++)
                {
                    // Create a triangle from vertices 0, i, i+1
                    var v1 = face.Vertices[0];
                    var v2 = face.Vertices[i];
                    var v3 = face.Vertices[i + 1];

                    // Add each vertex of the triangle
                    indices.Add(AddVertex(v1, objData, vertices, vertexMap, ref currentIndex));
                    indices.Add(AddVertex(v2, objData, vertices, vertexMap, ref currentIndex));
                    indices.Add(AddVertex(v3, objData, vertices, vertexMap, ref currentIndex));
                }
            }

            return (vertices.ToArray(), indices.ToArray());
        }

        /// <summary>
        /// Add a vertex to the vertex list, reusing existing vertices when possible
        /// </summary>
        private static uint AddVertex(FaceVertex faceVertex, ObjData objData, List<float> vertices, Dictionary<string, uint> vertexMap, ref uint currentIndex)
        {
            // Create a unique key for this vertex combination
            string key = $"{faceVertex.VertexIndex}_{faceVertex.TextureIndex}_{faceVertex.NormalIndex}";

            // Check if we've already added this vertex
            if (vertexMap.TryGetValue(key, out uint existingIndex))
            {
                return existingIndex;
            }

            // Add new vertex
            uint index = currentIndex++;

            // Position (required)
            if (faceVertex.VertexIndex >= 0 && faceVertex.VertexIndex < objData.Vertices.Count)
            {
                var pos = objData.Vertices[faceVertex.VertexIndex];
                vertices.Add(pos.X);
                vertices.Add(pos.Y);
                vertices.Add(pos.Z);
            }
            else
            {
                // Default position if index is invalid
                vertices.Add(0.0f);
                vertices.Add(0.0f);
                vertices.Add(0.0f);
            }

            // Texture coordinates (optional) - FIXED: Better UV handling
            if (faceVertex.TextureIndex >= 0 && faceVertex.TextureIndex < objData.TextureCoords.Count)
            {
                var texCoord = objData.TextureCoords[faceVertex.TextureIndex];
                vertices.Add(texCoord.X);
                vertices.Add(texCoord.Y); // Keep original Y - texture loading handles the flip
            }
            else
            {
                // Default texture coordinates - use vertex position as UV for basic mapping
                if (faceVertex.VertexIndex >= 0 && faceVertex.VertexIndex < objData.Vertices.Count)
                {
                    var pos = objData.Vertices[faceVertex.VertexIndex];
                    // Simple planar projection - map X and Z to UV
                    vertices.Add((pos.X + 1.0f) * 0.5f); // Map -1..1 to 0..1
                    vertices.Add((pos.Z + 1.0f) * 0.5f); // Map -1..1 to 0..1
                }
                else
                {
                    vertices.Add(0.5f); // Center UV
                    vertices.Add(0.5f);
                }
            }

            // Normal (optional)
            if (faceVertex.NormalIndex >= 0 && faceVertex.NormalIndex < objData.Normals.Count)
            {
                var normal = objData.Normals[faceVertex.NormalIndex];
                vertices.Add(normal.X);
                vertices.Add(normal.Y);
                vertices.Add(normal.Z);
            }
            else
            {
                // Default normal (pointing up)
                vertices.Add(0.0f);
                vertices.Add(1.0f);
                vertices.Add(0.0f);
            }

            // Color (default white)
            vertices.Add(1.0f);
            vertices.Add(1.0f);
            vertices.Add(1.0f);
            vertices.Add(1.0f);

            // Cache this vertex
            vertexMap[key] = index;

            return index;
        }

        /// <summary>
        /// Calculate normals for faces that don't have them
        /// </summary>
        public static void CalculateNormals(ObjData objData)
        {
            // Only calculate if we don't have normals
            if (objData.Normals.Count > 0)
                return;

            var vertexNormalAccum = new Dictionary<int, Vector3>();
            var vertexNormalCount = new Dictionary<int, int>();

            // Calculate face normals and accumulate for vertex normals
            foreach (var face in objData.Faces)
            {
                if (face.Vertices.Count >= 3)
                {
                    // Get first three vertices to calculate normal
                    var v1 = objData.Vertices[face.Vertices[0].VertexIndex];
                    var v2 = objData.Vertices[face.Vertices[1].VertexIndex];
                    var v3 = objData.Vertices[face.Vertices[2].VertexIndex];

                    // Calculate face normal
                    var edge1 = v2 - v1;
                    var edge2 = v3 - v1;
                    var faceNormal = Vector3.Normalize(Vector3.Cross(edge1, edge2));

                    // Accumulate normals for each vertex in the face
                    foreach (var faceVertex in face.Vertices)
                    {
                        int vertexIndex = faceVertex.VertexIndex;

                        if (!vertexNormalAccum.ContainsKey(vertexIndex))
                        {
                            vertexNormalAccum[vertexIndex] = Vector3.Zero;
                            vertexNormalCount[vertexIndex] = 0;
                        }

                        vertexNormalAccum[vertexIndex] += faceNormal;
                        vertexNormalCount[vertexIndex]++;
                    }
                }
            }

            // Average the accumulated normals
            for (int i = 0; i < objData.Vertices.Count; i++)
            {
                if (vertexNormalAccum.ContainsKey(i))
                {
                    var avgNormal = vertexNormalAccum[i] / vertexNormalCount[i];
                    objData.Normals.Add(Vector3.Normalize(avgNormal));
                }
                else
                {
                    // Default normal for vertices not in any face
                    objData.Normals.Add(Vector3.UnitY);
                }
            }

            // Update face vertices to reference the calculated normals
            foreach (var face in objData.Faces)
            {
                for (int i = 0; i < face.Vertices.Count; i++)
                {
                    var faceVertex = face.Vertices[i];
                    faceVertex.NormalIndex = faceVertex.VertexIndex; // Use same index as vertex
                    face.Vertices[i] = faceVertex;
                }
            }

            Console.WriteLine($"Calculated {objData.Normals.Count} normals for model");
        }
    }
}