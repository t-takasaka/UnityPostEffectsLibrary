using System;
using UnityEngine;

namespace UnityPostEffecs
{
    using static Mathf;

    // インスペクタ由来の情報を加工して下位レベルの処理に渡す

    //////////////////////////////////////////////////////////////////////////////////////////////////
    // Color Correction
    //////////////////////////////////////////////////////////////////////////////////////////////////
    public class CC
    {
        public float InBlack, InGamma, InWhite, OutBlack, OutWhite;
        public float MulLum, AddLum;
        public void Set(CommonOptions.InsCC cc)
        {
            InBlack = cc.InputBlack;
            InGamma = cc.InputGamma;
            InWhite = cc.InputWhite;
            OutBlack = cc.OutputBlack;
            OutWhite = cc.OutputWhite;
            MulLum = cc.MulLum;
            AddLum = cc.AddLum;
        }
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////
    // Posterize
    //////////////////////////////////////////////////////////////////////////////////////////////////
    public class Posterize
    {
        public float Bins, InvBins;
        public void Set(DebugOptions.InsPosterize posterize) 
        { 
            Bins = posterize.Bins; 
            InvBins = 1.0f / posterize.Bins; 
        }
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////
    // Canvas
    //////////////////////////////////////////////////////////////////////////////////////////////////
    public class Canvas
    {
        public float RuledLineDensity;
        public float RuledLineInvSize;
        public float RuledLineAngle;
        public Vector4 RuledLineRotMat;
        public void Set(CommonOptions.InsCanvas can)
        { 
            RuledLineDensity = can.RuledLineDensity;
            RuledLineInvSize = (1.0f / (can.RuledLineSize * 0.001f)) * PI;
            RuledLineAngle = can.RuledLineAngle;

            float ruledLineAngle = Deg2Rad * RuledLineAngle;
            float c = Cos(ruledLineAngle);
            float s = Sin(ruledLineAngle);
            RuledLineRotMat = new Vector4(c, -s, s, c);
        }
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////
    // Stroke Based Rendering
    //////////////////////////////////////////////////////////////////////////////////////////////////
    public class SBR
    {
        public float GridScale;
        public int Count = 0;
        public float[] Enable, MaskType, Radius, DetailThresholdHigh, DetailThresholdLow;
        public float[] StrokeWidth, StrokeLen, StrokeOpacity;
        public float[] ScratchOpacity, StrokeLenRand, InvGridX, InvGridY;
        public Vector4[] Tex2grid, Progress, ScratchSize, Tolerance, Add, Mul;

