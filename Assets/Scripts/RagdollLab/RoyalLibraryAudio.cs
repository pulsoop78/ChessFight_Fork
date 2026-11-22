using System;
using System.Collections.Generic;
using ChessFight.Game;
using ChessFight.Gameplay;
using ChessFight.ProtectKing;
using UnityEngine;
namespace ChessFight.RagdollLab
{
    // Local presentation owned by this course instance, never a global music replacement.
    public sealed class RoyalLibraryAudio : MonoBehaviour
    {
        [SerializeField, Range(0,1)] float effectsVolume = .8f;
        [SerializeField, Range(0,1)] float ambienceVolume = .45f;
        [SerializeField, Range(0,1)] float thinkingVolume = .5f;
        [SerializeField, Min(.1f)] float fadeSeconds = 2;
        readonly List<AudioSource> sources = new List<AudioSource>();
        readonly List<Action> unbind = new List<Action>();
        GiantChessCourse course; RoyalLibraryPortal portal; CourseModule module;
        AudioSource ambience, thinking;
        AudioClip placement, portalClip;
        bool paused;
        float ambientLevel, thinkingLevel;
        AudioClip Clip(string name) => Resources.Load<AudioClip>("RoyalLibraryAudio/"+name);
        AudioSource Source(string name, Transform anchor, bool spatial)
        {
            var go=new GameObject(name);go.transform.SetParent(anchor,false);
            var source=go.AddComponent<AudioSource>();source.playOnAwake=false;
            source.spatialBlend=spatial?1:0;source.minDistance=8;source.maxDistance=90;
            source.rolloffMode=AudioRolloffMode.Linear;source.dopplerLevel=0;
            sources.Add(source);return source;
        }
        void Bind(Action<Action> subscribe, Action<Action> unsubscribe, Transform anchor, string clipName)
        {
            var source=Source(clipName,anchor,true);var clip=Clip(clipName);
            Action handler=()=>{if(isActiveAndEnabled && !paused && course!=null && !course.AudioPaused && clip!=null)
                source.PlayOneShot(clip,Mathf.Clamp01(effectsVolume)*GameSettings.Effects);};
            subscribe(handler);unbind.Add(()=>unsubscribe(handler));
        }
        void Start()
        {
            course=GetComponent<GiantChessCourse>();portal=GetComponent<RoyalLibraryPortal>();module=GetComponent<CourseModule>();
            if(course==null || portal==null || portal.Entrance==null || module==null || module.Exit==null){enabled=false;return;}
            foreach(var stamp in GetComponentsInChildren<CommandStampPress>())
                Bind(h=>stamp.Impacted+=h,h=>stamp.Impacted-=h,stamp.transform,"Stamp");
            foreach(var book in GetComponentsInChildren<ClosingBook>())
            {
                Bind(h=>book.Opened+=h,h=>book.Opened-=h,book.transform,"BookOpen");
                Bind(h=>book.Closed+=h,h=>book.Closed-=h,book.transform,"BookClose");
            }
            foreach(var key in GetComponentsInChildren<TypewriterKey>())
                Bind(h=>key.Pressed+=h,h=>key.Pressed-=h,key.transform,"Typewriter");
            placement=Clip("Placement");portalClip=Clip("Portal");
            // Fixed pool prevents one GameObject per chess landing and preserves overlapping decays.
            var landings=new AudioSource[4];for(int i=0;i<landings.Length;i++)landings[i]=Source("Chess landing "+i,transform,true);
            int next=0;
            Action<Vector3> landed=p=>{if(!isActiveAndEnabled || paused || course.AudioPaused || placement==null)return;
                var source=landings[next++%landings.Length];source.transform.position=p;
                source.PlayOneShot(placement,Mathf.Clamp01(effectsVolume)*GameSettings.Effects);};
            course.PieceLanded+=landed;unbind.Add(()=>course.PieceLanded-=landed);
            var crossing=Source("Portal crossing",transform,false);
            Action crossed=()=>{if(isActiveAndEnabled && !course.AudioPaused && portalClip!=null)
                crossing.PlayOneShot(portalClip,Mathf.Clamp01(effectsVolume)*GameSettings.Effects);};
            portal.Traversed+=crossed;unbind.Add(()=>portal.Traversed-=crossed);
            ambience=Source("Library white noise",transform,false);ambience.clip=Clip("Ambience");ambience.loop=true;ambience.volume=0;
            thinking=Source("GM thinking",transform,false);thinking.clip=Clip("Thinking");thinking.loop=true;thinking.volume=0;
        }
        void LateUpdate()
        {
            if(course==null || ambience==null)return;
            bool stop=course.AudioPaused;
            if(stop!=paused)
            {
                paused=stop;foreach(var source in sources)if(paused)source.Pause();else source.UnPause();
            }
            if(paused)return;
            var pawn=course.AudioPawn;bool inside=false,onBoard=false;
            if(pawn!=null && pawn.gameObject.activeInHierarchy && pawn.IsFinite())
            {
                var p=transform.InverseTransformPoint(pawn.Hips.position);
                var entry=portal.Entrance.InverseTransformPoint(pawn.Hips.position);
                var exit=module.Exit.InverseTransformPoint(pawn.Hips.position);
                inside=entry.z>=0 && exit.z<0 && p.y>-8 && Mathf.Abs(p.x)<162;
                onBoard=inside && p.z>=0 && p.z<course.AudioBoardLength+3 && Mathf.Abs(p.x)<course.AudioBoardLength*.5f+8;
            }
            float step=Time.deltaTime/Mathf.Max(.1f,fadeSeconds);
            ambientLevel=Mathf.MoveTowards(ambientLevel,inside?1:0,step);
            thinkingLevel=Mathf.MoveTowards(thinkingLevel,onBoard?1:0,step);
            Loop(ambience,ambientLevel*Mathf.Clamp01(ambienceVolume)*GameSettings.Music);
            Loop(thinking,thinkingLevel*Mathf.Clamp01(thinkingVolume)*GameSettings.Effects);
        }
        static void Loop(AudioSource source,float volume)
        {
            source.volume=volume;
            if(volume>0 && !source.isPlaying && source.clip!=null)source.Play();
            else if(volume<=0 && source.isPlaying)source.Stop();
        }
        void OnDisable()
        {
            foreach(var source in sources)if(source!=null)source.Stop();
            ambientLevel=thinkingLevel=0;paused=false;
        }
        void OnDestroy(){foreach(var release in unbind)release();}
    }
}
