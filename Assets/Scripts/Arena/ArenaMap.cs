using System.Collections.Generic;
using UnityEngine;

// The arena's standable surfaces, found once by casting rays down through the screen on the Ground
// layer: the floor and every platform become segments (top y, xMin..xMax). The autopilot plans its
// way to another platform over this map: beside which edge to stand and jump, or off which edge to
// walk, instead of probing the ground around Freny on every decision (that found edges she could
// not climb and never saw the platform two steps away).
public class ArenaMap {

    public class Platform {
        public int index;
        public float y;              // the top surface
        public float xMin, xMax;
        public Collider2D collider;  // the collider the samples came from (edges are refined on it)

        public float Width => xMax - xMin;

        public bool Contains(float x, float margin = 0f) {
            return x >= xMin + margin && x <= xMax - margin;
        }

        public override string ToString() {
            return $"P{index} (y {y:0.0}, x {xMin:0.0} a {xMax:0.0})";
        }
    }

    public enum StepKind { Climb, Drop }

    // one hop of a route: where to stand and which way to go
    public struct Step {
        public Platform from, to;
        public StepKind kind;
        public float x;          // Climb: where to stand and jump; Drop: past this x the body is off the edge
        public float direction;  // Climb: towards the platform once above it; Drop: which way to walk off

        public bool Same(Step other) {
            return from == other.from && to == other.to && kind == other.kind && Mathf.Abs(x - other.x) < 0.01f;
        }

        public override string ToString() {
            string side = direction > 0f ? "esquerda" : "direita";
            return kind == StepKind.Climb
                ? $"subir de {from} para {to} pela borda {side} (x {x:0.0})"
                : $"descer de {from} para {to} pela borda {(direction > 0f ? "direita" : "esquerda")} (x {x:0.0})";
        }
    }

    public readonly List<Platform> Platforms = new List<Platform>();
    private readonly List<Step>[] steps;   // outgoing steps per platform
    private readonly LayerMask ground;
    private readonly float halfWidth, height, jumpHeight;
    private readonly float left, right;

    private const float SampleStep = 0.2f;
    private const float EdgeClearance = 0.18f; // gap between the body and the platform's side while rising (the feet circle must not graze it)
    private const float DropOvershoot = 0.25f; // how far past the edge the body centre goes before it falls
    private const float MinRise = 0.3f;        // lower than this is a step, not a platform to climb
    private const float RiseMargin = 0.3f;     // a platform this close to the jump's apex is not climbed (no time left to move onto it)
    private const float SteerRoom = 0.35f;     // the feet rise this much above the platform before she moves onto it (headroom checked up to there)

    private static readonly RaycastHit2D[] hits = new RaycastHit2D[16];

    public static ArenaMap Build(LayerMask ground, float left, float right, float bottom, float top,
                                 float bodyHalfWidth, float bodyHeight, float jumpHeight) {
        return new ArenaMap(ground, left, right, bottom, top, bodyHalfWidth, bodyHeight, jumpHeight);
    }

    private ArenaMap(LayerMask ground, float left, float right, float bottom, float top,
                     float bodyHalfWidth, float bodyHeight, float jumpHeight) {
        this.ground = ground;
        this.left = left;
        this.right = right;
        halfWidth = bodyHalfWidth;
        height = bodyHeight;
        this.jumpHeight = jumpHeight;

        FindPlatforms(bottom, top);
        steps = new List<Step>[Platforms.Count];
        for (int i = 0; i < Platforms.Count; i++) {
            steps[i] = new List<Step>();
        }
        FindSteps();
    }

    // ---- surfaces ----

    private ContactFilter2D GroundFilter() {
        ContactFilter2D filter = new ContactFilter2D();
        filter.useTriggers = false;
        filter.SetLayerMask(ground);
        return filter;
    }

