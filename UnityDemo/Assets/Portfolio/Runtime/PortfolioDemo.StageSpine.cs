using System;
using Pan.StageGenerators;
using Pan.SpinePackage;
using Spine.Unity;
using UnityEngine;

namespace Portfolio2026
{
    public partial class PortfolioDemo
    {
        public StageGenerator Stage;
        SkeletonAnimation rig;
        SkelObject skel;
        PortfolioSkelSbject rigSettings;
        SkeletonDataAsset rigData;
        LineRenderer[] boneLines;
        Material boneMaterial;
        bool facingLeft;
        string motion = "idle";

        void InitStage()
        {
            if (Stage == null) { Log("Stage 설정이 필요합니다."); return; }
            GenerateStage();
        }
        void GenerateStage()
        {
            try
            {
                bool result = Stage.GenerateM.Generate(Seed);
                Log("seed=" + Seed + " / Generate=" + result);
                StartCoroutine(LogSettledRoomCount());
                Check(result, "stage generation");
            }
            catch (Exception e) { Failures++; Log(e.GetType().Name + ": " + e.Message); Debug.LogException(e); }
        }
        System.Collections.IEnumerator LogSettledRoomCount()
        { yield return null; Log("활성 방=" + Stage.GetComponentsInChildren<RoomObject>().Length); }
        void StageUI()
        {
            GUILayout.Label("실제 StageGenerator: 공간 분할 → 방 배치 → 복도 연결", textStyle);
            string seedText = GUILayout.TextField(Seed.ToString());
            if (int.TryParse(seedText, out int seed)) Seed = seed;
            if (Button("현재 seed로 생성")) GenerateStage();
            if (Button("다음 seed로 재생성")) { Seed++; GenerateStage(); }
            if (Button("초기화")) { Stage.GenerateM.DestroyStage(true, true); Log("생성 결과 해제"); }
            GUILayout.Label("생성 설정·방 프리팹은 Stage의 Inspector에서 수정할 수 있습니다.", textStyle);
        }
        void InitSpine()
        {
            if (RigJson == null) { Log("직접 만든 최소 리그 JSON을 지정하세요."); return; }
            rigData = ScriptableObject.CreateInstance<SkeletonDataAsset>();
            rigData.skeletonJSON = RigJson;
            rigData.atlasAssets = Array.Empty<AtlasAssetBase>();
            rigData.scale = 1f;
            rigSettings = ScriptableObject.CreateInstance<PortfolioSkelSbject>();
            ((SkelSbject.ISkeletonDataAsset)rigSettings).Skeleton_DataAsset = rigData;
            var go = new GameObject("Own geometric Spine rig");
            go.transform.SetParent(transform);
            go.transform.localPosition = new Vector3(3, -2, 0);
            SkeletonAnimation.AddToGameObject(go, rigData);
            rig = go.GetComponent<SkeletonAnimation>();
            skel = go.AddComponent<SkelObject>();
            skel.CurrentDB = rigSettings;
            boneMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            boneMaterial.color = Accent;
            boneLines = new LineRenderer[rig.Skeleton.Bones.Count];
            for (int i=0;i<boneLines.Length;i++)
            {
                var lineGo = new GameObject("Bone " + rig.Skeleton.Bones.Items[i].Data.Name);
                lineGo.transform.SetParent(go.transform);
                var line = lineGo.AddComponent<LineRenderer>();
                line.sharedMaterial = boneMaterial;
                line.startWidth = line.endWidth = .16f;
                line.positionCount = 2;
                boneLines[i] = line;
            }
            PlayMotion("idle");
            Log("Pan SkelObject + 자체 최소 리그. 일반 전환 시연이며 과거 오류 재현이 아닙니다.");
        }
        void PlayMotion(string name)
        {
            motion = name;
            skel.Ani.ExecuteCustomAnimation(0, name, true, MixSeconds);
            Log("motion=" + name + " / mix=" + MixSeconds.ToString("F2"));
        }
        void UpdateSpine()
        {
            if (rig == null || rig.Skeleton == null) return;
            rig.timeScale = AnimationSpeed;
            rig.transform.localScale = new Vector3(facingLeft ? -1 : 1, 1, 1);
            for (int i=0;i<boneLines.Length;i++)
            {
                var bone = rig.Skeleton.Bones.Items[i];
                var pose = bone.Pose;
                var start = new Vector3(pose.WorldX, pose.WorldY, 0);
                var end = new Vector3(pose.WorldX + pose.A * bone.Data.Length, pose.WorldY + pose.C * bone.Data.Length, 0);
                boneLines[i].SetPosition(0, rig.transform.TransformPoint(start));
                boneLines[i].SetPosition(1, rig.transform.TransformPoint(end));
            }
        }
        void SpineUI()
        {
            GUILayout.Label("자체 도형 리그의 모션 혼합과 방향 변경", textStyle);
            if (skel == null) return;
            if (Button("Idle")) PlayMotion("idle");
            if (Button("Swing")) PlayMotion("swing");
            if (Button("방향 변경")) { facingLeft = !facingLeft; Log("facing=" + (facingLeft ? "Left" : "Right")); }
            if (Button("현재 모션 재진입")) PlayMotion(motion);
            AnimationSpeed = GUILayout.HorizontalSlider(AnimationSpeed, 0f, 1.5f);
            GUILayout.Label("속도 " + AnimationSpeed.ToString("F2") + " (0=중간 포즈 정지)", textStyle);
            if (Button("초기화")) { facingLeft=false; AnimationSpeed=1; PlayMotion("idle"); }
            GUILayout.Label("기존 실제 리그의 IgnoreBlend 오류 재현·수정 증명과 구분합니다.", textStyle);
        }
        void DisposeSpine()
        {
            if (rig != null) Destroy(rig.gameObject);
            if (rigSettings != null) Destroy(rigSettings);
            if (rigData != null) Destroy(rigData);
            if (boneMaterial != null) Destroy(boneMaterial);
        }
    }
    public sealed class PortfolioSkelSbject : SkelSbject
    {
        // Runtime-created settings receive their data immediately after CreateInstance.
        protected override void OnEnable() { if (Skeleton_DataAsset != null) base.OnEnable(); }
        protected override void WakeUp_Current() { }
    }
}


