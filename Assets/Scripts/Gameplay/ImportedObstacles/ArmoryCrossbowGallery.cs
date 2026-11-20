using System;
using System.Collections.Generic;
using ChessFight.Gameplay;
using UnityEngine;

namespace ChessFight.ProtectKing
{
    // StateDrivenMover: bounded offline crossbow volleys with swept projectile collision.
    public sealed class ArmoryCrossbowGallery : Obstacle
    {
        [SerializeField] ArmoryCourseContext context;
        [SerializeField] BoxCollider zone;
        [SerializeField] Transform[] emitters;
        [SerializeField] Transform[] aimLines;
        Vector3[] aimedDirections;
        long aimedVolley=long.MinValue;
        readonly Collider[] targets=new Collider[256];
        [SerializeField] Transform[] projectileVisuals;
        [SerializeField, Min(.5f)] float volleySeconds = 2.8f;
        [SerializeField, Min(.1f)] float telegraphSeconds = 1;
        [SerializeField, Min(1)] float arrowSpeed = 18;
        [SerializeField, Min(0)] float hitSpeed = 5;
        [SerializeField, Min(0)] float stun = .6f;
        [SerializeField, Min(.1f)] float hitCooldown = 1;
        readonly RaycastHit[] hits = new RaycastHit[128];
        readonly Dictionary<ImportedObstacleActor, double> lastHit = new Dictionary<ImportedObstacleActor, double>();
        Vector3[] positions, directions;
        float[] remaining;
        bool[] live;
        Renderer[][] bows;
        MaterialPropertyBlock tint;
        double lastTime;
        long lastVolley = long.MinValue;
        int seed, next;
        public int BlockedShots { get; private set; }
        public int PawnHits { get; private set; }
        public bool Warning { get; private set; }
        double Now => context != null ? context.Now : SharedTime;
        void Awake()
        {
            if (zone == null || emitters == null || emitters.Length == 0 || projectileVisuals == null || projectileVisuals.Length < emitters.Length)
            { Debug.LogError("Assign the bounded armory firing zone, emitters and projectile pool.", this); enabled = false; return; }
            foreach (var visual in projectileVisuals) if (visual == null)
            { Debug.LogError("Armory projectile pool contains an unassigned visual.", this); enabled = false; return; }
            int n = projectileVisuals.Length;
            positions = new Vector3[n]; directions = new Vector3[n]; remaining = new float[n]; live = new bool[n];
            aimedDirections=new Vector3[emitters.Length];
            bows = new Renderer[emitters.Length][];
            for (int i = 0; i < emitters.Length; i++) bows[i] = emitters[i] != null ? emitters[i].GetComponentsInChildren<Renderer>() : Array.Empty<Renderer>();
            tint = new MaterialPropertyBlock(); ResetGallery();
        }
        public void ResetGallery()
        {
            if (live == null) return;
            Array.Clear(live, 0, live.Length); foreach (var visual in projectileVisuals) visual.gameObject.SetActive(false);
            aimedVolley=long.MinValue;
            if(aimLines!=null)foreach(var line in aimLines)if(line!=null)line.gameObject.SetActive(false);
            lastHit.Clear(); lastVolley = long.MinValue; lastTime = Now; seed = context != null ? context.Seed : 0; next = 0;
            BlockedShots = PawnHits = 0;
        }
        // Slab clipping returns only the segment inside the damage zone, including rotated/scaled zones.
        public static bool ClipSegment(BoxCollider area, Vector3 from, Vector3 to, out Vector3 start, out Vector3 end)
        {
            Vector3 a = area.transform.InverseTransformPoint(from) - area.center;
            Vector3 b = area.transform.InverseTransformPoint(to) - area.center;
            Vector3 half = area.size * .5f;
            float near = 0, far = 1;
            bool x = Slab(a.x, b.x - a.x, half.x, ref near, ref far);
            bool y = Slab(a.y, b.y - a.y, half.y, ref near, ref far);
            bool z = Slab(a.z, b.z - a.z, half.z, ref near, ref far);
            start = Vector3.Lerp(from, to, near); end = Vector3.Lerp(from, to, far);
            return x && y && z && far > near;
        }
        static bool Slab(float p, float d, float half, ref float near, ref float far)
        {
            if (Mathf.Abs(d) < .000001f) return Mathf.Abs(p) <= half;
            float lo = (-half - p) / d, hi = (half - p) / d;
            if (lo > hi) { float swap = lo; lo = hi; hi = swap; }
            near = Mathf.Max(near, lo); far = Mathf.Min(far, hi);
            return near <= far;
        }
        void Aim(long volley)
        {
            aimedVolley=volley;var bounds=zone.bounds;
            int n=Physics.OverlapBoxNonAlloc(bounds.center,bounds.extents,targets,Quaternion.identity,~0,QueryTriggerInteraction.Ignore);
            for(int e=0;e<emitters.Length;e++)
            {
                var source=emitters[e];if(source==null)continue;
                float nearest=float.PositiveInfinity;Vector3 target=source.position+source.forward*30;
                for(int i=0;i<n;i++)
                {
                    var actor=ImportedObstacleActor.Find(targets[i]);if(actor==null||!actor.CanReceiveObstacle)continue;
                    Vector3 p=zone.transform.InverseTransformPoint(actor.Position)-zone.center,h=zone.size*.5f;
                    if(Mathf.Abs(p.x)>h.x||Mathf.Abs(p.y)>h.y||Mathf.Abs(p.z)>h.z)continue;
                    float distance=(actor.Position-source.position).sqrMagnitude;
                    if(distance<nearest){nearest=distance;target=actor.Position+Vector3.up*1.1f;}
                }
                var direction=(target-source.position).normalized;aimedDirections[e]=direction;
                if(aimLines!=null&&e<aimLines.Length&&aimLines[e]!=null&&ClipSegment(zone,source.position,source.position+direction*100,out var a,out var b))
                {var line=aimLines[e];line.position=(a+b)*.5f;line.rotation=Quaternion.LookRotation(direction);line.localScale=new Vector3(.06f,.06f,Vector3.Distance(a,b));}
            }
        }
        bool FiresInVolley(int emitter, long volley)
        {
            // Authored batteries contain three bows; small fixtures still fire together.
            int groups = (emitters.Length + 2) / 3;
            return emitter / 3 == (int)(volley % groups);
        }
        void FireVolley()
        {
            for(int e=0;e<emitters.Length;e++)
            {
                var source=emitters[e];
                if(!FiresInVolley(e, aimedVolley))continue;
                if (source == null) continue;
                Vector3 direction = aimedDirections[e].sqrMagnitude>.01f?aimedDirections[e]:source.forward.normalized;
                if (!ClipSegment(zone, source.position, source.position + direction * 200, out var entry, out var exit)) continue;
                int slot = next++ % live.Length;
                positions[slot] = entry + direction * .01f; directions[slot] = direction;
                remaining[slot] = Mathf.Max(0, Vector3.Distance(entry, exit) - .02f); live[slot] = remaining[slot] > 0;
                projectileVisuals[slot].position = positions[slot]; projectileVisuals[slot].rotation = Quaternion.LookRotation(direction);
                projectileVisuals[slot].gameObject.SetActive(live[slot]);
            }
        }
        void StepArrow(int slot, float dt, double now)
        {
            float distance = Mathf.Min(remaining[slot], Mathf.Max(1, arrowSpeed) * dt);
            Vector3 from = positions[slot], direction = directions[slot];
            int count = Physics.RaycastNonAlloc(from, direction, hits, distance, ~0, QueryTriggerInteraction.Ignore);
            // An overflowing query may have omitted nearer cover; never apply a speculative hit.
            if (count == hits.Length) { live[slot] = false; BlockedShots++; return; }
            float closest = distance + .001f; Collider obstacle = null;
            for (int i = 0; i < count; i++)
            {
                var hit = hits[i];
                if (hit.collider == null || hit.collider == zone || hit.distance > closest) continue;
                bool ownVisual = false;
                foreach (var visual in projectileVisuals) if (hit.collider.transform.IsChildOf(visual)) { ownVisual = true; break; }
                if (ownVisual) continue;
                closest = hit.distance; obstacle = hit.collider;
            }
            if (obstacle != null)
            {
                positions[slot] += direction * closest; live[slot] = false;
                var actor = ImportedObstacleActor.Find(obstacle);
                if (actor == null) { BlockedShots++; return; } // Shields and fixed covers are ordinary solid colliders.
                if (actor.CanReceiveObstacle && (!lastHit.TryGetValue(actor, out double previous) || now - previous >= Mathf.Max(.1f, hitCooldown)))
                {
                    // A hand protruding into the zone cannot let a projectile hit its owner at a checkpoint.
                    Vector3 p = zone.transform.InverseTransformPoint(actor.Position) - zone.center, h = zone.size * .5f;
                    if (Mathf.Abs(p.x) <= h.x && Mathf.Abs(p.y) <= h.y && Mathf.Abs(p.z) <= h.z)
                    {
                        lastHit[actor] = now; PawnHits++;
                        actor.AddObstacleHit(direction * Mathf.Max(0, hitSpeed), Mathf.Max(0, stun));
                    }
                }
                return;
            }
            positions[slot] += direction * distance; remaining[slot] -= distance;
            if (remaining[slot] <= .001f) live[slot] = false;
        }
        void FixedUpdate()
        {
            if (live == null || ObstacleClock.Shared || (context != null && !context.Running)) { lastTime = Now; return; }
            if (context != null && seed != context.Seed) ResetGallery();
            double now = Now; float dt = (float)Math.Max(0, Math.Min(.1, now - lastTime)); lastTime = now;
            float warning = Mathf.Max(.1f, telegraphSeconds), period = Mathf.Max(warning + .5f, volleySeconds);
            long volley = (long)Math.Floor(now / period); float age = (float)(now - volley * period);
            Warning = age < warning;
            if(aimedVolley!=volley)Aim(volley);
            if(aimLines!=null)for(int e=0;e<aimLines.Length;e++)if(aimLines[e]!=null)aimLines[e].gameObject.SetActive(Warning && FiresInVolley(e, volley));
            if (age >= warning && lastVolley != volley) { lastVolley = volley; FireVolley(); }
            for (int i = 0; i < live.Length; i++)
            {
                if (live[i]) StepArrow(i, dt, now);
                projectileVisuals[i].position = positions[i]; projectileVisuals[i].gameObject.SetActive(live[i]);
            }
            tint.Clear(); ChessFight.Game.LitMaterials.SetColor(tint, Warning ? Color.white * (.7f + .3f * Mathf.Sin(age * 20)) : Color.gray);
            for(int e=0;e<bows.Length;e++)foreach(var bow in bows[e])if(bow!=null)bow.SetPropertyBlock(FiresInVolley(e, volley)?tint:null);
        }
        void OnDisable()
        {
            if(aimLines!=null)foreach(var line in aimLines)if(line!=null)line.gameObject.SetActive(false);
            if (projectileVisuals != null) foreach (var visual in projectileVisuals) if (visual != null) visual.gameObject.SetActive(false);
            if (bows != null) foreach (var renderers in bows) foreach (var bow in renderers) if (bow != null) bow.SetPropertyBlock(null);
        }
    }
}