        public void Set(InsSBR sbr, int layerMax, int width, int height)
        { 
            GridScale = sbr.GridScale;

            int count = Min(sbr.Layers.Length, layerMax);
            if(count == 0){ return; }

            if(Count != count)
            {
                Count = count;
                Enable = new float[Count];
                MaskType = new float[Count];
                Radius = new float[Count];
                DetailThresholdHigh = new float[Count];
                DetailThresholdLow = new float[Count];
                StrokeWidth = new float[Count];
                StrokeLen = new float[Count];
                StrokeOpacity = new float[Count];
                ScratchOpacity = new float[Count];
                StrokeLenRand = new float[Count];
                InvGridX = new float[Count];
                InvGridY = new float[Count];
                Tex2grid = new Vector4[Count];
                Progress = new Vector4[Count];
                ScratchSize = new Vector4[Count];
                Tolerance = new Vector4[Count];
                Add = new Vector4[Count];
                Mul = new Vector4[Count];
            }

            float invLayerCount = 1.0f / (Count - 1);

            float aspectX = 1.0f, aspectY = 1.0f;
            float invWidth = 1.0f/ width, invHeight = 1.0f / height;
            if (width < height) { aspectX *= (float)width / (float)height; }
            if (width > height) { aspectY *= (float)height / (float)width; }

            for (int i = 0; i < Count; ++i)
            {
                SBRLayerAttribute layer = sbr.Layers[i];
                Enable[i] = layer.enable ? 1.0f : 0.0f;
                MaskType[i] = (float)layer.maskType;

                // 昇順のレイヤ進捗率、降順のレイヤ番号、降順のレイヤ進捗率
                float revLayer = (Count - 1) - i;
                Progress[i].Set(i * invLayerCount, revLayer, revLayer * invLayerCount, 0.0f);

                // レイヤ上の格子数。画面を格子状に分割して、格子の升目1つにつき1つの筆致を描く
                // 上層レイヤほど格子を多くする＝筆致を小さくする＝細かく描き込む
                //float gridCountX = layer.gridCount, gridCountY = gridCountX * this.aspect;
                float gridCount = layer.gridCount * GridScale;
                float gridCountX = gridCount * aspectX, gridCountY = gridCount * aspectY;
                float invGridCountX = 1.0f / gridCountX, invGridCountY = 1.0f / gridCountY;

                // 筆致の幅と高さの比率で最も大きい値からサンプリングの半径を出す
                // ※半径1画素（八近傍）の場合は3画素を超えると筆致が途切れる
                // ※手振れ分が足されて半径を超える（途切れる）ケースがあり得る
                float radMax = Max(layer.strokeWidth, layer.strokeLen);
                // サンプリングする半径。中央1画素分を引いて両脇を2で割る
                Radius[i] = CeilToInt((radMax - 1.0f) / 2.0f);

                // 画素数と格子数を相互に変換する変数
                float tex2gridX = gridCountX * invWidth, tex2gridY = gridCountY * invHeight;
                float grid2texX = width * invGridCountX, grid2texY = height * invGridCountY;
                Tex2grid[i].Set(tex2gridX, tex2gridY, grid2texX, grid2texY);

                // 筆致の幅と高さの比率を格子のサイズに適用
                DetailThresholdHigh[i] = layer.detailThresholdHigh;
                DetailThresholdLow[i] = layer.detailThresholdLow;
                StrokeWidth[i] = layer.strokeWidth * grid2texX;
                StrokeLen[i] = layer.strokeLen * grid2texY;
                StrokeOpacity[i] = layer.strokeOpacity;
                StrokeLenRand[i] = layer.strokeLenRand;
                float invScratchHeight = 1.0f / layer.scratchHeight;
                float invScratchWidth = 1.0f / layer.scratchWidth;
                ScratchSize[i].Set(layer.scratchHeight, layer.scratchWidth, invScratchHeight, invScratchWidth);
                ScratchOpacity[i] = layer.scratchOpacity;
                Tolerance[i].Set(layer.toleranceH1, layer.toleranceH2, layer.toleranceS, layer.toleranceV);

                Add[i].Set(layer.addH1, layer.addH2, layer.addS, layer.addV);
                Mul[i].Set(layer.mulH1, layer.mulH2, layer.mulS, layer.mulV);

                InvGridX[i] = invGridCountX;
                InvGridY[i] = invGridCountY;
            }            
        }
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////
    // Watercolor Rendering
    //////////////////////////////////////////////////////////////////////////////////////////////////
    public class WCR
    {
        public float Bleeding, Opacity, HandTremorLen, HandTremorScale;
        public float HandTremorDrawCount, HandTremorInvDrawCount, HandTremorOverlapCount;
        public float PigmentDispersionScale, TurbulenceFowScale1, TurbulenceFowScale2;
        public float WetInWetLenRatio, WetInWetInvLenRatio;
        public float WetInWetLow, WetInWetHigh;
        public float WetInWetDarkToLight, WetInWetHueSimilarity;
        public float EdgeDarkingLenRatio, EdgeDarkingInvLenRatio;
        public float EdgeDarkingEdgeThreshold;
        public float EdgeDarkingSize, EdgeDarkingScale;

        public float SNoiseUpdateTime;
        public SNoise SNoise1 = new SNoise(), SNoise2 = new SNoise();

