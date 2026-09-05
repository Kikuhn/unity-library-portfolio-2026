using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Spine;
using Spine.Unity;
using System.Linq;
using System;
using Pan.Util;
using Pan.Util.IOB;
using Pan.Event;
using Pan.SpinePackage;
using static Pan.SpinePackage.SkelObject.AniCore.Players;
using Pan.Util.Game;
using System.Text;



namespace Pan.SpinePackage
{
    public partial class SkelObject
    {
        public partial class AniCore : CoreBase
        {
            /// <summary>
            /// 플레이어에서 재생되는 트랙
            /// </summary>
            public class Track
            {
                ///======================================================================================================================================================



                public Track(int memoCapctiy)
                {
                    PlayingSkelAni = SkelAni.Create_Husk();

                    MemoList = new(memoCapctiy);
                    MemoExtendList = new(memoCapctiy);

                    RefreshTrack();
                }



                /// <summary>
                /// 트랙 초기화
                /// </summary>
                public void RefreshTrack()
                {
                    //. 재생중인 껍질 애니메이션의 값을 초기화한다
                    PlayingSkelAni.ResetValues();
                    PlayingSkelAni_Reference = null;
                    AnimationStarter = null;


                    //? 메모들을 초기화한다
                    MemoList.Clear();
                    MemoExtendList.Clear();


                    MultipleTime = 1f;
                    MultipleTimeFunc = null;
                    EndEvent = null;


                    SpineEventCustom_1 = null;
                    SpineEventCustom_2 = null;
                    SpineEventCustom_3 = null;
                    SpineEventCustom_4 = null;
                    SpineEventCustom_5 = null;
                    SpineEventCustom_6 = null;
                    SpineEventCustom_7 = null;
                    SpineEventCustom_8 = null;
                    SpineEventCustom_9 = null;
                }



                ///======================================================================================================================================================



                /// <summary>
                /// 재생되고있는 현재 애니메이션의 활성화/비활성화 여부
                /// 이 트랙의 활성화/비활성화 여부
                /// <para> <see cref="SetEnable(int, ISkelAni)"/></para>
                /// <para> <see cref="DisableTrack"/></para>
                /// <para>에서 결정된다</para>
                /// </summary>
                public bool IsEnable { get; private set; }



                ///<summary>
                /// 재생중인 애니메이션이 생길때마다, 이 객체에 복사(Deep)되어 사용
                /// <para>생성자에서부터 <see cref="SkelAni.Create_Husk"/>로 생성되며, 주기적으로 초기화되며 사용한다</para>
                /// </summary>
                public SkelAni PlayingSkelAni { get; set; }



                ///======================================================================================================================================================



                //? 활성화/비활성화



                /// <summary>
                /// 이 Track에 애니메이션을 할당하고, <b>활성화</b> 시킨다
                /// </summary>
                public void EnableTrack(ISkelAni skelAni)
                {
                    RefreshTrack();
                    PlayingSkelAni_Reference = skelAni;
                    PlayingSkelAni.Copy(skelAni);
                    IsEnable = true;
                }



                /// <summary>
                /// 이 Track을 <b>비활성화</b> 시키고, 초기화한다
                /// </summary>
                public void DisableTrack()
                {
                    RefreshTrack();
                    IsEnable = false;
                }



                ///======================================================================================================================================================



                //? 재생중인 애니메이션 재생속도 배율



                /// <summary>
                /// 재생중인 애니메이션의 재생속도 배율
                /// <para>기본값은 1f</para>
                /// </summary>
                public float MultipleTime { private get; set; } = 1f;



                /// <summary>
                /// 재생중인 애니메이션의 재생속도 배율 Func
                /// <para>한개라도 추가될 경우, <see cref="MultipleTime"/>과 곱연산으로 반환된다</para>
                /// <para>null일경우, 애초에 연산에 포함되지 않는다</para>
                /// </summary>
                public Func<float> MultipleTimeFunc { private get; set; } = null;



                /// <summary>
                /// 재생중인 애니메이션의 재생속도 배율 Func 지정
                /// <para>체이닝에 의미가 없기에, 덮어쓰기만 가능</para>
                /// </summary>
                public void SetMultipleTimeFunc(Func<float> multipleTimeFunc)
                {
                    MultipleTimeFunc = multipleTimeFunc;
                }



                /// <summary>
                /// 재생중인 애니메이션 재생속도 배율 얻기
                /// </summary>
                public float GetMultipleTime()
                {
                    return (MultipleTimeFunc == null) ? MultipleTime : MultipleTimeFunc.Invoke() * MultipleTime;
                }



                ///======================================================================================================================================================



                //? 애니메이션을 재생할때 사용되는 필드



                ///<summary>
                ///재생중인 애니메이션을 실행한 객체
                ///<para>애니메이션 재생 중단시, 실행한 객체에서만 중단을 하고싶을경우</para>
                ///<para>실행시 객체를 이곳에 할당하고, 중단시 이 객체와 비교하여 중단여부를 결정한다</para>
                /// </summary>
                public object AnimationStarter { get; set; }



                /// <summary>
                /// 재생중인 원본 애니메이션의 참조
                /// <para>수정 절대 금지, 원본 애니메이션이 수정되어버림</para>
                /// </summary>
                public ISkelAni PlayingSkelAni_Reference { get; private set; }



                ///======================================================================================================================================================



                //? 애니메이션 이벤트



                ///<summary>
                ///스파인 델리게이트 이벤트 1
                /// </summary>
                public Action SpineEventCustom_1 = null;

                ///<summary>
                ///스파인 델리게이트 이벤트 2
                /// </summary>
                public Action SpineEventCustom_2 = null;

