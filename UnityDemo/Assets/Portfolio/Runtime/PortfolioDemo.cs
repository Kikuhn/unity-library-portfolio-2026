using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using Pan.Event;
using Pan.AddressableManagers;
using Pan.HighDensityElement;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace Portfolio2026
{
    public partial class PortfolioDemo : MonoBehaviour
    {
        public enum Case { Menu, Stage, Event, Addressables, Density, Spine }
        public Case SelectedCase;
        [Header("Presentation")]
        public Color Background = new Color(.045f,.08f,.11f);
        public Color Accent = new Color(.23f,.85f,.65f);
        public int PanelWidth = 440;
        [Header("Stage")]
        public int Seed = 12345;
        [Header("Addressables")]
        public string AssetKey = "portfolio-cube";
        [Header("Density")]
        [Range(100,20000)] public int ElementCount = 1000;
        public float ElementSpeed = 1.5f;
        public Material ElementMaterial;
        public Sprite ElementSprite;
        [Header("Spine")]
        public TextAsset RigJson;
        public float MixSeconds = .5f;
        public float AnimationSpeed = 1f;

        readonly Queue<string> messages = new Queue<string>();
        Font uiFont;
        GUIStyle titleStyle, textStyle, buttonStyle;
        DemoOwner ownerA,ownerB;
        DemoValue currentValue;
        Renderer[] ownerVisuals; Material ownerMaterial; MaterialPropertyBlock ownerColor;
        bool targetAlive = true;
        int lifecycle, requestId;
        readonly List<LoadRequest> loads = new List<LoadRequest>();
        readonly List<GameObject> loadedObjects = new List<GameObject>();
        ElementWorld world;
        ElementSpriteRenderer elementRenderer;
        ElementHandle firstElement;
        double tickMs;
        readonly Stopwatch watch = new Stopwatch();
        bool measuring;
        double measuredMs;
        int measuredTicks;
        float measureStarted;
        public float SimulationSeconds => Time.time;
        public int RenderedFrames => Time.frameCount;
        Vector2 logScroll;
        public string Status { get; private set; } = "Ready";
        public bool AutoFinished { get; private set; }
        public int Failures { get; private set; }

        void Start()
        {
            var args=Environment.GetCommandLineArgs();int caseIndex=Array.IndexOf(args,"-portfolioCase");
            if(SelectedCase==Case.Menu && caseIndex>=0 && caseIndex+1<args.Length && Enum.TryParse<Case>(args[caseIndex+1],out var requested) && requested!=Case.Menu)
            { SceneManager.LoadScene(requested.ToString());return; }
            Application.runInBackground = true;
            Application.targetFrameRate = 60;
            Camera.main.backgroundColor = Background;
            uiFont = Font.CreateDynamicFontFromOSFont(new[]{"Malgun Gothic","Arial"},20);
            if (SelectedCase == Case.Event) InitEvent();
            if (SelectedCase == Case.Density) ResetDensity();
            if (SelectedCase == Case.Stage) InitStage();
            if (SelectedCase == Case.Spine) InitSpine();
            Log("Unity " + Application.unityVersion + " / " + SelectedCase);
            bool autoRun = Array.IndexOf(Environment.GetCommandLineArgs(),"-portfolioAuto") >= 0;
#if UNITY_EDITOR
            autoRun |= UnityEditor.SessionState.GetBool("PortfolioCapture.Active",false);
#endif
            if (autoRun)
                StartCoroutine(AutoDemonstration());
        }

        void Update()
        {
            if (SelectedCase == Case.Spine) UpdateSpine();
            if(ownerVisuals!=null){ for(int i=0;i<2;i++){bool attached=DemoValue.TryPeek(i==0?ownerA:ownerB,out var value);ownerColor.SetColor("_BaseColor",attached?Accent:Color.gray);ownerVisuals[i].SetPropertyBlock(ownerColor);} }
            if (SelectedCase == Case.Density && measuring && Time.unscaledTime-measureStarted>12f)
            {
                measuring=false;
                string folder=Path.Combine(Application.persistentDataPath,"PortfolioMeasurements");
                Directory.CreateDirectory(folder);
                string file=Path.Combine(folder,"density-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".csv");
                File.WriteAllText(file,"unity,cpu,gpu,elements,fixed_dt,ticks,mean_tick_ms,build_type\n"+
                    $"{Application.unityVersion},{SystemInfo.processorType.Replace(',',' ')},{SystemInfo.graphicsDeviceName.Replace(',',' ')},{ElementCount},{Time.fixedDeltaTime},{measuredTicks},{measuredMs/Math.Max(1,measuredTicks):F5},{(Application.isEditor?"Editor":"Player")}\n");
                Log("Tick 평균 " +(measuredMs/Math.Max(1,measuredTicks)).ToString("F3")+" ms / CSV: "+file);
            }
        }

        void FixedUpdate()
        {
            if(world==null)return;
            watch.Restart(); world.Tick(Time.fixedDeltaTime); watch.Stop();
            tickMs=watch.Elapsed.TotalMilliseconds;
            if(measuring && Time.unscaledTime-measureStarted>2f){ measuredMs+=tickMs; measuredTicks++; }
        }

        void RenderElements(ScriptableRenderContext context, Camera camera)
        {
            if (world!=null && elementRenderer!=null && camera==Camera.main) elementRenderer.Render(world,camera);
        }

        public void Log(string message)
        {
            Status=message;logScroll.y=float.MaxValue;
            messages.Enqueue(Time.unscaledTime.ToString("F2")+"  "+message);
            while(messages.Count>9)messages.Dequeue();
            Debug.Log("[Portfolio/"+SelectedCase+"] "+message);
        }

        void Check(bool pass,string name)
        {
            if(!pass)Failures++;
            Log((pass?"PASS ":"FAIL ")+name);
        }

        void OnGUI()
        {
            if(uiFont==null)return;
            float scale=Mathf.Min(Screen.width/1440f,Screen.height/900f);
            GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));
            titleStyle ??= new GUIStyle(GUI.skin.label){font=uiFont,fontSize=29,fontStyle=FontStyle.Bold,wordWrap=true};
            textStyle ??= new GUIStyle(GUI.skin.label){font=uiFont,fontSize=18,wordWrap=true};
            buttonStyle ??= new GUIStyle(GUI.skin.button){font=uiFont,fontSize=18,padding=new RectOffset(12,12,9,9)};
            GUILayout.BeginArea(new Rect(26,22,PanelWidth,850),GUI.skin.box);
            GUILayout.Label("UNITY LIBRARY LAB",textStyle);
            GUILayout.Label(SelectedCase.ToString(),titleStyle);
            GUILayout.Label("정판영 · 기능 검증용 샘플",textStyle);
            GUILayout.Space(15);
            if(SelectedCase==Case.Menu)
            {
                GUILayout.Label("각 사례에서 실제 라이브러리를 호출합니다. 버튼과 Inspector 설정으로 입력을 바꿔 확인하세요.",textStyle);
                foreach(Case item in Enum.GetValues(typeof(Case)))if(item!=Case.Menu && Button(item.ToString()))SceneManager.LoadScene(item.ToString());
            }
            else
            {
                if(Button("← 사례 선택"))SceneManager.LoadScene("Menu");
                if(SelectedCase==Case.Event) EventUI();
                if(SelectedCase==Case.Addressables) AddressUI();
                if(SelectedCase==Case.Density) DensityUI();
                if(SelectedCase==Case.Stage) StageUI();
                if(SelectedCase==Case.Spine) SpineUI();
                if(Button("PNG 캡처")) StartCoroutine(Capture());
            }
            GUILayout.Space(12);
            GUILayout.Label("관측 로그",titleStyle);
            logScroll=GUILayout.BeginScrollView(logScroll);
            foreach(string message in messages)GUILayout.Label(message,textStyle);
            GUILayout.EndScrollView();
            GUILayout.EndArea();
            if(SelectedCase==Case.Event && ownerA!=null)
            {
                GUI.Label(new Rect(540,220,800,60),"TARGET A                            TARGET B",titleStyle);
                bool a=DemoValue.TryPeek(ownerA,out var av),b=DemoValue.TryPeek(ownerB,out var bv);
                GUI.Label(new Rect(540,620,800,100),"A: "+(a?"부착 #"+RuntimeHelpers.GetHashCode(av):"해제")+"          B: "+(b?"부착 #"+RuntimeHelpers.GetHashCode(bv):"해제"),textStyle);
            }
        }

        bool Button(string label)=>GUILayout.Button(label,buttonStyle,GUILayout.MinHeight(40));

        IEnumerator Capture()
        {
            yield return new WaitForEndOfFrame();
            string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../Recordings"));
            Directory.CreateDirectory(folder);
            ScreenCapture.CaptureScreenshot(Path.Combine(folder,SelectedCase+"-"+DateTime.Now.ToString("HHmmss")+".png"));
        }

        void InitEvent()
        {
            if(!PanEventGeneralManager.IsInitialize)PanEventGeneralManager.Initialize();
            ownerA=new DemoOwner("A"); ownerB=new DemoOwner("B");
            DemoValue.Report=Log;
            ownerVisuals=new Renderer[2]; ownerColor=new MaterialPropertyBlock();
            ownerMaterial=new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            for(int i=0;i<2;i++){var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name="Target "+(i==0?"A":"B");go.transform.SetParent(transform);go.transform.position=new Vector3(2+i*5,0,0);go.transform.localScale=Vector3.one*2;ownerVisuals[i]=go.GetComponent<Renderer>();ownerVisuals[i].sharedMaterial=ownerMaterial;}
            Log("대상 A / B 준비. 기능을 부착하세요.");
        }

        void Attach(DemoOwner owner)
        {
            try { currentValue=owner.EventAble.Require<DemoValue>(); Log(owner.Name+" value #"+RuntimeHelpers.GetHashCode(currentValue)+" enabled="+currentValue.Valid_CurrentEventAble); }
            catch(Exception e){Log("예상된 활성화 실패: "+e.Message);}
        }

        void Remove(DemoOwner owner)
        {
            if(owner!=null)DemoValue.Remove(owner);
            Log(owner.Name+" 해제 요청");
        }

        void EventUI()
        {
            GUILayout.Label("풀링된 기능의 대상과 활성 상태를 관찰합니다.",textStyle);
            if(Button("A에 부착 / 같은 타입 다시 요구"))Attach(ownerA);
            if(Button("A에서 해제"))Remove(ownerA);
            if(Button("B에 부착"))Attach(ownerB);
            if(Button("B에서 해제"))Remove(ownerB);
            if(Button("활성화 예외 → 재시도"))
            {
                Remove(ownerA); Remove(ownerB); DemoValue.ThrowNext=true; Attach(ownerA); Attach(ownerA);
            }
            GUILayout.Label("현재 값: "+(currentValue==null?"없음":RuntimeHelpers.GetHashCode(currentValue)+" / active="+currentValue.Valid_CurrentEventAble),textStyle);
        }

        void BeginLoad(string key)
        {
            targetAlive=true;
            var request=new LoadRequest{Id=++requestId,Version=lifecycle};
            loads.Add(request);
            request.Handle=PanAddressableNative.LoadAssetAsyncHandle<GameObject>(key);
            Log("request #"+request.Id+" 시작 / version "+lifecycle);
            request.Handle.Completed += handle =>
            {
                if(!handle.IsValid()){Log("이미 반환된 핸들");return;}
                Log("request #"+request.Id+" 완료 "+handle.Status);
                if(this!=null && targetAlive && request.Version==lifecycle && handle.Status==AsyncOperationStatus.Succeeded)
                {
                    loadedObjects.Add(Instantiate(handle.Result,new Vector3(3,0,0),Quaternion.identity));
                    request.Applied=true; Log("현재 대상에 결과 적용");
                }
                else Release(request);
            };
        }

        void Release(LoadRequest request)
        {
            if(request.Released)return;
            request.Released=true;
            if(request.Handle.IsValid())Addressables.Release(request.Handle);
            Log("request #"+request.Id+" 반환 1회 / applied="+request.Applied);
        }

        void ClearTarget()
        {
            targetAlive=false; lifecycle++;
            foreach(var item in loadedObjects)if(item!=null)Destroy(item);
            loadedObjects.Clear();
            foreach(var request in loads)if(request.Handle.IsValid() && request.Handle.IsDone)Release(request);
            Log("대상 해제 / version "+lifecycle+" / 미완료 요청은 완료 시 반환");
        }

        void AddressUI()
        {
            GUILayout.Label("완료 시 요청 수명을 재확인하고, 유효 핸들을 소유자가 반환합니다.",textStyle);
            if(Button("정상 로딩"))BeginLoad(AssetKey);
            if(Button("요청 직후 대상 해제")){BeginLoad(AssetKey);ClearTarget();}
            if(Button("실패 key 로딩"))BeginLoad("portfolio-missing-key");
            if(Button("대상 해제 / 초기화"))ClearTarget();
            GUILayout.Label("대상 alive="+targetAlive+" / version="+lifecycle+"\n적용 객체="+loadedObjects.Count,textStyle);
        }

        void ResetDensity()
        {
            RenderPipelineManager.beginCameraRendering-=RenderElements;
            elementRenderer?.Dispose(); world?.Dispose();
            world=new ElementWorld(ElementCount);
            var registry=new ElementVisualRegistry();
            registry.Register(1,new ElementVisualDefinition(ElementSprite,ElementMaterial));
            elementRenderer=new ElementSpriteRenderer(registry,initialCapacity:ElementCount,maximumInstancesPerBatch:511,spatialChunkSize:16f);
            if(!ElementCompiledArchetype.TryCreate(ElementCapabilities.KinematicMotion2D|ElementCapabilities.SpriteVisual2D,.08f,100f,1,0,false,PhysicsCoreBodyMode.Kinematic,PhysicsCoreShape2D.Circle,out var archetype,out var failure))throw new InvalidOperationException(failure.ToString());
            var random=new System.Random(Seed);
            for(int i=0;i<ElementCount;i++)
            {
                var builder=ElementSpawnBuilder.From(archetype).WithPose(new float2((float)random.NextDouble()*13f-3f,(float)random.NextDouble()*12f-6f),new float2(.07f))
                    .WithVelocity(new float2((float)random.NextDouble()-.5f,(float)random.NextDouble()-.5f)*ElementSpeed).WithColor(new Color32(65,(byte)(150+i%100),210,255));
                var result=world.Spawn(builder); if(!result.Succeeded)throw new InvalidOperationException(result.Status.ToString());
                if(i==0)firstElement=result.Handle;
            }
            RenderPipelineManager.beginCameraRendering+=RenderElements;
            Log(ElementCount+"개 실제 Element 생성");
        }

        void DensityUI()
        {
            GUILayout.Label("Native 상태 + Burst Job + 라이브러리 Sprite instancing",textStyle);
            ElementCount=(int)GUILayout.HorizontalSlider(ElementCount,100,20000);
            GUILayout.Label("생성 수 "+ElementCount,textStyle);
            if(Button("설정으로 다시 생성"))ResetDensity();
            if(Button("2초 예열 + 10초 Tick 측정")){measuredMs=0;measuredTicks=0;measureStarted=Time.unscaledTime;measuring=true;}
            GUILayout.Label("최근 Tick "+tickMs.ToString("F3")+" ms\n렌더 인스턴스 "+elementRenderer?.LastInstanceCount+" / 배치 "+elementRenderer?.LastBatchCount,textStyle);
            GUILayout.Label("Tick 구간의 CPU 경과시간입니다. 전체 프레임/GPU 비용과 다릅니다. 녹화와 분리해 Player에서 측정하세요.",textStyle);
        }

        IEnumerator AutoDemonstration()
        {
            yield return new WaitForSeconds(2);
            if(SelectedCase==Case.Stage)
            {
                var rooms=Stage.GetComponentsInChildren<Pan.StageGenerators.RoomObject>();
                Check(rooms.Length>1,"multiple rooms");
                var positions=Array.ConvertAll(rooms,r=>r.transform.position.ToString("F3"));Array.Sort(positions);
                GenerateStage();yield return null;
                var repeated=Array.ConvertAll(Stage.GetComponentsInChildren<Pan.StageGenerators.RoomObject>(),r=>r.transform.position.ToString("F3"));Array.Sort(repeated);
                Check(string.Join(";",positions)==string.Join(";",repeated),"same-seed room positions");
                Stage.GenerateM.DestroyStage(true,true);yield return null;
                Check(Stage.GetComponentsInChildren<Pan.StageGenerators.RoomObject>().Length==0,"stage reset");
                Seed++;GenerateStage();
            }
            if(SelectedCase==Case.Spine)
            {
                Check(rig!=null && rig.IsValid,"Spine initialization");
                PlayMotion("swing");yield return new WaitForSeconds(.25f);
                Check(rig.AnimationState.GetTrack(0).Animation.Name=="swing","motion switch");
                facingLeft=true;yield return new WaitForSeconds(.25f);Check(rig.transform.localScale.x<0,"direction change");
                PlayMotion("idle");yield return new WaitForSeconds(.25f);PlayMotion("swing");
                Check(rig.AnimationState.GetTrack(0).Animation.Name=="swing","motion reentry");
            }
            if(SelectedCase==Case.Event)
            {
                Attach(ownerA);var first=currentValue;Attach(ownerA);Check(ReferenceEquals(first,currentValue),"same-type identity");
                Remove(ownerA);Check(!first.Valid_CurrentEventAble,"detach");yield return new WaitForSeconds(.5f);Attach(ownerB);Check(currentValue.Valid_CurrentEventAble,"reuse");Remove(ownerB);
                DemoValue.ThrowNext=true;Attach(ownerA);Attach(ownerA);Check(currentValue.Valid_CurrentEventAble,"exception recovery");
                Remove(ownerA);DemoValue nested=null;
                DemoValue.EnableProbe=()=>{DemoValue.EnableProbe=null;nested=ownerA.EventAble.Require<DemoValue>();};
                Attach(ownerA);Check(ReferenceEquals(nested,currentValue),"enable reentry identity");
            }
            if(SelectedCase==Case.Addressables)
            {
                BeginLoad(AssetKey);yield return new WaitForSeconds(2);Check(loadedObjects.Count==1,"normal load");ClearTarget();
                BeginLoad(AssetKey);ClearTarget();yield return new WaitForSeconds(2);Check(loadedObjects.Count==0,"late result rejected");
                Check(loads.TrueForAll(r=>r.Released),"completed handles returned");
                BeginLoad(AssetKey);ClearTarget();BeginLoad(AssetKey);yield return new WaitForSeconds(1);
                Check(loadedObjects.Count==1,"replacement target receives only current result");ClearTarget();
                BeginLoad("portfolio-missing-key");yield return new WaitForSeconds(1);
                Check(loadedObjects.Count==0 && loads[loads.Count-1].Released,"missing key recovered and returned");
            }
            if(SelectedCase==Case.Density){Check(firstElement.IsAlive,"live element");world.TryGetPose(firstElement.Key,out var before);yield return new WaitForSeconds(1);world.TryGetPose(firstElement.Key,out var after);Check(!before.Position.Equals(after.Position),"native motion");Check(elementRenderer.LastInstanceCount>0,"render submission");}
            yield return new WaitForSeconds(2);
            yield return Capture();
            Log("AUTO FINISHED failures="+Failures); AutoFinished=true;
            if(!Application.isEditor)
            {
                string directory=Path.GetFullPath(Path.Combine(Application.dataPath,"../PlayerVerification"));Directory.CreateDirectory(directory);
                File.WriteAllText(Path.Combine(directory,SelectedCase+".json"),"{\"scene\":\""+SelectedCase+"\",\"failures\":"+Failures+",\"completed\":true}");
                yield return new WaitForSecondsRealtime(1);
                Application.Quit(Failures==0?0:2);
            }
        }

        void OnDestroy()
        {
            if(SelectedCase==Case.Addressables)ClearTarget();
            if(SelectedCase==Case.Event){Remove(ownerA);Remove(ownerB);DemoValue.Report=null;DemoValue.EnableProbe=null;}
            RenderPipelineManager.beginCameraRendering-=RenderElements;
            elementRenderer?.Dispose();world?.Dispose();
            DisposeSpine();
            if(uiFont!=null)Destroy(uiFont); if(ownerMaterial!=null)Destroy(ownerMaterial);
        }

        sealed class LoadRequest { public int Id,Version; public bool Released,Applied; public AsyncOperationHandle<GameObject> Handle; }
        sealed class DemoOwner:IEventAble { public string Name; public EventAble EventAble{get;} public DemoOwner(string name){Name=name;EventAble=new EventAble(this,4);} }
    }

    public sealed class DemoValue:PanBaseEventValue.EventAbles<DemoValue>
    {
        public static Action<string> Report;
        public static Action EnableProbe;
        public static bool ThrowNext;
        protected override void Enable(){if(ThrowNext){ThrowNext=false;throw new InvalidOperationException("Injected sample failure");}EnableProbe?.Invoke();Report?.Invoke("Enable #"+RuntimeHelpers.GetHashCode(this));}
        protected override void Disable(){Report?.Invoke("Disable #"+RuntimeHelpers.GetHashCode(this));}
    }
}






