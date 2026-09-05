using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using SitraUtils;
using Pan.Util;
using Pan.StageGenerators;
using Pan.GridCompatibles2;
using System.Text;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine.Events;



namespace Pan.StageGenerators
{
    [Serializable]
    public class StageGeneratorSettingCalculate
    {
        ///======================================================================================================================================================



        //? 오브젝트 파괴



        ///<summary>
        ///  총 오브젝트 비동기 파괴 작업의 최대 허용 시간 (초 단위)
        ///</summary>        
        [TitleGroup("오브젝트 파괴"), BoxGroup("오브젝트 파괴/박스", false)]
        [LabelText("(비동기) 총 파괴 허용 시간 (초)")]
        [LabelWidth(250)]
        [InfoBox("비동기 파괴가 이 시간을 넘어가면, 나머지는 동기로 파괴된다")]
        [ShowInInspector]
        public float DestroyObjects_MaxSecondsAsync
        {
            get => destroyObjects_MaxSecondsAsync;
            private set => destroyObjects_MaxSecondsAsync = value;
        }
        [SerializeField, HideInInspector] private float destroyObjects_MaxSecondsAsync = 0.1f;



        ///<summary>
        /// 총 오브젝트 비동기 파괴 속도 강도(0~1 범위, 값이 클수록 빠르게 파괴)
        ///</summary>
        [TitleGroup("오브젝트 파괴"), BoxGroup("오브젝트 파괴/박스", false)]
        [LabelText("(비동기) 총 파괴 속도 강도")]
        [LabelWidth(250)]
        [InfoBox("값이 클수록 빠르게 파괴된다")]
        [PropertyRange(0f, 1f)]
        [ShowInInspector]
        public float DestroyObjects_DestroyLengthAsync
        {
            get => destroyObjects_DestroyLengthAsync;
            private set => destroyObjects_DestroyLengthAsync = value;
        }
        [SerializeField, HideInInspector] private float destroyObjects_DestroyLengthAsync = 0.5f;



        ///======================================================================================================================================================



        //? 그리드 생성



        ///<summary>
        ///총 그리드 비동기 생성 작업의 최대 허용 시간 (초 단위)
        ///</summary>
        [TitleGroup("그리드 생성"), BoxGroup("그리드 생성/박스", false)]
        [LabelText("(비동기) 총 생성 허용 시간 (초)")]
        [LabelWidth(250)]
        [InfoBox("비동기 그리드 생성이 이 시간을 넘어가면, 나머지는 동기로 생성된다")]
        [ShowInInspector]
        public float GridsGenerate_MaxSecondsAsync
        {
            get => gridsGenerate_MaxSecondsAsync;
            private set => gridsGenerate_MaxSecondsAsync = value;
        }
        [SerializeField, HideInInspector] private float gridsGenerate_MaxSecondsAsync = 0.1f;



        ///<summary>
        ///총 그리드 비동기 생성 속도 강도(0~1 범위, 값이 클수록 빠르게 생성)
        ///</summary>
        [TitleGroup("그리드 생성"), BoxGroup("그리드 생성/박스", false)]
        [LabelText("(비동기) 총 생성 강도")]
        [LabelWidth(250)]
        [InfoBox("값이 클수록 빠르게 생성된다")]
        [PropertyRange(0f, 1f)]
        [ShowInInspector]
        public float GridsGenerate_LengthAsync
        {
            get => gridsGenerate_LengthAsync;
            private set => gridsGenerate_LengthAsync = value;
        }
        [SerializeField, HideInInspector] private float gridsGenerate_LengthAsync = 0.5f;



        ///======================================================================================================================================================



        //? 공간



        ///<summary>
        ///공간을 분할할때, 이 횟수가 반복될때마다 프레임 양보를 한다
        /// </summary>
        [TitleGroup("공간"), BoxGroup("공간/박스", false)]
        [LabelText("(비동기)공간 분할 프레임 양보")]
        [LabelWidth(250)]
        [InfoBox("공간이 분할 될 때, 분할 횟수가 이 단위에 도달 할 때 마다 프레임을 양보한다")]
        [ShowInInspector]
        public int DivideSpaces_YieldUnitAsync
        {
            get => divideSpaces_YieldUnitAsync;
            private set => divideSpaces_YieldUnitAsync = value;
        }
        [SerializeField, HideInInspector] private int divideSpaces_YieldUnitAsync = 50;



        ///<summary>
        ///공간 생성 이후 후작업 (노드)의 병렬 배치 크기 (이 단위만큼 묶어서 병렬로 처리됨)
        /// </summary>
        [TitleGroup("공간"), BoxGroup("공간/박스", false)]
        [LabelText("노드 지정 BatchSize")]
        [LabelWidth(250)]
        [InfoBox("공간 생성 이후, 노드를 생성할때 이 단위만큼 묶어서 병렬로 처리한다")]
        [ShowInInspector]
        public int SpacePostSettingNode_BatchSize
        {
            get => spacePostSettingNode_BatchSize;
            private set => spacePostSettingNode_BatchSize = value;
        }
        [SerializeField, HideInInspector] private int spacePostSettingNode_BatchSize = 50;



