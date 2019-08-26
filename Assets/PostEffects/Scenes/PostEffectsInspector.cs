using System;
using UnityEngine;

namespace UnityPostEffecs
{
    // インスペクタ由来の情報をまとめる
    public class ButtonAttribute : PropertyAttribute
    {
        public string name;
        public string text;
        public object[] parameters;

        public ButtonAttribute(string name, string text, params object[] parameters)
        {
            this.name = name;
            this.text = text;
            this.parameters = parameters;
        }
    }

    public enum MaskType { None = 0, Mask = 1, MaskReverse = 2 }

    //////////////////////////////////////////////////////////////////////////////////////////////////
    // Common Options
    //////////////////////////////////////////////////////////////////////////////////////////////////
    [Serializable]
    public class CommonOptions
    {
        [SerializeField] internal string[] ModelNames = { "SendagayaShibuNormal", "SendagayaShibuSpring" };

        //////////////////////////////////////////////////////////////////////////////////////////////////
        // Mask
        //////////////////////////////////////////////////////////////////////////////////////////////////
        [Serializable]
        public class InsMask
        {
            [SerializeField] internal string[] FaceMeshNames = 
            { 
                "Face", 
                "Hairs/Hair001" 
            };
            [SerializeField] internal string[] FaceMaterialNames = 
            { 
                "F00_000_Face_00_SKIN", "F00_000_00_Face_00_SKIN", 
                "F00_000_EyeWhite_00_EYE", "F00_000_00_EyeWhite_00_EYE", 
                "F00_000_EyeIris_00_EYE", "F00_000_00_EyeIris_00_EYE", 
                "F00_000_Hair_00_HAIR_01", "F00_000_Hair_00_HAIR_02"
            };
            [SerializeField] internal string[] BodyMeshNames = 
            { 
                "Body" 
            };
            [SerializeField] internal string[] BodyMaterialNames = 
            { 
                "F00_001_Body_00_SKIN", "F00_001_01_Body_00_SKIN", 
                "F00_001_Tops_01_CLOTH", "F00_001_01_Tops_01_CLOTH", 
                "F00_001_Bottoms_01_CLOTH", "F00_001_01_Bottoms_01_CLOTH" 
            };
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////
        // Color Correction
        //////////////////////////////////////////////////////////////////////////////////////////////////
        [Serializable]
        public class InsCC
        {
            [SerializeField, Range(0.0f, 255.0f)] internal float InputBlack = 0.0f;
            [SerializeField, Range(0.0f, 2.0f)] internal float InputGamma = 1.0f;
            [SerializeField, Range(0.0f, 255.0f)] internal float InputWhite = 255.0f;
            [SerializeField, Range(0.0f, 255.0f)] internal float OutputBlack = 0.0f;
            [SerializeField, Range(0.0f, 255.0f)] internal float OutputWhite = 255.0f;
            internal const float MulLumMin = 0.0f, MulLumMax = 2.0f;
            [SerializeField, Range(MulLumMin, MulLumMax)] internal float MulLum = 1.0f;
            [SerializeField, Range(-1.0f, 1.0f)] internal float AddLum = 0.0f;
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////
        // Canvas
        //////////////////////////////////////////////////////////////////////////////////////////////////
        [Serializable]
        public class InsCanvas
        {
            [SerializeField, Range(0.0f, 40.0f)] internal float WrinkleWaveLen = 20.0f;
            [SerializeField, Range(0.0f, 10.0f)] internal float WrinkleAmplitude = 5.0f;
            [SerializeField, Range(0.0f, 1.0f)] internal float RuledLineDensity = 0.0f;
            [SerializeField, Range(1.0f, 3.0f)] internal float RuledLineSize = 2.0f;
            [SerializeField, Range(0.0f, 90.0f)] internal float RuledLineAngle = 45.0f;
        }

        [SerializeField] internal InsMask MaskParameters = new InsMask();
        [SerializeField] internal InsCC CCParameters = new InsCC();
        [SerializeField] internal InsCanvas CanvasParameters = new InsCanvas();
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////
    // Stroke Based Rendering
    //////////////////////////////////////////////////////////////////////////////////////////////////
    [Serializable]
    public class SBRLayerAttribute
    {
        public bool enable = true;
        public string memo = "";
        public MaskType maskType = MaskType.None;

        [Range(1, 1000)] public int gridCount = 100;
        [Range(0.0f, 1.0f)] public float detailThresholdHigh = 1.0f;
        [Range(0.0f, 1.0f)] public float detailThresholdLow = 0.0f;

