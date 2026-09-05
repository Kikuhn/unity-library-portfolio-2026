using Cysharp.Threading.Tasks;
using Pan.Util;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;



namespace Pan.StageGenerators
{
    public abstract class StageGenerateCustomEventBase_CreateGrid : BaseStageGenerateCustomEvent
    {
        ///======================================================================================================================================================



        /// <summary>
        /// 그리드 배열을 생성하기 전 (전체)
        /// </summary>
        /// <param name="main"></param>
        public abstract void Before_CreateGridArray(StageGenerator main);



        /// <summary>
        /// [비동기] 그리드 배열을 생성하기 전 (전체)
        /// </summary>
        /// <param name="main"></param>
        public abstract UniTask Before_CreateGridArrayAsync(StageGenerator main);



        ///======================================================================================================================================================



        /// <summary>
        /// 그리드 배열을 생성한 후 (전체)
        /// </summary>
        /// <param name="main"></param>
        /// <param name="grids"></param>
        public abstract void After_CreateGridArray(StageGenerator main, LinearGrid2D<StageGenerator.Grid> grids);



        /// <summary>
        /// [비동기] 그리드 배열을 생성한 후 (전체)
        /// </summary>
        /// <param name="main"></param>
        /// <param name="grids"></param>
        public abstract UniTask After_CreateGridArrayAsync(StageGenerator main, LinearGrid2D<StageGenerator.Grid> grids);



        ///======================================================================================================================================================



        /// <summary>
        /// 그리드를 생성하기 전 (각각)
        /// </summary>
        /// <param name="main"></param>
        /// <param name="grid"></param>
        public abstract void Before_CreateGrid(StageGenerator main, StageGenerator.Grid grid);



        ///======================================================================================================================================================



        /// <summary>
        /// 그리드를 생성한 후 (각각)
        /// </summary>
        /// <param name="main"></param>
        /// <param name="grid"></param>
        public abstract void After_CreateGrid(StageGenerator main, StageGenerator.Grid grid);



        ///======================================================================================================================================================
    }

}
