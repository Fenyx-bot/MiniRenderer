using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using MiniRenderer.Graphics;
using MiniRenderer.Camera;
using MiniRenderer.Lighting;
using MiniRenderer.Scene; // MODULE 8 NEW: Added for scene management
using System.IO;

namespace MiniRenderer.Engine
{
    /// <summary>
    /// Module 7: Lighting Basics - Clean modular version
    /// MODULE 8 UPDATE: Added Scene Management for handling multiple objects efficiently
    /// 
    /// New in Module 8:
    /// - SceneManager for organizing multiple objects
    /// - Simple distance culling for performance
    /// - Performance monitoring and statistics
    /// - All Module 7 features remain unchanged!
    /// </summary>
    public class Engine : IDisposable
    {
        private readonly GameWindow _window;

        // Camera
        private Camera3D _camera;

        // Shaders
        private Shader _lightingShader;

        // Module 7: Lighting System (unchanged)
        private LightingManager _lightingManager;
        private LightingController _lightingController;
        private MaterialManager _materialManager;

        // MODULE 8 NEW: Scene Management System
        private SceneManager _sceneManager;

        // Module 6: 3D Models (unchanged)
        private List<Model> _models = new List<Model>();

        // Module 5: Original objects for comparison (unchanged)
        private Mesh _cube1;
        private Mesh _cube2;
        private Mesh _grid;

        // Textures (unchanged)
        private Texture _containerTexture;
        private Texture _containerSpecularTexture;
        private Texture _brickTexture;
        private Texture _defaultTexture;

        // Mouse state (unchanged)
        private Vector2 _lastMousePosition;
        private bool _firstMouseMove = true;
        private bool _mouseCaptured = false;

        // Animation (unchanged)
        private float _time = 0.0f;
        private bool _autoRotate = true;
        private bool _showWireframe = false;

        // MODULE 8 NEW: Performance monitoring
        private bool _showPerformanceInfo = true;

        // Flag for proper resource disposal
        private bool _disposed = false;

        public Engine(GameWindow window)
        {
            _window = window;

            // Set up event handlers (unchanged)
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
            Console.WriteLine("Module 7: Lighting Basics (Modular Architecture)");
            Console.WriteLine("MODULE 8 UPDATE: Now with Scene Management!"); // MODULE 8 NEW
            Console.WriteLine("OpenGL Version: " + GL.GetString(StringName.Version));

            // Enable depth testing (unchanged)
            GL.Enable(EnableCap.DepthTest);

            // Enable alpha blending (unchanged)
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            // Set background color (unchanged)
            GL.ClearColor(0.02f, 0.02f, 0.05f, 1.0f);

            // Create directories (unchanged)
            Directory.CreateDirectory("Shaders");
            Directory.CreateDirectory("Assets/Textures");
            Directory.CreateDirectory("Assets/Models");

            // Initialize lighting system first (Module 7 - unchanged)
            InitializeLightingSystem();

            // MODULE 8 NEW: Initialize scene management system
            InitializeSceneSystem();

            // Initialize content (builds on Modules 5 & 6 - unchanged)
            CreateLightingShaders();
            LoadTextures();
            CreateMeshes();
            LoadModels();
            SetupMaterials();

            // Create camera (unchanged)
            CreateCamera();

            // MODULE 8 NEW: Create demo scene with scene manager
            CreateDemoScene();

            // Print controls (updated for Module 8)
            PrintControls();
        }

        /// <summary>
        /// Initialize the modular lighting system (Module 7 - UNCHANGED)
        /// </summary>
        private void InitializeLightingSystem()
        {
            // Create lighting manager with default setup
            _lightingManager = new LightingManager();

            // Create lighting controller for user input
            _lightingController = new LightingController(_lightingManager);

            // Create material manager for educational material presets
            _materialManager = new MaterialManager();

            Console.WriteLine("✓ Modular lighting system initialized");
        }

        /// <summary>
        /// MODULE 8 NEW: Initialize the scene management system
        /// This is the main addition for Module 8
        /// </summary>
        private void InitializeSceneSystem()
        {
            _sceneManager = new SceneManager();

            // Configure basic performance settings that students can experiment with
            _sceneManager.EnableDistanceCulling = true;
            _sceneManager.MaxRenderDistance = 50.0f;

            Console.WriteLine("✓ Scene management system initialized (MODULE 8 NEW)");
        }

