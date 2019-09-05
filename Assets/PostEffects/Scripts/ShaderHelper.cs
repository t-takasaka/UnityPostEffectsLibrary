using System.Collections.Generic;
using UnityEngine;

namespace UnityPostEffecs
{
    // シェーダ関連の定数など
    public class ShaderHelper
    {
        private static readonly string[] passNames = 
        {
            "Entry", "MaskFace", "MaskBody", "SBR", "WCR", "HandTremor", "BF", "FBF", "AKF", "SNN", 
            "FXDoGGradient", "FXDoGTangent", "Posterize", "Outline",
            "Sobel3", "TFM", "LIC", "GBlur", "GBlur2", "UnsharpMask", 
            "RGB2HSV", "HSV2RGB", "RGB2HSL", "HSL2RGB", "RGB2YUV", "YUV2RGB", "RGB2LAB", "LAB2RGB",
            "GNoise", "SNoise", "FNoise", "VNoise", "Lerp", "Test", "TestBF"
        };
        public Dictionary<string, int> pass = new Dictionary<string, int>();

        // GL_MAX_FRAGMENT_UNIFORM_VECTORSやGL_MAX_FRAGMENT_UNIFORM_COMPONENTSを考慮する
        // ある程度の変数が出揃った段階でレイヤ数の上限を見直す
        // ※シェーダファイル側と定数値を合わせる
        public readonly int SBR_LAYER_MAX = 10;

        private static readonly string[] propNames = 
        {
            "_MainTex", "_RT_WORK0", "_RT_WORK1", "_RT_WORK2", "_RT_WORK3", "_RT_WORK4", "_RT_WORK5", "_RT_WORK6", "_RT_WORK7", 
            "_RT_ORIG", "_RT_MASK", "_RT_SOBEL", "_RT_TFM", "_RT_OUTLINE", "_RT_SNOISE", "_RT_FNOISE", "_RT_SBR_HSV", 
            "_CCMulLum", "_CCAddLum", "_CCInBlack", "_CCInGamma", "_CCInWhite", "_CCOutBlack", "_CCOutWhite", 
            "_SBRLayerCount", "_SBRInvLayerCount", "_SBRLayerEnable", "_SBRRadius", "_SBRTex2Grid",
            "_SBRProgress", "_SBRDetailThresholdHigh", "_SBRDetailThresholdLow", "_SBRStrokeWidth", "_SBRStrokeLen",
            "_SBRStrokeOpacity", "_SBRStrokeLenRand", "_SBRScratchSize", "_SBRScratchOpacity", "_SBRTolerance",
            "_SBRMaskType", "_SBRAdd", "_SBRMul", "_SBRInvGridX", "_SBRInvGridY",
            "_WCRBleeding", "_WCROpacity", "_WCRHandTremorLen", "_WCRHandTremorScale", 
            "_WCRHandTremorDrawCount", "_WCRHandTremorInvDrawCount", "_WCRHandTremorOverlapCount", 
            "_WCRPigmentDispersionScale", "_WCRTurbulenceFowScale1", "_WCRTurbulenceFowScale2", 
            "_WetInWetLenRatio", "_WetInWetInvLenRatio", 
            "_WetInWetLow", "_WetInWetHigh", "_WetInWetDarkToLight", "_WetInWetHueSimilarity", 
            "_EdgeDarkingLenRatio", "_EdgeDarkingInvLenRatio", "_EdgeDarkingSize", "_EdgeDarkingScale", 
            "_BFSampleLen", "_BFDomainVariance", "_BFRangeVariance", "_BFDomainBias", "_BFRangeBias", "_BFRangeThreshold", 
            "_BFOrthogonalize", "_BFStepDirScale", "_BFStepLenScale", "_BFRangeWeight",
            "_AKFRadius", "_AKFMaskRadius", "_AKFSharpness", "_AKFOverlapX", "_AKFOverlapY", "_AKFSampleStep",
            "_SNNRadius", "_SNNWeight", "_PosterizeBins", "_PosterizeInvBins", "_PosterizeReturnHSV",
            "_FXDoGGradientMaxLen", "_FXDoGTangentMaxLen", 
            "_FXDoGGradientVarianceL", "_FXDoGGradientVarianceS", "_FXDoGTangentVariance", 
            "_FXDoGSharpness", "_FXDoGSmoothRange", "_FXDoGThresholdSlope", "_FXDoGThreshold", 
            "_SobelCarryDigit", "_SobelInvCarryDigit",
            "_OutlineSize", "_OutlineInvSize", "_OutlineOpacity", "_OutlineDetail", "_OutlineDensity", "_OutlineReverse",
            "_LICScale", "_LICMaxLen", "_LICVariance",
            "_GBlurLOD", "_GBlurTileSize", "_GBlurSampleLen", "_GBlurSize", "_GBlurInvDomainSigma", "_GBlurDomainVariance", 
            "_GBlurDomainBias", "_GBlurMean", "_GBlurOffsetX", "_GBlurOffsetY", "_GBlurDomainWeight", 
            "_UnsharpMaskLOD", "_UnsharpMaskTileSize", "_UnsharpMaskSampleLen", "_UnsharpMaskSize", "_UnsharpMaskInvDomainSigma", 
            "_UnsharpMaskDomainVariance", "_UnsharpMaskDomainBias", "_UnsharpMaskMean", "_UnsharpMaskSharpness", 
            "_SNOIZE_SIZE", "_SNOIZE_SCALE", "_SNOIZE_SPEED",
            "_FNOIZE_SIZE", "_FNOIZE_SCALE", "_FNOIZE_SPEED",
            "_RuledLineDensity", "_RuledLineInvSize", "_RuledLineRotMat", 
            "_LerpBuf", "_LerpRate",
            "_Test0", "_Test1", "_Test2", "_Test3", "_Test4", "_Test5", "_Test6", "_Test7", "_Test8", "_Test9", 
            "_TestBF0", "_TestBF1", "_TestBF2", "_TestBF3", "_TestBF4", "_TestBF5", "_TestBF6", "_TestBF7", "_TestBF8", "_TestBF9", 
        };
        public Dictionary<string, int> prop = new Dictionary<string, int>();

        public ShaderHelper(Camera camera, Material mat)
        {
            foreach (var passName in passNames) { pass[passName] = mat.FindPass(passName.ToUpper()); }
            foreach (var propName in propNames) { prop[propName] = Shader.PropertyToID(propName); }
        }
    }
}


