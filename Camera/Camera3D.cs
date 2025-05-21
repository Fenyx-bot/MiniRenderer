using OpenTK.Mathematics;
using System;

namespace MiniRenderer.Camera
{
    /// <summary>
    /// 3D camera with support for perspective and orthographic projections
    /// </summary>
    public class Camera3D
    {
        // Camera position in 3D space
        public Vector3 Position { get; set; }

        // Camera orientation
        private float _yaw = -90.0f;   // Rotation around Y-axis (in degrees)
        private float _pitch = 0.0f;   // Rotation around X-axis (in degrees)

        // Camera orientation vectors
        public Vector3 Front { get; private set; }
        public Vector3 Right { get; private set; }
        public Vector3 Up { get; private set; }
        public Vector3 WorldUp { get; private set; }

        // Field of view in degrees (for perspective projection)
        public float FieldOfView { get; set; }

        // Aspect ratio (width / height)
        public float AspectRatio { get; private set; }

        // Near and far clipping planes
        public float NearPlane { get; set; }
        public float FarPlane { get; set; }

        // Orthographic projection size
        public float OrthographicSize { get; set; }

        // Camera projection mode
        public ProjectionMode Mode { get; set; }

        // Projection modes
        public enum ProjectionMode
        {
            Perspective,
            Orthographic
        }

        // Camera transform matrices
        private Matrix4 _viewMatrix;
        private Matrix4 _projectionMatrix;
        private bool _isDirty;

        /// <summary>
        /// Create a new 3D camera with default settings
        /// </summary>
        /// <param name="position">Initial camera position</param>
        /// <param name="viewportWidth">Width of the viewport in pixels</param>
        /// <param name="viewportHeight">Height of the viewport in pixels</param>
        public Camera3D(Vector3 position, int viewportWidth, int viewportHeight)
        {
            Position = position;
            WorldUp = Vector3.UnitY; // Default up is Y-axis (0, 1, 0)

            // Default settings
            _yaw = -90.0f;  // -90 degrees means looking along negative Z
            _pitch = 0.0f;
            FieldOfView = 45.0f;
            AspectRatio = viewportWidth / (float)viewportHeight;
            NearPlane = 0.1f;
            FarPlane = 100.0f;
            OrthographicSize = 5.0f;
            Mode = ProjectionMode.Perspective;

            // Initialize orientation vectors
            UpdateVectors();

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
            AspectRatio = width / (float)height;
            _isDirty = true;
        }

        /// <summary>
        /// Rotate the camera based on mouse movement
        /// </summary>
        /// <param name="xOffset">Horizontal mouse movement</param>
        /// <param name="yOffset">Vertical mouse movement</param>
        /// <param name="sensitivity">Mouse sensitivity</param>
        public void Rotate(float xOffset, float yOffset, float sensitivity = 0.1f)
        {
            _yaw += xOffset * sensitivity;
            _pitch -= yOffset * sensitivity; // Inverted Y-axis

            // Constrain pitch to avoid camera flipping
            _pitch = Math.Clamp(_pitch, -89.0f, 89.0f);

            // Update camera orientation vectors
            UpdateVectors();
        }

        /// <summary>
        /// Move the camera relative to its orientation
        /// </summary>
        /// <param name="direction">Movement direction</param>
        /// <param name="speed">Movement speed</param>
        public void Move(Vector3 direction, float speed)
        {
            // Apply movement based on camera orientation
            if (direction.Z != 0) // Forward/Backward
                Position += Front * direction.Z * speed;

            if (direction.X != 0) // Right/Left
                Position += Right * direction.X * speed;

            if (direction.Y != 0) // Up/Down
                Position += Up * direction.Y * speed;

            _isDirty = true;
        }

        /// <summary>
        /// Toggle between perspective and orthographic projection modes
        /// </summary>
        public void ToggleProjectionMode()
        {
            Mode = Mode == ProjectionMode.Perspective
                ? ProjectionMode.Orthographic
                : ProjectionMode.Perspective;
            _isDirty = true;
        }

        /// <summary>
        /// Update the camera's orientation vectors based on yaw and pitch
        /// </summary>
        private void UpdateVectors()
        {
            // Calculate front vector from yaw and pitch angles
            Vector3 front;
            front.X = (float)Math.Cos(MathHelper.DegreesToRadians(_yaw)) * (float)Math.Cos(MathHelper.DegreesToRadians(_pitch));
            front.Y = (float)Math.Sin(MathHelper.DegreesToRadians(_pitch));
            front.Z = (float)Math.Sin(MathHelper.DegreesToRadians(_yaw)) * (float)Math.Cos(MathHelper.DegreesToRadians(_pitch));

            // Normalize front vector
            Front = Vector3.Normalize(front);

            // Calculate right and up vectors
            Right = Vector3.Normalize(Vector3.Cross(Front, WorldUp));
            Up = Vector3.Normalize(Vector3.Cross(Right, Front));

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
            // Calculate view matrix (look from Position along Front direction)
            _viewMatrix = Matrix4.LookAt(Position, Position + Front, Up);

            // Calculate projection matrix based on current mode
            if (Mode == ProjectionMode.Perspective)
            {
                // Create perspective projection
                _projectionMatrix = Matrix4.CreatePerspectiveFieldOfView(
                    MathHelper.DegreesToRadians(FieldOfView),
                    AspectRatio,
                    NearPlane,
                    FarPlane
                );
            }
            else
            {
                // Calculate orthographic size based on aspect ratio
                float orthoWidth = OrthographicSize * AspectRatio;
                float orthoHeight = OrthographicSize;

                // Create orthographic projection
                _projectionMatrix = Matrix4.CreateOrthographic(
                    orthoWidth * 2.0f,
                    orthoHeight * 2.0f,
                    NearPlane,
                    FarPlane
                );
            }

            // Mark as up to date
            _isDirty = false;
        }

        /// <summary>
        /// Adjust the camera's field of view (zoom)
        /// </summary>
        /// <param name="zoomAmount">Amount to adjust FOV (positive = zoom in, negative = zoom out)</param>
        public void AdjustZoom(float zoomAmount)
        {
            if (Mode == ProjectionMode.Perspective)
            {
                // Adjust field of view (with clamping)
                FieldOfView = Math.Clamp(FieldOfView - zoomAmount, 1.0f, 90.0f);
            }
            else
            {
                // Adjust orthographic size (with clamping)
                OrthographicSize = Math.Clamp(OrthographicSize - zoomAmount * 0.5f, 0.1f, 100.0f);
            }

            _isDirty = true;
        }
    }
}