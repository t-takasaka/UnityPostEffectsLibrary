using UnityEngine;
using UnityEngine.Rendering;

namespace UnityPostEffecs
{
    using static Mathf;
    using RT = RenderTexture;
    using RB = RenderBuffer;

    public class ShaderManager
    {
        private Camera camera;
        private Material material = new Material(Shader.Find(shaderName));
        private ShaderHelper helper;
        private int width, height;
        private float invWidth, invHeight;
        private float aspect;

        public bool needsUpdate = true;

        private const int RENDER_TEXTURE_COUNT = 8;
        private RT[] workRT = new RT[RENDER_TEXTURE_COUNT];
        private RB[] workRB = new RB[RENDER_TEXTURE_COUNT];
        private string[] warkRTName = new string[RENDER_TEXTURE_COUNT];
        private RT maskRT = null;

        private const string faceShaderName = "Hidden/PostEffectsMaskFace";
        private const string bodyShaderName = "Hidden/PostEffectsMaskBody";
        private const string shaderName = "Hidden/PostEffects";
        public int SBR_LAYER_MAX { get{ return helper.SBR_LAYER_MAX; } }
        public readonly int RT_WORK0 = 0, RT_ORIG = 1, RT_WORK2 = 2, RT_WORK3 = 3;
        public readonly int RT_WORK4 = 4, RT_WORK5 = 5, RT_WORK6 = 6, RT_WORK7 = 7;
        public readonly int RT_TFM = 2;
        public readonly int RT_SOBEL = 3;
        public readonly int RT_OUTLINE = 4;
        public readonly int RT_SNOISE = 6;
        public readonly int RT_FNOISE = 7;
        public readonly int RT_SBR_HSV = 0;
        public readonly int RT_LERP0 = 5;
        public readonly int RT_LERP1 = 6;
        public readonly int RT_LERP2 = 7;

        public string GetFaceShaderName(){ return faceShaderName; }
        public string GetBodyShaderName(){ return bodyShaderName; }
        public RT GetRT(int index){ return workRT[index]; }

        // ARGBFloatはモバイルでunsupportedのエラーが出るためhalfを使う
        private readonly RenderTextureFormat RENDER_TEXTURE_FORMAT = RenderTextureFormat.ARGBHalf;
        // halfの精度は–60000～60000で小数点以下約3桁。前段のバッファは桁をずらして精度を上げる
        private readonly float CARRY_DIGIT = 10000.0f;

        public ShaderManager(Camera camera, Texture debugTexture)
        {
            this.material.hideFlags = HideFlags.DontSave;
            this.material.SetTexture("_DebugTex", debugTexture);

            this.helper = new ShaderHelper(camera, material);

            // String.Formatが地味に重いのでキャッシュしておく
            for (int i = 0; i < RENDER_TEXTURE_COUNT; ++i){ warkRTName[i] = $"_RT_WORK{i:D1}"; }
        }

        private void InitWorkRT(RT src)
        {
            for (int i = 0; i < workRT.Length; ++i)
            {
                if(workRT[i] == null || workRT[i].width != src.width || workRT[i].height != src.height)
                {
                    if(workRT[i] != null){ workRT[i].Release(); }
                    // GetTemporaryはMipMapが使えないのでnewで確保する
                    // （MipMapが必要な処理だけ別途RTを確保した方が負荷は減る）
                    workRT[i] = new RT(width, height, 24, RENDER_TEXTURE_FORMAT);
                    workRT[i].hideFlags = HideFlags.DontSave;
                    workRT[i].filterMode = FilterMode.Bilinear;
                    workRT[i].useMipMap = true;
                    workRB[i] = workRT[i].colorBuffer;
                    SetTexture(warkRTName[i], workRT[i]);
                }
            }
            Graphics.SetRenderTarget(workRB, workRT[0].depthBuffer);
            SetTexture("_MainTex", src);
            SetTexture("_RT_ORIG", src);
            Blit(src, "Entry");

        }
        private void InitMaskRT(RT src) 
        {
            if(maskRT == null || maskRT.width != src.width || maskRT.height != src.height)
            {
                if(maskRT != null){ maskRT.Release(); }
                maskRT = new RT(width, height, 24, RENDER_TEXTURE_FORMAT);
                maskRT.hideFlags = HideFlags.DontSave;
            }
            Graphics.SetRenderTarget(maskRT.colorBuffer, src.depthBuffer);
            GL.Clear(false, true, Color.clear);
            Blit(src, "MaskBody");
            Blit(src, "MaskFace");

            SetTexture("_RT_MASK", maskRT);
        }
        public void Begin(RT src, CC cc)
        {
            this.width = src.width;
            this.height = src.height;
            this.invWidth = 1.0f / width;
            this.invHeight = 1.0f / height;
            this.aspect = (float)height / (float)width;

            SetFloat("_CCInBlack", cc.InBlack);
            SetFloat("_CCInGamma", cc.InGamma);
            SetFloat("_CCInWhite", cc.InWhite);
            SetFloat("_CCOutBlack", cc.OutBlack);
            SetFloat("_CCOutWhite", cc.OutWhite);
            SetFloat("_CCMulLum", cc.MulLum);
            SetFloat("_CCAddLum", cc.AddLum);

            InitWorkRT(src);
            InitMaskRT(src);
        }
        public void End(){ needsUpdate = false; }

