using Sirenix.OdinInspector;
using System;
using UnityEngine;
using static Pan.StageGenerators.StageGenerator;
using Grid = Pan.StageGenerators.StageGenerator.Grid;



namespace Pan.StageGenerators
{
    [CreateAssetMenu(menuName = CreateAssetMenuInfo.STAGEGEN_GRID_SETTING)]
    public class GridTagSettingSbject : ScriptableObject
    {
        ///======================================================================================================================================================



#if UNITY_EDITOR

        [ShowInInspector, HideLabel, DisplayAsString(Alignment = TextAlignment.Center, EnableRichText = true, Overflow = false), EnableGUI]
        [PropertyOrder(-100)]
        [PropertySpace(8, 8)]
        private string dummy_Title
        {
            get
            {
                return $"<b><size=15>그리드 태그 설정 SO</size></b>\n\"{name}\"";
            }
        }



        ////. 에디터에서 매니저들 상시 WakeUp 시켜주는 OnValidate
        //private void OnValidate()
        //{
        //    //setting.OnValidate(true);
        //}



#endif



        ///======================================================================================================================================================



        //? 적용(추가/제거)할 요소



        public StageGenerator.GridTag ApplyGridTag => applyGridTag;
        [BoxGroup("박스", false), BoxGroup("박스/적용 요소")]
        [BoxGroup("박스/적용 요소/태그박스", false)]
        [LabelText("태그")]
        [SerializeField]
        protected StageGenerator.GridTag applyGridTag;



        public bool ApplyUseOccupied => applyUseOccupied;
        [BoxGroup("박스", false), BoxGroup("박스/적용 요소")]
        [BoxGroup("박스/적용 요소/점유박스", false)]
        [HorizontalGroup("박스/적용 요소/점유박스/점유적용가로")]
        [LabelText("점유 적용 사용")]
        [LabelWidth(100)]
        [SerializeField]
        protected bool applyUseOccupied;



        public bool ApplyOccupied => applyOccupied;
        [BoxGroup("박스", false), BoxGroup("박스/적용 요소")]
        [BoxGroup("박스/적용 요소/점유박스", false)]
        [HorizontalGroup("박스/적용 요소/점유박스/점유적용가로")]
        [EnableIf(nameof(applyUseOccupied))]
        [LabelText("점유 상태")]
        [LabelWidth(90)]
        [SerializeField]
        protected bool applyOccupied;



        public int ApplyOccupiedLayer => applyOccupiedLayer;
        [BoxGroup("박스", false), BoxGroup("박스/적용 요소")]
        [BoxGroup("박스/적용 요소/점유박스", false)]
        [HorizontalGroup("박스/적용 요소/점유박스/점유적용가로")]
        [EnableIf(nameof(applyUseOccupied))]
        [LabelText("레이어")]
        [LabelWidth(90)]
        [SerializeField]
        protected int applyOccupiedLayer = 0;



        public bool ApplyUsePathCost => applyusePathCost;
        [BoxGroup("박스", false), BoxGroup("박스/적용 요소")]
        [BoxGroup("박스/적용 요소/경로비용박스", false)]
        [HorizontalGroup("박스/적용 요소/경로비용박스/경로비용가로")]
        [ToggleLeft, LabelText(" 경로 Cost 사용")]
        [SerializeField] protected bool applyusePathCost = false;



        public int ApplyPathCost => applyPathCost;
        [BoxGroup("박스", false), BoxGroup("박스/적용 요소")]
        [BoxGroup("박스/적용 요소/경로비용박스", false)]
        [HorizontalGroup("박스/적용 요소/경로비용박스/경로비용가로")]
        [LabelText("Cost")]
        [EnableIf(nameof(applyusePathCost)), MinValue(0)]
        [SerializeField] protected int applyPathCost = 0;



        ///======================================================================================================================================================



        //? Enable 조건



        [BoxGroup("박스", false)]
        [BoxGroup("박스/Enable 조건"), HorizontalGroup("박스/Enable 조건/가로")]
        [LabelText("네거티브")]
        [LabelWidth(80)]
        [SerializeField, EnableIf(nameof(enableConditionGridTag_IsActive))]
        protected bool enableConditionGridTag_Negative;



        public bool EnableConditionGridTag_Any => enableConditionGridTag_Any;
        [BoxGroup("박스", false)]
        [BoxGroup("박스/Enable 조건"), HorizontalGroup("박스/Enable 조건/가로")]
        [LabelText("Any")]
        [LabelWidth(40)]
        [SerializeField, EnableIf(nameof(enableConditionGridTag_IsActive))]
        protected bool enableConditionGridTag_Any;



        public StageGenerator.GridTag EnableConditionGridTag => enableConditionGridTag;
        [BoxGroup("박스", false)]
        [BoxGroup("박스/Enable 조건")]
        [LabelText("조건 확인 대상 태그")]
        [SerializeField]
        protected StageGenerator.GridTag enableConditionGridTag;

        private bool enableConditionGridTag_IsActive => enableConditionGridTag != StageGenerator.GridTag.None; public bool EnableConditionGridTag_Negative => enableConditionGridTag_Negative;



        public bool EnableConditionGridTag_UseCheckOccupied => enableConditionGridTag_UseCheckOccupied;
        [BoxGroup("박스", false)]
        [BoxGroup("박스/Enable 조건"), HorizontalGroup("박스/Enable 조건/가로2")]
        [LabelText("점유 확인 사용")]
        [LabelWidth(100)]
        [SerializeField]
        private bool enableConditionGridTag_UseCheckOccupied;



