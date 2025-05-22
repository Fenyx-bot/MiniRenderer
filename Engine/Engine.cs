using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using MiniRenderer.Graphics;
using MiniRenderer.Camera;
using System.IO;
using System.Linq;

namespace MiniRenderer.Engine
{
    /// <summary>
    /// Module 6: Loading 3D Models
    /// Builds on Module 5's material system to add OBJ model loading capability
    /// </summary>
    public class Engine : IDisposable
    {
        private readonly GameWindow _window;

        // Camera
        private Camera3D _camera;

        // Shaders (using Module 5's material shader)
        private Shader _materialShader;

        // Module 6: 3D Models
        private List<Model> _models = new List<Model>();

        // Module 5: Original cubes for comparison
        private Mesh _cube1;
        private Mesh _cube2;
        private Mesh _grid;

        // Textures
        private Texture _containerTexture;
        private Texture _containerSpecularTexture;
        private Texture _brickTexture;
        private Texture _defaultTexture;

        // Materials
        private Material _containerMaterial;
        private Material _brickMaterial;
        private Material _gridMaterial;
        private Material _defaultMaterial;

        // Mouse state
        private Vector2 _lastMousePosition;
        private bool _firstMouseMove = true;
        private bool _mouseCaptured = false;

        // Animation
        private float _time = 0.0f;
        private bool _autoRotate = true;
        private bool _showWireframe = false;

        // Light properties
        private Vector3 _lightPosition = new Vector3(1.2f, 1.0f, 2.0f);
        private Vector3 _lightColor = new Vector3(1.0f, 1.0f, 1.0f);
        private float _lightIntensity = 1.5f;
        private bool _lightRotate = true;

        // Specular testing
        private bool _specularEnabled = true;

        // Flag for proper resource disposal
        private bool _disposed = false;

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

        private void OnLoad()
        {
            Console.WriteLine("Module 6: Loading 3D Models");
            Console.WriteLine("OpenGL Version: " + GL.GetString(StringName.Version));

            // Enable depth testing
            GL.Enable(EnableCap.DepthTest);

            // Enable alpha blending
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            // Set background color
            GL.ClearColor(0.05f, 0.05f, 0.1f, 1.0f);

            // Create directories
            Directory.CreateDirectory("Shaders");
            Directory.CreateDirectory("Assets/Textures");
            Directory.CreateDirectory("Assets/Models");

            // Initialize components (Module 5 foundation)
            CreateShaders();
            LoadTextures();
            CreateMaterials();
            CreateMeshes(); // Module 5 cubes

            // Module 6: Load 3D models
            LoadModels();

            // Create camera
            CreateCamera();

            // Print controls
            PrintControls();
        }

        /// <summary>
        /// Create 3D camera positioned for viewing both cubes and models
        /// </summary>
        private void CreateCamera()
        {
            _camera = new Camera3D(new Vector3(3, 2, 6), _window.Size.X, _window.Size.Y);
            Console.WriteLine("✓ Camera created");
        }