        public void UpdateSBR(SBR sbr)
        {
            if (!needsUpdate) { return; }

            SetTexture("_RT_SBR_HSV", workRT[RT_SBR_HSV]);
            SetInt("_SBRLayerCount", sbr.Count);
            SetFloat("_SBRInvLayerCount", 1.0f / sbr.Count);
            SetFloatArray("_SBRLayerEnable", sbr.Enable);
            SetFloatArray("_SBRMaskType", sbr.MaskType);
            SetFloatArray("_SBRRadius", sbr.Radius);
            SetVectorArray("_SBRTex2Grid", sbr.Tex2grid);
            SetVectorArray("_SBRProgress", sbr.Progress);

            SetFloatArray("_SBRDetailThresholdHigh", sbr.DetailThresholdHigh);
            SetFloatArray("_SBRDetailThresholdLow", sbr.DetailThresholdLow);
            SetFloatArray("_SBRStrokeWidth", sbr.StrokeWidth);
            SetFloatArray("_SBRStrokeLen", sbr.StrokeLen);
            SetFloatArray("_SBRStrokeOpacity", sbr.StrokeOpacity);
            SetFloatArray("_SBRStrokeLenRand", sbr.StrokeLenRand);
            SetVectorArray("_SBRScratchSize", sbr.ScratchSize);
            SetFloatArray("_SBRScratchOpacity", sbr.ScratchOpacity);
            SetVectorArray("_SBRTolerance", sbr.Tolerance);
            SetVectorArray("_SBRAdd", sbr.Add);
            SetVectorArray("_SBRMul", sbr.Mul);

            SetFloatArray("_SBRInvGridX", sbr.InvGridX);
            SetFloatArray("_SBRInvGridY", sbr.InvGridY);
        }
        public void RenderSBR(int src, RT dst) { Blit(src, dst, "SBR"); }
        public void RenderSBR(int src, int dst) { RenderSBR(src, workRT[dst]); }

        public void UpdateHandTremor(WCR wcr)
        {
            if (!needsUpdate) { return; }

            SetFloat("_WCRBleeding", wcr.Bleeding);
            SetFloat("_WCROpacity", wcr.Opacity);
            SetFloat("_WCRHandTremorLen", wcr.HandTremorLen);
            SetFloat("_WCRHandTremorScale", wcr.HandTremorScale);
            SetFloat("_WCRHandTremorDrawCount", wcr.HandTremorDrawCount);
            SetFloat("_WCRHandTremorInvDrawCount", wcr.HandTremorInvDrawCount);
            SetFloat("_WCRHandTremorOverlapCount", wcr.HandTremorOverlapCount);
            SetFloat("_WCRPigmentDispersionScale", wcr.PigmentDispersionScale);
            SetFloat("_WCRTurbulenceFowScale1", wcr.TurbulenceFowScale1);
            SetFloat("_WCRTurbulenceFowScale2", wcr.TurbulenceFowScale2);
        }
        public void RenderHandTremor(int src, RT dst, WCR wcr){ Blit(src, dst, "HandTremor"); }
        public void RenderHandTremor(int src, int dst, WCR wcr) { RenderHandTremor(src, workRT[dst], wcr); }