                ///<summary>
                ///스파인 델리게이트 이벤트 3
                /// </summary>
                public Action SpineEventCustom_3 = null;

                ///<summary>
                ///스파인 델리게이트 이벤트 4
                /// </summary>
                public Action SpineEventCustom_4 = null;

                ///<summary>
                ///스파인 델리게이트 이벤트 5
                /// </summary>
                public Action SpineEventCustom_5 = null;

                ///<summary>
                ///스파인 델리게이트 이벤트 6
                /// </summary>
                public Action SpineEventCustom_6 = null;

                ///<summary>
                ///스파인 델리게이트 이벤트 7
                /// </summary>
                public Action SpineEventCustom_7 = null;

                ///<summary>
                ///스파인 델리게이트 이벤트 8
                /// </summary>
                public Action SpineEventCustom_8 = null;

                ///<summary>
                ///스파인 델리게이트 이벤트 9
                /// </summary>
                public Action SpineEventCustom_9 = null;



                /// <summary>
                /// 애니메이션이 종료시 델리게이트 이벤트 (체이닝 가능)
                /// </summary>
                public event Action<SkelObject, Track> EndEvent = null;



                /// <summary>
                /// <see cref="EndEvent"/>의 존재 여부
                /// </summary>
                public bool Contains_EndEvent => EndEvent != null;



                /// <summary>
                /// 애니메이션 종료 이벤트 실행
                /// <para>애니메이션 종료 로직에서 실행되도록 하자</para>
                /// </summary>
                public void ExecuteEndEvent(SkelObject skelObject, Track track)
                {
                    EndEvent?.Invoke(skelObject, track);
                }



                ///======================================================================================================================================================



                //? 트랙 메모



                /// <summary>
                /// 메모 저장 리스트
                /// </summary>
                private readonly List<string> MemoList;



                /// <summary>
                /// 메모(확장) 저장 리스트
                /// </summary>
                private readonly List<TrackMemoExtend> MemoExtendList;



                /// <summary>
                /// <see cref="MemoList"/> 얻기 (사용 자제 권장)
                /// </summary>
                public IReadOnlyList<string> GetMemoList => MemoList;



                /// <summary>
                /// <see cref="MemoExtendList"/> 얻기 (사용 자제 권장)
                /// </summary>
                public IReadOnlyList<TrackMemoExtend> GetMemoExtendList => MemoExtendList;



                ///======================================================================================================================================================



                //? 메모 메서드



                #region 메모 메서드



                /// <summary>
                /// 메모 추가
                /// </summary>
                public void AddMemo(string memo)
                {
                    MemoList.Add(memo);
                }



                /// <summary>
                /// 메모 제거
                /// </summary>
                public void RemoveMemo(string memo)
                {
                    MemoList.Remove(memo);
                }



                /// <summary>
                /// 메모 확인
                /// </summary>
                public bool CheckMemo(string memo)
                {
                    return MemoList.Contains(memo);
                }



                /// <summary>
                /// 메모확장 추가
                /// </summary>
                public void AddMemoExtend(TrackMemoExtend memo)
                {
                    MemoExtendList.Add(memo);
                }



                /// <summary>
                /// 메모확장 제거
                /// </summary>
                public bool RemoveMemoExtend(string memo)
                {
                    for (int i = MemoExtendList.Count - 1; i >= 0; i--)
                    {
                        if (MemoExtendList[i].Memo == memo)
                        {
                            MemoExtendList.RemoveAt(i);
                            return true;
                        }
                    }
                    return false;
                }



                /// <summary>
                /// 메모확장 확인
                /// </summary>
                public bool CheckMemoExtend(string memo)
                {
                    for (int i = MemoExtendList.Count - 1; i >= 0; i--)
                    {
                        if (MemoExtendList[i].Memo == memo)
                        {
                            return true;
                        }
                    }

                    return false;
                }



                /// <summary>
                /// 메모확장 TryGet
                /// </summary>
                public bool TryGetMemoExtend(string memo, out TrackMemoExtend resultMemoExtend)
                {
                    for (int i = MemoExtendList.Count - 1; i >= 0; i--)
                    {
                        if (MemoExtendList[i].Memo == memo)
                        {
                            resultMemoExtend = MemoExtendList[i];
                            return true;
                        }
                    }

                    resultMemoExtend = default;
                    return false;
                }



                #endregion



                ///======================================================================================================================================================



                //? 확장 메서드



                /// <summary>
                /// 이 <see cref="Track"/>의 애니메이션이 재생되는동안,
                /// <para>PostUpdateEvent에 할당되어, Update 될때마다</para>
                /// <para><paramref name="updateEvent"/>이 실행되게끔 한다</para>
                /// </summary>
                public void AddPostUpdate_DuringPlayingAnimation(SkelObject skelObject, Action<SkelObject> updateEvent)
                {
                    skelObject.Ani.AddPostUpdate_DuringPlayingAnimation(this, updateEvent);
                }



                ///======================================================================================================================================================
            }



            /// <summary>
            /// 트랙 메모 확장
            /// </summary>
            public struct TrackMemoExtend
            {
                public TrackMemoExtend(string memo, int trackIndex, ISkelAni skelAni)
                {
                    Memo = memo;
                    TrackIndex = trackIndex;
                    SkelAni = skelAni;
                }

                public TrackMemoExtend(string memo, ISkelAni skelAni)
                {
                    Memo = memo;
                    TrackIndex = skelAni.TrackIndex;
                    SkelAni = skelAni;
                }

                public string Memo;

                public int TrackIndex;

                public ISkelAni SkelAni;
            }
        }
    }
}