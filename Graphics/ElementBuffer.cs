using OpenTK.Graphics.OpenGL4;
using System;

namespace MiniRenderer.Graphics
{
    /// <summary>
    /// Wrapper class for OpenGL Element Buffer Object (EBO)
    /// Used for indexed rendering of geometry
    /// </summary>
    public class ElementBuffer : IDisposable
    {
        // The OpenGL handle to the buffer
        private readonly int _handle;

        // Flag for resource disposal
        private bool _disposed = false;

        /// <summary>
        /// Create a new element buffer
        /// </summary>
        public ElementBuffer()
        {
            // Generate a buffer object
            _handle = GL.GenBuffer();
        }

        /// <summary>
        /// Bind this buffer to make it the current element buffer for subsequent operations
        /// </summary>
        public void Bind()
        {
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _handle);
        }

        /// <summary>
        /// Unbind this buffer (bind element buffer 0)
        /// </summary>
        public void Unbind()
        {
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
        }

        /// <summary>
        /// Upload unsigned integer (uint) index data to the buffer
        /// </summary>
        /// <param name="indices">Array of uint indices</param>
        /// <param name="usageHint">Hint about how the data will be used</param>
        public void SetData(uint[] indices, BufferUsageHint usageHint = BufferUsageHint.StaticDraw)
        {
            Bind();
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, usageHint);
        }

        /// <summary>
        /// Upload integer (int) index data to the buffer
        /// </summary>
        /// <param name="indices">Array of int indices</param>
        /// <param name="usageHint">Hint about how the data will be used</param>
        public void SetData(int[] indices, BufferUsageHint usageHint = BufferUsageHint.StaticDraw)
        {
            Bind();
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(int), indices, usageHint);
        }

        /// <summary>
        /// Get the OpenGL handle for this buffer
        /// </summary>
        public int Handle => _handle;

        /// <summary>
        /// Dispose of the buffer resource
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                GL.DeleteBuffer(_handle);
                _disposed = true;
                GC.SuppressFinalize(this);
            }
        }

        /// <summary>
        /// Finalizer to warn if buffer wasn't properly disposed
        /// </summary>
        ~ElementBuffer()
        {
            if (!_disposed)
            {
                Console.WriteLine("WARNING: ElementBuffer was not disposed! Call Dispose() to prevent resource leaks.");
            }
        }
    }
}