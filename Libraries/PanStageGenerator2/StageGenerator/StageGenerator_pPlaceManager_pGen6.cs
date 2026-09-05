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
            /// 마무리 커튼콜
            /// </summary>
            [Serializable]
            public class Generator6_CurtainCall
            {
                public void CurtainCall(StageGenerator main)
                {
                    main.GenerateM.GeneratingCurrentSteps = GenerateManager.GenerateSteps.GenStep8_Places_Gen6_CurtainCall;

                    //. 방 트랜스폼 정보 초기화 (null)
                    if (main.GenerateInfoM.CurtenCall_ClearCaches)
                    {
                        main.placeM.PlacedRoomTFInfoCacheM.Clear_PlacedRoomTransformInfoCache(true);
                    }
                }



                public async UniTask CurtainCallAsync(StageGenerator main)
                {
                    main.GenerateM.GeneratingCurrentSteps = GenerateManager.GenerateSteps.GenStep8_Places_Gen6_CurtainCall;

                    //. 방 트랜스폼 정보 초기화 (null)
                    if (main.GenerateInfoM.CurtenCall_ClearCaches)
                    {
                        main.placeM.PlacedRoomTFInfoCacheM.Clear_PlacedRoomTransformInfoCache(true);
                    }

                    await UniTask.CompletedTask;
                }
            }



            ///======================================================================================================================================================
        }
    }
}