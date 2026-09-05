using System.Collections.Generic;
using UnityEngine;
using Pan.Util;
using System;
using Pan.GridCompatibles2;
using Pan.StageGenerators;



namespace Pan.StageGenerators
{
    public partial class StageGenerator
    {
        //[Serializable]
        //public class RandSeedManager : BaseManager
        //{
        //    ///======================================================================================================================================================



        //    public static implicit operator CustomRandom(RandSeedManager value) => value.Random;



        //    ///======================================================================================================================================================



        //    //? 커스텀 랜덤 매니저



        //    public CustomRandom Random => random;
        //    [field: SerializeField] private CustomRandom random = new CustomRandom(CustomRandom.EMode.LateCreate);



        //    ///======================================================================================================================================================



        //    //? 시드



        //    ///<summary>
        //    ///마지막으로 적용된 시드
        //    ///</summary>
        //    public int Seed_LastApplied => random.Seed;



        //    ///<summary>
        //    ///다음에 적용될 시드
        //    ///</summary>
        //    public int Seed_NextWillApplied;



        //    ///<summary>"다음에 적용될 시드" 기능 활성화 여부<br/>
        //    ///(<see cref="Seed_NextWillApplied"/>를 사용할지 여부)</summary>
        //    public bool UseSeed_NextWillApplied;



        //    ///======================================================================================================================================================



        //    ///<summary>무작위 랜덤 시드 생성,<br/>
        //    ///seed를 받아오면, 해당 시드로 적용된다.
        //    ///null을 받아오면, 시드가 무작위로 생성되어 적용된다
        //    /// </summary>
        //    /// <param name="customOtherSeed">
        //    /// null일 경우, 시드를 무작위로 생성하여 적용되며,
        //    /// null이 아닐경우, 해당 시드값이 적용된다
        //    /// </param>
        //    public bool RefreshRandom(int? customOtherSeed = null)
        //    {
        //        //? #1 파라미터로 받아온 커스텀 시드를 적용
        //        if (customOtherSeed.HasValue)
        //        {
        //            random.RefreshRandom(customOtherSeed.Value);
        //            return true;
        //        }



        //        //? #2 다음에 적용될 시드값을 적용
        //        if (UseSeed_NextWillApplied)
        //        {
        //            random.RefreshRandom(Seed_NextWillApplied);
        //            return true;
        //        }



        //        //? #3 시드를 무작위로 생성하여 적용
        //        random.RefreshRandom();

        //        return true;
        //    }



        //    ///<summary>
        //    ///무작위 랜덤 시드 생성,<br/>
        //    ///기존 설정값을 무시하고 진짜 무작위 시드가 지정된다
        //    /// </summary>
        //    public void RefreshRealyRandom()
        //    {
        //        random.RefreshRandom();
        //    }



        //    ///======================================================================================================================================================
        //}
    }
}