        public void UpdateWCR(WCR wcr)
        {
            if (!needsUpdate) { return; }

            SetFloat("_WetInWetLenRatio", wcr.WetInWetLenRatio);
            SetFloat("_WetInWetInvLenRatio", wcr.WetInWetInvLenRatio);
            SetFloat("_WetInWetLow", wcr.WetInWetLow);
            SetFloat("_WetInWetHigh", wcr.WetInWetHigh);
            SetFloat("_WetInWetDarkToLight", wcr.WetInWetDarkToLight);
            SetFloat("_WetInWetHueSimilarity", wcr.WetInWetHueSimilarity);
            SetFloat("_EdgeDarkingLenRatio", wcr.EdgeDarkingLenRatio);
            SetFloat("_EdgeDarkingInvLenRatio", wcr.EdgeDarkingInvLenRatio);
            SetFloat("_EdgeDarkingSize", wcr.EdgeDarkingSize);
            SetFloat("_EdgeDarkingScale", wcr.EdgeDarkingScale);
        }
        public void RenderWCR(int src, RT dst, WCR wcr){ Blit(src, dst, "WCR"); }
        public void RenderWCR(int src, int dst, WCR wcr) { RenderWCR(src, workRT[dst], wcr); }
        public void RenderMin(int src, RT dst) { Blit(src, dst, "Min"); }

        public void UpdateAKF(AKF akf)
        {
            if (!needsUpdate) { return; }

            SetFloat("_AKFRadius", akf.Radius);
            SetFloat("_AKFMaskRadius", akf.MaskRadius);
            SetFloat("_AKFSharpness", akf.Sharpness);
            SetInt("_AKFSampleStep", akf.SampleStep);
            SetFloat("_AKFOverlapX", akf.OverlapX);
            SetFloat("_AKFOverlapY", akf.OverlapY);
        }
        public void RenderAKF(int src, RT dst) { Blit(src, dst, "AKF"); }
        public void RenderAKF(int src, int dst) { RenderAKF(src, workRT[dst]); }

        public void UpdateBF(BF bf)
        {
            if (!needsUpdate) { return; }

            SetFloat("_BFSampleLen", bf.SampleLen);
            SetFloat("_BFDomainVariance", bf.DomainVariance);
            SetFloat("_BFDomainBias", bf.DomainBias);
            SetFloat("_BFRangeVariance", bf.RangeVariance);
            SetFloat("_BFRangeBias", bf.RangeBias);
            SetFloat("_BFRangeThreshold", bf.RangeThreshold);
            SetFloat("_BFStepDirScale", bf.StepDirScale);
            SetFloat("_BFStepLenScale", bf.StepLenScale);

            if (!bf.UsePreCalc){ return; }
            SetFloatArray("_BFRangeWeight", bf.RangeWeight);
        }
        public void RenderBF(int src, int dst, int work, BF bf)
        {
            int tangentPass = bf.FlowBased ? helper.pass["FBF"] : helper.pass["BF"];

            for (int i = 0; i < bf.BlurCount; i++)
            {
                SetFloat("_BFOrthogonalize", 1.0f);
                Blit(src, work, "BF");

                SetFloat("_BFOrthogonalize", 0.0f);
                Blit(work, dst, tangentPass);

                src = dst;
            }
        }

        public void UpdateSNN(SNN snn)
        {
            if (!needsUpdate) { return; }

            SetInt("_SNNRadius", snn.Radius);
            SetFloat("_SNNWeight", snn.Weight);
        }
        public void RenderSNN(int src, RT dst) { Blit(src, dst, "SNN"); }
        public void RenderSNN(int src, int dst) { RenderSNN(src, workRT[dst]); }

        public void UpdatePosterize(Posterize pst, bool returnHSV = false)
        {
            if (!needsUpdate) { return; }

            SetFloat("_PosterizeBins", pst.Bins);
            SetFloat("_PosterizeInvBins", pst.InvBins);
            SetFloat("_PosterizeReturnHSV", returnHSV ? 1.0f : 0.0f);
        }
        public void RenderPosterize(int src, RT dst) { Blit(src, dst, "Posterize"); }
        public void RenderPosterize(int src, int dst) { RenderPosterize(src, workRT[dst]); }

