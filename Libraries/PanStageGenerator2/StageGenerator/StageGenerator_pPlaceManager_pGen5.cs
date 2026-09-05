using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Pan.Util;
using System;
using System.Text;
using Cysharp.Threading.Tasks;
using System.Threading;
using Pan.GridCompatibles2;
using Pan.StageGenerators;
using Sirenix.Utilities;



namespace Pan.StageGenerators
{
    public partial class StageGenerator
    {
        public partial class PlaceManger : BaseManager
        {
            ///======================================================================================================================================================



            ///<summary>
            /// Post 생성
            /// </summary>
            [Serializable]
            public class Generator5_PostGenerate
            {
                public void PostGenerate(StageGenerator main)
                {
                    main.GenerateM.GeneratingCurrentSteps = GenerateManager.GenerateSteps.GenStep7_Places_Gen5_PostGenerate;

                    if (main.Setting.CustomEvent.PostEvent != null)
                    {
                        var postEvent = main.Setting.CustomEvent.PostEvent;


                        postEvent.PostEvent_BeforeGridEventAll(main, main.gridM);


                        postEvent.PostEvent_PlaceObjects_Room(main, main.placeM.PlaceObjects_Room);


                        postEvent.PostEvent_PlaceObjects_Hallway(main, main.placeM.PlaceObjects_Hallway);


                        postEvent.PostEvent_PlaceObjects_HallwayEdge(main, main.placeM.PlaceObjects_HallwayEdge);


                        postEvent.PostEvent_PlaceObjects_ETC(main, main.placeM.PlaceObjects_ETC);


                        postEvent.PostEvent_CustomExpandGrid(main, main.placeM.GetGrids_CustomExpand);


                        postEvent.PostEvent_AfterGridEventAll(main, main.gridM);
                    }
                }



                public async UniTask PostGenerateAsync(StageGenerator main)
                {
                    main.GenerateM.GeneratingCurrentSteps = GenerateManager.GenerateSteps.GenStep7_Places_Gen5_PostGenerate;

                    if (main.Setting.CustomEvent.PostEvent != null)
                    {
                        var postEvent = main.Setting.CustomEvent.PostEvent;


                        await postEvent.PostEvent_BeforeGridEventAllAsync(main, main.gridM);


                        await postEvent.PostEvent_PlaceObjects_RoomAsync(main, main.placeM.PlaceObjects_Room);


                        await postEvent.PostEvent_PlaceObjects_HallwayAsync(main, main.placeM.PlaceObjects_Hallway);


                        await postEvent.PostEvent_PlaceObjects_HallwayEdgeAsync(main, main.placeM.PlaceObjects_HallwayEdge);


                        await postEvent.PostEvent_PlaceObjects_ETCAsync(main, main.placeM.PlaceObjects_ETC);


                        await postEvent.PostEvent_CustomExpandGridAsync(main, main.placeM.GetGrids_CustomExpand);


                        await postEvent.PostEvent_AfterGridEventAllAsync(main, main.gridM);
                    }
                }
            }



            ///======================================================================================================================================================
        }
    }
}