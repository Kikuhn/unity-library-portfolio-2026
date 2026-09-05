using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Pan.Util;
using System;
using System.Text;
using Cysharp.Threading.Tasks;
using Pan.GridCompatibles2;
using Pan.StageGenerators;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System.Threading;




namespace Pan.StageGenerators
{
    public partial class StageGenerator : MonoBehaviour
    {
        [Serializable]
        public class GenerateManager : BaseManager
        {
            ///======================================================================================================================================================



            //? 생성중 정보



            [TitleGroup("생성"), BoxGroup("생성/박스", false)]
            [Sirenix.OdinInspector.ReadOnly, EnableGUI, DisplayAsString]
            [LabelText("생성중 여부")]
            [PropertyOrder(40)]
            [SerializeField]
            private bool isGenerating;



            #region 생성 로딩 바

            [TitleGroup("생성"), BoxGroup("생성/박스", false)]
            [ShowIf(nameof(isGenerating))]
            [HideLabel]
            [ProgressBar(0, 1, DrawValueLabel = false, CustomValueStringGetter = "@GenerateProgress_ProgressLabel", ColorGetter = nameof(GenerateProgress_ProgressColor))]
            [PropertyOrder(41)]
            [ShowInInspector]
            public float GenerateProgress
            {
                get
                {
                    if (!isGenerating) { return 0; }

                    //. 현재 단계 인덱스(0~TotalSteps)
                    int idx = GenerateProgress_GetStepIndex(GeneratingCurrentSteps);
                    return Mathf.Clamp01((float)idx / GenerateProgress_TotalSteps);
                }
            }


            //? 단계 → 1~8 인덱스 매핑 (None=0)
            private int GenerateProgress_GetStepIndex(GenerateSteps step)
            {
                switch (step)
                {
                    case GenerateSteps.GenStep1_Grids: return 1;
                    case GenerateSteps.GenStep2_Spaces: return 2;
                    case GenerateSteps.GenStep3_Places_Gen1_CreateRooms: return 3;
                    case GenerateSteps.GenStep4_Places_Gen2_LinkRooms: return 4;
                    case GenerateSteps.GenStep5_Places_Gen3_ConnectHallways: return 5;
                    case GenerateSteps.GenStep6_Places_Gen4_CreateHallways: return 6;
                    case GenerateSteps.GenStep7_Places_Gen5_PostGenerate: return 7;
                    case GenerateSteps.GenStep8_Places_Gen6_CurtainCall: return 8;
                    default: return 0;
                }
            }

            //. 총 단계 수 (None 제외)
            private const int GenerateProgress_TotalSteps = 8;

            //? 프로그레스 바에 표시될 문자열
            private string GenerateProgress_ProgressLabel
            {
                get
                {
                    int idx = GenerateProgress_GetStepIndex(GeneratingCurrentSteps);
                    string stepName = GenerateProgress_GetStepNameKo(GeneratingCurrentSteps);
                    //. "1 / 8 그리드 생성 작업중..." 형태
                    return idx == 0 ? "대기 중..." : $"{idx} / {GenerateProgress_TotalSteps}  {stepName} 작업중...";
                }
            }

            //? 진행도에 따라 색상 변화(선택)
            private Color GenerateProgress_ProgressColor
            {
                get
                {
                    float t = GenerateProgress;
                    //. 0→빨강, 1→초록 그라디언트
                    return Color.Lerp(new Color(0.85f, 0.25f, 0.25f), new Color(0.25f, 0.8f, 0.35f), t);
                }
            }

            //? 한글 라벨 (인스펙터 LabelText와 동일하게 노출하고 싶다면 여기서 관리)
            private string GenerateProgress_GetStepNameKo(GenerateSteps step)
            {
                switch (step)
                {
                    case GenerateSteps.GenStep1_Grids: return "그리드 생성";
                    case GenerateSteps.GenStep2_Spaces: return "공간 생성";
                    case GenerateSteps.GenStep3_Places_Gen1_CreateRooms: return "Gen1 방 생성";
                    case GenerateSteps.GenStep4_Places_Gen2_LinkRooms: return "Gen2 방 연결";
                    case GenerateSteps.GenStep5_Places_Gen3_ConnectHallways: return "Gen3 복도 연결";
                    case GenerateSteps.GenStep6_Places_Gen4_CreateHallways: return "Gen4 복도 생성";
                    case GenerateSteps.GenStep7_Places_Gen5_PostGenerate: return "Gen5 PostGenerate";
                    case GenerateSteps.GenStep8_Places_Gen6_CurtainCall: return "Gen6 커튼콜";
                    default: return "대기";
                }
            }


