using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using TestingTaskFramework;
using VRageMath;

namespace TestingTask
{
    // TODO: Modify 'OnUpdate' method, find asteroids in World (property Ship.World) and shoot them.
    class ShipBehavior : IShipBehavior
    {
        /// <summary>
        /// The ship which has this behavior.
        /// </summary>
        public Ship Ship { get; set; }

        /// <summary>
        /// [added] The last shot object, this is to prevent to shoot same obejct more than one times.
        /// </summary>
        private WorldObject lastShotObject = null;

        double AimAhead(Vector3 delta, Vector3 vr, float muzzleV)
        {
            //delta: relative position
            //vr, relative velocity
            //muzzleV: Speed of the bullet


            double a = Vector3.Dot(vr, vr) - muzzleV * muzzleV;
            double b = 2f *  Vector3.Dot(vr, delta);
            double c =  Vector3.Dot(delta, delta);

            double desc = b * b - 4f * a * c;

            // If the discriminant is negative, then there is no solution
            if (desc > 0)
            {
                return (2 * c / (Math.Sqrt(desc) - b));
            }
            else
            {
                return -1;
            }
        }


        /// <summary>
        /// [updated] Called when ship is being updated, Ship property is never null when OnUpdate is called.
        /// </summary>
        public void OnUpdate()
        {
            // check if ship can shoot 
            if (!Ship.CanShoot)
                return;

            List<WorldObject> m_res = new List<WorldObject>();

            // check the objects only limited box area around ship
            float box_size = 60;
            BoundingBox box = new BoundingBox(
                new Vector3(Ship.Position.X - box_size / 2, Ship.Position.Y - box_size / 2, Ship.Position.Z - box_size / 2),
                new Vector3(Ship.Position.X + box_size / 2, Ship.Position.Y + box_size / 2, Ship.Position.Z + box_size / 2)
                );
            Ship.World.Query(box, m_res);

            int count = 0;

            WorldObject aimObject = null;
            float minDistance = float.PositiveInfinity;

            WorldObject moveObject = null;
            float moveMinDistance = float.PositiveInfinity;

            float min_delta_time = 0;

            foreach (var astroid in m_res)
            {
                if (!(astroid is Asteroid))
                    continue;

                if (astroid == lastShotObject)
                    continue;

                Vector3 delta = astroid.Position - Ship.Position;
                Vector3 vr = astroid.LinearVelocity - Ship.LinearVelocity;

                float distance = Vector3.Distance(astroid.Position, Ship.Position);

                // Calculate the time a bullet will collide
                // if it's possible to hit the target.
                double deltaTime = AimAhead(delta, vr, Ship.GunInfo.ProjectileSpeed);

                // If the time is negative, then we didn't get a solution.
                if (deltaTime < 0f || deltaTime > Ship.GunInfo.ProjectileLifetime.TotalSeconds)
                    continue;

                count++;

                if (distance < minDistance)
                {
                    // get the object nearest by the ship
                    distance = minDistance;
                    aimObject = astroid;
                    min_delta_time = (float)deltaTime;
                }

                if (Vector3.Distance(new Vector3(0, 0, 0), astroid.LinearVelocity) != 0)
                {
                    // if there is a moving astroid, select it first
                    if (moveMinDistance < distance)
                    {
                        moveObject = astroid;
                        moveMinDistance = distance;
                        min_delta_time = (float)deltaTime;
                    }
                }
            }

            if (count <= 0)
                return;

            // save the last shot object
            lastShotObject = aimObject;

            // Aim at the point where the target will be at the time of the collision.
            Vector3 aimPoint = aimObject.Position - Ship.Position + aimObject.LinearVelocity * min_delta_time - Ship.LinearVelocity * min_delta_time;
            Ship.Shoot(Vector3.Normalize(aimPoint));
        }
    }
}
