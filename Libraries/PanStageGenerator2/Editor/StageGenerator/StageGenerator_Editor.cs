using System.Collections;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Pan.GridCompatibles2;
using Pan.StageGenerators;
using Pan.StageGenerators.Editor;
using Pan.Util;
using Sirenix.OdinInspector.Editor;
using DG.Tweening;
using DG.DOTweenEditor;
using System.IO;
using Pan.Util.Editors;



namespace Pan.StageGenerators.Editor
{
    [CustomEditor(typeof(StageGenerator), true)]
    public class StageGenerator_Editor : EditorExpand_InspectorGUI<StageGenerator>
    {
        ///======================================================================================================================================================



        protected override EDrawDefaultInspectorMode? CurrentMode_DrawDefaultInspector_Fixed => EditorExpand_InspectorGUI<StageGenerator>.EDrawDefaultInspectorMode.OdinVisible;



        public StageGenerator_Editor()
        {
            AddGUIEvent_OnSceneGUI();
        }



        ///======================================================================================================================================================



        protected override void Awake()
        {
            base.Awake();
        }



        protected override void OnEnable()
        {
            base.OnEnable();

            //. 이 스테이지생성기를 선택할때마다, 전역 스테이지생성기 목록을 갱신한다
            isLoaded_InstancedStageGenerators = false;
            TryGetInstance_StageGenerators(out _);
        }



        private void OnDestroy()
        {
            //. 스테이지생성기가 선택할때마다, 전역 스테이지생성기 목록을 갱신한다
            isLoaded_InstancedStageGenerators = false;
            TryGetInstance_StageGenerators(out _);
        }



        ///======================================================================================================================================================



        //? 전역 스테이지 생성기



        private static StageGenerator[] instance_StageGenerators;

        private static bool isLoaded_InstancedStageGenerators;



        private static bool TryGetInstance_StageGenerators(out StageGenerator[] stageGenerators)
        {
            // ── 파괴된 참조가 남아 있다면 즉시 새로 검색하도록 플래그 설정
            if (!isLoaded_InstancedStageGenerators ||
                instance_StageGenerators == null ||
                Array.Exists(instance_StageGenerators, sg => !sg))             //! 파괴되면 Unity == null
            {
                instance_StageGenerators = FindObjectsByType<StageGenerator>(FindObjectsSortMode.None);
                isLoaded_InstancedStageGenerators = true;
            }

            stageGenerators = instance_StageGenerators;
            return stageGenerators != null && stageGenerators.Length > 0;


            ////? 불러온 스테이지 생성기들이 없거나, 플래그가 비활성화되어있을경우 (새롭게 스테이지 생성기를 불러와 갱신할 필요가 있을때 사용)
            //if (instance_StageGenerators == null || !isLoaded_InstancedStageGenerators)
            //{
            //    //. 새롭게 스테이지 생성기를 찾아, 할당한다
            //    instance_StageGenerators = FindObjectsByType<StageGenerator>(FindObjectsSortMode.None);

            //    //. 찾지 못했을경우, 실패한다
            //    if (instance_StageGenerators == null || instance_StageGenerators.Length == 0)
            //    {
            //        stageGenerators = null;
            //        isLoaded_InstancedStageGenerators = false;
            //        return false;
            //    }
            //    //? 찾았을경우, 플래그를 활성화하고 성공한다
            //    else
            //    {
            //        stageGenerators = instance_StageGenerators;
            //        isLoaded_InstancedStageGenerators = true;
            //        return true;
            //    }
            //}


            ////. 그 외
            //stageGenerators = instance_StageGenerators;
            //return stageGenerators != null;
        }



        ///======================================================================================================================================================



        //? 씬GUI 그리기



        private static bool isLoaded_OnSceneGUI;



        private static void AddGUIEvent_OnSceneGUI()
        {
            if (isLoaded_OnSceneGUI) { return; }
            isLoaded_OnSceneGUI = true;
            SceneView.duringSceneGui += OnSceneGUI_StageInfoBoxes;
        }



