using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Mathematics;
using System;

namespace MiniRenderer.Lighting
{
    /// <summary>
    /// Handles user input for controlling lighting in real-time
    /// Provides an educational interface for learning lighting concepts
    /// </summary>
    public class LightingController
    {
        private LightingManager _lightingManager;

        // Input state tracking
        private bool[] _keyPressed = new bool[512]; // Track key press states to avoid repeats

        // Cycling state
        private int _currentColorIndex = 0;
        private int _currentScenarioIndex = 0;
        private readonly string[] _colorPresets = { "white", "warm", "cool", "red", "green", "blue", "yellow", "orange" };
        private readonly string[] _lightingScenarios = { "default", "sunrise", "noon", "sunset", "night", "studio", "candlelight" };

        // Animation time for color cycling
        private float _animationTime = 0.0f;
        private bool _isColorCycling = false;

        /// <summary>
        /// Create a lighting controller for the given lighting manager
        /// </summary>
        /// <param name="lightingManager">The lighting manager to control</param>
        public LightingController(LightingManager lightingManager)
        {
            _lightingManager = lightingManager ?? throw new ArgumentNullException(nameof(lightingManager));
        }

        /// <summary>
        /// Update the controller (handle animations, etc.)
        /// </summary>
        /// <param name="deltaTime">Time since last update</param>
        public void Update(float deltaTime)
        {
            _animationTime += deltaTime;

            // Handle color cycling animation
            if (_isColorCycling && _lightingManager.PrimaryLight != null)
            {
                float time = _animationTime * 2.0f;
                var newColor = new Vector3(
                    (float)(Math.Sin(time) * 0.5 + 0.5),
                    (float)(Math.Sin(time + Math.PI * 2.0 / 3.0) * 0.5 + 0.5),
                    (float)(Math.Sin(time + Math.PI * 4.0 / 3.0) * 0.5 + 0.5)
                );
                _lightingManager.PrimaryLight.Color = newColor;
            }
        }

