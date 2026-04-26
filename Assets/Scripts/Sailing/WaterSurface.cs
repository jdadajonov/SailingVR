using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SailingVR.Sailing
{
    /// <summary>
    /// Procedural water surface using summed Gerstner waves.
    ///
    /// Gerstner wave (one term):
    ///   Phase   φ = k·(D·pos) − ω·t       where k = 2π/λ, ω = sqrt(g·k)  (deep-water dispersion)
    ///   y       = A · sin(φ)               vertical displacement
    ///   xz      = Q·A·D · cos(φ)           horizontal displacement (creates the forward-leaning crest)
    ///   Q       = steepness / (k·A)        0 → sinusoidal, 1 → fully peaked cycloid
    ///
    /// Three independent wave trains are summed; their parameters are tunable in Inspector.
    /// The mesh is a flat CPU-side grid updated every frame (40×40 = 1 600 vertices — cheap on Quest 3).
    /// GetHeight() evaluates the same math without mesh lookups, used by a future Buoyancy component.
    ///
    /// Place on a dedicated scene-level GameObject (not on the Boat).
    /// Run "Setup Water Material" from the context menu to create a URP material asset.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class WaterSurface : MonoBehaviour
    {
        // ── Inspector ───────────────────────────────────────────────────────────

        [Header("Mesh")]
        [Tooltip("Vertices per side. 40 → 1 600 verts, fine for Quest 3.")]
        [SerializeField] [Range(10, 80)] private int _gridSize = 40;

        [Tooltip("Total size of the water plane in metres.")]
        [SerializeField] private float _worldSize = 300f;

        [Header("Wave 1  (long swell)")]
        [SerializeField] private float _w1Amplitude   = 0.30f;
        [SerializeField] private float _w1Wavelength  = 25f;
        [SerializeField] [Range(0f, 360f)] private float _w1DirectionDeg = 10f;
        [SerializeField] [Range(0f, 1f)]   private float _w1Steepness   = 0.45f;

        [Header("Wave 2  (medium chop)")]
        [SerializeField] private float _w2Amplitude   = 0.12f;
        [SerializeField] private float _w2Wavelength  = 10f;
        [SerializeField] [Range(0f, 360f)] private float _w2DirectionDeg = 55f;
        [SerializeField] [Range(0f, 1f)]   private float _w2Steepness   = 0.35f;

        [Header("Wave 3  (small ripple)")]
        [SerializeField] private float _w3Amplitude   = 0.05f;
        [SerializeField] private float _w3Wavelength  = 4f;
        [SerializeField] [Range(0f, 360f)] private float _w3DirectionDeg = 310f;
        [SerializeField] [Range(0f, 1f)]   private float _w3Steepness   = 0.25f;

        // ── Private ──────────────────────────────────────────────────────────

        private Mesh      _mesh;
        private Vector3[] _baseVerts;   // flat grid, no waves
        private Vector3[] _verts;       // displaced each frame

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void Start()
        {
            BuildMesh();
        }

        private void Update()
        {
            ApplyWaves(Time.time);
        }

        // ── Mesh generation ───────────────────────────────────────────────────

        private void BuildMesh()
        {
            int n = _gridSize;
            _mesh      = new Mesh { name = "WaterMesh" };
            _baseVerts = new Vector3[n * n];
            _verts     = new Vector3[n * n];

            var uvs       = new Vector2[n * n];
            var triangles = new int[(n - 1) * (n - 1) * 6];

            float step = _worldSize / (n - 1);
            float half = _worldSize * 0.5f;

            for (int z = 0; z < n; z++)
            for (int x = 0; x < n; x++)
            {
                int i = z * n + x;
                _baseVerts[i] = new Vector3(x * step - half, 0f, z * step - half);
                uvs[i]        = new Vector2((float)x / (n - 1), (float)z / (n - 1));
            }

            int t = 0;
            for (int z = 0; z < n - 1; z++)
            for (int x = 0; x < n - 1; x++)
            {
                int i = z * n + x;
                triangles[t++] = i;         triangles[t++] = i + n;     triangles[t++] = i + 1;
                triangles[t++] = i + 1;     triangles[t++] = i + n;     triangles[t++] = i + n + 1;
            }

            _mesh.MarkDynamic(); // hint to GPU: vertices change every frame
            _mesh.vertices  = _baseVerts;
            _mesh.triangles = triangles;
            _mesh.uv        = uvs;
            _mesh.bounds    = new Bounds(Vector3.zero, new Vector3(_worldSize, 20f, _worldSize));
            _mesh.RecalculateNormals();

            GetComponent<MeshFilter>().mesh = _mesh;
        }

        private void ApplyWaves(float time)
        {
            for (int i = 0; i < _baseVerts.Length; i++)
            {
                Vector3 p = _baseVerts[i];
                _verts[i] = p
                    + Gerstner(p, _w1Amplitude, _w1Wavelength, _w1DirectionDeg, _w1Steepness, time)
                    + Gerstner(p, _w2Amplitude, _w2Wavelength, _w2DirectionDeg, _w2Steepness, time)
                    + Gerstner(p, _w3Amplitude, _w3Wavelength, _w3DirectionDeg, _w3Steepness, time);
            }

            _mesh.vertices = _verts;
            _mesh.RecalculateNormals();
        }

        // ── Gerstner wave kernel ──────────────────────────────────────────────

        private static Vector3 Gerstner(Vector3 pos, float A, float wavelength,
                                         float dirDeg, float steepness, float time)
        {
            float k   = 2f * Mathf.PI / wavelength;
            float omega = Mathf.Sqrt(9.81f * k);           // deep-water dispersion ω = √(g·k)

            float rad = dirDeg * Mathf.Deg2Rad;
            var   D   = new Vector2(Mathf.Sin(rad), Mathf.Cos(rad)); // unit wave direction

            float phase = k * (D.x * pos.x + D.y * pos.z) - omega * time;
            float Q     = steepness / (k * A + 0.0001f);   // horizontal crest-peaking factor

            return new Vector3(
                Q * A * D.x * Mathf.Cos(phase),   // horizontal X
                A       * Mathf.Sin(phase),         // vertical
                Q * A * D.y * Mathf.Cos(phase)    // horizontal Z
            );
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Water surface height at a world-space XZ position.
        /// Evaluates the same wave math as the mesh — used by a Buoyancy component.
        /// </summary>
        public float GetHeight(float worldX, float worldZ)
        {
            float t   = Time.time;
            var   pos = new Vector3(worldX, 0f, worldZ);
            return Gerstner(pos, _w1Amplitude, _w1Wavelength, _w1DirectionDeg, _w1Steepness, t).y
                 + Gerstner(pos, _w2Amplitude, _w2Wavelength, _w2DirectionDeg, _w2Steepness, t).y
                 + Gerstner(pos, _w3Amplitude, _w3Wavelength, _w3DirectionDeg, _w3Steepness, t).y;
        }

        /// <summary>Convenience overload accepting a world-space position.</summary>
        public float GetHeight(Vector3 worldPosition) => GetHeight(worldPosition.x, worldPosition.z);

        // ── Editor helper ─────────────────────────────────────────────────────

#if UNITY_EDITOR
        [ContextMenu("Setup Water Material")]
        private void SetupWaterMaterial()
        {
            const string path = "Assets/Materials/Water.mat";

            // Find existing or create new
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                System.IO.Directory.CreateDirectory("Assets/Materials");
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, path);
            }

            // Ocean-ish teal, smooth (high reflections), matte metallic
            mat.color = new Color(0.04f, 0.28f, 0.40f, 1f);
            mat.SetFloat("_Smoothness", 0.88f);
            mat.SetFloat("_Metallic",   0.0f);

            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();

            GetComponent<MeshRenderer>().sharedMaterial = mat;
            Debug.Log("[WaterSurface] Material created/updated at " + path);
        }
#endif
    }
}
