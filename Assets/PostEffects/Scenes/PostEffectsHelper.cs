using System;
using UnityEngine;

namespace UnityPostEffecs
{
    using static Mathf;
    using PE = PostEffects;
    using ET = PostEffects.EffectType;

    // 上位レベルの処理でコア機能と関係の薄いものをまとめる
    public class PostEffectsHelper
    {
        public bool initialized = false;
        public ET currentEffect = ET.None;

        private static readonly Array ETypes = Enum.GetValues(typeof(ET));
        private bool[] enableFlagCache = new bool[ETypes.Length];
        private EnableFlagIndexer enableFlagIndexer;

        private GameObject[] models;
        private SkinnedMeshRenderer facialExpression;

        public PostEffectsHelper() { }

        public void Init(PE pe)
        {
            enableFlagIndexer = new EnableFlagIndexer(pe);
            initialized = true;
        }
        public void SetFrameRate(int frameRate)
        {
            QualitySettings.vSyncCount = 0;
            // フレームレートに0が指定されていたら制限無し
            if (frameRate == 0) { frameRate = 9999; }
            Application.targetFrameRate = frameRate;
        }
        public void SetResolutionScale(float resolutionScale)
        {
            // モバイルは解像度を下げて負荷を減らす
#if UNITY_IPHONE || UNITY_ANDROID
            int w = FloorToInt(Screen.width * resolutionScale);
            int h = FloorToInt(Screen.height * resolutionScale);
            Screen.SetResolution(w, h, Screen.fullScreen);
#endif
        }

        public void SetModels(string[] names) 
        { 
            models = new GameObject[names.Length];
            for(var i = 0; i < names.Length; ++i){ models[i] = GameObject.Find(names[i]); }
        }

        // マスクしたいサブメッシュを複製してマテリアルとシェーダを追加する
        public void SetMask(string[] meshNames, string[] materialNames, string shaderName)
        {
            // マスク用マテリアル
            var material = new Material(Shader.Find(shaderName));

            foreach(var model in models)
            {
                if (!model){ continue; }

                foreach(var meshName in meshNames)
                {
                    if (meshName == ""){ continue; }

                    var mesh = model.transform.Find(meshName);
                    var renderer = mesh.GetComponent<SkinnedMeshRenderer>();
                    var sharedMaterials = renderer.sharedMaterials;
                    var sharedMesh = renderer.sharedMesh;

                    int sharedMaterialsCount = sharedMaterials.Length;
                    int subMeshCount = sharedMesh.subMeshCount;

                    // 既存のマスク用マテリアルを剥がしてから付け直す
                    foreach(var sharedMaterial in sharedMaterials)
                    { 
                        if(sharedMaterial.name == shaderName){ --sharedMaterialsCount; } 
                    }

                    if(sharedMaterialsCount != subMeshCount)
                    { 
                        sharedMesh.subMeshCount = Min(sharedMaterialsCount, subMeshCount);
                        subMeshCount = sharedMesh.subMeshCount;
                    }

                    // サブメッシュはsharedMeshの最後に都度追加していく
                    for(int i = 0; i < sharedMaterialsCount; ++i)
                    { 
                        var sharedMaterial = sharedMaterials[i];
                        foreach(var materialName in materialNames)
                        {
                            if(sharedMaterial.name != materialName){ continue; }

                            ++sharedMesh.subMeshCount;
                            var triangles = sharedMesh.GetTriangles(i);
                            sharedMesh.SetTriangles(triangles, sharedMesh.subMeshCount - 1);
                        }
                    }

                    // マテリアルは新規に配列を作成して既存分と追加分を用意した後、
                    // 一括でsharedMaterialsを上書きする
                    var materials = new Material[sharedMesh.subMeshCount];
                    for(int i = 0; i < sharedMaterialsCount; ++i){ materials[i] = renderer.sharedMaterials[i]; }

                    // 別マテリアルに同一シェーダが付くことはあり得るので、
                    // materialNames.Lengthではなく実際に追加されたサブメッシュ数で回す
                    for(int i = 0; i < sharedMesh.subMeshCount - subMeshCount; ++i)
                    { 
                        materials[sharedMaterialsCount + i] = material; 
                    }
                    // sharedMaterialsを上書き
                    renderer.sharedMaterials = materials;
                }
            }
        }
        public void SetFacialExpression(string meshName)
        {
            foreach(var model in models)
            {
                if (!model) { continue; }
                var face = model.transform.Find(meshName);
                facialExpression = face.GetComponent<SkinnedMeshRenderer>();
            }
        }
        public void UpdateFacialExpression()
        {
            foreach(var model in models)
            {
                if (!model) { return; }
                // 25～100の範囲で表情の変化を付ける
                float weight = (Sin(Time.time) + 1.0f) * 37.5f + 25.0f;
                facialExpression.SetBlendShapeWeight(1, weight);
            }
        }