        ///<summary>
        ///공간 생성 이후 후작업 (노드 재정렬)의 병렬 배치 크기 (이 단위만큼 묶어서 병렬로 처리됨)
        /// </summary>
        [TitleGroup("공간"), BoxGroup("공간/박스", false)]
        [LabelText("노드 정렬 BatchSize")]
        [LabelWidth(250)]
        [InfoBox("노드 생성 이후, 노드를 재정렬 할때 이 단위만큼 묶어서 병렬로 처리한다")]
        [ShowInInspector]
        public int SpacePostSettingSortNodes_BatchSize
        {
            get => spacePostSettingSortNodes_BatchSize;
            private set => spacePostSettingSortNodes_BatchSize = value;
        }
        [SerializeField, HideInInspector] private int spacePostSettingSortNodes_BatchSize = 50;



        ///======================================================================================================================================================



        //? 배치



        [TitleGroup("배치"), BoxGroup("배치/박스", false)]
        [LabelText("방 배치 Job 사용")]
        [LabelWidth(300)]
        [InfoBox("방이 배치될때 Job을 사용해 병렬로 배치된다")]
        [ShowInInspector]
        public bool RoomPlaceUseJob
        {
            get => roomPlaceUseJob;
            private set => roomPlaceUseJob = value;
        }
        [SerializeField, HideInInspector] private bool roomPlaceUseJob;



        [TitleGroup("배치"), BoxGroup("배치/박스", false)]
        [LabelText("방 배치 Job 전환")]
        [EnableIf(nameof(RoomPlaceUseJob))]
        [LabelWidth(300)]
        [InfoBox("방 개수가 이 값보다 적다면, Job으로 실행되지 않는다")]
        [ShowInInspector]
        [Indent(1)]
        public int RoomPlaceNotUseJobCount
        {
            get => roomPlaceNotUseJobCount;
            private set => roomPlaceNotUseJobCount = Mathf.Max(0, value);
        }
        [SerializeField, HideInInspector] private int roomPlaceNotUseJobCount = 50;



        ///<summary>
        /// 방 인접 을 위한 정렬을 한뒤, 크로노브레이크를 확인할때 이 횟수가 반복될때마다 프레임 양보를 한다
        /// </summary>
        [TitleGroup("배치"), BoxGroup("배치/박스", false)]
        [LabelText("(비동기) 방 인접 프레임 양보")]
        [LabelWidth(300)]
        [InfoBox("방 인접 정렬중, 순회 횟수가 이 단위에 도달 할 때 마다 프레임을 양보한다")]
        [ShowInInspector]
        public int RoomNearestYieldUnitAsync
        {
            get => roomNearestYieldUnitAsync;
            private set => roomNearestYieldUnitAsync = value;
        }
        [SerializeField, HideInInspector] private int roomNearestYieldUnitAsync = 50;



        ///<summary>
        ///도어로부터 생성되는 복도의 그리드를 설정할때, 그 방향의 도어의 개수가 이 값보다 높다면 그 방의 도어는 병렬로 실행된다
        /// </summary>
        [TitleGroup("배치"), BoxGroup("배치/박스", false)]
        [LabelText("방향별 도어&복도 그리드 배치 병렬 기준 개수")]
        [LabelWidth(300)]
        [InfoBox("도어로부터 생성되는 복도의 그리드를 설정할때, 그 방향의 도어의 개수가 이 값보다 높다면 그 방의 도어는 병렬로 실행된다")]
        [ShowInInspector]
        public int PlaceGrid_HallwaysFromDoor_ParallelCount
        {
            get => placeGrid_HallwaysFromDoor_ParallelCount;
            private set => placeGrid_HallwaysFromDoor_ParallelCount = value;
        }
        [SerializeField, HideInInspector] private int placeGrid_HallwaysFromDoor_ParallelCount = 50;



        ///======================================================================================================================================================
    }



    [CreateAssetMenu(menuName = CreateAssetMenuInfo.STAGEGEN_CALCULATE_SETTING)]
    public class StageGeneratorSettingCalculateSbject : ScriptableObject
    {
#if UNITY_EDITOR

        [ShowInInspector, HideLabel, DisplayAsString(Alignment = TextAlignment.Center, EnableRichText = true, Overflow = false), EnableGUI]
        [PropertyOrder(-100)]
        [PropertySpace(8, 8)]
        private string dummy_Title
        {
            get
            {
                return $"<b><size=15>스테이지 생성기 연산 설정 SO</size></b>";
            }
        }

#endif



        [SerializeField, InlineProperty, HideLabel]
        private StageGeneratorSettingCalculate setting;

        public StageGeneratorSettingCalculate Setting => setting;
    }
}