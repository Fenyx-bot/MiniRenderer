using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using MiniRenderer.Graphics;

namespace MiniRenderer.Engine
{
    /// <summary>
    /// Main engine class responsible for managing the render loop and OpenGL resources
    /// </summary>
    public class Engine : IDisposable
    {
        private readonly GameWindow _window;

        // OpenGL objects
        private VertexArray _vertexArray;
        private VertexBuffer _vertexBuffer;

        // Flag for proper resource disposal
        private bool _disposed = false;

        /// <summary>
        /// Create a new engine instance
        /// </summary>
        /// <param name="window">The game window to render to</param>
        public Engine(GameWindow window)
        {
            _window = window;

            // Set up event handlers
            _window.Load += OnLoad;
            _window.Resize += OnResize;
            _window.UpdateFrame += OnUpdateFrame;
            _window.RenderFrame += OnRenderFrame;
        }

        /// <summary>
        /// Called when the window loads
        /// </summary>
        private void OnLoad()
        {
            Console.WriteLine("OpenGL Version: " + GL.GetString(StringName.Version));

            // Set a background color (blue-ish)
            GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);

            // Create our objects
            CreateTriangle();
        }

        /// <summary>
        /// Create a simple triangle
        /// </summary>
        private void CreateTriangle()
        {
            // Create a Vertex Array Object (VAO)
            _vertexArray = new VertexArray();
            _vertexArray.Bind();

            // Triangle vertex data with fixed colors embedded
            // Format: X, Y, R, G, B
            float[] vertices = {
                 0.0f,  0.5f, 1.0f, 1.0f, 1.0f,  // top vertex (red)
                -0.5f, -0.5f, 0.0f, 0.0f, 0.0f,  // bottom-left vertex (green)
                 0.5f, -0.5f, 0.0f, 0.0f, 0.0f   // bottom-right vertex (blue)
            };

            // Create a Vertex Buffer Object (VBO)
            _vertexBuffer = new VertexBuffer();
            _vertexBuffer.Bind();

            // Upload the vertex data to the GPU
            _vertexBuffer.SetData(vertices);

            // Tell OpenGL how to interpret the vertex data

            // Position attribute (2 floats) at location 0
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            // Color attribute (3 floats) at location 1
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 2 * sizeof(float));
            GL.EnableVertexAttribArray(1);

            // Unbind VAO to prevent accidental modification
            _vertexArray.Unbind();
        }

        /// <summary>
        /// Called when the window is resized
        /// </summary>
        private void OnResize(ResizeEventArgs e)
        {
            // Update viewport when the window is resized
            GL.Viewport(0, 0, e.Width, e.Height);
        }

        /// <summary>
        /// Called to update the game logic
        /// </summary>
        private void OnUpdateFrame(FrameEventArgs e)
        {
            // Here we would update any game logic
            // For now, just check if Escape key is pressed to close the window
            var keyboard = _window.KeyboardState;
            if (keyboard.IsKeyDown(Keys.Escape))
            {
                _window.Close();
            }
        }

        /// <summary>
        /// Called to render a frame
        /// </summary>
        private void OnRenderFrame(FrameEventArgs e)
        {
            // Clear the screen
            GL.Clear(ClearBufferMask.ColorBufferBit);

            // Draw the triangle
            DrawTriangle();

            // Swap buffers
            _window.SwapBuffers();
        }

        /// <summary>
        /// Draw the triangle
        /// </summary>
        private void DrawTriangle()
        {
            // Bind the VAO
            _vertexArray.Bind();

            // In Module 1, we use the fixed-function pipeline
            // This means we don't write our own shaders yet
            // OpenGL 3.3+ requires shaders, so we use a simple built-in shader

            // The fixed function pipeline (OpenGL 1.x) is deprecated,
            // but we're simulating it with this very basic shader functionality

            // Draw the triangle
            GL.DrawArrays(PrimitiveType.Triangles, 0, 3);

            // Unbind the VAO
            _vertexArray.Unbind();
        }

        /// <summary>
        /// Start the game loop
        /// </summary>
        public void Run()
        {
            _window.Run();
        }

        /// <summary>
        /// Clean up resources
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                // Clean up OpenGL resources
                _vertexBuffer?.Dispose();
                _vertexArray?.Dispose();

                _disposed = true;
                GC.SuppressFinalize(this);
            }
        }
    }
}