            #endregion



            public enum GenerateSteps
            {
                None = 0,
                [LabelText("Step1: 그리드 생성")]
                GenStep1_Grids = 1,

                [LabelText("Step2: 공간 생성")]
                GenStep2_Spaces = 2,

                [LabelText("Step3: Gen1 방 생성")]
                GenStep3_Places_Gen1_CreateRooms = 3,

                [LabelText("Step4: Gen2 방 연결")]
                GenStep4_Places_Gen2_LinkRooms = 4,

                [LabelText("Step5: Gen3 복도 연결")]
                GenStep5_Places_Gen3_ConnectHallways = 5,

                [LabelText("Step6: Gen4 복도 생성")]
                GenStep6_Places_Gen4_CreateHallways = 6,

                [LabelText("Step7: Gen5 PostGenerate")]
                GenStep7_Places_Gen5_PostGenerate = 7,

                [LabelText("Step8: Gen6 커튼콜")]
                GenStep8_Places_Gen6_CurtainCall = 8,
            }



            [TitleGroup("생성"), BoxGroup("생성/박스", false)]
            [HideInInspector]
            [PropertyOrder(51)]
            public GenerateSteps GeneratingCurrentSteps = GenerateSteps.None;



            [TitleGroup("생성"), BoxGroup("생성/박스", false)]
            [LabelText("생성 브레이크")]
            [PropertyTooltip("설정한 단계에서 생성이 중단된다")]
            [PropertyOrder(51)]
#if UNITY_EDITOR
            [GUIColor(nameof(editorGeneratingStepsBreakColor))]
#endif
            public GenerateSteps GeneratingStepsBreak = GenerateSteps.None;


#if UNITY_EDITOR
            private Color editorGeneratingStepsBreakColor
            {
                get
                {
                    if (GeneratingStepsBreak == GenerateSteps.None)
                    {
                        return Color.white;
                    }
                    else
                    {
                        return Color.red;
                    }
                }
            }
#endif



            ///======================================================================================================================================================



            //? 생성 [동기]



            ///<summary>절차적 생성을 시작한다 <see cref="StageParent"/>의 자식으로 오브젝트들이 생성된다</summary>
            ///<param name="customSeed">null이 아닐경우, 시드가 해당 값으로 생성된다, null일경우 설정값에 따라 시드를 부여받는다</param>
            public bool Generate(int? customSeed = null)
            {
                //! 애초에 스테이지 생성기 설정이 유효하지 않다면, 그냥 절대 실패
                if (!Main.IsValid_TotalSettings) { return false; }


                //. 생성 실행중 플래그 검사 및 활성화
                if (isGenerating) { return false; }
                isGenerating = true;


                try
                {
                    //. 초기화를 하고 시작한다
                    Main.Refresh_StageGenerator(true);


                    //. 생성 반복 횟수
                    int currentReGenerateCount = 0;


                    while (true)
                    {
                        //? 생성을 시도한다.
                        var executeGenerate = ExecuteGenerate(customSeed, (currentReGenerateCount <= 0) ? null : currentReGenerateCount);


                        //? 재생성이 필요하다면 하고, 필요하지 않다면 하지 않는다
                        //? null을 반환하면, 재생성이 시작된다
                        //? 그 외의 값은 즉시 그 값은 반환한다
                        var regenerateBranch = ReGenerateBranch(ref customSeed, executeGenerate, currentReGenerateCount);
                        if (regenerateBranch.HasValue) { return regenerateBranch.Value; }

                        currentReGenerateCount++;
                    }
                }
                finally
                {
                    isGenerating = false;
                }
            }



