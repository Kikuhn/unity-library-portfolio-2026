using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Spine;
using Spine.Unity;
using System.Linq;
using System;
using Pan.Util;
using Pan.Util.IOB;
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
            /// 애니메이션 재생기
            /// </summary>
            [Serializable]
            public class Players : Base
            {
                ///======================================================================================================================================================



                public Players(SkelObject skelObject) : base(skelObject)
                {
                    //? 애니메이션 설정에 있는, 모든 등록된 TrackIndex의 총 개수를 가져온다
                    int capacity = AniCore.Setting.EnumIndex_AniTrack.GetAssignedIndexes.Count;


                    //? 용량을 적절하게 설정한다
                    PlayingAnimationTracks = new Dictionary<int, Track>(capacity);
                    EnablePlayingAnimationTrackIndexes = new List<int>(capacity / 2);


                    //? TrackIndex들을 순회하면서, 딕셔너리에 미리 모든 Track들을 캐싱한다
                    foreach (var trackIndex in AniCore.Setting.EnumIndex_AniTrack.GetAssignedIndexes)
                    {
                        PlayingAnimationTracks.Add(trackIndex, new Track(skelObject.AnimationTrackMemoInitialCapacity));
                    }

                    //Debug.Log($"--빈애니캐싱시작-- {skelObject.name} {skelObject.name.GetHashCode()}");
                    //foreach (var playingAni in PlayingAnimationTracks)
                    //{
                    //    AniCore.SetAnimationEmpty(playingAni.Key, 0);
                    //}
                    //Debug.Log($"--빈애니캐싱끝-- {skelObject.name} {skelObject.name.GetHashCode()}");
                }



                //! 250224 추가된 빈애니메이션들, 초기화되서 제대로안되서 이거 잘 배치해야하마
                public void EmptyAnimations()
                {
                    //if (PlayingAnimationTracks != null)
                    {
                        //! 트랙 미리 빈거재생시키는게 낫지않아? 그리고 트랙 비울 필요가 있나? <empty>나 N을 넣자
                        //? 그래서 다시 넣음
                        foreach (var playingAni in PlayingAnimationTracks)
                        {
                            AniCore.ExecuteEmptyAnimation(playingAni.Key, 0);
                        }
                    }
                }



                ///======================================================================================================================================================



                //? TrackIndex별로 애니메이션이 재생중인 딕셔너리



                ///<summary>
                ///재생중인 애니메이션들이 TrackIndex별로 저장되어있는 딕셔너리
                ///<para>Key: TrackIndex</para>
                ///<para>Value: 해당 <see cref="Track"/> 클래스</para>
                /// </summary>
                private readonly Dictionary<int, Track> PlayingAnimationTracks;



                /// <summary>
                /// <b>활성화</b> 되어있는 재생중인 애니메이션들의 TrackIndex만을 저장하는 리스트
                /// <para>딕셔너리를 사용함에도, 별도로 사용하는 이유는</para>
                /// <para>반복문에서의 최대한 빠른 접근을 위해 사용한다</para>
                /// <para>제거의 최대 비용이 O(n)인 부분은 감수한다</para>
                /// </summary>
                private readonly List<int> EnablePlayingAnimationTrackIndexes;



                /// <summary>
                /// <see cref="PlayingAnimationTracks"/> 얻기
                /// </summary>
                public IReadOnlyDictionary<int, Track> GetPlayingAnimationTracks => PlayingAnimationTracks;



                /// <summary>
                /// <see cref="EnablePlayingAnimationTrackIndexes"/> 얻기
                /// </summary>
                public IReadOnlyList<int> GetEnablePlayingAnimationTrackIndexes => EnablePlayingAnimationTrackIndexes;



                ///======================================================================================================================================================



                //? 트랙 활성화/비활성화



                /// <summary>
                /// <paramref name="trackIndex"/>의 트랙을 실행할 애니메이션과 함께 <b>활성화</b> 한다
                /// <para>해당 <paramref name="trackIndex"/>가 없어도, 새로 추가되어 <b>활성화</b> 된다</para>
                /// </summary>
                public void EnableTrack(int trackIndex, ISkelAni skelAni)
                {
                    //? 해당 trackIndex가 존재하지 않을경우, 새롭게 추가한다
                    if (!PlayingAnimationTracks.ContainsKey(trackIndex))
                    {
                        AddNewTrack(trackIndex);
                    }

                    EnablePlayingAnimationTrackIndexes.Add(trackIndex);
                    PlayingAnimationTracks[trackIndex].EnableTrack(skelAni);
                }



                /// <summary>
                /// <paramref name="trackIndex"/>의 트랙을 <b>비활성화</b> 한다
                /// </summary>
                public void DisableTrack(int trackIndex)
                {
                    if (!IsActiveTrack(trackIndex)) { return; }

                    EnablePlayingAnimationTrackIndexes.Remove(trackIndex);
                    PlayingAnimationTracks[trackIndex].DisableTrack();
                }



                ///<summary>
                /// <paramref name="trackIndex"/> 트랙을 새롭게 추가한다
                /// <para>대부분 생성자에서, 모든 트랙들이 생성되지만,</para>
                /// <para>커스텀 트랙 인덱스에서, 기존에 설정된 트랙 외에 트랙에서 실행될때를 위해 사용한다</para>
                /// </summary>
                public bool AddNewTrack(int trackIndex)
                {
                    if (IsActiveTrack(trackIndex)) { return false; }

                    PlayingAnimationTracks.EnsureCapacity(PlayingAnimationTracks.Count + 1);
                    PlayingAnimationTracks.Add(trackIndex, new Track(SkelObject.AnimationTrackMemoInitialCapacity));

                    return true;
                }



                ///======================================================================================================================================================



                //? 트랙 접근 및 조회



                /// <summary>
                /// <paramref name="trackIndex"/>의 트랙이 존재하는지 확인한다
                /// </summary>
                public bool ContainsTrack(int trackIndex)
                {
                    return PlayingAnimationTracks.ContainsKey(trackIndex);
                }



                /// <summary>
                /// <paramref name="trackIndex"/>의 트랙이 존재하고, <b>활성화</b> 되어있는지 확인한다
                /// </summary>
                public bool IsActiveTrack(int trackIndex)
                {
                    if (!PlayingAnimationTracks.ContainsKey(trackIndex)) { return false; }
                    return PlayingAnimationTracks[trackIndex].IsEnable;
                }



                /// <summary>
                /// 재생중인 트랙을 얻는다
                /// <para><paramref name="trackIndex"/>가 없다면 null을 반환한다</para>
                /// </summary>
                public Track GetTrack(int trackIndex)
                {
                    return PlayingAnimationTracks.TryGetValue(trackIndex, out var resultTrack) ? resultTrack : null;
                }



                /// <summary>재생중인 애니메이션의 ISkelAni 얻기</summary>
                /// <param name="trackIndex">트랙 번호</param>
                public ISkelAni GetSkelAnimation(int trackIndex)
                {
                    return TryGetTrack(trackIndex, out var resultTrack) ? resultTrack.PlayingSkelAni : null;
                }



                /// <summary>
                /// 재생중인 트랙을 얻어본다
                /// </summary>
                public bool TryGetTrack(int trackIndex, out Track resultTrack)
                {
                    return PlayingAnimationTracks.TryGetValue(trackIndex, out resultTrack);
                }



                /// <summary>
                /// 재생중인 모든 트랙을 <b>비활성화</b>한다
                /// </summary>
                public void DisableAllTrack()
                {
                    for (int i = 0; i < EnablePlayingAnimationTrackIndexes.Count; i++)
                    {
                        int trackIndex = EnablePlayingAnimationTrackIndexes[i];
                        PlayingAnimationTracks[trackIndex].DisableTrack();
                    }

                    #region Legacy
                    //foreach (var playingAnimation in PlayingAnimationTracks)
                    //{
                    //    playingAnimation.Value.DisableTrack();
                    //} 
                    #endregion
                }



                ///======================================================================================================================================================



                //? 애니메이션 업데이트



                ///<summary>
                ///애니메이션 플레이어를 업데이트한다
                ///<para>주로 애니메이션의 TimeScale, MultipleTime, BonusSpeed 연산이 매 프레임 마다 이루어진다</para>
                ///</summary>
                public void UpdatePlayer(float timeScale)
                {
                    AniCore.SkelAnimation.AnimationState.TimeScale = timeScale;

                    for (int i = 0; i < EnablePlayingAnimationTrackIndexes.Count; i++)
                    {
                        int trackIndex = EnablePlayingAnimationTrackIndexes[i];
                        Track playingTrack = PlayingAnimationTracks[trackIndex];
                        AniCore.SkelAnimation.AnimationState.Tracks.Items[trackIndex].TimeScale = playingTrack.GetMultipleTime() * playingTrack.PlayingSkelAni.BonusSpeed;
                    }

                    #region Legacy

                    //트랙번호 배열 순환
                    //foreach (var item in PlayingAnimationTracks)
                    //{
                    //    int trackIndex = item.Key;
                    //    Track playingTrack = item.Value;

                    //    //? PlayingTrack 이 활성화가 되어있다면
                    //    if (playingTrack.IsEnable)
                    //    {
                    //        //? 타임스케일 연산
                    //        AnimationState.GetCurrent(trackIndex).TimeScale = playingTrack.GetMultipleTime() * playingTrack.PlayingSkelAni.BonusSpeed;
                    //    }
                    //}

                    #endregion
                }



                ///======================================================================================================================================================
            }
        }
    }
}
