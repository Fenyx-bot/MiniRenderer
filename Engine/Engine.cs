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
    /// Main engine class responsible for managing the render loop and OpenGL resources
    /// </summary>
    public class Engine : IDisposable
    {
        private readonly GameWindow _window;

        // Camera
        private Camera3D _camera;

        // Shaders
        private Shader _materialShader;

        // Meshes
        private Mesh _cube1;
        private Mesh _cube2;
        private Mesh _grid;

        // Textures
        private Texture _containerTexture;
        private Texture _containerSpecularTexture;
        private Texture _brickTexture;
        private Texture _brickSpecularTexture;
        private Texture _defaultTexture;

        // Materials
        private Material _containerMaterial;
        private Material _brickMaterial;
        private Material _gridMaterial;

        // Mouse state
        private Vector2 _lastMousePosition;
        private bool _firstMouseMove = true;
        private bool _mouseCaptured = false;

        // Animation
        private float _time = 0.0f;

        // Light properties
        private Vector3 _lightPosition = new Vector3(1.2f, 1.0f, 2.0f);
        private Vector3 _lightColor = new Vector3(1.0f, 1.0f, 1.0f);
        private float _lightIntensity = 1.5f; // Increased intensity
        private bool _lightRotate = true;

        // Specular testing
        private bool _specularEnabled = true;

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

            // Set a background color (dark blue)
            GL.ClearColor(0.05f, 0.05f, 0.1f, 1.0f);

            // Create directory for shaders if it doesn't exist
            Directory.CreateDirectory("Shaders");

            // Create textures directory
            Directory.CreateDirectory("Assets/Textures");

            // Create shaders
            CreateShaders();

            // Load textures
            LoadTextures();

            // Create materials
            CreateMaterials();

            // Create meshes
            CreateMeshes();

            // Create 3D camera
            CreateCamera();

            // Print controls
            PrintControls();
        }

        /// <summary>
        /// Create 3D camera with good positioning for viewing specular effects
        /// </summary>
        private void CreateCamera()
        {
            // Position camera for better specular viewing (at an angle)
            _camera = new Camera3D(new Vector3(3, 2, 4), _window.Size.X, _window.Size.Y);
        }

        /// <summary>
        /// Create and initialize shaders
        /// </summary>
        private void CreateShaders()
        {
            string vertexShaderPath = "Shaders/material.vert";
            string fragmentShaderPath = "Shaders/material.frag";

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
    // Apply the full MVP transformation
    gl_Position = uProjection * uView * uModel * vec4(aPosition, 1.0);
    
    // Calculate world space fragment position (for lighting)
    fragPos = vec3(uModel * vec4(aPosition, 1.0));
    
    // Transform the normal to world space
    normal = mat3(transpose(inverse(uModel))) * aNormal;
    
    // Apply tiling to texture coordinates
    texCoord = (aTexCoord * uTextureScale) + uTextureOffset;
    
    // Pass the vertex color to the fragment shader
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
    vec3 direction;
    vec3 color;
    float intensity;
    float constant;
    float linear;
    float quadratic;
    bool isDirectional;
};

// Uniforms
uniform Material material;
uniform Light light;
uniform vec3 viewPos; // Camera position for specular calculation
uniform bool uSpecularEnabled; // Global specular toggle