            /// <summary>
            /// 절차적 생성
            /// </summary>
            ///<param name="customSeed">null이 아닐경우, 시드가 해당 값으로 생성된다, null일경우 설정값에 따라 시드를 부여받는다</param>
            /// <param name="isRegenerateCount">절차적 생성이 재연산된 횟수 (null일경우, 첫 연산)</param>
            /// <returns></returns>
            private EGenerateState ExecuteGenerate(int? customSeed = null, int? isRegenerateCount = null)
            {
                //. 생성 성공 여부 기록
                EGenerateState state = EGenerateState.Failure;
                GeneratingCurrentSteps = GenerateSteps.None;


                try
                {
                    Main.LogM.StartLog(isRegenerateCount); //. 로깅 시작


                    //? 생성 전 파괴 설정이 되어있다면, 파괴
                    Main.LogM.Log_Head1_Ready(LOG_TITLE_DESTROYPREVIOUS);
                    if (!DestroyStage(
                        Main.GenerateInfoM.UseDestroyBeforeGenerate,
                        Main.GenerateInfoM.UseDestroyBeforeGenerate))
                    {
                        Main.LogM.Log_Head1_Failure(LOG_TITLE_DESTROYPREVIOUS);
                        state = EGenerateState.Failure;
                        throw new Exception($"<color=red><b> Generate 실패, 생성 전 파괴 실패</b></color>");
                    }
                    Main.LogM.Log_Head1_Success(LOG_TITLE_DESTROYPREVIOUS);


                    //? 커스텀 시드값을 받아왔다면, 해당 시드값을 적용
                    Main.LogM.Log_Head1_Ready(LOG_TITLE_REFRESHRANDOM);
                    if (!Main.GenerateInfoM.RefreshRandom(customSeed))
                    {
                        Main.LogM.Log_Head1_Failure(LOG_TITLE_REFRESHRANDOM);
                        state = EGenerateState.Failure;
                        throw new Exception($"<color=red><b> Generate 실패, 랜덤 시드 갱신 실패</b></color>");
                    }
                    Main.LogM.Log_RandomSeedInfo();
                    Main.LogM.Log_Head1_Success(LOG_TITLE_REFRESHRANDOM);


                    //? Grid 매니저 생성
                    Main.LogM.Log_Head1_Ready(LOG_TITLE_GENERATEGRIDMANAGER);
                    if (!Main.GridM.Generate_GridArray())
                    {
                        Main.LogM.Log_Head1_Failure(LOG_TITLE_GENERATEGRIDMANAGER);
                        state = EGenerateState.Failure;
                        throw new Exception($"<color=red><b> Generate 실패, 그리드 배열 생성 실패</b></color>");
                    }
                    Main.LogM.Log_Head1_Success(LOG_TITLE_GENERATEGRIDMANAGER);


                    if (GeneratingStepsBreak == GenerateSteps.GenStep1_Grids) { state = EGenerateState.Success; return state; } //! 생성 스텝 브레이크


                    //? Space 매니저 생성
                    Main.LogM.Log_Head1_Ready(LOG_TITLE_GENERATESPACELIST);
                    if (!Main.SpaceM.Generate_SpaceList(out var spaceList))
                    {
                        Main.LogM.Log_Head1_Failure(LOG_TITLE_GENERATESPACELIST);
                        state = EGenerateState.Failure;
                        throw new Exception($"<color=red><b> Generate 실패, 공간 리스트 생성 실패</b></color>");
                    }
                    Main.LogM.Log_SpaceInfo();
                    Main.LogM.Log_Head1_Success(LOG_TITLE_GENERATESPACELIST);


                    if (GeneratingStepsBreak == GenerateSteps.GenStep2_Spaces) { state = EGenerateState.Success; return state; } //! 생성 스텝 브레이크

                    //? Place 매니저 생성
                    Main.LogM.Log_Head1_Ready(LOG_TITLE_PLACE);
                    if (Main.PlaceM.TryGenerate_Place(spaceList, Main.TransformM.StageParent) == false)
                    {
                        Main.LogM.Log_Head1_Failure(LOG_TITLE_PLACE);
                        state = EGenerateState.Failure;
                        throw new Exception($"<color=red><b>Generate 실패, Place 생성 실패</b></color>");
                    }
                    Main.LogM.Log_Head1_Success(LOG_TITLE_PLACE);


                    //. 여기까지 왔으면 성공
                    state = EGenerateState.Success;
                }

                catch (Exception ex)
                {
                    Main.LogM.ErrorLog(ex); //. 에러 로깅

#if UNITY_EDITOR
                    Debug.LogError($"<color=red><b>Stage Generate ERROR!</b></color>\tSeed: <b>[ <color=#ed5565>{Main.GenerateInfoM.Seed_LastApplied}</color> ]</b>\n{ex}");
#endif
                }

                finally
                {
                    Main.LogM.EndLog(); //. 로깅 종료
                }

                GeneratingCurrentSteps = GenerateSteps.None;
                return state;
            }



            ///======================================================================================================================================================



            //? 생성 [비동기]



