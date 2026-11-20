using System.Collections.Generic;
using ChessFight.Gameplay;
using UnityEngine;

namespace ChessFight.ProtectKing
{
    // StateDrivenMover: a capped offline supply shield, equipped locally or held through existing grab joints.
    [RequireComponent(typeof(Rigidbody))]
    public sealed class ArmoryCarryShield : Obstacle
    {
        [SerializeField] ArmoryCourseContext context;
        [SerializeField] Rigidbody body;
        [SerializeField] Transform spawnPoint;
        [SerializeField] BoxCollider allowedArea;
        [SerializeField, Min(.2f)] float recoverySeconds = 3;
        [SerializeField, Min(1)] float abandonedSeconds = 12;
        [SerializeField, Min(1)] float lostBelowSpawn = 8;
        [SerializeField, Min(5), Tooltip("Recall even a held shield after this loan to prevent permanent supply denial.")] float maximumLoanSeconds = 35;
        readonly Collider[] nearby = new Collider[512];
        readonly List<Joint> joints = new List<Joint>();
        [SerializeField, Min(.2f)] float carryDistance = .9f;
        Transform carrier;
        Collider[] carrierColliders;
        Vector3 carryFacing = Vector3.forward;
        public Transform EquippedBy => carrier;
        Collider[] solids;
        Vector3 restPosition;
        Quaternion restRotation;
        double recoveryAt = -1, lastHeldAt, releaseAt, loanAt = -1;
        int seed;
        bool resetting;
        public bool Returning => resetting;
        public bool IsHeld
        {
            get
            {
                if (carrier != null) return true;
                if (body == null) return false;
                int count = Physics.OverlapSphereNonAlloc(body.position, 4, nearby, ~0, QueryTriggerInteraction.Ignore);
                // A saturated query cannot safely establish that a shield is free.
                if (count == nearby.Length) return true;
                for (int i = 0; i < count; i++)
                {
                    var rb = nearby[i].attachedRigidbody;
                    if (rb == null || rb == body) continue;
                    rb.GetComponents(joints);
                    foreach (var joint in joints) if (joint != null && joint.connectedBody == body) return true;
                }
                return false;
            }
        }
        double Now => context != null ? context.Now : SharedTime;
        void Awake()
        {
            if (body == null) body = GetComponent<Rigidbody>();
            solids = GetComponentsInChildren<Collider>();
            restPosition = spawnPoint != null ? spawnPoint.position : body.position;
            restRotation = spawnPoint != null ? spawnPoint.rotation : body.rotation;
            body.isKinematic = false; body.useGravity = true;
            body.mass = 2; body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            lastHeldAt = Now; seed = context != null ? context.Seed : 0;
        }
        public bool TryEquip(Transform holder, Collider[] ownerColliders, Vector3 facing)
        {
            if (holder == null || body == null || solids == null || Returning || IsHeld || ObstacleClock.Shared) return false;
            carrier = holder; carrierColliders = ownerColliders;
            SetFacing(facing);
            SetOwnerCollision(true);
            body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            body.isKinematic = true; body.useGravity = false;
            // Snap only on pickup; subsequent movement follows the physics tick.
            body.position = carrier.position + carryFacing * Mathf.Max(.2f, carryDistance);
            body.rotation = Quaternion.LookRotation(carryFacing);
            return true;
        }
        public void SetFacing(Vector3 facing)
        {
            facing.y = 0;
            if (Finite(facing) && facing.sqrMagnitude > .001f) carryFacing = facing.normalized;
        }
        void SetOwnerCollision(bool ignore)
        {
            if (solids == null || carrierColliders == null) return;
            foreach (var solid in solids) foreach (var owner in carrierColliders)
                if (solid != null && owner != null && solid != owner) Physics.IgnoreCollision(solid, owner, ignore);
        }
        public void Drop()
        {
            if (carrierColliders == null && carrier == null) return;
            SetOwnerCollision(false); carrier = null; carrierColliders = null;
            if (body == null) return;
            body.isKinematic = false; body.useGravity = true;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero;
        }
        bool InSupplyBounds()
        {
            if (!Finite(body.position) || body.position.y < restPosition.y - Mathf.Max(1, lostBelowSpawn)) return false;
            if (allowedArea == null) return true;
            Vector3 p = allowedArea.transform.InverseTransformPoint(body.position) - allowedArea.center;
            Vector3 h = allowedArea.size * .5f;
            return Mathf.Abs(p.x) <= h.x && Mathf.Abs(p.y) <= h.y && Mathf.Abs(p.z) <= h.z;
        }
        static bool Finite(Vector3 p) => !float.IsNaN(p.x) && !float.IsInfinity(p.x) && !float.IsNaN(p.y) && !float.IsInfinity(p.y) && !float.IsNaN(p.z) && !float.IsInfinity(p.z);
        public void ResetShield()
        {
            if (body == null || solids == null) return;
            Drop();
            // PawnHand sees its disabled target collider and releases on its next physics step.
            // Do not teleport a body while a character still has a joint attached to it.
            resetting = true; releaseAt = Now + Mathf.Max(.05f, Time.fixedDeltaTime * 2);
            foreach (var solid in solids) if (solid != null) solid.enabled = false;
        }
        void FixedUpdate()
        {
            if (ObstacleClock.Shared || (context != null && !context.Running)) return;
            double now = Now;
            if (context != null && seed != context.Seed) { seed = context.Seed; ResetShield(); }
            if (resetting)
            {
                if (now < releaseAt || IsHeld) return;
                body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero;
                body.position = restPosition; body.rotation = restRotation;
                foreach (var solid in solids) if (solid != null) solid.enabled = true;
                resetting = false; recoveryAt = -1; loanAt = -1; lastHeldAt = now;
                return;
            }
            if (carrier != null)
            {
                if (!carrier.gameObject.activeInHierarchy) Drop();
                else
                {
                    body.MovePosition(carrier.position + carryFacing * Mathf.Max(.2f, carryDistance));
                    body.MoveRotation(Quaternion.LookRotation(carryFacing));
                }
            }
            else if (carrierColliders != null) Drop();
            bool held = IsHeld;
            if (held) lastHeldAt = now;
            bool atSupply = (body.position - restPosition).sqrMagnitude < 4;
            if (held || !atSupply) { if (loanAt < 0) loanAt = now; }
            else loanAt = -1;
            bool overdue = loanAt >= 0 && now - loanAt >= Mathf.Max(5, maximumLoanSeconds);
            bool missing = overdue || !InSupplyBounds() || (!held && !atSupply && now - lastHeldAt >= Mathf.Max(1, abandonedSeconds));
            if (!missing) { recoveryAt = -1; return; }
            if (recoveryAt < 0) recoveryAt = now + Mathf.Max(.2f, recoverySeconds);
            if (now >= recoveryAt) ResetShield();
        }
        void OnDisable()
        {
            Drop();
            // Restore collision without relocating a grabbed body.
            if (solids != null) foreach (var solid in solids) if (solid != null) solid.enabled = true;
            resetting = false; recoveryAt = -1;
        }
    }
}
