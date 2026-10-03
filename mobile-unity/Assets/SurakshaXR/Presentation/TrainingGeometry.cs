using System.Collections.Generic;
using UnityEngine;

namespace SurakshaXR.Presentation
{
    // Original, compact geometry shared by simulation and AR. No remote assets,
    // numeric detector thresholds, or renderer-owned training decisions.
    public sealed class TrainingGeometry : MonoBehaviour
    {
        private readonly Dictionary<Color, Material> materials = new Dictionary<Color, Material>();
        private readonly Dictionary<Color, Material> rockMaterials = new Dictionary<Color, Material>();
        private Material guidanceMaterial;
        private Material flameMaterial, rustMaterial;
        private Texture2D mineralTexture, rustTexture;
        private readonly List<Mesh> meshes = new List<Mesh>();
        private static readonly Color Steel = new Color(.23f, .29f, .31f), Dark = new Color(.07f, .095f, .10f), Yellow = new Color(.96f, .66f, .12f), Red = new Color(.73f, .055f, .035f), White = new Color(.88f, .91f, .86f), Green = new Color(.04f, .53f, .29f);
        public GameObject Group(string label, Transform parent, Vector3 position)
        { var result = new GameObject(label); result.transform.SetParent(parent, false); result.transform.localPosition = position; return result; }
        public GameObject Part(Transform parent, string label, PrimitiveType type, Vector3 position, Vector3 scale, Color color, bool solid = false)
        {
            var item = GameObject.CreatePrimitive(type); item.name = label; item.transform.SetParent(parent, false); item.transform.localPosition = position; item.transform.localScale = scale;
            var collider = item.GetComponent<Collider>(); if (collider != null) collider.enabled = solid;
            item.GetComponent<Renderer>().sharedMaterial = Material(color); return item;
        }
        public GameObject Box(Transform p, string n, Vector3 position, Vector3 scale, Color c, bool solid = false) => Part(p, n, PrimitiveType.Cube, position, scale, c, solid);
        public GameObject GroundRing(Transform parent, string label, Vector3 center, float radius, Color color)
        {
            var vertices = new List<Vector3>(); var triangles = new List<int>(); var colors = new List<Color>();
            AddGuidanceRing(vertices, triangles, colors, center, radius, color);
            return GuidanceMesh(parent, label, vertices, triangles, colors);
        }
        public GameObject DashedGroundRing(Transform parent, string label, Vector3 center, float radius, Color color)
        {
            var vertices = new List<Vector3>(); var triangles = new List<int>(); var colors = new List<Color>();
            const int segments = 72;
            for (var i = 0; i < segments; i++) {
                if (i % 6 >= 4) continue;
                var first = new Vector3(Mathf.Sin(i * Mathf.PI * 2 / segments), 0, Mathf.Cos(i * Mathf.PI * 2 / segments));
                var next = new Vector3(Mathf.Sin((i + 1) * Mathf.PI * 2 / segments), 0, Mathf.Cos((i + 1) * Mathf.PI * 2 / segments));
                var start = vertices.Count; var width = Mathf.Min(.12f, radius * .16f);
                vertices.Add(center + first * radius); vertices.Add(center + first * (radius - width));
                vertices.Add(center + next * radius); vertices.Add(center + next * (radius - width));
                for (var c = 0; c < 4; c++) colors.Add(color);
                triangles.AddRange(new[] { start, start + 2, start + 1, start + 1, start + 2, start + 3 });
            }
            return GuidanceMesh(parent, label, vertices, triangles, colors);
        }
        // Green check and beacon are symbols, so labels can use the app's locale.
        // The existing full-height exit gate remains a separate, unchanged entity.
        public GameObject SafeZoneMarker(Transform parent, Vector3 center, float radius, Color color)
        {
            var root = Group("Safe zone check beacon", parent, center);
            GroundRing(root.transform, "Safe zone ground ring", Vector3.up * .035f, radius, color);
            var vertices = new List<Vector3>(); var triangles = new List<int>(); var colors = new List<Color>();
            foreach (var right in new[] { Vector3.right }) {
                var up = Vector3.up; var sign = Vector3.up * 1.35f;
                AddRibbon(vertices, triangles, colors, Vector3.up * .08f, Vector3.up * .93f, right, .045f, color);
                const int segments = 24;
                for (var i = 0; i < segments; i++) {
                    var a = i * Mathf.PI * 2 / segments; var b = (i + 1) * Mathf.PI * 2 / segments; var n = vertices.Count;
                    vertices.Add(sign); vertices.Add(sign + (right * Mathf.Cos(a) + up * Mathf.Sin(a)) * .43f);
                    vertices.Add(sign + (right * Mathf.Cos(b) + up * Mathf.Sin(b)) * .43f);
                    colors.Add(color); colors.Add(color); colors.Add(color); triangles.AddRange(new[] { n, n + 1, n + 2 });
                }
                foreach (var side in new[] { -1, 1 }) {
                    var offset = Vector3.Cross(right, up) * (.012f * side);
                    var first = sign + offset - right * .23f; var middle = sign + offset - right * .06f - up * .16f;
                    var end = sign + offset + right * .25f + up * .20f;
                    AddRibbon(vertices, triangles, colors, first, middle, (right + up).normalized, .06f, Color.white);
                    AddRibbon(vertices, triangles, colors, middle, end, (-right + up).normalized, .06f, Color.white);
                }
            }
            GuidanceMesh(root.transform, "Safe zone beacon symbol", vertices, triangles, colors).AddComponent<TrainingSignBillboard>();
            return root;
        }
        private static void AddRibbon(List<Vector3> vertices, List<int> triangles, List<Color> colors, Vector3 a, Vector3 b, Vector3 across, float halfWidth, Color color)
        {
            var n = vertices.Count;
            vertices.Add(a - across * halfWidth); vertices.Add(a + across * halfWidth);
            vertices.Add(b - across * halfWidth); vertices.Add(b + across * halfWidth);
            for (var i = 0; i < 4; i++) colors.Add(color);
            triangles.AddRange(new[] { n, n + 1, n + 2, n + 1, n + 3, n + 2 });
        }
        public GameObject GroundArrows(Transform parent, IReadOnlyList<Vector3> points, Color? color = null, float scale = 1)
        {
            var vertices = new List<Vector3>(); var triangles = new List<int>(); var colors = new List<Color>();
            for (var i = 0; i + 1 < points.Count; i++) {
                var direction = points[i + 1] - points[i];
                if (direction.sqrMagnitude < .0025f) continue;
                var forward = direction.normalized; var right = Vector3.Cross(Vector3.up, forward).normalized;
                var p = points[i] + Vector3.up * (.055f * scale);
                // One combined mesh: broad dark border stays visible over light concrete,
                // while the unlit green center remains visible in the virtual mine.
                AddGuidanceArrow(vertices, triangles, colors, p, right, forward, 1.18f * scale, GuidanceOutline);
                AddGuidanceArrow(vertices, triangles, colors, p + Vector3.up * (.006f * scale), right, forward, scale, color ?? new Color(.28f, 1, .55f, 1));
            }
            return GuidanceMesh(parent, "Anchored evacuation arrows", vertices, triangles, colors);
        }
        // This is a configured teaching cue, never a measurement of live wind.
        public GameObject WindIndicator(Transform parent, Vector3 center, Vector3 direction)
        {
            direction = ArGroundLayout.Flat(direction);
            if (direction.sqrMagnitude < .0001f) direction = Vector3.left;
            var forward = direction.normalized; var right = Vector3.Cross(Vector3.up, forward);
            var vertices = new List<Vector3>(); var triangles = new List<int>(); var colors = new List<Color>();
            var p = center + Vector3.up * .055f;
            AddGuidanceArrow(vertices, triangles, colors, p, right, forward, 1.18f, GuidanceOutline);
            AddGuidanceArrow(vertices, triangles, colors, p + Vector3.up * .006f, right, forward, 1, new Color(1, .78f, .09f));
            foreach (var offset in new[] { -.29f, 0f, .29f })
                AddRibbon(vertices, triangles, colors, p + right * offset - forward * .86f,
                    p + right * offset - forward * .47f, right, .035f, new Color(.15f, .78f, 1));
            return GuidanceMesh(parent, "Simulated wind indicator", vertices, triangles, colors);
        }
        // Caller positions this root on the target's ground and applies uniform model scale.
        // Crossed downward arrows are legible from either side without camera-facing transforms.
        public GameObject CreateFocusMarker(Transform parent)
        {
            var root = Group("Practice target ring and beacon", parent, Vector3.zero);
            GroundRing(root.transform, "Practice target ground ring", Vector3.up * .055f, .55f, new Color(1, .91f, .15f, 1));
            var vertices = new List<Vector3>(); var triangles = new List<int>(); var colors = new List<Color>();
            foreach (var right in new[] { Vector3.right, Vector3.forward }) {
                AddGuidanceArrow(vertices, triangles, colors, Vector3.zero, right, Vector3.down, .638f, GuidanceOutline);
                // Place the smaller face on both sides of the outline so the beacon is
                // readable from front/back. Only this child shrinks on approach.
                var normal = Vector3.Cross(right, Vector3.down) * .009f;
                AddGuidanceArrow(vertices, triangles, colors, normal, right, Vector3.down, .55f, new Color(1, .91f, .15f, 1));
                AddGuidanceArrow(vertices, triangles, colors, -normal, right, Vector3.down, .55f, new Color(1, .91f, .15f, 1));
            }
            var arrow = GuidanceMesh(root.transform, "Practice target arrow", vertices, triangles, colors);
            var guidance = root.AddComponent<TargetGuidance>(); guidance.Initialize(arrow.transform); guidance.SetTargetHeight(2.1f);
            return root;
        }
        private static readonly Color GuidanceOutline = new Color(.035f, .05f, .065f, 0);
        private static void AddGuidanceRing(List<Vector3> vertices, List<int> triangles, List<Color> colors, Vector3 center, float radius, Color color)
        {
            var width = Mathf.Min(.23f, radius * .35f); var border = Mathf.Min(.055f, radius * .08f);
            AddRingBand(vertices, triangles, colors, center, radius + border, Mathf.Max(.01f, radius - width - border), GuidanceOutline);
            color.a = 1;
            AddRingBand(vertices, triangles, colors, center + Vector3.up * .006f, radius, Mathf.Max(.015f, radius - width), color);
        }
        private static void AddRingBand(List<Vector3> vertices, List<int> triangles, List<Color> colors, Vector3 center, float outer, float inner, Color color)
        {
            const int segments = 48;
            for (var i = 0; i < segments; i++) {
                var a = i * Mathf.PI * 2 / segments; var b = (i + 1) * Mathf.PI * 2 / segments; var start = vertices.Count;
                var first = new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a)); var next = new Vector3(Mathf.Sin(b), 0, Mathf.Cos(b));
                vertices.Add(center + first * outer); vertices.Add(center + first * inner);
                vertices.Add(center + next * outer); vertices.Add(center + next * inner);
                for (var c = 0; c < 4; c++) colors.Add(color);
                triangles.AddRange(new[] { start, start + 2, start + 1, start + 1, start + 2, start + 3 });
            }
        }
        private static void AddGuidanceArrow(List<Vector3> vertices, List<int> triangles, List<Color> colors, Vector3 center, Vector3 right, Vector3 forward, float scale, Color color)
        {
            var start = vertices.Count;
            foreach (var p in new[] { new Vector2(0, .52f), new Vector2(.38f, .08f), new Vector2(.15f, .08f), new Vector2(.15f, -.38f), new Vector2(-.15f, -.38f), new Vector2(-.15f, .08f), new Vector2(-.38f, .08f) }) {
                vertices.Add(center + (right * p.x + forward * p.y) * scale); colors.Add(color);
            }
            triangles.AddRange(new[] { start, start + 1, start + 6, start + 2, start + 3, start + 4, start + 2, start + 4, start + 5 });
        }
        private GameObject GuidanceMesh(Transform parent, string label, List<Vector3> vertices, List<int> triangles, List<Color> colors)
        {
            var mesh = new Mesh { name = label }; mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.SetColors(colors); mesh.RecalculateBounds(); meshes.Add(mesh);
            var item = Group(label, parent, Vector3.zero); item.AddComponent<MeshFilter>().sharedMesh = mesh;
            if (guidanceMaterial == null) guidanceMaterial = new Material(Resources.Load<Shader>("Guidance")) { name = "Shared unlit training guidance", enableInstancing = true };
            var renderer = item.AddComponent<MeshRenderer>(); renderer.sharedMaterial = guidanceMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; renderer.receiveShadows = false;
            return item;
        }
        private GameObject GroundMesh(Transform parent, string label, List<Vector3> vertices, List<int> triangles, Color color)
        {
            var mesh = new Mesh { name = label }; mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds(); meshes.Add(mesh);
            var item = Group(label, parent, Vector3.zero); item.AddComponent<MeshFilter>().sharedMesh = mesh;
            item.AddComponent<MeshRenderer>().sharedMaterial = Material(color); return item;
        }
        private GameObject Sphere(Transform p, string n, Vector3 position, Vector3 scale, Color c) => Part(p, n, PrimitiveType.Sphere, position, scale, c);
        private GameObject Cylinder(Transform p, string n, Vector3 position, float diameter, float height, Color c) => Part(p, n, PrimitiveType.Cylinder, position, new Vector3(diameter, height / 2, diameter), c);
        private void Rod(Transform p, string n, Vector3 a, Vector3 b, float width, Color c)
        { var rod = Cylinder(p, n, (a + b) / 2, width, Vector3.Distance(a, b), c); rod.transform.localRotation = Quaternion.FromToRotation(Vector3.up, b - a); }
        private Material Material(Color color)
        {
            if (materials.TryGetValue(color, out var material)) return material;
            material = color.a < 1 ? new Material(Resources.Load<Shader>("SoftSmoke")) : new Material(Resources.Load<Material>("PreviewMaterial"));
            material.color = color; material.enableInstancing = true;
            if (color.a < 1) {
                material.renderQueue = 3000;
            }
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", .18f); materials.Add(color, material); return material;
        }
        private void RockFinish(GameObject item)
        {
            var renderer = item.GetComponent<Renderer>(); var tint = renderer.sharedMaterial.color;
            if (mineralTexture == null) {
                mineralTexture = new Texture2D(128, 128, TextureFormat.RGB24, true) { name = "Original mineral grain", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear };
                var pixels = new Color[128 * 128];
                for (var y = 0; y < 128; y++) for (var x = 0; x < 128; x++) {
                    var grain = .6f * Mathf.PerlinNoise(x * .08f + 26, y * .08f + 41) + .4f * Mathf.PerlinNoise(x * .7f + 13, y * .7f + 7);
                    pixels[y * 128 + x] = new Color(grain, grain, grain);
                }
                mineralTexture.SetPixels(pixels); mineralTexture.Apply(true, true);
            }
            if (!rockMaterials.TryGetValue(tint, out var material)) {
                material = new Material(Resources.Load<Shader>("CaveRock")); material.color = tint;
                material.mainTexture = mineralTexture; material.enableInstancing = true; rockMaterials.Add(tint, material);
            }
            renderer.sharedMaterial = material;
        }
        public void Mine()
        {
            // Keep the static shell isolated from independently animated/hidden entities.
            var environment = Group("Mine environment", transform, Vector3.zero).transform;
            RockFinish(Box(environment, "Gravel floor", new Vector3(0, -.2f, 4), new Vector3(20, .4f, 26), new Color(.26f, .25f, .22f), true));
            CaveRoof(environment);
            RockFinish(Box(environment, "Left rock face", new Vector3(-10, 2, 4), new Vector3(.6f, 5, 26), Steel, true));
            RockFinish(Box(environment, "Right rock face", new Vector3(10, 2, 4), new Vector3(.6f, 5, 26), Steel, true));
            RockFinish(Box(environment, "Rear rock face", new Vector3(0, 2, -9), new Vector3(20, 5, .6f), Steel, true));
            RockFinish(Box(environment, "End rock face", new Vector3(0, 2, 17), new Vector3(20, 5, .6f), Steel, true));
            var rng = new System.Random(26041);
            for (var i = 0; i < 72; i++)
            {
                var side = i % 2 == 0 ? -1 : 1;
                Rock(environment, new Vector3(side * (9.5f + (float)rng.NextDouble() * .2f), .4f + (float)rng.NextDouble() * 3.6f, -8 + (float)rng.NextDouble() * 24), new Vector3(.65f, .7f + (float)rng.NextDouble(), 1 + (float)rng.NextDouble()), new Color(.25f + i % 3 * .025f, .25f + i % 3 * .023f, .23f + i % 3 * .02f));
            }
            for (var z = -6; z <= 15; z += 4)
            {
                foreach (var x in new[] { -8.7f, 8.7f })
                {
                    Box(environment, "Steel support", new Vector3(x, 2, z), new Vector3(.25f, 4, .35f), Steel, true);
                    Box(environment, "Support foot", new Vector3(x, .12f, z), new Vector3(.6f, .24f, .65f), Dark);
                    Rod(environment, "Roof brace", new Vector3(x, 3.2f, z), new Vector3(x * .82f, 4.25f, z), .14f, Steel);
                    Box(environment, "Walkway edge", new Vector3(x * .77f, .012f, z), new Vector3(.12f, .02f, 2.6f), Yellow);
                }
                Box(environment, "Roof girder", new Vector3(0, 4.22f, z), new Vector3(17.5f, .28f, .35f), Steel);
                Box(environment, "Lamp housing", new Vector3(0, 4.02f, z), new Vector3(1.7f, .16f, .4f), Dark);
                Box(environment, "Lamp diffuser", new Vector3(0, 3.93f, z), new Vector3(1.5f, .035f, .32f), new Color(1, .91f, .68f));
            }
            // Services and disused rail along the edge leave the training route clear.
            Rod(environment, "Ventilation duct", new Vector3(7.6f, 3.55f, -8), new Vector3(7.6f, 3.55f, 16), .65f, new Color(.42f, .46f, .43f));
            for (var z = -8; z < 17; z += 2) {
                var collar = Cylinder(environment, "Duct collar", new Vector3(7.6f, 3.55f, z), .70f, .12f, Steel); collar.transform.localRotation = Quaternion.Euler(90, 0, 0);
                Box(environment, "Rail sleeper", new Vector3(8.1f, .03f, z), new Vector3(1.5f, .06f, .2f), new Color(.24f, .15f, .08f));
            }
            foreach (var x in new[] { 7.65f, 8.55f }) Box(environment, "Steel rail", new Vector3(x, .09f, 4), new Vector3(.09f, .10f, 25), Steel);
            Rod(environment, "Service pipe", new Vector3(-9, 2.8f, -8), new Vector3(-9, 2.8f, 16), .13f, Yellow);
            CombineEnvironment(environment);
        }
        private void CombineEnvironment(Transform environment, string batchName = "Environment material batch")
        {
            var groups = new Dictionary<Material, List<CombineInstance>>();
            var originals = new List<MeshRenderer>();
            foreach (var filter in environment.GetComponentsInChildren<MeshFilter>()) {
                var renderer = filter.GetComponent<MeshRenderer>();
                if (renderer == null || !renderer.enabled || filter.sharedMesh == null || renderer.sharedMaterial == null) continue;
                if (filter.GetComponent<TrainingFlame>() != null || filter.GetComponent<TrainingSmoke>() != null) continue;
                var material = renderer.sharedMaterial;
                if (!groups.TryGetValue(material, out var batch)) { batch = new List<CombineInstance>(); groups.Add(material, batch); }
                batch.Add(new CombineInstance { mesh = filter.sharedMesh, transform = environment.worldToLocalMatrix * filter.transform.localToWorldMatrix });
                originals.Add(renderer);
            }
            foreach (var group in groups) {
                var combined = new Mesh { name = batchName == "Environment material batch" ? "Combined mine environment" : "Combined static training props" };
                var vertices = 0; foreach (var item in group.Value) vertices += item.mesh.vertexCount;
                if (vertices > 65535) combined.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                combined.CombineMeshes(group.Value.ToArray(), true, true); combined.RecalculateBounds(); meshes.Add(combined);
                var batch = Group(batchName, environment, Vector3.zero);
                batch.AddComponent<MeshFilter>().sharedMesh = combined;
                var renderer = batch.AddComponent<MeshRenderer>(); renderer.sharedMaterial = group.Key;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            // Preserve original colliders and their transforms; only render submissions change.
            // The combined meshes live in environment coordinates, so AR anchors and virtual
            // locomotion may still move/rotate the parent without Unity static batching rules.
            foreach (var renderer in originals) renderer.enabled = false;
        }
        private void CaveRoof(Transform parent)
        {
            // Inward-facing irregular arch; one mesh/collider rather than many roof cubes.
            var vertices = new List<Vector3>(); var triangles = new List<int>();
            const int slices = 24, lengths = 26;
            Vector3 Point(int s, int z) {
                var angle = s * Mathf.PI / slices;
                var rough = Mathf.PerlinNoise(s * .73f + 26, z * .61f + 41) * .34f;
                return new Vector3(Mathf.Cos(angle) * 9.8f, 3.7f + Mathf.Sin(angle) * 1.6f - rough, -9 + z);
            }
            for (var z = 0; z < lengths; z++) for (var s = 0; s < slices; s++) {
                var n = vertices.Count;
                vertices.Add(Point(s, z)); vertices.Add(Point(s + 1, z));
                vertices.Add(Point(s, z + 1)); vertices.Add(Point(s + 1, z + 1));
                triangles.AddRange(new[] { n, n + 2, n + 1, n + 1, n + 2, n + 3 });
            }
            var roof = GroundMesh(parent, "Irregular inward cave arch", vertices, triangles, new Color(.28f, .235f, .18f));
            RockFinish(roof);
            roof.AddComponent<MeshCollider>().sharedMesh = roof.GetComponent<MeshFilter>().sharedMesh;
        }
        public GameObject Entity(string id, string kind, Transform parent, Vector3 position)
        {
            var root = Group(id, parent, position);
            var visual = Group("Model", root.transform, Vector3.zero); var p = visual.transform;
            if (kind == "exit" || kind == "training_equipment") p.localPosition = Vector3.down * position.y;
            if (kind == "interaction") {
                p.localPosition = Vector3.down * .7f;
                // Model yaw below turns its rear mounting face toward local -Z.
                Rod(root.transform, "Alarm pedestal", new Vector3(0, -position.y, -.15f), new Vector3(0, 0, -.15f), .10f, Steel);
            }
            if (kind == "interaction" || kind == "detector" || kind == "exit") p.localRotation = Quaternion.Euler(0, 180, 0);
            switch (kind)
            {
                case "hazard_fire": Fire(p); break;
                case "interaction": Alarm(p); break;
                case "training_equipment":
                    // Metre-scale visual dimensions: the original 1.22 m model
                    // becomes a 0.76 m extinguisher/plinth. Keep its ground pivot
                    // independent of scale; scenario anchor coordinates stay intact.
                    p.localScale = Vector3.one * .62f; Extinguisher(p); break;
                case "detector": Detector(p, position.y); break;
                case "hazard_zone": ConfinedSpace(p); break;
                case "ppe": EquipmentRack(p); break;
                case "buddy": Worker(p); break;
                case "exit": if (id.Contains("blocked")) Barrier(p, false); else Exit(p); break;
                default: Box(p, "Equipment case", Vector3.up * .5f, new Vector3(.6f, 1, .45f), Steel); break;
            }
            return root;
        }
        private void Extinguisher(Transform p)
        {
            Cylinder(p, "Red pressure cylinder", new Vector3(0, .62f, 0), .36f, .73f, Red);
            Sphere(p, "Cylinder shoulder", new Vector3(0, .97f, 0), new Vector3(.36f, .24f, .36f), Red);
            Cylinder(p, "Base boot", new Vector3(0, .25f, 0), .39f, .13f, Dark);
            Cylinder(p, "Valve", new Vector3(0, 1.10f, 0), .08f, .17f, Steel);
            Box(p, "Squeeze handle", new Vector3(.035f, 1.2f, 0), new Vector3(.3f, .04f, .08f), Dark);
            Box(p, "Instruction label", new Vector3(0, .7f, -.185f), new Vector3(.22f, .28f, .012f), White);
            Rod(p, "Hose upper", new Vector3(.08f, 1.1f, 0), new Vector3(.32f, .92f, 0), .045f, Dark);
            Rod(p, "Hose", new Vector3(.32f, .92f, 0), new Vector3(.3f, .45f, 0), .045f, Dark);
            Rod(p, "Nozzle", new Vector3(.3f, .45f, 0), new Vector3(.38f, .32f, -.04f), .085f, Dark);
            Box(p, "Equipment plinth", new Vector3(0, .07f, 0), new Vector3(.75f, .14f, .65f), Steel);
        }
        private void Alarm(Transform p)
        {
            Box(p, "Red alarm enclosure", new Vector3(0, .7f, 0), new Vector3(.52f, .55f, .2f), Red);
            Box(p, "White switch surround", new Vector3(0, .65f, -.11f), new Vector3(.33f, .26f, .02f), White);
            var button = Cylinder(p, "Alarm push button", new Vector3(0, .65f, -.145f), .13f, .06f, Red); button.transform.localRotation = Quaternion.Euler(90, 0, 0);
            Sphere(p, "Alarm beacon", new Vector3(0, 1.07f, 0), new Vector3(.18f, .19f, .18f), Yellow);
        }
        private void Detector(Transform p, float mountHeight)
        {
            Box(p, "Protective detector body", Vector3.zero, new Vector3(.4f, .6f, .18f), Yellow);
            Box(p, "Detector readout", new Vector3(0, .06f, -.10f), new Vector3(.29f, .22f, .025f), Dark);
            // Alarm symbol has no fabricated ppm or gas-specific numeric limit.
            Box(p, "Alarm symbol stem", new Vector3(0, .09f, -.12f), new Vector3(.027f, .10f, .02f), Red);
            Sphere(p, "Alarm symbol dot", new Vector3(0, .006f, -.125f), Vector3.one * .035f, Red);
            for (var i = -1; i <= 1; i++) Sphere(p, "Control button", new Vector3(i * .10f, -.14f, -.105f), new Vector3(.055f, .055f, .025f), Dark);
            for (var i = -1; i <= 1; i++) Box(p, "Sensor vent", new Vector3(i * .07f, .24f, -.1f), new Vector3(.025f, .05f, .02f), Dark);
            Rod(p, "Detector stand", new Vector3(0, -.3f, .1f), new Vector3(0, -mountHeight, .1f), .08f, Steel);
        }
        private void Barrier(Transform p, bool confined)
        {
            foreach (var x in new[] { -1.4f, 1.4f }) { Cylinder(p, "Barrier post", new Vector3(x, .65f, 0), .10f, 1.3f, Yellow); Box(p, "Barrier foot", new Vector3(x, .08f, 0), new Vector3(.6f, .16f, .6f), Dark); }
            foreach (var y in new[] { .65f, 1.1f }) {
                Box(p, "Warning rail", new Vector3(0, y, 0), new Vector3(2.9f, .17f, .12f), Yellow);
                for (var x = -1.2f; x <= 1.3f; x += .4f) { var stripe = Box(p, "Hazard stripe", new Vector3(x, y, -.07f), new Vector3(.14f, .17f, .014f), Dark); stripe.transform.localRotation = Quaternion.Euler(0, 0, -25); }
            }
            Box(p, "Restriction sign", new Vector3(0, 1.55f, 0), new Vector3(.65f, .62f, .08f), White);
            var ring = Cylinder(p, "Do not enter disc", new Vector3(0, 1.55f, -.06f), .49f, .035f, Red); ring.transform.localRotation = Quaternion.Euler(90, 0, 0);
            Box(p, "Do not enter bar", new Vector3(0, 1.55f, -.087f), new Vector3(.34f, .075f, .02f), White);
            if (confined) {
                Box(p, "Confined space opening", new Vector3(0, .03f, 1.9f), new Vector3(2.4f, .05f, 2), Dark);
                foreach (var x in new[] { -1.3f, 1.3f }) Box(p, "Opening curb", new Vector3(x, .1f, 1.9f), new Vector3(.18f, .2f, 2.3f), Steel);
                for (var i = 0; i < 4; i++) Rod(p, "Ladder rung", new Vector3(-.4f, .06f, 1.25f + i * .3f), new Vector3(.4f, .06f, 1.25f + i * .3f), .06f, Steel);
            } else for (var i = 0; i < 7; i++) Rock(p, new Vector3((i % 3 - 1) * .7f, .3f + i / 3 * .35f, .8f), Vector3.one * .8f, Steel);
        }
        private void ConfinedSpace(Transform p)
        {
            var leakSource = new Vector3(-.75f, .70f, 1.08f);
            Group("Gas leak source", p, leakSource);
            // This is a shallow virtual cutaway above the scanned surface, not a
            // claimed hole or reconstruction of the real ground beneath the phone.
            Cylinder(p, "Confined space dark well", new Vector3(0, .018f, 0), 1.05f, .036f, new Color(.012f, .018f, .016f));
            Annulus(p, "Raised manhole rim", Vector3.zero, .6f, .49f, .04f, .27f, Steel);
            Annulus(p, "Inner well wall", Vector3.zero, .495f, .478f, .025f, .265f, new Color(.045f, .062f, .055f));
            foreach (var x in new[] { -.18f, .18f }) Rod(p, "Entry ladder rail", new Vector3(x, .055f, .27f), new Vector3(x, .81f, .47f), .045f, Yellow);
            for (var i = 0; i < 4; i++) {
                var height = .13f + i * .17f; var z = .29f + i * .045f;
                Rod(p, "Entry ladder rung", new Vector3(-.18f, height, z), new Vector3(.18f, height, z), .045f, Steel);
            }
            var head = new Vector3(0, 2.05f, 0);
            foreach (var foot in new[] { new Vector3(-.67f, .045f, -.40f), new Vector3(.67f, .045f, -.40f), new Vector3(0, .045f, .74f) }) {
                Rod(p, "Retrieval tripod leg", foot, head, .055f, Steel);
                Box(p, "Tripod foot", foot, new Vector3(.16f, .07f, .18f), Dark);
            }
            Sphere(p, "Tripod head", head, Vector3.one * .16f, Yellow);
            Rod(p, "Retrieval cable", head - Vector3.up * .08f, new Vector3(0, .64f, 0), .012f, Dark);
            Rod(p, "Retrieval hook", new Vector3(0, .64f, 0), new Vector3(.06f, .56f, 0), .025f, Steel);
            var tank = Group("Industrial gas source", p, Vector3.zero);
            // Authored demo dimensions in metres. Keep the vessel behind the
            // tripod instead of overlapping its entry and rear support leg.
            var barrel = Cylinder(tank.transform, "Weathered pressure tank", new Vector3(-.12f, 1.10f, 1.65f), .82f, 1.50f, Steel);
            barrel.transform.localRotation = Quaternion.Euler(0, 0, 90); RustFinish(barrel);
            foreach (var x in new[] { -.87f, .63f }) {
                var end = Sphere(tank.transform, "Tank rounded end", new Vector3(x, 1.10f, 1.65f), new Vector3(.20f, .82f, .82f), Steel); RustFinish(end);
            }
            foreach (var x in new[] { -.61f, .37f }) {
                Box(tank.transform, "Tank saddle", new Vector3(x, .38f, 1.65f), new Vector3(.18f, .64f, .56f), Steel);
                Box(tank.transform, "Tank base", new Vector3(x, .04f, 1.65f), new Vector3(.38f, .08f, .66f), Dark);
            }
            Rod(tank.transform, "Tank outlet", new Vector3(-.57f, .95f, 1.35f), leakSource, .12f, Steel);
            Rod(tank.transform, "Leaking pipe", leakSource, new Vector3(-1.05f, .70f, .45f), .12f, Steel);
            Rod(tank.transform, "Pipe return", new Vector3(-1.05f, .70f, .45f), new Vector3(-1.05f, .12f, .45f), .12f, Steel);
            var flange = Cylinder(tank.transform, "Leak flange", leakSource, .23f, .055f, Dark);
            flange.transform.localRotation = Quaternion.FromToRotation(Vector3.up, new Vector3(-.30f, 0, -.63f));
            Rod(tank.transform, "Valve stem", new Vector3(-.93f, .70f, .70f), new Vector3(-.93f, .86f, .70f), .035f, Steel);
            var wheel = Annulus(tank.transform, "Valve wheel", new Vector3(-.93f, .89f, .70f), .14f, .10f, -.022f, .022f, Red);
            Rod(wheel.transform, "Valve spoke", new Vector3(-.10f, 0, 0), new Vector3(.10f, 0, 0), .022f, Red);
            var barrier = Group("Entry warning barrier", p, new Vector3(0, 0, -.82f));
            foreach (var x in new[] { -.68f, .68f }) {
                Rod(barrier.transform, "Warning post", new Vector3(x, .04f, 0), new Vector3(x, .96f, 0), .05f, Steel);
                Box(barrier.transform, "Warning foot", new Vector3(x, .035f, 0), new Vector3(.25f, .07f, .3f), Dark);
            }
            Box(barrier.transform, "Entry warning rail", new Vector3(0, .85f, 0), new Vector3(1.4f, .11f, .08f), White);
            for (var i = -3; i <= 3; i++) {
                var stripe = Box(barrier.transform, "Barrier red stripe", new Vector3(i * .19f, .85f, -.045f), new Vector3(.09f, .11f, .01f), Red);
                stripe.transform.localRotation = Quaternion.Euler(0, 0, -22);
            }
            Box(barrier.transform, "Entry warning board", new Vector3(0, .53f, -.04f), new Vector3(.56f, .46f, .055f), White);
            var stop = Cylinder(barrier.transform, "Entry restricted symbol", new Vector3(0, .53f, -.08f), .34f, .025f, Red);
            stop.transform.localRotation = Quaternion.Euler(90, 0, 0);
            Box(barrier.transform, "Entry restricted bar", new Vector3(0, .53f, -.10f), new Vector3(.24f, .052f, .01f), White);
            Cone(p, "Entry cone left", new Vector3(-.99f, 0, -.50f)); Cone(p, "Entry cone right", new Vector3(.99f, 0, -.50f));
            var plume = Group("Practice gas plume", p, leakSource);
            for (var i = 0; i < 4; i++) {
                var puff = Part(plume.transform, "Illustrative gas puff", PrimitiveType.Quad, new Vector3(i * .16f, .08f + i * .06f, -.11f * i),
                    new Vector3(.90f + i * .12f, .80f + i * .10f, 1), new Color(.57f, .76f, .19f, .45f));
                puff.AddComponent<TrainingSmoke>().Phase = i * 1.1f;
            }
            // Assessment must not imply that gas can be seen; the shared renderer
            // enables this illustrative child only for practice after recognition.
            plume.SetActive(false);
            CombineEnvironment(p, "Confined area material batch");
        }
        private GameObject Annulus(Transform parent, string label, Vector3 position, float outer, float inner, float bottom, float top, Color color)
        {
            var vertices = new List<Vector3>(); var triangles = new List<int>(); const int segments = 32;
            for (var i = 0; i < segments; i++) {
                var a = new Vector3(Mathf.Sin(i * Mathf.PI * 2 / segments), 0, Mathf.Cos(i * Mathf.PI * 2 / segments));
                var b = new Vector3(Mathf.Sin((i + 1) * Mathf.PI * 2 / segments), 0, Mathf.Cos((i + 1) * Mathf.PI * 2 / segments));
                var n = vertices.Count;
                vertices.Add(a * outer + Vector3.up * bottom); vertices.Add(b * outer + Vector3.up * bottom);
                vertices.Add(a * outer + Vector3.up * top); vertices.Add(b * outer + Vector3.up * top);
                vertices.Add(a * inner + Vector3.up * top); vertices.Add(b * inner + Vector3.up * top);
                vertices.Add(a * inner + Vector3.up * bottom); vertices.Add(b * inner + Vector3.up * bottom);
                triangles.AddRange(new[] { n, n + 1, n + 2, n + 1, n + 3, n + 2,
                    n + 2, n + 3, n + 4, n + 3, n + 5, n + 4,
                    n + 4, n + 5, n + 6, n + 5, n + 7, n + 6 });
            }
            var mesh = GroundMesh(parent, label, vertices, triangles, color); mesh.transform.localPosition = position; return mesh;
        }
        private void Cone(Transform parent, string label, Vector3 position)
        {
            var root = Group(label, parent, position); var vertices = new List<Vector3>(); var triangles = new List<int>();
            const int segments = 12;
            for (var i = 0; i < segments; i++) {
                var n = vertices.Count; var a = i * Mathf.PI * 2 / segments; var b = (i + 1) * Mathf.PI * 2 / segments;
                vertices.Add(new Vector3(Mathf.Sin(a) * .14f, .04f, Mathf.Cos(a) * .14f));
                vertices.Add(new Vector3(Mathf.Sin(b) * .14f, .04f, Mathf.Cos(b) * .14f));
                vertices.Add(new Vector3(Mathf.Sin(a) * .025f, .45f, Mathf.Cos(a) * .025f));
                vertices.Add(new Vector3(Mathf.Sin(b) * .025f, .45f, Mathf.Cos(b) * .025f));
                triangles.AddRange(new[] { n, n + 1, n + 2, n + 1, n + 3, n + 2 });
            }
            GroundMesh(root.transform, "Orange warning cone", vertices, triangles, new Color(.93f, .27f, .035f));
            Cylinder(root.transform, "Cone reflective band", new Vector3(0, .28f, 0), .151f, .055f, White);
            Box(root.transform, "Cone base", new Vector3(0, .025f, 0), new Vector3(.32f, .05f, .32f), Dark);
        }
        private void Helmet(Transform p, Vector3 center)
        { Sphere(p, "Hard hat dome", center, new Vector3(.29f, .14f, .28f), Yellow); Cylinder(p, "Helmet brim", center + Vector3.down * .055f, .34f, .022f, Yellow); Box(p, "Helmet lamp", center + new Vector3(0, -.005f, -.145f), new Vector3(.075f, .052f, .045f), White); }
        private void EquipmentRack(Transform p)
        {
            // Illustrative kit only: the shared content still decides the allowed
            // PPE choice; this visual does not specify real equipment suitability.
            Box(p, "PPE equipment mat", new Vector3(0, .025f, 0), new Vector3(1.18f, .05f, .62f), Dark);
            foreach (var x in new[] { -.26f, .26f }) {
                Box(p, "Breathing kit backplate", new Vector3(x, .41f, .11f), new Vector3(.25f, .68f, .055f), Steel);
                Cylinder(p, "Training air cylinder", new Vector3(x, .34f, 0), .18f, .42f, Yellow);
                Sphere(p, "Air cylinder shoulder", new Vector3(x, .57f, 0), new Vector3(.18f, .10f, .18f), Yellow);
                Sphere(p, "Air cylinder base", new Vector3(x, .14f, 0), new Vector3(.18f, .08f, .18f), Dark);
                Cylinder(p, "Cylinder valve", new Vector3(x, .66f, 0), .055f, .08f, Steel);
                foreach (var y in new[] { .23f, .47f }) Cylinder(p, "Cylinder retaining band", new Vector3(x, y, 0), .188f, .035f, Dark);
                Rod(p, "Harness shoulder strap", new Vector3(x - .10f, .71f, .13f), new Vector3(x - .135f, .31f, -.10f), .035f, Dark);
                Rod(p, "Harness second strap", new Vector3(x + .10f, .71f, .13f), new Vector3(x + .135f, .31f, -.10f), .035f, Dark);
                Rod(p, "Breathing hose upper", new Vector3(x, .66f, 0), new Vector3(x + .14f, .47f, -.12f), .023f, Dark);
                Rod(p, "Breathing hose lower", new Vector3(x + .14f, .47f, -.12f), new Vector3(x + .09f, .14f, -.22f), .023f, Dark);
                Sphere(p, "Face mask rim", new Vector3(x + .09f, .15f, -.21f), new Vector3(.18f, .21f, .11f), Dark);
                Sphere(p, "Face mask visor", new Vector3(x + .09f, .17f, -.247f), new Vector3(.14f, .14f, .055f), new Color(.20f, .32f, .35f));
            }
            Helmet(p, new Vector3(0, 1.12f, .15f));
            Box(p, "Kit display shelf", new Vector3(0, 1.04f, .16f), new Vector3(.6f, .035f, .31f), Steel);
            foreach (var x in new[] { -.22f, .22f }) Rod(p, "Kit shelf support", new Vector3(x, .05f, .25f), new Vector3(x, 1.05f, .25f), .035f, Steel);
            CombineEnvironment(p, "PPE station material batch");
        }
        private void Worker(Transform p)
        {
            // Adult-sized reference silhouette (1.825 m including hard hat),
            // authored for the demo rather than an anthropometric standard.
            var trouser = new Color(.13f, .22f, .28f);
            foreach (var side in new[] { -1, 1 }) {
                Part(p, "Work trouser leg", PrimitiveType.Capsule, new Vector3(side * .13f, .53f, 0), new Vector3(.19f, .40f, .20f), trouser);
                Box(p, "Safety boot", new Vector3(side * .13f, .07f, -.055f), new Vector3(.21f, .14f, .32f), Dark);
                Rod(p, "Coverall upper arm", new Vector3(side * .22f, 1.35f, 0), new Vector3(side * .30f, 1.10f, -.015f), .115f, Yellow);
                Sphere(p, "Coverall elbow", new Vector3(side * .30f, 1.10f, -.015f), Vector3.one * .115f, Yellow);
                Rod(p, "Coverall forearm", new Vector3(side * .30f, 1.10f, -.015f), new Vector3(side * .33f, .92f, -.055f), .10f, Yellow);
                Sphere(p, "Gloved hand", new Vector3(side * .33f, .88f, -.065f), new Vector3(.105f, .14f, .085f), Steel);
            }
            Part(p, "Work coverall hips", PrimitiveType.Capsule, new Vector3(0, .95f, 0), new Vector3(.35f, .14f, .25f), trouser);
            Part(p, "High visibility torso", PrimitiveType.Capsule, new Vector3(0, 1.17f, 0), new Vector3(.44f, .28f, .27f), Yellow);
            Box(p, "Reflective vest strip", new Vector3(0, 1.11f, -.14f), new Vector3(.37f, .045f, .015f), White);
            foreach (var x in new[] { -.12f, .12f }) Box(p, "Reflective shoulder strip", new Vector3(x, 1.29f, -.133f), new Vector3(.035f, .19f, .015f), White);
            var skin = new Color(.48f, .32f, .23f);
            Cylinder(p, "Neck", new Vector3(0, 1.48f, 0), .11f, .10f, skin);
            Sphere(p, "Head", new Vector3(0, 1.635f, 0), new Vector3(.205f, .27f, .21f), skin);
            Helmet(p, new Vector3(0, 1.755f, 0));
            Box(p, "Attendant clipboard", new Vector3(.31f, 1.03f, -.20f), new Vector3(.22f, .28f, .035f), White);
            CombineEnvironment(p, "Attendant material batch");
        }
        private void Exit(Transform p)
        {
            foreach (var x in new[] { -.765f, .765f }) Box(p, "Exit frame", new Vector3(x, 1.125f, 0), new Vector3(.12f, 2.25f, .12f), Green);
            Box(p, "Exit sign", new Vector3(0, 2.19f, 0), new Vector3(1.65f, .32f, .16f), Green);
            // A familiar running-person/door symbol works in all three locales.
            Box(p, "Door symbol", new Vector3(-.42f, 2.19f, -.095f), new Vector3(.19f, .24f, .02f), White);
            Sphere(p, "Exit person head", new Vector3(-.055f, 2.28f, -.1f), Vector3.one * .058f, White);
            Rod(p, "Exit person torso", new Vector3(-.039f, 2.237f, -.1f), new Vector3(.031f, 2.128f, -.1f), .035f, White);
            Rod(p, "Exit person leg", new Vector3(.031f, 2.128f, -.1f), new Vector3(.14f, 2.073f, -.1f), .027f, White);
            Rod(p, "Exit person other leg", new Vector3(.031f, 2.128f, -.1f), new Vector3(-.078f, 2.057f, -.1f), .027f, White);
            Box(p, "Muster pad", new Vector3(0, .025f, .5f), new Vector3(1.4f, .05f, 1.1f), Green);
        }
        private void Fire(Transform p)
        {
            Group("Fire action target", p, new Vector3(0, .88f, 0));
            Cylinder(p, "Scorched drum base", new Vector3(0, .012f, 0), 1.7f, .024f, new Color(.105f, .085f, .065f));
            var positions = new[] { new Vector3(-.37f, 0, .02f), new Vector3(.25f, 0, .35f), new Vector3(.27f, 0, -.34f) };
            for (var i = 0; i < positions.Length; i++) {
                var drum = Group("Rusted drum " + i, p, positions[i]);
                var shell = Cylinder(drum.transform, "Weathered drum shell", new Vector3(0, .44f, 0), .54f, .84f, Steel); RustFinish(shell);
                foreach (var height in new[] { .04f, .27f, .62f, .86f })
                    Cylinder(drum.transform, "Drum rolled seam", new Vector3(0, height, 0), .557f, .034f, new Color(.25f, .17f, .12f));
                Cylinder(drum.transform, "Drum dark opening", new Vector3(0, .883f, 0), .49f, .018f, Dark);
                var flame = Flame(p, positions[i] + Vector3.up * .78f, .42f, 1.10f + i * .09f, Color.white);
                flame.AddComponent<TrainingFlame>().Phase = i * 1.7f;
            }
            for (var i = 0; i < 5; i++) {
                var smoke = Part(p, "Training smoke", PrimitiveType.Quad, new Vector3(.04f + i * .06f, 1.62f + i * .33f, .06f + i * .04f),
                    new Vector3(.78f + i * .16f, .76f + i * .10f, 1), new Color(.19f, .175f, .15f, .34f));
                smoke.AddComponent<TrainingSmoke>().Phase = i;
            }
            var glow = Group("Fire glow", p, new Vector3(0, 1.0f, -.5f)).AddComponent<Light>(); glow.type = LightType.Point; glow.color = new Color(1, .40f, .1f); glow.range = 3; glow.intensity = 1.5f; glow.shadows = LightShadows.None;
            CombineEnvironment(p, "Drum source material batch");
        }
        private GameObject Flame(Transform p, Vector3 pos, float radius, float height, Color color)
        {
            var vertices = new List<Vector3>(); var triangles = new List<int>(); var uv = new List<Vector2>();
            // Three crossed sheets remain visible while circling the anchored fire.
            // Soft, animated tongues come from the shared shader, not solid triangles.
            for (var i = 0; i < 3; i++) {
                var across = Quaternion.Euler(0, i * 60, 0) * Vector3.right * radius; var n = vertices.Count;
                vertices.Add(-across); vertices.Add(across); vertices.Add(-across + Vector3.up * height); vertices.Add(across + Vector3.up * height);
                uv.Add(new Vector2(0, 0)); uv.Add(new Vector2(1, 0)); uv.Add(new Vector2(0, 1)); uv.Add(new Vector2(1, 1));
                triangles.AddRange(new[] { n, n + 1, n + 2, n + 1, n + 3, n + 2 });
            }
            var mesh = new Mesh { name = "Crossed soft flame sheets" }; mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.SetUVs(0, uv); mesh.RecalculateBounds(); meshes.Add(mesh);
            var flame = Group("Training flame volume", p, pos); flame.AddComponent<MeshFilter>().sharedMesh = mesh;
            if (flameMaterial == null) flameMaterial = new Material(Resources.Load<Shader>("TrainingFire")) { name = "Shared daylight training fire", enableInstancing = true };
            var renderer = flame.AddComponent<MeshRenderer>(); renderer.sharedMaterial = flameMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; renderer.receiveShadows = false; return flame;
        }
        private void RustFinish(GameObject item)
        {
            if (rustMaterial == null) {
                rustTexture = new Texture2D(128, 128, TextureFormat.RGB24, true) { name = "Original weathered metal", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear };
                var pixels = new Color[128 * 128];
                for (var y = 0; y < 128; y++) for (var x = 0; x < 128; x++) {
                    var broad = Mathf.PerlinNoise(x * .085f + 11, y * .10f + 19);
                    var grain = Mathf.PerlinNoise(x * .65f + 7, y * .61f + 5);
                    var rust = Color.Lerp(new Color(.19f, .10f, .045f), new Color(.52f, .28f, .12f), grain);
                    pixels[y * 128 + x] = Color.Lerp(new Color(.36f, .39f, .36f), rust, Mathf.SmoothStep(.1f, 1, Mathf.InverseLerp(.30f, .65f, broad)));
                }
                rustTexture.SetPixels(pixels); rustTexture.Apply(true, true);
                rustMaterial = new Material(Resources.Load<Material>("PreviewMaterial")) { name = "Shared original rusted metal", color = Color.white, mainTexture = rustTexture, enableInstancing = true };
                if (rustMaterial.HasProperty("_Metallic")) rustMaterial.SetFloat("_Metallic", .22f);
                if (rustMaterial.HasProperty("_Glossiness")) rustMaterial.SetFloat("_Glossiness", .15f);
            }
            item.GetComponent<Renderer>().sharedMaterial = rustMaterial;
        }
        private void Rock(Transform p, Vector3 pos, Vector3 size, Color color)
        {
            var v = new[] { new Vector3(0,.7f,0), new Vector3(-.55f,0,-.35f), new Vector3(.43f,0,-.5f), new Vector3(.6f,0,.45f), new Vector3(-.48f,0,.5f), new Vector3(0,-.6f,0) };
            var rock = MeshObject(p, "Faceted rock", pos, v, new[] {0,2,1,0,3,2,0,4,3,0,1,4,5,1,2,5,2,3,5,3,4,5,4,1}, color); rock.transform.localScale = size; RockFinish(rock);
        }
        private GameObject MeshObject(Transform p, string n, Vector3 pos, Vector3[] vertices, int[] indices, Color color)
        {
            // Split vertices gives intentional flat shading without texture downloads.
            var flat = new Vector3[indices.Length]; var sequential = new int[indices.Length];
            for (var i = 0; i < indices.Length; i++) { flat[i] = vertices[indices[i]]; sequential[i] = i; }
            var mesh = new Mesh { name = n, vertices = flat, triangles = sequential }; mesh.RecalculateNormals(); mesh.RecalculateBounds(); meshes.Add(mesh);
            var obj = Group(n, p, pos); obj.AddComponent<MeshFilter>().sharedMesh = mesh; obj.AddComponent<MeshRenderer>().sharedMaterial = Material(color); return obj;
        }
        private void OnDestroy() {
            if (guidanceMaterial != null) { if (Application.isPlaying) Destroy(guidanceMaterial); else DestroyImmediate(guidanceMaterial); }
            if (flameMaterial != null) { if (Application.isPlaying) Destroy(flameMaterial); else DestroyImmediate(flameMaterial); }
            if (rustMaterial != null) { if (Application.isPlaying) Destroy(rustMaterial); else DestroyImmediate(rustMaterial); }
            if (rustTexture != null) { if (Application.isPlaying) Destroy(rustTexture); else DestroyImmediate(rustTexture); }
            foreach (var material in rockMaterials.Values) { if (Application.isPlaying) Destroy(material); else DestroyImmediate(material); }
            if (mineralTexture != null) { if (Application.isPlaying) Destroy(mineralTexture); else DestroyImmediate(mineralTexture); }
            foreach (var m in materials.Values) { if (Application.isPlaying) Destroy(m); else DestroyImmediate(m); }
            foreach (var mesh in meshes) { if (Application.isPlaying) Destroy(mesh); else DestroyImmediate(mesh); }
        }
    }
    // Only the guidance sign turns. Its world anchor and ground ring stay fixed.
    public sealed class TrainingSignBillboard : MonoBehaviour
    {
        public void FaceCamera(Camera camera)
        {
            if (camera == null) return;
            // The symbol is authored for a view from local -Z. Keep its local
            // right aligned with the viewer's right so the check is not mirrored.
            var direction = transform.position - camera.transform.position; direction.y = 0;
            if (direction.sqrMagnitude > .0001f) transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }
        private void LateUpdate() => FaceCamera(Camera.main);
    }
    public sealed class TrainingFlame : MonoBehaviour
    {
        public float Phase;
        private float elapsed;
        private Renderer visual;
        private MaterialPropertyBlock properties;
        private void Update() {
            elapsed += Time.deltaTime;
            if (visual == null) { visual = GetComponent<Renderer>(); properties = new MaterialPropertyBlock(); }
            transform.localScale = new Vector3(1, 1 + Mathf.Sin(elapsed * 2 + Phase) * .035f, 1);
            if (visual != null) { visual.GetPropertyBlock(properties); properties.SetFloat("_EffectTime", elapsed + Phase); visual.SetPropertyBlock(properties); }
        }
    }
    public sealed class TrainingSmoke : MonoBehaviour
    {
        public float Phase;
        private Vector3 origin;
        private float elapsed;
        private void Start() { origin = transform.localPosition; }
        private void Update() {
            elapsed += Time.deltaTime;
            transform.localPosition = origin + new Vector3(Mathf.Sin(elapsed * .35f + Phase) * .065f, Mathf.Sin(elapsed * .4f + Phase) * .08f, 0);
            if (Camera.main != null) transform.rotation = Camera.main.transform.rotation;
        }
    }
}