            ///<summary>절차적 생성을 시작한다 <see cref="StageParent"/>의 자식으로 오브젝트들이 생성된다</summary>
            ///<param name="customSeed">null이 아닐경우, 시드가 해당 값으로 생성된다, null일경우 설정값에 따라 시드를 부여받는다</param>
            public async UniTask<bool> GenerateAsync(int? customSeed = null)
            {
                //! 애초에 스테이지 생성기 설정이 유효하지 않다면, 그냥 절대 실패
                if (!Main.IsValid_TotalSettings) { return false; }


                //. 생성 실행중 플래그 검사 및 활성화
                if (isGenerating) { return false; }
                isGenerating = true;


                try
                {
                    await UniTask.SwitchToMainThread();


                    //. 초기화를 하고 시작한다 (설정SO의 DeepCopy, 매니저들의 WakeUp)
                    Main.Refresh_StageGenerator(true);


                    //. 생성 반복 횟수
                    int currentReGenerateCount = 0;


                    while (true)
                    {
                        //? 생성을 시도한다.
                        var executeGenerate = await ExecuteGenerateAsync(customSeed, (currentReGenerateCount <= 0) ? null : currentReGenerateCount);


                        //? 재생성이 필요하다면 하고, 필요하지 않다면 하지 않는다
                        //? null을 반환하면, 재생성이 시작된다
                        //? 그 외의 값은 즉시 그 값은 반환한다
                        var regenerateBranch = ReGenerateBranch(ref customSeed, executeGenerate, currentReGenerateCount);
                        if (regenerateBranch.HasValue) { return regenerateBranch.Value; }


                        currentReGenerateCount++;
                    }
                }
                finally
                {
                    isGenerating = false;
                }
            }



            /// <summary>
            /// 절차적 생성
            /// </summary>
            ///<param name="customSeed">null이 아닐경우, 시드가 해당 값으로 생성된다, null일경우 설정값에 따라 시드를 부여받는다</param>
            /// <param name="isRegenerateCount">절차적 생성이 재연산된 횟수 (null일경우, 첫 연산)</param>
            /// <returns></returns>
            private async UniTask<EGenerateState> ExecuteGenerateAsync(int? customSeed = null, int? isRegenerateCount = null)
            {
                //. 생성 성공 여부 기록
                EGenerateState state = EGenerateState.Failure;
                GeneratingCurrentSteps = GenerateSteps.None;

                try
                {
                    Main.LogM.StartLog(isRegenerateCount); //. 로깅 시작


                    //? 생성 전 파괴 설정이 되어있다면, 파괴
                    //. [비동기 O]
                    Main.LogM.Log_Head1_Ready(LOG_TITLE_DESTROYPREVIOUS);
                    if (!await DestroyStageAsync(
                        Main.GenerateInfoM.UseDestroyBeforeGenerate,
                        Main.GenerateInfoM.UseDestroyBeforeGenerate))
                    {
                        Main.LogM.Log_Head1_Failure(LOG_TITLE_DESTROYPREVIOUS);
                        state = EGenerateState.Failure;
                        throw new Exception($"<color=red><b> Generate 실패, 이전 생성 파괴 실패</b></color>");
                    }
                    Main.LogM.Log_Head1_Success(LOG_TITLE_DESTROYPREVIOUS);


                    //? 커스텀 시드값을 받아왔다면, 해당 시드값을 적용
                    Main.LogM.Log_Head1_Ready(LOG_TITLE_REFRESHRANDOM);
                    if (!Main.GenerateInfoM.RefreshRandom(customSeed))
                    {
                        Main.LogM.Log_Head1_Failure(LOG_TITLE_REFRESHRANDOM);
                        state = EGenerateState.Failure;
                        throw new Exception($"<color=red><b> Generate 실패, 랜덤 시드 갱신 실패</b></color>");
                    }
                    Main.LogM.Log_RandomSeedInfo();
                    Main.LogM.Log_Head1_Success(LOG_TITLE_REFRESHRANDOM);


                    //? Grid 매니저 생성
                    //. [비동기 O]
                    Main.LogM.Log_Head1_Ready(LOG_TITLE_GENERATEGRIDMANAGER);
                    if (!await Main.GridM.Generate_GridArrayAsync())
                    {
                        Main.LogM.Log_Head1_Failure(LOG_TITLE_GENERATEGRIDMANAGER);
                        state = EGenerateState.Failure;
                        throw new Exception($"<color=red><b> Generate 실패, 그리드 배열 생성 실패</b></color>");
                    }
                    Main.LogM.Log_Head1_Success(LOG_TITLE_GENERATEGRIDMANAGER);


                    if (GeneratingStepsBreak == GenerateSteps.GenStep1_Grids) { state = EGenerateState.Success; return state; } //! 생성 스텝 브레이크


                    //? Space 매니저 생성
                    //. [비동기 O]
                    Main.LogM.Log_Head1_Ready(LOG_TITLE_GENERATESPACELIST);
                    var spaceList = await Main.SpaceM.Generate_SpaceListAsync();
                    if (spaceList == null)
                    {
                        Main.LogM.Log_Head1_Failure(LOG_TITLE_GENERATESPACELIST);
                        state = EGenerateState.Failure;
                        throw new Exception($"<color=red><b> Generate 실패, 공간 리스트 생성 실패</b></color>");
                    }
                    Main.LogM.Log_SpaceInfo();
                    Main.LogM.Log_Head1_Success(LOG_TITLE_GENERATESPACELIST);


                    if (GeneratingStepsBreak == GenerateSteps.GenStep2_Spaces) { state = EGenerateState.Success; return state; } //! 생성 스텝 브레이크


                    //? Place 매니저 생성
                    //. [비동기 O]
                    Main.LogM.Log_Head1_Ready(LOG_TITLE_PLACE);
                    if (await Main.PlaceM.TryGenerate_PlaceAsync(spaceList, Main.TransformM.StageParent) == false)
                    {
                        Main.LogM.Log_Head1_Failure(LOG_TITLE_PLACE);
                        state = EGenerateState.Failure;
                        throw new Exception($"<color=red><b>Generate 실패, Place 메서드 실패</b></color>");
                    }
                    Main.LogM.Log_Head1_Success(LOG_TITLE_PLACE);


                    //. 여기까지 왔으면 성공
                    state = EGenerateState.Success;
                }

                catch (Exception ex)
                {
                    Main.LogM.ErrorLog(ex); //. 에러 로깅

#if UNITY_EDITOR
                    Debug.LogError($"<color=red><b>Stage Generate ERROR!</b></color>\tSeed: <b>[ <color=#ed5565>{Main.GenerateInfoM.Seed_LastApplied}</color> ]</b>\n{ex}");
#endif
                }

                finally
                {
                    Main.LogM.EndLog(); //. 로깅 종료
                }

                GeneratingCurrentSteps = GenerateSteps.None;
                return state;
            }



