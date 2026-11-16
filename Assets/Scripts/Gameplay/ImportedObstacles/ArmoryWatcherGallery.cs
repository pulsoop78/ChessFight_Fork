using System.Collections.Generic;
using UnityEngine;
using ChessFight.Gameplay;
using ChessFight.Game;
namespace ChessFight.ProtectKing
{
    // StateDrivenMover: offline authored look-away cycle; only character-adapter intent can trigger a hit.
    public sealed class ArmoryWatcherGallery : Obstacle
    {
        [SerializeField] ArmoryCourseContext context;
        [SerializeField] BoxCollider zone;
        [SerializeField] Transform[] watchers;
        [SerializeField] Renderer[] eyes;
        [SerializeField] Transform[] sightFans;
        [SerializeField, Min(.5f)] float lookSeconds=2;
        [SerializeField, Min(.5f)] float lookAwaySeconds=2.8f;
        [SerializeField, Min(.1f)] float turnSeconds=.8f;
        [SerializeField, Min(.1f)] float warningSeconds=1.2f;
        [SerializeField, Min(1)] float range=18;
        [SerializeField, Range(10,160)] float viewAngle=80;
        [SerializeField, Min(0)] float hitSpeed=5;
        [SerializeField, Min(0)] float stun=1;
        [SerializeField, Min(.2f)] float hitCooldown=2;
        readonly Collider[] overlaps=new Collider[128];
        readonly HashSet<ImportedObstacleActor> unique=new HashSet<ImportedObstacleActor>();
        readonly Dictionary<ImportedObstacleActor,double> hitAt=new Dictionary<ImportedObstacleActor,double>();
        readonly RaycastHit[] sightHits=new RaycastHit[32];
        Quaternion[] facing;
        MaterialPropertyBlock tint;
        int seed=int.MinValue;
        double lastTime=-1;
        void Awake()
        {
            if(context==null)context=GetComponentInParent<ArmoryCourseContext>();
            if(zone==null || watchers==null || watchers.Length==0){enabled=false;return;}
            facing=new Quaternion[watchers.Length];for(int i=0;i<watchers.Length;i++)if(watchers[i]!=null)facing[i]=watchers[i].localRotation;
            tint=new MaterialPropertyBlock();
        }
        public static bool InCone(Vector3 delta,Vector3 forward,float distance,float angle)
        {
            delta=Vector3.ProjectOnPlane(delta,Vector3.up);
            return delta.sqrMagnitude<=distance*distance && (delta.sqrMagnitude<.001f || Vector3.Angle(delta,forward)<=angle*.5f);
        }
        bool Visible(Transform eye,ImportedObstacleActor actor,int index)
        {
            Vector3 origin=eyes!=null && index<eyes.Length && eyes[index]!=null ? eyes[index].bounds.center : eye.position+Vector3.up*1.3f;
            Vector3 target=actor.Position+Vector3.up*.8f,delta=target-origin;
            int n=Physics.RaycastNonAlloc(origin,delta.normalized,sightHits,delta.magnitude,~0,QueryTriggerInteraction.Ignore);
            // A saturated query is conservative: do not claim a clear line through unexamined cover.
            if(n==sightHits.Length)return false;
            for(int i=0;i<n;i++)
            {
                var hit=sightHits[i].collider;
                if(hit==null || hit.transform.IsChildOf(eye) || hit.transform.IsChildOf(actor.gameObject.transform))continue;
                return false;
            }
            return true;
        }
        void FixedUpdate()
        {
            if(context==null || !context.Running || ObstacleClock.Shared || facing==null)return;
            double now=context.Now;
            if(seed!=context.Seed || now<lastTime){seed=context.Seed;hitAt.Clear();}
            lastTime=now;float turn=Mathf.Max(.1f,turnSeconds),warn=Mathf.Max(.1f,warningSeconds);
            float cycle=Mathf.Max(.5f,lookSeconds)+Mathf.Max(.5f,lookAwaySeconds)+turn*2+warn;
            for(int w=0;w<watchers.Length;w++)
            {
                var eye=watchers[w];if(eye==null)continue;
                float phase=(float)((now+w*cycle*.5)%cycle);bool watching=phase<lookSeconds;
                float angle=phase<lookSeconds?0:phase<lookSeconds+turn?180*(phase-lookSeconds)/turn:phase<lookSeconds+turn+lookAwaySeconds?180:phase<lookSeconds+turn*2+lookAwaySeconds?180*(1-(phase-lookSeconds-turn-lookAwaySeconds)/turn):0;
                eye.localRotation=facing[w]*Quaternion.Euler(0,angle,0);
                bool warning=phase>=cycle-warn;
                if(sightFans!=null && w<sightFans.Length && sightFans[w]!=null)sightFans[w].gameObject.SetActive(watching || (warning && (int)(phase*6)%2==0));
                if(eyes!=null && w<eyes.Length && eyes[w]!=null)
                {tint.Clear();LitMaterials.SetColor(tint,watching||warning?Color.white:Color.gray);eyes[w].SetPropertyBlock(tint);}
                if(!watching)continue;
                var s=zone.transform.lossyScale;unique.Clear();
                int n=Physics.OverlapBoxNonAlloc(zone.transform.TransformPoint(zone.center),Vector3.Scale(zone.size*.5f,new Vector3(Mathf.Abs(s.x),Mathf.Abs(s.y),Mathf.Abs(s.z))),overlaps,zone.transform.rotation,~0,QueryTriggerInteraction.Ignore);
                for(int i=0;i<n;i++)
                {
                    var actor=ImportedObstacleActor.Find(overlaps[i]);
                    if(actor==null || !actor.CanReceiveObstacle || !unique.Add(actor))continue;
                    var intent=actor.gameObject.GetComponent<IArmoryVoluntaryMotion>();if(intent==null || !intent.MovingByChoice)continue;
                    Vector3 p=zone.transform.InverseTransformPoint(actor.Position)-zone.center,h=zone.size*.5f;
                    if(Mathf.Abs(p.x)>h.x || Mathf.Abs(p.y)>h.y || Mathf.Abs(p.z)>h.z || !InCone(actor.Position-eye.position,eye.forward,range,viewAngle))continue;
                    if(hitAt.TryGetValue(actor,out double last) && now-last<Mathf.Max(.2f,hitCooldown))continue;
                    if(!Visible(eye,actor,w))continue;
                    hitAt[actor]=now;actor.AddObstacleHit(-zone.transform.forward*Mathf.Max(0,hitSpeed)+Vector3.up*2,Mathf.Max(0,stun));
                }
            }
        }
    }
}