        /// <summary>
        /// Create enhanced lighting shaders (Module 7 - UNCHANGED)
        /// </summary>
        private void CreateLightingShaders()
        {
            // Same implementation as Module 7 - no changes needed
            string vertexShaderPath = "Shaders/lighting.vert";
            string fragmentShaderPath = "Shaders/lighting.frag";

            // Create enhanced lighting vertex shader
            if (!File.Exists(vertexShaderPath))
            {
                File.WriteAllText(vertexShaderPath, @"#version 330 core
layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec2 aTexCoord;
layout(location = 2) in vec3 aNormal;
layout(location = 3) in vec4 aColor;

// Output to fragment shader
out vec2 texCoord;
out vec3 normal;
out vec3 fragPos;
out vec4 vertexColor;

// Transformation matrices
uniform mat4 uModel;
uniform mat4 uView;
uniform mat4 uProjection;

// Texture tiling parameters
uniform vec2 uTextureScale = vec2(1.0, 1.0);
uniform vec2 uTextureOffset = vec2(0.0, 0.0);

void main()
{
    // Calculate world space position
    vec4 worldPos = uModel * vec4(aPosition, 1.0);
    fragPos = worldPos.xyz;
    
    // Apply MVP transformation
    gl_Position = uProjection * uView * worldPos;
    
    // Transform normal to world space (important for lighting)
    normal = mat3(transpose(inverse(uModel))) * aNormal;
    
    // Pass texture coordinates with tiling
    texCoord = (aTexCoord * uTextureScale) + uTextureOffset;
    
    // Pass vertex color
    vertexColor = aColor;
}");
            }

            // Create comprehensive lighting fragment shader
            if (!File.Exists(fragmentShaderPath))
            {
                File.WriteAllText(fragmentShaderPath, @"#version 330 core
// Input from vertex shader
in vec2 texCoord;
in vec3 normal;
in vec3 fragPos;
in vec4 vertexColor;

// Output
out vec4 FragColor;

// Material properties
struct Material {
    bool useTextures;
    vec4 diffuseColor;
    float specularIntensity;
    float shininess;
    float ambientStrength;
    float alpha;
    sampler2D diffuseMap;
    sampler2D specularMap;
    bool hasSpecularMap;
};

// Light properties
struct Light {
    int type; // 0=Directional, 1=Point, 2=Spot
    vec3 position;
    vec3 direction;
    vec3 color;
    float intensity;
    
    // Attenuation (for point/spot lights)
    float constant;
    float linear;
    float quadratic;
    
    // Spot light specific
    float cutOff;
    float outerCutOff;
};

// Uniforms
uniform Material material;
uniform Light light;
uniform vec3 viewPos;

// Lighting component toggles
uniform bool enableAmbient;
uniform bool enableDiffuse;
uniform bool enableSpecular;
uniform float ambientStrength;
uniform float diffuseStrength;
uniform float specularStrength;
uniform vec3 ambientColor;

void main()
{
    // Normalize the normal
    vec3 norm = normalize(normal);
    vec3 viewDir = normalize(viewPos - fragPos);
    
    // Get base color
    vec3 baseColor;
    if (material.useTextures) {
        baseColor = texture(material.diffuseMap, texCoord).rgb * vertexColor.rgb;
    } else {
        baseColor = material.diffuseColor.rgb * vertexColor.rgb;
    }
    
    // Simple lighting calculation (keeping it simple for students)
    vec3 lightDir;
    float attenuation = 1.0;
    
    if (light.type == 0) { // Directional
        lightDir = normalize(-light.direction);
    } else { // Point
        lightDir = normalize(light.position - fragPos);
        float distance = length(light.position - fragPos);
        attenuation = 1.0 / (light.constant + light.linear * distance + light.quadratic * (distance * distance));
    }
    
    // Calculate lighting components
    vec3 ambient = enableAmbient ? ambientStrength * ambientColor : vec3(0.0);
    
    vec3 diffuse = vec3(0.0);
    if (enableDiffuse) {
        float diff = max(dot(norm, lightDir), 0.0);
        diffuse = diff * light.color * light.intensity * diffuseStrength;
    }
    
    vec3 specular = vec3(0.0);
    if (enableSpecular) {
        vec3 reflectDir = reflect(-lightDir, norm);
        float spec = pow(max(dot(viewDir, reflectDir), 0.0), material.shininess);
        
        float specularComponent = material.specularIntensity;
        if (material.hasSpecularMap && material.useTextures) {
            specularComponent *= texture(material.specularMap, texCoord).r;
        }
        
        specular = spec * specularComponent * light.color * light.intensity * specularStrength;
    }
    
    // Apply attenuation to diffuse and specular (not ambient)
    diffuse *= attenuation;
    specular *= attenuation;
    
    vec3 result = (ambient + diffuse + specular) * baseColor;
    
    // Get alpha
    float alpha = material.useTextures ? texture(material.diffuseMap, texCoord).a * vertexColor.a : material.diffuseColor.a * vertexColor.a;
    
    FragColor = vec4(result, alpha);
}");
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
        /// Load textures (Module 5/6 foundation - UNCHANGED)
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

            Console.WriteLine("✓ Textures loaded");
        }

        private Texture LoadTextureWithPriority(string[] primaryNames, string[] fallbackNames, string textureType, string[] directories, bool allowNull = false)
        {
            // Same implementation as before - unchanged
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
        /// Create Module 5's original objects (UNCHANGED)
        /// </summary>
        private void CreateMeshes()
        {
            _cube1 = Mesh.CreateCube(1.0f);
            _cube1.Position = new Vector3(-4.0f, 0.5f, 0.0f);

            _cube2 = Mesh.CreateCube(1.0f);
            _cube2.Position = new Vector3(-4.0f, 0.5f, -3.0f);

            _grid = Mesh.CreateGrid(12.0f, 12.0f, 12);
            _grid.Position = new Vector3(0.0f, -1.0f, 0.0f);

            Console.WriteLine("✓ Module 5 objects created");
        }

        /// <summary>
        /// Load Module 6 models (UNCHANGED)
        /// </summary>
        private void LoadModels()
        {
            Console.WriteLine("\n=== MODULE 6: Loading Models for Lighting ===");

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
                    Console.WriteLine($"✓ Car model loaded for lighting demonstration");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"✗ Error loading car model: {ex.Message}");
                }
            }

            // Create test objects with different materials for lighting comparison
            var testCube1 = Model.CreateCube(1.0f);
            testCube1.Name = "Test Cube 1";
            testCube1.Position = new Vector3(3.0f, 1.0f, 0.0f);
            _models.Add(testCube1);

            var testCube2 = Model.CreateCube(1.0f);
            testCube2.Name = "Test Cube 2";
            testCube2.Position = new Vector3(3.0f, 1.0f, 3.0f);
            _models.Add(testCube2);

            Console.WriteLine($"✓ {_models.Count} models loaded for lighting testing");
        }

        /// <summary>
        /// Setup materials using the material manager (UNCHANGED)
        /// </summary>
        private void SetupMaterials()
        {
            // Create materials with enhanced lighting properties
            var containerMaterial = new Material(_containerTexture, _containerSpecularTexture);
            var brickMaterial = new Material(_brickTexture);
            var gridMaterial = Material.CreateColored(new Vector4(0.5f, 0.5f, 0.5f, 1.0f));

            // Apply materials to meshes
            _cube1.SetMaterial(containerMaterial);
            _cube2.SetMaterial(brickMaterial);
            _grid.SetMaterial(gridMaterial);

            // Add materials to the material manager for educational control
            _materialManager.AddMaterial(containerMaterial);
            _materialManager.AddMaterial(brickMaterial);
            _materialManager.AddMaterial(gridMaterial);

            // Add model materials to manager
            foreach (var model in _models)
            {
                if (model.Mesh?.Material != null)
                {
                    _materialManager.AddMaterial(model.Mesh.Material);
                }
            }

            // Apply default material preset
            _materialManager.ApplyPreset("Default");

            Console.WriteLine("✓ Materials setup with lighting manager");
        }

        private void CreateCamera()
        {
            _camera = new Camera3D(new Vector3(3, 3, 8), _window.Size.X, _window.Size.Y);
            Console.WriteLine("✓ Camera created");
        }

        /// <summary>
        /// MODULE 8 NEW: Create demo scene using scene manager
        /// This demonstrates the new scene management capabilities
        /// </summary>
        private void CreateDemoScene()
        {
            Console.WriteLine("\n=== MODULE 8: Creating Demo Scene ===");

            // Find our car model if we have one
            Model carModel = _models.FirstOrDefault(m => m.Name == "Car");

            // Let the scene manager create an impressive demo scene
            _sceneManager.CreateDemoScene(carModel);

            Console.WriteLine("✓ Demo scene created with SceneManager!");
            Console.WriteLine("  - Multiple objects with animations");
            Console.WriteLine("  - Distance culling for performance");
            Console.WriteLine("  - Performance monitoring available");
        }

        private void OnKeyDown(KeyboardKeyEventArgs e)
        {
            // MODULE 8 NEW: Scene management controls
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
                    _sceneManager.AdjustRenderDistance(5.0f);
                    break;

                case Keys.U:
                    _sceneManager.AdjustRenderDistance(-5.0f);
                    break;

                case Keys.H:
                    PrintControls();
                    break;
            }

            // Handle basic model controls (UNCHANGED from Module 7)
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

                // Material controls using material manager (UNCHANGED)
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

        private void OnMouseMove(MouseMoveEventArgs e)
        {
            // UNCHANGED from Module 7
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
            // UNCHANGED from Module 7
            _camera.AdjustZoom(e.OffsetY);
        }

        private void OnResize(ResizeEventArgs e)
        {
            // UNCHANGED from Module 7
            GL.Viewport(0, 0, e.Width, e.Height);
            _camera.Resize(e.Width, e.Height);
        }

        private void OnUpdateFrame(FrameEventArgs e)
        {
            _time += (float)e.Time;

            // Input handling (UNCHANGED from Module 7)
            if (_window.KeyboardState.IsKeyDown(Keys.Escape))
            {
                _window.Close();
            }

            // Toggle mouse capture (UNCHANGED)
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

            // MODULE 8 NEW: Update scene manager
            // This handles all scene objects automatically
            _sceneManager.Update((float)e.Time);

            // Update lighting system (UNCHANGED from Module 7)
            _lightingManager.Update((float)e.Time);
            _lightingController.Update((float)e.Time);

            // Handle lighting input (UNCHANGED)
            _lightingController.HandleInput(_window.KeyboardState);
        }

        private void HandleCameraMovement(float deltaTime)
        {
            // UNCHANGED from Module 7
            float speed = 5.0f * deltaTime;
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
            // UNCHANGED from Module 7
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
                        _models[i].Rotation += new Vector3(0, 20 * deltaTime, 0);
                    }
                    else
                    {
                        // Other models rotate differently
                        _models[i].Rotation += new Vector3(5 * deltaTime, 10 * deltaTime, 0);
                    }
                }
            }
        }

        private void OnRenderFrame(FrameEventArgs e)
        {
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            // Get camera matrices (UNCHANGED)
            Matrix4 viewMatrix = _camera.GetViewMatrix();
            Matrix4 projectionMatrix = _camera.GetProjectionMatrix();

            // Set up lighting shader (UNCHANGED)
            _lightingShader.Use();
            _lightingShader.SetMatrix4("uView", viewMatrix);
            _lightingShader.SetMatrix4("uProjection", projectionMatrix);

            // Apply lighting using the lighting manager (UNCHANGED)
            _lightingManager.ApplyToShader(_lightingShader, _camera.Position);

            // Render all original objects with lighting (UNCHANGED from Module 7)
            _grid?.Render(_lightingShader);
            _cube1?.Render(_lightingShader);
            _cube2?.Render(_lightingShader);

            foreach (var model in _models)
            {
                model?.Render(_lightingShader);
            }

            // MODULE 8 NEW: Render scene-managed objects
            // This is where we see the benefit of scene management!
            _sceneManager.Render(_lightingShader, _camera.Position);

            // Render light visualization (UNCHANGED)
            _lightingManager.RenderLightVisualization(_lightingShader);

            // MODULE 8 NEW: Display performance info periodically
            if (_showPerformanceInfo && (int)_time % 3 == 0 && (int)((_time - (float)e.Time)) % 3 != 0)
            {
                Console.WriteLine($"PERFORMANCE: {_sceneManager.GetPerformanceInfo()}");
            }

            _window.SwapBuffers();
        }

        /// <summary>
        /// Print controls (updated for Module 8)
        /// </summary>
        private void PrintControls()
        {
            Console.WriteLine("\n" + new string('=', 70));
            Console.WriteLine("MODULE 7: Lighting + MODULE 8: Scene Management");
            Console.WriteLine(new string('=', 70));

            // All Module 7 controls still work!
            _lightingController.PrintControls();

            // MODULE 8 NEW controls
            Console.WriteLine("\n=== MODULE 8 NEW: Scene Management Controls ===");
            Console.WriteLine("  P - Toggle performance info display");
            Console.WriteLine("  J - Toggle distance culling on/off");
            Console.WriteLine("  K - Increase render distance");
            Console.WriteLine("  U - Decrease render distance");
            Console.WriteLine("  H - Show this help");

            Console.WriteLine("\n=== What's New in Module 8 ===");
            Console.WriteLine("✓ SceneManager handles multiple objects efficiently");
            Console.WriteLine("✓ Distance culling improves performance");
            Console.WriteLine("✓ Automatic object animations and organization");
            Console.WriteLine("✓ Performance monitoring and statistics");
            Console.WriteLine("✓ All Module 7 lighting features still work!");

            Console.WriteLine(new string('=', 70));

            // Show current scene status
            Console.WriteLine($"\nScene Status: {_sceneManager.GetPerformanceInfo()}");
        }

        public void Run()
        {
            _window.Run();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                // MODULE 8 NEW: Dispose scene manager
                _sceneManager?.Dispose();

                // Dispose lighting system (UNCHANGED)
                _lightingManager?.Dispose();
                _materialManager?.Dispose();

                // Dispose models (UNCHANGED)
                foreach (var model in _models)
                {
                    model?.Dispose();
                }
                _models.Clear();

                // Dispose meshes (UNCHANGED)
                _cube1?.Dispose();
                _cube2?.Dispose();
                _grid?.Dispose();

                // Dispose shaders (UNCHANGED)
                _lightingShader?.Dispose();

                // Dispose textures (UNCHANGED)
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