        [Range(0.1f, 7.0f)] public float strokeWidth = 1.0f;
        [Range(0.1f, 7.0f)] public float strokeLen = 3.0f;
        [Range(0.0f, 1.0f)] public float strokeOpacity = 1.0f;
        [Range(0.0f, 10.0f)] public float strokeLenRand = 0.0f;

        [Range(0.1f, 10.0f)] public float scratchWidth = 15.0f;
        [Range(0.1f, 30.0f)] public float scratchHeight = 2.0f;
        [Range(0.0f, 1.0f)] public float scratchOpacity = 1.0f;

        [Range(0.0f, 1.0f)] public float toleranceH1 = 1.0f;
        [Range(0.0f, 1.0f)] public float toleranceH2 = 1.0f;
        [Range(0.0f, 1.0f)] public float toleranceS = 1.0f;
        [Range(0.0f, 1.0f)] public float toleranceV = 1.0f;

        [Range(-1.0f, 1.0f)] public float addH1 = 0.0f;
        [Range(-1.0f, 1.0f)] public float addH2 = 0.0f;
        [Range(-1.0f, 1.0f)] public float addS = 0.0f;
        [Range(-1.0f, 1.0f)] public float addV = 0.0f;
        [Range(0.0f, 2.0f)] public float mulH1 = 1.0f;
        [Range(0.0f, 2.0f)] public float mulH2 = 1.0f;
        [Range(0.0f, 2.0f)] public float mulS = 1.0f;
        [Range(0.0f, 2.0f)] public float mulV = 1.0f;

        public SBRLayerAttribute(string memo, MaskType maskType)
        {
            this.memo = memo; this.maskType = maskType; 
        }
        public void SetGrid(int gridCount, float low, float high)
        {
            this.gridCount = gridCount;
            detailThresholdLow = low; detailThresholdHigh = high;
        }
        public void SetStroke(float width, float len, float opacity, float lenRand)
        {
            strokeWidth = width; strokeLen = len; strokeOpacity = opacity; strokeLenRand = lenRand;
        }
        public void SetScratch(float width, float height, float opacity)
        {
            scratchWidth = width; scratchHeight = height; scratchOpacity = opacity;
        }
        public void SetTolerance(float H1, float H2, float S, float V)
        {
            toleranceH1 = H1; toleranceH2 = H2; toleranceS = S; toleranceV = V;
        }
        public void SetColorGrading(float addH1, float addH2, float addS, float addV, 
                                    float mulH1, float mulH2, float mulS, float mulV)
        {
            this.addH1 = addH1; this.addH2 = addH2; this.addS = addS; this.addV = addV;
            this.mulH1 = mulH1; this.mulH2 = mulH2; this.mulS = mulS; this.mulV = mulV;
        }
    }

