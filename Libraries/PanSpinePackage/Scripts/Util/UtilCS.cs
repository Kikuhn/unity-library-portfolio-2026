using Pan.SpineUtil;
using Pan.Util;
using Spine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;



namespace Pan.SpinePackage
{
    public static class SU_SpinePackage
    {

        /// <summary>
        /// <para>
        /// 기준 드로우오더(<paramref name="baseOrder"/>)를 유지하면서,
        /// 특정 조건(<paramref name="targetSlotCondition"/>)을 만족하는 슬롯(=이동 대상)만
        /// 참조 드로우오더(<paramref name="targetOrder"/>)의 상대적인 상하관계(순서)를 반영해 재배치합니다.
        /// </para>
        /// <para>
        /// 핵심 목표: "이동 대상"을 <paramref name="getSlotDataBundleKey"/> 기준 번들 단위로 다루고,
        /// 번들 내부 순서는 <paramref name="targetOrder"/>를 따르되, 나머지 슬롯들의 상대 순서는 <paramref name="baseOrder"/>를 그대로 유지합니다.
        /// </para>
        /// </summary>
        /// <param name="baseOrder">
        /// <para>
        /// 기준이 되는 드로우오더(Index 배열).
        /// </para>
        /// <para>
        /// "이동 대상이 아닌 슬롯"은 이 배열의 상대 순서를 유지합니다.
        /// </para>
        /// </param>
        /// <param name="targetOrder">
        /// <para>
        /// 이동 대상 번들의 상대 위치/상하관계를 참고할 드로우오더(Index 배열).
        /// </para>
        /// <para>
        /// 이동 대상 번들 내부 슬롯 순서는 이 배열의 순서를 따릅니다.
        /// </para>
        /// </param>
        /// <param name="getSlotDataByIndex">
        /// <para>
        /// 슬롯 인덱스로 <see cref="SlotData"/>를 얻는 함수.
        /// </para>
        /// <para>
        /// 조건 판정 및 번들 키 추출에 사용됩니다.
        /// </para>
        /// </param>
        /// <param name="getSlotDataBundleKey">
        /// <para>
        /// 슬롯의 번들 키를 추출하는 함수.
        /// </para>
        /// <para>
        /// 이동은 "슬롯 단위"가 아니라 "번들 단위"로 수행됩니다.
        /// </para>
        /// </param>
        /// <param name="targetSlotCondition">
        /// <para>
        /// 이동 대상으로 판단할 조건(예: "{HEAD}" 포함 등).
        /// </para>
        /// <para>
        /// true인 슬롯들은 번들 단위로 재배치 대상이 됩니다.
        /// </para>
        /// </param>
        /// <returns>
        /// <para>
        /// 재배치된 드로우오더(Index 배열).
        /// </para>
        /// <para>
        /// 반환 배열의 방향(앞/뒤)은 입력 <paramref name="baseOrder"/>와 동일한 방향으로 되돌려 반환합니다.
        /// </para>
        /// </returns>
        public static int[] RebuildDrawOrderByReferenceBundles(
            int[] baseOrder,
            int[] targetOrder,
            Func<int, SlotData> getSlotDataByIndex,
            Func<SlotData, string> getSlotDataBundleKey,
            Func<SlotData, bool> targetSlotCondition
            )
        {
            //! 입력이 비정상이어도 예외 없이 "최대한" 안전한 결과를 반환
            if (baseOrder == null || baseOrder.Length == 0)
                return Array.Empty<int>();

            if (targetOrder == null || targetOrder.Length == 0)
            {
                //. target이 없으면 base를 그대로 반환(복사)
                var copy = new int[baseOrder.Length];
                for (int i = 0; i < baseOrder.Length; i++)
                    copy[i] = baseOrder[i];
                return copy;
            }

            // =========================================================================
            //? 0) 내부 연산은 "view(앞→뒤)" 기준으로 진행
            //? - Spine drawOrder 배열은 보통 "뒤→앞" 방향인 경우가 많아서
            //?   사람이 보는 논리(앞→뒤)로 뒤집어서 상대관계를 계산한 뒤,
            //?   마지막에 baseOrder와 같은 방향으로 되돌려 반환한다.
            // =========================================================================
            int baseLen = baseOrder.Length;
            int targetLen = targetOrder.Length;

            int[] baseView = new int[baseLen];
            for (int i = 0; i < baseLen; i++)
                baseView[i] = baseOrder[baseLen - 1 - i];

            int[] targetView = new int[targetLen];
            for (int i = 0; i < targetLen; i++)
                targetView[i] = targetOrder[targetLen - 1 - i];

            // =========================================================================
            //? 1) 이동 대상(movable) 판정 (base 기준)
            //? - "조건을 만족하는 슬롯"을 HashSet으로 잡고, 나머지는 고정(fixed)으로 취급한다.
            // =========================================================================
            var movableSet = new HashSet<int>(baseLen);

            for (int i = 0; i < baseLen; i++)
            {
                int slotIndex = baseOrder[i];
                SlotData slotData = getSlotDataByIndex(slotIndex);

                if (targetSlotCondition.Invoke(slotData))
                    movableSet.Add(slotIndex);
            }

            // =========================================================================
            //? 2) base의 fixed 시퀀스(view 기준) 생성
            //? - 이후 target을 "회전(cut)"할 때 기준 시퀀스로 사용한다.
            // =========================================================================
            var fixedSeqBaseView = new List<int>(baseLen);

            for (int i = 0; i < baseView.Length; i++)
            {
                int s = baseView[i];

                if (movableSet.Contains(s))
                    continue;

                fixedSeqBaseView.Add(s);
            }

            // =========================================================================
            //? 3) targetView 회전(cut) 지점 찾기
            //? - movable을 제외했을 때 target의 fixed 시퀀스가 base의 fixed 시퀀스와
            //?   동일해지는 시작점을 찾아, "회전된 targetView(rotTargetView)"를 만든다.
            //?
            //? - 이렇게 해두면, base와 target이 같은 방향성을 공유하는 상태가 되어
            //?   상대관계 계산이 안정적으로 동작한다.
            // =========================================================================
            int bestStart = 0;

            if (targetView.Length > 0 && fixedSeqBaseView.Count > 0)
            {
                for (int start = 0; start < targetView.Length; start++)
                {
                    int fi = 0;
                    bool ok = true;

                    for (int step = 0; step < targetView.Length; step++)
                    {
                        int idx = start + step;
                        if (idx >= targetView.Length) idx -= targetView.Length;

                        int s = targetView[idx];

                        if (movableSet.Contains(s))
                            continue;

                        //! fixed 시퀀스 비교: 순서가 다르면 이 start는 실패
                        if (fi >= fixedSeqBaseView.Count || s != fixedSeqBaseView[fi])
                        {
                            ok = false;
                            break;
                        }

                        fi++;
                    }

                    if (ok && fi == fixedSeqBaseView.Count)
                    {
                        bestStart = start;
                        break;
                    }
                }
            }

            int[] rotTargetView = new int[targetView.Length];
            for (int step = 0; step < targetView.Length; step++)
            {
                int idx = bestStart + step;
                if (idx >= targetView.Length) idx -= targetView.Length;

                rotTargetView[step] = targetView[idx];
            }

            // =========================================================================
            //? 4) movable을 "번들 단위"로 수집 (rotTargetView 순서 기준)
            //? - 번들 내부 슬롯 순서는 rotTargetView 등장 순서를 따른다.
            //? - bundleMinPos / bundleMaxPos는 해당 번들의 위치 범위를 나타낸다.
            // =========================================================================
            var movableBundleSlotsInTargetViewOrder = new Dictionary<string, List<int>>();
            var movableBundleOrderInTargetView = new List<string>();

            var bundleMinPos = new Dictionary<string, int>();
            var bundleMaxPos = new Dictionary<string, int>();

            var movableAdded = new HashSet<int>(movableSet.Count);

            for (int i = 0; i < rotTargetView.Length; i++)
            {
                int s = rotTargetView[i];

                if (movableSet.Contains(s) == false)
                    continue;

                //! 중복 슬롯 방지(안정성)
                if (movableAdded.Add(s) == false)
                    continue;

                SlotData sd = getSlotDataByIndex(s);
                string bk = getSlotDataBundleKey(sd);

                if (movableBundleSlotsInTargetViewOrder.TryGetValue(bk, out var slotList) == false)
                {
                    slotList = new List<int>();
                    movableBundleSlotsInTargetViewOrder.Add(bk, slotList);
                    movableBundleOrderInTargetView.Add(bk);
                }

                slotList.Add(s);

                if (bundleMinPos.ContainsKey(bk) == false)
                {
                    bundleMinPos.Add(bk, i);
                    bundleMaxPos.Add(bk, i);
                }
                else
                {
                    if (i < bundleMinPos[bk]) bundleMinPos[bk] = i;
                    if (i > bundleMaxPos[bk]) bundleMaxPos[bk] = i;
                }
            }

            //? target에 존재하지 않는 movable 슬롯이 있으면 baseView 순서로 보강
            //? (희귀 케이스 대응: "가능하면 제자리 유지" 쪽으로 유도)
            if (movableAdded.Count < movableSet.Count)
            {
                for (int i = 0; i < baseView.Length; i++)
                {
                    int s = baseView[i];

                    if (movableSet.Contains(s) == false)
                        continue;

                    if (movableAdded.Add(s) == false)
                        continue;

                    SlotData sd = getSlotDataByIndex(s);
                    string bk = getSlotDataBundleKey(sd);

                    if (movableBundleSlotsInTargetViewOrder.TryGetValue(bk, out var slotList) == false)
                    {
                        slotList = new List<int>();
                        movableBundleSlotsInTargetViewOrder.Add(bk, slotList);
                        movableBundleOrderInTargetView.Add(bk);
                    }

                    slotList.Add(s);

                    if (bundleMinPos.ContainsKey(bk) == false)
                    {
                        //. rotTargetView 뒤쪽으로 보내서 "불필요한 이동"을 줄이는 쪽으로 유도
                        int pseudoPos = rotTargetView.Length + i;
                        bundleMinPos.Add(bk, pseudoPos);
                        bundleMaxPos.Add(bk, pseudoPos);
                    }
                }
            }

            // =========================================================================
            //? 5) baseView에서 movable을 제거한 fixed 리스트 + 인덱스 맵 구성
            //? - 이후 "삽입 위치"는 fixed 리스트의 인덱스 공간에서 계산한다.
            // =========================================================================
            var baseFixedView = new List<int>(baseView.Length);
            var baseFixedIndex = new Dictionary<int, int>(baseView.Length);

            for (int i = 0; i < baseView.Length; i++)
            {
                int s = baseView[i];

                if (movableSet.Contains(s))
                    continue;

                baseFixedIndex[s] = baseFixedView.Count;
                baseFixedView.Add(s);
            }

            // =========================================================================
            //? 6) 번들별 삽입 위치 계산
            //?
            //? 규칙(상하관계 유지):
            //? - rotTargetView에서 "번들보다 위(앞)"에 있던 fixed는 base에서도 번들보다 위에 있어야 한다.
            //? - rotTargetView에서 "번들보다 아래(뒤)"에 있던 fixed는 base에서도 번들보다 아래에 있어야 한다.
            //?
            //? 이를 fixed 인덱스 공간으로 환산하여:
            //? - afterIndex : 번들보다 위에 있어야 하는 fixed의 최대 인덱스
            //? - beforeIndex: 번들보다 아래에 있어야 하는 fixed의 최소 인덱스
            //? - insertIndex = afterIndex + 1 (단, beforeIndex를 넘지 않게 clamp)
            // =========================================================================
            var bundlesByInsertIndex = new Dictionary<int, List<string>>();

            for (int b = 0; b < movableBundleOrderInTargetView.Count; b++)
            {
                string bk = movableBundleOrderInTargetView[b];

                if (bundleMinPos.TryGetValue(bk, out int minPos) == false)
                    continue;

                int maxPos = bundleMaxPos[bk];

                int afterIndex = -1;
                int beforeIndex = baseFixedView.Count;

                for (int i = 0; i < rotTargetView.Length; i++)
                {
                    int s = rotTargetView[i];

                    //! movable은 fixed 관계의 기준이 될 수 없다
                    if (movableSet.Contains(s))
                        continue;

                    //! base에 존재하는 fixed만 관계 계산 대상으로 인정
                    if (baseFixedIndex.TryGetValue(s, out int bi) == false)
                        continue;

                    if (i < minPos)
                    {
                        if (bi > afterIndex)
                            afterIndex = bi;
                    }
                    else if (i > maxPos)
                    {
                        if (bi < beforeIndex)
                            beforeIndex = bi;
                    }
                }

                int insertIndex = afterIndex + 1;

                if (insertIndex < 0) insertIndex = 0;
                if (insertIndex > baseFixedView.Count) insertIndex = baseFixedView.Count;

                //! 충돌 시: below 제약을 우선(가능하면 위로 붙인다)
                if (insertIndex > beforeIndex)
                    insertIndex = beforeIndex;

                if (bundlesByInsertIndex.TryGetValue(insertIndex, out var keyList) == false)
                {
                    keyList = new List<string>();
                    bundlesByInsertIndex.Add(insertIndex, keyList);
                }

                //. 같은 insertIndex로 모이는 번들은 target에서의 등장 순서대로 추가
                keyList.Add(bk);
            }

            // =========================================================================
            //? 7) 최종 view 구성
            //? - baseFixedView 사이사이에, 계산된 insertIndex에 해당 번들 슬롯들을 삽입한다.
            // =========================================================================
            var finalView = new List<int>(baseView.Length);

            for (int i = 0; i <= baseFixedView.Count; i++)
            {
                if (bundlesByInsertIndex.TryGetValue(i, out var keyList) && keyList != null && keyList.Count > 0)
                {
                    for (int k = 0; k < keyList.Count; k++)
                    {
                        string bk = keyList[k];

                        if (movableBundleSlotsInTargetViewOrder.TryGetValue(bk, out var slotList) == false || slotList == null)
                            continue;

                        for (int s = 0; s < slotList.Count; s++)
                            finalView.Add(slotList[s]);
                    }
                }

                if (i < baseFixedView.Count)
                    finalView.Add(baseFixedView[i]);
            }

            // =========================================================================
            //? 8) 반환값: view(앞→뒤)에서 원본 방향(뒤→앞 등, baseOrder와 동일)으로 되돌려 반환
            // =========================================================================
            int[] resultArray = new int[finalView.Count];

            for (int i = 0; i < finalView.Count; i++)
                resultArray[i] = finalView[finalView.Count - 1 - i];

            return resultArray;
        }


