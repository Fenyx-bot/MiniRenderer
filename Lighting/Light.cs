using OpenTK.Mathematics;
using System;

namespace MiniRenderer.Lighting
{
    /// <summary>
    /// Light types supported by the lighting system
    /// </summary>
    public enum LightType
    {
        Directional = 0,
        Point = 1,
        Spot = 2
    }

    /// <summary>
    /// Represents a light source in 3D space with various properties
    /// </summary>
    public class Light
    {
        // Light identification
        public string Name { get; set; } = "Light";
        public LightType Type { get; set; } = LightType.Point;

        // Transform properties
        public Vector3 Position { get; set; } = new Vector3(0.0f, 2.0f, 0.0f);
        public Vector3 Direction { get; set; } = new Vector3(0.0f, -1.0f, 0.0f);

        // Light properties
        public Vector3 Color { get; set; } = new Vector3(1.0f, 1.0f, 1.0f);
        public float Intensity { get; set; } = 1.0f;

        // Attenuation (for point and spot lights)
        public float Constant { get; set; } = 1.0f;
        public float Linear { get; set; } = 0.09f;
        public float Quadratic { get; set; } = 0.032f;

        // Spot light properties
        public float InnerCutOff { get; set; } = 12.5f; // In degrees
        public float OuterCutOff { get; set; } = 17.5f; // In degrees

        // Animation properties
        public bool AutoRotate { get; set; } = false;
        public float RotationSpeed { get; set; } = 1.0f;
        public float RotationRadius { get; set; } = 3.0f;
        public Vector3 RotationCenter { get; set; } = Vector3.Zero;

        // Internal animation state
        private float _animationTime = 0.0f;
        private Vector3 _originalPosition;
        private bool _animationInitialized = false;

        /// <summary>
        /// Create a new light with default point light settings
        /// </summary>
        public Light()
        {
            Name = "Point Light";
            Type = LightType.Point;
        }

        /// <summary>
        /// Create a new light with specified type
        /// </summary>
        /// <param name="type">Type of light to create</param>
        /// <param name="name">Name for the light</param>
        public Light(LightType type, string name = null)
        {
            Type = type;
            Name = name ?? $"{type} Light";

            // Set appropriate defaults based on type
            switch (type)
            {
                case LightType.Directional:
                    Direction = new Vector3(-0.2f, -1.0f, -0.3f);
                    Position = new Vector3(0.0f, 10.0f, 0.0f); // High up (position less important for directional)
                    break;

                case LightType.Point:
                    Position = new Vector3(0.0f, 2.0f, 0.0f);
                    break;

                case LightType.Spot:
                    Position = new Vector3(0.0f, 3.0f, 0.0f);
                    Direction = new Vector3(0.0f, -1.0f, 0.0f);
                    InnerCutOff = 12.5f;
                    OuterCutOff = 15.0f;
                    break;
            }
        }

        /// <summary>
        /// Update the light (handle animations, etc.)
        /// </summary>
        /// <param name="deltaTime">Time since last update</param>
        public void Update(float deltaTime)
        {
            if (AutoRotate)
            {
                if (!_animationInitialized)
                {
                    _originalPosition = Position;
                    _animationInitialized = true;
                }

                _animationTime += deltaTime * RotationSpeed;

                // Calculate new position based on rotation
                float x = (float)Math.Sin(_animationTime) * RotationRadius;
                float z = (float)Math.Cos(_animationTime) * RotationRadius;
                float y = _originalPosition.Y + (float)Math.Sin(_animationTime * 2.0f) * 0.5f; // Slight Y variation

                Position = RotationCenter + new Vector3(x, y, z);

                // For spot lights, make them look at the center
                if (Type == LightType.Spot)
                {
                    Direction = Vector3.Normalize(RotationCenter - Position);
                }
            }
            else
            {
                _animationInitialized = false;
            }
        }

        /// <summary>
        /// Apply this light's properties to a shader
        /// </summary>
        /// <param name="shader">Shader to apply properties to</param>
        /// <param name="uniformPrefix">Prefix for uniform names (e.g., "light" for "light.position")</param>
        public void ApplyToShader(Graphics.Shader shader, string uniformPrefix = "light")
        {
            shader.Use();

            // Basic properties
            shader.SetInt($"{uniformPrefix}.type", (int)Type);
            shader.SetVector3($"{uniformPrefix}.position", Position);
            shader.SetVector3($"{uniformPrefix}.direction", Vector3.Normalize(Direction));
            shader.SetVector3($"{uniformPrefix}.color", Color);
            shader.SetFloat($"{uniformPrefix}.intensity", Intensity);

            // Attenuation
            shader.SetFloat($"{uniformPrefix}.constant", Constant);
            shader.SetFloat($"{uniformPrefix}.linear", Linear);
            shader.SetFloat($"{uniformPrefix}.quadratic", Quadratic);

            // Spot light properties
            if (Type == LightType.Spot)
            {
                shader.SetFloat($"{uniformPrefix}.cutOff", (float)Math.Cos(MathHelper.DegreesToRadians(InnerCutOff)));
                shader.SetFloat($"{uniformPrefix}.outerCutOff", (float)Math.Cos(MathHelper.DegreesToRadians(OuterCutOff)));
            }
        }