    private void FindPlatforms(float bottom, float top) {
        ContactFilter2D filter = GroundFilter();
        float castTop = top + 1f;
        float castLength = top - bottom + 2f;
        List<Platform> open = new List<Platform>();
        List<bool> continued = new List<bool>();

        for (float x = left + 0.05f; x <= right - 0.05f + 0.0001f; x += SampleStep) {
            for (int k = 0; k < continued.Count; k++) {
                continued[k] = false;
            }
            int n = Physics2D.Raycast(new Vector2(x, castTop), Vector2.down, filter, hits, castLength);
            for (int i = 0; i < n; i++) {
                float y = hits[i].point.y;
                if (y < bottom + 0.3f) {
                    continue; // under the screen: nobody stands there (the floor's own bottom edge)
                }
                int found = -1;
                for (int k = 0; k < open.Count; k++) {
                    if (!continued[k] && open[k].collider == hits[i].collider && Mathf.Abs(open[k].y - y) <= 0.08f) {
                        found = k;
                        break;
                    }
                }
                if (found >= 0) {
                    open[found].xMax = x;
                    continued[found] = true;
                } else {
                    open.Add(new Platform { y = y, xMin = x, xMax = x, collider = hits[i].collider });
                    continued.Add(true);
                }
            }
            for (int k = open.Count - 1; k >= 0; k--) {
                if (!continued[k]) {
                    Platforms.Add(open[k]);
                    open.RemoveAt(k);
                    continued.RemoveAt(k);
                }
            }
        }
        Platforms.AddRange(open);

        foreach (Platform platform in Platforms) {
            RefineEdges(platform);
        }
        MergeNeighbours();
        // too narrow to stand on
        Platforms.RemoveAll(p => p.Width < halfWidth * 2f + 0.1f);
        Platforms.Sort((a, b) => a.y != b.y ? a.y.CompareTo(b.y) : a.xMin.CompareTo(b.xMin));
        for (int i = 0; i < Platforms.Count; i++) {
            Platforms[i].index = i;
        }
    }

    // the samples are SampleStep apart: find the real edges between the last hit and the first miss
    private void RefineEdges(Platform platform) {
        float lo = Mathf.Max(left, platform.xMin - SampleStep), hi = platform.xMin; // lo misses (or is the wall), hi hits
        if (lo < hi && !HitsSurface(lo, platform)) {
            for (int i = 0; i < 8; i++) {
                float mid = (lo + hi) * 0.5f;
                if (HitsSurface(mid, platform)) hi = mid; else lo = mid;
            }
            platform.xMin = (lo + hi) * 0.5f;
        } else {
            platform.xMin = lo;
        }
        lo = platform.xMax;
        hi = Mathf.Min(right, platform.xMax + SampleStep); // lo hits, hi misses (or is the wall)
        if (lo < hi && !HitsSurface(hi, platform)) {
            for (int i = 0; i < 8; i++) {
                float mid = (lo + hi) * 0.5f;
                if (HitsSurface(mid, platform)) lo = mid; else hi = mid;
            }
            platform.xMax = (lo + hi) * 0.5f;
        } else {
            platform.xMax = hi;
        }
    }

    private bool HitsSurface(float x, Platform platform) {
        int n = Physics2D.Raycast(new Vector2(x, platform.y + 0.3f), Vector2.down, GroundFilter(), hits, 0.6f);
        for (int i = 0; i < n; i++) {
            if (hits[i].collider == platform.collider && Mathf.Abs(hits[i].point.y - platform.y) <= 0.08f) {
                return true;
            }
        }
        return false;
    }

    // two colliders meeting at the same height (the floor's tilemaps) are one surface
    private void MergeNeighbours() {
        Platforms.Sort((a, b) => a.y != b.y ? a.y.CompareTo(b.y) : a.xMin.CompareTo(b.xMin));
        for (int i = Platforms.Count - 1; i > 0; i--) {
            Platform a = Platforms[i - 1], b = Platforms[i];
            if (Mathf.Abs(a.y - b.y) <= 0.08f && b.xMin <= a.xMax + 0.1f) {
                a.xMax = Mathf.Max(a.xMax, b.xMax);
                a.xMin = Mathf.Min(a.xMin, b.xMin);
                Platforms.RemoveAt(i);
            }
        }
    }

    // ---- routes ----

    private void FindSteps() {
        foreach (Platform from in Platforms) {
            foreach (Platform to in Platforms) {
                if (to != from) {
                    AddClimbs(from, to);
                }
            }
            AddDrops(from);
        }
    }

    // stand beside an edge of `to` on `from`, jump straight up, move onto it once above it
    private void AddClimbs(Platform from, Platform to) {
        float rise = to.y - from.y;
        if (rise < MinRise || rise > jumpHeight - RiseMargin) {
            return;
        }
        for (int side = 0; side < 2; side++) {
            float direction = side == 0 ? 1f : -1f; // 0: stand left of the platform, move right onto it
            float standX = side == 0 ? to.xMin - (halfWidth + EdgeClearance) : to.xMax + (halfWidth + EdgeClearance);
            if (!from.Contains(standX, 0.1f) || !Inside(standX, halfWidth)) {
                continue;
            }
            if (!Headroom(standX, from.y, rise + SteerRoom)) {
                continue;
            }
            steps[from.index].Add(new Step { from = from, to = to, kind = StepKind.Climb, x = standX, direction = direction });
        }
    }