        /// <summary>
        /// 제약이 없는 <typeparamref name="TSkin"/>의 정체를 파악해 <see cref="Spine.Skin"/> 으로 반환하는 메서드
        /// </summary>
        /// <typeparam name="TSkin"></typeparam>
        /// <param name="skin"></param>
        /// <returns></returns>
        public static Skin GetSkin_UnknownTypeSkin<TSkin>(TSkin skin)
        {
            switch (skin)
            {
                case Spine.Skin skin_Skin: return skin_Skin;
                case ExtendSkin skin_ExtendSkin: return skin_ExtendSkin.MainSkin;
                default: return null;
            }
        }



    }



    /// <summary>
    /// 턴뷰 방향(<see cref="ETurnViews8"/>) 분류 규칙 확장 메서드
    /// <para>도메인 분류 로직을 한 곳에 모아 유지보수성을 높인다</para>
    /// </summary>
    public static class TurnViewExtensions
    {
        /// <summary>
        /// <paramref name="direction"/> 이 <paramref name="rule"/> 규칙에 해당하는지 여부를 반환한다
        /// <para>규칙(분류) 기반으로 방향을 판정해야 할 때 사용한다</para>
        /// <para>예: FrontFamily / BackFamily / Left / Right / FrontOrBackOnly 등</para>
        /// </summary>
        /// <param name="direction">
        /// 판정 대상이 되는 턴-뷰 방향 값
        /// </param>
        /// <param name="rule">
        /// 적용할 분류 규칙
        /// </param>
        /// <returns>
        /// 규칙에 해당하면 true, 그렇지 않으면 false
        /// </returns>
        public static bool Is(this ETurnViews8 direction, ETurnViewRule rule)
        {
            switch (rule)
            {
                case ETurnViewRule.FrontFamily:
                return direction == ETurnViews8.Front
                    || direction == ETurnViews8.Front_L
                    || direction == ETurnViews8.Front_R;

                case ETurnViewRule.BackFamily:
                return direction == ETurnViews8.Back
                    || direction == ETurnViews8.Back_L
                    || direction == ETurnViews8.Back_R;

                case ETurnViewRule.Left:
                return direction == ETurnViews8.Front_L
                    || direction == ETurnViews8.Side_L
                    || direction == ETurnViews8.Back_L;

                case ETurnViewRule.Right:
                return direction == ETurnViews8.Front_R
                    || direction == ETurnViews8.Side_R
                    || direction == ETurnViews8.Back_R;

                case ETurnViewRule.FrontOrBackOnly:
                return direction == ETurnViews8.Front
                    || direction == ETurnViews8.Back;

                default:
                return false;
            }
        }