        // 輪郭線の画素が引き伸ばされてチラつくため消し込む
        public void DisableOutline()
        {
            foreach (GameObject obj in MonoBehaviour.FindObjectsOfType(typeof(GameObject)))
            {
                if (!obj.activeInHierarchy) { continue; }

                var skinnedMeshRenderers = obj.GetComponents<SkinnedMeshRenderer>();
                foreach (var skinnedMeshRenderer in skinnedMeshRenderers)
                {
                    foreach (var sharedMaterial in skinnedMeshRenderer.sharedMaterials)
                    {
                        // ※VRM/MToon決め打ち
                        if (sharedMaterial == null) { continue; }
                        if (sharedMaterial.shader.name != "VRM/MToon") { continue; }
                        sharedMaterial.SetFloat("_OutlineWidthMode", 0.0f);
                        sharedMaterial.SetFloat("_OutlineWidth", 0.0f);
                    }
                }
            }
        }

        // UpdateWhenOffscreen有効化
        public void EnableUpdateWhenOffscreen(bool enable)
        {
            var gameObjects = MonoBehaviour.FindObjectsOfType(typeof(GameObject));
            foreach (GameObject gameObject in gameObjects)
            {
                if (!gameObject.activeInHierarchy) { continue; }

                var skinnedMeshRenderers = gameObject.GetComponents<SkinnedMeshRenderer>();
                foreach (var skinnedMeshRenderer in skinnedMeshRenderers)
                {
                    skinnedMeshRenderer.updateWhenOffscreen = enable;
                }
            }
        }

        // インスペクタ側のフラグ群にインデクスアクセスする
        public class EnableFlagIndexer
        {
            PE pe;

            public EnableFlagIndexer(PE pe){ this.pe = pe; }