        /// <summary>
        /// Use Module 5's existing material shader system
        /// </summary>
        private void CreateShaders()
        {
            string vertexShaderPath = "Shaders/material.vert";
            string fragmentShaderPath = "Shaders/material.frag";

            try
            {
                _materialShader = Shader.FromFiles(vertexShaderPath, fragmentShaderPath);
                Console.WriteLine("✓ Material shader loaded successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error loading shader: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Load textures (Module 5 system)
        /// </summary>
        private void LoadTextures()
        {
            _defaultTexture = Texture.CreateWhiteTexture();

            // Look for textures
            string[] directories = { "Assets/Textures/", "Textures/", "" };

            _containerTexture = LoadTextureWithPriority(new string[] { "container.jpg", "container.png" },
                                                      new string[] { "crate.png", "crate.jpg" },
                                                      "container diffuse", directories);

            _containerSpecularTexture = LoadTextureWithPriority(new string[] { "container_specular.jpg", "container_specular.png" },
                                                              new string[] { },
                                                              "container specular", directories, allowNull: true);

            _brickTexture = LoadTextureWithPriority(new string[] { "brick.jpg", "brick.png" },
                                                   new string[] { "awesome_face.png", "face.png" },
                                                   "brick diffuse", directories);

            Console.WriteLine("✓ Textures loaded");
        }

        private Texture LoadTextureWithPriority(string[] primaryNames, string[] fallbackNames, string textureType, string[] directories, bool allowNull = false)
        {
            // Try primary names first
            foreach (string name in primaryNames)
            {
                foreach (string dir in directories)
                {
                    string path = Path.Combine(dir, name);
                    if (File.Exists(path))
                    {
                        try
                        {
                            Console.WriteLine($"  ✓ Found {textureType}: {path}");
                            return new Texture(path);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"  Error loading {path}: {ex.Message}");
                        }
                    }
                }
            }

            // Try fallback names
            foreach (string name in fallbackNames)
            {
                foreach (string dir in directories)
                {
                    string path = Path.Combine(dir, name);
                    if (File.Exists(path))
                    {
                        try
                        {
                            Console.WriteLine($"  ✓ Found {textureType}: {path} (fallback)");
                            return new Texture(path);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"  Error loading {path}: {ex.Message}");
                        }
                    }
                }
            }

            if (allowNull)
            {
                Console.WriteLine($"  - {textureType} not found (optional)");
                return null;
            }
            else
            {
                Console.WriteLine($"  - {textureType} not found, using default");
                return _defaultTexture;
            }
        }

        /// <summary>
        /// Create materials (Module 5 system)
        /// </summary>
        private void CreateMaterials()
        {
            // Container material
            _containerMaterial = new Material(_containerTexture, _containerSpecularTexture);
            _containerMaterial.Shininess = 128.0f;
            _containerMaterial.SpecularIntensity = 1.5f;
            _containerMaterial.AmbientStrength = 0.05f;

            // Brick material
            _brickMaterial = new Material(_brickTexture);
            _brickMaterial.Shininess = 32.0f;
            _brickMaterial.SpecularIntensity = 1.0f;
            _brickMaterial.AmbientStrength = 0.1f;

            // Grid material
            _gridMaterial = Material.CreateColored(new Vector4(0.7f, 0.7f, 0.7f, 1.0f));
            _gridMaterial.Shininess = 16.0f;
            _gridMaterial.SpecularIntensity = 0.2f;
            _gridMaterial.AmbientStrength = 0.3f;

            // Default material for models (use textures if available)
            _defaultMaterial = new Material(_containerTexture);  // Use container texture instead of solid color
            _defaultMaterial.Shininess = 32.0f;
            _defaultMaterial.SpecularIntensity = 0.5f;
            _defaultMaterial.AmbientStrength = 0.2f;
            _defaultMaterial.UseTextures = true;  // Enable textures

            Console.WriteLine("✓ Materials created");
        }

        /// <summary>
        /// Create Module 5's original cubes for comparison
        /// </summary>
        private void CreateMeshes()
        {
            // Create cubes from Module 5
            _cube1 = Mesh.CreateCube(1.0f);
            _cube1.SetMaterial(_containerMaterial);
            _cube1.Position = new Vector3(-3.0f, 0.5f, 0.0f);

            _cube2 = Mesh.CreateCube(1.0f);
            _cube2.SetMaterial(_brickMaterial);
            _cube2.Position = new Vector3(-3.0f, 0.5f, -2.0f);

            // Create grid
            _grid = Mesh.CreateGrid(10.0f, 10.0f, 10);
            _grid.SetMaterial(_gridMaterial);
            _grid.Position = new Vector3(0.0f, -1.0f, 0.0f);

            Console.WriteLine("✓ Module 5 cubes created");
        }

        /// <summary>
        /// Module 6: Load 3D models from OBJ files
        /// </summary>
        private void LoadModels()
        {
            Console.WriteLine("\n=== MODULE 6: Loading 3D Models ===");

            // Try to load the car model
            string carObjPath = "Assets/Models/Car/car.obj";
            bool carLoaded = false;

            if (File.Exists(carObjPath))
            {
                try
                {
                    Console.WriteLine($"Found car model: {carObjPath}");
                    var carModel = new Model(carObjPath, _defaultMaterial);
                    carModel.Name = "Car";

                    // Position the car in the center
                    carModel.Position = new Vector3(0.0f, 0.0f, 0.0f);

                    // Debug: Print original bounding box
                    Console.WriteLine($"  Original bounding box: Min={carModel.BoundingBoxMin}, Max={carModel.BoundingBoxMax}");
                    Console.WriteLine($"  Original size: {carModel.BoundingBoxSize}");

                    // Auto-scale the car to a reasonable size
                    float maxDimension = Math.Max(Math.Max(carModel.BoundingBoxSize.X, carModel.BoundingBoxSize.Y), carModel.BoundingBoxSize.Z);
                    if (maxDimension > 0)
                    {
                        if (maxDimension < 0.5f || maxDimension > 8.0f)
                        {
                            carModel.ScaleToFit(3.0f);
                            Console.WriteLine($"  Car auto-scaled to fit 3.0 units (was {maxDimension:F2})");
                        }
                        else
                        {
                            Console.WriteLine($"  Car size ({maxDimension:F2}) is reasonable, no auto-scaling");
                        }
                    }

                    _models.Add(carModel);
                    carLoaded = true;

                    Console.WriteLine($"✓ Car model loaded successfully!");
                    Console.WriteLine($"  Final Position: {carModel.Position}");
                    Console.WriteLine($"  Final Scale: {carModel.Scale}");
                    Console.WriteLine($"  Final size: {carModel.BoundingBoxSize.X * carModel.Scale.X:F2} x {carModel.BoundingBoxSize.Y * carModel.Scale.Y:F2} x {carModel.BoundingBoxSize.Z * carModel.Scale.Z:F2}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"✗ Error loading car model: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                }
            }
            else
            {
                Console.WriteLine($"✗ Car model not found at: {carObjPath}");
                Console.WriteLine("Expected file structure:");
                Console.WriteLine("  Assets/Models/Car/car.obj");
            }

            // Create spaced test cubes for comparison
            try
            {
                var testCube1 = Model.CreateCube(1.0f, _containerMaterial);
                testCube1.Name = "Test Model Cube 1";
                testCube1.Position = new Vector3(carLoaded ? 4.0f : 2.0f, 1.0f, 0.0f);
                _models.Add(testCube1);

                var testCube2 = Model.CreateCube(1.0f, _brickMaterial);
                testCube2.Name = "Test Model Cube 2";
                testCube2.Position = new Vector3(carLoaded ? 4.0f : 2.0f, 1.0f, 3.0f);
                _models.Add(testCube2);

                Console.WriteLine($"✓ Test model cubes created at {testCube1.Position} and {testCube2.Position}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error creating test model cubes: {ex.Message}");
            }

            Console.WriteLine($"\nTotal models loaded: {_models.Count}");
            Console.WriteLine("=== End Model Loading ===\n");
        }

        private void PrintControls()
        {
            Console.WriteLine("\n" + new string('=', 60));
            Console.WriteLine("MODULE 6: LOADING 3D MODELS - CONTROLS");
            Console.WriteLine(new string('=', 60));
            Console.WriteLine("Camera:");
            Console.WriteLine("  WASD - Move camera");
            Console.WriteLine("  Space/Shift - Move camera up/down");
            Console.WriteLine("  Mouse - Look around (click to capture mouse)");
            Console.WriteLine("  Mouse Wheel - Zoom in/out");
            Console.WriteLine("  R - Reset camera position");
            Console.WriteLine("Controls for testing:");
            Console.WriteLine("  Current model count: " + _models.Count);
            Console.WriteLine("  Car should be at (0,0,0) and rotating");
            Console.WriteLine("  Test cubes should be spread out to the right");
            Console.WriteLine("  Press UP/DOWN to scale - watch the car get bigger/smaller");
            Console.WriteLine("  Press C to center car - should move to align with its center");
            Console.WriteLine("  Press 1 to add cubes - they appear in a line to the right");
            Console.WriteLine("  Press 2 to reset car - should snap back to (0,0,0) with scale 1");
            Console.WriteLine();
            Console.WriteLine("Models:");
            Console.WriteLine("  T - Toggle auto-rotation");
            Console.WriteLine("  F - Toggle wireframe mode");
            Console.WriteLine("  Up/Down Arrow - Scale first model");
            Console.WriteLine("  C - Center first model at origin");
            Console.WriteLine("  1 - Add another test cube");
            Console.WriteLine("  2 - Reset first model position/scale");
            Console.WriteLine();
            Console.WriteLine("Lighting:");
            Console.WriteLine("  L - Toggle light rotation");
            Console.WriteLine("  G - Toggle specular on/off");
            Console.WriteLine();
            Console.WriteLine("  Escape - Exit");
            Console.WriteLine(new string('=', 60));

            if (_models.Count > 0)
            {
                Console.WriteLine($"\nLoaded {_models.Count} model(s):");
                for (int i = 0; i < _models.Count; i++)
                {
                    var model = _models[i];
                    Console.WriteLine($"  {i + 1}. {model.Name} at {model.Position}");
                }
            }

            Console.WriteLine("\nModule 5 objects still available:");
            Console.WriteLine("  - Container cube (left side)");
            Console.WriteLine("  - Brick cube (left side)");
            Console.WriteLine("  - Grid (floor)");
            Console.WriteLine();
        }

        private void OnKeyDown(KeyboardKeyEventArgs e)
        {
            switch (e.Key)
            {
                case Keys.R:
                    CreateCamera();
                    Console.WriteLine("Camera reset");
                    break;

                case Keys.L:
                    _lightRotate = !_lightRotate;
                    Console.WriteLine($"Light rotation: {_lightRotate}");
                    break;

                case Keys.T:
                    _autoRotate = !_autoRotate;
                    Console.WriteLine($"Auto-rotation: {_autoRotate}");
                    break;

                case Keys.F:
                    _showWireframe = !_showWireframe;
                    if (_showWireframe)
                    {
                        GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
                        Console.WriteLine("Wireframe mode enabled");
                    }
                    else
                    {
                        GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
                        Console.WriteLine("Wireframe mode disabled");
                    }
                    break;

                case Keys.G:
                    _specularEnabled = !_specularEnabled;
                    Console.WriteLine($"Specular lighting: {(_specularEnabled ? "ENABLED" : "DISABLED")}");
                    break;

                case Keys.Up:
                    if (_models.Count > 0)
                    {
                        _models[0].Scale *= 1.1f;
                        Console.WriteLine($"Scaled {_models[0].Name} up: Scale={_models[0].Scale.X:F2}, Position={_models[0].Position}");
                    }
                    break;

                case Keys.Down:
                    if (_models.Count > 0)
                    {
                        _models[0].Scale *= 0.9f;
                        Console.WriteLine($"Scaled {_models[0].Name} down: Scale={_models[0].Scale.X:F2}, Position={_models[0].Position}");
                    }
                    break;

                case Keys.C:
                    if (_models.Count > 0)
                    {
                        var oldPos = _models[0].Position;
                        _models[0].CenterAtOrigin();
                        Console.WriteLine($"Centered {_models[0].Name}: {oldPos} -> {_models[0].Position}");
                        Console.WriteLine($"  Bounding box center was: {_models[0].BoundingBoxCenter}");
                    }
                    break;

                case Keys.D1:
                    var newTestCube = Model.CreateCube(1.0f, _containerMaterial);
                    newTestCube.Name = $"Test Cube {_models.Count + 1}";
                    // Place new cubes in a line to the right
                    newTestCube.Position = new Vector3(
                        6.0f + (_models.Count * 2.0f), // Spread them out to the right
                        1.0f,
                        (float)(new Random().NextDouble() * 2 - 1)  // -1 to 1 for slight variation
                    );
                    _models.Add(newTestCube);
                    Console.WriteLine($"Added {newTestCube.Name} at {newTestCube.Position}");
                    break;

                case Keys.D2:
                    if (_models.Count > 0)
                    {
                        var oldPos = _models[0].Position;
                        var oldScale = _models[0].Scale;
                        _models[0].Position = Vector3.Zero;
                        _models[0].Scale = Vector3.One;
                        Console.WriteLine($"Reset {_models[0].Name}:");
                        Console.WriteLine($"  Position: {oldPos} -> {_models[0].Position}");
                        Console.WriteLine($"  Scale: {oldScale} -> {_models[0].Scale}");
                    }
                    break;
            }
        }

        private void OnMouseMove(MouseMoveEventArgs e)
        {
            if (!_mouseCaptured)
                return;

            if (_firstMouseMove)
            {
                _lastMousePosition = new Vector2(e.X, e.Y);
                _firstMouseMove = false;
                return;
            }

            float xOffset = e.X - _lastMousePosition.X;
            float yOffset = e.Y - _lastMousePosition.Y;
            _lastMousePosition = new Vector2(e.X, e.Y);

            _camera.Rotate(xOffset, yOffset);
        }

        private void OnMouseWheel(MouseWheelEventArgs e)
        {
            _camera.AdjustZoom(e.OffsetY);
        }

        private void OnResize(ResizeEventArgs e)
        {
            GL.Viewport(0, 0, e.Width, e.Height);
            _camera.Resize(e.Width, e.Height);
        }

        private void OnUpdateFrame(FrameEventArgs e)
        {
            _time += (float)e.Time;

            if (_window.KeyboardState.IsKeyDown(Keys.Escape))
            {
                _window.Close();
            }

            // Toggle mouse capture
            if (_window.MouseState.IsButtonPressed(MouseButton.Left))
            {
                if (!_mouseCaptured)
                {
                    _mouseCaptured = true;
                    _firstMouseMove = true;
                    _window.CursorState = CursorState.Grabbed;
                }
            }

            if (_window.MouseState.IsButtonPressed(MouseButton.Right))
            {
                if (_mouseCaptured)
                {
                    _mouseCaptured = false;
                    _window.CursorState = CursorState.Normal;
                }
            }

            HandleCameraMovement((float)e.Time);
            UpdateObjects((float)e.Time);
            UpdateLight((float)e.Time);
        }

        private void HandleCameraMovement(float deltaTime)
        {
            float speed = 3.0f * deltaTime;
            Vector3 movement = Vector3.Zero;

            var keyboard = _window.KeyboardState;

            if (keyboard.IsKeyDown(Keys.W)) movement.Z = 1.0f;
            if (keyboard.IsKeyDown(Keys.S)) movement.Z = -1.0f;
            if (keyboard.IsKeyDown(Keys.A)) movement.X = -1.0f;
            if (keyboard.IsKeyDown(Keys.D)) movement.X = 1.0f;
            if (keyboard.IsKeyDown(Keys.Space)) movement.Y = 1.0f;
            if (keyboard.IsKeyDown(Keys.LeftShift)) movement.Y = -1.0f;

            if (movement != Vector3.Zero)
                _camera.Move(movement, speed);
        }

        private void UpdateObjects(float deltaTime)
        {
            if (_autoRotate)
            {
                // Rotate Module 5 cubes
                _cube1.Rotation += new Vector3(0, 15 * deltaTime, 0);
                _cube2.Rotation += new Vector3(15 * deltaTime, 0, 0);

                // Rotate Module 6 models
                for (int i = 0; i < _models.Count; i++)
                {
                    if (i == 0)
                    {
                        // First model (car) rotates around Y axis
                        _models[i].Rotation += new Vector3(0, 30 * deltaTime, 0);
                    }
                    else
                    {
                        // Other models rotate differently for variety
                        _models[i].Rotation += new Vector3(10 * deltaTime, 20 * deltaTime, 0);
                    }
                }
            }
        }

        private void UpdateLight(float deltaTime)
        {
            if (_lightRotate)
            {
                float radius = 3.0f;
                float angle = _time * 0.5f;
                _lightPosition.X = (float)Math.Sin(angle) * radius;
                _lightPosition.Z = (float)Math.Cos(angle) * radius;
            }
        }

        private void OnRenderFrame(FrameEventArgs e)
        {
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            // Get camera matrices
            Matrix4 viewMatrix = _camera.GetViewMatrix();
            Matrix4 projectionMatrix = _camera.GetProjectionMatrix();

            // Set common shader uniforms
            _materialShader.Use();
            _materialShader.SetMatrix4("uView", viewMatrix);
            _materialShader.SetMatrix4("uProjection", projectionMatrix);

            // Set light properties
            _materialShader.SetVector3("light.position", _lightPosition);
            _materialShader.SetVector3("light.color", _lightColor);
            _materialShader.SetFloat("light.intensity", _lightIntensity);
            _materialShader.SetFloat("light.constant", 1.0f);
            _materialShader.SetFloat("light.linear", 0.045f);
            _materialShader.SetFloat("light.quadratic", 0.0075f);
            _materialShader.SetBool("light.isDirectional", false);

            // Global specular toggle
            _materialShader.SetBool("uSpecularEnabled", _specularEnabled);

            // Set camera position for specular calculations
            _materialShader.SetVector3("viewPos", _camera.Position);

            // Render Module 5 objects
            _grid?.Render(_materialShader);
            _cube1?.Render(_materialShader);
            _cube2?.Render(_materialShader);

            // Render Module 6 models
            foreach (var model in _models)
            {
                model?.Render(_materialShader);
            }

            _window.SwapBuffers();
        }

        public void Run()
        {
            _window.Run();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                // Dispose models
                foreach (var model in _models)
                {
                    model?.Dispose();
                }
                _models.Clear();

                // Dispose Module 5 meshes
                _cube1?.Dispose();
                _cube2?.Dispose();
                _grid?.Dispose();

                // Dispose shaders
                _materialShader?.Dispose();

                // Dispose textures
                _containerTexture?.Dispose();
                _containerSpecularTexture?.Dispose();
                _brickTexture?.Dispose();
                _defaultTexture?.Dispose();

                _disposed = true;
                GC.SuppressFinalize(this);
            }
        }
    }
}