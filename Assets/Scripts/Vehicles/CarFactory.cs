using UnityEngine;
using TurboLoop.Core;

namespace TurboLoop.Vehicles
{
    /// <summary>Builds a good-looking car out of primitives: low body, cabin, stripe, spoiler, lights and four wheels.</summary>
    public static class CarFactory
    {
        public static CarController Create(string name, Color paint, Vector3 position, Quaternion rotation, bool isPlayer, Transform parent)
        {
            var root = new GameObject("Car " + name);
            root.transform.SetParent(parent, true);
            root.transform.SetPositionAndRotation(position, rotation);

            var rb = root.AddComponent<Rigidbody>();
            rb.mass = 1200f; rb.drag = 0.02f; rb.angularDrag = 4f;
            rb.centerOfMass = new Vector3(0f, -0.2f, 0f);
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            var col = root.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.55f, 0f);
            col.size = new Vector3(1.9f, 0.9f, 4.3f);
            var pm = new PhysicMaterial("CarBody") { dynamicFriction = 0.15f, staticFriction = 0.15f, bounciness = 0.05f, frictionCombine = PhysicMaterialCombine.Minimum, bounceCombine = PhysicMaterialCombine.Minimum };
            col.material = pm;

            var car = root.AddComponent<CarController>();
            car.DriverName = name; car.IsPlayer = isPlayer; car.PaintColor = paint;

            var bodyMat = Materials.Get(paint, 0.35f, 0.75f);
            var darkMat = Materials.Get(new Color(0.08f, 0.08f, 0.1f), 0.2f, 0.5f);
            var glassMat = Materials.Get(new Color(0.15f, 0.2f, 0.3f), 0.6f, 0.95f);
            var stripeMat = Materials.Get(Color.Lerp(paint, Color.white, 0.85f), 0.2f, 0.6f);
            var tyreMat = Materials.Get(new Color(0.06f, 0.06f, 0.06f), 0f, 0.3f);
            var hubMat = Materials.Get(new Color(0.75f, 0.75f, 0.78f), 0.8f, 0.8f);

            var visual = new GameObject("BodyVisual").transform;
            visual.SetParent(root.transform, false);
            car.BodyVisual = visual;
            Part(visual, PrimitiveType.Cube, new Vector3(0f, 0.55f, 0f), new Vector3(1.9f, 0.42f, 4.3f), bodyMat, "Body");
            Part(visual, PrimitiveType.Cube, new Vector3(0f, 0.82f, -0.35f), new Vector3(1.55f, 0.24f, 2.3f), bodyMat, "Upper");
            Part(visual, PrimitiveType.Cube, new Vector3(0f, 1.02f, -0.45f), new Vector3(1.35f, 0.34f, 1.7f), glassMat, "Cabin");
            Part(visual, PrimitiveType.Cube, new Vector3(0f, 0.77f, 0.6f), new Vector3(0.5f, 0.03f, 3.1f), stripeMat, "Stripe");
            Part(visual, PrimitiveType.Cube, new Vector3(0f, 0.36f, 0f), new Vector3(2.0f, 0.12f, 4.35f), darkMat, "Skirt");
            Part(visual, PrimitiveType.Cube, new Vector3(0f, 1.05f, -2.0f), new Vector3(1.8f, 0.06f, 0.42f), darkMat, "Spoiler");
            Part(visual, PrimitiveType.Cube, new Vector3(-0.6f, 0.88f, -2.0f), new Vector3(0.08f, 0.3f, 0.25f), darkMat, "Strut");
            Part(visual, PrimitiveType.Cube, new Vector3(0.6f, 0.88f, -2.0f), new Vector3(0.08f, 0.3f, 0.25f), darkMat, "Strut");
            Part(visual, PrimitiveType.Cube, new Vector3(0f, 0.5f, 2.18f), new Vector3(1.7f, 0.18f, 0.12f), darkMat, "Grille");
            var headMat = Materials.Emissive(new Color(1f, 0.95f, 0.8f));
            var tailMat = Materials.Emissive(new Color(1f, 0.15f, 0.1f));
            Part(visual, PrimitiveType.Cube, new Vector3(-0.65f, 0.62f, 2.16f), new Vector3(0.35f, 0.12f, 0.08f), headMat, "Headlight");
            Part(visual, PrimitiveType.Cube, new Vector3(0.65f, 0.62f, 2.16f), new Vector3(0.35f, 0.12f, 0.08f), headMat, "Headlight");
            Part(visual, PrimitiveType.Cube, new Vector3(-0.6f, 0.62f, -2.16f), new Vector3(0.45f, 0.1f, 0.08f), tailMat, "Taillight");
            Part(visual, PrimitiveType.Cube, new Vector3(0.6f, 0.62f, -2.16f), new Vector3(0.45f, 0.1f, 0.08f), tailMat, "Taillight");

            Vector3[] wheelPos = { new Vector3(-0.88f, 0.35f, 1.35f), new Vector3(0.88f, 0.35f, 1.35f), new Vector3(-0.88f, 0.35f, -1.4f), new Vector3(0.88f, 0.35f, -1.4f) };
            for (int i = 0; i < 4; i++)
            {
                var pivot = new GameObject("Wheel" + i).transform;
                pivot.SetParent(root.transform, false);
                pivot.localPosition = wheelPos[i];
                var spinner = new GameObject("Spinner").transform;
                spinner.SetParent(pivot, false);
                spinner.localRotation = Quaternion.Euler(0f, 0f, 90f);
                Part(spinner, PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.7f, 0.14f, 0.7f), tyreMat, "Tyre");
                Part(spinner, PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.42f, 0.15f, 0.42f), hubMat, "Hub");
                car.WheelPivots[i] = pivot;
                car.WheelSpinners[i] = spinner;
            }
            return car;
        }

        private static GameObject Part(Transform parent, PrimitiveType type, Vector3 localPos, Vector3 scale, Material mat, string name)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }
    }
}
