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

        // Camera
        private Camera3D _camera;

        // Shaders
        private Shader _modelShader;
        private Shader _wireframeShader;

        // Meshes
        private Mesh _solidCube;
        private Mesh _wireframeCube;
        private Mesh _grid;

        // Textures
        private Texture _containerTexture;
        private Texture _defaultTexture;

        // Mouse state
        private Vector2 _lastMousePosition;
        private bool _firstMouseMove = true;
        private bool _mouseCaptured = false;

        // Animation
        private float _time = 0.0f;

        // Drawing mode
        private enum DrawMode { Solid, Wireframe, Both }
        private DrawMode _drawMode = DrawMode.Both;

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
            _window.MouseMove += OnMouseMove;
            _window.MouseWheel += OnMouseWheel;
        }

        /// <summary>
        /// Called when the window loads
        /// </summary>
        private void OnLoad()
        {
            Console.WriteLine("OpenGL Version: " + GL.GetString(StringName.Version));

            // Enable depth testing
            GL.Enable(EnableCap.DepthTest);

            // Set a background color (dark blue)
            GL.ClearColor(0.0f, 0.1f, 0.2f, 1.0f);

            // Create directory for shaders if it doesn't exist
            Directory.CreateDirectory("Shaders");

            // Create shaders
            CreateShaders();

            // Load textures
            LoadTextures();

            // Create meshes
            CreateMeshes();

            // Create 3D camera
            _camera = new Camera3D(new Vector3(0, 1, 3), _window.Size.X, _window.Size.Y);

            // Print controls
            PrintControls();
        }

        /// <summary>
        /// Create and initialize shaders
        /// </summary>
        private void CreateShaders()
        {
            // Create model shader files if they don't exist
            string modelVertPath = "Shaders/model.vert";
            string modelFragPath = "Shaders/model.frag";

            if (!File.Exists(modelVertPath))
            {
                File.WriteAllText(modelVertPath, @"#version 330 core
layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec2 aTexCoord;
layout(location = 2) in vec3 aNormal;
layout(location = 3) in vec4 aColor;

out vec2 texCoord;
out vec3 normal;
out vec4 vertexColor;

uniform mat4 uModel;
uniform mat4 uView;
uniform mat4 uProjection;
uniform bool uUseTexture;

void main()
{
    gl_Position = uProjection * uView * uModel * vec4(aPosition, 1.0);
    texCoord = aTexCoord;
    
    // Transform the normal to world space
    normal = mat3(transpose(inverse(uModel))) * aNormal;
    
    vertexColor = aColor;
}");
            }

            if (!File.Exists(modelFragPath))
            {
                File.WriteAllText(modelFragPath, @"#version 330 core
in vec2 texCoord;
in vec3 normal;
in vec4 vertexColor;

out vec4 FragColor;

uniform sampler2D uTexture;
uniform bool uUseTexture;
uniform vec4 uColor;

void main()
{
    // Normalize the normal
    vec3 norm = normalize(normal);
    
    // Basic lighting calculation
    vec3 lightDir = normalize(vec3(0.5, 1.0, 0.3)); // Light direction
    float diff = max(dot(norm, lightDir), 0.0); // Diffuse factor
    
    // Ambient lighting
    float ambientStrength = 0.3;
    vec3 ambient = ambientStrength * vec3(1.0);
    
    // Combine ambient and diffuse
    vec3 lighting = ambient + diff * vec3(0.7);
    
    if (uUseTexture)
    {
        // Sample the texture
        vec4 texColor = texture(uTexture, texCoord);
        
        // Apply lighting to the texture color
        FragColor = vec4(texColor.rgb * lighting, texColor.a) * uColor;
    }
    else
    {
        // Apply lighting to the vertex color
        FragColor = vec4(vertexColor.rgb * lighting, vertexColor.a) * uColor;
    }
}");
            }

            // Create wireframe shader files if they don't exist
            string wireframeVertPath = "Shaders/wireframe.vert";
            string wireframeFragPath = "Shaders/wireframe.frag";

            if (!File.Exists(wireframeVertPath))
            {
                File.WriteAllText(wireframeVertPath, @"#version 330 core
layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec4 aColor;

out vec4 vertexColor;

uniform mat4 uModel;
uniform mat4 uView;
uniform mat4 uProjection;
uniform bool uUseTexture; // Added but unused to avoid warnings

void main()
{
    gl_Position = uProjection * uView * uModel * vec4(aPosition, 1.0);
    vertexColor = aColor;
}");
            }

            if (!File.Exists(wireframeFragPath))
            {
                File.WriteAllText(wireframeFragPath, @"#version 330 core
in vec4 vertexColor;

out vec4 FragColor;

uniform vec4 uColor;
uniform bool uUseTexture; // Added but unused to avoid warnings
uniform sampler2D uTexture; // Added but unused to avoid warnings

void main()
{
    FragColor = vertexColor * uColor;
}");
            }

            // Load the shaders
            try
            {
                _modelShader = Shader.FromFiles(modelVertPath, modelFragPath);
                _wireframeShader = Shader.FromFiles(wireframeVertPath, wireframeFragPath);

                // Set the texture unit for the model shader
                _modelShader.Use();
                _modelShader.SetInt("uTexture", 0);

                // Set the texture unit for the wireframe shader to avoid warnings
                _wireframeShader.Use();
                _wireframeShader.SetInt("uTexture", 0);

                Console.WriteLine("Shaders loaded successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading shaders: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Load textures
        /// </summary>
        private void LoadTextures()
        {
            // Create the Textures directory if it doesn't exist
            Directory.CreateDirectory("Assets/Textures");

            // Create a default texture
            _defaultTexture = Texture.CreateWhiteTexture();

            // Try to load the container texture
            try
            {
                if (File.Exists("Assets/Textures/container.jpg"))
                {
                    _containerTexture = new Texture("Assets/Textures/container.jpg");
                    Console.WriteLine("Container texture loaded successfully");
                }
                else
                {
                    _containerTexture = _defaultTexture;
                    Console.WriteLine("Texture 'container.jpg' not found, using default white texture");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading textures: {ex.Message}");
                _containerTexture = _defaultTexture;
            }
        }

        /// <summary>
        /// Create mesh objects
        /// </summary>
        private void CreateMeshes()
        {
            // Create a solid cube
            _solidCube = Mesh.CreateCube(1.0f);
            _solidCube.Texture = _containerTexture;
            _solidCube.Position = new Vector3(0.0f, 0.0f, 0.0f);

            // Create a wireframe cube
            _wireframeCube = Mesh.CreateCube(1.02f, true);
            _wireframeCube.Color = new Vector4(0.0f, 1.0f, 1.0f, 1.0f); // Cyan
            _wireframeCube.Position = new Vector3(0.0f, 0.0f, 0.0f);

            // Create a grid
            _grid = Mesh.CreateGrid(10.0f, 10.0f, 10);
            _grid.Color = new Vector4(0.5f, 0.5f, 0.5f, 1.0f); // Gray
        }

        /// <summary>
        /// Print controls to the console
        /// </summary>
        private void PrintControls()
        {
            Console.WriteLine("Controls:");
            Console.WriteLine("  WASD - Move camera");
            Console.WriteLine("  Space/Shift - Move camera up/down");
            Console.WriteLine("  Mouse - Look around (click to capture/release mouse)");
            Console.WriteLine("  Mouse Wheel - Zoom in/out");
            Console.WriteLine("  R - Reset camera");
            Console.WriteLine("  T - Reset cube position and rotation");
            Console.WriteLine("  M - Toggle drawing mode (solid/wireframe/both)");
            Console.WriteLine("  P - Toggle projection mode (perspective/orthographic)");
            Console.WriteLine("  Arrow Keys - Rotate cube");
            Console.WriteLine("  Page Up/Down - Scale cube");
            Console.WriteLine("  Escape - Exit");
        }

        /// <summary>
        /// Called when a key is pressed
        /// </summary>
        private void OnKeyDown(KeyboardKeyEventArgs e)
        {
            switch (e.Key)
            {
                case Keys.R:
                    // Reset camera
                    _camera = new Camera3D(new Vector3(0, 1, 3), _window.Size.X, _window.Size.Y);
                    Console.WriteLine("Camera reset");
                    break;

                case Keys.T:
                    // Reset cube position and rotation
                    _solidCube.Position = Vector3.Zero;
                    _solidCube.Rotation = Vector3.Zero;
                    _solidCube.Scale = Vector3.One;

                    _wireframeCube.Position = Vector3.Zero;
                    _wireframeCube.Rotation = Vector3.Zero;
                    _wireframeCube.Scale = Vector3.One;
                    Console.WriteLine("Cube reset");
                    break;

                case Keys.M:
                    // Toggle drawing mode
                    _drawMode = (DrawMode)(((int)_drawMode + 1) % 3);
                    Console.WriteLine($"Drawing mode: {_drawMode}");
                    break;

                case Keys.P:
                    // Toggle projection mode
                    _camera.ToggleProjectionMode();
                    Console.WriteLine($"Projection mode: {_camera.Mode}");
                    break;
            }
        }

        /// <summary>
        /// Called when the mouse moves
        /// </summary>
        private void OnMouseMove(MouseMoveEventArgs e)
        {
            if (!_mouseCaptured)
                return;

            // Skip the first mouse move to avoid a large jump
            if (_firstMouseMove)
            {
                _lastMousePosition = new Vector2(e.X, e.Y);
                _firstMouseMove = false;
                return;
            }

            // Calculate mouse offset
            float xOffset = e.X - _lastMousePosition.X;
            float yOffset = e.Y - _lastMousePosition.Y;
            _lastMousePosition = new Vector2(e.X, e.Y);

            // Rotate the camera based on mouse movement
            _camera.Rotate(xOffset, yOffset);
        }

        /// <summary>
        /// Called when the mouse wheel is scrolled
        /// </summary>
        private void OnMouseWheel(MouseWheelEventArgs e)
        {
            // Adjust the camera's zoom
            _camera.AdjustZoom(e.OffsetY);
        }

        /// <summary>
        /// Called when the window is resized
        /// </summary>
        private void OnResize(ResizeEventArgs e)
        {
            // Update viewport
            GL.Viewport(0, 0, e.Width, e.Height);

            // Update camera aspect ratio
            _camera.Resize(e.Width, e.Height);
        }

        /// <summary>
        /// Called to update the game logic
        /// </summary>
        private void OnUpdateFrame(FrameEventArgs e)
        {
            // Update time
            _time += (float)e.Time;

            // Check for exit
            if (_window.KeyboardState.IsKeyDown(Keys.Escape))
            {
                _window.Close();
            }

            // Toggle mouse capture on mouse click
            if (_window.MouseState.IsButtonPressed(MouseButton.Left))
            {
                if (!_mouseCaptured)
                {
                    _mouseCaptured = true;
                    _firstMouseMove = true;
                    _window.CursorState = CursorState.Grabbed;
                }
            }

            // Release mouse with right click
            if (_window.MouseState.IsButtonPressed(MouseButton.Right))
            {
                if (_mouseCaptured)
                {
                    _mouseCaptured = false;
                    _window.CursorState = CursorState.Normal;
                }
            }

            // Handle camera movement
            HandleCameraMovement((float)e.Time);

            // Handle cube movement
            HandleCubeMovement((float)e.Time);
        }

        /// <summary>
        /// Handle camera movement based on keyboard input
        /// </summary>
        private void HandleCameraMovement(float deltaTime)
        {
            // Movement speed
            float speed = 3.0f * deltaTime;

            // Create a movement vector
            Vector3 movement = Vector3.Zero;

            // Check keyboard state
            var keyboard = _window.KeyboardState;

            // Forward/backward
            if (keyboard.IsKeyDown(Keys.W))
                movement.Z = 1.0f;
            if (keyboard.IsKeyDown(Keys.S))
                movement.Z = -1.0f;

            // Left/right
            if (keyboard.IsKeyDown(Keys.A))
                movement.X = -1.0f;
            if (keyboard.IsKeyDown(Keys.D))
                movement.X = 1.0f;

            // Up/down
            if (keyboard.IsKeyDown(Keys.Space))
                movement.Y = 1.0f;
            if (keyboard.IsKeyDown(Keys.LeftShift))
                movement.Y = -1.0f;

            // Move the camera
            if (movement != Vector3.Zero)
                _camera.Move(movement, speed);
        }

        /// <summary>
        /// Handle cube movement and rotation based on keyboard input
        /// </summary>
        private void HandleCubeMovement(float deltaTime)
        {
            // Check keyboard state
            var keyboard = _window.KeyboardState;

            // Rotation speed
            float rotationSpeed = 100.0f * deltaTime;

            // Cube rotation
            if (keyboard.IsKeyDown(Keys.Up))
            {
                _solidCube.Rotation += new Vector3(rotationSpeed, 0, 0);
                _wireframeCube.Rotation += new Vector3(rotationSpeed, 0, 0);
            }
            if (keyboard.IsKeyDown(Keys.Down))
            {
                _solidCube.Rotation += new Vector3(-rotationSpeed, 0, 0);
                _wireframeCube.Rotation += new Vector3(-rotationSpeed, 0, 0);
            }
            if (keyboard.IsKeyDown(Keys.Left))
            {
                _solidCube.Rotation += new Vector3(0, -rotationSpeed, 0);
                _wireframeCube.Rotation += new Vector3(0, -rotationSpeed, 0);
            }
            if (keyboard.IsKeyDown(Keys.Right))
            {
                _solidCube.Rotation += new Vector3(0, rotationSpeed, 0);
                _wireframeCube.Rotation += new Vector3(0, rotationSpeed, 0);
            }

            // Cube scaling
            if (keyboard.IsKeyDown(Keys.PageUp))
            {
                float scaleFactor = 1.0f + 0.5f * deltaTime;
                _solidCube.Scale *= scaleFactor;
                _wireframeCube.Scale *= scaleFactor;
            }
            if (keyboard.IsKeyDown(Keys.PageDown))
            {
                float scaleFactor = 1.0f - 0.5f * deltaTime;
                _solidCube.Scale *= scaleFactor;
                _wireframeCube.Scale *= scaleFactor;
            }
        }

        /// <summary>
        /// Called to render a frame
        /// </summary>
        private void OnRenderFrame(FrameEventArgs e)
        {
            // Clear the color and depth buffers
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            // Get camera matrices
            Matrix4 viewMatrix = _camera.GetViewMatrix();
            Matrix4 projectionMatrix = _camera.GetProjectionMatrix();

            // Set common shader uniforms
            _modelShader.Use();
            _modelShader.SetMatrix4("uView", viewMatrix);
            _modelShader.SetMatrix4("uProjection", projectionMatrix);

            _wireframeShader.Use();
            _wireframeShader.SetMatrix4("uView", viewMatrix);
            _wireframeShader.SetMatrix4("uProjection", projectionMatrix);

            // Draw grid
            _grid.Render(_modelShader);

            // Draw cube based on the current draw mode
            if (_drawMode == DrawMode.Solid || _drawMode == DrawMode.Both)
            {
                _solidCube.Render(_modelShader);
            }

            if (_drawMode == DrawMode.Wireframe || _drawMode == DrawMode.Both)
            {
                _wireframeCube.Render(_wireframeShader);
            }

            // Swap buffers
            _window.SwapBuffers();
        }

        /// <summary>
        /// Start the game loop
        /// </summary>
        public void Run()
        {
            _window.Run();
        }

        /// <summary>
        /// Dispose of resources
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                // Dispose of meshes
                _solidCube?.Dispose();
                _wireframeCube?.Dispose();
                _grid?.Dispose();

                // Dispose of shaders
                _modelShader?.Dispose();
                _wireframeShader?.Dispose();

                // Dispose of textures
                _containerTexture?.Dispose();
                _defaultTexture?.Dispose();

                _disposed = true;
                GC.SuppressFinalize(this);
            }
        }
    }
}