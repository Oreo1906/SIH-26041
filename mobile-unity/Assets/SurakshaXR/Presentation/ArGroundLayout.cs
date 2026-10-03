using System;
using System.Collections.Generic;
using System.Linq;
using SurakshaXR.Domain;
using UnityEngine;

namespace SurakshaXR.Presentation
{
    [Serializable]
    public sealed class ArDemoSettings
    {
        // Geometry/readiness tolerances only. These are NOT safety distances.
        public float bubbleRadius = 9.5f, minimumDistance = 1.25f, itemSeparation = 1.65f;
        public float minimumScanSeconds = 2.5f, stableTrackingSeconds = .7f, countdownSeconds = 3;
        public float sampleInterval = .3f, stablePositionTolerance = .3f;
        public int stableSamples = 2, minimumCandidates = 5;
        public float maximumSlopeDegrees = 18, minimumGroundDrop = .35f, maximumGroundDrop = 3.5f;
        public float maximumHeightDifference = .45f, setupTimeoutSeconds = 45, anchorTimeoutSeconds = 6;
        public float hazardRadius = 1.45f, routeClearance = 1.4f;
        public float wideObjectRadius = 1.15f, smallObjectRadius = .45f;
        public float observationLifetime = 2.5f, trackingGraceSeconds = 1.2f, compactAfterSeconds = 7;
        public float compactMinimumDistance = .35f, compactMinimumDrop = .12f;
        public float layoutDriftTolerance = .35f, horizontalHitTolerance = .12f;
        public float virtualEyeHeight = 1.65f, virtualMoveSpeed = 2;
        public float minScenarioScale = 1, defaultScenarioScale = 1, maxScenarioScale = 1;
        public int maximumFitCandidates = 72;
        public float[] layoutOrientations = { 0, 45, -45, 90, -90, 135, -135, 180 };
        public Vector3[] rootOffsets = { Vector3.zero, new Vector3(-.35f, 0, .2f), new Vector3(.35f, 0, .2f) };
        public float[] compactScales = { .55f, .4f, .28f };
    }

    [Serializable] public sealed class ScenarioLayoutPoint
    {
        public string entityId;
        public Vector3 localPosition;
        public float localYaw, footprintRadius;
    }
    [Serializable] public sealed class ScenarioLayoutTemplate
    {
        public string moduleId, hazardEntityId, safeEntityId;
        public ScenarioLayoutPoint[] points;
        public Vector3[] path, requiredSupportPoints;
        public float hazardRadius, routeSpacing, routeClearance;
        public float arrowScale = .6f;
        public float minStationClearance = .75f;
        // Include the outlined arrow's furthest vertex, including its tip at turns.
        // These are visual layout clearances, not safety or regulatory distances.
        public float RouteCorridorRadius => routeClearance + .52f * 1.18f * arrowScale;
        public Vector3 simulatedWind;
    }
    [Serializable] public sealed class ScenarioLayoutCatalog { public ScenarioLayoutTemplate[] templates; }
    public sealed class ScenarioLayoutPlacement
    {
        public ScenarioLayoutTemplate Template;
        public Pose RootPose;
        public Quaternion Heading => RootPose.rotation;
        public float Scale;
        public Dictionary<string, Vector3> Positions;
        public List<Vector3> Route;
        public List<Vector3> SupportPoints;
    }

