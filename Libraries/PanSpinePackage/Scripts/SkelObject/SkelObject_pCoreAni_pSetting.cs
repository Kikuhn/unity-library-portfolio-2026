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
using Sirenix.OdinInspector;



namespace Pan.SpinePackage
{
    public partial class SkelObject
    {
        public partial class AniCore : CoreBase
        {
            /// <summary>
            /// 애니메이션 설정 클래스
            /// </summary>
            [Serializable]
            public class Settings : Base
            {
                ///======================================================================================================================================================



                public Settings(SkelObject skelObject, SkelSbject skelSbject) : base(skelObject)
                {
                    Setting(skelSbject.EnumIndex_AniTracks, skelSbject.EnumIndex_AniRanks, skelSbject.AnimationMixDuration);
                }



                ///<summary>
                ///설정 초기화
                /// </summary>
                public void Reset()
                {
                    Clear_EmptyAnimationMixduration();
                    EnumIndex_AniTrack = null;
                    EnumIndex_AniRank = null;
                    DefaultAnimationMixDuration = 0.2f;
                }



                ///<summary>
                ///새롭게 설정
                /// </summary>
                /// <param name="beforeReset">true라면, 설정 전 <see cref="Reset"/>을 호출한뒤 설정된다</param>
                public void Setting(SkelSbject.IEnumIndex_AniTrack enumIndex_AniTrack, SkelSbject.IEnumIndex_AniRank enumIndex_AniRank, float defaultAnimationMixDuration, bool beforeReset = false)
                {
                    if (beforeReset) { Reset(); }

                    EnumIndex_AniTrack = enumIndex_AniTrack;
                    EnumIndex_AniRank = enumIndex_AniRank;
                    DefaultAnimationMixDuration = defaultAnimationMixDuration;
                }



                ///======================================================================================================================================================



                ///<summary>
                /// 애니메이션 트랙의 Enum-Index 관리자
                /// </summary>
                public SkelSbject.IEnumIndex_AniTrack EnumIndex_AniTrack { get; private set; }

                ///<summary>
                /// 애니메이션 랭크의 Enum-Index 관리자
                /// </summary>
                public SkelSbject.IEnumIndex_AniRank EnumIndex_AniRank { get; private set; }



                ///<summary>
                /// 기본 애니메이션 MixDuration
                /// <para>기본값: 스파인과 동일한 <c>0.2f</c></para>
                /// </summary>
                public float DefaultAnimationMixDuration
                {
                    get => defaultAnimationMixDuration;
                    set
                    {
                        defaultAnimationMixDuration = value;
                        SkelObject.m_SkeletonAnimation.AnimationState.Data.DefaultMix = value;
                    }
                }
                [LabelText("기본 애니 MixDuration")]
                [SerializeField][OnValueChanged(nameof(InspectorRefresh_defaultAnimationMixDuration))] private float defaultAnimationMixDuration = 0.2f;
                private void InspectorRefresh_defaultAnimationMixDuration()
                {
                    if (SkelObject.m_SkeletonAnimation != null &&
                        SkelObject.m_SkeletonAnimation.AnimationState != null &&
                        SkelObject.m_SkeletonAnimation.AnimationState.Data != null
                        )
                    {
                        SkelObject.m_SkeletonAnimation.AnimationState.Data.DefaultMix = defaultAnimationMixDuration;
                    }
                }



                /// <summary>
                /// 트랙이 비워질 때 (해당 트랙에 더이상 애니메이션이 재생되지 않음)
                /// <para>Empty 애니메이션을 실행하려고 할때, 일반적인 MixDuration이 아닌</para>
                /// <para>별도의 MixDuration을 사용할 경우 그 값이 트랙별로 저장되는 딕셔너리</para>
                /// </summary>
                private readonly Dictionary<int, float> EmptyAnimation_MixDurationDictionary = new Dictionary<int, float>();



                /// <summary>
                /// 트랙이 비워질 때 (해당 트랙에 더이상 애니메이션이 재생되지 않음)
                /// <para>Empty 애니메이션을 실행하려고 할때, 일반적인 MixDuration이 아닌</para>
                /// <para>별도의 MixDuration을 사용할 경우 그 값이 트랙별로 저장되는 딕셔너리 를 얻기</para>
                /// </summary>
                public IReadOnlyDictionary<int, float> GetEmptyAnimation_MixDurationDictionary => EmptyAnimation_MixDurationDictionary;



                ///======================================================================================================================================================



                //? EmptyAnimationMixduration



                /// <summary>
                /// 트랙이 비워질 때 (해당 트랙에 더이상 애니메이션이 재생되지 않음)
                /// <para>Empty 애니메이션을 실행하려고 할때, 일반적인 MixDuration이 아닌</para>
                /// <para>별도의 MixDuration을 추가하기</para>
                /// </summary>
                public void Add_EmptyAnimationMixduration(int trackIndex, float emptyAnimationMixDuration)
                {
                    EmptyAnimation_MixDurationDictionary.Add(trackIndex, emptyAnimationMixDuration);
                }



                /// <summary>
                /// 트랙이 비워질 때 (해당 트랙에 더이상 애니메이션이 재생되지 않음)
                /// <para>Empty 애니메이션을 실행하려고 할때, 일반적인 MixDuration이 아닌</para>
                /// <para>별도의 MixDuration을 얻어보기</para>
                /// </summary>
                public bool TryGet_EmptyAnimationMixduration(int trackIndex, out float emptyAnimationMixDuration)
                {
                    return EmptyAnimation_MixDurationDictionary.TryGetValue(trackIndex, out emptyAnimationMixDuration);
                }



