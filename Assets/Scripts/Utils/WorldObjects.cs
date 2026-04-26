using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace SailingVR.Utils
{
    /// <summary>
    /// Procedurally builds static world objects — islands, buoys, lighthouse —
    /// so the player has fixed visual references and can tell the boat is moving.
    ///
    /// Right-click this component → "Build World Objects".
    /// All objects are prefixed with "[W]" and can be rebuilt at any time.
    ///
    /// Place on a dedicated scene-level GameObject (not on the Boat).
    /// </summary>
    public class WorldObjects : MonoBehaviour
    {
        private const string Prefix = "[W]";

        // ── Editor action ─────────────────────────────────────────────────────

        [ContextMenu("Build World Objects")]
        private void Build()
        {
            Clear();

            // ── Islands ───────────────────────────────────────────────────────
            //   Position (X, Z)     baseRadius  hillHeight  name
            BuildIsland(new Vector3(  0f, 0f,  120f),  22f, 6f,  "IslandNorth");
            BuildIsland(new Vector3(-150f, 0f,  90f),  35f, 10f, "IslandNW");
            BuildIsland(new Vector3( 180f, 0f,  60f),  18f, 5f,  "IslandEast",  withLighthouse: true);
            BuildIsland(new Vector3( -60f, 0f, 220f),  45f, 14f, "IslandFar");

            // ── Buoys (channel markers) ───────────────────────────────────────
            // Green (starboard) buoys — right side of channel heading north
            BuildBuoy(new Vector3( 20f, 0f,  40f), Color.green);
            BuildBuoy(new Vector3( 18f, 0f,  70f), Color.green);
            BuildBuoy(new Vector3( 22f, 0f, 100f), Color.green);

            // Red (port) buoys — left side of channel
            BuildBuoy(new Vector3(-20f, 0f,  40f), Color.red);
            BuildBuoy(new Vector3(-18f, 0f,  70f), Color.red);
            BuildBuoy(new Vector3(-22f, 0f, 100f), Color.red);

            // ── Distant headland silhouette (very large, far away) ────────────
            BuildHeadland(new Vector3(-300f, 0f, 400f), 120f, 40f);
            BuildHeadland(new Vector3( 350f, 0f, 350f),  90f, 30f);

#if UNITY_EDITOR
            EditorSceneManager.MarkSceneDirty(gameObject.scene);
            Debug.Log("[WorldObjects] Built. Save the scene (Cmd+S) before pressing Play.");
#endif
        }

        [ContextMenu("Clear World Objects")]
        private void Clear()
        {
            var toDelete = new System.Collections.Generic.List<GameObject>();
            foreach (Transform child in transform)
                if (child.name.StartsWith(Prefix))
                    toDelete.Add(child.gameObject);
            foreach (var go in toDelete)
                DestroyImmediate(go);
        }

        // ── Island builder ────────────────────────────────────────────────────

        private void BuildIsland(Vector3 centre, float baseRadius, float hillHeight,
                                  string label, bool withLighthouse = false)
        {
            var root = new GameObject(Prefix + label);
            root.transform.SetParent(transform, false);
            root.transform.position = centre;

            // Sandy beach base — flat cylinder
            var beach = Prim(PrimitiveType.Cylinder, "Beach", new Color(0.82f, 0.76f, 0.55f), root);
            beach.transform.localPosition = new Vector3(0f, -0.5f, 0f);
            beach.transform.localScale    = new Vector3(baseRadius * 2f, 0.5f, baseRadius * 2f);

            // Main hill — squashed sphere
            var hill = Prim(PrimitiveType.Sphere, "Hill", new Color(0.22f, 0.50f, 0.18f), root);
            hill.transform.localPosition = new Vector3(0f, hillHeight * 0.3f, 0f);
            hill.transform.localScale    = new Vector3(baseRadius * 1.6f, hillHeight, baseRadius * 1.6f);

            // Secondary smaller hill (gives shape variety)
            var hill2 = Prim(PrimitiveType.Sphere, "Hill2", new Color(0.20f, 0.46f, 0.16f), root);
            hill2.transform.localPosition = new Vector3(baseRadius * 0.35f, hillHeight * 0.2f, baseRadius * 0.2f);
            hill2.transform.localScale    = new Vector3(baseRadius * 0.9f, hillHeight * 0.6f, baseRadius * 0.9f);

            // Rock accent — dark grey sphere at water's edge
            var rock = Prim(PrimitiveType.Sphere, "Rock", new Color(0.40f, 0.38f, 0.35f), root);
            rock.transform.localPosition = new Vector3(-baseRadius * 0.7f, 0.5f, baseRadius * 0.5f);
            rock.transform.localScale    = new Vector3(3f, 2.5f, 3f);

            if (withLighthouse) BuildLighthouse(root, hillHeight);
        }

        // ── Lighthouse ────────────────────────────────────────────────────────

        private void BuildLighthouse(GameObject islandRoot, float hillHeight)
        {
            float baseY = hillHeight * 0.55f;

            // Tower
            var tower = Prim(PrimitiveType.Cylinder, "LH_Tower", Color.white, islandRoot);
            tower.transform.localPosition = new Vector3(0f, baseY + 5f, 0f);
            tower.transform.localScale    = new Vector3(1.4f, 5f, 1.4f);

            // Red stripe
            var stripe = Prim(PrimitiveType.Cylinder, "LH_Stripe", new Color(0.85f, 0.1f, 0.1f), islandRoot);
            stripe.transform.localPosition = new Vector3(0f, baseY + 4.5f, 0f);
            stripe.transform.localScale    = new Vector3(1.45f, 0.8f, 1.45f);

            // Lantern room
            var lantern = Prim(PrimitiveType.Sphere, "LH_Lantern", new Color(1f, 0.85f, 0.1f), islandRoot);
            lantern.transform.localPosition = new Vector3(0f, baseY + 11f, 0f);
            lantern.transform.localScale    = Vector3.one * 1.8f;

            // Cap
            var cap = Prim(PrimitiveType.Cylinder, "LH_Cap", new Color(0.3f, 0.3f, 0.3f), islandRoot);
            cap.transform.localPosition = new Vector3(0f, baseY + 12.2f, 0f);
            cap.transform.localScale    = new Vector3(2f, 0.4f, 2f);
        }

        // ── Buoy builder ──────────────────────────────────────────────────────

        private void BuildBuoy(Vector3 worldPos, Color color)
        {
            var root = new GameObject(Prefix + "Buoy");
            root.transform.SetParent(transform, false);
            root.transform.position = worldPos;

            // Floating cylinder body
            var body = Prim(PrimitiveType.Cylinder, "Body", color, root);
            body.transform.localPosition = new Vector3(0f, 0.7f, 0f);
            body.transform.localScale    = new Vector3(0.35f, 0.7f, 0.35f);

            // Top marker ball
            var ball = Prim(PrimitiveType.Sphere, "Top", color * 0.8f, root);
            ball.transform.localPosition = new Vector3(0f, 1.55f, 0f);
            ball.transform.localScale    = Vector3.one * 0.35f;

            // Submerged base (dark, below waterline)
            var sub = Prim(PrimitiveType.Cylinder, "Sub", new Color(0.15f, 0.15f, 0.15f), root);
            sub.transform.localPosition = new Vector3(0f, -0.4f, 0f);
            sub.transform.localScale    = new Vector3(0.25f, 0.4f, 0.25f);
        }

        // ── Distant headland (large background feature) ───────────────────────

        private void BuildHeadland(Vector3 worldPos, float radius, float height)
        {
            var root = new GameObject(Prefix + "Headland");
            root.transform.SetParent(transform, false);
            root.transform.position = worldPos;

            // Dark blue-grey cliff face
            var cliff = Prim(PrimitiveType.Sphere, "Cliff",
                new Color(0.25f, 0.30f, 0.28f), root);
            cliff.transform.localPosition = new Vector3(0f, height * 0.4f, 0f);
            cliff.transform.localScale    = new Vector3(radius * 2f, height, radius * 1.2f);

            var cliff2 = Prim(PrimitiveType.Sphere, "Cliff2",
                new Color(0.22f, 0.27f, 0.25f), root);
            cliff2.transform.localPosition = new Vector3(radius * 0.6f, height * 0.25f, 0f);
            cliff2.transform.localScale    = new Vector3(radius * 1.4f, height * 0.7f, radius);
        }

        // ── Helper ────────────────────────────────────────────────────────────

        private static GameObject Prim(PrimitiveType type, string label, Color color,
                                        GameObject parent)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = Prefix + label;
            go.transform.SetParent(parent.transform, false);

            // Colliders on static scenery would block the boat — remove them
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
