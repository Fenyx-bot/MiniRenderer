using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using MiniRenderer.Graphics;
using MiniRenderer.Camera;
using System.IO;

namespace MiniRenderer.Engine
{
    /// <summary>
    /// Main engine class responsible for managing the render loop and OpenGL resources
    /// </summary>
    public class Engine : IDisposable
    {
        private readonly GameWindow _window;

        // OpenGL objects
        private VertexArray _quadVAO;
        private VertexBuffer _quadVBO;
        private ElementBuffer _quadEBO;

        // Shaders
        private Shader _spriteShader;

        // Textures
        private Texture _crateTexture;
        private Texture _faceTexture;
        private Texture _whiteTexture;

        // Sprites
        private List<Sprite> _sprites = new List<Sprite>();

        // Camera
        private Camera2D _camera;

        // Timing and animation
        private float _time = 0.0f;

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
            _window.KeyDown += OnKeyDown;
            _window.MouseWheel += OnMouseWheel;
        }

        /// <summary>
        /// Called when the window loads
        /// </summary>
        private void OnLoad()
        {
            Console.WriteLine("OpenGL Version: " + GL.GetString(StringName.Version));

            // Enable alpha blending
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            // Set a background color (dark blue)
            GL.ClearColor(0.0f, 0.1f, 0.2f, 1.0f);

            // Create our resources
            CreateQuad();
            CreateShaders();
            LoadTextures();
            CreateSprites();

            // Create camera
            _camera = new Camera2D(_window.Size.X, _window.Size.Y);

            // Print controls
            Console.WriteLine("Controls:");
            Console.WriteLine("  WASD - Move camera");
            Console.WriteLine("  Q/E - Rotate camera");
            Console.WriteLine("  Mouse Wheel - Zoom in/out");
            Console.WriteLine("  R - Reset camera");
            Console.WriteLine("  Space - Add a random sprite");
            Console.WriteLine("  Escape - Exit");
        }

        /// <summary>
        /// Create a quad for sprite rendering
        /// </summary>
        private void CreateQuad()
        {
            // Create a Vertex Array Object (VAO)
            _quadVAO = new VertexArray();
            _quadVAO.Bind();

            // Quad vertices with position and texture coordinates
            // Positions are from 0 to 1 in both dimensions
            // Position (X, Y), Texture Coord (U, V), Color (R, G, B, A)
            float[] vertices = {
                // Position    // TexCoords   // Color
                0.0f, 0.0f,    0.0f, 0.0f,    1.0f, 1.0f, 1.0f, 1.0f,  // Bottom-left
                1.0f, 0.0f,    1.0f, 0.0f,    1.0f, 1.0f, 1.0f, 1.0f,  // Bottom-right
                1.0f, 1.0f,    1.0f, 1.0f,    1.0f, 1.0f, 1.0f, 1.0f,  // Top-right
                0.0f, 1.0f,    0.0f, 1.0f,    1.0f, 1.0f, 1.0f, 1.0f   // Top-left
            };

            // Indices for the quad (forming two triangles)
            uint[] indices = {
                0, 1, 2,  // First triangle
                2, 3, 0   // Second triangle
            };

            // Create and bind the Vertex Buffer Object (VBO)
            _quadVBO = new VertexBuffer();
            _quadVBO.Bind();
            _quadVBO.SetData(vertices);

            // Create and bind the Element Buffer Object (EBO)
            _quadEBO = new ElementBuffer();
            _quadEBO.Bind();
            _quadEBO.SetData(indices);

            // Set up vertex attributes

            // Position attribute (2 floats)
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 8 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            // Texture coordinate attribute (2 floats)
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 8 * sizeof(float), 2 * sizeof(float));
            GL.EnableVertexAttribArray(1);

            // Color attribute (4 floats)
            GL.VertexAttribPointer(2, 4, VertexAttribPointerType.Float, false, 8 * sizeof(float), 4 * sizeof(float));
            GL.EnableVertexAttribArray(2);