        public void Set(InsWCR wcr, CommonOptions.InsCanvas can) 
        {
            Bleeding = wcr.Bleeding;
            Opacity = wcr.Opacity;
            HandTremorLen = wcr.HandTremorLen;
            HandTremorScale = wcr.HandTremorScale;
            HandTremorDrawCount = wcr.HandTremorDrawCount;
            HandTremorInvDrawCount = 1.0f / wcr.HandTremorDrawCount;
            HandTremorOverlapCount = wcr.HandTremorOverlapCount;
            PigmentDispersionScale = wcr.PigmentDispersionScale; 
            TurbulenceFowScale1 = wcr.TurbulenceFowScale1;
            TurbulenceFowScale2 = wcr.TurbulenceFowScale2;
            WetInWetLenRatio = 1.0f - wcr.WetInWetLenRatio;
            WetInWetInvLenRatio = 1.0f / wcr.WetInWetLenRatio;
            WetInWetLow = wcr.WetInWetLow;
            WetInWetHigh = wcr.WetInWetHigh;
            WetInWetDarkToLight = wcr.WetInWetDarkToLight ? 1.0f : 0.0f;
            WetInWetHueSimilarity = wcr.WetInWetHueSimilarity;
            EdgeDarkingLenRatio = 1.0f - wcr.EdgeDarkingLenRatio;
            EdgeDarkingInvLenRatio = 1.0f / wcr.EdgeDarkingLenRatio;
            EdgeDarkingSize = wcr.EdgeDarkingSize;
            EdgeDarkingScale = wcr.EdgeDarkingScale;

            SNoiseUpdateTime = wcr.NoiseUpdateTime;
            SNoise1.Size.Set(wcr.HandTremorWaveLen1, wcr.HandTremorWaveLen2, 
                                wcr.TurbulenceFowWaveLen1, wcr.TurbulenceFowWaveLen2);
            SNoise1.Scale.Set(wcr.HandTremorAmplitude1, wcr.HandTremorAmplitude2, 
                                wcr.TurbulenceFowAmplitude1, wcr.TurbulenceFowAmplitude2);
            //SNoise1.Speed.Set(0.1f, 0.1f, 0.1f, 0.1f);
            SNoise1.Speed.Set(0.0f, 0.0f, 0.0f, 0.0f);
            SNoise1.RT = 6;

            SNoise2.Size.Set(wcr.WetInWetWaveLen, 1.0f, 1.0f, can.WrinkleWaveLen);
            SNoise2.Scale.Set(wcr.WetInWetAmplitude, 1.0f, 1.0f, can.WrinkleAmplitude);
            //SNoise2.Speed.Set(0.1f, 0.1f, 0.1f, 0.1f);
            SNoise2.Speed.Set(0.0f, 0.0f, 0.0f, 0.0f);
            SNoise2.RT = 7;
        }
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////
    // Anisotropic Kuwahara Filter
    //////////////////////////////////////////////////////////////////////////////////////////////////
    public class AKF
    {
        public float Radius, MaskRadius, Sharpness, OverlapX, OverlapY;
        public readonly int SampleStep = 2; // 固定

        public void Set(InsAKF akf)
        {
            Radius = akf.Radius;
            // RadiusMinを下回ると塗り漏れが発生する
            MaskRadius = Max(InsAKF.RadiusMin, akf.Radius * akf.MaskRadiusRatio);
            Sharpness = akf.Sharpness;

            // 楕円の分割数。固定
            float DIV_NUM  = 8.0f;
	        // 円を分割した扇形の切片を二次関数で近似して、画素が切片内にあるなら重みを付ける
	        // 中心から見て右方向の切片で考えたとき、単純な二次関数だと x + y^2 = 0
	        // 桑原フィルタは他の分割領域との分散を比較するので、切片同士を少しずつ重複させる必要がある
	        // （重複してサンプルした上で、分散が少ない切片の色平均を注目画素の色に選ぶことで平滑化する）
	        // 中心を反対方向へずらす係数をzeta、二次関数の放物線を両脇に広げる係数をetaとして 
	        // (x + zeta) - eta * y^2 = 0。 zeta はそのまま引数 CenterOverlap で指定
	        // eta は楕円の分割数により適切な値が変わるため、下記で計算する
	        // eta = (cos(PI/(SideOverlap*DIV_NUM)) + CenterOverlap) - sin(PI/(sideOverlap*DIV_NUM))^2
	        // 正規分布らしい重み付けにするには CenterOverlap≒1/3、SideOverlap≒3/2くらいが目安
	        float theta = akf.SideOverlap * (PI * (1.0f / DIV_NUM));
	        float cosTheta = Cos(theta), sinTheta = Sin(theta);
	        float invSinThetaSq = 1.0f / sinTheta * sinTheta;
	        OverlapY = (akf.CenterOverlap + cosTheta) * invSinThetaSq;
            OverlapX = akf.CenterOverlap;
        }
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////
    // Bilateral Filter
    //////////////////////////////////////////////////////////////////////////////////////////////////
    public class BF
    {
        public bool FlowBased;
        public int BlurCount;
        public float SampleLen;
        public float DomainVariance, RangeVariance;
        public float DomainBias, RangeBias;
        public float RangeThreshold;
        public float StepDirScale, StepLenScale;
        public bool UsePreCalc = false;
        public float[] RangeWeight = new float[256];

