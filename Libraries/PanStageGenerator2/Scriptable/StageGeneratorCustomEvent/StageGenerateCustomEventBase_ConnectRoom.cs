using Cysharp.Threading.Tasks;
using Pan.StageGenerators;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;



namespace Pan.StageGenerators
{
    public abstract class StageGenerateCustomEventBase_ConnectRoom : BaseStageGenerateCustomEvent
    {
        ///======================================================================================================================================================



        /// <summary>
        /// 방의 도어와 도어를 이어주기 전 실행 (전체)
        /// </summary>
        /// <param name="main"></param>
        /// <param name="spaceList"></param>
        public abstract void Before_RoomsConnectToDoor(StageGenerator main, IReadOnlyList<StageGenerator.Space> spaceList);



        /// <summary>
        /// [비동기] 방의 도어와 도어를 이어주기 전 실행 (전체)
        /// </summary>
        /// <param name="main"></param>
        /// <param name="spaceList"></param>
        public abstract UniTask Before_RoomsConnectToDoorAsync(StageGenerator main, IReadOnlyList<StageGenerator.Space> spaceList);



        ///======================================================================================================================================================



        /// <summary>
        /// 방의 도어와 도어를 이어주기 후 실행 (전체)
        /// </summary>
        /// <param name="main"></param>
        /// <param name="spaceList"></param>
        public abstract void After_RoomsConnectToDoor(StageGenerator main, IReadOnlyList<StageGenerator.Space> spaceList);



        /// <summary>
        /// [비동기] 방의 도어와 도어를 이어주기 후 실행 (전체)
        /// </summary>
        /// <param name="main"></param>
        /// <param name="spaceList"></param>
        public abstract UniTask After_RoomsConnectToDoorAsync(StageGenerator main, IReadOnlyList<StageGenerator.Space> spaceList);



        ///======================================================================================================================================================



        /// <summary>
        /// 방의 도어와 도어를 이어주기 후, 도어들의 그리드 이벤트까지 적용된 후 실행 (전체)
        /// </summary>
        /// <param name="main"></param>
        /// <param name="spaceList"></param>
        public abstract void AfterPost_RoomsConnectToDoor_AfterGridEvent(StageGenerator main, IReadOnlyList<StageGenerator.Space> spaceList);



        /// <summary>
        /// [비동기] 방의 도어와 도어를 이어주기 후, 도어들의 그리드 이벤트까지 적용된 후 실행 (전체)
        /// </summary>
        /// <param name="main"></param>
        /// <param name="spaceList"></param>
        public abstract UniTask AfterPost_RoomsConnectToDoor_AfterGridEventAsync(StageGenerator main, IReadOnlyList<StageGenerator.Space> spaceList);



        ///======================================================================================================================================================



        /// <summary>
        /// 방의 도어가 서로 연결된 후 실행 (각각)
        /// </summary>
        /// <param name="main"></param>
        /// <param name="roomA"></param>
        /// <param name="doorA"></param>
        /// <param name="roomB"></param>
        /// <param name="doorB"></param>
        public abstract void After_RoomConnectToDoor(StageGenerator main, RoomObject roomA, RoomObject.Door doorA, RoomObject roomB, RoomObject.Door doorB);



        /// <summary>
        /// [비동기] 방의 도어가 서로 연결된 후 실행 (각각)
        /// </summary>
        /// <param name="main"></param>
        /// <param name="roomA"></param>
        /// <param name="doorA"></param>
        /// <param name="roomB"></param>
        /// <param name="doorB"></param>
        public abstract UniTask After_RoomConnectToDoorAsync(StageGenerator main, RoomObject roomA, RoomObject.Door doorA, RoomObject roomB, RoomObject.Door doorB);



        ///======================================================================================================================================================



        /// <summary>
        /// 도어의 그리드 이벤트가 적용되기 전 실행 (각각)
        /// </summary>
        /// <param name="main"></param>
        /// <param name="room"></param>
        public abstract void Before_Door_GridEvent(StageGenerator main, RoomObject room);



        /// <summary>
        /// [비동기] 도어의 그리드 이벤트가 적용되기 전 실행 (각각)
        /// </summary>
        /// <param name="main"></param>
        /// <param name="room"></param>
        public abstract UniTask Before_Door_GridEventAsync(StageGenerator main, RoomObject room);



        ///======================================================================================================================================================



        /// <summary>
        /// 도어의 그리드 이벤트가 적용된 후 실행 (각각)
        /// </summary>
        /// <param name="main"></param>
        /// <param name="room"></param>
        public abstract void After_Door_GridEvent(StageGenerator main, RoomObject room);



        /// <summary>
        /// [비동기] 도어의 그리드 이벤트가 적용된 후 실행 (각각)
        /// </summary>
        /// <param name="main"></param>
        /// <param name="room"></param>
        public abstract UniTask After_Door_GridEventAsync(StageGenerator main, RoomObject room);



        ///======================================================================================================================================================



        /// <summary>
        /// 도어의 그리드 이벤트 대신 실행 (각각)
        /// </summary>
        /// <param name="main"></param>
        /// <param name="room"></param>
        /// <returns></returns>
        public abstract bool Switch_SummonHallway_ByGrid(StageGenerator main, RoomObject room);



        /// <summary>
        /// [비동기] 도어의 그리드 이벤트 대신 실행 (각각)
        /// </summary>
        /// <param name="main"></param>
        /// <param name="room"></param>
        /// <returns></returns>
        public abstract UniTask<bool> Switch_SummonHallway_ByGridAsync(StageGenerator main, RoomObject room);



        ///======================================================================================================================================================
    }

}