    // One spatial template is transformed as a unit. Only measured Y may vary;
    // renderers cannot move individual items to fill unrelated free patches.
    public static class ArGroundLayout
    {
        public delegate bool GroundProbe(Vector3 planned, out Vector3 measured);
        private static ScenarioLayoutCatalog catalog;
        private static readonly Vector3[] FootprintDirections = { Vector3.right, Vector3.left, Vector3.forward, Vector3.back,
            new Vector3(1, 0, 1).normalized, new Vector3(1, 0, -1).normalized,
            new Vector3(-1, 0, 1).normalized, new Vector3(-1, 0, -1).normalized };
        public static bool ReadyForCountdown(bool tracking, float scanSeconds, float continuousTrackingSeconds, bool validLayout, ArDemoSettings settings)
            => tracking && validLayout && scanSeconds >= settings.minimumScanSeconds && continuousTrackingSeconds >= settings.stableTrackingSeconds;
        public static Vector3 Flat(Vector3 value) => new Vector3(value.x, 0, value.z);
        public static ScenarioLayoutTemplate TemplateFor(ScenarioDefinition scenario)
        {
            if (catalog == null) {
                var resource = Resources.Load<TextAsset>("ScenarioLayouts");
                if (resource == null) throw new InvalidOperationException("Bundled scenario layout configuration is missing.");
                catalog = JsonUtility.FromJson<ScenarioLayoutCatalog>(resource.text);
            }
            var template = catalog.templates.SingleOrDefault(t => t.moduleId == scenario.moduleId);
            if (template == null || !TemplateIsCoherent(template)
                || !new HashSet<string>(template.points.Select(p => p.entityId)).SetEquals(scenario.entities.Select(e => e.id)))
                throw new InvalidOperationException("Scenario layout is missing, incoherent or does not match stable entity IDs.");
            return template;
        }
        public static bool TemplateIsCoherent(ScenarioLayoutTemplate template)
        {
            if (template?.points == null || template.points.Length == 0 || template.path == null || template.path.Length < 2
                || template.requiredSupportPoints == null || template.routeSpacing <= 0 || template.arrowScale <= 0 || template.hazardRadius <= 0
                || template.routeClearance < 0 || template.minStationClearance < 0
                || !Finite(new Vector3(template.routeClearance, template.minStationClearance, template.arrowScale))
                || template.points.Any(p => p == null || string.IsNullOrWhiteSpace(p.entityId)
                    || p.footprintRadius < 0 || !Finite(p.localPosition))
                || template.points.Select(p => p.entityId).Distinct().Count() != template.points.Length) return false;
            var hazard = template.points.FirstOrDefault(p => p.entityId == template.hazardEntityId);
            var safe = template.points.FirstOrDefault(p => p.entityId == template.safeEntityId);
            if (hazard == null || safe == null || Flat(hazard.localPosition).magnitude <= template.hazardRadius
                || Flat(template.path[template.path.Length - 1] - safe.localPosition).magnitude > .05f) return false;
            for (var i = 0; i < template.points.Length; i++) {
                var a = template.points[i];
                for (var j = i + 1; j < template.points.Length; j++) {
                    var b = template.points[j];
                    var ar = a == hazard ? Mathf.Max(a.footprintRadius, template.hazardRadius) : a.footprintRadius;
                    var br = b == hazard ? Mathf.Max(b.footprintRadius, template.hazardRadius) : b.footprintRadius;
                    if (Flat(a.localPosition - b.localPosition).magnitude < ar + br + template.minStationClearance) return false;
                }
            }
            for (var i = 0; i + 1 < template.path.Length; i++) {
                if (!Finite(template.path[i])) return false;
                foreach (var station in template.points) {
                    if (station == safe) continue; // The final route deliberately enters its destination.
                    var radius = station == hazard ? Mathf.Max(station.footprintRadius, template.hazardRadius) : station.footprintRadius;
                    if (DistanceToSegment(station.localPosition, template.path[i], template.path[i + 1])
                        < radius + template.RouteCorridorRadius) return false;
                }
            }
            return template.requiredSupportPoints.All(Finite) && Finite(template.path[template.path.Length - 1]);
        }
        private static bool Finite(Vector3 point) => !float.IsNaN(point.x) && !float.IsNaN(point.y) && !float.IsNaN(point.z)
            && !float.IsInfinity(point.x) && !float.IsInfinity(point.y) && !float.IsInfinity(point.z);
        public static ScenarioLayoutPlacement TransformTemplate(ScenarioLayoutTemplate template, Pose root, float scale = 1)
        {
            if (scale <= 0 || float.IsNaN(scale) || float.IsInfinity(scale)) throw new ArgumentOutOfRangeException(nameof(scale));
            Vector3 World(Vector3 local) => root.position + root.rotation * local * scale;
            var placement = new ScenarioLayoutPlacement {
                Template = template, RootPose = root, Scale = scale,
                Positions = template.points.ToDictionary(p => p.entityId, p => World(p.localPosition)),
                Route = new List<Vector3>(), SupportPoints = new List<Vector3>()
            };
            foreach (var point in template.requiredSupportPoints) placement.SupportPoints.Add(World(point));
            foreach (var point in template.points) foreach (var direction in FootprintDirections)
                placement.SupportPoints.Add(World(point.localPosition + direction * point.footprintRadius));
            for (var i = 0; i + 1 < template.path.Length; i++) {
                var length = Vector3.Distance(template.path[i], template.path[i + 1]);
                var steps = Mathf.Max(1, Mathf.CeilToInt(length / template.routeSpacing));
                for (var step = 0; step < steps; step++) placement.Route.Add(World(Vector3.Lerp(template.path[i], template.path[i + 1], step / (float)steps)));
            }
            placement.Route.Add(World(template.path[template.path.Length - 1]));
            return placement;
        }
        public static Dictionary<string, Vector3> CompactPlan(ScenarioDefinition scenario, Vector3 center, Quaternion heading, float scale)
            => TransformTemplate(TemplateFor(scenario), new Pose(center, heading), scale).Positions;
        public static bool ValidGround(Vector3 point, Vector3 normal, Vector3 origin, Vector3 camera, ArDemoSettings settings)
        {
            var distance = Flat(point - origin).magnitude;
            return distance >= settings.minimumDistance && Flat(point - camera).magnitude >= settings.minimumDistance
                && ValidSupport(point, normal, origin, camera, settings);
        }
        public static bool ValidSupport(Vector3 point, Vector3 normal, Vector3 origin, Vector3 camera, ArDemoSettings settings, bool compact = false)
        {
            var drop = camera.y - point.y;
            return Finite(point) && Flat(point - origin).magnitude <= settings.bubbleRadius + .01f
                && drop >= (compact ? settings.compactMinimumDrop : settings.minimumGroundDrop) && drop <= settings.maximumGroundDrop
                && Vector3.Angle(normal, Vector3.up) <= settings.maximumSlopeDegrees;
        }
        public static List<Vector3> ProbePoints(Vector3 center, Quaternion heading, ArDemoSettings settings)
        {
            var result = new List<Vector3>();
            for (var ring = 0; ring < 3; ring++) {
                var radius = Mathf.Lerp(settings.minimumDistance + .2f, settings.bubbleRadius - .2f, ring / 2f);
                for (var direction = 0; direction < 12; direction++) {
                    var angle = direction * 30 * Mathf.Deg2Rad;
                    result.Add(center + heading * new Vector3(Mathf.Sin(angle) * radius, 0, Mathf.Cos(angle) * radius));
                }
            }
            return result;
        }
        public static float DistanceToSegment(Vector3 point, Vector3 start, Vector3 end)
        {
            var segment = Flat(end - start); var relative = Flat(point - start);
            var t = segment.sqrMagnitude < .0001f ? 0 : Mathf.Clamp01(Vector3.Dot(relative, segment) / segment.sqrMagnitude);
            return (relative - segment * t).magnitude;
        }
        public static bool TryConform(ScenarioLayoutPlacement planned, GroundProbe ground, ArDemoSettings settings, out ScenarioLayoutPlacement conformed)
        {
            conformed = null;
            var positions = new Dictionary<string, Vector3>(); var route = new List<Vector3>(); var supports = new List<Vector3>();
            var minY = float.PositiveInfinity; var maxY = float.NegativeInfinity;
            bool Measure(Vector3 point, out Vector3 accepted) {
                accepted = point;
                if (!ground(point, out var measured) || !Finite(measured)
                    || Flat(measured - point).magnitude > settings.horizontalHitTolerance) return false;
                minY = Mathf.Min(minY, measured.y); maxY = Mathf.Max(maxY, measured.y);
                if (maxY - minY > settings.maximumHeightDifference) return false;
                accepted.y = measured.y; return true;
            }
            foreach (var point in planned.Positions) { if (!Measure(point.Value, out var accepted)) return false; positions.Add(point.Key, accepted); }
            foreach (var point in planned.SupportPoints) { if (!Measure(point, out var accepted)) return false; supports.Add(accepted); }
            foreach (var point in planned.Route) { if (!Measure(point, out var accepted)) return false; route.Add(accepted); }
            var root = planned.RootPose; root.position.y = supports.Count > 0 ? supports[0].y : minY;
            conformed = new ScenarioLayoutPlacement { Template = planned.Template, RootPose = root, Scale = planned.Scale,
                Positions = positions, Route = route, SupportPoints = supports };
            return true;
        }
        public static bool TryFit(ScenarioLayoutTemplate template, Pose worker, GroundProbe ground, ArDemoSettings settings,
            out ScenarioLayoutPlacement fit, out int tested, out int rejected)
        {
            fit = null; tested = rejected = 0;
            if (!TemplateIsCoherent(template)) return false;
            // Worker layouts remain close to life-size even if configuration is
            // mistyped; tabletop scaling is a separate explicit instructor path.
            var minimum = Mathf.Clamp(settings.minScenarioScale, .9f, 1.1f);
            var maximum = Mathf.Clamp(settings.maxScenarioScale, minimum, 1.1f);
            var standard = Mathf.Clamp(settings.defaultScenarioScale, minimum, maximum);
            var scales = new[] { standard, minimum, maximum }.Distinct();
            foreach (var scale in scales) foreach (var offset in settings.rootOffsets) foreach (var angle in settings.layoutOrientations) {
                if (tested >= Mathf.Clamp(settings.maximumFitCandidates, 1, 96)) return false;
                tested++;
                var pose = new Pose(worker.position + worker.rotation * Flat(offset), worker.rotation * Quaternion.Euler(0, angle, 0));
                var planned = TransformTemplate(template, pose, scale);
                if (planned.Positions.Values.Any(p => Flat(p - worker.position).magnitude < settings.minimumDistance
                    || Flat(p - worker.position).magnitude > settings.bubbleRadius)
                    || planned.SupportPoints.Any(p => Flat(p - worker.position).magnitude > settings.bubbleRadius)
                    || planned.Route.Any(p => Flat(p - worker.position).magnitude > settings.bubbleRadius)) { rejected++; continue; }
                if (TryConform(planned, ground, settings, out fit)) return true;
                rejected++;
            }
            return false;
        }
    }
}