    [Serializable]
    public class InsSBR
    {
        internal const float GridScaleMin = 0.1f, GridScaleMax = 2.0f;
        [SerializeField, Range(GridScaleMin, GridScaleMax)] internal float GridScale = 1.0f;
        [Button("SetDefaultParamsSBR", "Set Default Parameters")]
        [SerializeField] internal bool DefaultParameters;
        [SerializeField] internal SBRLayerAttribute[] Layers;
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////
    // Watercolor Rendering
    //////////////////////////////////////////////////////////////////////////////////////////////////
    [Serializable]
    public class InsWCR
    {
        [Button("SetDefaultParamsWCR", "Set Default Parameters")]
        [SerializeField] internal bool DefaultParameters;
        [SerializeField, Range(0.1f, 40.0f)] internal float Bleeding = 40.0f;
        [SerializeField, Range(0.0f, 1.0f)] internal float Opacity = 0.9f;
        [SerializeField, Range(0.0f, 100.0f)] internal float HandTremorWaveLen1 = 5.0f;
        [SerializeField, Range(0.0f, 100.0f)] internal float HandTremorAmplitude1 = 20.0f;
        [SerializeField, Range(0.0f, 100.0f)] internal float HandTremorWaveLen2 = 0.0f;
        [SerializeField, Range(0.0f, 100.0f)] internal float HandTremorAmplitude2 = 0.0f;
        [SerializeField, Range(0.0f, 30.0f)] internal float HandTremorLen = 10.0f;
        [SerializeField, Range(0.0f, 3.0f)] internal float HandTremorScale = 1.0f;
        [SerializeField, Range(0.0f, 4.0f)] internal float PigmentDispersionScale = 1.0f;
        [SerializeField, Range(0.0f, 4.0f)] internal float TurbulenceFowWaveLen1 = 2.0f;
        [SerializeField, Range(0.0f, 300.0f)] internal float TurbulenceFowAmplitude1 = 120.0f;
        [SerializeField, Range(0.0f, 4.0f)] internal float TurbulenceFowScale1 = 1.5f;
        [HideInInspector][SerializeField, Range(0.0f, 50.0f)] internal float TurbulenceFowWaveLen2 = 0.0f;
        [HideInInspector][SerializeField, Range(0.0f, 300.0f)] internal float TurbulenceFowAmplitude2 = 0.0f;
        [HideInInspector][SerializeField, Range(0.0f, 40.0f)] internal float TurbulenceFowScale2 = 0.0f;
        [SerializeField, Range(0.0f, 1.0f)] internal float EdgeDarkingSize = 0.1f;
        [HideInInspector][SerializeField, Range(0.1f, 1.0f)] internal float EdgeDarkingScale = 0.5f;
        [HideInInspector][SerializeField, Range(0.001f, 1.0f)] internal float EdgeDarkingLenRatio = 1.0f;
        [HideInInspector][SerializeField] internal bool WetInWetDarkToLight = true;
        internal const float WetInWetHueSimilarityMin = 0.0f, WetInWetHueSimilarityMax = 180.0f;
        [SerializeField, Range(WetInWetHueSimilarityMin, WetInWetHueSimilarityMax)] internal float WetInWetHueSimilarity = 10.0f;
        [SerializeField, Range(0.0f, 1.0f)] internal float WetInWetLow = 0.0f;
        [SerializeField, Range(0.0f, 1.0f)] internal float WetInWetHigh = 0.65f;
        [SerializeField, Range(0.0f, 300.0f)] internal float WetInWetWaveLen = 300.0f;
        [SerializeField, Range(0.0f, 40.0f)] internal float WetInWetAmplitude = 20.0f;
        [SerializeField, Range(0.001f, 1.0f)] internal float WetInWetLenRatio = 0.5f;
        [SerializeField, Range(0.0f, 10.0f)] internal float NoiseUpdateTime = 0.0333f;
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////
    // Bilateral Filter
    //////////////////////////////////////////////////////////////////////////////////////////////////
    [Serializable]
    public class InsBF
    {
        [Button("SetDefaultParamsBF", "Set Default Parameters")]
        [SerializeField] internal bool DefaultParameters;
        [HideInInspector][SerializeField] internal bool FlowBased = false;
        internal const int BlurCountMin = 1, BlurCountMax = 20;
        [SerializeField, Range(BlurCountMin, BlurCountMax)] internal int BlurCount = 4;
        [SerializeField, Range(0.1f, 20.0f)] internal float SampleLen = 10.0f;
        [SerializeField, Range(0.1f, 20.0f)] internal float DistanceSigma = 10.0f;
        [SerializeField, Range(0.1f, 2.0f)] internal float DistanceBias = 1.0f;
        [SerializeField, Range(0.1f, 4.0f)] internal float ColorSigma = 2.0f;
        [SerializeField, Range(0.1f, 128.0f)] internal float ColorBias = 64.0f;
        [SerializeField, Range(1.0f, 10.0f)] internal float StepDirScale = 2.0f;
        [SerializeField, Range(1.0f, 4.0f)] internal float StepLenScale = 1.0f;
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////
    // Anisotropic Kuwahara Filter
    //////////////////////////////////////////////////////////////////////////////////////////////////
    [Serializable]
    public class InsAKF
    {
        [Button("SetDefaultParamsAKF", "Set Default Parameters")]
        [SerializeField] internal bool DefaultParameters;
        internal const float RadiusMin = 4.0f, RadiusMax = 32.0f;
        [SerializeField, Range(RadiusMin, RadiusMax)] internal float Radius = 16.0f;
        [SerializeField, Range(0.2f, 1.0f)] internal float MaskRadiusRatio = 0.5f;
        [SerializeField, Range(0.1f, 8.0f)] internal float Sharpness = 8.0f;
        // 分割領域の両脇での他領域との重複量
        [SerializeField, Range(0.1f, 3.0f)] internal float SideOverlap = 1.5f;
        // 分割領域の中心での他領域との重複量
        [SerializeField, Range(0.1f, 1.0f)] internal float CenterOverlap = 0.3f;
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////
    // Symmetric Nearest Neighbor
    //////////////////////////////////////////////////////////////////////////////////////////////////
    [Serializable]
    public class InsSNN
    {
        internal const int RadiusMin = 1, RadiusMax = 20;
        [SerializeField, Range(RadiusMin, RadiusMax)] internal int Radius = 8;
    }

