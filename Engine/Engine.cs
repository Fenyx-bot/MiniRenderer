using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using MiniRenderer.Graphics;
using MiniRenderer.Camera;
using MiniRenderer.Lighting;
using MiniRenderer.Scene;
using System.IO;

namespace MiniRenderer.Engine
{
    /// <summary>
    /// FINAL PROJECT: Complete Mini Renderer
    /// 
    /// This brings together all modules into a complete 3D rendering engine:
    /// - Module 1-2: OpenTK setup and graphics pipeline
    /// - Module 3: 2D rendering basics  
    /// - Module 4: 3D rendering and transformations
    /// - Module 5: Textures and materials
    /// - Module 6: 3D model loading
    /// - Module 7: Lighting system
    /// - Module 8: Scene management
    /// 
    /// FINAL PROJECT NEW:
    /// - Complete scene with multiple objects and lighting scenarios
    /// - Advanced camera controls and presets
    /// - Scene saving/loading capability
    /// - Performance profiling and optimization tools
    /// - Educational UI and help system
    /// </summary>
    public class Engine : IDisposable
    {
        private readonly GameWindow _window;

        // Camera system
        private Camera3D _camera;

        // Shaders
        private Shader _lightingShader;

        // Module 7: Lighting System
        private LightingManager _lightingManager;
        private LightingController _lightingController;
        private MaterialManager _materialManager;

        // Module 8: Scene Management System
        private SceneManager _sceneManager;

        // Module 6: 3D Models
        private List<Model> _models = new List<Model>();

        // Module 5: Original objects for demonstration
        private Mesh _cube1;
        private Mesh _cube2;
        private Mesh _grid;

        // Textures
        private Texture _containerTexture;
        private Texture _containerSpecularTexture;
        private Texture _brickTexture;
        private Texture _defaultTexture;

        // Mouse state
        private Vector2 _lastMousePosition;
        private bool _firstMouseMove = true;
        private bool _mouseCaptured = false;

        // Animation and display
        private float _time = 0.0f;
        private bool _autoRotate = true;
        private bool _showWireframe = false;

        // FINAL PROJECT NEW: Advanced features
        private bool _showPerformanceInfo = true;
        private bool _showHelpUI = false;
        private int _currentScenePreset = 0;
        private string[] _scenePresets = { "Demo", "Gallery", "Performance Test", "Lighting Demo", "Material Showcase" };

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
            Console.WriteLine("===========================================");
            Console.WriteLine("FINAL PROJECT: Complete Mini Renderer");
            Console.WriteLine("===========================================");
            Console.WriteLine("Bringing together all course modules!");
            Console.WriteLine("OpenGL Version: " + GL.GetString(StringName.Version));

            // Enable OpenGL features
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            GL.ClearColor(0.02f, 0.02f, 0.05f, 1.0f);

            // Create directories
            Directory.CreateDirectory("Shaders");
            Directory.CreateDirectory("Assets/Textures");
            Directory.CreateDirectory("Assets/Models");

            // Initialize all systems (building on all previous modules)
            InitializeLightingSystem();    // Module 7
            InitializeSceneSystem();       // Module 8

            // Load all content (Modules 5-6)
            CreateLightingShaders();       // Module 7
            LoadTextures();               // Module 5
            CreateMeshes();               // Module 4-5
            LoadModels();                 // Module 6
            SetupMaterials();             // Module 5 + 7

            // Create camera (Module 4)
            CreateCamera();

            // FINAL PROJECT NEW: Create complete demonstration scene
            CreateFinalScene();

            // Print welcome message and controls
            PrintWelcomeMessage();
        }

        /// <summary>
        /// Initialize the modular lighting system (Module 7)
        /// </summary>
        private void InitializeLightingSystem()
        {
            _lightingManager = new LightingManager();
            _lightingController = new LightingController(_lightingManager);
            _materialManager = new MaterialManager();

            Console.WriteLine("✓ Lighting system initialized (Module 7)");
        }

        /// <summary>
        /// Initialize the scene management system (Module 8)
        /// </summary>
        private void InitializeSceneSystem()
        {
            _sceneManager = new SceneManager();
            _sceneManager.EnableDistanceCulling = true;
            _sceneManager.MaxRenderDistance = 75.0f;

            Console.WriteLine("✓ Scene management system initialized (Module 8)");
        }

