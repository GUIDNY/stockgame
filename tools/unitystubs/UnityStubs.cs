// Minimal, compile-only stub of the UnityEngine API surface used by ECHOBOUND.
// Bodies are empty; this exists so the Unity-facing scripts can be type-checked without the editor.
#pragma warning disable CS0067, CS0649, CS0169, CS8632, CS0108, CS0114
using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEngine
{
    public class Object { public string name { get; set; } public static void Destroy(Object o) { } public static void Destroy(Object o, float t) { } public static void DontDestroyOnLoad(Object o) { } public static T FindObjectOfType<T>() where T : Object => default; public static implicit operator bool(Object o) => o != null; }
    public class Component : Object { public GameObject gameObject { get; } = new GameObject("stub"); public Transform transform => gameObject.transform; public T GetComponent<T>() => default; public T GetComponentInParent<T>() => default; public T GetComponentInChildren<T>() => default; public T[] GetComponents<T>() => new T[0]; public T[] GetComponentsInParent<T>() => new T[0]; }
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
        public T[] GetComponents<T>() => new T[0];
        public static GameObject CreatePrimitive(PrimitiveType t) => new GameObject();
    }
    public class Transform : Component, IEnumerable
    {
        public Vector3 position { get; set; } public Quaternion rotation { get; set; } public Vector3 eulerAngles { get; set; }
        public Vector3 localPosition { get; set; } public Quaternion localRotation { get; set; } public Vector3 localScale { get; set; }
        public Vector3 forward { get; set; } public Vector3 right { get; set; } public Vector3 up { get; set; }
        public Transform parent { get; set; } public int childCount { get; }
        public void SetParent(Transform p, bool worldStays) { } public void SetParent(Transform p) { }
        public Transform GetChild(int i) => null; public Transform Find(string n) => null; public bool IsChildOf(Transform t) => false; public void SetAsLastSibling() { }
        public IEnumerator GetEnumerator() => null;
    }
    public class RectTransform : Transform { public Vector2 anchorMin { get; set; } public Vector2 anchorMax { get; set; } public Vector2 offsetMin { get; set; } public Vector2 offsetMax { get; set; } public Vector2 pivot { get; set; } public Vector2 sizeDelta { get; set; } public Vector2 anchoredPosition { get; set; } }
    public struct Vector2 { public float x, y; public Vector2(float x, float y) { this.x = x; this.y = y; } public static Vector2 zero => new Vector2(0, 0); public static Vector2 one => new Vector2(1, 1); public static Vector2 operator +(Vector2 a, Vector2 b) => new Vector2(a.x + b.x, a.y + b.y); public static Vector2 operator -(Vector2 a, Vector2 b) => new Vector2(a.x - b.x, a.y - b.y); public static Vector2 operator *(Vector2 a, float d) => new Vector2(a.x * d, a.y * d); }
    public struct Vector3
    {
        public float x, y, z; public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; } public Vector3(float x, float y) { this.x = x; this.y = y; z = 0; }
        public static Vector3 zero => new Vector3(0, 0, 0); public static Vector3 one => new Vector3(1, 1, 1); public static Vector3 up => new Vector3(0, 1, 0); public static Vector3 down => new Vector3(0, -1, 0); public static Vector3 forward => new Vector3(0, 0, 1); public static Vector3 right => new Vector3(1, 0, 0);
        public float magnitude => (float)Math.Sqrt(x * x + y * y + z * z); public float sqrMagnitude => x * x + y * y + z * z; public Vector3 normalized => this; public void Normalize() { }
        public static Vector3 operator +(Vector3 a, Vector3 b) => new Vector3(a.x + b.x, a.y + b.y, a.z + b.z); public static Vector3 operator -(Vector3 a, Vector3 b) => new Vector3(a.x - b.x, a.y - b.y, a.z - b.z); public static Vector3 operator -(Vector3 a) => new Vector3(-a.x, -a.y, -a.z);
        public static Vector3 operator *(Vector3 a, float d) => new Vector3(a.x * d, a.y * d, a.z * d); public static Vector3 operator *(float d, Vector3 a) => a * d; public static Vector3 operator /(Vector3 a, float d) => new Vector3(a.x / d, a.y / d, a.z / d);
        public static float Dot(Vector3 a, Vector3 b) => 0; public static Vector3 Cross(Vector3 a, Vector3 b) => a; public static Vector3 Lerp(Vector3 a, Vector3 b, float t) => a; public static Vector3 ProjectOnPlane(Vector3 v, Vector3 n) => v; public static float Distance(Vector3 a, Vector3 b) => 0;
    }
    public struct Quaternion { public static Quaternion identity => new Quaternion(); public static Quaternion Euler(float x, float y, float z) => new Quaternion(); public static Quaternion Euler(Vector3 v) => new Quaternion(); public static Quaternion LookRotation(Vector3 f) => new Quaternion(); public static Quaternion LookRotation(Vector3 f, Vector3 up) => new Quaternion(); public static Quaternion Slerp(Quaternion a, Quaternion b, float t) => a; public static Vector3 operator *(Quaternion q, Vector3 v) => v; }
    public struct Color { public float r, g, b, a; public Color(float r, float g, float b) { this.r = r; this.g = g; this.b = b; a = 1; } public Color(float r, float g, float b, float a) { this.r = r; this.g = g; this.b = b; this.a = a; } public static Color white => new Color(1, 1, 1); public static Color black => new Color(0, 0, 0); public static Color Lerp(Color a, Color b, float t) => a; public static Color operator *(Color a, float d) => new Color(a.r * d, a.g * d, a.b * d, a.a); public static Color HSVToRGB(float h, float s, float v) => white; }
    public static class Mathf { public const float PI = 3.14159f; public static float Clamp(float v, float a, float b) => v; public static int Clamp(int v, int a, int b) => v; public static float Clamp01(float v) => v; public static float Lerp(float a, float b, float t) => a; public static float Max(float a, float b) => a; public static int Max(int a, int b) => a; public static float Min(float a, float b) => a; public static int Min(int a, int b) => a; public static float Abs(float v) => v; public static int Abs(int v) => v; public static float Sin(float v) => v; public static float Cos(float v) => v; public static float Round(float v) => v; }
    public static class Time { public static float deltaTime; public static float unscaledDeltaTime; public static float unscaledTime; public static float time; public static float timeScale; }
    public enum KeyCode { None, LeftShift, E, I, J, R, M, Escape, Tab, F1, F5, F9, Return, Space }
    public static class Input { public static float GetAxis(string n) => 0; public static float GetAxisRaw(string n) => 0; public static bool GetKey(KeyCode k) => false; public static bool GetKeyDown(KeyCode k) => false; public static bool GetMouseButtonDown(int b) => false; }
    public enum QueryTriggerInteraction { UseGlobal, Ignore, Collide }
    public struct LayerMask { public int value; public static implicit operator int(LayerMask m) => m.value; public static implicit operator LayerMask(int v) => new LayerMask { value = v }; }
    public struct RaycastHit { public Collider collider; public float distance; public Vector3 point; }
    public static class Physics
    {
        public static bool Raycast(Vector3 o, Vector3 d, out RaycastHit hit, float max, int mask, QueryTriggerInteraction q) { hit = default; return false; }
        public static bool Raycast(Vector3 o, Vector3 d, float max, int mask, QueryTriggerInteraction q) => false;
        public static bool SphereCast(Vector3 o, float r, Vector3 d, out RaycastHit hit, float max, int mask, QueryTriggerInteraction q) { hit = default; return false; }
        public static int OverlapSphereNonAlloc(Vector3 o, float r, Collider[] results, int mask, QueryTriggerInteraction q) => 0;
    }
    public class Collider : Component { public bool isTrigger { get; set; } public bool enabled { get; set; } }
    public class BoxCollider : Collider { public Vector3 center { get; set; } public Vector3 size { get; set; } }
    public class CapsuleCollider : Collider { public float height { get; set; } public float radius { get; set; } public Vector3 center { get; set; } public int direction { get; set; } }
    public enum CollisionFlags { None }
    public class CharacterController : Collider { public float height { get; set; } public float radius { get; set; } public Vector3 center { get; set; } public float slopeLimit { get; set; } public float stepOffset { get; set; } public bool isGrounded { get; } public CollisionFlags Move(Vector3 m) => CollisionFlags.None; }
    public class Shader : Object { public static Shader Find(string n) => null; }
    public class Material : Object { public Material(Shader s) { } public Color color { get; set; } }
    public class Renderer : Component { public Material sharedMaterial { get; set; } public Material material { get; set; } }
    public class MeshRenderer : Renderer { }
    public enum LightType { Spot, Directional, Point }
    public enum LightShadows { None, Hard, Soft }
    public class Light : Behaviour { public LightType type { get; set; } public LightShadows shadows { get; set; } public float intensity { get; set; } public Color color { get; set; } public float range { get; set; } }
    public class Camera : Behaviour { public static Camera main => null; public float nearClipPlane { get; set; } public float farClipPlane { get; set; } public float fieldOfView { get; set; } public Color backgroundColor { get; set; } }
    public class AudioListener : Behaviour { }
    public enum TextAnchor { UpperLeft, UpperCenter, UpperRight, MiddleLeft, MiddleCenter, MiddleRight, LowerLeft, LowerCenter, LowerRight }
    public enum TextAlignment { Left, Center, Right }
    public enum FontStyle { Normal, Bold, Italic, BoldAndItalic }
    public enum HorizontalWrapMode { Wrap, Overflow }
    public enum VerticalWrapMode { Truncate, Overflow }
    public class Font : Object { public Material material { get; } }
    public class TextMesh : Component { public string text { get; set; } public int fontSize { get; set; } public float characterSize { get; set; } public TextAnchor anchor { get; set; } public TextAlignment alignment { get; set; } public Color color { get; set; } public Font font { get; set; } public bool richText { get; set; } }
    public static class Resources { public static T GetBuiltinResource<T>(string path) where T : Object => default; }
    public enum FogMode { Linear = 1, Exponential, ExponentialSquared }
    public static class RenderSettings { public static bool fog; public static FogMode fogMode; public static float fogStartDistance; public static float fogEndDistance; public static Color ambientLight; public static Color fogColor; }
    public static class Random { public static float value => 0; public static float Range(float a, float b) => a; public static int Range(int a, int b) => a; }
    public static class Application { public static string persistentDataPath => ""; public static string streamingAssetsPath => ""; public static bool runInBackground { get; set; } public static void Quit() { } }
    public enum CursorLockMode { None, Locked, Confined }
    public static class Cursor { public static CursorLockMode lockState { get; set; } public static bool visible { get; set; } }
    public static class Debug { public static void Log(object m) { } public static void LogWarning(object m) { } public static void LogError(object m) { } }
    public enum RuntimeInitializeLoadType { AfterSceneLoad, BeforeSceneLoad }
    public class RuntimeInitializeOnLoadMethodAttribute : Attribute { public RuntimeInitializeOnLoadMethodAttribute() { } public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType t) { } }
    public class RequireComponentAttribute : Attribute { public RequireComponentAttribute(Type a) { } public RequireComponentAttribute(Type a, Type b) { } }
    public enum RenderMode { ScreenSpaceOverlay, ScreenSpaceCamera, WorldSpace }
    public class Canvas : Behaviour { public RenderMode renderMode { get; set; } public int sortingOrder { get; set; } }
    public class RectOffset { public RectOffset(int l, int r, int t, int b) { } }
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
    public class InputField : Selectable { public enum LineType { SingleLine, MultiLineSubmit, MultiLineNewline } public class SubmitEvent : UnityEvent<string> { } public Text textComponent { get; set; } public Graphic placeholder { get; set; } public LineType lineType { get; set; } public int characterLimit { get; set; } public string text { get; set; } public SubmitEvent onSubmit { get; } = new SubmitEvent(); public void ActivateInputField() { } }
    public class ScrollRect : Behaviour { public enum MovementType { Unrestricted, Elastic, Clamped } public RectTransform content { get; set; } public RectTransform viewport { get; set; } public bool horizontal { get; set; } public bool vertical { get; set; } public float scrollSensitivity { get; set; } public MovementType movementType { get; set; } }
    public class Mask : Behaviour { public bool showMaskGraphic { get; set; } }
    public class LayoutElement : Behaviour { public float minHeight { get; set; } public float preferredHeight { get; set; } public float flexibleWidth { get; set; } public float minWidth { get; set; } public float preferredWidth { get; set; } public float flexibleHeight { get; set; } }
    public class LayoutGroup : Behaviour { public RectOffset padding { get; set; } public TextAnchor childAlignment { get; set; } }
    public class HorizontalOrVerticalLayoutGroup : LayoutGroup { public float spacing { get; set; } public bool childForceExpandHeight { get; set; } public bool childForceExpandWidth { get; set; } public bool childControlHeight { get; set; } public bool childControlWidth { get; set; } }
    public class VerticalLayoutGroup : HorizontalOrVerticalLayoutGroup { }
    public class HorizontalLayoutGroup : HorizontalOrVerticalLayoutGroup { }
    public class ContentSizeFitter : Behaviour { public enum FitMode { Unconstrained, MinSize, PreferredSize } public FitMode verticalFit { get; set; } public FitMode horizontalFit { get; set; } }
    public class Outline : Behaviour { public Color effectColor { get; set; } public Vector2 effectDistance { get; set; } }
}
