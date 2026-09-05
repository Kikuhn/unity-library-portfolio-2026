using Cysharp.Threading.Tasks;
using Pan.StageGenerators;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;



namespace Pan.StageGenerators
{
    public abstract class StageGenerateCustomEventBase_PlaceHallway : BaseStageGenerateCustomEvent
    {
        ///======================================================================================================================================================



        /// <summary>
        /// 복도를 생성하기 전 실행 (전체)
        /// </summary>
        /// <param name="main"></param>
        /// <param name="spaceList"></param>
        public abstract void Before_GenerateHallways(StageGenerator main, IReadOnlyList<StageGenerator.Space> spaceList);



        /// <summary>
        /// [비동기] 복도를 생성하기 전 실행 (전체)
        /// </summary>
        /// <param name="main"></param>
        /// <param name="spaceList"></param>
        public abstract UniTask Before_GenerateHallwaysAsync(StageGenerator main, IReadOnlyList<StageGenerator.Space> spaceList);



        ///======================================================================================================================================================



        /// <summary>
        /// 복도를 생성한 후 실행 (전체)
        /// </summary>
        /// <param name="main"></param>
        /// <param name="spaceList"></param>
        public abstract void After_GenerateHallways(StageGenerator main, IReadOnlyList<StageGenerator.Space> spaceList);



        /// <summary>
        /// [비동기] 복도를 생성한 후 실행 (전체)
        /// </summary>
        /// <param name="main"></param>
        /// <param name="spaceList"></param>
        public abstract UniTask After_GenerateHallwaysAsync(StageGenerator main, IReadOnlyList<StageGenerator.Space> spaceList);



        ///======================================================================================================================================================



        /// <summary>
        /// 복도를 생성하기 전 실행 (각각)
        /// </summary>
        /// <param name="main"></param>
        /// <param name="pathList"></param>
        /// <param name="isDoorCorrection"></param>
        /// <param name="hallwayMainWidth"></param>
        /// <param name="hallwayMaxWidth"></param>
        /// <param name="expandPositiveVertical"></param>
        /// <param name="expandPositiveHorizontal"></param>
        public abstract void Before_GenerateHallway_FromDoor(StageGenerator main, IList<Vector2Int> pathList, bool isDoorCorrection, int hallwayMainWidth, int hallwayMaxWidth, bool expandPositiveVertical, bool expandPositiveHorizontal);



        /// <summary>
        /// [비동기] 복도를 생성하기 전 실행 (각각)
        /// </summary>
        /// <param name="main"></param>
        /// <param name="pathList"></param>
        /// <param name="isDoorCorrection"></param>
        /// <param name="hallwayMainWidth"></param>
        /// <param name="hallwayMaxWidth"></param>
        /// <param name="expandPositiveVertical"></param>
        /// <param name="expandPositiveHorizontal"></param>
        public abstract UniTask Before_GenerateHallway_FromDoorAsync(StageGenerator main, IList<Vector2Int> pathList, bool isDoorCorrection, int hallwayMainWidth, int hallwayMaxWidth, bool expandPositiveVertical, bool expandPositiveHorizontal);



        ///======================================================================================================================================================



        /// <summary>
        /// 복도를 생성한 후 실행 (각각)
        /// </summary>
        /// <param name="main"></param>
        /// <param name="pathList"></param>
        /// <param name="isDoorCorrection"></param>
        /// <param name="hallwayMainWidth"></param>
        /// <param name="hallwayMaxWidth"></param>
        /// <param name="expandPositiveVertical"></param>
        /// <param name="expandPositiveHorizontal"></param>
        /// <param name="gridList"></param>
        public abstract void After_GenerateHallway_FromDoor(StageGenerator main, IList<Vector2Int> pathList, bool isDoorCorrection, int hallwayMainWidth, int hallwayMaxWidth, bool expandPositiveVertical, bool expandPositiveHorizontal, List<StageGenerator.Grid> gridList);



        /// <summary>
        /// [비동기] 복도를 생성한 후 실행 (각각)
        /// </summary>
        /// <param name="main"></param>
        /// <param name="pathList"></param>
        /// <param name="isDoorCorrection"></param>
        /// <param name="hallwayMainWidth"></param>
        /// <param name="hallwayMaxWidth"></param>
        /// <param name="expandPositiveVertical"></param>
        /// <param name="expandPositiveHorizontal"></param>
        /// <param name="gridList"></param>
        public abstract UniTask After_GenerateHallway_FromDoorAsync(StageGenerator main, IList<Vector2Int> pathList, bool isDoorCorrection, int hallwayMainWidth, int hallwayMaxWidth, bool expandPositiveVertical, bool expandPositiveHorizontal, List<StageGenerator.Grid> gridList);



        ///======================================================================================================================================================
    }

}