        ///======================================================================================================================================================



        //? 마우스에 위치한 스테이지 생성 정보 GUI 윈도우 그리기



        private static GUIStyle _guiStyle_DrawStageInfoBoxLabel;
        private static GUIStyle guiStyle_DrawStageInfoBoxLabel
        {
            get
            {
                _guiStyle_DrawStageInfoBoxLabel ??= new GUIStyle(GUI.skin.label) { richText = true };
                return _guiStyle_DrawStageInfoBoxLabel;
            }
        }


        private static GUIStyle _guiStyleDrawStageInfoBoxBox;
        private static GUIStyle guiStyle_DrawStageInfoBoxBox
        {
            get
            {
                _guiStyleDrawStageInfoBoxBox ??= _guiStyleDrawStageInfoBoxBox = new GUIStyle(GUI.skin.box);
                return _guiStyleDrawStageInfoBoxBox;
            }
        }


        private static StringBuilder _stageInfoStringBuilder;
        private static StringBuilder stageInfoStringBuilder
        {
            get
            {
                if (_stageInfoStringBuilder == null) _stageInfoStringBuilder = new StringBuilder();
                return _stageInfoStringBuilder;
            }
        }


        static int repaintFrameCount = 5;
        static int currentRepaintFrameCount = 0;


        private static void OnSceneGUI_StageInfoBoxes(SceneView sceneView)
        {
            //! 기즈모 사용 여부 확인
            if (!StageGeneratorSingletonSettingSbject.O.UseGizmo) { return; }

            if (TryGetInstance_StageGenerators(out var stageGenerators))
            {
                for (int i = 0; i < stageGenerators.Length; i++)
                {
                    var sg = stageGenerators[i];
                    if (!sg) { continue; }                                       //! 이미 Destroy 된 객체 건너뜀
                    OnSceneGUI_StageInfoBox(sceneView, sg);
                }
            }

            //? 마우스가 움직이는 n번째 프레임마다 Repaint를 실행한다
            //. 씬 전체 Repaint가 이루어진다
            if (Event.current.type == EventType.MouseMove)
            {
                currentRepaintFrameCount++;

                if (currentRepaintFrameCount >= repaintFrameCount)
                {
                    currentRepaintFrameCount = 0;
                    sceneView.Repaint();
                }
            }
        }



        private static void OnSceneGUI_StageInfoBox(SceneView sceneView, StageGenerator stageGenerator)
        {
            //! 스테이지 생성기 확인
            if (!stageGenerator.IsValid_TotalSettings ||
                !stageGenerator.GridM.IsValid_GridArray) { return; }


            //! 프리팹 모드인데, StageGenerator면 허용
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null)
            {
                GameObject prefabRoot = prefabStage.prefabContentsRoot;
                if (prefabRoot.GetComponent<StageGenerator>() == null) { return; }
            }


            //. 스테이지 생성 영역 위에 놓인 마우스 월드 좌표
            var calculatedMousePos =
                (Vector2)SU_Editor_Input.GetEditorMouseWorldPosition(stageGenerator.Setting.SnapSetting.Swizzle, stageGenerator.TransformM.StageCenterPositionTransform)
                .SwizzlesVector(stageGenerator.Setting.SnapSetting.Swizzle);


            //. 스테이지 생성기 의 좌표와 동기화되는 마우스 월드 좌표
            var calculatedMousePos_Synced = calculatedMousePos - (Vector2Int)stageGenerator.TransformM.StageParentSnappedPositionCurrent;//.StageParentPositionOriginal;


            //! 마우스가 스테이지 범위 안에 있는지 확인
            if (!stageGenerator.TransformM.StageTransformRect.ContainsPoint(calculatedMousePos, false)) { return; }


            bool useGridInfo = StageGeneratorSingletonSettingSbject.O.UseGridInfoTextMouseHovering;
            bool useSpaceInfo = StageGeneratorSingletonSettingSbject.O.UseSpaceInfoTextMouseHovering;



