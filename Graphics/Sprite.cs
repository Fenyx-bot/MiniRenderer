using OpenTK.Mathematics;

namespace MiniRenderer.Graphics
{
    /// <summary>
    /// Represents a 2D sprite with position, size, rotation, color, and texture
    /// </summary>
    public class Sprite
    {
        // Transform properties
        public Vector2 Position { get; set; }
        public Vector2 Size { get; set; }
        public float Rotation { get; set; } // In degrees

        // Visual properties
        public Vector4 Color { get; set; }
        public Texture? Texture { get; set; }

        // Origin for rotation (normalized 0-1 coordinates where 0.5,0.5 is center)
        public Vector2 Origin { get; set; }

        // Has the sprite changed since last render?
        private bool _isDirty;

        /// <summary>
        /// Create a new sprite
        /// </summary>
        /// <param name="texture">Texture to use for the sprite</param>
        /// <param name="position">Position in world coordinates</param>
        /// <param name="size">Size in world units</param>
        public Sprite(Texture texture, Vector2 position, Vector2 size)
        {
            Texture = texture;
            Position = position;
            Size = size;
            Rotation = 0.0f;
            Color = new Vector4(1.0f); // White, fully opaque
            Origin = new Vector2(0.5f); // Center by default
            _isDirty = true;
        }

        /// <summary>
        /// Create a colored rectangle without a texture
        /// </summary>
        /// <param name="position">Position in world coordinates</param>
        /// <param name="size">Size in world units</param>
        /// <param name="color">Color as RGBA</param>
        public Sprite(Vector2 position, Vector2 size, Vector4 color)
        {
            Texture = null;
            Position = position;
            Size = size;
            Rotation = 0.0f;
            Color = color;
            Origin = new Vector2(0.5f); // Center by default
            _isDirty = true;
        }

        /// <summary>
        /// Get the model matrix for this sprite
        /// </summary>
        /// <returns>The model transformation matrix</returns>
        public Matrix4 GetModelMatrix()
        {
            // Start with identity matrix
            Matrix4 model = Matrix4.Identity;

            // Calculate the sprite's pivot point
            float pivotX = Position.X + Size.X * Origin.X;
            float pivotY = Position.Y + Size.Y * Origin.Y;

            // Order of transformations (right to left):
            // 1. Scale to the desired size
            // 2. Rotate around the origin point
            // 3. Translate to the position

            // First, translate to the pivot point
            model *= Matrix4.CreateTranslation(new Vector3(pivotX, pivotY, 0.0f));

            // Then rotate
            model *= Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(Rotation));

            // Then translate back to have the pivot at the rotation point
            model *= Matrix4.CreateTranslation(new Vector3(-Size.X * Origin.X, -Size.Y * Origin.Y, 0.0f));

            // Finally, scale to the desired size
            model *= Matrix4.CreateScale(new Vector3(Size.X, Size.Y, 1.0f));

            return model;
        }

        /// <summary>
        /// Set the sprite's position
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        public void SetPosition(float x, float y)
        {
            Position = new Vector2(x, y);
            _isDirty = true;
        }

        /// <summary>
        /// Set the sprite's size
        /// </summary>
        /// <param name="width">Width</param>
        /// <param name="height">Height</param>
        public void SetSize(float width, float height)
        {
            Size = new Vector2(width, height);
            _isDirty = true;
        }

        /// <summary>
        /// Set the sprite's color
        /// </summary>
        /// <param name="r">Red (0-1)</param>
        /// <param name="g">Green (0-1)</param>
        /// <param name="b">Blue (0-1)</param>
        /// <param name="a">Alpha (0-1)</param>
        public void SetColor(float r, float g, float b, float a = 1.0f)
        {
            Color = new Vector4(r, g, b, a);
        }

        /// <summary>
        /// Get whether the sprite uses a texture
        /// </summary>
        public bool UseTexture => Texture != null;

        /// <summary>
        /// Is the sprite dirty (needs recalculation)?
        /// </summary>
        public bool IsDirty => _isDirty;

        /// <summary>
        /// Mark the sprite as clean (up to date)
        /// </summary>
        public void MarkClean()
        {
            _isDirty = false;
        }

        /// <summary>
        /// Mark the sprite as dirty (needs update)
        /// </summary>
        public void MarkDirty()
        {
            _isDirty = true;
        }
    }
}