            ///======================================================================================================================================================



            //? 생성 공통 메서드



            /// <summary>
            /// 재생성 분기점,<br/>
            /// null: 재생성 시작<br/>
            /// true: 생성에 성공했으니, true를 반환하고 재생성되지 않음<br/>
            /// false: 생성에 절대 실패했으니, false를 반환하고 재생성되지 않음
            /// </summary>
            /// <param name="customSeed"></param>
            /// <param name="generateState"></param>
            /// <param name="currentReGenerateCount">이미 실행한 재시도 횟수. 최초 시도는 0입니다.</param>
            /// <returns></returns>
            private bool? ReGenerateBranch(
                ref int? customSeed,
                EGenerateState generateState,
                int currentReGenerateCount)
            {
                switch (generateState)
                {
                    //? 생성에 성공했다면, 즉시 true를 반환하여 성공적으로 종료 되게끔 한다
                    case EGenerateState.Success: return true;


                    //! 생성에 절대실패 했다면, 즉시 false를 반환하여 실패 되게끔 한다
                    case EGenerateState.AbsoluteFailure: return false;


                    //! 생성에 실패했다면, 실패 이벤트와 모드에 따라 이벤트 결정한다
                    case EGenerateState.Failure:

                    //! 재시도 한도에 도달했다면 시드를 변경하지 않고 종료한다
                    if (!CanReGenerate(currentReGenerateCount, Main.GenerateInfoM.ReGenerateMaxCount))
                    {
                        return false;
                    }

                    switch (Main.GenerateInfoM.FailureGenerateEvent)
                    {
                        //? 무조건 재생성
                        case EFailureGenerateEvent.Regenrate_Absolute:


                        //? 명시/설정 고정 시드는 첫 실패 뒤 해제하고 새 시드로 재시도한다
                        if (customSeed.HasValue || Main.GenerateInfoM.UseSeed_NextWillApplied)
                        {
                            Main.GenerateInfoM.RefreshRealyRandom();
                            customSeed = Main.GenerateInfoM.Random.Seed;
                        }
                        //? null을 반환해 재연산이 되게끔 한다
                        return null;


                        //? 시드 고정이 아니라면 재생성
                        case EFailureGenerateEvent.ReGenerate_WhenFixSeedNotUsed:


                        //! 시드 고정이 사용중이라면, 실패를 반환하게끔 한다
                        if (customSeed.HasValue || Main.GenerateInfoM.UseSeed_NextWillApplied) { return false; }

                        //? null을 반환해 재연산이 되게끔 한다
                        return null;


                        //! 무조건 실패
                        case EFailureGenerateEvent.FailureAbsolute: return false;
                    }


                    break;
                }

                return null;
            }