        public void Set(InsBF bf) 
        { 
            FlowBased = bf.FlowBased;
            BlurCount = bf.BlurCount;
            SampleLen = bf.SampleLen;
            RangeBias = bf.ColorBias;
            RangeThreshold = 1.0f / bf.ColorThreshold;
            DomainBias = bf.DistanceBias;
            // Gσ(x) = exp(−(x^2) / (2 * σ^2)) の (2 * σ^2)
            // 分母として使うので逆数にしておく
            DomainVariance = 1.0f / (bf.DistanceSigma * bf.DistanceSigma * 2.0f);
            RangeVariance = 1.0f / (bf.ColorSigma * bf.ColorSigma * 2.0f);
            StepDirScale = bf.StepDirScale;
            StepLenScale = bf.StepLenScale;

            if(!UsePreCalc){ return; }

            for(int i = 0; i < 256; i++)
            {
                float x = i * RangeBias;
                RangeWeight[i] = Exp(-(x * x) * RangeVariance);
            }
        }
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////
    // Symmetric Nearest Neighbor
    //////////////////////////////////////////////////////////////////////////////////////////////////
    public class SNN
    {
        public int Radius;
        public float Weight;

        public void Set(InsSNN snn)
        {
            Radius = snn.Radius;
            Weight = (Radius * (Radius * 2 + 1) + Radius) * 2.0f;
        }
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////
    // Flow based eXtended Difference of Gaussians 
    //////////////////////////////////////////////////////////////////////////////////////////////////
    public class FXDoG
    {
        public float GradientMaxLen, TangentMaxLen, Sharpness, SmoothRange, ThresholdSlope, Threshold;
        public float GradientVarianceL, GradientVarianceS, TangentVariance;
        //差分カーネルの拡大率。1.6でLoGと近似
    	private const float diffKernelScale = 1.6f;

        public void Set(InsFXDoG fxdog) 
        {
            GradientMaxLen = fxdog.Contrast * fxdog.Abstractness;
            TangentMaxLen = fxdog.Smoothness * fxdog.Coherence;
            Sharpness = fxdog.Sharpness;
            SmoothRange = fxdog.SmoothRange;
            ThresholdSlope = fxdog.ThresholdSlope * 0.01f;
            Threshold = fxdog.Threshold;

            // Gσ(x) = exp(−(x^2) / (2 * σ^2)) の (2 * σ^2)
            // 分母として使うので逆数にしておく
            GradientVarianceL = 1.0f / (fxdog.Abstractness * fxdog.Abstractness * 2.0f);
            float gradientSigmaS = fxdog.Abstractness * diffKernelScale;
            GradientVarianceS = 1.0f / (gradientSigmaS * gradientSigmaS * 2.0f);
            TangentVariance = 1.0f / (fxdog.Coherence * fxdog.Coherence * 2.0f);
        }
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////
    // Outline
    //////////////////////////////////////////////////////////////////////////////////////////////////
    public class Outline
    {
        public float Size, InvSize, Opacity, Detail, Reverse = 1.0f;
        public readonly float Density = 5.0f;

        public void Set(InsOutline ol)
        {
            Size = ol.Size;
            InvSize =1.0f / ol.Size;
            Opacity = ol.Opacity;
            Detail = ol.Detail;
        }
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////
    // Line Integral Convolution
    //////////////////////////////////////////////////////////////////////////////////////////////////
    public class LIC
    {
        public float Scale, MaxLen, Variance;
        public void Set(DebugOptions debug)
        {
            Scale = debug.LICScale;
            MaxLen = debug.LICSigma;
            // Gσ(x) = exp(−(x^2) / (2 * σ^2)) の (2 * σ^2)
            // 分母として使うので逆数にしておく
            Variance = 1.0f / (debug.LICSigma * debug.LICSigma * 2.0f); 
        }
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////
    // Gaussian Blur
    //////////////////////////////////////////////////////////////////////////////////////////////////
    public class GBlur
    {
        public int LOD, TileSize, SampleLen, BlurSize;
        public float InvDomainSigma;
        public float DomainVariance;
        public float DomainBias;
        public float Mean;
        public bool UsePreCalc = true;
        public float[] OffsetX = new float[256];
        public float[] OffsetY = new float[256];
        public float[] DomainWeight = new float[256];