    // walk off an edge of `from` and fall straight down onto whatever is below
    private void AddDrops(Platform from) {
        for (int side = 0; side < 2; side++) {
            float direction = side == 0 ? -1f : 1f;
            float offX = side == 0 ? from.xMin - (halfWidth + DropOvershoot) : from.xMax + (halfWidth + DropOvershoot);
            if (!Inside(offX, 0.1f)) {
                continue; // this edge is the arena wall
            }
            Platform landing = null;
            foreach (Platform candidate in Platforms) {
                if (candidate == from || candidate.y >= from.y - 0.2f) {
                    continue;
                }
                // anything the body's width could touch on the way down counts as the landing
                if (candidate.xMax < offX - halfWidth + 0.05f || candidate.xMin > offX + halfWidth - 0.05f) {
                    continue;
                }
                if (landing == null || candidate.y > landing.y) {
                    landing = candidate;
                }
            }
            if (landing == null || !landing.Contains(offX, 0.2f)) {
                continue; // nothing below, or she would land on an edge
            }
            steps[from.index].Add(new Step { from = from, to = landing, kind = StepKind.Drop, x = offX, direction = direction });
        }
    }

    // nothing above the body standing at x up to `rise` higher (the platform she climbs is beside, not above)
    private bool Headroom(float x, float floorY, float rise) {
        Vector2 center = new Vector2(x, floorY + height * 0.5f + 0.03f);
        Vector2 size = new Vector2(halfWidth * 1.8f, height * 0.9f);
        return Physics2D.BoxCast(center, size, 0f, Vector2.up, rise, ground).collider == null;
    }

    private bool Inside(float x, float margin) {
        return x >= left + margin && x <= right - margin;
    }

    // the platform under a point: the highest one whose top is at or below it (within maxDrop)
    public Platform PlatformUnder(Vector2 point, float maxDrop) {
        Platform best = null;
        foreach (Platform platform in Platforms) {
            if (platform.y > point.y + 0.15f || point.y - platform.y > maxDrop || !platform.Contains(point.x, -0.05f)) {
                continue;
            }
            if (best == null || platform.y > best.y) {
                best = platform;
            }
        }
        return best;
    }

    // hops from `from` to every platform (int.MaxValue = no route)
    public int[] HopsFrom(Platform from) {
        int[] hops = new int[Platforms.Count];
        for (int i = 0; i < hops.Length; i++) {
            hops[i] = int.MaxValue;
        }
        if (from == null) {
            return hops;
        }
        Queue<Platform> queue = new Queue<Platform>();
        hops[from.index] = 0;
        queue.Enqueue(from);
        while (queue.Count > 0) {
            Platform current = queue.Dequeue();
            foreach (Step step in steps[current.index]) {
                if (hops[step.to.index] == int.MaxValue) {
                    hops[step.to.index] = hops[current.index] + 1;
                    queue.Enqueue(step.to);
                }
            }
        }
        return hops;
    }

    // the first hop of the shortest route from `from` to `to`, fewest hops first and the least
    // walking among those (from x `fromX`); false when there is no route or she is already there
    public bool FirstStep(Platform from, Platform to, float fromX, out Step first) {
        first = default;
        if (from == null || to == null || from == to) {
            return false;
        }
        int n = Platforms.Count;
        float[] cost = new float[n];
        float[] arriveX = new float[n];
        Step[] via = new Step[n];
        bool[] done = new bool[n];
        for (int i = 0; i < n; i++) {
            cost[i] = float.MaxValue;
        }
        cost[from.index] = 0f;
        arriveX[from.index] = fromX;

        for (int round = 0; round < n; round++) {
            int best = -1;
            for (int i = 0; i < n; i++) {
                if (!done[i] && cost[i] < float.MaxValue && (best < 0 || cost[i] < cost[best])) {
                    best = i;
                }
            }
            if (best < 0 || best == to.index) {
                break;
            }
            done[best] = true;
            foreach (Step step in steps[best]) {
                // a hop costs 1; walking to its edge costs a little, so the nearer edge wins ties
                float stepCost = 1f + Mathf.Abs(step.x - arriveX[best]) / 40f;
                float total = cost[best] + stepCost;
                if (total < cost[step.to.index]) {
                    cost[step.to.index] = total;
                    via[step.to.index] = step;
                    arriveX[step.to.index] = step.kind == StepKind.Climb ? step.x + step.direction * (halfWidth + EdgeClearance + 0.4f) : step.x;
                }
            }
        }
        if (cost[to.index] == float.MaxValue) {
            return false;
        }
        // walk the chain back to the hop that leaves `from`
        Step hop = via[to.index];
        while (hop.from != from) {
            hop = via[hop.from.index];
        }
        first = hop;
        return true;
    }

    public string Describe() {
        System.Text.StringBuilder text = new System.Text.StringBuilder();
        text.Append(Platforms.Count).Append(" plataformas:");
        foreach (Platform platform in Platforms) {
            text.Append(' ').Append(platform.ToString()).Append(" com ").Append(steps[platform.index].Count).Append(" saídas;");
        }
        return text.ToString();
    }
}