            /// <summary>
            /// 최초 생성 이후 추가 재시도를 실행할 수 있는지 확인합니다.
            /// </summary>
            internal static bool CanReGenerate(int currentReGenerateCount, int reGenerateMaxCount)
            {
                return currentReGenerateCount < Mathf.Max(0, reGenerateMaxCount);
            }



            ///======================================================================================================================================================



            //? 파괴



            /// <summary>생성된 오브젝트를 동기적으로 파괴하는 메서드</summary>
            /// <param name="destroyChildren">true이면 StageParent의 모든 자식 오브젝트를 파괴</param>
            /// <returns>파괴 작업이 완료되면 true 반환</returns>
            public bool DestroyStage(bool destroyChildren, bool destroyStageParentChilds = false)
            {
                //? Grid 및 Stage 데이터 초기화
                Main.GridM.ClearGridArray();
                Main.SpaceM.ClearSpaceManager();
                Main.PlaceM.ClearPlaceManager(true, true, false);


                #region 확실하게 파괴하기위해, StageParent 자식 검사 & 파괴

                if (destroyStageParentChilds)
                {
                    if (destroyChildren && Main.TransformM.StageParent.childCount > 0)
                    {
                        DestroyObjects(DetachStageChildren());
                    }
                }

                #endregion


                return true;
            }



            /// <summary>[비동기] 생성된 오브젝트를 파괴</summary>
            /// <param name="destroyChildren">true이면 StageParent의 모든 자식 오브젝트를 파괴</param>
            /// <returns>비동기 작업이 완료되면 true 반환</returns>
            public async UniTask<bool> DestroyStageAsync(bool destroyChildren, bool destroyStageParentChilds = false)
            {
                Main.GridM.ClearGridArray();
                Main.SpaceM.ClearSpaceManager();
                await Main.PlaceM.ClearPlaceManagerAsync(true, true, false);


                #region 확실하게 파괴하기위해, StageParent 자식 검사 & 파괴

                if (destroyStageParentChilds)
                {
                    if (destroyChildren && Main.TransformM.StageParent.childCount > 0)
                    {
                        await DestroyObjectsAsync(DetachStageChildren());
                    }
                }

                #endregion


                return true;
            }



            private Transform[] DetachStageChildren()
            {
                Transform stageParent = Main.TransformM.StageParent;
                Transform[] children = new Transform[stageParent.childCount];
                for (int i = 0; i < children.Length; i++)
                {
                    children[i] = stageParent.GetChild(i);
                }

                foreach (Transform child in children)
                {
                    child.gameObject.SetActive(false);
                    child.SetParent(null, true);
                }

                return children;
            }



            /// <summary>[비동기] 생성된 오브젝트를 파괴</summary>
            /// <param name="objectsToDestroy">파괴할 오브젝트 목록 (ICollection)</param>
            public void DestroyObjects<T>(ICollection<T> objectsToDestroy) where T : UnityEngine.Object
            {
                if (objectsToDestroy == null || objectsToDestroy.Count == 0) return;

                foreach (var obj in objectsToDestroy)
                {
                    obj?.DestroyAuto();
                }
            }



            /// <summary>[비동기] 생성된 오브젝트를 비동기적으로 파괴</summary>
            /// <param name="objectsToDestroy">파괴할 오브젝트 목록 (ICollection)</param>
            public async UniTask DestroyObjectsAsync<T>(ICollection<T> objectsToDestroy) where T : UnityEngine.Object
            {
                if (objectsToDestroy == null || objectsToDestroy.Count == 0) return;


                int totalObjects = objectsToDestroy.Count; //. 전체 오브젝트 개수
                float startTime = Time.realtimeSinceStartup; //. 시작 시간 기록


                int destroyedCount = 0; // 파괴된 오브젝트 카운트


                foreach (var obj in objectsToDestroy)
                {
                    float elapsedTime = Time.realtimeSinceStartup - startTime; //. 경과 시간 계산

                    //! 최대 허용 시간을 초과하면 남은 모든 오브젝트를 즉시 파괴
                    if (elapsedTime > CalculateSetting.DestroyObjects_MaxSecondsAsync)
                    {
                        foreach (var remainingObj in objectsToDestroy)
                        {
                            if (remainingObj == null) { continue; }

                            remainingObj.DestroyAuto();
                        }

                        break; //! 루프 종료
                    }

                    //? 남은 시간과 파괴 속도 강도에 따라 프레임당 파괴할 오브젝트 수 동적 계산
                    float remainingTime = CalculateSetting.DestroyObjects_MaxSecondsAsync - elapsedTime;
                    int dynamicDestroyCount = Mathf.Max(1, (int)(totalObjects * CalculateSetting.DestroyObjects_DestroyLengthAsync * remainingTime * 10));

                    //? 파괴할 개수만큼 처리
                    if (obj != null)
                    {
                        obj.DestroyAuto();
                        destroyedCount++;
                    }

                    //. 프레임당 최대 개수 도달 시 다음 프레임으로 넘어감
                    if (destroyedCount >= dynamicDestroyCount)
                    {
                        destroyedCount = 0; //. 카운트 초기화
                        await UniTask.Yield(PlayerLoopTiming.Update);
                    }
                }
            }