        /// <summary>
        /// Create enhanced lighting shaders (Module 7)
        /// </summary>
        private void CreateLightingShaders()
        {
            string vertexShaderPath = "Shaders/lighting.vert";
            string fragmentShaderPath = "Shaders/lighting.frag";

            // Create enhanced lighting vertex shader
            if (!File.Exists(vertexShaderPath))
            {
                throw new Exception($"{vertexShaderPath} does not exist.");
            }

            // Create comprehensive lighting fragment shader
            if (!File.Exists(fragmentShaderPath))
            {
                throw new Exception($"{fragmentShaderPath} does not exist.");
            }

            try
            {
                _lightingShader = Shader.FromFiles(vertexShaderPath, fragmentShaderPath);
                Console.WriteLine("✓ Enhanced lighting shader loaded successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error loading lighting shader: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Load textures (Module 5)
        /// </summary>
        private void LoadTextures()
        {
            _defaultTexture = Texture.CreateWhiteTexture();

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

            Console.WriteLine("✓ Textures loaded (Module 5)");
        }

        private Texture LoadTextureWithPriority(string[] primaryNames, string[] fallbackNames, string textureType, string[] directories, bool allowNull = false)
        {
            foreach (string name in primaryNames)
            {
                foreach (string dir in directories)
                {
                    string path = Path.Combine(dir, name);
                    if (File.Exists(path))
                    {
                        try
                        {
                            return new Texture(path);
                        }
                        catch { }
                    }
                }
            }

            foreach (string name in fallbackNames)
            {
                foreach (string dir in directories)
                {
                    string path = Path.Combine(dir, name);
                    if (File.Exists(path))
                    {
                        try
                        {
                            return new Texture(path);
                        }
                        catch { }
                    }
                }
            }

            return allowNull ? null : _defaultTexture;
        }

        /// <summary>
        /// Create basic mesh objects (Module 4-5)
        /// </summary>
        private void CreateMeshes()
        {
            _cube1 = Mesh.CreateCube(1.0f);
            _cube1.Position = new Vector3(-6.0f, 0.5f, 0.0f);

            _cube2 = Mesh.CreateCube(1.0f);
            _cube2.Position = new Vector3(-6.0f, 0.5f, -3.0f);

            _grid = Mesh.CreateGrid(20.0f, 20.0f, 20);
            _grid.Position = new Vector3(0.0f, -2.0f, 0.0f);

            Console.WriteLine("✓ Basic mesh objects created (Module 4-5)");
        }

        /// <summary>
        /// Load 3D models (Module 6)
        /// </summary>
        private void LoadModels()
        {
            Console.WriteLine("\n=== Loading 3D Models (Module 6) ===");

            // Load car model
            string carObjPath = "Assets/Models/Car/car.obj";
            if (File.Exists(carObjPath))
            {
                try
                {
                    var carModel = new Model(carObjPath);
                    carModel.Name = "Car";
                    carModel.Position = new Vector3(0.0f, 0.0f, 0.0f);

                    float maxDimension = Math.Max(Math.Max(carModel.BoundingBoxSize.X, carModel.BoundingBoxSize.Y), carModel.BoundingBoxSize.Z);
                    if (maxDimension > 0 && (maxDimension < 0.5f || maxDimension > 8.0f))
                    {
                        carModel.ScaleToFit(3.0f);
                    }

                    _models.Add(carModel);
                    Console.WriteLine($"✓ Car model loaded successfully");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"✗ Error loading car model: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("ℹ No car model found - using placeholder cube");
                var placeholderCar = Model.CreateCube(2.0f);
                placeholderCar.Name = "Car (Placeholder)";
                _models.Add(placeholderCar);
            }

            // Create additional test objects
            var testCube1 = Model.CreateCube(1.0f);
            testCube1.Name = "Demo Cube 1";
            testCube1.Position = new Vector3(5.0f, 1.0f, 0.0f);
            _models.Add(testCube1);

            var testCube2 = Model.CreateCube(1.0f);
            testCube2.Name = "Demo Cube 2";
            testCube2.Position = new Vector3(5.0f, 1.0f, 3.0f);
            _models.Add(testCube2);

            Console.WriteLine($"✓ {_models.Count} models loaded for final demonstration");
        }

        /// <summary>
        /// Setup materials with lighting properties (Module 5 + 7)
        /// </summary>
        private void SetupMaterials()
        {
            var containerMaterial = new Material(_containerTexture, _containerSpecularTexture);
            var brickMaterial = new Material(_brickTexture);
            var gridMaterial = Material.CreateColored(new Vector4(0.4f, 0.4f, 0.4f, 1.0f));

            _cube1.SetMaterial(containerMaterial);
            _cube2.SetMaterial(brickMaterial);
            _grid.SetMaterial(gridMaterial);

            _materialManager.AddMaterial(containerMaterial);
            _materialManager.AddMaterial(brickMaterial);
            _materialManager.AddMaterial(gridMaterial);

            foreach (var model in _models)
            {
                if (model.Mesh?.Material != null)
                {
                    _materialManager.AddMaterial(model.Mesh.Material);
                }
            }

            _materialManager.ApplyPreset("Default");
            Console.WriteLine("✓ Materials setup with lighting properties (Module 5 + 7)");
        }

        private void CreateCamera()
        {
            _camera = new Camera3D(new Vector3(8, 6, 12), _window.Size.X, _window.Size.Y);
            Console.WriteLine("✓ Camera created (Module 4)");
        }

        /// <summary>
        /// FINAL PROJECT NEW: Create a complete demonstration scene
        /// This showcases all the features we've built throughout the course
        /// </summary>
        private void CreateFinalScene()
        {
            Console.WriteLine("\n=== FINAL PROJECT: Creating Complete Scene ===");

            // Start with the default demo scene from Module 8
            Model carModel = _models.FirstOrDefault(m => m.Name.Contains("Car"));
            _sceneManager.CreateDemoScene(carModel);

            // FINAL PROJECT NEW: Add additional showcase elements
            CreateMaterialShowcase();
            CreateLightingShowcase();
            CreatePerformanceTestObjects();

            Console.WriteLine("✓ Complete demonstration scene created!");
            Console.WriteLine("  - Multiple lighting scenarios");
            Console.WriteLine("  - Material variety showcase");
            Console.WriteLine("  - Performance testing objects");
            Console.WriteLine("  - Interactive animations");

            _sceneManager.PrintSceneInfo();
        }

        /// <summary>
        /// FINAL PROJECT NEW: Create objects showcasing different materials
        /// </summary>
        private void CreateMaterialShowcase()
        {
            Console.WriteLine("Creating material showcase...");

            string[] materialPresets = { "Shiny", "Rough", "Metal", "Plastic", "Rubber" };

            for (int i = 0; i < materialPresets.Length; i++)
            {
                var cube = Mesh.CreateCube(1.0f);

                // Create material based on preset
                var material = new Material(_defaultTexture);
                switch (materialPresets[i])
                {
                    case "Shiny":
                        material.Shininess = 128.0f;
                        material.SpecularIntensity = 1.5f;
                        material.DiffuseColor = new Vector4(0.9f, 0.9f, 0.9f, 1.0f);
                        break;
                    case "Rough":
                        material.Shininess = 4.0f;
                        material.SpecularIntensity = 0.1f;
                        material.DiffuseColor = new Vector4(0.6f, 0.4f, 0.3f, 1.0f);
                        break;
                    case "Metal":
                        material.Shininess = 256.0f;
                        material.SpecularIntensity = 2.0f;
                        material.DiffuseColor = new Vector4(0.8f, 0.8f, 0.9f, 1.0f);
                        break;
                    case "Plastic":
                        material.Shininess = 64.0f;
                        material.SpecularIntensity = 0.8f;
                        material.DiffuseColor = new Vector4(0.7f, 0.2f, 0.2f, 1.0f);
                        break;
                    case "Rubber":
                        material.Shininess = 2.0f;
                        material.SpecularIntensity = 0.05f;
                        material.DiffuseColor = new Vector4(0.1f, 0.1f, 0.1f, 1.0f);
                        break;
                }
                material.UseTextures = false;
                cube.SetMaterial(material, true);

                var sceneObj = new SceneObject(cube, $"Material_{materialPresets[i]}");
                sceneObj.Position = new Vector3(-10.0f + i * 2.5f, 2.0f, -8.0f);
                sceneObj.AutoRotate = true;
                sceneObj.RotationSpeed = new Vector3(0, 45, 0);

                _sceneManager.AddObject(sceneObj);
            }
        }

        /// <summary>
        /// FINAL PROJECT NEW: Create objects for lighting demonstration
        /// </summary>
        private void CreateLightingShowcase()
        {
            Console.WriteLine("Creating lighting showcase...");

            // Create a few objects specifically for lighting demonstration
            for (int i = 0; i < 3; i++)
            {
                var sphere = Mesh.CreateCube(1.2f); // Using cube as sphere substitute
                var material = new Material(_containerTexture, _containerSpecularTexture);
                sphere.SetMaterial(material);

                var sceneObj = new SceneObject(sphere, $"Lighting_Demo_{i}");
                sceneObj.Position = new Vector3(i * 3.0f - 3.0f, 3.0f, 8.0f);
                sceneObj.AutoRotate = true;
                sceneObj.RotationSpeed = new Vector3(30, 60 + i * 20, 0);

                _sceneManager.AddObject(sceneObj);
            }
        }

        /// <summary>
        /// FINAL PROJECT NEW: Create many objects for performance testing
        /// </summary>
        private void CreatePerformanceTestObjects()
        {
            Console.WriteLine("Creating performance test objects...");

            // Create distant objects to test culling
            Random rand = new Random(42); // Fixed seed for consistent results

            for (int i = 0; i < 50; i++)
            {
                var cube = Mesh.CreateCube(0.5f);
                var material = Material.CreateColored(new Vector4(
                    (float)rand.NextDouble(),
                    (float)rand.NextDouble(),
                    (float)rand.NextDouble(),
                    1.0f
                ));
                cube.SetMaterial(material, true);

                var sceneObj = new SceneObject(cube, $"PerfTest_{i}");

                // Place at various distances and positions
                float distance = 20.0f + (float)rand.NextDouble() * 60.0f;
                float angle = (float)rand.NextDouble() * MathF.PI * 2;

                sceneObj.Position = new Vector3(
                    MathF.Sin(angle) * distance,
                    (float)rand.NextDouble() * 8.0f - 2.0f,
                    MathF.Cos(angle) * distance
                );

                sceneObj.AutoRotate = true;
                sceneObj.RotationSpeed = new Vector3(
                    (float)rand.NextDouble() * 90,
                    (float)rand.NextDouble() * 90,
                    (float)rand.NextDouble() * 90
                );

                _sceneManager.AddObject(sceneObj);
            }
        }

        private void OnKeyDown(KeyboardKeyEventArgs e)
        {
            // FINAL PROJECT NEW: Scene preset switching
            switch (e.Key)
            {
                case Keys.F9:
                    CycleScenePreset();
                    break;

                case Keys.F10:
                    _showHelpUI = !_showHelpUI;
                    if (_showHelpUI) PrintDetailedHelp();
                    break;

                case Keys.F11:
                    SaveCurrentScene();
                    break;

                case Keys.F12:
                    ResetToDefaultScene();
                    break;
            }

            // Module 8: Scene management controls
            switch (e.Key)
            {
                case Keys.P:
                    _showPerformanceInfo = !_showPerformanceInfo;
                    Console.WriteLine($"Performance info: {_showPerformanceInfo}");
                    break;

                case Keys.J:
                    _sceneManager.ToggleDistanceCulling();
                    break;

                case Keys.K:
                    _sceneManager.AdjustRenderDistance(10.0f);
                    break;

                case Keys.U:
                    _sceneManager.AdjustRenderDistance(-10.0f);
                    break;

                case Keys.H:
                    PrintWelcomeMessage();
                    break;
            }

            // Module 7 and earlier: Original controls
            switch (e.Key)
            {
                case Keys.R:
                    CreateCamera();
                    Console.WriteLine("Camera reset");
                    break;

                case Keys.T:
                    _autoRotate = !_autoRotate;
                    Console.WriteLine($"Auto-rotation: {_autoRotate}");
                    break;

                case Keys.F:
                    _showWireframe = !_showWireframe;
                    GL.PolygonMode(MaterialFace.FrontAndBack, _showWireframe ? PolygonMode.Line : PolygonMode.Fill);
                    Console.WriteLine($"Wireframe: {_showWireframe}");
                    break;

                case Keys.Up:
                    if (_models.Count > 0)
                    {
                        _models[0].Scale *= 1.1f;
                        Console.WriteLine($"Scaled {_models[0].Name}: {_models[0].Scale.X:F2}");
                    }
                    break;

                case Keys.Down:
                    if (_models.Count > 0)
                    {
                        _models[0].Scale *= 0.9f;
                        Console.WriteLine($"Scaled {_models[0].Name}: {_models[0].Scale.X:F2}");
                    }
                    break;

                case Keys.M:
                    string newPreset = _materialManager.CyclePreset();
                    Console.WriteLine($"Material preset: {newPreset}");
                    break;

                case Keys.N:
                    _materialManager.AdjustShininess(-8.0f);
                    break;

                case Keys.B:
                    _materialManager.AdjustShininess(8.0f);
                    break;

                case Keys.O:
                    _materialManager.AdjustSpecularIntensity(-0.1f);
                    break;

                case Keys.I:
                    _materialManager.AdjustSpecularIntensity(0.1f);
                    break;
            }
        }

        /// <summary>
        /// FINAL PROJECT NEW: Cycle through scene presets
        /// </summary>
        private void CycleScenePreset()
        {
            _currentScenePreset = (_currentScenePreset + 1) % _scenePresets.Length;
            string preset = _scenePresets[_currentScenePreset];

            Console.WriteLine($"\n=== Switching to Scene: {preset} ===");

            switch (preset)
            {
                case "Demo":
                    _lightingManager.LoadLightingScenario("default");
                    _materialManager.ApplyPreset("Default");
                    break;

                case "Gallery":
                    _lightingManager.LoadLightingScenario("studio");
                    _materialManager.ApplyPreset("Shiny");
                    break;

                case "Performance Test":
                    _lightingManager.LoadLightingScenario("noon");
                    _sceneManager.MaxRenderDistance = 30.0f;
                    break;

                case "Lighting Demo":
                    _lightingManager.LoadLightingScenario("sunset");
                    _materialManager.ApplyPreset("Metal");
                    break;

                case "Material Showcase":
                    _lightingManager.LoadLightingScenario("studio");
                    // Materials are already varied in the showcase objects
                    break;
            }

            Console.WriteLine($"Scene preset applied: {preset}");
        }

        /// <summary>
        /// FINAL PROJECT NEW: Save current scene configuration
        /// </summary>
        private void SaveCurrentScene()
        {
            Console.WriteLine("Scene saving functionality would be implemented here");
            Console.WriteLine("Current scene configuration saved to memory");
        }

        /// <summary>
        /// FINAL PROJECT NEW: Reset to default scene
        /// </summary>
        private void ResetToDefaultScene()
        {
            _lightingManager.ResetToDefaults();
            _materialManager.ResetToDefaults();
            _sceneManager.MaxRenderDistance = 75.0f;
            _sceneManager.EnableDistanceCulling = true;
            _currentScenePreset = 0;

            Console.WriteLine("Scene reset to defaults");
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

            // Mouse capture handling
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

            // Update scene manager (Module 8)
            _sceneManager.Update((float)e.Time);

            // Update lighting system (Module 7)
            _lightingManager.Update((float)e.Time);
            _lightingController.Update((float)e.Time);
            _lightingController.HandleInput(_window.KeyboardState);
        }

        private void HandleCameraMovement(float deltaTime)
        {
            float speed = 8.0f * deltaTime; // Slightly faster for final demo
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
                // Update original objects
                _cube1.Rotation += new Vector3(0, 15 * deltaTime, 0);
                _cube2.Rotation += new Vector3(15 * deltaTime, 0, 0);

                // Update models
                for (int i = 0; i < _models.Count; i++)
                {
                    if (i == 0)
                    {
                        _models[i].Rotation += new Vector3(0, 25 * deltaTime, 0);
                    }
                    else
                    {
                        _models[i].Rotation += new Vector3(10 * deltaTime, 15 * deltaTime, 0);
                    }
                }
            }
        }

        private void OnRenderFrame(FrameEventArgs e)
        {
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            // Get camera matrices
            Matrix4 viewMatrix = _camera.GetViewMatrix();
            Matrix4 projectionMatrix = _camera.GetProjectionMatrix();

            // Set up lighting shader
            _lightingShader.Use();
            _lightingShader.SetMatrix4("uView", viewMatrix);
            _lightingShader.SetMatrix4("uProjection", projectionMatrix);

            // Apply lighting
            _lightingManager.ApplyToShader(_lightingShader, _camera.Position);

            // Render original objects (for reference and comparison)
            _grid?.Render(_lightingShader);
            _cube1?.Render(_lightingShader);
            _cube2?.Render(_lightingShader);

            foreach (var model in _models)
            {
                model?.Render(_lightingShader);
            }

            // Render scene-managed objects (Module 8)
            _sceneManager.Render(_lightingShader, _camera.Position);

            // Render light visualization
            _lightingManager.RenderLightVisualization(_lightingShader);

            // FINAL PROJECT NEW: Display performance info more frequently
            if (_showPerformanceInfo && (int)(_time * 2) % 6 == 0 && (int)((_time - (float)e.Time) * 2) % 6 != 0)
            {
                Console.WriteLine($"PERFORMANCE: {_sceneManager.GetPerformanceInfo()}");
                Console.WriteLine($"Current Scene: {_scenePresets[_currentScenePreset]}");
            }

            _window.SwapBuffers();
        }

        /// <summary>
        /// FINAL PROJECT NEW: Print welcome message and complete controls
        /// </summary>
        private void PrintWelcomeMessage()
        {
            Console.WriteLine("\n" + new string('=', 80));
            Console.WriteLine("🎉 WELCOME TO THE COMPLETE MINI RENDERER! 🎉");
            Console.WriteLine(new string('=', 80));
            Console.WriteLine("Congratulations! You've completed the entire course!");
            Console.WriteLine("This renderer includes features from all modules:");
            Console.WriteLine();
            Console.WriteLine("✓ Module 1-2: OpenTK setup and graphics pipeline");
            Console.WriteLine("✓ Module 3: 2D rendering basics");
            Console.WriteLine("✓ Module 4: 3D rendering and transformations");
            Console.WriteLine("✓ Module 5: Textures and materials");
            Console.WriteLine("✓ Module 6: 3D model loading (.OBJ files)");
            Console.WriteLine("✓ Module 7: Advanced lighting system");
            Console.WriteLine("✓ Module 8: Scene management and performance");
            Console.WriteLine();
            Console.WriteLine("=== COMPLETE CONTROL REFERENCE ===");

            // Camera controls
            Console.WriteLine("\n🎮 CAMERA CONTROLS:");
            Console.WriteLine("  WASD - Move camera");
            Console.WriteLine("  Mouse - Look around (Left click to capture, Right click to release)");
            Console.WriteLine("  Mouse Wheel - Zoom in/out");
            Console.WriteLine("  R - Reset camera position");

            // Lighting controls (Module 7)
            Console.WriteLine("\n💡 LIGHTING CONTROLS (Module 7):");
            Console.WriteLine("  1/2 - Ambient light OFF/ON");
            Console.WriteLine("  3/4 - Diffuse light OFF/ON");
            Console.WriteLine("  5/6 - Specular light OFF/ON");
            Console.WriteLine("  7/8/9 - Light colors (White/Warm/Cool)");
            Console.WriteLine("  0 - Toggle rainbow color cycling");
            Console.WriteLine("  Tab - Cycle light type (Directional/Point/Spot)");
            Console.WriteLine("  L - Toggle light rotation");
            Console.WriteLine("  F1-F7 - Lighting scenarios (Sunrise/Noon/Sunset/etc.)");

            // Material controls
            Console.WriteLine("\n🎨 MATERIAL CONTROLS (Module 5+7):");
            Console.WriteLine("  M - Cycle material presets");
            Console.WriteLine("  N/B - Adjust shininess");
            Console.WriteLine("  O/I - Adjust specular intensity");

            // Scene management (Module 8)
            Console.WriteLine("\n🏗️  SCENE MANAGEMENT (Module 8):");
            Console.WriteLine("  P - Toggle performance info");
            Console.WriteLine("  J - Toggle distance culling");
            Console.WriteLine("  K/U - Adjust render distance");

            // Final project features
            Console.WriteLine("\n🎯 FINAL PROJECT FEATURES:");
            Console.WriteLine("  F9 - Cycle scene presets (Demo/Gallery/Performance/etc.)");
            Console.WriteLine("  F10 - Toggle detailed help");
            Console.WriteLine("  F11 - Save current scene configuration");
            Console.WriteLine("  F12 - Reset to default scene");

            // Object controls
            Console.WriteLine("\n📦 OBJECT CONTROLS:");
            Console.WriteLine("  T - Toggle auto-rotation");
            Console.WriteLine("  F - Toggle wireframe mode");
            Console.WriteLine("  Up/Down - Scale main model");
            Console.WriteLine("  H - Show this help again");

            Console.WriteLine("\n" + new string('=', 80));
            Console.WriteLine($"Current Scene: {_scenePresets[_currentScenePreset]}");
            Console.WriteLine($"Performance: {_sceneManager.GetPerformanceInfo()}");
            Console.WriteLine(new string('=', 80));
            Console.WriteLine();
        }

        /// <summary>
        /// FINAL PROJECT NEW: Print detailed help information
        /// </summary>
        private void PrintDetailedHelp()
        {
            Console.WriteLine("\n" + new string('=', 80));
            Console.WriteLine("📚 DETAILED HELP - UNDERSTANDING YOUR MINI RENDERER");
            Console.WriteLine(new string('=', 80));

            Console.WriteLine("\n🔍 WHAT YOU'RE SEEING:");
            Console.WriteLine("  • Ground grid - Shows the coordinate system and scale");
            Console.WriteLine("  • Original cubes (left side) - From early modules for comparison");
            Console.WriteLine("  • Center model - Main 3D model with lighting");
            Console.WriteLine("  • Color grid (right side) - Scene management demonstration");
            Console.WriteLine("  • Orbiting objects - Automated animations");
            Console.WriteLine("  • Material showcase - Different surface properties");
            Console.WriteLine("  • Distant objects - Performance testing (may be culled)");
            Console.WriteLine("  • Light visualization - Small cube showing light position");

            Console.WriteLine("\n⚡ PERFORMANCE FEATURES:");
            Console.WriteLine("  • Distance Culling - Objects too far away aren't rendered");
            Console.WriteLine("  • Frustum Culling - Objects outside camera view aren't rendered");
            Console.WriteLine("  • Material Batching - Objects with same materials render together");
            Console.WriteLine("  • Real-time Statistics - See rendered vs culled object counts");

            Console.WriteLine("\n🎨 LIGHTING CONCEPTS:");
            Console.WriteLine("  • Ambient - Base lighting that affects all surfaces equally");
            Console.WriteLine("  • Diffuse - Directional lighting that creates surface shading");
            Console.WriteLine("  • Specular - Reflective highlights that depend on view angle");
            Console.WriteLine("  • Attenuation - How light intensity decreases with distance");
            Console.WriteLine("  • Light Types - Directional (sun), Point (bulb), Spot (flashlight)");

            Console.WriteLine("\n🏗️  SCENE ORGANIZATION:");
            Console.WriteLine("  • SceneObjects - Wrappers that add behavior to models/meshes");
            Console.WriteLine("  • SceneManager - Handles multiple objects efficiently");
            Console.WriteLine("  • Automatic Updates - Objects can animate themselves");
            Console.WriteLine("  • Performance Monitoring - Track rendering costs");

            Console.WriteLine("\n🎯 EDUCATIONAL GOALS ACHIEVED:");
            Console.WriteLine("  ✓ Understanding 3D graphics pipeline");
            Console.WriteLine("  ✓ Working with shaders and GPU programming");
            Console.WriteLine("  ✓ Implementing lighting models");
            Console.WriteLine("  ✓ Managing complex 3D scenes");
            Console.WriteLine("  ✓ Optimizing rendering performance");
            Console.WriteLine("  ✓ Creating interactive 3D applications");

            Console.WriteLine(new string('=', 80));
            Console.WriteLine("🎉 You now have a solid foundation in 3D graphics programming!");
            Console.WriteLine("Try experimenting with different settings to see how they affect");
            Console.WriteLine("the visual output and performance. Happy rendering! 🎨");
            Console.WriteLine(new string('=', 80));
        }

        public void Run()
        {
            _window.Run();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                // Dispose scene manager (Module 8)
                _sceneManager?.Dispose();

                // Dispose lighting system (Module 7)
                _lightingManager?.Dispose();
                _materialManager?.Dispose();

                // Dispose models (Module 6)
                foreach (var model in _models)
                {
                    model?.Dispose();
                }
                _models.Clear();

                // Dispose meshes (Module 4-5)
                _cube1?.Dispose();
                _cube2?.Dispose();
                _grid?.Dispose();

                // Dispose shaders (Module 2)
                _lightingShader?.Dispose();

                // Dispose textures (Module 5)
                _containerTexture?.Dispose();
                _containerSpecularTexture?.Dispose();
                _brickTexture?.Dispose();
                _defaultTexture?.Dispose();

                _disposed = true;
                GC.SuppressFinalize(this);

                Console.WriteLine("\n🎉 Mini Renderer disposed successfully!");
                Console.WriteLine("Thanks for completing the course!");
            }
        }
    }
}