        public void UpdateOutline(Outline ol)
        {
            if (!needsUpdate) { return; }

            SetFloat("_OutlineSize", ol.Size);
            SetFloat("_OutlineInvSize", ol.InvSize);
            SetFloat("_OutlineOpacity", ol.Opacity);
            SetFloat("_OutlineDetail", ol.Detail);
            SetFloat("_OutlineDensity", ol.Density);
            SetFloat("_OutlineReverse", ol.Reverse);
        }
        public void RenderOutline(int src, RT dst)
        {
            Blit(src, dst, "Outline");
            // Outlineを使う次のパスのために先にRTを登録しておく
            SetTexture("_RT_OUTLINE", workRT[RT_OUTLINE]);
        }
        public void RenderOutline(int src, int dst) { RenderOutline(src, workRT[dst]); }

        public void UpdateFXDoG(FXDoG fxdog)
        {
            if (!needsUpdate) { return; }

            SetFloat("_FXDoGGradientMaxLen", fxdog.GradientMaxLen);
            SetFloat("_FXDoGTangentMaxLen", fxdog.TangentMaxLen);
            SetFloat("_FXDoGGradientVarianceL", fxdog.GradientVarianceL);
            SetFloat("_FXDoGGradientVarianceS", fxdog.GradientVarianceS);
            SetFloat("_FXDoGTangentVariance", fxdog.TangentVariance);
            SetFloat("_FXDoGSharpness", fxdog.Sharpness);
            SetFloat("_FXDoGSmoothRange", fxdog.SmoothRange);
            SetFloat("_FXDoGThresholdSlope", fxdog.ThresholdSlope);
            SetFloat("_FXDoGThreshold", fxdog.Threshold);
        }
        public void RenderFXDoG(int src, RT dst, int work)
        {
            Blit(src, work, "FXDoGGradient");
            Blit(work, dst, "FXDoGTangent");
        }

        public void RenderMask(RT dst) { Blit(maskRT, dst); }
        public void RenderMask(RT src, int dst) { RenderMask(workRT[dst]); }

        public void RenderSobel(int src, RT dst, float carryDigit = 1.0f)
        {
            // 桁上げして精度を高める
            SetFloat("_SobelCarryDigit", carryDigit);
            Blit(src, dst, "Sobel3");
            // 後段のためにRTを登録しておく
            SetTexture("_RT_SOBEL", workRT[RT_SOBEL]);
            // 後段のために桁下げを登録しておく
            SetFloat("_SobelInvCarryDigit", 1.0f / carryDigit);
        }
        public void RenderSobel(int src, int dst) { RenderSobel(src, workRT[dst], CARRY_DIGIT); }
        public void UpdateGBlur(GBlur gb)
        {
            if (!needsUpdate) { return; }

            SetInt("_GBlurLOD", gb.LOD);
            SetInt("_GBlurTileSize", gb.TileSize);
            SetInt("_GBlurSampleLen", gb.SampleLen);
            SetInt("_GBlurSize", gb.BlurSize);
            SetFloat("_GBlurInvDomainSigma", gb.InvDomainSigma);
            SetFloat("_GBlurDomainVariance", gb.DomainVariance);
            SetFloat("_GBlurDomainBias", gb.DomainBias);
            SetFloat("_GBlurMean", gb.Mean);

            if (!gb.UsePreCalc){ return; }
            SetFloatArray("_GBlurOffsetX", gb.OffsetX);
            SetFloatArray("_GBlurOffsetY", gb.OffsetY);
            SetFloatArray("_GBlurDomainWeight", gb.DomainWeight);
        }
        public void RenderGBlur(int src, RT dst, GBlur gb) 
        { 
            Blit(src, dst, gb.UsePreCalc ? "GBlur2" : "GBlur"); 
        }
        public void RenderGBlur(int src, int dst, GBlur gb) { RenderGBlur(src, workRT[dst], gb); }
        public void UpdateTFM()
        {
            if (!needsUpdate) { return; }
        }
        public void RenderTFM(int src, RT dst)
        {
            Blit(src, dst, "TFM");
            // 後段のためにRTを登録しておく
            SetTexture("_RT_TFM", workRT[RT_TFM]);
        }
        public void RenderTFM(int src) { RenderTFM(src, workRT[RT_TFM]); }

        public void UpdateLIC(LIC lic)
        {
            if (!needsUpdate) { return; }

            SetFloat("_LICScale", lic.Scale);
            SetFloat("_LICMaxLen", lic.MaxLen);
            SetFloat("_LICVariance", lic.Variance);
        }

