using Cysharp.Threading.Tasks;
using Pan.StageGenerators;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;



namespace Pan.StageGenerators
{
    public abstract class StageGenerateCustomEventBase_SummonHallway : BaseStageGenerateCustomEvent
    {
        ///======================================================================================================================================================



        /// <summary>
        /// 복도를 생성하기 전 실행 (전체)
        /// </summary>
        /// <param name="main"></param>
        /// <param name="placeManager"></param>
        public abstract void Before_SummonHallways(StageGenerator main, StageGenerator.PlaceManger placeManager);



        /// <summary>
        /// [비동기] 복도를 생성하기 전 실행 (전체)
        /// </summary>
        /// <param name="main"></param>
        /// <param name="placeManager"></param>
        public abstract UniTask Before_SummonHallwaysAsync(StageGenerator main, StageGenerator.PlaceManger placeManager);



        ///======================================================================================================================================================



        /// <summary>
        /// 복도를 생성한 후 실행 (전체)
        /// </summary>
        /// <param name="main"></param>
        /// <param name="placeManager"></param>
        public abstract void After_SummonHallways(StageGenerator main, StageGenerator.PlaceManger placeManager);



        /// <summary>
        /// [비동기] 복도를 생성한 후 실행 (전체)
        /// </summary>
        /// <param name="main"></param>
        /// <param name="placeManager"></param>
        public abstract UniTask After_SummonHallwaysAsync(StageGenerator main, StageGenerator.PlaceManger placeManager);



        ///======================================================================================================================================================



        /// <summary>
        /// 복도 그리드를 생성하기 전 실행 (각각)
        /// </summary>
        /// <param name="main"></param>
        /// <param name="gridManager"></param>
        /// <param name="grid"></param>
        public abstract void Before_SummonHallway_ByGrid(StageGenerator main, StageGenerator.GridManager gridManager, StageGenerator.Grid grid);



        /// <summary>
        /// [비동기] 복도 그리드를 생성하기 전 실행 (각각)
        /// </summary>
        /// <param name="main"></param>
        /// <param name="gridManager"></param>
        /// <param name="grid"></param>
        public abstract UniTask Before_SummonHallway_ByGridAsync(StageGenerator main, StageGenerator.GridManager gridManager, StageGenerator.Grid grid);



        ///======================================================================================================================================================



        /// <summary>
        /// 복도 그리드를 생성한 후 실행 (각각)
        /// </summary>
        /// <param name="main"></param>
        /// <param name="gridManager"></param>
        /// <param name="grid"></param>
        public abstract void After_SummonHallway_ByGrid(StageGenerator main, StageGenerator.GridManager gridManager, StageGenerator.Grid grid);



        /// <summary>
        /// [비동기] 복도 그리드를 생성한 후 실행 (각각)
        /// </summary>
        /// <param name="main"></param>
        /// <param name="gridManager"></param>
        /// <param name="grid"></param>
        public abstract UniTask After_SummonHallway_ByGridAsync(StageGenerator main, StageGenerator.GridManager gridManager, StageGenerator.Grid grid);



        ///======================================================================================================================================================



        /// <summary>
        /// 복도 그리드 생성 대신 실행 (각각)
        /// </summary>
        /// <param name="main"></param>
        /// <param name="gridManager"></param>
        /// <param name="grid"></param>
        /// <returns></returns>
        public abstract bool Switch_SummonHallway_ByGrid(StageGenerator main, StageGenerator.GridManager gridManager, StageGenerator.Grid grid);



        /// <summary>
        /// [비동기] 복도 그리드 생성 대신 실행 (각각)
        /// </summary>
        /// <param name="main"></param>
        /// <param name="gridManager"></param>
        /// <param name="grid"></param>
        /// <returns></returns>
        public abstract UniTask<bool> Switch_SummonHallway_ByGridAsync(StageGenerator main, StageGenerator.GridManager gridManager, StageGenerator.Grid grid);



        ///======================================================================================================================================================



        /// <summary>
        /// 복도 테두리 그리드를 생성하기 전 실행 (각각)
        /// </summary>
        /// <param name="main"></param>
        /// <param name="gridManager"></param>
        /// <param name="grid"></param>
        public abstract void Before_SummonHallwayEdge_ByGrid(StageGenerator main, StageGenerator.GridManager gridManager, StageGenerator.Grid grid);



        /// <summary>
        /// [비동기] 복도 테두리 그리드를 생성하기 전 실행 (각각)
        /// </summary>
        /// <param name="main"></param>
        /// <param name="gridManager"></param>
        /// <param name="grid"></param>
        public abstract UniTask Before_SummonHallwayEdge_ByGridAsync(StageGenerator main, StageGenerator.GridManager gridManager, StageGenerator.Grid grid);



        ///======================================================================================================================================================



        /// <summary>
        /// 복도 테두리 그리드를 생성한 후 실행 (각각)
        /// </summary>
        /// <param name="main"></param>
        /// <param name="gridManager"></param>
        /// <param name="grid"></param>
        public abstract void After_SummonHallwayEdge_ByGrid(StageGenerator main, StageGenerator.GridManager gridManager, StageGenerator.Grid grid);



        /// <summary>
        /// [비동기] 복도 테두리 그리드를 생성한 후 실행 (각각)
        /// </summary>
        /// <param name="main"></param>
        /// <param name="gridManager"></param>
        /// <param name="grid"></param>
        public abstract UniTask After_SummonHallwayEdge_ByGridAsync(StageGenerator main, StageGenerator.GridManager gridManager, StageGenerator.Grid grid);



        ///======================================================================================================================================================



        /// <summary>
        /// 복도 테두리 그리드 생성 대신 실행 (각각)
        /// </summary>
        /// <param name="main"></param>
        /// <param name="gridManager"></param>
        /// <param name="grid"></param>
        /// <returns></returns>
        public abstract bool Switch_SummonHallwayEdge_ByGrid(StageGenerator main, StageGenerator.GridManager gridManager, StageGenerator.Grid grid);



        /// <summary>
        /// [비동기] 복도 테두리 그리드 생성 대신 실행 (각각)
        /// </summary>
        /// <param name="main"></param>
        /// <param name="gridManager"></param>
        /// <param name="grid"></param>
        /// <returns></returns>
        public abstract UniTask<bool> Switch_SummonHallwayEdge_ByGridAsync(StageGenerator main, StageGenerator.GridManager gridManager, StageGenerator.Grid grid);



        ///======================================================================================================================================================
    }

}
