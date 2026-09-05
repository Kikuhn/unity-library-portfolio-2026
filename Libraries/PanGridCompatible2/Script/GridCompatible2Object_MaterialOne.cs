using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Text;
using UnityEngine;



namespace Pan.GridCompatibles2
{
    public class GridCompatible2Object_MaterialOne : GridCompatible2Object
    {
        [BoxGroup("Material One 설정")]
        [LabelText("마테리얼 One")]
        [SerializeField]
        [PropertyOrder(100)]
#if UNITY_EDITOR
        [OnValueChanged(nameof(RefreshMaterialOnee))]
#endif
        private Material materialOne;
        public Material MaterialOne => materialOne;


        [BoxGroup("Material One 설정")]
        [LabelText("Material의 전체 배열에 적용")]
        [SerializeField]
        [PropertyOrder(101)]
#if UNITY_EDITOR
        [OnValueChanged(nameof(RefreshMaterialOnee))]
#endif
        private bool ApplyMaterialOneToAllChildren = false;



#if UNITY_EDITOR

        [BoxGroup("Material One 설정")]
        [Button("Material One 수동 갱신", Icon = SdfIconType.ArrowClockwise, ButtonAlignment = 1f, Stretch = false), GUIColor(0.97f, 0.85f, 0.39f)]
        [PropertyOrder(102)]
        private void RefreshMaterialOnee()
        {
            ReplaceMaterialsInChildren(gameObject, materialOne, ApplyMaterialOneToAllChildren);
        }



        /// <summary>
        /// 하위 모든 오브젝트의 Renderer 컴포넌트를 찾아, Material을 새로 할당한다.
        /// </summary>
        /// <param name="target">Material을 변경할 대상 GameObject.</param>
        /// <param name="newMaterial">적용할 새 Material.</param>
        /// <param name="applyAll">Material의 전체 배열에 적용할지, 0번째 Material에만 적용할지 여부.</param>
        private static void ReplaceMaterialsInChildren(GameObject target, Material newMaterial, bool applyAll)
        {
            //! 필수 파라미터 누락 시 조기 종료
            if (target == null || newMaterial == null) { return; }

            //. 디버그용 문자열 빌더 (에디터 전용)
            string newMaterialName = newMaterial.name;
            System.Text.StringBuilder debugStr = new System.Text.StringBuilder();

            //. 하위의 모든 Renderer 수집 (비활성 오브젝트 포함)
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
            int applyCount = 0;

            foreach (Renderer renderer in renderers)
            {
                debugStr.AppendLine(renderer.name);   //? 어떤 오브젝트에 적용됐는지 기록
                applyCount++;

                if (applyAll)
                {
                    Material[] mats = renderer.sharedMaterials;      //? 기존 배열 참조
                    for (int i = 0; i < mats.Length; ++i) { mats[i] = newMaterial; }
                    renderer.sharedMaterials = mats;                 //! 배열 전체 교체
                }
                else
                {
                    renderer.sharedMaterial = newMaterial;           //! 첫 슬롯만 교체
                }
            }
            Debug.Log(
                $"<b>{target.name}</b> 하위 <b>{applyCount}</b>개 Renderer의 Material을 <b>{newMaterialName}</b> 로 교체했습니다 (배열 전체 적용: {applyAll})\n{debugStr}"
            );
        }

#endif
    }
}
