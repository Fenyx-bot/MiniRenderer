using OpenTK.Graphics.OpenGL4;
using System;

namespace MiniRenderer.Graphics
{
    /// <summary>
    /// Wrapper class for OpenGL Vertex Array Object (VAO)
    /// </summary>
    public class VertexArray : IDisposable
    {
        // The OpenGL handle to the vertex array
        private readonly int _handle;

        // Flag for resource disposal
        private bool _disposed = false;

        /// <summary>
        /// Create a new vertex array
        /// </summary>
        public VertexArray()
        {
            // Generate a vertex array object
            _handle = GL.GenVertexArray();
        }

        /// <summary>
        /// Bind this vertex array to make it the current vertex array for subsequent operations
        /// </summary>
        public void Bind()
        {
            GL.BindVertexArray(_handle);
        }

        /// <summary>
        /// Unbind this vertex array (bind vertex array 0)
        /// </summary>
        public void Unbind()
        {
            GL.BindVertexArray(0);
        }

        /// <summary>
        /// Enable a vertex attribute
        /// </summary>
        /// <param name="index">The attribute index to enable</param>
        public void EnableAttribute(int index)
        {
            Bind();
            GL.EnableVertexAttribArray(index);
        }

        /// <summary>
        /// Disable a vertex attribute
        /// </summary>
        /// <param name="index">The attribute index to disable</param>
        public void DisableAttribute(int index)
        {
            Bind();
            GL.DisableVertexAttribArray(index);
        }

        /// <summary>
        /// Get the OpenGL handle for this vertex array
        /// </summary>
        public int Handle => _handle;

        /// <summary>
        /// Dispose of the vertex array resource
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                GL.DeleteVertexArray(_handle);
                _disposed = true;
                GC.SuppressFinalize(this);
            }
        }

        /// <summary>
        /// Finalizer to warn if vertex array wasn't properly disposed
        /// </summary>
        ~VertexArray()
        {
            if (!_disposed)
            {
                Console.WriteLine("WARNING: VertexArray was not disposed! Call Dispose() to prevent resource leaks.");
            }
        }
    }
}