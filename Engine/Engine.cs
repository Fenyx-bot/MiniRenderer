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
    /// Main engine class responsible for managing the render loop and loading 3D models
    /// </summary>
    public class Engine : IDisposable
    {
        private readonly GameWindow _window;

        // Camera
        private Camera3D _camera;

        // Shaders
        private Shader _modelShader;

        // Models
        private List<Model> _models = new List<Model>();

        // Default primitives for comparison
        private Model _primitiveGrid;

        // Textures
        private Texture _defaultTexture;
        private Texture _containerTexture;
        private Texture _brickTexture;

        // Materials
        private Material _defaultMaterial;
        private Material _containerMaterial;
        private Material _brickMaterial;

        // Mouse state
        private Vector2 _lastMousePosition;
        private bool _firstMouseMove = true;
        private bool _mouseCaptured = false;

        // Animation and controls
        private float _time = 0.0f;
        private bool _autoRotate = true;
        private bool _showWireframe = false;

        // Light properties
        private Vector3 _lightPosition = new Vector3(2.0f, 2.0f, 2.0f);
        private Vector3 _lightColor = new Vector3(1.0f, 1.0f, 1.0f);
        private float _lightIntensity = 1.2f;
        private bool _lightRotate = true;

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

            // Enable alpha blending
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            // Set a background color
            GL.ClearColor(0.1f, 0.1f, 0.15f, 1.0f);

            // Create directories
            Directory.CreateDirectory("Shaders");
            Directory.CreateDirectory("Assets/Models");
            Directory.CreateDirectory("Assets/Textures");

            // Initialize components
            CreateShaders();
            LoadTextures();
            CreateMaterials();
            LoadModels();
            CreatePrimitives();
            CreateCamera();

            // Print controls
            PrintControls();
        }

        /// <summary>
        /// Create and initialize shaders
        /// </summary>
        private void CreateShaders()
        {
            string vertexShaderPath = "Shaders/model.vert";
            string fragmentShaderPath = "Shaders/model.frag";

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

void main()
{
    // Apply the full MVP transformation
    gl_Position = uProjection * uView * uModel * vec4(aPosition, 1.0);
    
    // Calculate world space fragment position (for lighting)
    fragPos = vec3(uModel * vec4(aPosition, 1.0));
    
    // Transform the normal to world space
    normal = mat3(transpose(inverse(uModel))) * aNormal;
    
    // Pass texture coordinates and color
    texCoord = aTexCoord;
    vertexColor = aColor;
}");
            }

            if (!File.Exists(fragmentShaderPath))
            {
                File.WriteAllText(fragmentShaderPath, @"#version 330 core
// Input from vertex shader
in vec2 texCoord;
in vec3 normal;
in vec3 fragPos;
in vec4 vertexColor;

// Output color
out vec4 FragColor;

// Material structure
struct Material {
    bool useTextures;
    vec4 diffuseColor;
    float specularIntensity;
    float shininess;
    float ambientStrength;
    float alpha;
    bool hasSpecularMap;
    sampler2D diffuseMap;
    sampler2D specularMap;
};

// Light structure
struct Light {
    vec3 position;
    vec3 color;
    float intensity;
    float constant;
    float linear;
    float quadratic;
};

// Uniforms
uniform Material material;
uniform Light light;
uniform vec3 viewPos;

void main()
{
    // Normalize the normal
    vec3 norm = normalize(normal);
    
    // Calculate lighting
    vec3 lightDir = normalize(light.position - fragPos);
    float distance = length(light.position - fragPos);
    float attenuation = 1.0 / (light.constant + light.linear * distance + light.quadratic * (distance * distance));
    
    // Diffuse lighting
    float diff = max(dot(norm, lightDir), 0.0);
    
    // Specular lighting
    vec3 viewDir = normalize(viewPos - fragPos);
    vec3 reflectDir = reflect(-lightDir, norm);
    float spec = pow(max(dot(viewDir, reflectDir), 0.0), material.shininess);
    
    // Specular multiplier
    float specMultiplier = material.specularIntensity;
    if (material.hasSpecularMap && material.useTextures) {
        specMultiplier *= texture(material.specularMap, texCoord).r;
    }
    
    // Combine lighting components
    vec3 ambient = material.ambientStrength * light.color;
    vec3 diffuse = diff * light.color * light.intensity;
    vec3 specular = spec * specMultiplier * light.color * light.intensity;
    
    // Apply attenuation
    ambient *= attenuation;
    diffuse *= attenuation;
    specular *= attenuation;
    
    // Base color
    vec4 baseColor;
    if (material.useTextures) {
        baseColor = texture(material.diffuseMap, texCoord) * vertexColor;
    } else {
        baseColor = material.diffuseColor * vertexColor;
    }
    
    // Final color
    vec3 result = (ambient + diffuse + specular) * baseColor.rgb;
    FragColor = vec4(result, baseColor.a * material.alpha);
}");
            }

            try
            {
                _modelShader = Shader.FromFiles(vertexShaderPath, fragmentShaderPath);
                Console.WriteLine("Model shader loaded successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading shader: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Load textures
        /// </summary>
        private void LoadTextures()
        {
            _defaultTexture = Texture.CreateWhiteTexture();

            // Try to load textures from different possible locations
            string[] texturePaths = {
                "Assets/Textures/container.jpg",
                "Textures/container.jpg",
                "container.jpg"
            };

            string[] brickPaths = {
                "Assets/Textures/brick.jpg",
                "Textures/brick.jpg",
                "brick.jpg"
            };

            _containerTexture = LoadTextureFromPaths(texturePaths, "container");
            _brickTexture = LoadTextureFromPaths(brickPaths, "brick");
        }

        /// <summary>
        /// Load a texture from multiple possible paths
        /// </summary>
        private Texture LoadTextureFromPaths(string[] paths, string name)
        {
            foreach (string path in paths)
            {
                if (File.Exists(path))
                {
                    try
                    {
                        Console.WriteLine($"Loaded {name} texture from: {path}");
                        return new Texture(path);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error loading texture {path}: {ex.Message}");
                    }
                }
            }

            Console.WriteLine($"{name} texture not found, using default");
            return _defaultTexture;
        }

        /// <summary>
        /// Create materials
        /// </summary>
        private void CreateMaterials()
        {
            // Default material for models without specific materials
            _defaultMaterial = new Material(_defaultTexture);
            _defaultMaterial.DiffuseColor = new Vector4(0.8f, 0.8f, 0.9f, 1.0f);
            _defaultMaterial.Shininess = 32.0f;
            _defaultMaterial.SpecularIntensity = 0.5f;
            _defaultMaterial.AmbientStrength = 0.1f;

            // Container material
            _containerMaterial = new Material(_containerTexture);
            _containerMaterial.Shininess = 64.0f;
            _containerMaterial.SpecularIntensity = 0.8f;
            _containerMaterial.AmbientStrength = 0.1f;

            // Brick material
            _brickMaterial = new Material(_brickTexture);
            _brickMaterial.Shininess = 16.0f;
            _brickMaterial.SpecularIntensity = 0.3f;
            _brickMaterial.AmbientStrength = 0.2f;
        }

        /// <summary>
        /// Load the car model specifically
        /// </summary>
        private void LoadModels()
        {
            Console.WriteLine("Loading car model...");

            // Look specifically for the car model
            string carObjPath = "Assets/Models/Car/car.obj";

            if (File.Exists(carObjPath))
            {
                try
                {
                    var carModel = new Model(carObjPath);
                    carModel.Name = "Car";

                    // DON'T center at origin yet - let's see where it naturally is
                    Console.WriteLine($"Original car position: {carModel.Position}");
                    Console.WriteLine($"Original bounding box: Min={carModel.BoundingBoxMin}, Max={carModel.BoundingBoxMax}");
                    Console.WriteLine($"Original bounding box center: {carModel.BoundingBoxCenter}");

                    // Scale it up significantly and position it higher
                    carModel.ScaleToFit(5.0f); // Make it bigger
                    carModel.Position = new Vector3(0.0f, 2.0f, 0.0f); // Move it up

                    _models.Add(carModel);

                    Console.WriteLine($"✓ Car model loaded successfully!");
                    Console.WriteLine($"  Model: {carModel.Name}");
                    Console.WriteLine($"  Final Position: {carModel.Position}");
                    Console.WriteLine($"  Final Scale: {carModel.Scale}");
                    Console.WriteLine($"  Materials: {(carModel.Mesh.Material?.UseTextures == true ? "With textures" : "Default material")}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"✗ Error loading car model: {ex.Message}");

                    // Create a fallback cube if car loading fails
                    CreateFallbackModel();
                }
            }
            else
            {
                Console.WriteLine($"✗ Car model not found at: {carObjPath}");
                Console.WriteLine("Please ensure you have:");
                Console.WriteLine("  - Assets/Models/Car/car.obj");
                Console.WriteLine("  - Assets/Models/Car/car.png (or similar texture file)");

                // Create a fallback model
                CreateFallbackModel();
            }

            Console.WriteLine($"Total models loaded: {_models.Count}");
        }

        /// <summary>
        /// Create a fallback model if the car model fails to load
        /// </summary>
        private void CreateFallbackModel()
        {
            Console.WriteLine("Creating fallback cube model...");

            var fallbackCube = Model.CreateCube(2.0f, _containerMaterial);
            fallbackCube.Name = "Fallback Cube";
            fallbackCube.Position = new Vector3(0.0f, 1.0f, 0.0f);
            _models.Add(fallbackCube);

            Console.WriteLine("✓ Fallback model created");
        }

        /// <summary>
        /// Create primitive objects for comparison
        /// </summary>
        private void CreatePrimitives()
        {
            var gridMesh = Mesh.CreateGrid(20.0f, 20.0f, 20);
            gridMesh.SetMaterial(Material.CreateColored(new Vector4(0.3f, 0.3f, 0.3f, 1.0f)));
            _primitiveGrid = new Model(gridMesh, "Grid");
            _primitiveGrid.Position = new Vector3(0.0f, -2.0f, 0.0f);
        }

        /// <summary>
        /// Create 3D camera
        /// </summary>
        private void CreateCamera()
        {
            _camera = new Camera3D(new Vector3(0, 2, 6), _window.Size.X, _window.Size.Y);
            Console.WriteLine($"Camera created at position: {_camera.Position}");
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
            Console.WriteLine("  R - Reset camera position");
            Console.WriteLine("  L - Toggle light rotation");
            Console.WriteLine("");
            Console.WriteLine("Model Controls:");
            Console.WriteLine("  T - Toggle auto-rotation");
            Console.WriteLine("  F - Toggle wireframe mode");
            Console.WriteLine("  Up/Down Arrow - Scale model");
            Console.WriteLine("  C - Center model at origin");
            Console.WriteLine("  1 - Add test cube");
            Console.WriteLine("  2 - Reset car position/scale");
            Console.WriteLine("");
            Console.WriteLine("  Escape - Exit");
            Console.WriteLine("");
            if (_models.Count > 0)
            {
                Console.WriteLine($"Loaded model: {_models[0].Name}");
                var material = _models[0].Mesh?.Material;
                if (material != null)
                {
                    Console.WriteLine($"Uses textures: {material.UseTextures}");
                    if (material.UseTextures)
                    {
                        Console.WriteLine($"Diffuse texture: {material.DiffuseMap?.Path ?? "None"}");
                        Console.WriteLine($"Specular texture: {material.SpecularMap?.Path ?? "None"}");
                    }
                }
            }
        }

        /// <summary>
        /// Called when a key is pressed
        /// </summary>
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

                case Keys.Up:
                    if (_models.Count > 0)
                    {
                        _models[0].Scale *= 1.1f;
                        Console.WriteLine($"Scaled model up: {_models[0].Scale.X:F2}");
                    }
                    break;

                case Keys.Down:
                    if (_models.Count > 0)
                    {
                        _models[0].Scale *= 0.9f;
                        Console.WriteLine($"Scaled model down: {_models[0].Scale.X:F2}");
                    }
                    break;

                case Keys.C:
                    if (_models.Count > 0)
                    {
                        _models[0].CenterAtOrigin();
                        Console.WriteLine("Centered model at origin");
                    }
                    break;

                case Keys.D1:
                    // Test: Create a simple test cube at a known position
                    var testCube = Model.CreateCube(1.0f, _containerMaterial);
                    testCube.Name = "Test Cube";
                    testCube.Position = new Vector3(2.0f, 1.0f, 0.0f);
                    _models.Add(testCube);
                    Console.WriteLine("Added test cube at (2, 1, 0)");
                    break;

                case Keys.D2:
                    // Reset car position to origin and make it much bigger
                    if (_models.Count > 0)
                    {
                        _models[0].Position = Vector3.Zero;
                        _models[0].Scale = new Vector3(2.0f); // Much bigger
                        Console.WriteLine($"Reset car - Position: {_models[0].Position}, Scale: {_models[0].Scale}");
                    }
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

        /// <summary>
        /// Called when the mouse wheel is scrolled
        /// </summary>
        private void OnMouseWheel(MouseWheelEventArgs e)
        {
            _camera.AdjustZoom(e.OffsetY);
        }

        /// <summary>
        /// Called when the window is resized
        /// </summary>
        private void OnResize(ResizeEventArgs e)
        {
            GL.Viewport(0, 0, e.Width, e.Height);
            _camera.Resize(e.Width, e.Height);
        }

        /// <summary>
        /// Called to update the game logic
        /// </summary>
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
            UpdateModels((float)e.Time);
            UpdateLight((float)e.Time);
        }

        /// <summary>
        /// Handle camera movement
        /// </summary>
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

        /// <summary>
        /// Update model animations
        /// </summary>
        private void UpdateModels(float deltaTime)
        {
            if (_autoRotate && _models.Count > 0)
            {
                // Rotate the car model slowly
                _models[0].Rotation += new Vector3(0, 30 * deltaTime, 0);
            }
        }

        /// <summary>
        /// Update light position
        /// </summary>
        private void UpdateLight(float deltaTime)
        {
            if (_lightRotate)
            {
                float radius = 4.0f;
                float angle = _time * 0.5f;
                _lightPosition.X = (float)Math.Sin(angle) * radius;
                _lightPosition.Z = (float)Math.Cos(angle) * radius;
            }
        }

        /// <summary>
        /// Called to render a frame
        /// </summary>
        private void OnRenderFrame(FrameEventArgs e)
        {
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            // Get camera matrices
            Matrix4 viewMatrix = _camera.GetViewMatrix();
            Matrix4 projectionMatrix = _camera.GetProjectionMatrix();

            // Set common shader uniforms
            _modelShader.Use();
            _modelShader.SetMatrix4("uView", viewMatrix);
            _modelShader.SetMatrix4("uProjection", projectionMatrix);

            // Set light properties
            _modelShader.SetVector3("light.position", _lightPosition);
            _modelShader.SetVector3("light.color", _lightColor);
            _modelShader.SetFloat("light.intensity", _lightIntensity);
            _modelShader.SetFloat("light.constant", 1.0f);
            _modelShader.SetFloat("light.linear", 0.09f);
            _modelShader.SetFloat("light.quadratic", 0.032f);

            // Set camera position for specular calculations
            _modelShader.SetVector3("viewPos", _camera.Position);

            // Render grid
            _primitiveGrid?.Render(_modelShader);

            // Render all models with debug info
            foreach (var model in _models)
            {
                if (model != null)
                {
                    // Debug: Print model matrix occasionally
                    if (_time > 1.0f && (int)_time % 5 == 0 && _time - (int)_time < 0.1f)
                    {
                        Matrix4 modelMatrix = model.GetModelMatrix();
                        Console.WriteLine($"Rendering {model.Name} - Position: {model.Position}, Scale: {model.Scale}");
                        Console.WriteLine($"Model matrix: {modelMatrix.M11:F2},{modelMatrix.M12:F2},{modelMatrix.M13:F2},{modelMatrix.M14:F2}");
                    }

                    model.Render(_modelShader);
                }
            }

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
                // Dispose models
                foreach (var model in _models)
                {
                    model?.Dispose();
                }
                _models.Clear();

                _primitiveGrid?.Dispose();

                // Dispose shaders
                _modelShader?.Dispose();

                // Dispose textures
                _defaultTexture?.Dispose();
                _containerTexture?.Dispose();
                _brickTexture?.Dispose();

                // Materials are disposed by models

                _disposed = true;
                GC.SuppressFinalize(this);
            }
        }
    }
}