        /// <summary>
        /// Create a directional light (like sunlight)
        /// </summary>
        /// <param name="direction">Direction the light is pointing</param>
        /// <param name="color">Light color</param>
        /// <param name="intensity">Light intensity</param>
        /// <returns>Configured directional light</returns>
        public static Light CreateDirectional(Vector3 direction, Vector3 color, float intensity = 1.0f)
        {
            var light = new Light(LightType.Directional, "Directional Light");
            light.Direction = Vector3.Normalize(direction);
            light.Color = color;
            light.Intensity = intensity;
            return light;
        }

        /// <summary>
        /// Create a point light (like a light bulb)
        /// </summary>
        /// <param name="position">Position of the light</param>
        /// <param name="color">Light color</param>
        /// <param name="intensity">Light intensity</param>
        /// <param name="constant">Constant attenuation factor</param>
        /// <param name="linear">Linear attenuation factor</param>
        /// <param name="quadratic">Quadratic attenuation factor</param>
        /// <returns>Configured point light</returns>
        public static Light CreatePoint(Vector3 position, Vector3 color, float intensity = 1.0f,
                                       float constant = 1.0f, float linear = 0.09f, float quadratic = 0.032f)
        {
            var light = new Light(LightType.Point, "Point Light");
            light.Position = position;
            light.Color = color;
            light.Intensity = intensity;
            light.Constant = constant;
            light.Linear = linear;
            light.Quadratic = quadratic;
            return light;
        }

        /// <summary>
        /// Create a spot light (like a flashlight)
        /// </summary>
        /// <param name="position">Position of the light</param>
        /// <param name="direction">Direction the light is pointing</param>
        /// <param name="color">Light color</param>
        /// <param name="intensity">Light intensity</param>
        /// <param name="innerCutOff">Inner cone angle in degrees</param>
        /// <param name="outerCutOff">Outer cone angle in degrees</param>
        /// <returns>Configured spot light</returns>
        public static Light CreateSpot(Vector3 position, Vector3 direction, Vector3 color, float intensity = 1.0f,
                                      float innerCutOff = 12.5f, float outerCutOff = 17.5f)
        {
            var light = new Light(LightType.Spot, "Spot Light");
            light.Position = position;
            light.Direction = Vector3.Normalize(direction);
            light.Color = color;
            light.Intensity = intensity;
            light.InnerCutOff = innerCutOff;
            light.OuterCutOff = outerCutOff;
            return light;
        }

        /// <summary>
        /// Set light color using RGB values (0-255)
        /// </summary>
        /// <param name="r">Red component (0-255)</param>
        /// <param name="g">Green component (0-255)</param>
        /// <param name="b">Blue component (0-255)</param>
        public void SetColorRGB(int r, int g, int b)
        {
            Color = new Vector3(r / 255.0f, g / 255.0f, b / 255.0f);
        }

        /// <summary>
        /// Set light color using a predefined color
        /// </summary>
        /// <param name="colorName">Name of the color preset</param>
        public void SetColorPreset(string colorName)
        {
            switch (colorName.ToLower())
            {
                case "white":
                    Color = new Vector3(1.0f, 1.0f, 1.0f);
                    break;
                case "warm":
                case "warmwhite":
                    Color = new Vector3(1.0f, 0.8f, 0.6f);
                    break;
                case "cool":
                case "coolwhite":
                    Color = new Vector3(0.6f, 0.8f, 1.0f);
                    break;
                case "red":
                    Color = new Vector3(1.0f, 0.0f, 0.0f);
                    break;
                case "green":
                    Color = new Vector3(0.0f, 1.0f, 0.0f);
                    break;
                case "blue":
                    Color = new Vector3(0.0f, 0.0f, 1.0f);
                    break;
                case "yellow":
                    Color = new Vector3(1.0f, 1.0f, 0.0f);
                    break;
                case "cyan":
                    Color = new Vector3(0.0f, 1.0f, 1.0f);
                    break;
                case "magenta":
                    Color = new Vector3(1.0f, 0.0f, 1.0f);
                    break;
                case "orange":
                    Color = new Vector3(1.0f, 0.5f, 0.0f);
                    break;
                case "purple":
                    Color = new Vector3(0.5f, 0.0f, 1.0f);
                    break;
                default:
                    Color = new Vector3(1.0f, 1.0f, 1.0f); // Default to white
                    break;
            }
        }

        /// <summary>
        /// Get a string representation of the light for debugging
        /// </summary>
        /// <returns>String describing the light</returns>
        public override string ToString()
        {
            return $"{Name} ({Type}) - Pos: {Position}, Color: ({Color.X:F2}, {Color.Y:F2}, {Color.Z:F2}), Intensity: {Intensity:F2}";
        }

        /// <summary>
        /// Clone this light
        /// </summary>
        /// <returns>A copy of this light</returns>
        public Light Clone()
        {
            return new Light
            {
                Name = Name + " (Copy)",
                Type = Type,
                Position = Position,
                Direction = Direction,
                Color = Color,
                Intensity = Intensity,
                Constant = Constant,
                Linear = Linear,
                Quadratic = Quadratic,
                InnerCutOff = InnerCutOff,
                OuterCutOff = OuterCutOff,
                AutoRotate = AutoRotate,
                RotationSpeed = RotationSpeed,
                RotationRadius = RotationRadius,
                RotationCenter = RotationCenter
            };
        }
    }
}