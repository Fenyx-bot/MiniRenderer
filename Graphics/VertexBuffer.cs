using OpenTK.Graphics.OpenGL4;
using System;

namespace MiniRenderer.Graphics
{
    /// <summary>
    /// Wrapper class for OpenGL Vertex Buffer Object (VBO)
    /// </summary>
    public class VertexBuffer : IDisposable
    {
        // The OpenGL handle to the buffer
        private readonly int _handle;

        // Flag for resource disposal
        private bool _disposed = false;

        /// <summary>
        /// Create a new vertex buffer
        /// </summary>
        public VertexBuffer()
        {
            // Generate a buffer object
            _handle = GL.GenBuffer();
        }

        /// <summary>
        /// Bind this buffer to make it the current buffer for subsequent operations
        /// </summary>
        public void Bind()
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, _handle);
        }

        /// <summary>
        /// Unbind this buffer (bind buffer 0)
        /// </summary>
        public void Unbind()
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        }

        /// <summary>
        /// Upload data to the buffer
        /// </summary>
        /// <param name="data">Array of floating-point values</param>
        /// <param name="usageHint">Hint about how the data will be used</param>
        public void SetData(float[] data, BufferUsageHint usageHint = BufferUsageHint.StaticDraw)
        {
            Bind();
            GL.BufferData(BufferTarget.ArrayBuffer, data.Length * sizeof(float), data, usageHint);
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
        ~VertexBuffer()
        {
            if (!_disposed)
            {
                Console.WriteLine("WARNING: VertexBuffer was not disposed! Call Dispose() to prevent resource leaks.");
            }
        }
    }
}