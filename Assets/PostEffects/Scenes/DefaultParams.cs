namespace UnityPostEffecs
{
    public class DefaultParams
    {
        public static void SetSBR(ref SBRLayerAttribute[] layers, ref CommonOptions.InsCC cc, 
                                    ref DebugOptions.InsGBlur gb)
        {
            layers = new SBRLayerAttribute[8];

            layers[0] = new SBRLayerAttribute("Background", MaskType.None);
            layers[0].SetGrid(100, 0, 1);
            layers[0].SetStroke(3, 3, 1, 0);
            layers[0].SetScratch(0.1f, 0.1f, 0);
            layers[0].SetTolerance(0.3f, 0.3f, 1, 0.1f);
            layers[0].SetColorGrading(0, 0, 0, 0, 1, 1, 1, 1);

            layers[1] = new SBRLayerAttribute("BaseColor1", MaskType.None);
            layers[1].SetGrid(50, 0.001f, 0.05f);
            layers[1].SetStroke(0.8f, 2.5f, 1, 0.1f);
            layers[1].SetScratch(1, 30, 1);
            layers[1].SetTolerance(0.3f, 0.3f, 1, 0.2f);
            layers[1].SetColorGrading(0, 0, 0.05f, 0, 1, 1, 0.7f, 1);

            layers[2] = new SBRLayerAttribute("BaseColor2", MaskType.None);
            layers[2].SetGrid(51, 0.001f, 0.05f);
            layers[2].SetStroke(0.8f, 2.5f, 1, 0.1f);
            layers[2].SetScratch(1, 30, 1);
            layers[2].SetTolerance(0.3f, 0.3f, 1, 0.2f);
            layers[2].SetColorGrading(0, 0, 0.1f, 0, 1, 1, 0.7f, 1);

            layers[3] = new SBRLayerAttribute("BaseColor3", MaskType.None);
            layers[3].SetGrid(52, 0.001f, 0.05f);
            layers[3].SetStroke(0.8f, 2.5f, 1, 0.1f);
            layers[3].SetScratch(1, 30, 1);
            layers[3].SetTolerance(0.3f, 0.3f, 1, 0.2f);
            layers[3].SetColorGrading(0, 0, 0.15f, 0, 1, 1, 0.7f, 1);

            layers[4] = new SBRLayerAttribute("Outline1", MaskType.MaskReverse);
            layers[4].SetGrid(101, 0.005f, 1);
            layers[4].SetStroke(0.6f, 2.7f, 1, 0.1f);
            layers[4].SetScratch(1, 30, 1);
            layers[4].SetTolerance(1, 1, 1, 1);
            layers[4].SetColorGrading(0, 0, 0.15f, 0, 1, 1, 0.7f, 1);

            layers[5] = new SBRLayerAttribute("Outline2", MaskType.MaskReverse);
            layers[5].SetGrid(102, 0.005f, 1);
            layers[5].SetStroke(0.6f, 2.7f, 1, 0.1f);
            layers[5].SetScratch(1, 30, 1);
            layers[5].SetTolerance(1, 1, 1, 1);
            layers[5].SetColorGrading(0, 0, 0.15f, 0, 1, 1, 0.7f, 1);

            layers[6] = new SBRLayerAttribute("Outline3", MaskType.MaskReverse);
            layers[6].SetGrid(105, 0.005f, 1);
            layers[6].SetStroke(0.6f, 2.7f, 1, 0.1f);
            layers[6].SetScratch(1, 30, 1);
            layers[6].SetTolerance(1, 1, 1, 1);
            layers[6].SetColorGrading(0, 0, 0.15f, 0, 1, 1, 0.7f, 1);

            layers[7] = new SBRLayerAttribute("Skin", MaskType.Mask);
            layers[7].SetGrid(1000, 0.01f, 1);
            layers[7].SetStroke(2, 2.7f, 1, 0);
            layers[7].SetScratch(0.1f, 0.1f, 0);
            layers[7].SetTolerance(1, 1, 1, 1);
            layers[7].SetColorGrading(0, 0, 0.15f, 0, 1, 1, 0.7f, 1);

            cc.InputBlack = 0.0f;
            cc.InputGamma = 1.0f;
            cc.InputWhite = 1.0f;
            cc.OutputBlack = 0.0f;
            cc.OutputWhite = 1.0f;
            cc.MulLum = 1.2f;
            cc.AddLum = 0.0f;

            gb.SampleLen = 16;
            gb.LOD = 2;
        }
        public static void SetWCR(ref InsWCR wcr, ref CommonOptions.InsCC cc, 
                                    ref DebugOptions.InsGBlur gb, ref InsBF bf)
        {
            wcr.Bleeding = 40.0f;
            wcr.Opacity = 0.9f;
            wcr.HandTremorWaveLen1 = 5.0f;
            wcr.HandTremorAmplitude1 = 20.0f;
            wcr.HandTremorWaveLen2 = 0.0f;
            wcr.HandTremorAmplitude2 = 0.0f;
            wcr.HandTremorLen = 10.0f;
            wcr.HandTremorScale = 1.0f;
            wcr.HandTremorDrawCount = 16;
            wcr.HandTremorOverlapCount = 2;
            wcr.PigmentDispersionScale = 1.0f;
            wcr.TurbulenceFowWaveLen1 = 2.0f;
            wcr.TurbulenceFowAmplitude1 = 120.0f;
            wcr.TurbulenceFowScale1 = 1.5f;
            wcr.TurbulenceFowWaveLen2 = 0.0f;
            wcr.TurbulenceFowAmplitude2 = 0.0f;
            wcr.TurbulenceFowScale2 = 0.0f;
            wcr.EdgeDarkingLenRatio = 1.0f;
            wcr.EdgeDarkingSize = 0.1f;
            wcr.EdgeDarkingScale = 0.5f;
            wcr.WetInWetLenRatio = 0.5f;
            wcr.WetInWetDarkToLight = true;
            wcr.WetInWetHueSimilarity = 10.0f;
            wcr.WetInWetLow = 0.0f;
            wcr.WetInWetHigh = 0.65f;
            wcr.WetInWetWaveLen = 300.0f;
            wcr.WetInWetAmplitude = 20.0f;
            wcr.NoiseUpdateTime = 0.0333f;

            cc.InputBlack = 0.0f;
            cc.InputGamma = 1.0f;
            cc.InputWhite = 1.0f;
            cc.OutputBlack = 0.0f;
            cc.OutputWhite = 1.0f;
            cc.MulLum = 1.4f;
            cc.AddLum = 0.0f;

            gb.SampleLen = 16;
            gb.LOD = 2;

            bf.FlowBased = false;
            bf.BlurCount = 4;
            bf.SampleLen = 10.0f;
            bf.DistanceSigma = 10.0f;
            bf.DistanceBias = 1.0f;
            bf.ColorSigma = 2.0f;
            bf.ColorBias = 64.0f;
            bf.StepDirScale = 2;
            bf.StepLenScale = 1;
        }
        public static void SetAKF(ref InsAKF akf, ref DebugOptions.InsGBlur gb)
        {
            akf.Radius = 16;
            akf.MaskRadiusRatio = 0.5f;
            akf.Sharpness = 8.0f;
            akf.SideOverlap = 1.5f;
            akf.CenterOverlap = 0.3f;

            gb.SampleLen = 16;
            gb.LOD = 2;
        }
        public static void SetBF(ref InsBF bf, ref DebugOptions.InsGBlur gb)
        {
            bf.FlowBased = false;
            bf.BlurCount = 10;
            bf.SampleLen = 10.0f;
            bf.DistanceSigma = 10.0f;
            bf.DistanceBias = 1.0f;
            bf.ColorSigma = 1.5f;
            bf.ColorBias = 64.0f;

            gb.SampleLen = 16;
            gb.LOD = 2;
       }
    }
}