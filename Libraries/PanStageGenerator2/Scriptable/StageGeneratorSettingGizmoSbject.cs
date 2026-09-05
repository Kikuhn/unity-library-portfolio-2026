using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Pan.GridCompatibles2;
using Pan.StageGenerators;
using Pan.Util;
using System.Reflection;


namespace Pan.StageGenerators
{
    public interface IStageGeneratorSettingGizmo
    {
        StageGeneratorSettingGizmo Setting { get; }
    }



    [Serializable]
    public class StageGeneratorSettingGizmo : IStageGeneratorSettingGizmo
    {
        ///======================================================================================================================================================



        StageGeneratorSettingGizmo IStageGeneratorSettingGizmo.Setting => this;



        ///======================================================================================================================================================

        public bool UseGizmo;


        public bool UseGizmo_Spaces;

        public bool UseGizmo_SpaceRect;
        public Color SpaceRectColor;

        public bool UseGizmo_SpaceNode;
        public Color SpaceNodeColor;

        public bool UseGizmo_SpaceInRoomNode;
        public Color SpaceInRoomNodeColor;

        public bool UseGizmo_SpaceInfoText;
        public Color SpaceInfoTextColor;

        public bool UseGizmo_SpaceRoomRect;
        public Color SpaceRoomRectColor;



        public bool UseGizmo_Grids;

        public bool UseGizmo_AllGrid;
        public Color AllGridColor;

        public bool UseGizmo_OccupiedGrid;
        public Color OccupiedGridColor;

        public bool UseGizmo_PathCostGrid;
        public Color PathCostGridColor;

        public bool UseGizmo_WasBannedMainPath;
        public Color WasBannedMainPathColor;

        public bool UseGizmo_WasBanned_CollisionOtherHallways;
        public Color WasBannedCollisionOtherHallwaysColor;

        public bool UseGizmo_RoomGrid;
        public Color RoomGridColor;

        public bool UseGizmo_RoomSafeAreaGrid;
        public Color RoomSafeAreaGridColor;

        public bool UseGizmo_RoomExpandSafeAreaGrid;
        public Color RoomExpandSafeAreaGridColor;

        public bool UseGizmo_HallwayGrid;
        public Color HallwayGridColor;

        public bool UseGizmo_HallwayEdgeGrid;
        public Color HallwayEdgeGridColor;

        public bool UseGizmo_HallwaySafeAreaGrid;
        public Color HallwaySafeAreaGridColor;


        public bool UseGizmo_RoomDoorGrid;
        public Color RoomDoorGridColor;


        public bool UseGizmo_DrawNearGridInfoFromMouse;
        public float DrawNearGridInfoFromMouseRadius;
        public Color DrawNearGridInfoFromMouseTextColor;


        public bool UseGizmo_MouseDrawGUI_GridInfo;



        public bool UseGizmo_HallwayNavMesh;
        public Color HallwayNavMeshColor;



        public Color CustomTempDrawGridGizmoColor = Color.white;


        public Color DrawTempCacheRoomRects_RoomRectColor = Color.white;
        public Color DrawTempCacheRoomRects_RoomSafeAreaRectColor = Color.white;
        public Color DrawTempCacheRoomRects_RoomSafeAreaExpandRectColor = Color.white;



        ///======================================================================================================================================================



        ///<summary>
        ///모든 색깔 설정을, 보색설정 (텍스트 제외!)
        /// </summary>
        public void ConvertAllColor_Complementary()
        {
            foreach (FieldInfo field in GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                // 필드 타입이 Color인지 확인
                if (field.FieldType == typeof(Color) && !field.Name.Contains("Text"))
                {
                    // 필드의 값을 가져오기
                    Color colorValue = (Color)field.GetValue(this);

                    colorValue.ApplyComplementaryColor();

                    // 필드 값을 다시 설정
                    field.SetValue(this, colorValue);
                }
            }


        }



        ///======================================================================================================================================================
    }



    [CreateAssetMenu(menuName = CreateAssetMenuInfo.STAGEGEN_GIZMO_SETTING)]
    public class StageGeneratorSettingGizmoSbject : ScriptableObject, IStageGeneratorSettingGizmo
    {
        [SerializeField] private StageGeneratorSettingGizmo setting;

        public StageGeneratorSettingGizmo Setting => setting;
    }
}