        /// <summary>
        /// Handle keyboard input for lighting controls
        /// </summary>
        /// <param name="keyboard">Current keyboard state</param>
        public void HandleInput(KeyboardState keyboard)
        {
            // Light type controls
            HandleKeyPress(keyboard, Keys.Tab, () =>
            {
                _lightingManager.CyclePrimaryLightType();
                Console.WriteLine($"Light type: {_lightingManager.PrimaryLight?.Type}");
            });

            HandleKeyPress(keyboard, Keys.L, () =>
            {
                if (_lightingManager.PrimaryLight != null)
                {
                    _lightingManager.PrimaryLight.AutoRotate = !_lightingManager.PrimaryLight.AutoRotate;
                    Console.WriteLine($"Light rotation: {_lightingManager.PrimaryLight.AutoRotate}");
                }
            });

            HandleKeyPress(keyboard, Keys.V, () =>
            {
                _lightingManager.ShowLightPositions = !_lightingManager.ShowLightPositions;
                Console.WriteLine($"Light visualization: {_lightingManager.ShowLightPositions}");
            });

            // Lighting component toggles
            HandleKeyPress(keyboard, Keys.D1, () =>
            {
                _lightingManager.EnableAmbient = false;
                Console.WriteLine("Ambient light: OFF");
            });

            HandleKeyPress(keyboard, Keys.D2, () =>
            {
                _lightingManager.EnableAmbient = true;
                Console.WriteLine($"Ambient light: ON ({_lightingManager.AmbientStrength:F2})");
            });

            HandleKeyPress(keyboard, Keys.D3, () =>
            {
                _lightingManager.EnableDiffuse = false;
                Console.WriteLine("Diffuse light: OFF");
            });

            HandleKeyPress(keyboard, Keys.D4, () =>
            {
                _lightingManager.EnableDiffuse = true;
                Console.WriteLine($"Diffuse light: ON ({_lightingManager.DiffuseStrength:F2})");
            });

            HandleKeyPress(keyboard, Keys.D5, () =>
            {
                _lightingManager.EnableSpecular = false;
                Console.WriteLine("Specular light: OFF");
            });

            HandleKeyPress(keyboard, Keys.D6, () =>
            {
                _lightingManager.EnableSpecular = true;
                Console.WriteLine($"Specular light: ON ({_lightingManager.SpecularStrength:F2})");
            });

            // Light intensity controls
            HandleKeyPress(keyboard, Keys.Q, () =>
            {
                _lightingManager.AdjustLightingStrength("intensity", -0.2f);
                Console.WriteLine($"Light intensity: {_lightingManager.PrimaryLight?.Intensity:F1}");
            });

            HandleKeyPress(keyboard, Keys.E, () =>
            {
                _lightingManager.AdjustLightingStrength("intensity", 0.2f);
                Console.WriteLine($"Light intensity: {_lightingManager.PrimaryLight?.Intensity:F1}");
            });

            // Component strength controls
            HandleKeyPress(keyboard, Keys.Z, () =>
            {
                _lightingManager.AdjustLightingStrength("ambient", -0.05f);
                Console.WriteLine($"Ambient strength: {_lightingManager.AmbientStrength:F2}");
            });

            HandleKeyPress(keyboard, Keys.X, () =>
            {
                _lightingManager.AdjustLightingStrength("ambient", 0.05f);
                Console.WriteLine($"Ambient strength: {_lightingManager.AmbientStrength:F2}");
            });

            HandleKeyPress(keyboard, Keys.C, () =>
            {
                _lightingManager.AdjustLightingStrength("specular", -0.1f);
                Console.WriteLine($"Specular strength: {_lightingManager.SpecularStrength:F1}");
            });

            HandleKeyPress(keyboard, Keys.G, () =>
            {
                _lightingManager.AdjustLightingStrength("specular", 0.1f);
                Console.WriteLine($"Specular strength: {_lightingManager.SpecularStrength:F1}");
            });

            // Light color presets
            HandleKeyPress(keyboard, Keys.D7, () =>
            {
                _isColorCycling = false;
                _lightingManager.SetLightColor("white");
                Console.WriteLine("Light color: White");
            });

            HandleKeyPress(keyboard, Keys.D8, () =>
            {
                _isColorCycling = false;
                _lightingManager.SetLightColor("warm");
                Console.WriteLine("Light color: Warm White");
            });

            HandleKeyPress(keyboard, Keys.D9, () =>
            {
                _isColorCycling = false;
                _lightingManager.SetLightColor("cool");
                Console.WriteLine("Light color: Cool White");
            });

            HandleKeyPress(keyboard, Keys.D0, () =>
            {
                _isColorCycling = !_isColorCycling;
                if (_isColorCycling)
                {
                    Console.WriteLine("Light color: Cycling (rainbow)");
                }
                else
                {
                    _lightingManager.SetLightColor("white");
                    Console.WriteLine("Light color: Stopped cycling, reset to white");
                }
            });

            // Color cycling through presets
            HandleKeyPress(keyboard, Keys.Comma, () =>
            {
                _isColorCycling = false;
                _currentColorIndex = (_currentColorIndex - 1 + _colorPresets.Length) % _colorPresets.Length;
                _lightingManager.SetLightColor(_colorPresets[_currentColorIndex]);
                Console.WriteLine($"Light color: {_colorPresets[_currentColorIndex]}");
            });

            HandleKeyPress(keyboard, Keys.Period, () =>
            {
                _isColorCycling = false;
                _currentColorIndex = (_currentColorIndex + 1) % _colorPresets.Length;
                _lightingManager.SetLightColor(_colorPresets[_currentColorIndex]);
                Console.WriteLine($"Light color: {_colorPresets[_currentColorIndex]}");
            });

            // Lighting scenarios
            HandleKeyPress(keyboard, Keys.F1, () => LoadScenario("sunrise"));
            HandleKeyPress(keyboard, Keys.F2, () => LoadScenario("noon"));
            HandleKeyPress(keyboard, Keys.F3, () => LoadScenario("sunset"));
            HandleKeyPress(keyboard, Keys.F4, () => LoadScenario("night"));
            HandleKeyPress(keyboard, Keys.F5, () => LoadScenario("studio"));
            HandleKeyPress(keyboard, Keys.F6, () => LoadScenario("candlelight"));
            HandleKeyPress(keyboard, Keys.F7, () => LoadScenario("default"));

            // Cycle through scenarios
            HandleKeyPress(keyboard, Keys.F11, () =>
            {
                _currentScenarioIndex = (_currentScenarioIndex + 1) % _lightingScenarios.Length;
                LoadScenario(_lightingScenarios[_currentScenarioIndex]);
            });

            // Reset all lighting
            HandleKeyPress(keyboard, Keys.F12, () =>
            {
                _lightingManager.ResetToDefaults();
                _isColorCycling = false;
                _currentColorIndex = 0;
                _currentScenarioIndex = 0;
                Console.WriteLine("Lighting reset to defaults");
            });

            // Advanced controls (with modifiers)
            if (keyboard.IsKeyDown(Keys.LeftShift) || keyboard.IsKeyDown(Keys.RightShift))
            {
                // Shift + number keys for fine adjustments
                HandleKeyPress(keyboard, Keys.D1, () =>
                {
                    _lightingManager.AdjustLightingStrength("ambient", -0.01f);
                    Console.WriteLine($"Ambient (fine): {_lightingManager.AmbientStrength:F3}");
                });

                HandleKeyPress(keyboard, Keys.D2, () =>
                {
                    _lightingManager.AdjustLightingStrength("ambient", 0.01f);
                    Console.WriteLine($"Ambient (fine): {_lightingManager.AmbientStrength:F3}");
                });

                HandleKeyPress(keyboard, Keys.D3, () =>
                {
                    _lightingManager.AdjustLightingStrength("diffuse", -0.01f);
                    Console.WriteLine($"Diffuse (fine): {_lightingManager.DiffuseStrength:F3}");
                });

                HandleKeyPress(keyboard, Keys.D4, () =>
                {
                    _lightingManager.AdjustLightingStrength("diffuse", 0.01f);
                    Console.WriteLine($"Diffuse (fine): {_lightingManager.DiffuseStrength:F3}");
                });
            }
        }

