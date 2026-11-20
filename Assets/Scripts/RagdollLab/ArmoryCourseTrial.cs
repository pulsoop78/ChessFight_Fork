using System;
using ChessFight.Game;
using ChessFight.Gameplay;
using ChessFight.ProtectKing;
using UnityEngine;
namespace ChessFight.RagdollLab
{
    // Offline prefab fixture. It drives the existing pawn; it owns no movement or grab implementation.
    public sealed class ArmoryCourseTrial : MonoBehaviour
    {
        [SerializeField] RagdollPawn pawnPrefab;
        [SerializeField] RagdollTuning tuning;
        [SerializeField] CourseModule course;
        [SerializeField] ArmoryCourseContext context;
        [SerializeField, Range(1,12)] int playerCount=12;
        [SerializeField, Min(.2f)] float respawnSeconds=1.5f;
        RagdollPawn[] players;
        ArmoryCarryShield[] equipped;
        int[] checkpoint,falls;
        float[] recovery;
        double[] elapsed;
        bool[] started,finished;
        LabCamera cameraRig;
        GameObject ownedCamera;
        Vector3 savedGravity;
        CursorLockMode savedLock;
        bool savedCursor,initialized,paused;
        int selected;
        GUIStyle hud;
        static readonly string[] Titles={"시작","창 바닥","듀라한 내려찍기","휴대 방패 사격장","갑옷 감시 회랑","도착"};
        static readonly string[] Hints={"각 장치 바로 앞에서 체크포인트를 저장합니다.","빛나는 칸을 보고 연결된 안전 칸으로 이동하세요.","무기 들어올림 → 내려찍기 → 회수 틈에 전진하세요.","가까운 방패를 E로 들고, 마우스로 석궁 방향을 바라보며 막으세요. E로 내려놓습니다.","눈이 켜진 시선 안에서는 정지하고, 돌아보면 엄폐 뒤로 이동하세요.","통과 완료. R로 현재 지점, F3로 새 창 배치와 전체 초기화."};
        void Awake()
        {
            if(course==null)course=GetComponent<CourseModule>();
            if(context==null)context=GetComponent<ArmoryCourseContext>();
            if(context!=null)context.Running=false;
            if(pawnPrefab==null || tuning==null || course==null || context==null || course.Checkpoints==null || course.Checkpoints.Length!=6)
            {Debug.LogError("Armory trial requires the existing ragdoll/tuning and six authored checkpoints.",this);enabled=false;return;}
            foreach(var point in course.Checkpoints)if(point==null){enabled=false;return;}
            cameraRig=FindFirstObjectByType<LabCamera>();
            if(cameraRig==null)
            {
                ownedCamera=new GameObject("Armory trial camera");ownedCamera.AddComponent<Camera>();cameraRig=ownedCamera.AddComponent<LabCamera>();
            }
            savedGravity=Physics.gravity;savedLock=Cursor.lockState;savedCursor=Cursor.visible;
            Physics.gravity=Vector3.down*9.81f*tuning.values.gravityScale;initialized=true;
        }
        void Start()
        {
            int n=Mathf.Clamp(playerCount,1,12);players=new RagdollPawn[n];equipped=new ArmoryCarryShield[n];checkpoint=new int[n];falls=new int[n];recovery=new float[n];elapsed=new double[n];started=new bool[n];finished=new bool[n];
            for(int i=0;i<n;i++)
            {
                var point=course.Checkpoints[0];var pawn=Instantiate(pawnPrefab,point.position,point.rotation);players[i]=pawn;
                pawn.name="Armory trial pawn "+(i+1);pawn.DisplayName="시험 "+(i+1);pawn.Team=i<6?0:1;pawn.tuning=tuning;
                if(pawn.GetComponent<ArmoryPawnIntent>()==null)pawn.gameObject.AddComponent<ArmoryPawnIntent>();
                foreach(var body in pawn.bodies){body.solverIterations=24;body.solverVelocityIterations=6;}
                Recover(i);
            }
            Select(0);AddCheckpointLabels();context.ResetRun(UnityEngine.Random.Range(1,int.MaxValue));context.Running=true;SetCursor();
        }
        void AddCheckpointLabels()
        {
            for(int i=0;i<6;i++)
            {
                var go=new GameObject("Checkpoint label "+i);go.transform.SetParent(course.Checkpoints[i],false);go.transform.localPosition=Vector3.up*2.8f;
                var label=go.AddComponent<TextMesh>();label.text=(i==0?"S":i==5?"GOAL":"C"+i)+" · "+Titles[i];label.font=RuntimePanels.KoreanFont;
                label.anchor=TextAnchor.MiddleCenter;label.alignment=TextAlignment.Center;label.fontSize=48;label.characterSize=.45f;label.color=Color.white;
                var render=go.GetComponent<MeshRenderer>();if(label.font!=null)render.sharedMaterial=label.font.material;
                float width=render.localBounds.size.x;if(width>7)go.transform.localScale=Vector3.one*(7/width);
            }
        }
        void Select(int i)
        {
            selected=(i+players.Length)%players.Length;cameraRig.soloTarget=players[selected];cameraRig.distance=9;cameraRig.maxDistance=24;
            var direction=course.Checkpoints[checkpoint[selected]].forward;cameraRig.yaw=Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg;cameraRig.pitch=18;
        }
        void Recover(int i)
        {
            DropShield(i);
            var pawn=players[i];var point=course.Checkpoints[checkpoint[i]];
            pawn.gameObject.SetActive(true);pawn.SetInput(default);
            // Scatter twelve recoveries across the full safe platform, including before the shield supply.
            Vector3 offset=new Vector3((i%4-1.5f)*2,0,(i/4-1)*1.2f);
            pawn.Teleport(point.TransformPoint(offset)+Vector3.up*(pawn.standHeight+.05f),point.forward);
            recovery[i]=0;
        }
        void DropShield(int i)
        {
            if(equipped==null || equipped[i]==null)return;
            if(equipped[i].EquippedBy==players[i].Hips.transform)equipped[i].Drop();
            equipped[i]=null;
        }
        void ToggleShield()
        {
            var pawn=players[selected];
            if(equipped[selected]!=null && equipped[selected].EquippedBy==pawn.Hips.transform)
            {DropShield(selected);return;}
            equipped[selected]=null;
            ArmoryCarryShield nearest=null;float distance=3*3;
            foreach(var shield in GetComponentsInChildren<ArmoryCarryShield>())
            {
                if(shield.Returning || shield.IsHeld)continue;
                float d=(shield.transform.position-pawn.Hips.position).sqrMagnitude;
                if(d<distance){nearest=shield;distance=d;}
            }
            if(nearest!=null && nearest.TryEquip(pawn.Hips.transform,pawn.GetComponentsInChildren<Collider>(),cameraRig.FlatForward))
                equipped[selected]=nearest;
        }
        void ResetRun()
        {
            context.ResetRun(UnityEngine.Random.Range(1,int.MaxValue));
            for(int i=0;i<players.Length;i++){checkpoint[i]=falls[i]=0;elapsed[i]=0;started[i]=finished[i]=false;Recover(i);}
            foreach(var shield in GetComponentsInChildren<ArmoryCarryShield>())shield.ResetShield();
            Select(selected);
        }
        bool Near(Transform point,Vector3 feet)
        {
            var p=point.InverseTransformPoint(feet);return Mathf.Abs(p.x)<8 && Mathf.Abs(p.z)<3.4f && p.y>-.25f && p.y<1.2f;
        }
        void Update()
        {
            if(players==null)return;
            if(Application.isFocused)
            {
                if(Input.GetKeyDown(KeyCode.Escape)){paused=!paused;SetCursor();}
                if(Input.GetKeyDown(KeyCode.Tab))Select(selected+1);
                if(Input.GetKeyDown(KeyCode.R))Recover(selected);
                if(Input.GetKeyDown(KeyCode.F3))ResetRun();
                for(int i=1;i<=4;i++)if(Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha0+i)))
                {checkpoint[selected]=i;finished[selected]=false;Recover(selected);Select(selected);}
            }
            context.Running=!paused && Application.isFocused;
            for(int i=0;i<players.Length;i++)
            {
                var pawn=players[i];pawn.SetInput(default);
                if(recovery[i]>0)
                {
                    if(context.Running)recovery[i]-=Time.deltaTime;
                    if(recovery[i]<=0)Recover(i);
                    continue;
                }
                if(!context.Running)continue;
                if(started[i]&&!finished[i])elapsed[i]+=Time.deltaTime;
                Vector3 feet=pawn.Hips.position-Vector3.up*pawn.standHeight;
                float height=transform.InverseTransformPoint(feet).y,cpHeight=transform.InverseTransformPoint(course.Checkpoints[checkpoint[i]].position).y;
                if(!pawn.IsFinite() || height<Mathf.Max(-5,cpHeight-4))
                {
                    DropShield(i);falls[i]++;recovery[i]=Mathf.Max(.2f,respawnSeconds);pawn.gameObject.SetActive(false);continue;
                }
                int next=checkpoint[i]+1;
                if(next<course.Checkpoints.Length && Near(course.Checkpoints[next],feet))
                {checkpoint[i]=next;if(next==5)finished[i]=true;}
            }
            if(!context.Running || recovery[selected]>0)return;
            if(Input.GetKeyDown(KeyCode.E))ToggleShield();
            Vector2 move=GameSettings.Move;
            var desired=Vector3.ClampMagnitude(cameraRig.FlatRight*move.x+cameraRig.FlatForward*move.y,1);
            if(desired.sqrMagnitude>.02f)started[selected]=true;
            players[selected].SetInput(new PawnInput {move=desired,aim=cameraRig.AimForward,jump=GameSettings.Pressed(GameKey.Jump),sprint=GameSettings.Held(GameKey.Sprint),
                shove=Input.GetMouseButtonDown(0),shoveHeld=Input.GetMouseButton(0),grab=Input.GetMouseButton(1) && (equipped[selected]==null || equipped[selected].EquippedBy==null)});
        }
        void FixedUpdate()
        {
            if(players==null || context==null || !context.Running)return;
            for(int i=0;i<players.Length;i++)
            {
                var pawn=players[i];
                if(!pawn.gameObject.activeInHierarchy)continue;
                if(equipped[i]!=null)
                {
                    if(equipped[i].EquippedBy!=pawn.Hips.transform)equipped[i]=null;
                    else if(i==selected)equipped[i].SetFacing(cameraRig.FlatForward);
                }
                Vector3 direction=Vector3.ProjectOnPlane(pawn.Aim,Vector3.up);
                if(direction.sqrMagnitude<.01f)direction=pawn.Facing;
                for(int hand=0;hand<2;hand++)
                {
                    var held=hand==0?pawn.handL.HeldCollider:pawn.handR.HeldCollider;
                    if(held!=null && held.attachedRigidbody!=null && held.GetComponentInParent<ArmoryCarryShield>()!=null)
                        held.attachedRigidbody.MoveRotation(Quaternion.LookRotation(direction.normalized,Vector3.up));
                }
            }
        }
        void SetCursor(){Cursor.lockState=paused?CursorLockMode.None:CursorLockMode.Locked;Cursor.visible=paused;}
        void OnApplicationFocus(bool focus){if(initialized&&!focus){paused=true;context.Running=false;SetCursor();}}
        void OnGUI()
        {
            if(players==null)return;
            if(hud==null)hud=new GUIStyle(GUI.skin.label){font=RuntimePanels.KoreanFont,fontSize=16,wordWrap=true};
            GUI.Box(new Rect(12,12,680,156),GUIContent.none);
            int stage=checkpoint[selected];var time=TimeSpan.FromSeconds(elapsed[selected]);
            GUI.Label(new Rect(26,20,650,143),$"무구실 · 회색 블록아웃 · 오프라인 검증\n시험 {selected+1}/{players.Length} · {Titles[stage]} · {time.Minutes:00}:{time.Seconds:00} · 낙하 {falls[selected]}회 · 씨앗 {context.Seed}\n{Hints[stage]}\nWASD 이동 · Space 점프 · 좌클릭 밀치기 · E 방패 들기/내리기 · 우클릭 잡기 · Shift 질주\nTab 캐릭터 · 1~4 구간 바로가기 · R 복귀 · F3 새 판 · Esc {(paused?"계속":"메뉴")}",hud);
            if(paused)GUI.Label(new Rect(26,183,650,35),"일시정지 · Esc로 계속",hud);
        }
        void OnDestroy()
        {
            if(context!=null)context.Running=false;
            if(players!=null)for(int i=0;i<players.Length;i++)DropShield(i);
            if(players!=null)foreach(var pawn in players)if(pawn!=null)Destroy(pawn.gameObject);
            if(ownedCamera!=null)Destroy(ownedCamera);
            if(!initialized)return;Physics.gravity=savedGravity;Cursor.lockState=savedLock;Cursor.visible=savedCursor;
        }
    }
}
