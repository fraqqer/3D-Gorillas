using Gorillas3D.Objects;
using OpenTK;
using System.Collections.Generic;

namespace Gorillas3D.Collision
{
    public static class Collision
    {
        private static readonly Vector3 BoxSize = new Vector3(1, 2, 1);

        /// <summary>
        /// Tests whether two positions are colliding using AABB. Uses BoxSize as a boundary.
        /// </summary>
        /// <param name="positionOne">The current position of the first box.</param>
        /// <param name="positionTwo">The current position of the second box.</param>
        /// <returns></returns>
        private static bool AABBCollision(ref Vector3 positionOne, ref Vector3 positionTwo)
        {
            bool collisionX = false;
            bool collisionY = false;
            bool collisionZ = false;
            // When drawing the positions of the boxes, they are created by a factor of 2 in the x-axis. This means that in order to get from box A (0, 0) to box B (2, 0) to box C (4, 0), to box D (6, 0) etc.
            // This method assumes the size of the box is 1, as the center of each box is one game unit from every side.
            if (positionOne.X + BoxSize.X > positionTwo.X && positionTwo.X + BoxSize.X > positionOne.X)
                collisionX = true;

            if (positionOne.Y + BoxSize.Y > positionTwo.Y && positionTwo.Y + BoxSize.Y > positionOne.Y)
                collisionY = true;

            if (positionOne.Z + BoxSize.Z > positionTwo.Z && positionTwo.Z + BoxSize.Z > positionOne.Z)
                collisionZ = true;

            if (collisionX && collisionY && collisionZ)
                return true;

            return false;
        }

        /// <summary>
        /// Update each collision as they occur.
        /// </summary>
        public static void UpdateCollisions()
        {
            // Searches each cube position currently in the game.
            for (int cubeOneIndex = 0; cubeOneIndex < Game.Window.cubePositions.Count; cubeOneIndex++)
            {
                // Make sure each cube does not fall below the grassy plane.
                if (Game.Window.cubePositions[cubeOneIndex].Position.Y < 0)
                {
                    Game.Window.cubePositions[cubeOneIndex].Position = new Vector3(Game.Window.cubePositions[cubeOneIndex].Position.X, 0.0001f, Game.Window.cubePositions[cubeOneIndex].Position.Z);
                    StopCube(ref Game.Window.cubePositions, cubeOneIndex);
                }

                // Player 1 and 2 are unaffected by gravity. They are always drawn after the building cubes.
                if (cubeOneIndex == Game.Window.cubePositions.Count - 2 || cubeOneIndex == Game.Window.cubePositions.Count - 3)
                    StopCube(ref Game.Window.cubePositions, cubeOneIndex);

                // Each cube is affected via AABB.
                for (int cubeTwoIndex = 1; cubeTwoIndex < Game.Window.cubePositions.Count; cubeTwoIndex++)
                {
                    if (cubeOneIndex != cubeTwoIndex && AABBCollision(ref Game.Window.cubePositions[cubeOneIndex].position, ref Game.Window.cubePositions[cubeTwoIndex].position))
                    {
                        CollisionResponse(ref Game.Window.cubePositions, cubeOneIndex, cubeTwoIndex);
                    }
                }
            }
        }

        private static void CollisionResponse(ref List<Cube> pCubeList, int pFirstIndex, int pSecondIndex)
        {
            StopCube(ref pCubeList, pFirstIndex);
            StopCube(ref pCubeList, pSecondIndex);

            // if banana cube is one of the collided objects
            if (pFirstIndex == pCubeList.Count - 1 || pSecondIndex == pCubeList.Count - 1)
            {
                #region Debug Code, Remove Once tested
                if (pFirstIndex == pCubeList.Count - 3 || pSecondIndex == pCubeList.Count - 3)
                {
                    System.Console.WriteLine("Player Two Wins!");
                    Game.Window.currentGameState = Utility.GameState.P1_TURN;
                }
                else if (pFirstIndex == pCubeList.Count - 2 || pSecondIndex == pCubeList.Count - 2)
                {
                    System.Console.WriteLine("Player One Wins!");
                    Game.Window.currentGameState = Utility.GameState.P2_TURN;
                }

                while (true)
                {
                    System.Console.WriteLine("\nRestart to try again.\nPlease close the application to try again.");
                    System.Console.ReadKey();
                }
                #endregion
            }
        }

        /// <summary>
        /// Removes velocity and force from the specific index in the list.
        /// </summary>
        /// <param name="pCubeList">The list to search for the specified cube index.</param>
        /// <param name="pCubeIndex">The cube index to search pCubeList for.</param>
        private static void StopCube(ref List<Cube> pCubeList, int pCubeIndex)
        {
            pCubeList[pCubeIndex].Velocity = Vector3.Zero;
            pCubeList[pCubeIndex].Force = Vector3.Zero;
        }
    }
}