        public bool EnableConditionGridTag_Occupied => enableConditionGridTag_Occupied;
        [BoxGroup("박스", false)]
        [BoxGroup("박스/Enable 조건"), HorizontalGroup("박스/Enable 조건/가로2")]
        [EnableIf(nameof(enableConditionGridTag_UseCheckOccupied))]
        [LabelText("점유 조건")]
        [SerializeField]
        private bool enableConditionGridTag_Occupied;



        ///======================================================================================================================================================


        //? Disable 조건



        public bool DisableConditionGridTag_Any => disableConditionGridTag_Any;
        [BoxGroup("박스", false)]
        [BoxGroup("박스/Disable 조건"), HorizontalGroup("박스/Disable 조건/가로")]
        [LabelText("네거티브")]
        [LabelWidth(80)]
        [SerializeField, EnableIf(nameof(disableConditionGridTag_IsActive))]
        protected bool disableConditionGridTag_Any;



        public bool DisableConditionGridTag_Negative => disableConditionGridTag_Negative;
        [BoxGroup("박스", false)]
        [BoxGroup("박스/Disable 조건"), HorizontalGroup("박스/Disable 조건/가로")]
        [LabelText("Any")]
        [LabelWidth(40)]
        [SerializeField, EnableIf(nameof(disableConditionGridTag_IsActive))]
        protected bool disableConditionGridTag_Negative;



        public StageGenerator.GridTag DisableConditionGridTag => disableConditionGridTag;
        [BoxGroup("박스", false)]
        [BoxGroup("박스/Disable 조건")]
        [LabelText("조건 확인 대상 태그")]
        [SerializeField]
        protected StageGenerator.GridTag disableConditionGridTag;

        private bool disableConditionGridTag_IsActive => disableConditionGridTag != StageGenerator.GridTag.None;



        public bool DisableConditionGridTag_UseCheckOccupied => disableConditionGridTag_UseCheckOccupied;
        [BoxGroup("박스", false)]
        [BoxGroup("박스/Disable 조건"), HorizontalGroup("박스/Disable 조건/가로2")]
        [LabelText("점유 확인 사용")]
        [LabelWidth(100)]
        [SerializeField]
        private bool disableConditionGridTag_UseCheckOccupied;



        public bool DisableConditionGridTag_Occupied => disableConditionGridTag_Occupied;
        [BoxGroup("박스", false)]
        [BoxGroup("박스/Disable 조건"), HorizontalGroup("박스/Disable 조건/가로2")]
        [EnableIf(nameof(disableConditionGridTag_UseCheckOccupied))]
        [LabelText("점유 조건")]
        [SerializeField]
        private bool disableConditionGridTag_Occupied;



        ///======================================================================================================================================================



        //? 이 그리드 태그 설정으로 그리드 설정 (적용/해제)



        private bool CheckConditionInternal_GridTag(Grid grid, StageGenerator.GridTag tag, bool any, bool negative)
        {
            //. 조건 태그가 없으면 바로 통과
            if (tag == StageGenerator.GridTag.None) return true;

            //. Any/All 중 선택
            bool contains = any ? grid.AnyTag(tag) : grid.ContainsTag(tag);

            //. 부정(Negative) 여부 반영
            return negative ? !contains : contains;
        }



        private bool CheckContiionInternal_Occupied(Grid grid, bool useOccupiedCondition, bool occupid)
            => (useOccupiedCondition) ? (grid.IsOccupied == occupid) : true;



        public bool EnableGrid(GridManager gridManager, Grid grid)
        {
            if (!EnableConditionExtend(gridManager, grid)) { return false; }
            if (!CheckConditionInternal_GridTag(grid, enableConditionGridTag, enableConditionGridTag_Any, enableConditionGridTag_Negative)) { return false; }
            if (!CheckContiionInternal_Occupied(grid, enableConditionGridTag_UseCheckOccupied, enableConditionGridTag_Occupied)) { return false; }


            if (applyusePathCost) { grid.PathCost += ApplyPathCost; }
            if (applyUseOccupied) { grid.SetOccupied(applyOccupied, applyOccupiedLayer, true); }

            grid.AddTag(applyGridTag);
            EnableEventExtend(gridManager, grid);

            return true;
        }



        public bool DisableGrid(GridManager gridManager, Grid grid)
        {
            if (!DisableConditionExtend(gridManager, grid)) { return false; }
            if (!CheckConditionInternal_GridTag(grid, disableConditionGridTag, disableConditionGridTag_Any, disableConditionGridTag_Negative)) { return false; }
            if (!CheckContiionInternal_Occupied(grid, disableConditionGridTag_UseCheckOccupied, disableConditionGridTag_Occupied)) { return false; }


            if (applyusePathCost) { grid.PathCost -= ApplyPathCost; }
            if (applyUseOccupied) { grid.RemoveOccupied(applyOccupiedLayer); }
            grid.RemoveTag(applyGridTag);
            DisableEventExtend(gridManager, grid);


            return true;
        }



        ///======================================================================================================================================================



        //? 이 그리드 태그 설정으로 (적용/해제) 할 때, (적용/해제)가 가능한지 조건 확인과 무엇을 (적용/해제) 할지 정하기 (override)



        protected virtual bool EnableConditionExtend(GridManager gridManager, Grid grid) => true;



        protected virtual bool DisableConditionExtend(GridManager gridManager, Grid grid) => true;



        protected virtual void EnableEventExtend(GridManager gridManager, Grid grid) { }



        protected virtual void DisableEventExtend(GridManager gridManager, Grid grid) { }



        ///======================================================================================================================================================
    }
}
