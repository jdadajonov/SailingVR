using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SailingVR.Sailing
{
    /// <summary>
    /// Builds a Laser-proportioned placeholder boat from Unity primitives.
    ///
    /// HOW TO USE:
    ///   1. Add this component to the Boat GameObject.
    ///   2. Right-click the component header → "Build Placeholder Boat".
    ///   3. Parts are prefixed "[PH]" and can be cleared/rebuilt at any time.
    ///   4. When real models arrive: "Clear Placeholder Parts", import FBX, done.
    ///
    /// Laser dimensions: LOA 4.23 m, beam 1.37 m, mast 6.27 m, boom 2.75 m.
    /// Motor controls added: steering wheel at Z=-0.28 and throttle lever starboard aft.
    /// </summary>
    public class BoatPlaceholder : MonoBehaviour
    {
        private const string Prefix = "[PH]";

        // ── Editor actions ────────────────────────────────────────────────────

        [ContextMenu("Build Placeholder Boat")]
        private void Build()
        {
            ClearPlaceholders();

            // ── Hull ──────────────────────────────────────────────────────────
            var hull = Prim(PrimitiveType.Cube, "Hull", new Color(0.20f, 0.45f, 0.70f));
            hull.transform.localPosition = Vector3.zero;
            hull.transform.localScale    = new Vector3(1.37f, 0.25f, 4.23f);

            // ── Daggerboard ───────────────────────────────────────────────────
            var db = Prim(PrimitiveType.Cube, "Daggerboard", new Color(0.2f, 0.2f, 0.2f));
            db.transform.localPosition = new Vector3(0f, -0.55f, 0.15f);
            db.transform.localScale    = new Vector3(0.08f, 0.9f, 0.45f);

            // ── Cockpit indent ────────────────────────────────────────────────
            var cockpit = Prim(PrimitiveType.Cube, "Cockpit", new Color(0.15f, 0.35f, 0.55f));
            cockpit.transform.localPosition = new Vector3(0f, 0.13f, -0.15f);
            cockpit.transform.localScale    = new Vector3(0.75f, 0.06f, 1.8f);

            // ── MastBase (empty — Sail references this) ───────────────────────
            var mastBaseGO = new GameObject(Prefix + "MastBase");
            mastBaseGO.transform.SetParent(transform, false);
            mastBaseGO.transform.localPosition = new Vector3(0f, 0.13f, 1.0f);

            // ── Mast ──────────────────────────────────────────────────────────
            var mast = PrimOf(PrimitiveType.Cylinder, "Mast", new Color(0.7f, 0.7f, 0.7f), mastBaseGO.transform);
            mast.transform.localPosition = new Vector3(0f, 3.135f, 0f);
            mast.transform.localScale    = new Vector3(0.06f, 3.135f, 0.06f);

            // ── BoomPivot ─────────────────────────────────────────────────────
            var boomPivotGO = new GameObject(Prefix + "BoomPivot");
            boomPivotGO.transform.SetParent(mastBaseGO.transform, false);
            boomPivotGO.transform.localPosition = new Vector3(0f, 0.55f, 0f);

            // ── Boom ──────────────────────────────────────────────────────────
            var boom = PrimOf(PrimitiveType.Cylinder, "Boom", new Color(0.7f, 0.7f, 0.7f), boomPivotGO.transform);
            boom.transform.localPosition = new Vector3(0f, 0f, -1.375f);
            boom.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            boom.transform.localScale    = new Vector3(0.05f, 1.375f, 0.05f);

            // ── Sail ──────────────────────────────────────────────────────────
            var sail = PrimOf(PrimitiveType.Quad, "Sail", new Color(1f, 1f, 0.88f), boomPivotGO.transform);
            sail.transform.localPosition = new Vector3(0f, 2.7f, -1.1f);
            sail.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            sail.transform.localScale    = new Vector3(3.2f, 5.4f, 1f);
            var sailRend = sail.GetComponent<Renderer>();
            if (sailRend != null)
                sailRend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;

            // ── RudderPivot ───────────────────────────────────────────────────
            var rudderPivotGO = new GameObject(Prefix + "RudderPivot");
            rudderPivotGO.transform.SetParent(transform, false);
            rudderPivotGO.transform.localPosition = new Vector3(0f, 0f, -2.1f);

            var rudder = PrimOf(PrimitiveType.Cube, "Rudder", new Color(0.2f, 0.2f, 0.2f), rudderPivotGO.transform);
            rudder.transform.localPosition = new Vector3(0f, -0.28f, -0.18f);
            rudder.transform.localScale    = new Vector3(0.1f, 0.55f, 0.38f);

            var tiller = PrimOf(PrimitiveType.Cylinder, "Tiller", new Color(0.50f, 0.30f, 0.10f), rudderPivotGO.transform);
            tiller.transform.localPosition = new Vector3(0f, 0.05f, 0.55f);
            tiller.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            tiller.transform.localScale    = new Vector3(0.04f, 0.55f, 0.04f);

            var tillerHandleGO = new GameObject(Prefix + "TillerHandle");
            tillerHandleGO.transform.SetParent(rudderPivotGO.transform, false);
            tillerHandleGO.transform.localPosition = new Vector3(0f, 0.05f, 1.0f);

            var sheetGrabGO = new GameObject(Prefix + "SheetGrabPoint");
            sheetGrabGO.transform.SetParent(transform, false);
            sheetGrabGO.transform.localPosition = new Vector3(0.2f, 0.4f, -0.3f);

            // ── Steering wheel ────────────────────────────────────────────────
            BuildSteeringWheel();

            // ── Throttle lever ────────────────────────────────────────────────
            BuildThrottleLever();

            // ── Auto-wire ─────────────────────────────────────────────────────
#if UNITY_EDITOR
            var sailComp = GetComponent<Sail>();
            if (sailComp != null)
            {
                var so   = new SerializedObject(sailComp);
                var prop = so.FindProperty("_mastBase");
                if (prop != null) { prop.objectReferenceValue = mastBaseGO.transform; so.ApplyModifiedProperties(); }
            }

            var bv = GetComponent<BoomVisual>();
            if (bv != null)
            {
                var so = new SerializedObject(bv);
                var bp = so.FindProperty("_boomPivot");
                var rp = so.FindProperty("_rudderPivot");
                if (bp != null) bp.objectReferenceValue = boomPivotGO.transform;
                if (rp != null) rp.objectReferenceValue = rudderPivotGO.transform;
                so.ApplyModifiedProperties();
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
            Debug.Log("[BoatPlaceholder] Built. Save the scene (Cmd+S) before pressing Play.");
#endif
        }

        // ── Steering wheel ────────────────────────────────────────────────────

        private void BuildSteeringWheel()
        {
            var metal = new Color(0.55f, 0.55f, 0.55f);
            var wood  = new Color(0.28f, 0.18f, 0.10f);

            // Pivot = rotation centre; VRSteeringWheel rotates this around local Z
            var pivotGO = new GameObject(Prefix + "WheelPivot");
            pivotGO.transform.SetParent(transform, false);
            pivotGO.transform.localPosition = new Vector3(0f, 0.65f, -0.28f);
            pivotGO.AddComponent<VRSteeringWheel>();

            // Rim — flat cylinder in the wheel's XY plane (rotate 90° around X)
            var rim = PrimOf(PrimitiveType.Cylinder, "WheelRim", metal, pivotGO.transform);
            rim.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            rim.transform.localScale    = new Vector3(0.44f, 0.02f, 0.44f);

            // Hub
            var hub = PrimOf(PrimitiveType.Cylinder, "WheelHub", metal, pivotGO.transform);
            hub.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            hub.transform.localScale    = new Vector3(0.07f, 0.025f, 0.07f);

            // 3 spokes at 120° apart
            // Spokes lie in the XY plane (height along Y = radial direction in XY)
            // spokeHalf = (rimRadius - hubRadius) / 2 = (0.22 - 0.035) / 2 ≈ 0.09 m
            // spokeCenter = hubRadius + spokeHalf = 0.035 + 0.09 = 0.125 m from centre
            float spokeHalf   = 0.09f;
            float spokeCenter = 0.125f;
            for (int i = 0; i < 3; i++)
            {
                float deg = i * 120f;
                float rad = deg * Mathf.Deg2Rad;
                var spoke = PrimOf(PrimitiveType.Cylinder, $"WheelSpoke{i}", wood, pivotGO.transform);
                spoke.transform.localPosition = new Vector3(
                    Mathf.Sin(rad) * spokeCenter,
                    Mathf.Cos(rad) * spokeCenter,
                    0f);
                spoke.transform.localRotation = Quaternion.Euler(0f, 0f, -deg);
                spoke.transform.localScale    = new Vector3(0.018f, spokeHalf, 0.018f);
            }

            // Steering column (post from wheel down toward dashboard)
            var col = PrimOf(PrimitiveType.Cylinder, "WheelColumn", metal, pivotGO.transform);
            col.transform.localPosition = new Vector3(0f, -0.2f, 0f);
            col.transform.localScale    = new Vector3(0.04f, 0.2f, 0.04f);
        }

        // ── Throttle lever ────────────────────────────────────────────────────

        private void BuildThrottleLever()
        {
            var dark = new Color(0.12f, 0.12f, 0.12f);
            var grey = new Color(0.35f, 0.35f, 0.35f);

            // Pivot at BASE; VRThrottleLever tilts this around local X
            // Starboard (right) aft of cockpit, at hand height when seated
            var pivotGO = new GameObject(Prefix + "LeverPivot");
            pivotGO.transform.SetParent(transform, false);
            pivotGO.transform.localPosition = new Vector3(0.45f, 0.32f, -0.45f);
            pivotGO.AddComponent<VRThrottleLever>();

            // Mounting base (the console the lever sits on)
            var baseBox = PrimOf(PrimitiveType.Cube, "LeverBase", grey, pivotGO.transform);
            baseBox.transform.localPosition = new Vector3(0f, -0.04f, 0f);
            baseBox.transform.localScale    = new Vector3(0.14f, 0.05f, 0.14f);

            // Lever arm — extends upward from pivot
            var arm = PrimOf(PrimitiveType.Cylinder, "LeverArm", dark, pivotGO.transform);
            arm.transform.localPosition = new Vector3(0f, 0.16f, 0f);
            arm.transform.localScale    = new Vector3(0.03f, 0.16f, 0.03f);

            // Handle sphere at top (this is the grab point)
            var handle = PrimOf(PrimitiveType.Sphere, "LeverHandle", grey, pivotGO.transform);
            handle.transform.localPosition = new Vector3(0f, 0.32f, 0f);
            handle.transform.localScale    = Vector3.one * 0.07f;
        }

        // ── Clear ─────────────────────────────────────────────────────────────

        [ContextMenu("Clear Placeholder Parts")]
        private void ClearPlaceholders()
        {
            var toDelete = new System.Collections.Generic.List<GameObject>();
            foreach (Transform child in transform)
                if (child.name.StartsWith(Prefix))
                    toDelete.Add(child.gameObject);
            foreach (var go in toDelete)
                DestroyImmediate(go);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        // Parent to Boat
        private GameObject Prim(PrimitiveType type, string label, Color color)
            => PrimOf(type, label, color, transform);

        // Parent to arbitrary transform
        private static GameObject PrimOf(PrimitiveType type, string label, Color color, Transform parent)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = Prefix + label;
            go.transform.SetParent(parent, false);

            var col = go.GetComponent<Collider>();
            if (col != null) DestroyImmediate(col);

            var rend = go.GetComponent<Renderer>();
            if (rend != null)
            {
                var mat = new Material(rend.sharedMaterial);
                mat.color = color;
                rend.sharedMaterial = mat;
            }

            return go;
        }
    }
}
