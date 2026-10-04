using UnityEngine;

namespace Playble.EchoLoop
{
    /// <summary>
    /// Small helpers for assembling the blocky world out of primitives at runtime.
    /// Every collider is a box so level geometry stays cheap and trivially readable.
    /// </summary>
    public static class Primitives
    {
        public static GameObject Box(Transform parent, string name, Vector3 center, Vector3 size, Material mat, bool collider = true)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            go.transform.localScale = size;

            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = UnitCube();

            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            mr.receiveShadows = true;

            if (collider)
            {
                var box = go.AddComponent<BoxCollider>();
                box.size = Vector3.one;
            }

            return go;
        }

        public static GameObject Sphere(Transform parent, string name, Vector3 center, float diameter, Material mat, bool collider = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            go.transform.localScale = Vector3.one * diameter;

            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = UnitSphere();

            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;

            if (collider)
            {
                var sc = go.AddComponent<SphereCollider>();
                sc.radius = 0.5f;
            }

            return go;
        }

        public static GameObject Capsule(Transform parent, string name, Vector3 center, float height, float radius, Material mat, bool collider = true)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            go.transform.localScale = new Vector3(radius * 2f, height, radius * 2f);

            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = UnitCapsule();

            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.receiveShadows = true;

            if (collider)
            {
                var cc = go.AddComponent<CapsuleCollider>();
                cc.height = 2f;   // scaled by transform -> desired world height
                cc.radius = 0.5f; // scaled by transform -> desired world radius
                cc.direction = 1;
            }

            return go;
        }

        static Mesh _cube, _sphere, _capsule;

        public static Mesh UnitCube()
        {
            if (_cube == null)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                _cube = go.GetComponent<MeshFilter>().sharedMesh;
                Object.Destroy(go);
            }
            return _cube;
        }

        public static Mesh UnitSphere()
        {
            if (_sphere == null)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                _sphere = go.GetComponent<MeshFilter>().sharedMesh;
                Object.Destroy(go);
            }
            return _sphere;
        }

        public static Mesh UnitCapsule()
        {
            if (_capsule == null)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                _capsule = go.GetComponent<MeshFilter>().sharedMesh;
                Object.Destroy(go);
            }
            return _capsule;
        }
    }
}