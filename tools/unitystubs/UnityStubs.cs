// Minimal, compile-only stub of the UnityEngine API surface used by TURBO LOOP. Bodies are empty.
#pragma warning disable CS0067, CS0649, CS0169, CS8632, CS0108, CS0114
using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEngine
{
    public class Object { public string name { get; set; } public static void Destroy(Object o) { } public static void Destroy(Object o, float t) { } public static void DontDestroyOnLoad(Object o) { } public static T FindObjectOfType<T>() where T : Object => default; public static implicit operator bool(Object o) => o != null; }
    public class Component : Object { public GameObject gameObject { get; } = new GameObject("stub"); public Transform transform => gameObject.transform; public T GetComponent<T>() => default; public T GetComponentInParent<T>() => default; public T GetComponentInChildren<T>() => default; public T[] GetComponents<T>() => new T[0]; }
    public class Behaviour : Component { public bool enabled { get; set; } }
    public class Coroutine { }
    public class MonoBehaviour : Behaviour { public Coroutine StartCoroutine(IEnumerator e) => null; }
    public enum PrimitiveType { Sphere, Capsule, Cylinder, Cube, Plane, Quad }
    public class GameObject : Object
    {
        public GameObject() { }
        public GameObject(string name, params Type[] components) { this.name = name; }
        public Transform transform { get; } = null;
        public string tag { get; set; }
        public bool activeSelf { get; }
        public bool activeInHierarchy { get; }
        public void SetActive(bool v) { }
        public T AddComponent<T>() where T : Component => default;
        public T GetComponent<T>() => default;
        public T GetComponentInParent<T>() => default;
        public T GetComponentInChildren<T>() => default;
        public static GameObject CreatePrimitive(PrimitiveType t) => new GameObject();
    }
    public class Transform : Component
    {
        public Vector3 position { get; set; } public Quaternion rotation { get; set; } public Vector3 eulerAngles { get; set; }
        public Vector3 localPosition { get; set; } public Quaternion localRotation { get; set; } public Vector3 localScale { get; set; }
        public Vector3 forward { get; set; } public Vector3 right { get; set; } public Vector3 up { get; set; }
        public Transform parent { get; set; } public int childCount { get; }
        public void SetParent(Transform p, bool worldStays) { } public void SetParent(Transform p) { }
        public void SetPositionAndRotation(Vector3 p, Quaternion r) { }
        public Transform GetChild(int i) => null; public Transform Find(string n) => null; public bool IsChildOf(Transform t) => false; public void SetAsLastSibling() { }
    }
    public class RectTransform : Transform { public Vector2 anchorMin { get; set; } public Vector2 anchorMax { get; set; } public Vector2 offsetMin { get; set; } public Vector2 offsetMax { get; set; } public Vector2 pivot { get; set; } public Vector2 sizeDelta { get; set; } }
    public struct Vector2 { public float x, y; public Vector2(float x, float y) { this.x = x; this.y = y; } public static Vector2 zero => new Vector2(0, 0); public static Vector2 one => new Vector2(1, 1); public static Vector2 operator +(Vector2 a, Vector2 b) => new Vector2(a.x + b.x, a.y + b.y); public static Vector2 operator -(Vector2 a, Vector2 b) => new Vector2(a.x - b.x, a.y - b.y); public static Vector2 operator *(Vector2 a, float d) => new Vector2(a.x * d, a.y * d); }
    public struct Vector3
    {
        public float x, y, z; public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 zero => new Vector3(0, 0, 0); public static Vector3 one => new Vector3(1, 1, 1); public static Vector3 up => new Vector3(0, 1, 0); public static Vector3 down => new Vector3(0, -1, 0); public static Vector3 forward => new Vector3(0, 0, 1); public static Vector3 right => new Vector3(1, 0, 0);
        public float magnitude => (float)Math.Sqrt(x * x + y * y + z * z); public float sqrMagnitude => x * x + y * y + z * z; public Vector3 normalized => this;
        public static Vector3 operator +(Vector3 a, Vector3 b) => new Vector3(a.x + b.x, a.y + b.y, a.z + b.z); public static Vector3 operator -(Vector3 a, Vector3 b) => new Vector3(a.x - b.x, a.y - b.y, a.z - b.z); public static Vector3 operator -(Vector3 a) => new Vector3(-a.x, -a.y, -a.z);
        public static Vector3 operator *(Vector3 a, float d) => new Vector3(a.x * d, a.y * d, a.z * d); public static Vector3 operator *(float d, Vector3 a) => a * d; public static Vector3 operator /(Vector3 a, float d) => new Vector3(a.x / d, a.y / d, a.z / d);
        public static float Dot(Vector3 a, Vector3 b) => 0; public static Vector3 Cross(Vector3 a, Vector3 b) => a; public static Vector3 Lerp(Vector3 a, Vector3 b, float t) => a; public static Vector3 Slerp(Vector3 a, Vector3 b, float t) => a; public static Vector3 ProjectOnPlane(Vector3 v, Vector3 n) => v; public static float Distance(Vector3 a, Vector3 b) => 0;
        public static float SignedAngle(Vector3 a, Vector3 b, Vector3 axis) => 0; public static Vector3 SmoothDamp(Vector3 c, Vector3 t, ref Vector3 v, float s) => c;
    }
    public struct Quaternion { public static Quaternion identity => new Quaternion(); public static Quaternion Euler(float x, float y, float z) => new Quaternion(); public static Quaternion LookRotation(Vector3 f) => new Quaternion(); public static Quaternion LookRotation(Vector3 f, Vector3 up) => new Quaternion(); public static Quaternion Slerp(Quaternion a, Quaternion b, float t) => a; public static Vector3 operator *(Quaternion q, Vector3 v) => v; public static Quaternion operator *(Quaternion a, Quaternion b) => a; }
    public struct Color { public float r, g, b, a; public Color(float r, float g, float b) { this.r = r; this.g = g; this.b = b; a = 1; } public Color(float r, float g, float b, float a) { this.r = r; this.g = g; this.b = b; this.a = a; } public static Color white => new Color(1, 1, 1); public static Color black => new Color(0, 0, 0); public static Color red => new Color(1, 0, 0); public static Color Lerp(Color a, Color b, float t) => a; public static Color operator *(Color a, float d) => new Color(a.r * d, a.g * d, a.b * d, a.a); public static Color HSVToRGB(float h, float s, float v) => white; public override string ToString() => ""; }
    public static class Mathf { public const float PI = 3.14159f; public const float Deg2Rad = 0.0174f; public const float Rad2Deg = 57.29f; public static float Clamp(float v, float a, float b) => v; public static int Clamp(int v, int a, int b) => v; public static float Clamp01(float v) => v; public static float Lerp(float a, float b, float t) => a; public static float MoveTowards(float a, float b, float d) => a; public static float Max(float a, float b) => a; public static int Max(int a, int b) => a; public static float Min(float a, float b) => a; public static int Min(int a, int b) => a; public static float Abs(float v) => v; public static int Abs(int v) => v; public static float Sin(float v) => v; public static float Cos(float v) => v; public static float Sqrt(float v) => v; public static float Floor(float v) => v; public static float Round(float v) => v; public static int RoundToInt(float v) => 0; public static int CeilToInt(float v) => 0; public static float Repeat(float t, float l) => t; public static float Sign(float v) => 1; public static float PerlinNoise(float x, float y) => 0; }
    public static class Time { public static float deltaTime; public static float fixedDeltaTime; public static float unscaledDeltaTime; public static float unscaledTime; public static float time; public static float timeScale; }
    public enum KeyCode { None, Space, Escape, R }
    public static class Input { public static float GetAxis(string n) => 0; public static float GetAxisRaw(string n) => 0; public static bool GetKey(KeyCode k) => false; public static bool GetKeyDown(KeyCode k) => false; }
    public enum QueryTriggerInteraction { UseGlobal, Ignore, Collide }
    public struct RaycastHit { public Collider collider; public float distance; public Vector3 point; }
    public static class Physics
    {
        public static bool Raycast(Vector3 o, Vector3 d, out RaycastHit hit, float max, int mask, QueryTriggerInteraction q) { hit = default; return false; }
        public static bool Raycast(Vector3 o, Vector3 d, float max, int mask, QueryTriggerInteraction q) => false;
        public static bool SphereCast(Vector3 o, float r, Vector3 d, out RaycastHit hit, float max, int mask, QueryTriggerInteraction q) { hit = default; return false; }
    }
    public enum PhysicMaterialCombine { Average, Minimum, Multiply, Maximum }
    public class PhysicMaterial : Object { public PhysicMaterial(string n) { } public float dynamicFriction { get; set; } public float staticFriction { get; set; } public float bounciness { get; set; } public PhysicMaterialCombine frictionCombine { get; set; } public PhysicMaterialCombine bounceCombine { get; set; } }
    public class Collider : Component { public bool isTrigger { get; set; } public PhysicMaterial material { get; set; } }
    public class BoxCollider : Collider { public Vector3 center { get; set; } public Vector3 size { get; set; } }
    public class MeshCollider : Collider { public Mesh sharedMesh { get; set; } }
    public enum ForceMode { Force, Acceleration, Impulse, VelocityChange }
    public enum RigidbodyInterpolation { None, Interpolate, Extrapolate }
    public enum CollisionDetectionMode { Discrete, Continuous, ContinuousDynamic, ContinuousSpeculative }
    [Flags] public enum RigidbodyConstraints { None = 0, FreezeRotationX = 16, FreezeRotationY = 32, FreezeRotationZ = 64 }
    public class Rigidbody : Component { public float mass { get; set; } public float drag { get; set; } public float angularDrag { get; set; } public Vector3 centerOfMass { get; set; } public RigidbodyInterpolation interpolation { get; set; } public CollisionDetectionMode collisionDetectionMode { get; set; } public RigidbodyConstraints constraints { get; set; } public Vector3 velocity { get; set; } public Vector3 angularVelocity { get; set; } public Quaternion rotation { get; set; } public void AddForce(Vector3 f, ForceMode m) { } public void MoveRotation(Quaternion q) { } }
    public class Shader : Object { public static Shader Find(string n) => null; }
    public class Texture : Object { }
    public enum TextureFormat { RGBA32 }
    public enum TextureWrapMode { Repeat, Clamp }
    public enum FilterMode { Point, Bilinear, Trilinear }
    public class Texture2D : Texture { public Texture2D(int w, int h) { } public Texture2D(int w, int h, TextureFormat f, bool mip) { } public TextureWrapMode wrapMode { get; set; } public FilterMode filterMode { get; set; } public void SetPixels(Color[] c) { } public void Apply() { } }
    public class Material : Object { public Material(Shader s) { } public Color color { get; set; } public Texture mainTexture { get; set; } public Vector2 mainTextureScale { get; set; } public void SetFloat(string n, float v) { } public void SetColor(string n, Color c) { } public void EnableKeyword(string k) { } }
    public class Renderer : Component { public Material sharedMaterial { get; set; } public Material material { get; set; } }
    public class MeshRenderer : Renderer { }
    public class ParticleSystemRenderer : Renderer { }
    public class Mesh : Object { public Vector3[] vertices { get; set; } public Vector2[] uv { get; set; } public Vector3[] normals { get; set; } public int[] triangles { get; set; } public Rendering.IndexFormat indexFormat { get; set; } public void RecalculateBounds() { } public void RecalculateNormals() { } }
    public class MeshFilter : Component { public Mesh sharedMesh { get; set; } public Mesh mesh { get; set; } }
    public enum LightType { Spot, Directional, Point }
    public enum LightShadows { None, Hard, Soft }
    public class Light : Behaviour { public LightType type { get; set; } public LightShadows shadows { get; set; } public float shadowStrength { get; set; } public float intensity { get; set; } public Color color { get; set; } public float range { get; set; } }
    public enum CameraClearFlags { Skybox = 1, Color = 2 }
    public class Camera : Behaviour { public static Camera main => null; public float nearClipPlane { get; set; } public float farClipPlane { get; set; } public float fieldOfView { get; set; } public Color backgroundColor { get; set; } public CameraClearFlags clearFlags { get; set; } }
    public class AudioListener : Behaviour { public static bool pause { get; set; } }
    public enum AudioRolloffMode { Logarithmic, Linear, Custom }
    public class AudioClip : Object { public static AudioClip Create(string n, int len, int ch, int rate, bool stream) => new AudioClip(); public bool SetData(float[] d, int off) => true; }
    public class AudioSource : Behaviour { public AudioClip clip { get; set; } public bool loop { get; set; } public bool playOnAwake { get; set; } public float spatialBlend { get; set; } public float minDistance { get; set; } public float maxDistance { get; set; } public AudioRolloffMode rolloffMode { get; set; } public float volume { get; set; } public float pitch { get; set; } public void Play() { } public void Stop() { } }
    public enum TextAnchor { UpperLeft, UpperCenter, UpperRight, MiddleLeft, MiddleCenter, MiddleRight, LowerLeft, LowerCenter, LowerRight }
    public enum TextAlignment { Left, Center, Right }
    public enum FontStyle { Normal, Bold, Italic, BoldAndItalic }
    public enum HorizontalWrapMode { Wrap, Overflow }
    public enum VerticalWrapMode { Truncate, Overflow }
    public class Font : Object { public Material material { get; } }
    public class TextMesh : Component { public string text { get; set; } public int fontSize { get; set; } public float characterSize { get; set; } public TextAnchor anchor { get; set; } public TextAlignment alignment { get; set; } public Color color { get; set; } public Font font { get; set; } public bool richText { get; set; } }
    public static class Resources { public static T GetBuiltinResource<T>(string path) where T : Object => default; }
    public enum FogMode { Linear = 1, Exponential, ExponentialSquared }
    public static class RenderSettings { public static bool fog; public static FogMode fogMode; public static float fogStartDistance; public static float fogEndDistance; public static Color ambientLight; public static Color fogColor; public static Material skybox; public static Light sun; public static Rendering.AmbientMode ambientMode; public static Color ambientSkyColor, ambientEquatorColor, ambientGroundColor; }
    public static class DynamicGI { public static void UpdateEnvironment() { } }
    public static class QualitySettings { public static int vSyncCount { get; set; } }
    public static class Random { public static float value => 0; public static float Range(float a, float b) => a; public static int Range(int a, int b) => a; }
    public static class Application { public static string persistentDataPath => ""; public static int targetFrameRate { get; set; } public static void Quit() { } }
    public enum CursorLockMode { None, Locked, Confined }
    public static class Cursor { public static CursorLockMode lockState { get; set; } public static bool visible { get; set; } }
    public static class Debug { public static void Log(object m) { } public static void LogWarning(object m) { } public static void LogError(object m) { } }
    public enum RuntimeInitializeLoadType { AfterSceneLoad, BeforeSceneLoad }
    public class RuntimeInitializeOnLoadMethodAttribute : Attribute { public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType t) { } }
    public class RequireComponentAttribute : Attribute { public RequireComponentAttribute(Type a) { } public RequireComponentAttribute(Type a, Type b) { } }
    public class HeaderAttribute : Attribute { public HeaderAttribute(string h) { } }
    public enum RenderMode { ScreenSpaceOverlay, ScreenSpaceCamera, WorldSpace }
    public class Canvas : Behaviour { public RenderMode renderMode { get; set; } public int sortingOrder { get; set; } }
    public class RectOffset { public RectOffset(int l, int r, int t, int b) { } }
    public struct Keyframe { }
    public class AnimationCurve { public static AnimationCurve Linear(float a, float b, float c, float d) => new AnimationCurve(); }
    public struct GradientColorKey { public GradientColorKey(Color c, float t) { } }
    public struct GradientAlphaKey { public GradientAlphaKey(float a, float t) { } }
    public class Gradient { public void SetKeys(GradientColorKey[] c, GradientAlphaKey[] a) { } }
    public enum ParticleSystemSimulationSpace { Local, World }
    public enum ParticleSystemShapeType { Sphere, Cone, Box }
    public class ParticleSystem : Component
    {
        public struct MinMaxCurve { public MinMaxCurve(float a) { } public MinMaxCurve(float a, float b) { } public MinMaxCurve(float m, AnimationCurve c) { } public static implicit operator MinMaxCurve(float f) => new MinMaxCurve(f); }
        public struct MinMaxGradient { public static implicit operator MinMaxGradient(Color c) => new MinMaxGradient(); public static implicit operator MinMaxGradient(Gradient g) => new MinMaxGradient(); }
        public struct MainModule { public MinMaxCurve startLifetime { get; set; } public MinMaxCurve startSpeed { get; set; } public MinMaxCurve startSize { get; set; } public MinMaxGradient startColor { get; set; } public ParticleSystemSimulationSpace simulationSpace { get; set; } public int maxParticles { get; set; } public MinMaxCurve gravityModifier { get; set; } }
        public struct EmissionModule { public MinMaxCurve rateOverTime { get; set; } }
        public struct ShapeModule { public ParticleSystemShapeType shapeType { get; set; } public float radius { get; set; } }
        public struct ColorOverLifetimeModule { public bool enabled { get; set; } public MinMaxGradient color { get; set; } }
        public struct SizeOverLifetimeModule { public bool enabled { get; set; } public MinMaxCurve size { get; set; } }
        public MainModule main => new MainModule(); public EmissionModule emission => new EmissionModule(); public ShapeModule shape => new ShapeModule(); public ColorOverLifetimeModule colorOverLifetime => new ColorOverLifetimeModule(); public SizeOverLifetimeModule sizeOverLifetime => new SizeOverLifetimeModule();
    }
}
namespace UnityEngine.Rendering
{
    public enum IndexFormat { UInt16, UInt32 }
    public enum AmbientMode { Skybox, Trilight, Flat }
}
namespace UnityEngine.Events
{
    public delegate void UnityAction(); public delegate void UnityAction<T>(T a);
    public class UnityEvent { public void AddListener(UnityAction a) { } }
    public class UnityEvent<T> { public void AddListener(UnityAction<T> a) { } }
}
namespace UnityEngine.EventSystems
{
    public class EventSystem : MonoBehaviour { }
    public class StandaloneInputModule : MonoBehaviour { }
}
namespace UnityEngine.UI
{
    using UnityEngine.Events;
    public class CanvasScaler : Behaviour { public enum ScaleMode { ConstantPixelSize, ScaleWithScreenSize } public ScaleMode uiScaleMode { get; set; } public Vector2 referenceResolution { get; set; } public float matchWidthOrHeight { get; set; } }
    public class GraphicRaycaster : Behaviour { }
    public class Graphic : Behaviour { public Color color { get; set; } public bool raycastTarget { get; set; } public RectTransform rectTransform { get; } }
    public class Image : Graphic { }
    public class Text : Graphic { public Font font { get; set; } public int fontSize { get; set; } public TextAnchor alignment { get; set; } public FontStyle fontStyle { get; set; } public string text { get; set; } public HorizontalWrapMode horizontalOverflow { get; set; } public VerticalWrapMode verticalOverflow { get; set; } public bool supportRichText { get; set; } public float lineSpacing { get; set; } }
    public struct ColorBlock { public Color normalColor, highlightedColor, pressedColor, selectedColor, disabledColor; }
    public class Selectable : Behaviour { public bool interactable { get; set; } public ColorBlock colors { get; set; } public Image image { get; set; } }
    public class Button : Selectable { public class ButtonClickedEvent : UnityEvent { } public ButtonClickedEvent onClick { get; } = new ButtonClickedEvent(); }
    public class LayoutElement : Behaviour { public float minHeight { get; set; } public float preferredHeight { get; set; } public float flexibleWidth { get; set; } public float minWidth { get; set; } public float preferredWidth { get; set; } public float flexibleHeight { get; set; } }
    public class LayoutGroup : Behaviour { public RectOffset padding { get; set; } public TextAnchor childAlignment { get; set; } }
    public class HorizontalOrVerticalLayoutGroup : LayoutGroup { public float spacing { get; set; } public bool childForceExpandHeight { get; set; } public bool childForceExpandWidth { get; set; } public bool childControlHeight { get; set; } public bool childControlWidth { get; set; } }
    public class VerticalLayoutGroup : HorizontalOrVerticalLayoutGroup { }
    public class HorizontalLayoutGroup : HorizontalOrVerticalLayoutGroup { }
    public class Outline : Behaviour { public Color effectColor { get; set; } public Vector2 effectDistance { get; set; } }
}