        /// <summary>
        /// Load a lighting scenario and provide educational feedback
        /// </summary>
        /// <param name="scenarioName">Name of the scenario to load</param>
        private void LoadScenario(string scenarioName)
        {
            _lightingManager.LoadLightingScenario(scenarioName);
            _isColorCycling = false;

            // Provide educational information about the scenario
            switch (scenarioName.ToLower())
            {
                case "sunrise":
                    Console.WriteLine("=== SUNRISE LIGHTING ===");
                    Console.WriteLine("Warm, low-angle directional light simulating early morning sun");
                    Console.WriteLine("Notice: Strong directional shadows, warm color temperature");
                    break;

                case "noon":
                    Console.WriteLine("=== NOON LIGHTING ===");
                    Console.WriteLine("Bright, overhead directional light simulating midday sun");
                    Console.WriteLine("Notice: Short shadows, neutral white light, high intensity");
                    break;

                case "sunset":
                    Console.WriteLine("=== SUNSET LIGHTING ===");
                    Console.WriteLine("Warm, orange directional light simulating evening sun");
                    Console.WriteLine("Notice: Long shadows, warm/orange color, romantic atmosphere");
                    break;

                case "night":
                    Console.WriteLine("=== NIGHT LIGHTING ===");
                    Console.WriteLine("Cool, dim point light simulating moonlight or street lighting");
                    Console.WriteLine("Notice: Very low ambient, cool color temperature, dramatic shadows");
                    break;

                case "studio":
                    Console.WriteLine("=== STUDIO LIGHTING ===");
                    Console.WriteLine("Multiple lights: Key light + fill light for professional photography");
                    Console.WriteLine("Notice: Balanced lighting, minimal harsh shadows, good detail visibility");
                    break;

                case "candlelight":
                    Console.WriteLine("=== CANDLELIGHT ===");
                    Console.WriteLine("Warm, intimate point light with strong attenuation");
                    Console.WriteLine("Notice: Very warm color, rapid light falloff, cozy atmosphere");
                    break;

                default:
                    Console.WriteLine("=== DEFAULT LIGHTING ===");
                    Console.WriteLine("Basic point light setup for general 3D visualization");
                    Console.WriteLine("Notice: Balanced lighting suitable for examining 3D models");
                    break;
            }

            Console.WriteLine($"Light type: {_lightingManager.PrimaryLight?.Type}");
            Console.WriteLine($"Light count: {_lightingManager.Lights.Count}");
            Console.WriteLine();
        }

