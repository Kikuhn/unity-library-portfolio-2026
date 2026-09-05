using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Pan.GridCompatibles2;
using Pan.StageGenerators;



namespace Pan.StageGenerators
{
    public interface IRoomGizmoSetting
    {
        RoomGizmoSetting GetRoomGizmoSetting { get; }
    }



    [Serializable]
    public class RoomGizmoSetting : IRoomGizmoSetting
    {
        ///======================================================================================================================================================



        RoomGizmoSetting IRoomGizmoSetting.GetRoomGizmoSetting => this;



        ///======================================================================================================================================================



        public bool UseGizmo;
        public bool UseGizmo_UseTextOnlyPrefab = true;
        public bool UseGizmo_OnlySelected = true;



        public bool UseGizmo_UseGrid = true;
        public Color Grid_GridColor;
        public Color Grid_GridColor_DownLeft;
        public Color Grid_GridColor_UpRight;



        public bool UseGizmo_UseGrid_GridPosiiton = true;
        public Color Grid_GridPositionTextColor;
        public Vector3 Grid_GridPositionTextPosition;



        public bool UseGizmo_UseGrid_Posiiton = true;
        public Color Grid_PositionTextColor;
        public Vector3 Grid_PositionTextPosition;



        public bool UseTextMouseRadius;
        public float TextMouseRadius = 5f;



        public bool UseSwizzleAxisGizmo;



        public bool UseGizmo_UseRoomRect = true;
        public Color RoomRect_RectColor;
        public Color RoomRect_InfoTextColor;



        public bool UseGizmo_UseRoomRect_SafeArea = true;
        public Color RoomRect_RectSafeAreaColor;
        public Color RoomRect_RectSafeAreaTextColor;


        public bool UseGizmo_UseRoomRect_SafeAreaCurrent = true;
        public Color RoomRect_RectSafeAreaCurrentColor;
        public Color RoomRect_RectSafeAreaCurrentTextColor;



        public bool UseGizmo_UseRoomCenter = true;
        public Color RoomCenterColor;
        public Color RoomCenterTextColor;



        /// <summary>Gizmo 보정값 (float)</summary>
        public float Gizmo_CorrectionLength;



        /// <summary>Gizmo 보정값 (Vector3)</summary>
        public Vector3 Gizmo_CorrectionVector => new Vector3(Gizmo_CorrectionLength, Gizmo_CorrectionLength, Gizmo_CorrectionLength);



        ///======================================================================================================================================================



        public bool UseGizmo_Door;



        public Color Door_EnableColor;
        public Color Door_DisableColor;
        public Color Door_NodeColor;



        public float DoorGizmoTextPlusPositionCorrection = 0.5f;



        ///======================================================================================================================================================



        public bool UseGizmo_DoorGrid;
        public Color DoorGridColor;


        public bool UseGizmo_CorrectionHallwayGrid;
        public Color CorrectionHallwayGridColor;


        public bool UseGizmo_PathHallwayGrid;
        public Color PathHallwayGridColor;



        ///======================================================================================================================================================
    }



    [CreateAssetMenu(menuName = CreateAssetMenuInfo.ROOM_GIZMO)]
    public class RoomGizmoSettingSbject : ScriptableObject, RoomGizmoSettingSbject.IEdit, IRoomGizmoSetting
    {
        public interface IEdit
        {
            RoomGizmoSetting RoomGizmoSetting { get; set; }
        }



        [SerializeField] private RoomGizmoSetting RoomGizmoSetting;
        RoomGizmoSetting IEdit.RoomGizmoSetting { get => RoomGizmoSetting; set => RoomGizmoSetting = value; }
        public RoomGizmoSetting GetRoomGizmoSetting => RoomGizmoSetting;

    }
}