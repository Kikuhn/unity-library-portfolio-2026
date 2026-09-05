using Cysharp.Threading.Tasks;
using Pan.StageGenerators;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;



namespace Pan.StageGenerators
{
    public abstract class StageGenerateCustomEventBase_SummonRoom : BaseStageGenerateCustomEvent
    {
        ///======================================================================================================================================================



        /// <summary>
        /// 방을 생성하기 전 실행 (전체)
        /// </summary>
        /// <param name="main"></param>
        /// <param name="spaceList"></param>
        /// <param name="parent"></param>
        public abstract void Before_SummonRooms(StageGenerator main, IReadOnlyList<StageGenerator.Space> spaceList, Transform parent);



        /// <summary>
        /// [비동기] 방을 생성하기 전 실행 (전체)
        /// </summary>
        /// <param name="main"></param>
        /// <param name="spaceList"></param>
        /// <param name="parent"></param>
        public abstract UniTask Before_SummonRoomsAsync(StageGenerator main, IReadOnlyList<StageGenerator.Space> spaceList, Transform parent);



        ///======================================================================================================================================================



        /// <summary>
        /// 방을 생성한 후 실행 (전체)
        /// </summary>
        /// <param name="main"></param>
        /// <param name="spaceList"></param>
        /// <param name="parent"></param>
        public abstract void After_SummonRooms(StageGenerator main, IReadOnlyList<StageGenerator.Space> spaceList, Transform parent);



        /// <summary>
        /// [비동기] 방을 생성한 후 실행 (전체)
        /// </summary>
        /// <param name="main"></param>
        /// <param name="spaceList"></param>
        /// <param name="parent"></param>
        public abstract UniTask After_SummonRoomsAsync(StageGenerator main, IReadOnlyList<StageGenerator.Space> spaceList, Transform parent);



        ///======================================================================================================================================================



        /// <summary>
        /// 방을 생성한 후 실행 (각각)
        /// </summary>
        /// <param name="main"></param>
        /// <param name="parent"></param>
        /// <param name="summonedRoom"></param>
        /// <param name="space"></param>
        /// <param name="spaceList"></param>
        public abstract void After_SummonRoom(StageGenerator main, Transform parent, RoomObject summonedRoom, StageGenerator.Space space, IReadOnlyList<StageGenerator.Space> spaceList);



        /// <summary>
        /// [비동기] 방을 생성한 후 실행 (각각)
        /// </summary>
        /// <param name="main"></param>
        /// <param name="parent"></param>
        /// <param name="summonedRoom"></param>
        /// <param name="space"></param>
        /// <param name="spaceList"></param>
        public abstract UniTask After_SummonRoomAsync(StageGenerator main, Transform parent, RoomObject summonedRoom, StageGenerator.Space space, IReadOnlyList<StageGenerator.Space> spaceList);



        ///======================================================================================================================================================



        /// <summary>
        /// 방이 생성된 후, 그리드 이벤트가 적용되기 전 실행 (각각)
        /// </summary>
        /// <param name="main"></param>
        /// <param name="summonedRoom"></param>
        /// <param name="space"></param>
        /// <param name="spaceList"></param>
        public abstract void AfterPost_SummonRoom_BeforeGridEvent(StageGenerator main, RoomObject summonedRoom, StageGenerator.Space space, IReadOnlyList<StageGenerator.Space> spaceList);



        ///======================================================================================================================================================



        /// <summary>
        /// 방이 생성된 후, 그리드 이벤트가 적용된 후 실행 (각각)
        /// </summary>
        /// <param name="main"></param>
        /// <param name="summonedRoom"></param>
        /// <param name="space"></param>
        /// <param name="spaceList"></param>
        public abstract void AfterPost_SummonRoom_AfterGridEvent(StageGenerator main, RoomObject summonedRoom, StageGenerator.Space space, IReadOnlyList<StageGenerator.Space> spaceList);



        ///======================================================================================================================================================



        /// <summary>
        /// 방이 생성된 후, 방 그리드 이벤트대신 실행 (각각)
        /// </summary>
        /// <param name="main"></param>
        /// <param name="summonedRoom"></param>
        /// <param name="space"></param>
        /// <param name="spaceList"></param>
        /// <returns></returns>
        public abstract bool Switch_SummonRoom_GridEvent(StageGenerator main, RoomObject summonedRoom, StageGenerator.Space space, IReadOnlyList<StageGenerator.Space> spaceList);



        ///======================================================================================================================================================
    }

}