        /// <summary>
        /// Handle a key press with repeat prevention
        /// </summary>
        /// <param name="keyboard">Current keyboard state</param>
        /// <param name="key">Key to check</param>
        /// <param name="action">Action to perform on key press</param>
        private void HandleKeyPress(KeyboardState keyboard, Keys key, Action action)
        {
            int keyIndex = (int)key;
            if (keyIndex >= 0 && keyIndex < _keyPressed.Length)
            {
                if (keyboard.IsKeyDown(key) && !_keyPressed[keyIndex])
                {
                    _keyPressed[keyIndex] = true;
                    action?.Invoke();
                }
                else if (!keyboard.IsKeyDown(key))
                {
                    _keyPressed[keyIndex] = false;
                }
            }
        }

        /// <summary>
        /// Print available lighting controls to console
        /// </summary>
        public void PrintControls()
        {
            Console.WriteLine("\n" + new string('=', 70));
            Console.WriteLine("LIGHTING CONTROLS - Educational Interface");
            Console.WriteLine(new string('=', 70));
            Console.WriteLine("=== BASIC LIGHTING COMPONENTS ===");
            Console.WriteLine("  1/2 - Ambient light OFF/ON (base illumination)");
            Console.WriteLine("  3/4 - Diffuse light OFF/ON (surface shading)");
            Console.WriteLine("  5/6 - Specular light OFF/ON (reflective highlights)");
            Console.WriteLine();
            Console.WriteLine("=== LIGHT PROPERTIES ===");
            Console.WriteLine("  Tab - Cycle light type (Directional → Point → Spot)");
            Console.WriteLine("  L - Toggle light rotation/animation");
            Console.WriteLine("  V - Toggle light position visualization");
            Console.WriteLine("  Q/E - Decrease/Increase light intensity");
            Console.WriteLine();
            Console.WriteLine("=== COMPONENT STRENGTH ===");
            Console.WriteLine("  Z/X - Adjust ambient strength");
            Console.WriteLine("  C/G - Adjust specular strength");
            Console.WriteLine("  Shift+1/2 - Fine ambient adjustment");
            Console.WriteLine("  Shift+3/4 - Fine diffuse adjustment");
            Console.WriteLine();
            Console.WriteLine("=== LIGHT COLOR ===");
            Console.WriteLine("  7 - White light   |   8 - Warm white   |   9 - Cool white");
            Console.WriteLine("  0 - Toggle rainbow color cycling");
            Console.WriteLine("  , / . - Previous/Next color preset");
            Console.WriteLine();
            Console.WriteLine("=== LIGHTING SCENARIOS (Educational) ===");
            Console.WriteLine("  F1 - Sunrise    |   F2 - Noon       |   F3 - Sunset");
            Console.WriteLine("  F4 - Night      |   F5 - Studio     |   F6 - Candlelight");
            Console.WriteLine("  F7 - Default    |   F11 - Cycle scenarios");
            Console.WriteLine("  F12 - Reset all lighting to defaults");
            Console.WriteLine(new string('=', 70));
            Console.WriteLine();

            // Show current state
            Console.WriteLine("Current Lighting State:");
            Console.WriteLine($"  Light Type: {_lightingManager.PrimaryLight?.Type}");
            Console.WriteLine($"  Light Color: ({_lightingManager.PrimaryLight?.Color.X:F1}, {_lightingManager.PrimaryLight?.Color.Y:F1}, {_lightingManager.PrimaryLight?.Color.Z:F1})");
            Console.WriteLine($"  Light Intensity: {_lightingManager.PrimaryLight?.Intensity:F1}");
            Console.WriteLine($"  Components: Ambient={(_lightingManager.EnableAmbient ? "ON" : "OFF")} | Diffuse={(_lightingManager.EnableDiffuse ? "ON" : "OFF")} | Specular={(_lightingManager.EnableSpecular ? "ON" : "OFF")}");
            Console.WriteLine($"  Strengths: A={_lightingManager.AmbientStrength:F2} | D={_lightingManager.DiffuseStrength:F2} | S={_lightingManager.SpecularStrength:F2}");
            Console.WriteLine();
        }

