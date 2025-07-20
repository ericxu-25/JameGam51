using UnityEngine;
using System.Reflection;
using System.Collections.Generic;
using System.Collections;

namespace Globals
{
    [System.Serializable]
    public struct PowerScalerProperty {
        public PowerScalerProperty(bool enabled, AnimationCurve scaler = null) {
            this.enabled = enabled;
            this.scaling = scaler == null ? scaler : new AnimationCurve(new Keyframe(0, 1));
        }
        [Tooltip("Whether the scaler is enabled or not")]
        public bool enabled;
        public AnimationCurve scaling;
    }
    public class ListHelpers { 
        /// <summary>
        /// Returns a random choice from a list
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        public static T RandomFromList<T>(List<T> list) {
            if (list.Count <= 0) return default(T);
            return list[Random.Range(0, list.Count)];
        }

        /// <summary>
        /// Returns a random choice from an array within bounds. The max index is exclusive
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="minIndex"></param>
        /// <param name="maxIndex"></param>
        /// <returns></returns>
        public static T RandomFromList<T>(List<T> list, int minIndex, int maxIndex) { 
            if (list.Count<= minIndex) return default(T);
            return list[Random.Range(minIndex, maxIndex)];
        }

        /// <summary>
        /// Returns a random choice from an array 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        public static T RandomFromArray<T>(T[] list) { 
            if (list.Length<= 0) return default(T);
            return list[Random.Range(0, list.Length)];
        }

        /// <summary>
        /// Returns a random choice from an array within bounds. The max index is exclusive
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="minIndex"></param>
        /// <param name="maxIndex"></param>
        /// <returns></returns>
        public static T RandomFromArray<T>(T[] list, int minIndex, int maxIndex) { 
            if (list.Length <= minIndex) return default(T);
            return list[Random.Range(minIndex, maxIndex)];
        }

        public delegate int GetWeight<T>(T item);

        /// <summary>
        /// Returns a weighted random choice from an array. Accepts an optional totalWeight value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="weightFunction"></param>
        /// <param name="totalWeight"></param>
        /// <returns></returns>
        public static T WeightedRandomFromList<T>(T[] list, GetWeight<T> weightFunction, int totalWeight = 0) {
            if (list.Length <= 0) return default(T);
            if (totalWeight == 0)
            {
                for (int i = 0; i < list.Length; ++i)
                {
                    totalWeight += Mathf.Abs(weightFunction(list[i]));
                }
            }
            if (totalWeight == 0) {
                return default(T);
            }
            int choice = Mathf.FloorToInt(Random.value * totalWeight);
            int currentWeight = 0;
            for (int i = 0; i < list.Length; ++i) {
                currentWeight += Mathf.Abs(weightFunction(list[i]));
                if (currentWeight >= choice) return list[i];
            }
            return default(T);
        }