        /// <summary>
        /// 전달된 <see cref="ETurnViews8"/> 값이 좌/우(L/R) 타입일 경우,
        /// 반대편 방향으로 변환하여 <paramref name="opposite"/> 로 반환합니다.
        /// 
        /// Front_L → Front_R
        /// Side_R  → Side_L
        /// Back_L  → Back_R
        /// 
        /// Front, Back 처럼 좌/우 개념이 없는 경우에는
        /// false 를 반환하며 <paramref name="opposite"/> 는 기본값으로 설정됩니다.
        /// </summary>
        /// <param name="turnView">
        /// 반전 여부를 검사할 <see cref="ETurnViews8"/> 값
        /// </param>
        /// <param name="opposite">
        /// 좌/우 반전된 <see cref="ETurnViews8"/> 결과값
        /// </param>
        /// <returns>
        /// 좌/우 타입일 경우 true,
        /// 그렇지 않으면 false
        /// </returns>
        public static bool TryGetOppositeLR(this ETurnViews8 turnView, out ETurnViews8 opposite)
        {
            switch (turnView)
            {
                case ETurnViews8.Front_L:
                opposite = ETurnViews8.Front_R;
                return true;

                case ETurnViews8.Front_R:
                opposite = ETurnViews8.Front_L;
                return true;

                case ETurnViews8.Side_L:
                opposite = ETurnViews8.Side_R;
                return true;

                case ETurnViews8.Side_R:
                opposite = ETurnViews8.Side_L;
                return true;

                case ETurnViews8.Back_L:
                opposite = ETurnViews8.Back_R;
                return true;

                case ETurnViews8.Back_R:
                opposite = ETurnViews8.Back_L;
                return true;

                //! 좌/우 개념이 없는 경우 (Front, Back 등)
                default:
                opposite = default;
                return false;
            }
        }
    }

}