                /// <summary>
                /// 트랙이 비워질 때 (해당 트랙에 더이상 애니메이션이 재생되지 않음)
                /// <para>Empty 애니메이션을 실행하려고 할때, 일반적인 MixDuration이 아닌</para>
                /// <para>별도의 MixDuration을 추가하기</para>
                ///<para><b>열거형의 제약이 완벽하지 않아, 사용에 주의가 필요하다</b></para>
                /// </summary>
                public void Add_EmptyAnimationMixduration<TEnum>(TEnum trackEnum, float emptyAnimationMixDuration) where TEnum : struct, Enum
                {
                    Add_EmptyAnimationMixduration(EnumIndex_AniTrack.GetIndex(trackEnum), emptyAnimationMixDuration);
                }



                /// <summary>
                /// 트랙이 비워질 때 (해당 트랙에 더이상 애니메이션이 재생되지 않음)
                /// <para>Empty 애니메이션을 실행하려고 할때, 일반적인 MixDuration이 아닌</para>
                /// <para>별도의 MixDuration을 얻어보기</para>
                ///<para><b>열거형의 제약이 완벽하지 않아, 사용에 주의가 필요하다</b></para>
                /// </summary>
                public bool TryGet_EmptyAnimationMixduration<TEnum>(TEnum trackEnum, out float emptyAnimationMixDuration) where TEnum : struct, Enum
                {
                    return TryGet_EmptyAnimationMixduration(EnumIndex_AniTrack.GetIndex(trackEnum), out emptyAnimationMixDuration);
                }



                /// <summary>
                /// 트랙이 비워질 때 (해당 트랙에 더이상 애니메이션이 재생되지 않음)
                /// <para>Empty 애니메이션을 실행하려고 할때, 일반적인 MixDuration이 아닌</para>
                /// <para>별도의 MixDuration을 한번에 추가하기</para>
                ///<para><b>열거형의 제약이 완벽하지 않아, 사용에 주의가 필요하다</b></para>
                /// </summary>
                public void QuickFullSetting_CustomEndMixDurations(params ValueTuple<int, float>[] trackIndex_MixDurations)
                {
                    SetCapacity_EmptyAnimationMixduration(trackIndex_MixDurations.Length);

                    for (int i = 0; i < trackIndex_MixDurations.Length; i++)
                    {
                        Add_EmptyAnimationMixduration(trackIndex_MixDurations[i].Item1, trackIndex_MixDurations[i].Item2);
                    }
                }



                /// <summary>
                /// 트랙이 비워질 때 (해당 트랙에 더이상 애니메이션이 재생되지 않음)
                /// <para>Empty 애니메이션을 실행하려고 할때, 일반적인 MixDuration이 아닌</para>
                /// <para>별도의 MixDuration을 한번에 추가하기</para>
                ///<para><b>열거형의 제약이 완벽하지 않아, 사용에 주의가 필요하다</b></para>
                /// </summary>
                public void QuickFullSetting_CustomEndMixDurations<TEnum>(params ValueTuple<TEnum, float>[] trackIndex_MixDurations) where TEnum : struct, Enum
                {
                    SetCapacity_EmptyAnimationMixduration(trackIndex_MixDurations.Length);

                    for (int i = 0; i < trackIndex_MixDurations.Length; i++)
                    {
                        Add_EmptyAnimationMixduration(trackIndex_MixDurations[i].Item1, trackIndex_MixDurations[i].Item2);
                    }
                }



                /// <summary>
                /// 빈 트랙 MixDuration 딕셔너리 Clear
                /// </summary>
                public void Clear_EmptyAnimationMixduration()
                {
                    EmptyAnimation_MixDurationDictionary.Clear();
                }



                /// <summary>
                /// 빈 트랙 MixDuration 딕셔너리 용량 설정
                /// </summary>
                public void SetCapacity_EmptyAnimationMixduration(int capacity)
                {
                    EmptyAnimation_MixDurationDictionary.EnsureCapacity(capacity);
                }



                /// <summary>
                /// 빈 트랙 MixDuration 딕셔너리 용량 조정
                /// </summary>
                public void TrimExcess_EmptyAnimationMixduration()
                {
                    EmptyAnimation_MixDurationDictionary.TrimExcess();
                }



                ///======================================================================================================================================================



                ///<summary>
                ///열거형 <typeparamref name="TEnum"/>을 받아와
                ///<para>TrackIndex를 반환한다</para>
                ///<para><b>열거형의 제약이 완벽하지 않아, 사용에 주의가 필요하다</b></para>
                ///</summary>
                public int GetTrackIndex<TEnum>(TEnum trackEnum) where TEnum : struct, Enum
                {
                    return EnumIndex_AniTrack.GetIndex(trackEnum);
                }



                ///<summary>
                ///열거형 <typeparamref name="TEnum"/>을 받아와
                ///<para>Rank를 반환한다</para>
                ///<para><b>열거형의 제약이 완벽하지 않아, 사용에 주의가 필요하다</b></para>
                ///</summary>
                public int GetRankIndex<TEnum>(TEnum rankEnum) where TEnum : struct, Enum
                {
                    return EnumIndex_AniRank.GetIndex(rankEnum);
                }



                ///======================================================================================================================================================
            }
        }
    }
}