using UnityEngine;

namespace UnityPostEffecs
{
    using RT = RenderTexture;
    using ET = PostEffects.EffectType;

    // 上位ベルの処理でコア機能と関係の濃いものをまとめる
    public class PostEffetcsManager
    {
        private PostEffects pe;
        private ShaderManager shader;
        private MachineLearningManager ml;

        private CC cc = new CC();
        private Canvas cavas = new Canvas();
        private Posterize pst = new Posterize();
        private SBR sbr = new SBR();
        private WCR wcr = new WCR();
        private BF bf = new BF();
        private AKF akf = new AKF();
        private SNN snn = new SNN();
        private FXDoG fxdog = new FXDoG();
        private Outline outline = new Outline();
        private LIC lic = new LIC();
        private GBlur gblur = new GBlur();
        private SNoise snoise = new SNoise();
        private FNoise fnoise = new FNoise();
        private Test test = new Test();
        private TestBF testBF = new TestBF();

        public int SBR_LAYER_MAX { get{ return shader.SBR_LAYER_MAX; } }
        public bool initialized = false;

        public PostEffetcsManager() { }
        public string GetFaceShaderName() { return shader.GetFaceShaderName(); }
        public string GetBodyShaderName() { return shader.GetBodyShaderName(); }

        public void Init(PostEffects pe)
        {
            this.pe = pe;
            var camera = pe.GetComponent<Camera>();
            camera.depthTextureMode |= DepthTextureMode.Depth;
            // MSAAが有効だとOnRenderImageでデプスが取れないので無効にする
            camera.allowMSAA = false;
            
            // 深度でLinear01Depthを使いたいのでFarを短めに指定する
            //camera.depthTextureMode |= DepthTextureMode.DepthNormals;
            //camera.nearClipPlane = 0.01f;
            //camera.farClipPlane = 5.0f;

            shader = new ShaderManager(camera, pe.DebugParameters.DebugTexture);
            shader.needsUpdate = true;

            initialized = true;
        }
        public void Update(int width, int height)
        {
            cc.Set(pe.CommonParameters.CCParameters);
            cavas.Set(pe.CommonParameters.CanvasParameters);
            sbr.Set(pe.SBRParameters, SBR_LAYER_MAX, width, height);
            wcr.Set(pe.WCRParameters, pe.CommonParameters.CanvasParameters);
            bf.Set(pe.BFParameters);
            akf.Set(pe.AKFParameters);
            snn.Set(pe.SNNParamters);
            outline.Set(pe.OutlineParameters);
            fxdog.Set(pe.FXDoGParamters);
            lic.Set(pe.DebugParameters);
            gblur.Set(pe.DebugParameters.GBlurParameters);
            pst.Set(pe.DebugParameters.PosterizeParameters);
            snoise.Set(pe.DebugParameters.SimplexNoiseParameters);
            fnoise.Set(pe.DebugParameters.FlowNoiseParameters);
            test.Set(pe.DebugParameters.TestParameters);
            testBF.Set(pe.DebugParameters.TestBFParameters);
        }
        public void Render(RT src, RT dst, ET currentEffect, bool needsUpdate) 
        { 
            if (needsUpdate){ Update(src.width, src.height); }

            Begin(src, needsUpdate);
            Canvas();
            switch (currentEffect)
            {
                case ET.SBR: SBR(dst); break;
                case ET.WCR: WCR(dst); break;
                case ET.BF: BF(dst); break;
                case ET.AKF: AKF(dst); break;
                case ET.SNN: SNN(dst); break;
                case ET.FXDoG: FXDoG(dst); break;
                case ET.Outline: Outline(dst); break;
                case ET.Mask: Mask(dst); break;
                case ET.Sobel: Sobel(dst); break;
                case ET.SST: SST(dst); break;
                case ET.TFM: TFM(dst); break;
                case ET.LIC: LIC(dst); break;
                case ET.GBlur: GBlur(dst); break;
                case ET.Posterize: Posterize(dst); break;
                case ET.SNoise: SNoise(dst); break;
                case ET.FNoise: FNoise(dst); break;
                case ET.VNoise: VNoise(dst); break;
                case ET.Test: Test(dst); break;
                case ET.TestBF: TestBF(dst); break;
                default: Default(dst); break;
            }
            End();
        }

        public void Validate()
        {
            // 一定間隔で呼ばれる関数を強制的に呼ぶ
            // インスペクタ側の操作を即時反映するために使う
            timeElapsedWCR = float.MaxValue;
        }