            ///======================================================================================================================================================



            //? 생성 버튼



            [TitleGroup("생성"), BoxGroup("생성/박스", false)]
            [ButtonGroup("생성/박스/생성버튼그룹")]
            [Button("생성", Icon = SdfIconType.PlusCircleFill), GUIColor(0.18f, 0.80f, 0.44f)]
            [PropertyOrder(51)]
            private void GenerateButton()
            {
                Generate();
            }



            [TitleGroup("생성"), BoxGroup("생성/박스", false)]
            [ButtonGroup("생성/박스/생성버튼그룹")]
            [Button("비동기 생성", Icon = SdfIconType.PlusCircle), GUIColor(0.18f, 0.80f, 0.44f)]
            [PropertyOrder(51)]
            private void GenerateAsyncButton()
            {
                GenerateAsync().Forget();
            }



            [TitleGroup("생성"), BoxGroup("생성/박스", false)]
            [HorizontalGroup("생성/박스/파괴버튼가로", 0.3f)]
            [LabelText("강제 파괴")]
            [PropertyTooltip("스테이지 부모 내에 있는 모든 게임 오브젝트를 파괴한다")]
            [PropertyOrder(52)]
            [SerializeField] private bool UseDestroyButton_AbsoluteDestroy;



            [TitleGroup("생성"), BoxGroup("생성/박스", false)]
            [HorizontalGroup("생성/박스/파괴버튼가로")]
            [ButtonGroup("생성/박스/파괴버튼가로/파괴버튼그룹")]
            [Button("파괴", Icon = SdfIconType.TrashFill), GUIColor(0.93f, 0.33f, 0.40f)]
            [PropertyOrder(52)]
            private void DestroyButton()
            {
                DestroyStage(UseDestroyButton_AbsoluteDestroy, UseDestroyButton_AbsoluteDestroy);
            }



            [TitleGroup("생성"), BoxGroup("생성/박스", false)]
            [HorizontalGroup("생성/박스/파괴버튼가로")]
            [ButtonGroup("생성/박스/파괴버튼가로/파괴버튼그룹")]
            [Button("비동기 파괴", Icon = SdfIconType.Trash), GUIColor(0.93f, 0.33f, 0.40f)]
            [PropertyOrder(52)]
            private void DestroyAsyncButton()
            {
                DestroyStageAsync(UseDestroyButton_AbsoluteDestroy, UseDestroyButton_AbsoluteDestroy).Forget();
            }



            [TitleGroup("생성"), BoxGroup("생성/박스", false)]
            [Button("모든 생성정보 일괄 제거", Icon = SdfIconType.Trash), GUIColor(0.67f, 0.57f, 0.93f)]
            [PropertyOrder(53)]
            public void ClearGeneratedInfoAll()
            {
                //? Place 초기화
                Main.PlaceM.ClearPlaceManager_GeneratedInfo();

                //? Grid 초기화
                Main.GridM.ClearGridArray();

                //? Space 초기화
                Main.SpaceM.ClearSpaceManager();
            }



            #region 생성 버스트 테스트



            [TitleGroup("생성"), BoxGroup("생성/박스", false)]
            [FoldoutGroup("생성/박스/버스트 생성 테스트")]
            [LabelText("버스트 횟수")]
            [PropertyOrder(52)]
            [SerializeField]
            [PropertySpace(SpaceBefore = 8)]
            private int GenerateBurstTestCounting = 0;

            [TitleGroup("생성"), BoxGroup("생성/박스", false)]
            [FoldoutGroup("생성/박스/버스트 생성 테스트")]
            [LabelText("버스트 간격")]
            [PropertyOrder(52)]
            [SerializeField]
            private float GenerateBurstTestDelaySeconds = 0.1f;


            //? 생성 버스트 캔슬러
            private bool GenerateBurstTest_Canceler = false;
            //? 생성 버스트중인지 확인
            private bool IsExecuting_GenerateBurstTest = false;