    //////////////////////////////////////////////////////////////////////////////////////////////
    // Outline
    //////////////////////////////////////////////////////////////////////////////////////////////
    [Serializable]
    public class InsOutline
    {
        internal const float SizeMin = 1.0f, SizeMax = 5.0f;
        [SerializeField, Range(SizeMin, SizeMax)] internal float Size = 3.0f;
        [SerializeField, Range(0.0f, 0.2f)] internal float Opacity = 0.1f;
        [SerializeField, Range(0.0f, 2.0f)] internal float Detail = 1.0f;
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////
    // Flow based eXtended Difference of Gaussians 
    //////////////////////////////////////////////////////////////////////////////////////////////////
    [Serializable]
    public class InsFXDoG
    {
        internal const float AbstractnessMin = 0.1f, AbstractnessMax = 3.0f;
        [SerializeField, Range(AbstractnessMin, AbstractnessMax)] internal float Abstractness = 2.0f;
        [SerializeField, Range(1.0f, 3.0f)] internal float Contrast = 2.0f;
        [SerializeField, Range(1.0f, 40.0f)] internal float Sharpness = 20.0f;
        [SerializeField, Range(1.0f, 10.0f)] internal float Coherence = 5.0f;
        [SerializeField, Range(1.0f, 3.0f)] internal float Smoothness = 2.0f;
        [SerializeField, Range(1.0f, 3.0f)] internal float SmoothRange = 2.0f;
        [SerializeField, Range(0.1f, 3.0f)] internal float ThresholdSlope = 2.0f;
        [SerializeField, Range(1.0f, 100.0f)] internal float Threshold = 80.0f;
    }

    //////////////////////////////////////////////////////////////////////////////////////////////
    // Mobile Options
    //////////////////////////////////////////////////////////////////////////////////////////////
    [Serializable]
    public class MobileOptions
    {
        // 0ならFPSは制限しない
        // SBRはフレームレートが高いと筆致がチラつく。15程度で十分
        [SerializeField, Range(0, 30)] internal int FrameRate = 0;
        [SerializeField, Range(0.1f, 1.0f)] internal float ResolutionScale = 1.0f;
    }

    //////////////////////////////////////////////////////////////////////////////////////////////
    // Debug Options
    //////////////////////////////////////////////////////////////////////////////////////////////
    [Serializable]
    public class DebugOptions
    {
        [SerializeField] internal bool MaskEnable = false;
        [SerializeField] internal bool SobelEnable = false;
        [SerializeField] internal bool SSTEnable = false;
        [SerializeField] internal bool TFMEnable = false;
        [SerializeField] internal bool LICEnable = false;
        [Range(0.0f, 10.0f)] internal float LICScale = 2.0f;
        [Range(0.0f, 20.0f)] internal float LICSigma = 9.0f;
        [SerializeField] internal bool GBlurEnable = false;
        [Serializable]
        public class InsGBlur
        {
            [SerializeField, Range(1, 64)] internal int SampleLen = 16;
            [SerializeField, Range(0, 3)] internal int LOD = 2;
            [HideInInspector][SerializeField, Range(0.1f, 10.0f)] internal float DomainSigma = 1.0f;
            [HideInInspector][SerializeField, Range(0.1f, 10.0f)] internal float DomainBias = 1.0f;
        }
        [SerializeField] internal InsGBlur GBlurParameters = new InsGBlur();

        [SerializeField] internal bool PosterizeEnable = false;
        [Serializable]
        public class InsPosterize
        {
            [SerializeField, Range(1, 16)] internal int Bins = 8;
        }
        [SerializeField] internal InsPosterize PosterizeParameters = new InsPosterize();

