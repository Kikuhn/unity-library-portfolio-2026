using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;



namespace Pan.StageGenerators
{
    public abstract class StageGenerateCustomEventBase_PostEvent : BaseStageGenerateCustomEvent
    {
        ///======================================================================================================================================================



        /// <summary>
        /// Post이벤트: 방 오브젝트
        /// </summary>
        /// <param name="main"></param>
        /// <param name="placeObjects_Room"></param>
        public abstract void PostEvent_PlaceObjects_Room(StageGenerator main, StageGenerator.PlaceManger.PlaceObjectList<RoomObject> placeObjects_Room);



        /// <summary>
        /// [비동기] Post이벤트: 방 오브젝트
        /// </summary>
        /// <param name="main"></param>
        /// <param name="placeObjects_Room"></param>
        public abstract UniTask PostEvent_PlaceObjects_RoomAsync(StageGenerator main, StageGenerator.PlaceManger.PlaceObjectList<RoomObject> placeObjects_Room);



        ///======================================================================================================================================================



        /// <summary>
        /// Post이벤트: 복도 오브젝트
        /// </summary>
        /// <param name="main"></param>
        /// <param name="placeObjects_hallway"></param>
        public abstract void PostEvent_PlaceObjects_Hallway(StageGenerator main, StageGenerator.PlaceManger.PlaceObjectList placeObjects_hallway);



        /// <summary>
        /// [비동기] Post이벤트: 복도 오브젝트
        /// </summary>
        /// <param name="main"></param>
        /// <param name="placeObjects_hallway"></param>
        public abstract UniTask PostEvent_PlaceObjects_HallwayAsync(StageGenerator main, StageGenerator.PlaceManger.PlaceObjectList placeObjects_hallway);



        ///======================================================================================================================================================



        /// <summary>
        /// Post이벤트: 복도 테두리 오브젝트
        /// </summary>
        /// <param name="main"></param>
        /// <param name="placeObjects_hallwayEdge"></param>
        public abstract void PostEvent_PlaceObjects_HallwayEdge(StageGenerator main, StageGenerator.PlaceManger.PlaceObjectList placeObjects_hallwayEdge);



        /// <summary>
        /// [비동기] Post이벤트: 복도 테두리 오브젝트
        /// </summary>
        /// <param name="main"></param>
        /// <param name="placeObjects_hallwayEdge"></param>
        public abstract UniTask PostEvent_PlaceObjects_HallwayEdgeAsync(StageGenerator main, StageGenerator.PlaceManger.PlaceObjectList placeObjects_hallwayEdge);



        ///======================================================================================================================================================



        /// <summary>
        /// Post이벤트: ETC 오브젝트
        /// </summary>
        /// <param name="main"></param>
        /// <param name="placeObjects_ETC"></param>
        public abstract void PostEvent_PlaceObjects_ETC(StageGenerator main, StageGenerator.PlaceManger.PlaceObjectList placeObjects_ETC);



        /// <summary>
        /// [비동기] Post이벤트: ETC 오브젝트
        /// </summary>
        /// <param name="main"></param>
        /// <param name="placeObjects_ETC"></param>
        public abstract UniTask PostEvent_PlaceObjects_ETCAsync(StageGenerator main, StageGenerator.PlaceManger.PlaceObjectList placeObjects_ETC);



        ///======================================================================================================================================================



        /// <summary>
        /// Post이벤트: 확장 그리드
        /// </summary>
        /// <param name="main"></param>
        /// <param name="customExpandGrids"></param>
        public abstract void PostEvent_CustomExpandGrid(StageGenerator main, HashSet<StageGenerator.Grid> customExpandGrids);



        /// <summary>
        /// [비동기] Post이벤트: 확장 그리드
        /// </summary>
        /// <param name="main"></param>
        /// <param name="customExpandGrids"></param>
        public abstract UniTask PostEvent_CustomExpandGridAsync(StageGenerator main, HashSet<StageGenerator.Grid> customExpandGrids);



        ///======================================================================================================================================================



        /// <summary>
        /// Post이벤트: Post이벤트 전 의 <b>전체</b> 그리드 이벤트
        /// </summary>
        /// <param name="main"></param>
        /// <param name="gridManager"></param>
        public abstract void PostEvent_BeforeGridEventAll(StageGenerator main, StageGenerator.GridManager gridManager);



        /// <summary>
        /// [비동기] Post이벤트: Post이벤트 전 의 <b>전체</b> 그리드 이벤트
        /// </summary>
        /// <param name="main"></param>
        /// <param name="gridManager"></param>
        public abstract UniTask PostEvent_BeforeGridEventAllAsync(StageGenerator main, StageGenerator.GridManager gridManager);



        ///======================================================================================================================================================



        /// <summary>
        /// Post이벤트: Post이벤트 후 의 <b>전체</b> 그리드 이벤트
        /// </summary>
        /// <param name="main"></param>
        /// <param name="gridManager"></param>
        public abstract void PostEvent_AfterGridEventAll(StageGenerator main, StageGenerator.GridManager gridManager);



        /// <summary>
        /// [비동기] Post이벤트: Post이벤트 후 의 <b>전체</b> 그리드 이벤트
        /// </summary>
        /// <param name="main"></param>
        /// <param name="gridManager"></param>
        public abstract UniTask PostEvent_AfterGridEventAllAsync(StageGenerator main, StageGenerator.GridManager gridManager);



        ///===============================================================================================================================================
    }
}