        public void Begin(RT src, bool needsUpdate)
        { 
            shader.needsUpdate = needsUpdate;
            shader.Begin(src, cc);
        }
        public void End() { shader.End(); }
        public void Mask(RT dst){ shader.RenderMask(dst); }
        public void Sobel(RT dst){ shader.RenderSobel(shader.RT_WORK0, dst); }
        public void SST(RT dst)
        {
            shader.RenderSobel(shader.RT_WORK0, shader.RT_SOBEL);
            shader.UpdateGBlur(gblur); 
            shader.RenderGBlur(shader.RT_SOBEL, dst, gblur);
        }
        private void SST()
        {
            shader.RenderSobel(shader.RT_WORK0, shader.RT_SOBEL);
            shader.UpdateGBlur(gblur); 
            shader.RenderGBlur(shader.RT_SOBEL, shader.RT_WORK0, gblur); 

            shader.UpdateTFM();
            shader.RenderTFM(shader.RT_WORK0);
        }
        public void TFM(RT dst)
        {
            SST();
            shader.Swap(shader.RT_TFM, dst);
        }
        public void GBlur(RT dst)
        {
            shader.UpdateGBlur(gblur); 
            shader.RenderGBlur(shader.RT_WORK0, dst, gblur);
        }
        public void SBR(RT dst)
        {
            SST();
            shader.UpdatePosterize(pst, true);
            shader.RenderPosterize(shader.RT_ORIG, shader.RT_SBR_HSV);

            // 0:HSV, 1:ORIG, 2:TFM, 3:SOBEL(.w)->OUTLINE
            shader.UpdateSBR(sbr);
            shader.RenderSBR(shader.RT_SBR_HSV, dst);
        }

        private float timeElapsedWCR = float.MaxValue;
        public void WCR(RT dst)
        {
            // ノイズ生成の負荷が大きいので毎フレーム呼ばないようにする
            timeElapsedWCR += Time.deltaTime;
            if(timeElapsedWCR >= wcr.SNoiseUpdateTime)
            { 
                timeElapsedWCR = 0.0f;
                shader.UpdateSNoise(wcr.SNoise1);
                shader.RenderSNoise(wcr.SNoise1.RT, wcr.SNoise1);
                shader.UpdateSNoise(wcr.SNoise2);
                shader.RenderSNoise(wcr.SNoise2.RT, wcr.SNoise2);
            }

            BF(shader.GetRT(shader.RT_WORK0));
            shader.UpdateHandTremor(wcr);
            shader.RenderHandTremor(shader.RT_WORK0, shader.RT_WORK4, wcr);
            shader.Swap(shader.RT_WORK4, shader.RT_WORK0);

            SST();
            shader.UpdateWCR(wcr);
            shader.RenderWCR(shader.RT_WORK4, dst, wcr);
        }
        public void BF(RT dst)
        {
            SST();
            shader.RenderRGB2LAB(shader.RT_ORIG, shader.RT_WORK0);
            shader.UpdateBF(bf);
            shader.RenderBF(shader.RT_WORK0, shader.RT_WORK3, shader.RT_WORK4, bf);
            shader.RenderLAB2RGB(shader.RT_WORK3, dst);
        }
        public void AKF(RT dst)
        {
            // Sobel後にブラーを掛けない場合はKuhawara Filterとほぼ同じ見た目になる
            SST();
            shader.UpdateAKF(akf);
            shader.RenderAKF(shader.RT_WORK0, dst);
        }
        public void SNN(RT dst)
        {
            shader.UpdatePosterize(pst, true);
            shader.RenderPosterize(shader.RT_WORK0, shader.RT_WORK3);
            shader.UpdateSNN(snn);
            shader.RenderSNN(shader.RT_WORK3, shader.RT_WORK0);
            shader.RenderHSV2RGB(shader.RT_WORK0, dst);
        }
        public void FXDoG(RT dst)
        {
            SST();
            shader.UpdateFXDoG(fxdog);
            shader.RenderFXDoG(shader.RT_WORK0, dst, shader.RT_WORK3);
        }
        public void Outline(RT dst)
        {
            shader.RenderSobel(shader.RT_WORK0, shader.RT_SOBEL);
            shader.UpdateOutline(outline);
            shader.RenderOutline(shader.RT_ORIG, dst);
        }
        public void LIC(RT dst)
        {
            SST();
            shader.UpdateLIC(lic);
            shader.RenderLIC(dst);
        }
        public void Posterize(RT dst)
        {
            shader.UpdateGBlur(gblur); 
            shader.RenderGBlur(shader.RT_WORK0, shader.RT_WORK2, gblur);
            shader.UpdatePosterize(pst, false);
            shader.RenderPosterize(shader.RT_WORK2, dst);
        }
        public void SNoise(RT dst)
        {
            shader.UpdateSNoise(snoise);
            shader.RenderSNoise(dst, snoise);
        }
        public void FNoise(RT dst)
        {
            shader.UpdateFNoise(fnoise);
            shader.RenderFNoise(dst);
        }
        public void VNoise(RT dst)
        {
            shader.UpdateVNoise();
            shader.RenderVNoise(dst);
        }
        public void Canvas(){ shader.UpdateCanvas(cavas); }

        public void Swap(int src, RT dst) { shader.Swap(src, dst); }
        public void Default(RT dst) { shader.Swap(shader.RT_WORK0, dst); }
        public void Test(RT dst)
        { 
            shader.UpdateTest(test); 
            shader.RenderTest(shader.RT_WORK0, dst); 
        }
        public void TestBF(RT dst)
        { 
            shader.UpdateGBlur(gblur); 
            shader.RenderGBlur(shader.RT_WORK0, shader.RT_WORK2, gblur); 
            shader.UpdateTestBF(testBF); 
            shader.RenderTestBF(shader.RT_WORK0, dst); 
        }
    }
}