        public void RenderLIC(RT dst) { Blit(RT_TFM, dst, "LIC"); }
        public void RenderLIC(int dst) { RenderLIC(workRT[dst]); }

        public void UpdateUnsharpMask(UnsharpMask um)
        {
            if (!needsUpdate) { return; }

            SetInt("_UnsharpMaskLOD", um.LOD);
            SetInt("_UnsharpMaskTileSize", um.TileSize);
            SetInt("_UnsharpMaskSampleLen", um.SampleLen);
            SetInt("_UnsharpMaskSize", um.BlurSize);
            SetFloat("_UnsharpMaskInvDomainSigma", um.InvDomainSigma);
            SetFloat("_UnsharpMaskDomainVariance", um.DomainVariance);
            SetFloat("_UnsharpMaskDomainBias", um.DomainBias);
            SetFloat("_UnsharpMaskMean", um.Mean);
            SetFloat("_UnsharpMaskSharpness", um.Sharpness);
        }
        public void RenderUnsharpMask(int src, RT dst, UnsharpMask um){ Blit(src, dst, "UnsharpMask"); }
        public void RenderUnsharpMask(int src, int dst, UnsharpMask um) { RenderUnsharpMask(src, workRT[dst], um); }

        public void RenderRGB2HSV(int src, RT dst) { Blit(src, dst, "RGB2HSV"); }
        public void RenderRGB2HSV(int src, int dst) { RenderRGB2HSV(src, workRT[dst]); }
        public void RenderHSV2RGB(int src, RT dst) { Blit(src, dst, "HSV2RGB"); }
        public void RenderHSV2RGB(int src, int dst) { RenderHSV2RGB(src, workRT[dst]); }
        public void RenderRGB2HSL(int src, RT dst) { Blit(src, dst, "RGB2HSL"); }
        public void RenderRGB2HSL(int src, int dst) { RenderRGB2HSL(src, workRT[dst]); }
        public void RenderHSL2RGB(int src, RT dst) { Blit(src, dst, "HSL2RGB"); }
        public void RenderHSL2RGB(int src, int dst) { RenderHSL2RGB(src, workRT[dst]); }
        public void RenderRGB2YUV(int src, RT dst) { Blit(src, dst, "RGB2YUV"); }
        public void RenderRGB2YUV(int src, int dst) { RenderRGB2YUV(src, workRT[dst]); }
        public void RenderYUV2RGB(int src, RT dst) { Blit(src, dst, "YUV2RGB"); }
        public void RenderYUV2RGB(int src, int dst) { RenderYUV2RGB(src, workRT[dst]); }
        public void RenderRGB2LAB(int src, RT dst) { Blit(src, dst, "RGB2LAB"); }
        public void RenderRGB2LAB(int src, int dst) { RenderRGB2LAB(src, workRT[dst]); }
        public void RenderLAB2RGB(int src, RT dst) { Blit(src, dst, "LAB2RGB"); }
        public void RenderLAB2RGB(int src, int dst) { RenderLAB2RGB(src, workRT[dst]); }

        public void UpdateSNoise(SNoise noise)
        {
            if (!needsUpdate) { return; }

        }
        public void RenderSNoise(RT dst, SNoise noise) { 
            SetVector("_SNOIZE_SIZE", noise.Size);
            SetVector("_SNOIZE_SCALE", noise.Scale);
            SetVector("_SNOIZE_SPEED", noise.Speed);
            SetTexture("_RT_SNOISE", workRT[noise.RT]);
            Blit(RT_ORIG, dst, "SNoise"); 
        }
        public void RenderSNoise(int dst, SNoise noise) { RenderSNoise(workRT[dst], noise); }

        public void UpdateFNoise(FNoise noise)
        {
            if (!needsUpdate) { return; }

            SetVector("_FNOIZE_SIZE", noise.Size);
            SetVector("_FNOIZE_SCALE", noise.Scale);
            SetVector("_FNOIZE_SPEED", noise.Speed);
            SetTexture("_RT_FNOISE", workRT[RT_FNOISE]);
        }
        public void RenderFNoise(RT dst) { Blit(RT_ORIG, dst, "FNoise"); }
        public void RenderFNoise(int dst) { RenderFNoise(workRT[dst]); }
        public void UpdateVNoise()
        {
            if (!needsUpdate) { return; }
        }
        public void RenderVNoise(RT dst) { Blit(RT_ORIG, dst, "VNoise"); }
        public void RenderVNoise(int dst) { RenderVNoise(workRT[dst]); }

