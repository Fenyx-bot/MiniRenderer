using OpenTK.Mathematics;

namespace MiniRenderer.Camera
{
    /// <summary>
    /// 2D camera for rendering sprites and 2D shapes
    /// </summary>
    public class Camera2D
    {
        // Camera position (center of the view)
        public Vector2 Position { get; set; }

        // Camera zoom factor (1.0 = normal, 2.0 = zoomed in, 0.5 = zoomed out)
        public float Zoom { get; set; }

        // Camera rotation in degrees
        public float Rotation { get; set; }

        // Viewport dimensions
        public int ViewportWidth { get; private set; }
        public int ViewportHeight { get; private set; }

        // Camera transform matrices
        private Matrix4 _viewMatrix;
        private Matrix4 _projectionMatrix;
        private bool _isDirty;

        /// <summary>
        /// Create a new 2D camera
        /// </summary>
        /// <param name="viewportWidth">Width of the viewport in pixels</param>
        /// <param name="viewportHeight">Height of the viewport in pixels</param>
        public Camera2D(int viewportWidth, int viewportHeight)
        {
            ViewportWidth = viewportWidth;
            ViewportHeight = viewportHeight;
            Position = Vector2.Zero;
            Zoom = 1.0f;
            Rotation = 0.0f;

            // Mark as dirty to calculate matrices on first use
            _isDirty = true;
        }

        /// <summary>
        /// Update the camera's viewport dimensions
        /// </summary>
        /// <param name="width">New viewport width</param>
        /// <param name="height">New viewport height</param>
        public void Resize(int width, int height)
        {
            ViewportWidth = width;
            ViewportHeight = height;
            _isDirty = true;
        }

        /// <summary>
        /// Move the camera by the specified offset
        /// </summary>
        /// <param name="offset">Movement offset</param>
        public void Move(Vector2 offset)
        {
            Position += offset;
            _isDirty = true;
        }

        /// <summary>
        /// Zoom the camera by the specified amount
        /// </summary>
        /// <param name="zoomAmount">Zoom factor (positive = zoom in, negative = zoom out)</param>
        public void AdjustZoom(float zoomAmount)
        {
            Zoom = Math.Clamp(Zoom + zoomAmount, 0.1f, 10.0f);
            _isDirty = true;
        }

        /// <summary>
        /// Rotate the camera by the specified angle in degrees
        /// </summary>
        /// <param name="angle">Rotation angle in degrees</param>
        public void Rotate(float angle)
        {
            Rotation += angle;
            _isDirty = true;
        }

        /// <summary>
        /// Get the view matrix for this camera
        /// </summary>
        /// <returns>View matrix</returns>
        public Matrix4 GetViewMatrix()
        {
            if (_isDirty)
            {
                UpdateMatrices();
            }

            return _viewMatrix;
        }

        /// <summary>
        /// Get the projection matrix for this camera
        /// </summary>
        /// <returns>Projection matrix</returns>
        public Matrix4 GetProjectionMatrix()
        {
            if (_isDirty)
            {
                UpdateMatrices();
            }

            return _projectionMatrix;
        }

        /// <summary>
        /// Update the view and projection matrices
        /// </summary>
        private void UpdateMatrices()
        {
            // Calculate aspect ratio
            float aspectRatio = ViewportWidth / (float)ViewportHeight;

            // Create a view matrix that:
            // 1. Applies rotation
            // 2. Applies zoom
            // 3. Translates to camera position

            _viewMatrix = Matrix4.Identity;

            // Apply rotation around the origin
            _viewMatrix *= Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(-Rotation));

            // Apply zoom (scale)
            _viewMatrix *= Matrix4.CreateScale(new Vector3(Zoom, Zoom, 1.0f));

            // Apply translation (negate position for camera movement)
            _viewMatrix *= Matrix4.CreateTranslation(new Vector3(-Position.X, -Position.Y, 0.0f));

            // Create an orthographic projection
            float width = 2.0f * aspectRatio;
            float height = 2.0f;

            // Create an orthographic projection with the coordinates:
            // Left: -width/2, Right: width/2
            // Bottom: -height/2, Top: height/2
            // Near: -1, Far: 1
            _projectionMatrix = Matrix4.CreateOrthographicOffCenter(
                -width / 2.0f, width / 2.0f,
                -height / 2.0f, height / 2.0f,
                -1.0f, 1.0f
            );

            // Mark as up to date
            _isDirty = false;
        }

        /// <summary>
        /// Convert screen coordinates to world coordinates
        /// </summary>
        /// <param name="screenPosition">Position in screen coordinates</param>
        /// <returns>Position in world coordinates</returns>
        public Vector2 ScreenToWorld(Vector2 screenPosition)
        {
            // Normalize screen coordinates to [-1, 1]
            float normalizedX = 2.0f * screenPosition.X / ViewportWidth - 1.0f;
            float normalizedY = 1.0f - 2.0f * screenPosition.Y / ViewportHeight; // Flip Y

            // Convert to homogeneous clip space
            Vector4 clipCoords = new Vector4(normalizedX, normalizedY, 0.0f, 1.0f);

            // Get the inverse view-projection matrix
            Matrix4 invVP = Matrix4.Invert(GetProjectionMatrix() * GetViewMatrix());

            // Get the view-projection matrix
            Matrix3 rotationMatrix = new Matrix3(invVP);

            // Transform to world space
            Vector4 worldCoords = Vector4.Transform(clipCoords, Quaternion.FromMatrix(rotationMatrix));

            // Perspective divide (not strictly necessary for orthographic, but good practice)
            return new Vector2(worldCoords.X, worldCoords.Y) / worldCoords.W;
        }

        /// <summary>
        /// Convert world coordinates to screen coordinates
        /// </summary>
        /// <param name="worldPosition">Position in world coordinates</param>
        /// <returns>Position in screen coordinates</returns>
        public Vector2 WorldToScreen(Vector2 worldPosition)
        {
            // Convert to homogeneous world space
            Vector4 worldCoords = new Vector4(worldPosition.X, worldPosition.Y, 0.0f, 1.0f);

            // Get the view-projection matrix
            Matrix4 vp = GetProjectionMatrix() * GetViewMatrix();

            Matrix3 rotationMatrix = new Matrix3(vp);

            // Transform to clip space
            Vector4 clipCoords = Vector4.Transform(worldCoords, Quaternion.FromMatrix(rotationMatrix));

            // Perspective divide
            Vector2 ndcCoords = new Vector2(clipCoords.X, clipCoords.Y) / clipCoords.W;

            // Convert to screen coordinates
            float screenX = (ndcCoords.X + 1.0f) * 0.5f * ViewportWidth;
            float screenY = (1.0f - ndcCoords.Y) * 0.5f * ViewportHeight; // Flip Y

            return new Vector2(screenX, screenY);
        }
    }
}