        /// <summary>
        /// Returns a weighted random choice from an list. Accepts an optional totalWeight value
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="weightFunction"></param>
        /// <param name="totalWeight"></param>
        /// <returns></returns>
        public static T WeightedRandomFromList<T>(List<T> list, GetWeight<T> weightFunction, int totalWeight = 0)
        {
            if (list.Count <= 0) return default(T);
            if (totalWeight == 0)
            {
                for (int i = 0; i < list.Count; ++i)
                {
                    totalWeight += Mathf.Abs(weightFunction(list[i]));
                }
            }
            int choice = Mathf.FloorToInt(Random.value * totalWeight);
            int currentWeight = 0;
            for (int i = 0; i < list.Count; ++i) {
                currentWeight += Mathf.Abs(weightFunction(list[i]));
                if (currentWeight >= choice) return list[i];
            }
            return default(T);
        }
    }
    public class PhysicsHelpers
    {
        /// <summary>
        /// Given a jump height and gravity, returns the amount of velocity needed to reach that height
        /// </summary>
        /// <param name="jumpHeight"></param>
        /// <param name="gravity"></param>
        /// <returns></returns>
        public static float JumpHeightToJumpPower(float jumpHeight, float gravity)
        {
            return Mathf.Sqrt(2 * gravity * jumpHeight);
        }

        /// <summary>
        /// Given a height and time, returns the amount of jump velocity needed to reach that height by that distance
        /// </summary>
        /// <param name="jumpHeight"></param>
        /// <param name="time"></param>
        /// <param name="gravity"></param>
        /// <returns></returns>
        public static float JumpHeightAndTimeToJumpPower(float jumpHeight, float time, float gravity)
        {
            return gravity * time + 2 * (jumpHeight) / time;
        }

        /// <summary>
        /// Given jump power and gravity, returns the amount of time needed to reach a certain height
        /// </summary>
        /// <param name="jumpPower"></param>
        /// <param name="gravity"></param>
        /// <returns></returns>
        public static float JumpPowerAndJumpHeightToTime(float jumpPower, float jumpHeight, float gravity)
        {
            if (jumpHeight == 0f || jumpPower == 0f) return Mathf.Infinity;
            if (jumpPower < JumpHeightToJumpPower(jumpHeight, gravity)) return Mathf.Infinity;
            if (gravity == 0f) return jumpHeight / jumpPower;
            // returns solution towards the right
            return (-jumpPower + Mathf.Sqrt(jumpPower * jumpPower - 2 * gravity * jumpHeight)) / -gravity;
        }

        /// <summary>
        /// Calculates the collision impulse to separate the source from a collider 
        /// given their relative velocity and normal. Doesn't take into account angular forces.
        /// </summary>
        /// <param name="sourceCollider"></param>
        /// <param name="collider"></param>
        /// <param name="normal"></param>
        /// <param name="relativeVelocity"></param>
        /// <param name="NormalImpulse"></param>
        /// <param name="TangentImpulse"></param>
        public static void CalculateCollisionImpulse(Rigidbody2D sourceCollider, Collider2D collider, Vector2 normal, Vector2 relativeVelocity, out Vector2 NormalImpulse, out Vector2 TangentImpulse) { 
            Helpers.TouchingPhysicsMaterial2DValues(sourceCollider.sharedMaterial, collider.sharedMaterial, out float friction, out float bounciness);
            float normalForce = Mathf.Max(Vector2.Dot(relativeVelocity, -normal), 0f);
            float tangentForce = Vector2.Dot(relativeVelocity, Vector2.Perpendicular(normal));
            Vector2 frictionForce = Mathf.Min(friction * normalForce, Mathf.Abs(tangentForce)) * -Mathf.Sign(tangentForce) * Vector2.Perpendicular(normal);
            TangentImpulse = frictionForce;
            Vector2 bounceForce = bounciness * normalForce * normal;
            NormalImpulse =  normalForce * normal + bounceForce;
        }
 

        /// <summary>
        /// Given a valid raycast hit with another collider, manually calculates the collision impulse used to separate the source from the collider 
        /// this doesn't take into account angular forces but does take into account the physics material
        /// </summary>
        /// <param name="sourceCollider"></param>
        /// <param name="collisionCast"></param>
        /// <param name="relativeVelocity"> relative velocity between the sourceCollider and the collision </param>
        /// <param name="NormalImpulse">change in velocity normal to the collider we hit's normal</param>
        /// <param name="TangentImpulse">change in velocity tangent to the collider we hit's normal</param>
        public static void CalculateCollisionImpulse(Rigidbody2D sourceCollider, RaycastHit2D collisionCast, Vector2 relativeVelocity, out Vector2 NormalImpulse, out Vector2 TangentImpulse) {
            CalculateCollisionImpulse(sourceCollider, collisionCast.collider, collisionCast.normal, relativeVelocity, out NormalImpulse, out TangentImpulse);
        }

        public static void CalculateCollisionImpulse(Rigidbody2D sourceCollider, RaycastHit2D collisionCast, out Vector2 NormalImpulse, out Vector2 TangentImpulse) {
            Vector2 relativeVelocity = sourceCollider.linearVelocity;
            if (collisionCast.collider.attachedRigidbody) {
                relativeVelocity -= collisionCast.collider.attachedRigidbody.linearVelocity;
            }
            CalculateCollisionImpulse(sourceCollider, collisionCast, relativeVelocity, out NormalImpulse, out TangentImpulse);
        }


    }
    public class Helpers
    {
        // reference: https://discussions.unity.com/t/copy-a-component-at-runtime/71172
        // https://discussions.unity.com/t/how-to-get-a-component-from-an-object-and-add-it-to-another-copy-components-at-runtime/80939/4
        public static T CopyComponent<T>(T original, GameObject destination) where T : Component
        {
            System.Type type = original.GetType();
            T copy = destination.GetComponent(type) as T;
            if (!copy) copy = destination.AddComponent(type) as T;
            // Reference: https://learn.microsoft.com/en-us/dotnet/api/system.type.findmembers
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Default | BindingFlags.DeclaredOnly;
            FieldInfo[] fields = type.GetFields(flags);
            foreach (FieldInfo field in fields)
            {
                field.SetValue(copy, field.GetValue(original));
            }

            PropertyInfo[] properties = type.GetProperties();
            foreach (PropertyInfo property in properties)
            {
                if (!property.IsDefined(typeof(System.ObsoleteAttribute), true) && property.CanWrite)
                {
                    try
                    {
                        property.SetValue(copy, property.GetValue(original));
                    }
                    catch
                    {
                        //noop
                    }
                }
            }
            return copy as T;
        }

        // the automatic version works okay... but has side effects like changing the game object name
        public static ConfigurableJoint CopyConfigurableJoint(ConfigurableJoint original, GameObject destination)
        {
            ConfigurableJoint joint;
            if (!destination.TryGetComponent<ConfigurableJoint>(out joint))
            {
                joint = destination.AddComponent<ConfigurableJoint>();
            }
            joint.angularXDrive = original.angularXDrive;
            joint.angularXDrive = original.angularXDrive;
            joint.angularXLimitSpring = original.angularXLimitSpring;
            joint.angularXMotion = original.angularXMotion;
            joint.angularYLimit = original.angularYLimit;
            joint.angularYMotion = original.angularYMotion;
            joint.angularYZDrive = original.angularYZDrive;
            joint.angularYZLimitSpring = original.angularYZLimitSpring;
            joint.angularZLimit = original.angularZLimit;
            joint.angularZMotion = original.angularZMotion;
            joint.configuredInWorldSpace = original.configuredInWorldSpace;
            joint.highAngularXLimit = original.highAngularXLimit;
            joint.linearLimit = original.linearLimit;
            joint.linearLimitSpring = original.linearLimitSpring;
            joint.lowAngularXLimit = original.lowAngularXLimit;
            joint.projectionAngle = original.projectionAngle;
            joint.projectionDistance = original.projectionDistance;
            joint.projectionMode = original.projectionMode;
            joint.rotationDriveMode = original.rotationDriveMode;
            joint.secondaryAxis = original.secondaryAxis;
            joint.slerpDrive = original.slerpDrive;
            joint.swapBodies = original.swapBodies;
            joint.targetAngularVelocity = original.targetAngularVelocity;
            joint.targetPosition = original.targetPosition;
            joint.targetRotation = original.targetRotation;
            joint.targetVelocity = original.targetVelocity;
            joint.xDrive = original.xDrive;
            joint.xMotion = original.xMotion;
            joint.yDrive = original.yDrive;
            joint.yMotion = original.yMotion;
            joint.zDrive = original.zDrive;
            joint.zMotion = original.zMotion;
            joint.anchor = original.anchor;
            joint.autoConfigureConnectedAnchor = original.autoConfigureConnectedAnchor;
            joint.axis = original.axis;
            joint.breakForce = original.breakForce;
            joint.breakTorque = original.breakTorque;
            joint.connectedAnchor = original.connectedAnchor;
            joint.connectedArticulationBody = original.connectedArticulationBody;
            joint.connectedBody = original.connectedBody;
            joint.connectedMassScale = original.connectedMassScale;
            joint.enableCollision = original.enableCollision;
            joint.enablePreprocessing = original.enablePreprocessing;
            joint.massScale = original.massScale;
            return joint;
        }

        public static Vector3 ReciprocalVector3(Vector3 value) {
            return new Vector3(Mathf.Approximately(value.x, 0) ? 0f : 1f / value.x,
                Mathf.Approximately(value.y, 0) ? 0f : 1f / value.x,
                Mathf.Approximately(value.z, 0) ? 0f : 1f / value.x);
        }

        public static Vector2 ProjectVector2OnLine(Vector2 line, Vector2 vector) {
            return Vector2.Dot(vector, line.normalized) * line.normalized;
        }

        public static Vector2 Vec3ToVec2(Vector3 value) {
            return new Vector2(value.x, value.y);
        }

        private static readonly PhysicsMaterial2D defaultPhysicsMaterial = new PhysicsMaterial2D();
        /// <summary>
        /// Determines the friction and bounce coefficients of two touching materials. Uses the combine settings of the second material.
        /// </summary>
        /// <param name="mat1"></param>
        /// <param name="mat2"></param>
        /// <param name="friction"></param>
        /// <param name="bounciness"></param>
        public static void TouchingPhysicsMaterial2DValues(PhysicsMaterial2D mat1, PhysicsMaterial2D mat2, out float friction, out float bounciness) {
            if (!mat1) mat1 = defaultPhysicsMaterial;
            if (!mat2) mat2 = defaultPhysicsMaterial;
            switch (mat2.bounceCombine)
            {
                default:
                case PhysicsMaterialCombine2D.Average:
                    bounciness = (mat1.bounciness + mat2.bounciness) / 2f;
                    break;
                case PhysicsMaterialCombine2D.Mean:
                    bounciness = Mathf.Sqrt(mat1.bounciness * mat2.bounciness);
                    break;
                case PhysicsMaterialCombine2D.Multiply:
                    bounciness = mat1.bounciness * mat2.bounciness;
                    break;
                case PhysicsMaterialCombine2D.Minimum:
                    bounciness = Mathf.Min(mat1.bounciness, mat2.bounciness);
                    break;
                case PhysicsMaterialCombine2D.Maximum:
                    bounciness = Mathf.Max(mat1.bounciness, mat2.bounciness);
                    break;
            }
            switch (mat2.frictionCombine)
            {
                default:
                case PhysicsMaterialCombine2D.Average:
                    friction = (mat1.friction + mat2.friction) / 2f;
                    break;
                case PhysicsMaterialCombine2D.Mean:
                    friction = Mathf.Sqrt(mat1.friction * mat2.friction);
                    break;
                case PhysicsMaterialCombine2D.Multiply:
                    friction = mat1.friction * mat2.friction;
                    break;
                case PhysicsMaterialCombine2D.Minimum:
                    friction = Mathf.Min(mat1.friction, mat2.friction);
                    break;
                case PhysicsMaterialCombine2D.Maximum:
                    friction = Mathf.Max(mat1.friction, mat2.friction);
                    break;
            }
        }

        /// <summary>
        /// Converts a degree reading to a Vector 2, with 0 degrees being <1, 0> and 90 being <0, 1>
        /// </summary>
        /// <param name="degrees"></param>
        /// <returns></returns>
        public static Vector2 RadToVector2(float degrees) {
            return new Vector2(Mathf.Cos(degrees * Mathf.Deg2Rad), Mathf.Sin(degrees * Mathf.Deg2Rad));
        }
        public static Vector2 RotateVector2(Vector2 vector, float degrees) {
            float sin = Mathf.Sin(degrees * Mathf.Deg2Rad);
            float cos  = Mathf.Cos(degrees * Mathf.Deg2Rad);
            return new Vector2(vector.x * cos - vector.y * sin, vector.y * cos + vector.x * sin);
        }

        public static void DebugLog(string output, bool debug) {
            if (!debug) return;
            Debug.Log(output);
        }

        public static void DebugWarning(string output, bool debug) {
            if (!debug) return;
            Debug.LogWarning(output);
        }

        public static void DebugError(string output, bool debug) {
            if (!debug) return;
            Debug.LogError(output);
        }
    }
    public class ShortestDistanceRaycastHit : Comparer<RaycastHit>
    {
        public override int Compare(RaycastHit h1, RaycastHit h2)
        {
            return (int)(h1.distance - h2.distance);
        }
    }

    public class ShortestDistanceRaycastHit2D : IComparer<RaycastHit2D>
    {
        public int Compare(RaycastHit2D h1, RaycastHit2D h2)
        {
            return (int)(h1.distance - h2.distance);
        }
    }
}
