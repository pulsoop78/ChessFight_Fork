using UnityEngine;
using ChessFight.Gameplay;
namespace ChessFight.ProtectKing
{
    // Offline trial clock, isolated per prefab. Never installs or replaces the shared match clock.
    [DefaultExecutionOrder(-200)]
    public sealed class ArmoryCourseContext : MonoBehaviour
    {
        [SerializeField] int seed = 17023;
        public int Seed => seed;
        public double Now { get; private set; }
        public bool Running { get; set; }
        public void ResetRun(int newSeed) { seed=newSeed;Now=0; }
        void FixedUpdate() { if(Running && !ObstacleClock.Shared) Now+=Time.fixedDeltaTime; }
    }
    // The character adapter separates intended locomotion from hits, carrying and passive body motion.
    public interface IArmoryVoluntaryMotion { bool MovingByChoice { get; } }
}
