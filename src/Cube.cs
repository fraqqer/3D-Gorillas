using OpenTK;
using System;

namespace Gorillas3D.Objects
{
    public class Cube
    {
        public Vector3 position;
        private Vector3 velocity;
        private Vector3 acceleration;
        private Vector3 force;
        private readonly string name = $"Cube {cubeIndex}";
        private const float mass = 3;
        // Gravity acts on the force, allowing us to derive the acceleration of the object.
        private const float gravity = -0.00001f;
        private static int cubeIndex = 1;

        public Cube(ref Vector3 pCubePosition, string pName = "")
        {
            position = pCubePosition;
            force.Y = gravity;

            if (pName == string.Empty)
            {
                cubeIndex++;
            }
            else
            {
                name = pName;
            }
        }

        public Vector3 Position {get => position; set => this.position = value; }
        public Vector3 Velocity { get => velocity; set => this.velocity = value; }
        public Vector3 Acceleration { get => acceleration; }
        public string Name { get => name; }
        public Vector3 Force { get => force; set => this.force = value; }
        public float Mass { get => mass; }
        public float Gravity { get => gravity; }

        // simple euler integration
        public void UpdatePhysics()
        {
            try
            {
                acceleration = force / mass;
            }
            catch
            {
                acceleration = Vector3.Zero;
            }
            velocity += acceleration * Game.Window.deltaTime;
            position += velocity * Game.Window.deltaTime;
        }
    }
}