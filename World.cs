using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using TestingTaskFramework;
using VRageMath;

namespace TestingTask
{
    // TODO: World is really slow now, optimize it.
    // TODO: Fix excessive allocations during run.
    // TODO: Write body of 'PreciseCollision' method.
    class World : IWorld
    {
        /// <summary>
        /// [added] save the list of objects in each area box separately, and hash them to find easily with position.
        /// </summary>
        private Dictionary<Int32, List<WorldObject>> m_objects = new Dictionary<Int32, List<WorldObject>>();

        /// <summary>
        /// [added] save the objects which need update while adding and removing them
        /// </summary>
        private List<WorldObject> objectsToUpdate = new List<WorldObject>();    

        /// <summary>
        /// [added] indicate the area size in which objects are saved separately.
        /// </summary>
        private float scale = 30.0f;

        /// <summary>
        /// [added] save the last queried BoundingBox to use when update
        /// </summary>
        private BoundingBox lastQueriedBox = default(BoundingBox);

        private const int ROW_COL_CNT = 10000;

        /// <summary>
        /// Time of the world, increased with each update.
        /// </summary>
        public TimeSpan Time { get; private set; }

        /// <summary>
        /// [added] get hash key of the list of objects in the area nearest by "obj"
        /// </summary>
        private Int32 getKeyOfObject(WorldObject obj)
        {
            int col = (int)(obj.Position.X / scale);
            int row = (int)(obj.Position.Z / scale);
            return row * ROW_COL_CNT + col;
        }

        /// <summary>
        /// Adds new object into world.
        /// World is responsible for calling OnAdded method on object when object is added.
        /// </summary>
        public void Add(WorldObject obj)
        {
            obj.OnAdded(this);

            Int32 key = getKeyOfObject(obj);
            if (m_objects.ContainsKey(key))
            {
                List<WorldObject> objs = m_objects[key];
                objs.Add(obj);
            }
            else
            {
                List<WorldObject> objs = new List<WorldObject>();
                objs.Add(obj);
                m_objects[key] = objs;
            }

            if (obj.NeedsUpdate)
                objectsToUpdate.Add(obj);
        }

        /// <summary>
        /// Removes object from world.
        /// World is responsible for calling OnRemoved method on object when object is removed.
        /// </summary>
        public void Remove(WorldObject obj)
        {
            Int32 key = getKeyOfObject(obj);
            if (m_objects.ContainsKey(key))
            {
                List<WorldObject> objs = m_objects[key];
                objs.Remove(obj);
            }

            objectsToUpdate.Remove(obj);
            obj.OnRemoved();
        }

        /// <summary>
        /// Called when object is moved in the world.
        /// </summary>
        public void OnObjectMoved(WorldObject obj, Vector3 displacement)
        {
        }

        /// <summary>
        /// Clears whole world and resets the time.
        /// </summary>
        public void Clear()
        {
            Time = TimeSpan.Zero;
            m_objects.Clear();
        }

        /// <summary>
        /// Queries the world for objects in a box. Matching objects are added into result list.
        /// Query should return all overlapping objects.
        /// </summary>
        public void Query(BoundingBox box, List<WorldObject> resultList)
        {
            if (box.Center.Y == 0)
                lastQueriedBox = box;

            int from_col = (int)(box.Min.X / scale) - 1;
            int from_row = (int)(box.Min.Z / scale) - 1;
            int last_col = (int)(box.Max.X / scale) + 1;
            int last_row = (int)(box.Max.Z / scale) + 1;
            for (int i = from_col; i <= last_col; i++)
            {
                for (int j = from_row; j <= last_row; j++)
                {
                    Int32 key = j * ROW_COL_CNT + i;

                    if (!m_objects.ContainsKey(key))
                        continue;

                    foreach (var obj in m_objects[key])
                    {
                        if (obj.BoundingBox.Contains(box) != ContainmentType.Disjoint)
                            resultList.Add(obj);
                    }
                }
            }
        }

        /// <summary>
        /// Updates the world in following order:
        /// 1. Increase time.
        /// 2. Call Update on all objects with NeedsUpdate flag.
        /// 3. Call PostUpdate on all objects with NeedsUpdate flag.
        /// PostUpdate on first object must be called when all other objects are Updated.
        /// </summary>
        public void Update(TimeSpan deltaTime)
        {
            Time += deltaTime;

            for (int i = 0; i < objectsToUpdate.Count; i++)
            {
                var obj = objectsToUpdate[i];
                if (obj.BoundingBox.Contains(lastQueriedBox) == ContainmentType.Disjoint)
                    continue;

                if (obj.NeedsUpdate)
                {
                    Int32 old_key = getKeyOfObject(obj);
                    obj.Update(deltaTime);
                    Int32 new_key = getKeyOfObject(obj);

                    if (!old_key.Equals(new_key))
                    {
                        m_objects[old_key].Remove(obj);
                        if (!m_objects.ContainsKey(new_key))
                        {
                            m_objects[new_key] = new List<WorldObject>();
                        }
                        m_objects[new_key].Add(obj);
                    }
                }
            }

            for (int i = 0; i < objectsToUpdate.Count; i++)
            {
                var obj = objectsToUpdate[i];
                if (obj.BoundingBox.Contains(lastQueriedBox) == ContainmentType.Disjoint)
                    continue;

                if (obj.NeedsUpdate)
                    obj.PostUpdate();
            }

        }

        /// <summary>
        /// Calculates precise collision of two moving objects.
        /// Returns exact delta time of touch (e.g. 1 is one second in future from now).
        /// When objects are already touching or overlapping, returns zero. When the objects won't ever touch, returns positive infinity.
        /// </summary>
        public float PreciseCollision(WorldObject a, WorldObject b)
        {
            Vector3 velocity1 = a.LinearVelocity;
            Vector3 velocity2 = b.LinearVelocity;

            Vector3 v = a.LinearVelocity - b.LinearVelocity;
            Vector3 s = a.Position - b.Position;

            double r = a.BoundingRadius + b.BoundingRadius;

            double c = Vector3.Dot(s, s) - r * r;
            if (c < 0)
                return 0;

            double a1 = Vector3.Dot(v, v);
            double b1 = Vector3.Dot(v, s);
            if (b1 >= 0)
                return float.PositiveInfinity;

            double d = b1 * b1 - a1 * c;
            if (d < 0)
                return float.PositiveInfinity;

            return (float) ((-b1 - Math.Sqrt(d)) / a1);
        }
    }
}