            [TitleGroup("생성"), BoxGroup("생성/박스", false)]
            [FoldoutGroup("생성/박스/버스트 생성 테스트")]
            [ButtonGroup("생성/박스/버스트 생성 테스트/버튼그룹")]
            [Button("버스트 생성 시작", Icon = SdfIconType.PlusCircleFill), GUIColor(0.18f, 0.80f, 0.44f)]
            [PropertyOrder(52)]
            private void GenerateBurstTestButton()
            {
                ExecuteGenerateBurstTest(false);
            }



            [TitleGroup("생성"), BoxGroup("생성/박스", false)]
            [FoldoutGroup("생성/박스/버스트 생성 테스트")]
            [ButtonGroup("생성/박스/버스트 생성 테스트/버튼그룹")]
            [Button("비동기 버스트 생성 시작", Icon = SdfIconType.PlusCircle), GUIColor(0.18f, 0.80f, 0.44f)]
            [PropertyOrder(52)]
            private void GenerateAsyncBurstTestButton()
            {
                ExecuteGenerateBurstTest(true);
            }



            [TitleGroup("생성"), BoxGroup("생성/박스", false)]
            [FoldoutGroup("생성/박스/버스트 생성 테스트")]
            [ButtonGroup("생성/박스/버스트 생성 테스트/버튼그룹")]
            [Button("버스트 중단하기", Icon = SdfIconType.StopBtnFill), GUIColor(0.93f, 0.33f, 0.40f)]
            [PropertyOrder(52)]
            [PropertySpace(SpaceAfter = 8)]
            private void StopGenerateAsyncBurstButton()
            {
                GenerateBurstTest_Canceler = true;
            }



            private void ExecuteGenerateBurstTest(bool useGenerateAsync)
            {
                if (IsExecuting_GenerateBurstTest) { Debug.Log("이미 생성 버스트가 실행중입니다."); return; }

                GenerateBurstTest_Canceler = false;
                IsExecuting_GenerateBurstTest = true;

                UniTask.RunOnThreadPool(async () =>
                {
                    await UniTask.SwitchToMainThread();

                    System.Diagnostics.Stopwatch stopwatch = new(); //? 실행 시간을 측정하기 위한 스톱워치

                    try
                    {
                        for (int i = 0; i < GenerateBurstTestCounting; i++)
                        {
                            stopwatch.Restart(); //? 스톱워치 시작

                            Debug.Log($"<b>생성 버스트</b> <b><color=#2ecc71>{i} / {GenerateBurstTestCounting}</color></b>\t Seed: <color=#ac92ec>{Main.GenerateInfoM.Seed_LastApplied}</color>");

                            if (GenerateBurstTest_Canceler)
                            {
                                Debug.Log($"<color=#ed5565><b>생성 버스트 중단</b></color> <color=#2ecc71>{i} / {GenerateBurstTestCounting}</color>");
                                break;
                            }

                            if (!useGenerateAsync)
                            {
                                if (!Generate())
                                {
                                    Debug.LogError($"<color=red>생성 버스트중 실패</color> 감지 <color=#2ecc71>{i} / {GenerateBurstTestCounting}</color>\t Seed: <color=#ac92ec>{Main.GenerateInfoM.Seed_LastApplied}</color>");
                                    break;
                                }
                            }
                            else
                            {
                                if (!await GenerateAsync())
                                {
                                    Debug.LogError($"<color=red>생성 버스트중 실패</color> 감지 <color=#2ecc71>{i} / {GenerateBurstTestCounting}</color>\t Seed: <color=#ac92ec>{Main.GenerateInfoM.Seed_LastApplied}</color>");
                                    break;
                                }
                            }

                            //if (Main.PlaceM.Reconstruction_ConnectHallways_ReportCount > 0)
                            //{
                            //    Debug.Log("복도 재구성 발견!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                            //    Debug.Log("복도 재구성 발견!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                            //    Debug.Log("복도 재구성 발견!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                            //    break;
                            //}

                            stopwatch.Stop(); //? 스톱워치 중지

                            Debug.Log($"<b><color=#2ecc71>{i} / {GenerateBurstTestCounting}</color></b> <b><color=#4fc1e9>실행 시간:</color></b> <b><color=#f7da64>{stopwatch.ElapsedMilliseconds}ms</color></b>");

                            await UniTask.WaitForSeconds(GenerateBurstTestDelaySeconds, true);
                        }
                    }
                    finally
                    {
                        Debug.Log("<b><color=#ed5565>생성 버스트 종료</color></b>");
                        IsExecuting_GenerateBurstTest = false;
                    }

                }).Forget();
            }



            #endregion



            ///======================================================================================================================================================
        }
    }
}