            // Unbind VAO to prevent accidental modification
            _quadVAO.Unbind();
        }

        /// <summary>
        /// Create and load the shaders
        /// </summary>
        private void CreateShaders()
        {
            // Create shader directory if it doesn't exist
            Directory.CreateDirectory("Shaders");

            // Paths to shader files
            string vertPath = "Shaders/sprite.vert";
            string fragPath = "Shaders/sprite.frag";

            // Create shader files if they don't exist
            if (!File.Exists(vertPath))
            {
                File.WriteAllText(vertPath, @"#version 330 core
layout(location = 0) in vec2 aPosition;
layout(location = 1) in vec2 aTexCoord;
layout(location = 2) in vec4 aColor;

out vec2 texCoord;
out vec4 vertexColor;

uniform mat4 uModel;
uniform mat4 uView;
uniform mat4 uProjection;

void main()
{
    gl_Position = uProjection * uView * uModel * vec4(aPosition, 0.0, 1.0);
    texCoord = aTexCoord;
    vertexColor = aColor;
}");
            }

            if (!File.Exists(fragPath))
            {
                File.WriteAllText(fragPath, @"#version 330 core
in vec2 texCoord;
in vec4 vertexColor;

out vec4 FragColor;

uniform sampler2D uTexture;
uniform bool uUseTexture;

void main()
{
    if (uUseTexture)
    {
        vec4 texColor = texture(uTexture, texCoord);
        FragColor = texColor * vertexColor;
    }
    else
    {
        FragColor = vertexColor;
    }
}");
            }

            // Load the shader
            try
            {
                _spriteShader = Shader.FromFiles(vertPath, fragPath);
                Console.WriteLine("Sprite shader loaded successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading sprite shader: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Load textures for sprite rendering
        /// </summary>
        private void LoadTextures()
        {
            // Create the Textures directory if it doesn't exist
            Directory.CreateDirectory("Textures");

            // Create a white texture for untextured sprites
            _whiteTexture = Texture.CreateWhiteTexture();

            // Try to load textures
            try
            {
                if (File.Exists("Textures/crate.png"))
                {
                    _crateTexture = new Texture("Textures/crate.png");
                }
                else
                {
                    // Use default texture if file not found
                    _crateTexture = _whiteTexture;
                    Console.WriteLine("Texture 'crate.png' not found, using default white texture");
                }

                if (File.Exists("Textures/awesome_face.png"))
                {
                    _faceTexture = new Texture("Textures/awesome_face.png");
                }
                else
                {
                    // Use default texture if file not found
                    _faceTexture = _whiteTexture;
                    Console.WriteLine("Texture 'awesome_face.png' not found, using default white texture");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading textures: {ex.Message}");
                // Use default texture if error occurs
                _crateTexture = _whiteTexture;
                _faceTexture = _whiteTexture;
            }
        }

        /// <summary>
        /// Create initial sprites
        /// </summary>
        private void CreateSprites()
        {
            // Create a crate sprite at the center
            var crateSprite = new Sprite(_crateTexture, new Vector2(0.0f, 0.0f), new Vector2(0.5f));
            _sprites.Add(crateSprite);

            // Create a face sprite to the right
            var faceSprite = new Sprite(_faceTexture, new Vector2(0.7f, 0.0f), new Vector2(0.4f));
            _sprites.Add(faceSprite);

            // Create a red rectangle to the left
            var redRect = new Sprite(new Vector2(-0.7f, 0.0f), new Vector2(0.3f, 0.6f), new Vector4(1.0f, 0.0f, 0.0f, 1.0f));
            _sprites.Add(redRect);

            // Create a blue rectangle below
            var blueRect = new Sprite(new Vector2(0.0f, -0.7f), new Vector2(0.6f, 0.3f), new Vector4(0.0f, 0.5f, 1.0f, 1.0f));
            _sprites.Add(blueRect);

            // Create a semi-transparent green square above
            var greenSquare = new Sprite(new Vector2(0.0f, 0.7f), new Vector2(0.4f), new Vector4(0.0f, 1.0f, 0.0f, 0.5f));
            _sprites.Add(greenSquare);
        }

        /// <summary>
        /// Add a random sprite at a random position
        /// </summary>
        private void AddRandomSprite()
        {
            // Generate random values
            Random random = new Random();

            // Random position in visible area
            float x = (float)random.NextDouble() * 3.0f - 1.5f;
            float y = (float)random.NextDouble() * 3.0f - 1.5f;

            // Random size
            float size = (float)random.NextDouble() * 0.4f + 0.2f;

            // Random color
            float r = (float)random.NextDouble();
            float g = (float)random.NextDouble();
            float b = (float)random.NextDouble();
            float a = (float)random.NextDouble() * 0.5f + 0.5f; // Semi-transparent to opaque

            // 50% chance of having a texture
            bool useTexture = true;//random.Next(2) == 0;

            // Create the sprite
            Sprite sprite;
            if (useTexture)
            {
                // Use either crate or face texture
                Texture texture = random.Next(2) == 0 ? _crateTexture : _faceTexture;
                sprite = new Sprite(texture, new Vector2(x, y), new Vector2(size));
                sprite.Color = new Vector4(r, g, b, a); // Apply tint
            }
            else
            {
                // Create a colored rectangle
                sprite = new Sprite(new Vector2(x, y), new Vector2(size, size * (float)random.NextDouble() + 0.5f), new Vector4(r, g, b, a));
            }

            // Add the sprite
            _sprites.Add(sprite);
            Console.WriteLine($"Added sprite at ({x:F2}, {y:F2}) with size {size:F2}");
        }

        /// <summary>
        /// Handle key down events
        /// </summary>
        private void OnKeyDown(KeyboardKeyEventArgs e)
        {
            if (e.Key == Keys.Space)
            {
                // Add a random sprite when space is pressed
                AddRandomSprite();
            }
            else if (e.Key == Keys.R)
            {
                // Reset camera when R is pressed
                _camera.Position = Vector2.Zero;
                _camera.Rotation = 0.0f;
                _camera.Zoom = 1.0f;
                Console.WriteLine("Camera reset");
            }
        }

        /// <summary>
        /// Handle mouse wheel events for camera zooming
        /// </summary>
        private void OnMouseWheel(MouseWheelEventArgs e)
        {
            // Adjust zoom based on mouse wheel movement
            _camera.AdjustZoom(e.OffsetY * 0.1f);
        }

        /// <summary>
        /// Called when the window is resized
        /// </summary>
        private void OnResize(ResizeEventArgs e)
        {
            // Update viewport when the window is resized
            GL.Viewport(0, 0, e.Width, e.Height);

            // Update camera viewport
            _camera.Resize(e.Width, e.Height);
        }

        /// <summary>
        /// Called to update the game logic
        /// </summary>
        private void OnUpdateFrame(FrameEventArgs e)
        {
            // Update time
            _time += (float)e.Time;

            // Get keyboard state
            var keyboard = _window.KeyboardState;

            // Check if Escape key is pressed to close the window
            if (keyboard.IsKeyDown(Keys.Escape))
            {
                _window.Close();
            }

            // Handle camera movement with WASD
            float cameraSpeed = 1.0f * (float)e.Time;
            if (keyboard.IsKeyDown(Keys.W))
            {
                _camera.Move(new Vector2(0.0f, cameraSpeed));
            }
            if (keyboard.IsKeyDown(Keys.S))
            {
                _camera.Move(new Vector2(0.0f, -cameraSpeed));
            }
            if (keyboard.IsKeyDown(Keys.A))
            {
                _camera.Move(new Vector2(-cameraSpeed, 0.0f));
            }
            if (keyboard.IsKeyDown(Keys.D))
            {
                _camera.Move(new Vector2(cameraSpeed, 0.0f));
            }

            // Handle camera rotation with Q/E
            float rotationSpeed = 90.0f * (float)e.Time;
            if (keyboard.IsKeyDown(Keys.Q))
            {
                _camera.Rotate(-rotationSpeed);
            }
            if (keyboard.IsKeyDown(Keys.E))
            {
                _camera.Rotate(rotationSpeed);
            }

            // Update sprite animations
            UpdateSprites((float)e.Time);
        }

        /// <summary>
        /// Update sprites (animation, movement, etc.)
        /// </summary>
        private void UpdateSprites(float deltaTime)
        {
            // Animate some sprites for demonstration
            if (_sprites.Count >= 2)
            {
                // Rotate the crate
                _sprites[0].Rotation += 45.0f * deltaTime;

                // Make the face bob up and down
                float offset = (float)Math.Sin(_time * 2.0f) * 0.2f;
                _sprites[1].Position = new Vector2(0.7f, offset);
            }
        }

        /// <summary>
        /// Called to render a frame
        /// </summary>
        private void OnRenderFrame(FrameEventArgs e)
        {
            // Clear the screen
            GL.Clear(ClearBufferMask.ColorBufferBit);

            // Draw sprites
            RenderSprites();

            // Swap buffers
            _window.SwapBuffers();
        }

        /// <summary>
        /// Render all sprites
        /// </summary>
        private void RenderSprites()
        {
            // Use the sprite shader
            _spriteShader.Use();

            // Set camera matrices
            Matrix4 viewMatrix = _camera.GetViewMatrix();
            Matrix4 projectionMatrix = _camera.GetProjectionMatrix();

            _spriteShader.SetMatrix4("uView", viewMatrix);
            _spriteShader.SetMatrix4("uProjection", projectionMatrix);

            // Bind the VAO
            _quadVAO.Bind();

            // Draw each sprite
            foreach (var sprite in _sprites)
            {
                // Get the model matrix for this sprite
                Matrix4 model = sprite.GetModelMatrix();

                // Set the model matrix uniform
                _spriteShader.SetMatrix4("uModel", model);

                // Set texture uniform
                _spriteShader.SetBool("uUseTexture", sprite.UseTexture);

                if (sprite.UseTexture)
                {
                    // Bind the texture
                    sprite.Texture.Use();
                }
                else
                {
                    // Bind the white texture as fallback
                    _whiteTexture.Use();
                }

                // Draw the sprite quad
                GL.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, 0);
            }

            // Unbind VAO
            _quadVAO.Unbind();
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
                // Clean up shader resources
                _spriteShader?.Dispose();

                // Clean up texture resources
                _crateTexture?.Dispose();
                _faceTexture?.Dispose();
                _whiteTexture?.Dispose();

                // Clean up OpenGL resources
                _quadVBO?.Dispose();
                _quadEBO?.Dispose();
                _quadVAO?.Dispose();

                _disposed = true;
                GC.SuppressFinalize(this);
            }
        }
    }
}