        public void UpdateCanvas(Canvas can)
        {
            if (!needsUpdate) { return; }

            SetFloat("_RuledLineDensity", can.RuledLineDensity);
            SetFloat("_RuledLineInvSize", can.RuledLineInvSize);
            SetVector("_RuledLineRotMat", can.RuledLineRotMat);
        }

        private bool lerpBufFlag = true;
        public void RenderLerp(int src, RT dst, float larpRate)
        {
            var input = lerpBufFlag ? workRT[RT_LERP1] : workRT[RT_LERP2];
            var output = lerpBufFlag ? workRT[RT_LERP2] : workRT[RT_LERP1];
            lerpBufFlag = !lerpBufFlag;

            SetTexture("_LerpBuf", input);
            SetFloat("_LerpRate", larpRate);
            Blit(src, output, "Lerp");
            Blit(output, dst);
        }
        public void RenderLerp(int src, int dst, float larpRate){ RenderLerp(src, workRT[dst], larpRate); }

        public void UpdateTest(Test test)
        {
            if (!needsUpdate) { return; }

            SetFloat("_Test0", test.Test0);
            SetFloat("_Test1", test.Test1);
            SetFloat("_Test2", test.Test2);
            SetFloat("_Test3", test.Test3);
            SetFloat("_Test4", test.Test4);
            SetFloat("_Test5", test.Test5);
            SetFloat("_Test6", test.Test6);
            SetFloat("_Test7", test.Test7);
            SetFloat("_Test8", test.Test8);
            SetFloat("_Test9", test.Test9);
        }
        public void RenderTest(int src, RT dst) { Blit(src, dst, "Test"); }
        public void RenderTest(int src, int dst) { RenderTest(src, workRT[dst]); }
        public void UpdateTestBF(TestBF test)
        {
            if (!needsUpdate) { return; }

            SetFloat("_TestBF0", test.TestBF0);
            SetFloat("_TestBF1", test.TestBF1);
            SetFloat("_TestBF2", test.TestBF2);
            SetFloat("_TestBF3", test.TestBF3);
            SetFloat("_TestBF4", test.TestBF4);
            SetFloat("_TestBF5", test.TestBF5);
            SetFloat("_TestBF6", test.TestBF6);
            SetFloat("_TestBF7", test.TestBF7);
            SetFloat("_TestBF8", test.TestBF8);
            SetFloat("_TestBF9", test.TestBF9);
        }
        public void RenderTestBF(int src, RT dst) { Blit(src, dst, "TestBF"); }
        public void RenderTestBF(int src, int dst) { RenderTestBF(src, workRT[dst]); }


        public void Swap(int src, RT dst) { Blit(src, dst); }
        public void Swap(int src, int dst) { Swap(src, workRT[dst]); }
        private void Blit(RT src, RT dst) { Graphics.Blit(src, dst); }
        private void Blit(int src, RT dst) { Blit(workRT[src], dst); }
        private void Blit(RT src, string pass) { Graphics.Blit(src, material, helper.pass[pass]); }
        private void Blit(RT src, RT dst, int pass) { Graphics.Blit(src, dst, material, pass); }
        private void Blit(RT src, RT dst, string pass) { Blit(src, dst, helper.pass[pass]); }
        private void Blit(int src, int dst, string pass) { Blit(src, workRT[dst], pass); }
        private void Blit(int src, RT dst, string pass) { Blit(src, dst, helper.pass[pass]); }
        private void Blit(int src, int dst, int pass) { Blit(src, workRT[dst], pass); }
        private void Blit(int src, RT dst, int pass) { Blit(workRT[src], dst, pass); }
        private void SetInt(string pass, int val) { material.SetInt(helper.prop[pass], val); }
        private void SetFloat(string pass, float val) { material.SetFloat(helper.prop[pass], val); }
        private void SetFloatArray(string pass, float[] val) { material.SetFloatArray(helper.prop[pass], val); }
        private void SetVector(string pass, Vector4 val) { material.SetVector(helper.prop[pass], val); }
        private void SetVectorArray(string pass, Vector4[] val) { material.SetVectorArray(helper.prop[pass], val); }
        private void SetTexture(string pass, RT val) { material.SetTexture(helper.prop[pass], val); }
    }
}