            //. 그리드 정보 띄우기
            if (useGridInfo)
            {
                var calculateMousePosition = (calculatedMousePos_Synced).SwizzlesVector2To3(stageGenerator.Setting.SnapSetting.Swizzle);
                calculateMousePosition -= stageGenerator.Setting.StageVector.StageInstancePointTransformCurrent;

                if (stageGenerator.Setting.SnapSetting.SnapToGridCellCenter)
                {
                    calculateMousePosition -= new Vector3(stageGenerator.Setting.SnapSetting.GridUnitX_Width * 0.5f, stageGenerator.Setting.SnapSetting.GridUnitY_Height * 0.5f, 0).SwizzlesVector(stageGenerator.Setting.SnapSetting.Swizzle);
                }

                //var calculateMousePosition = (calculatedMousePos_Synced - stageGenerator.Setting.StageVector.StageInstanceTransformPoint).SwizzlesVector2To3(stageGenerator.Setting.SnapSetting.Swizzle);


                //if (stageGenerator.Setting.SnapSetting.SnapToGridCellCenter)
                //{
                //    calculateMousePosition -= new Vector3(stageGenerator.Setting.SnapSetting.GridUnitX_Width * 0.5f, stageGenerator.Setting.SnapSetting.GridUnitY_Height * 0.5f, 0).SwizzlesVector(stageGenerator.Setting.SnapSetting.Swizzle);
                //}
                //calculateMousePosition += new Vector3(stageGenerator.Setting.SnapSetting.GridUnitX_Width * 0.5f, stageGenerator.Setting.SnapSetting.GridUnitY_Height * 0.5f, 0).SwizzlesVector(stageGenerator.Setting.SnapSetting.Swizzle);



                //. 스테이지 생성기 위의 마우스 월드 좌표를 그리드 좌표로 변환
                var gridPosition = stageGenerator.Setting.SnapSetting.CalculateGridPosition_byTransformPositionV3(calculateMousePosition);
                stageInfoStringBuilder.AppendLine(calculateMousePosition.ToString());
                stageInfoStringBuilder.AppendLine(gridPosition.ToString());


                //? 그리드 좌표가 유효하다면, 해당 그리드의 정보를 그리기
                if (stageGenerator.GridM.TryGet_GridPosition(gridPosition, out var mouseHoveringGrid))
                {
                    //? 그리드 정보 텍스트를 추가한다
                    stageInfoStringBuilder.AppendLine(calculatedMousePos.ToString());
                    stageInfoStringBuilder.AppendLine("<size=13><b><color=white>그리드 정보</color></b></size>");
                    stageInfoStringBuilder.Append("    ");
                    var tx = mouseHoveringGrid.GetGridInfoText_ForEditor(false).Replace("\n", "\n    ");
                    stageInfoStringBuilder.AppendLine(tx);
                }
            }

            //. 공간 정보 띄우기
            if (useSpaceInfo)
            {
                StageGenerator.Space space = null;

                for (int i = 0; i < stageGenerator.SpaceM.SpacesBinaryTree.Count; i++)
                {
                    var spaceStageGeneratorRect = stageGenerator.SpaceM.SpacesBinaryTree[i].SpaceTransformRect;

                    if (spaceStageGeneratorRect.ContainsPoint(calculatedMousePos_Synced, true))
                    {
                        space = stageGenerator.SpaceM.SpacesBinaryTree[i];
                        break;
                    }
                }

                if (space != null)
                {
                    if (useGridInfo) { stageInfoStringBuilder.AppendLine("─────"); }
                    stageInfoStringBuilder.AppendLine("<size=13><b><color=white>공간 정보</color></b></size>");
                    stageInfoStringBuilder.Append("    ");
                    var tx = space.GetSpaceInfoText_ForEditor().Replace("\n", "\n    ");
                    stageInfoStringBuilder.AppendLine(tx);
                }
            }