            public bool this[ET index]
            {
                set
                {
                    switch (index)
                    {
                        case ET.SBR: pe.SBREnable = value; break;
                        case ET.WCR: pe.WCREnable = value; break;
                        case ET.BF: pe.BFEnable = value; break;
                        case ET.AKF: pe.AKFEnable = value; break;
                        case ET.SNN: pe.SNNEnable = value; break;
                        case ET.Outline: pe.OutlineEnable = value; break;
                        case ET.FXDoG: pe.FXDoGEnable = value; break;
                        case ET.Mask: pe.DebugParameters.MaskEnable = value; break;
                        case ET.Sobel: pe.DebugParameters.SobelEnable = value; break;
                        case ET.SST: pe.DebugParameters.SSTEnable = value; break;
                        case ET.TFM: pe.DebugParameters.TFMEnable = value; break;
                        case ET.LIC: pe.DebugParameters.LICEnable = value; break;
                        case ET.GBlur: pe.DebugParameters.GBlurEnable = value; break;
                        case ET.Sharpen: pe.DebugParameters.SharpenEnable = value; break;
                        case ET.Comp: pe.DebugParameters.ComplementaryEnable = value; break;
                        case ET.Posterize: pe.DebugParameters.PosterizeEnable = value; break;
                        case ET.SNoise: pe.DebugParameters.SimplexNoiseEnable = value; break;
                        case ET.FNoise: pe.DebugParameters.FlowNoiseEnable = value; break;
                        case ET.VNoise: pe.DebugParameters.VoronoiNoiseEnable = value; break;
                        case ET.Test: pe.DebugParameters.TestEnable = value; break;
                        case ET.TestBF: pe.DebugParameters.TestBFEnable = value; break;
                    }
                }
                get
                {
                    switch (index)
                    {
                        case ET.SBR: return pe.SBREnable;
                        case ET.WCR: return pe.WCREnable;
                        case ET.BF: return pe.BFEnable;
                        case ET.AKF: return pe.AKFEnable;
                        case ET.SNN: return pe.SNNEnable;
                        case ET.Outline: return pe.OutlineEnable;
                        case ET.FXDoG: return pe.FXDoGEnable;
                        case ET.Mask: return pe.DebugParameters.MaskEnable;
                        case ET.Sobel: return pe.DebugParameters.SobelEnable;
                        case ET.SST: return pe.DebugParameters.SSTEnable;
                        case ET.TFM: return pe.DebugParameters.TFMEnable;
                        case ET.LIC: return pe.DebugParameters.LICEnable;
                        case ET.GBlur: return pe.DebugParameters.GBlurEnable;
                        case ET.Sharpen: return pe.DebugParameters.SharpenEnable;
                        case ET.Comp: return pe.DebugParameters.ComplementaryEnable;
                        case ET.Posterize: return pe.DebugParameters.PosterizeEnable;
                        case ET.SNoise: return pe.DebugParameters.SimplexNoiseEnable;
                        case ET.FNoise: return pe.DebugParameters.FlowNoiseEnable;
                        case ET.VNoise: return pe.DebugParameters.VoronoiNoiseEnable;
                        case ET.Test: return pe.DebugParameters.TestEnable;
                        case ET.TestBF: return pe.DebugParameters.TestBFEnable;
                    }
                    return false;
                }
            }
        }

        // エフェクトタイプのチェックボックスを択一にする
        public void ValidateEnableFlags(PE pe)
        {
            foreach(ET index in ETypes)
            {
                // フラグが非活性でチェックボックスが活性なら押下されたとみなす
                if (!enableFlagCache[(int)index] && enableFlagIndexer[index]) { NextEnableFlags(pe, index); }
            }
            CheckEnableFlags();
        }
        private void NextEnableFlags(PE pe, ET nextEffect)
        {
            foreach(ET index in ETypes)
            {
                // 押下されたチェックボックス以外を全て非活性に変更
                if (nextEffect != index) { enableFlagCache[(int)index] = enableFlagIndexer[index] = false; }
            }

            // 押下された対象がSBRで且つレイヤが無い場合はデフォルトのパラメータを設定する
            if((nextEffect == ET.SBR) && (pe.SBRParameters.Layers.Length == 0)){ pe.SetDefaultParamsSBR(); }

            // 押下されたチェックボックスのフラグを活性に変更
            enableFlagCache[(int)nextEffect] = true;
            currentEffect = nextEffect;
        }
        private void CheckEnableFlags()
        {
            // いずれのエフェクトにもチェックが付いていないならキャッシュをクリアする
            foreach(ET index in ETypes)
            {
                if (enableFlagIndexer[index]) { return; }
            }
            foreach(ET index in ETypes){ enableFlagCache[(int)index] = false; }
            currentEffect = ET.None;
        }