        [HideInInspector][SerializeField] internal bool HSVEnable = false;
        [HideInInspector][SerializeField] internal bool HSLEnable = false;
        [HideInInspector][SerializeField] internal bool YUVEnable = false;
        [HideInInspector][SerializeField] internal bool LABEnable = false;
        [SerializeField] internal bool SimplexNoiseEnable = false;
        [Serializable]
        public class InsSNoise
        {
            [SerializeField, Range(1.0f, 256.0f)] internal float Size1 = 3.0f;
            [SerializeField, Range(1.0f, 256.0f)] internal float Scale1 = 64.0f;
            [SerializeField, Range(0.0f, 2.0f)] internal float Speed1 = 1.0f;
            [SerializeField, Range(1.0f, 256.0f)] internal float Size2 = 3.0f;
            [SerializeField, Range(1.0f, 256.0f)] internal float Scale2 = 64.0f;
            [SerializeField, Range(0.0f, 2.0f)] internal float Speed2 = 1.0f;
            [SerializeField, Range(1.0f, 256.0f)] internal float Size3 = 3.0f;
            [SerializeField, Range(1.0f, 256.0f)] internal float Scale3 = 64.0f;
            [SerializeField, Range(0.0f, 2.0f)] internal float Speed3 = 1.0f;
            [SerializeField, Range(1.0f, 256.0f)] internal float Size4 = 3.0f;
            [SerializeField, Range(1.0f, 256.0f)] internal float Scale4 = 64.0f;
            [SerializeField, Range(0.0f, 2.0f)] internal float Speed4 = 1.0f;
        }
        [SerializeField] internal InsSNoise SimplexNoiseParameters = new InsSNoise();
        [SerializeField] internal bool FlowNoiseEnable = false;
        [Serializable]
        public class InsFNoise
        {
            [SerializeField, Range(1.0f, 256.0f)] internal float Size1 = 3.0f;
            [SerializeField, Range(1.0f, 64.0f)] internal float Scale1 = 64.0f;
            [SerializeField, Range(0.0f, 2.0f)] internal float Speed1 = 1.0f;
            [SerializeField, Range(1.0f, 256.0f)] internal float Size2 = 3.0f;
            [SerializeField, Range(1.0f, 64.0f)] internal float Scale2 = 46.0f;
            [SerializeField, Range(0.0f, 2.0f)] internal float Speed2 = 1.0f;
            [SerializeField, Range(1.0f, 256.0f)] internal float Size3 = 3.0f;
            [SerializeField, Range(1.0f, 64.0f)] internal float Scale3 = 64.0f;
            [SerializeField, Range(0.0f, 2.0f)] internal float Speed3 = 1.0f;
        }
        [SerializeField] internal InsFNoise FlowNoiseParameters = new InsFNoise();
        [SerializeField] internal bool VoronoiNoiseEnable = false;
        [SerializeField] internal bool TestEnable = false;
        [Serializable]
        public class InsTest
        {
            [SerializeField, Range(0.0f, 100.0f)] internal float Test0 = 0.0f;
            [SerializeField, Range(0.0f, 100.0f)] internal float Test1 = 0.0f;
            [SerializeField, Range(0.0f, 100.0f)] internal float Test2 = 0.0f;
            [SerializeField, Range(0.0f, 100.0f)] internal float Test3 = 0.0f;
            [SerializeField, Range(0.0f, 100.0f)] internal float Test4 = 0.0f;
            [SerializeField, Range(0.0f, 100.0f)] internal float Test5 = 0.0f;
            [SerializeField, Range(0.0f, 100.0f)] internal float Test6 = 0.0f;
            [SerializeField, Range(0.0f, 100.0f)] internal float Test7 = 0.0f;
            [SerializeField, Range(0.0f, 100.0f)] internal float Test8 = 0.0f;
            [SerializeField, Range(0.0f, 100.0f)] internal float Test9 = 0.0f;
        }
        [SerializeField] internal InsTest TestParameters = new InsTest();
        [SerializeField] internal bool TestBFEnable = false;
        [Serializable]
        public class InsTestBF
        {
            [SerializeField, Range(0.0f, 100.0f)] internal float TestBF0 = 1.0f;
            [SerializeField, Range(0.0f, 100.0f)] internal float TestBF1 = 32.0f;
            [SerializeField, Range(0.0f, 100.0f)] internal float TestBF2 = 1.0f;
            [SerializeField, Range(0.0f, 100.0f)] internal float TestBF3 = 1.0f;
            [SerializeField, Range(0.0f, 100.0f)] internal float TestBF4 = 1.0f;
            [SerializeField, Range(0.0f, 100.0f)] internal float TestBF5 = 30.0f;
            [SerializeField, Range(0.0f, 100.0f)] internal float TestBF6 = 0.0f;
            [SerializeField, Range(0.0f, 100.0f)] internal float TestBF7 = 0.0f;
            [SerializeField, Range(0.0f, 100.0f)] internal float TestBF8 = 0.0f;
            [SerializeField, Range(0.0f, 100.0f)] internal float TestBF9 = 0.0f;
        }
        [SerializeField] internal InsTestBF TestBFParameters = new InsTestBF();

        [Space(10)]
        [SerializeField] internal bool FacialExpression = false;
        [SerializeField] internal string FaceMeshName = "Face";

        // 検証に使いたいテクスチャを指定して、シェーダ側の_DebugTexで受ける
        [Space(10)]
        [SerializeField] internal Texture DebugTexture;
    }
}