void main()
{
    // Normalize the normal
    vec3 norm = normalize(normal);
    
    // =====================
    // Lighting calculations
    // =====================
    
    vec3 lightDir;
    float attenuation = 1.0;
    
    if (light.isDirectional) {
        // For directional lights, use the negative direction
        lightDir = normalize(-light.direction);
    } else {
        // For point lights, calculate direction and attenuation
        lightDir = normalize(light.position - fragPos);
        
        // Calculate attenuation based on distance
        float distance = length(light.position - fragPos);
        attenuation = 1.0 / (light.constant + light.linear * distance + light.quadratic * (distance * distance));
    }
    
    // Calculate diffuse factor
    float diff = max(dot(norm, lightDir), 0.0);
    
    // Calculate specular factor (only if enabled)
    vec3 viewDir = normalize(viewPos - fragPos);
    vec3 reflectDir = reflect(-lightDir, norm);
    float spec = 0.0;
    
    if (uSpecularEnabled && diff > 0.0) {
        // Use Blinn-Phong for better specular highlights
        vec3 halfwayDir = normalize(lightDir + viewDir);
        spec = pow(max(dot(norm, halfwayDir), 0.0), material.shininess);
    }
    
    // Determine specular multiplier (from texture or uniform)
    float specMultiplier = material.specularIntensity;
    if (material.hasSpecularMap && material.useTextures && uSpecularEnabled) {
        vec4 specularTexture = texture(material.specularMap, texCoord);
        specMultiplier *= specularTexture.r; // Use red channel for specular intensity
    }
    
    // =====================
    // Final color calculation
    // =====================
    
    // Calculate ambient component
    vec3 ambient = material.ambientStrength * light.color;
    
    // Calculate diffuse component
    vec3 diffuse = diff * light.color * light.intensity;
    
    // Calculate specular component (only if enabled)
    vec3 specular = vec3(0.0);
    if (uSpecularEnabled) {
        specular = spec * specMultiplier * light.color * light.intensity;
    }
    
    // Apply attenuation to all components
    ambient *= attenuation;
    diffuse *= attenuation;
    specular *= attenuation;
    
    // Base color (from texture or material color)
    vec4 baseColor;
    if (material.useTextures) {
        baseColor = texture(material.diffuseMap, texCoord) * vertexColor;
    } else {
        baseColor = material.diffuseColor * vertexColor;
    }
    
    // Final color with lighting
    vec3 result = (ambient + diffuse + specular) * baseColor.rgb;
    
    // Set final color with alpha
    FragColor = vec4(result, baseColor.a * material.alpha);
}");
            }

            // Load the shader
            try
            {
                _materialShader = Shader.FromFiles(vertexShaderPath, fragmentShaderPath);
                Console.WriteLine("Material shader loaded successfully");
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
            // Create a default white texture
            _defaultTexture = Texture.CreateWhiteTexture();

            // Print available textures for debugging
            Console.WriteLine("Checking for available textures:");
            string[] possibleDirectories = { "Assets/Textures/", "Textures/", "" };

            foreach (string dir in possibleDirectories)
            {
                if (Directory.Exists(dir))
                {
                    Console.WriteLine($"  Directory '{dir}' exists, contains:");
                    string[] files = Directory.GetFiles(dir, "*.*")
                        .Where(file => file.ToLower().EndsWith(".jpg") ||
                                      file.ToLower().EndsWith(".png") ||
                                      file.ToLower().EndsWith(".bmp"))
                        .ToArray();

                    foreach (string file in files)
                    {
                        Console.WriteLine($"    - {file}");
                    }
                }
            }

            try
            {
                // Load CONTAINER textures (prioritize container.jpg over crate.png)
                Console.WriteLine("\nLooking for CONTAINER textures:");
                _containerTexture = LoadTextureWithPriority(new string[] {
                    "container.jpg", "container.png"
                }, new string[] {
                    "crate.png", "crate.jpg"
                }, "container/crate diffuse");

                // Load CONTAINER SPECULAR map
                Console.WriteLine("Looking for CONTAINER SPECULAR textures:");
                _containerSpecularTexture = LoadTextureWithPriority(new string[] {
                    "container_specular.jpg", "container_specular.png"
                }, new string[] {
                    "container_spec.jpg", "container_spec.png"
                }, "container specular", allowNull: true);

                // Load BRICK textures
                Console.WriteLine("Looking for BRICK textures:");
                _brickTexture = LoadTextureWithPriority(new string[] {
                    "brick.jpg", "brick.png"
                }, new string[] {
                    "awesome_face.png", "face.png"
                }, "brick diffuse");

                // Load BRICK SPECULAR map  
                Console.WriteLine("Looking for BRICK SPECULAR textures:");
                var brickSpecularTexture = LoadTextureWithPriority(new string[] {
                    "brick_specular.jpg", "brick_specular.png", "brick_spec.jpg"
                }, new string[] {
                }, "brick specular", allowNull: true);

                // Store brick specular for material creation
                if (brickSpecularTexture != null && brickSpecularTexture != _defaultTexture)
                {
                    _brickSpecularTexture = brickSpecularTexture;
                }

                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading textures: {ex.Message}");
                _containerTexture = _defaultTexture;
                _brickTexture = _defaultTexture;
                _containerSpecularTexture = null;
            }
        }

        /// <summary>
        /// Load a texture with priority (tries primary names first, then fallback names)
        /// </summary>
        private Texture LoadTextureWithPriority(string[] primaryNames, string[] fallbackNames, string textureType, bool allowNull = false)
        {
            string[] directories = { "Assets/Textures/", "Textures/", "" };

            // Try primary names first
            foreach (string name in primaryNames)
            {
                foreach (string dir in directories)
                {
                    string path = Path.Combine(dir, name);
                    if (File.Exists(path))
                    {
                        Console.WriteLine($"  ✓ Found {textureType}: {path} (PRIMARY)");
                        return new Texture(path);
                    }
                }
            }

            // Try fallback names if primary not found
            foreach (string name in fallbackNames)
            {
                foreach (string dir in directories)
                {
                    string path = Path.Combine(dir, name);
                    if (File.Exists(path))
                    {
                        Console.WriteLine($"  ✓ Found {textureType}: {path} (FALLBACK)");
                        return new Texture(path);
                    }
                }
            }

            // Not found
            if (allowNull)
            {
                Console.WriteLine($"  ✗ {textureType} not found (optional)");
                return null;
            }
            else
            {
                Console.WriteLine($"  ✗ {textureType} not found, using default white");
                return _defaultTexture;
            }
        }

        /// <summary>
        /// Create materials
        /// </summary>
        private void CreateMaterials()
        {
            // Create container material with diffuse and specular maps (more dramatic settings)
            _containerMaterial = new Material(_containerTexture, _containerSpecularTexture);
            _containerMaterial.Shininess = 128.0f; // Higher shininess for tighter highlights
            _containerMaterial.SpecularIntensity = 1.5f; // Much higher specular intensity
            _containerMaterial.AmbientStrength = 0.05f; // Lower ambient to see specular better

            // Create brick material with diffuse and optionally specular (more dramatic settings)
            _brickMaterial = new Material(_brickTexture, _brickSpecularTexture);
            _brickMaterial.Shininess = 32.0f; // Higher shininess
            _brickMaterial.SpecularIntensity = 1.0f; // Higher specular intensity
            _brickMaterial.AmbientStrength = 0.1f; // Lower ambient

            // Create a simple colored material for the grid
            _gridMaterial = Material.CreateColored(new Vector4(0.7f, 0.7f, 0.7f, 1.0f));
            _gridMaterial.Shininess = 16.0f;
            _gridMaterial.SpecularIntensity = 0.2f;
            _gridMaterial.AmbientStrength = 0.3f;

            // Debug output
            Console.WriteLine("Materials created:");
            Console.WriteLine($"  Container material:");
            Console.WriteLine($"    - Diffuse texture: {(_containerTexture == _defaultTexture ? "DEFAULT WHITE" : _containerTexture.Path)}");
            Console.WriteLine($"    - Specular texture: {(_containerSpecularTexture?.Path ?? "NONE")}");
            Console.WriteLine($"    - Has specular map: {_containerSpecularTexture != null}");
            Console.WriteLine($"    - Uses textures: {_containerMaterial.UseTextures}");
            Console.WriteLine($"    - Shininess: {_containerMaterial.Shininess}");
            Console.WriteLine($"    - Specular intensity: {_containerMaterial.SpecularIntensity}");
            Console.WriteLine($"  Brick material:");
            Console.WriteLine($"    - Diffuse texture: {(_brickTexture == _defaultTexture ? "DEFAULT WHITE" : _brickTexture.Path)}");
            Console.WriteLine($"    - Specular texture: {(_brickSpecularTexture?.Path ?? "NONE")}");
            Console.WriteLine($"    - Has specular map: {_brickSpecularTexture != null}");
            Console.WriteLine($"    - Uses textures: {_brickMaterial.UseTextures}");
            Console.WriteLine($"    - Shininess: {_brickMaterial.Shininess}");
            Console.WriteLine($"    - Specular intensity: {_brickMaterial.SpecularIntensity}");
            Console.WriteLine();
        }

        /// <summary>
        /// Create mesh objects
        /// </summary>
        private void CreateMeshes()
        {
            // Create a cube with the container material
            _cube1 = Mesh.CreateCube(1.0f);
            _cube1.SetMaterial(_containerMaterial);
            _cube1.Position = new Vector3(-1.5f, 0.5f, 0.0f);
            _cube1.TextureScale = new Vector2(1.0f, 1.0f); // Initialize texture scale

            // Create a cube with the brick material
            _cube2 = Mesh.CreateCube(1.0f);
            _cube2.SetMaterial(_brickMaterial);
            _cube2.Position = new Vector3(1.5f, 0.5f, 0.0f);
            _cube2.TextureScale = new Vector2(1.0f, 1.0f); // Initialize texture scale

            // Create a grid
            _grid = Mesh.CreateGrid(10.0f, 10.0f, 10);
            _grid.SetMaterial(_gridMaterial);

            // Set up texture tiling on the grid
            _grid.TextureScale = new Vector2(3.0f, 3.0f);
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
            Console.WriteLine("Material Controls:");
            Console.WriteLine("  1/2 - Decrease/Increase specular intensity");
            Console.WriteLine("  3/4 - Decrease/Increase shininess");
            Console.WriteLine("  5/6 - Decrease/Increase ambient lighting");
            Console.WriteLine("  7/8 - Decrease/Increase texture tiling (affects all objects)");
            Console.WriteLine("");
            Console.WriteLine("Specular Testing:");
            Console.WriteLine("  G - Toggle specular on/off globally");
            Console.WriteLine("  H - Set EXTREME specular values for testing");
            Console.WriteLine("  N - Reset to normal specular values");
            Console.WriteLine("");
            Console.WriteLine("Object Controls:");
            Console.WriteLine("  Arrow Keys - Move selected cube");
            Console.WriteLine("  Tab - Toggle between cubes (hold to select cube 2)");
            Console.WriteLine("  Escape - Exit");
            Console.WriteLine("");
            Console.WriteLine($"Initial values:");
            Console.WriteLine($"  Container material - Shininess: {_containerMaterial.Shininess:F0}, Specular: {_containerMaterial.SpecularIntensity:F1}");
            Console.WriteLine($"  Brick material - Shininess: {_brickMaterial.Shininess:F0}, Specular: {_brickMaterial.SpecularIntensity:F1}");
            Console.WriteLine($"  Texture tiling: {_cube1.TextureScale.X:F1}");
            Console.WriteLine($"  Light intensity: {_lightIntensity:F1}");
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
                    CreateCamera();
                    Console.WriteLine("Camera reset");
                    break;

                case Keys.L:
                    // Toggle light rotation
                    _lightRotate = !_lightRotate;
                    Console.WriteLine($"Light rotation: {_lightRotate}");
                    break;

                case Keys.G:
                    // Toggle specular globally
                    _specularEnabled = !_specularEnabled;
                    Console.WriteLine($"Specular lighting: {(_specularEnabled ? "ENABLED" : "DISABLED")}");
                    break;

                case Keys.H:
                    // Set very high specular for testing
                    _containerMaterial.SpecularIntensity = 3.0f;
                    _brickMaterial.SpecularIntensity = 3.0f;
                    _containerMaterial.Shininess = 256.0f;
                    _brickMaterial.Shininess = 256.0f;
                    Console.WriteLine("Set EXTREME specular values for testing");
                    break;

                case Keys.N:
                    // Reset to normal specular values
                    _containerMaterial.SpecularIntensity = 1.5f;
                    _brickMaterial.SpecularIntensity = 1.0f;
                    _containerMaterial.Shininess = 128.0f;
                    _brickMaterial.Shininess = 32.0f;
                    Console.WriteLine("Reset to normal specular values");
                    break;

                case Keys.D1:
                    // Decrease specular intensity
                    _containerMaterial.SpecularIntensity = Math.Max(0.0f, _containerMaterial.SpecularIntensity - 0.2f);
                    _brickMaterial.SpecularIntensity = Math.Max(0.0f, _brickMaterial.SpecularIntensity - 0.2f);
                    Console.WriteLine($"Specular intensity: Container={_containerMaterial.SpecularIntensity:F1}, Brick={_brickMaterial.SpecularIntensity:F1}");
                    break;

                case Keys.D2:
                    // Increase specular intensity
                    _containerMaterial.SpecularIntensity = Math.Min(5.0f, _containerMaterial.SpecularIntensity + 0.2f);
                    _brickMaterial.SpecularIntensity = Math.Min(5.0f, _brickMaterial.SpecularIntensity + 0.2f);
                    Console.WriteLine($"Specular intensity: Container={_containerMaterial.SpecularIntensity:F1}, Brick={_brickMaterial.SpecularIntensity:F1}");
                    break;

                case Keys.D3:
                    // Decrease shininess
                    _containerMaterial.Shininess = Math.Max(1.0f, _containerMaterial.Shininess - 16.0f);
                    _brickMaterial.Shininess = Math.Max(1.0f, _brickMaterial.Shininess - 8.0f);
                    Console.WriteLine($"Shininess: Container={_containerMaterial.Shininess:F0}, Brick={_brickMaterial.Shininess:F0}");
                    break;

                case Keys.D4:
                    // Increase shininess
                    _containerMaterial.Shininess = Math.Min(512.0f, _containerMaterial.Shininess + 16.0f);
                    _brickMaterial.Shininess = Math.Min(512.0f, _brickMaterial.Shininess + 8.0f);
                    Console.WriteLine($"Shininess: Container={_containerMaterial.Shininess:F0}, Brick={_brickMaterial.Shininess:F0}");
                    break;

                case Keys.D5:
                    // Decrease ambient lighting
                    _containerMaterial.AmbientStrength = Math.Max(0.0f, _containerMaterial.AmbientStrength - 0.05f);
                    _brickMaterial.AmbientStrength = Math.Max(0.0f, _brickMaterial.AmbientStrength - 0.05f);
                    _gridMaterial.AmbientStrength = Math.Max(0.0f, _gridMaterial.AmbientStrength - 0.05f);
                    Console.WriteLine($"Ambient: Container={_containerMaterial.AmbientStrength:F2}, Brick={_brickMaterial.AmbientStrength:F2}");
                    break;

                case Keys.D6:
                    // Increase ambient lighting
                    _containerMaterial.AmbientStrength = Math.Min(1.0f, _containerMaterial.AmbientStrength + 0.05f);
                    _brickMaterial.AmbientStrength = Math.Min(1.0f, _brickMaterial.AmbientStrength + 0.05f);
                    _gridMaterial.AmbientStrength = Math.Min(1.0f, _gridMaterial.AmbientStrength + 0.05f);
                    Console.WriteLine($"Ambient: Container={_containerMaterial.AmbientStrength:F2}, Brick={_brickMaterial.AmbientStrength:F2}");
                    break;

                case Keys.D7:
                    // Decrease texture tiling
                    _cube1.TextureScale = new Vector2(Math.Max(0.1f, _cube1.TextureScale.X - 0.5f));
                    _cube2.TextureScale = new Vector2(Math.Max(0.1f, _cube2.TextureScale.X - 0.5f));
                    _grid.TextureScale = new Vector2(Math.Max(0.1f, _grid.TextureScale.X - 0.5f));
                    Console.WriteLine($"Texture tiling: {_cube1.TextureScale.X:F1}");
                    break;

                case Keys.D8:
                    // Increase texture tiling
                    _cube1.TextureScale = new Vector2(Math.Min(10.0f, _cube1.TextureScale.X + 0.5f));
                    _cube2.TextureScale = new Vector2(Math.Min(10.0f, _cube2.TextureScale.X + 0.5f));
                    _grid.TextureScale = new Vector2(Math.Min(10.0f, _grid.TextureScale.X + 0.5f));
                    Console.WriteLine($"Texture tiling: {_cube1.TextureScale.X:F1}");
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

            // Update cubes
            UpdateCubes((float)e.Time);

            // Update light position
            UpdateLight((float)e.Time);
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
        /// Update cube positions and rotations
        /// </summary>
        private void UpdateCubes(float deltaTime)
        {
            // Rotate the cubes
            _cube1.Rotation += new Vector3(0, 15 * deltaTime, 0);
            _cube2.Rotation += new Vector3(15 * deltaTime, 0, 0);

            // Handle keyboard movement of cubes
            var keyboard = _window.KeyboardState;
            bool isTabDown = keyboard.IsKeyDown(Keys.Tab);

            // Determine which cube to move
            Mesh activeCube = isTabDown ? _cube2 : _cube1;

            // Movement amount
            float moveAmount = 2.0f * deltaTime;

            // Handle arrow keys
            if (keyboard.IsKeyDown(Keys.Up))
                activeCube.Position += new Vector3(0, moveAmount, 0);
            if (keyboard.IsKeyDown(Keys.Down))
                activeCube.Position -= new Vector3(0, moveAmount, 0);
            if (keyboard.IsKeyDown(Keys.Left))
                activeCube.Position -= new Vector3(moveAmount, 0, 0);
            if (keyboard.IsKeyDown(Keys.Right))
                activeCube.Position += new Vector3(moveAmount, 0, 0);
        }

        /// <summary>
        /// Update the light position
        /// </summary>
        private void UpdateLight(float deltaTime)
        {
            if (_lightRotate)
            {
                // Rotate the light around the scene
                float radius = 3.0f;
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
            // Clear the color and depth buffers
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            // Get camera matrices
            Matrix4 viewMatrix = _camera.GetViewMatrix();
            Matrix4 projectionMatrix = _camera.GetProjectionMatrix();

            // Set common shader uniforms
            _materialShader.Use();
            _materialShader.SetMatrix4("uView", viewMatrix);
            _materialShader.SetMatrix4("uProjection", projectionMatrix);

            // Set light properties (with specular toggle)
            _materialShader.SetVector3("light.position", _lightPosition);
            _materialShader.SetVector3("light.color", _lightColor);
            _materialShader.SetFloat("light.intensity", _lightIntensity);
            _materialShader.SetFloat("light.constant", 1.0f);
            _materialShader.SetFloat("light.linear", 0.045f); // Reduced for less attenuation
            _materialShader.SetFloat("light.quadratic", 0.0075f); // Reduced for less attenuation
            _materialShader.SetBool("light.isDirectional", false);

            // Global specular toggle
            _materialShader.SetBool("uSpecularEnabled", _specularEnabled);

            // Set camera position for specular calculations
            _materialShader.SetVector3("viewPos", _camera.Position);

            // Draw objects
            _grid.Render(_materialShader);
            _cube1.Render(_materialShader);
            _cube2.Render(_materialShader);

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
                _cube1?.Dispose();
                _cube2?.Dispose();
                _grid?.Dispose();

                // Dispose of shaders
                _materialShader?.Dispose();

                // Dispose of textures
                _containerTexture?.Dispose();
                _containerSpecularTexture?.Dispose();
                _brickTexture?.Dispose();
                _brickSpecularTexture?.Dispose();
                _defaultTexture?.Dispose();

                // Note: materials are disposed by meshes if owned

                _disposed = true;
                GC.SuppressFinalize(this);
            }
        }
    }
}