        public void Set(DebugOptions.InsGBlur gb) 
        {
            LOD = gb.LOD;
            TileSize = 1 << LOD;
            SampleLen = Max(TileSize, gb.SampleLen);
	        BlurSize = SampleLen / TileSize;

            float domainSigma = SampleLen * (1.0f / TileSize) * gb.DomainSigma;
            InvDomainSigma = 1.0f / domainSigma;
            DomainVariance = 1.0f / (domainSigma * domainSigma * 2.0f);
            DomainBias = gb.DomainBias;

	        Mean = SampleLen * 0.5f;

            UsePreCalc = true;
            if(BlurSize * BlurSize > 256)
            {
                UsePreCalc = false;
                return;
            }

            // 指数計算等を事前に済ませておく
            Vector2 offset = new Vector2();
            Vector2 mean = new Vector2(Mean, Mean);
            for(int y = 0;y < BlurSize; ++y)
            { 
                for(int x = 0; x < BlurSize; ++x)
                {
                    int index = y * BlurSize + x;
                    offset.Set(x, y);
			        offset = offset * TileSize - mean;
                    OffsetX[index] = offset.x;
                    OffsetY[index] = offset.y;

			        offset *= InvDomainSigma * DomainBias;
                    float dot = offset.x * offset.x + offset.y * offset.y;
			        float weight = Exp(-0.5f * dot) * DomainVariance;
                    DomainWeight[index] = weight;
                }
            }
        }
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////
    // Sharpen
    //////////////////////////////////////////////////////////////////////////////////////////////////
    public class Sharpen
    {
        public bool UnsharpMask;
        public int LOD, TileSize, SampleLen, BlurSize;
        public float InvDomainSigma;
        public float DomainVariance;
        public float DomainBias;
        public float Mean;
        public float Sharpness;
 
        public void Set(DebugOptions.InsSharpen s) 
        {
            UnsharpMask = s.UnsharpMask;
            LOD = s.LOD;
            TileSize = 1 << LOD;
            SampleLen = Max(TileSize, s.SampleLen);
	        BlurSize = SampleLen / TileSize;

            float domainSigma = SampleLen * (1.0f / TileSize) * s.DomainSigma;
            InvDomainSigma = 1.0f / domainSigma;
            DomainVariance = 1.0f / (domainSigma * domainSigma * 2.0f);
            DomainBias = s.DomainBias;

	        Mean = SampleLen * 0.5f;
            Sharpness = s.Sharpness;
        }
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////
    // Simplex Noise
    //////////////////////////////////////////////////////////////////////////////////////////////////
    public class SNoise
    {
        public Vector4 Size = new Vector4();
        public Vector4 Scale = new Vector4();
        public Vector4 Speed = new Vector4();
        public int RT = 6; //TODO：マジックナンバーやめる
        public void Set(DebugOptions.InsSNoise noise)
        {
            Size.Set(noise.Size1, noise.Size2, noise.Size3, noise.Size4);
            Scale.Set(noise.Scale1, noise.Scale2, noise.Scale3, noise.Scale4);
            Speed.Set(noise.Speed1, noise.Speed2, noise.Speed3, noise.Speed4);
        }
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////
    // Flow Noise
    //////////////////////////////////////////////////////////////////////////////////////////////////
    public class FNoise
    {
        public Vector4 Size = new Vector4();
        public Vector4 Scale = new Vector4();
        public Vector4 Speed = new Vector4();
        public void Set(DebugOptions.InsFNoise noise)
        {
            Size.Set(noise.Size1, noise.Size2, noise.Size3, 0.0f);
            Scale.Set(noise.Scale1, noise.Scale2, noise.Scale3, 0.0f);
            Speed.Set(noise.Speed1, noise.Speed2, noise.Speed3, 0.0f);
        }
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////
    // Test
    //////////////////////////////////////////////////////////////////////////////////////////////////
    public class Test
    {
        public float Test0, Test1, Test2, Test3, Test4, Test5, Test6, Test7, Test8, Test9;
        public void Set(DebugOptions.InsTest test) 
        { 
            Test0 = test.Test0;
            Test1 = test.Test1;
            Test2 = test.Test2;
            Test3 = test.Test3;
            Test4 = test.Test4;
            Test5 = test.Test5;
            Test6 = test.Test6;
            Test7 = test.Test7;
            Test8 = test.Test8;
            Test9 = test.Test9;
        }
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////
    // TestBF
    //////////////////////////////////////////////////////////////////////////////////////////////////
    public class TestBF
    {
        public float TestBF0, TestBF1, TestBF2, TestBF3, TestBF4;
        public float TestBF5, TestBF6, TestBF7, TestBF8, TestBF9;
        public void Set(DebugOptions.InsTestBF test) 
        { 
            TestBF0 = test.TestBF0;
            TestBF1 = test.TestBF1;
            TestBF2 = test.TestBF2;
            TestBF3 = test.TestBF3;
            TestBF4 = test.TestBF4;
            TestBF5 = test.TestBF5;
            TestBF6 = test.TestBF6;
            TestBF7 = test.TestBF7;
            TestBF8 = test.TestBF8;
            TestBF9 = test.TestBF9;
        }
    }
}