            if ((useGridInfo || useSpaceInfo) && stageInfoStringBuilder.Length != 0)
            {
                //? 드로우는 Repaint일 때만 - 드래그 중 포커스 탈취 방지
                var evt = Event.current;
                if (evt.type == EventType.Repaint /*|| evt.type == EventType.Layout*/)
                {
                    DrawGUI_DrawStageInfoBox_NoGUILayout(stageInfoStringBuilder.ToString(true));
                }
            }
            stageInfoStringBuilder.Clear();
            //if ((useGridInfo || useSpaceInfo) && stageInfoStringBuilder.Length != 0)
            //{
            //    //. 최종적으로 스테이지 정보 박스 GUI 그리기
            //    DrawGUI_DrawStageInfoBox(stageInfoStringBuilder.ToString(true));
            //}
            //stageInfoStringBuilder.Clear();
        }


        private static void DrawGUI_DrawStageInfoBox_NoGUILayout(string guiText)
        {
            Handles.BeginGUI();

            //. SceneView 영역/마우스 좌표
            Vector2 mousePosition = Event.current.mousePosition;

            //. 텍스트 사이즈 계산 (고정 레이아웃: GUIStyle.CalcSize)
            Vector2 size = guiStyle_DrawStageInfoBoxLabel.CalcSize(new GUIContent(guiText));
            float width = size.x + 30f; //. 패딩
            float height = size.y + 30f; //. 패딩

            //. 마우스 기준 오프셋 위치
            float x = mousePosition.x + 10f;
            float y = mousePosition.y + 20f;

            //. 반투명 배경
            Color originalBg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0f, 0f, 0f, 0.9f);

            //. 비인터랙티브 고정 그리기 (GUI는 기본적으로 포커스를 안가짐)
            var area = new Rect(x, y, width, height);
            GUI.Box(area, GUIContent.none, guiStyle_DrawStageInfoBoxBox);

            //. 라벨 영역(내부 패딩 10)
            var labelRect = new Rect(area.x + 10f, area.y + 10f, area.width - 20f, area.height - 20f);
            GUI.Label(labelRect, guiText, guiStyle_DrawStageInfoBoxLabel);

            GUI.backgroundColor = originalBg;
            Handles.EndGUI();
        }



        private static void _DrawGUI_DrawStageInfoBox(string guiText)
        {
            //. Handles.BeginGUI()와 Handles.EndGUI() 사이에 GUI 코드를 작성하여 Scene 뷰에 그립니다.
            Handles.BeginGUI();


            Rect sceneViewRect = SceneView.currentDrawingSceneView.position; //. Scene 뷰의 크기를 가져옵니다.            
            Vector2 mousePosition = Event.current.mousePosition; //. 마우스 위치를 가져옵니다.


            //. 텍스트 크기를 계산합니다.
            Vector2 size = guiStyle_DrawStageInfoBoxLabel.CalcSize(new GUIContent(guiText));
            float width = size.x + 30; //. 여백 추가
            float height = size.y + 30; //. 여백 추가


            //. 마우스 위치로부터 우측 하단으로 조금 떨어진 위치에 패널을 배치합니다.
            float x = mousePosition.x + 10;
            float y = mousePosition.y + 20;


            //. 반투명 배경 설정
            Color originalBackgroundColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0, 0, 0, 0.9f); //. 반투명 검정색 배경


            //. GUI 코드 실행
            GUILayout.BeginArea(new Rect(x, y, width, height), guiStyle_DrawStageInfoBoxBox);
            GUILayout.BeginVertical();


            //. 텍스트 색상을 흰색으로 설정하여 표시
            GUILayout.Label(guiText, guiStyle_DrawStageInfoBoxLabel);


            GUILayout.EndVertical();
            GUILayout.EndArea();


            //. 원래 배경색으로 복구
            GUI.backgroundColor = originalBackgroundColor;

            Handles.EndGUI();
        }



        ///======================================================================================================================================================
    }
}