        // エフェクトタイプの切り替え
        public void ChangeEffect(PE pe)
        {
            switch (currentEffect)
            {
                case ET.SBR:
                    pe.WCREnable = true; 
                    pe.SetDefaultParamsWCR();
                    break;
                case ET.WCR: 
                    pe.AKFEnable = true; 
                    pe.SetDefaultParamsAKF();
                    break;
                case ET.AKF: pe.SNNEnable = true; break;
                case ET.SNN: 
                    pe.BFEnable = true; 
                    pe.SetDefaultParamsBF();
                    break;
                case ET.BF: pe.OutlineEnable = true; break;
                case ET.Outline: 
                    pe.SBREnable = true; 
                    pe.SetDefaultParamsSBR();
                    break;
                default: 
                    pe.SBREnable = true; 
                    pe.SetDefaultParamsSBR();
                    break;
            }
            ValidateEnableFlags(pe);
        }

        //エフェクト効果のインクリメント
        private float IncSBR(float v) { return Min(v + 0.1f, InsSBR.GridScaleMax); }
        private float IncWCR(float v) { return Min(v + 10.0f, InsWCR.WetInWetHueSimilarityMax); }
        private float IncAKF(float v) { return Min(v + 1.0f, InsAKF.RadiusMax); }
        private int IncSNN(int v) { return Min(v + 1, InsSNN.RadiusMax); }
        private int IncBF(int v) { return Min(v + 1, InsBF.BlurCountMax); }
        private float IncOutline(float v) { return Min(v + 1.0f, InsOutline.SizeMax); }

        public void IncEffect(PE pe)
        {
            switch (currentEffect)
            {
                case ET.SBR: pe.SBRParameters.GridScale = IncSBR(pe.SBRParameters.GridScale); break;
                case ET.WCR: pe.WCRParameters.WetInWetHueSimilarity = IncWCR(pe.WCRParameters.WetInWetHueSimilarity); break;
                case ET.AKF: pe.AKFParameters.Radius = IncAKF(pe.AKFParameters.Radius); break;
                case ET.SNN: pe.SNNParamters.Radius = IncSNN(pe.SNNParamters.Radius); break;
                case ET.BF: pe.BFParameters.BlurCount = IncBF(pe.BFParameters.BlurCount); break;
                case ET.Outline: pe.OutlineParameters.Size = IncOutline(pe.OutlineParameters.Size); break;
            }
        }

        //エフェクト効果のデクリメント
        private float DecSBR(float v) { return Max(v - 0.1f, InsSBR.GridScaleMin); }
        private float DecWCR(float v) { return Max(v - 10.0f, InsWCR.WetInWetHueSimilarityMin); }
        private float DecAKF(float v) { return Max(v - 1.0f, InsAKF.RadiusMin); }
        private int DecSNN(int v) { return Max(v - 1, InsSNN.RadiusMin); }
        private int DecBF(int v) { return Max(v - 1, InsBF.BlurCountMin); }
        private float DecOutline(float v) { return Max(v - 1.0f, InsOutline.SizeMin); }

        public void DecEffect(PE pe)
        {
            switch (currentEffect)
            {
                case ET.SBR: pe.SBRParameters.GridScale = DecSBR(pe.SBRParameters.GridScale); break;
                case ET.WCR: pe.WCRParameters.WetInWetHueSimilarity = DecWCR(pe.WCRParameters.WetInWetHueSimilarity); break;
                case ET.AKF: pe.AKFParameters.Radius = DecAKF(pe.AKFParameters.Radius); break;
                case ET.SNN: pe.SNNParamters.Radius = DecSNN(pe.SNNParamters.Radius); break;
                case ET.BF: pe.BFParameters.BlurCount = DecBF(pe.BFParameters.BlurCount); break;
                case ET.Outline: pe.OutlineParameters.Size = DecOutline(pe.OutlineParameters.Size); break;
            }
        }
        public void IncLum(PE pe) 
        { 
            CommonOptions.InsCC cc = pe.CommonParameters.CCParameters;
            cc.MulLum = Min(cc.MulLum + 0.1f, CommonOptions.InsCC.MulLumMax); 
        }
        public void DecLum(PE pe) 
        { 
            CommonOptions.InsCC cc = pe.CommonParameters.CCParameters;
            cc.MulLum = Max(cc.MulLum - 0.1f, CommonOptions.InsCC.MulLumMin); 
        }

    }
}