        /// <summary>
        /// Get a summary of current lighting settings for UI display
        /// </summary>
        /// <returns>Formatted string with current settings</returns>
        public string GetCurrentSettings()
        {
            if (_lightingManager.PrimaryLight == null) return "No primary light";

            return $"Type: {_lightingManager.PrimaryLight.Type} | " +
                   $"Intensity: {_lightingManager.PrimaryLight.Intensity:F1} | " +
                   $"A:{(_lightingManager.EnableAmbient ? _lightingManager.AmbientStrength.ToString("F1") : "OFF")} " +
                   $"D:{(_lightingManager.EnableDiffuse ? _lightingManager.DiffuseStrength.ToString("F1") : "OFF")} " +
                   $"S:{(_lightingManager.EnableSpecular ? _lightingManager.SpecularStrength.ToString("F1") : "OFF")}";
        }

        /// <summary>
        /// Enable/disable educational mode (more verbose console output)
        /// </summary>
        public bool EducationalMode { get; set; } = true;

        /// <summary>
        /// Check if any lighting-related key is currently pressed
        /// </summary>
        /// <param name="keyboard">Current keyboard state</param>
        /// <returns>True if any lighting control key is pressed</returns>
        public bool IsHandlingInput(KeyboardState keyboard)
        {
            return keyboard.IsKeyDown(Keys.Tab) || keyboard.IsKeyDown(Keys.L) || keyboard.IsKeyDown(Keys.V) ||
                   keyboard.IsKeyDown(Keys.D1) || keyboard.IsKeyDown(Keys.D2) || keyboard.IsKeyDown(Keys.D3) ||
                   keyboard.IsKeyDown(Keys.D4) || keyboard.IsKeyDown(Keys.D5) || keyboard.IsKeyDown(Keys.D6) ||
                   keyboard.IsKeyDown(Keys.Q) || keyboard.IsKeyDown(Keys.E) ||
                   keyboard.IsKeyDown(Keys.Z) || keyboard.IsKeyDown(Keys.X) ||
                   keyboard.IsKeyDown(Keys.C) || keyboard.IsKeyDown(Keys.G) ||
                   keyboard.IsKeyDown(Keys.D7) || keyboard.IsKeyDown(Keys.D8) || keyboard.IsKeyDown(Keys.D9) || keyboard.IsKeyDown(Keys.D0) ||
                   keyboard.IsKeyDown(Keys.F1) || keyboard.IsKeyDown(Keys.F2) || keyboard.IsKeyDown(Keys.F3) ||
                   keyboard.IsKeyDown(Keys.F4) || keyboard.IsKeyDown(Keys.F5) || keyboard.IsKeyDown(Keys.F6) ||
                   keyboard.IsKeyDown(Keys.F7) || keyboard.IsKeyDown(Keys.F11) || keyboard.IsKeyDown(Keys.F12);
        }
    }
}