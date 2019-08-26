using UnityEngine;

namespace UnityPostEffecs
{
    using RT = RenderTexture;

    // 上位ベルの処理でコア機能と関係の濃いものをまとめる
    public class PostEffetcsManager
    {
        private PostEffects pe;
        private ShaderManager shader;
        private MachineLearningManager ml;

        public int SBR_LAYER_MAX { get{ return shader.SBR_LAYER_MAX; } }
        public bool initialized = false;

        public PostEffetcsManager() { }
        public string GetFaceShaderName() { return shader.GetFaceShaderName(); }
        public string GetBodyShaderName() { return shader.GetBodyShaderName(); }

        public void Init(PostEffects pe)
        {
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
        public void Validate()
        {
            // 一定間隔で呼ばれる関数を強制的に呼ぶ
            // インスペクタ側の操作を即時反映するために使う
            timeElapsedWCR = float.MaxValue;
        }

        public void Begin(RT src, CC cc, bool needsUpdate)
        { 
            shader.needsUpdate = needsUpdate;
            shader.Begin(src, cc);
        }
        public void End() { shader.End(); }
        public void Mask(RT dst){ shader.RenderMask(dst); }
        public void Sobel(RT dst){ shader.RenderSobel(shader.RT_WORK0, dst); }
        public void SST(RT dst, GBlur gb)
        {
            shader.RenderSobel(shader.RT_WORK0, shader.RT_SOBEL);
            shader.UpdateGBlur(gb); 
            shader.RenderGBlur(shader.RT_SOBEL, dst, gb);
        }
        private void SST(GBlur gb)
        {
            shader.RenderSobel(shader.RT_WORK0, shader.RT_SOBEL);
            shader.UpdateGBlur(gb); 
            shader.RenderGBlur(shader.RT_SOBEL, shader.RT_WORK0, gb); 

            shader.UpdateTFM();
            shader.RenderTFM(shader.RT_WORK0);
        }
        public void TFM(RT dst, GBlur gb)
        {
            SST(gb);
            shader.Swap(shader.RT_TFM, dst);
        }
        public void GBlur(RT dst, GBlur gb)
        {
            shader.UpdateGBlur(gb); 
            shader.RenderGBlur(shader.RT_WORK0, dst, gb);
        }
        public void SBR(RT dst, GBlur gb, Posterize pst, SBR sbr)
        {
            SST(gb);
            shader.UpdatePosterize(pst, true);
            shader.RenderPosterize(shader.RT_ORIG, shader.RT_SBR_HSV);

            // 0:HSV, 1:ORIG, 2:TFM, 3:SOBEL(.w)->OUTLINE
            shader.UpdateSBR(sbr);
            shader.RenderSBR(shader.RT_SBR_HSV, dst);
        }

        private float timeElapsedWCR = float.MaxValue;
        public void WCR(RT dst, GBlur gb, BF bf, WCR wcr)
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

            BF(shader.GetRT(shader.RT_WORK0), gb, bf);
            shader.UpdateHandTremor(wcr);
            shader.RenderHandTremor(shader.RT_WORK0, shader.RT_WORK4, wcr);
            shader.Swap(shader.RT_WORK4, shader.RT_WORK0);

            SST(gb);
            shader.UpdateWCR(wcr);
            shader.RenderWCR(shader.RT_WORK4, dst, wcr);
        }
        public void BF(RT dst, GBlur gb, BF bf)
        {
            SST(gb);
            shader.RenderRGB2LAB(shader.RT_ORIG, shader.RT_WORK0);
            shader.UpdateBF(bf);
            shader.RenderBF(shader.RT_WORK0, shader.RT_WORK3, shader.RT_WORK4, bf);
            shader.RenderLAB2RGB(shader.RT_WORK3, dst);
        }
        public void AKF(RT dst, GBlur gb, AKF akf)
        {
            // Sobel後にブラーを掛けない場合はKuhawara Filterとほぼ同じ見た目になる
            SST(gb);
            shader.UpdateAKF(akf);
            shader.RenderAKF(shader.RT_WORK0, dst);
        }
        public void SNN(RT dst, Posterize pst, SNN snn)
        {
            shader.UpdatePosterize(pst, true);
            shader.RenderPosterize(shader.RT_WORK0, shader.RT_WORK3);
            shader.UpdateSNN(snn);
            shader.RenderSNN(shader.RT_WORK3, shader.RT_WORK0);
            shader.RenderHSV2RGB(shader.RT_WORK0, dst);
        }
        public void FXDoG(RT dst, GBlur gb, FXDoG fxdog)
        {
            SST(gb);
            shader.UpdateFXDoG(fxdog);
            shader.RenderFXDoG(shader.RT_WORK0, dst, shader.RT_WORK3);
        }
        public void Outline(RT dst, Outline ol)
        {
            shader.RenderSobel(shader.RT_WORK0, shader.RT_SOBEL);
            shader.UpdateOutline(ol);
            shader.RenderOutline(shader.RT_ORIG, dst);
        }
        public void LIC(RT dst, GBlur gb, LIC lic)
        {
            SST(gb);
            shader.UpdateLIC(lic);
            shader.RenderLIC(dst);
        }
        public void Posterize(RT dst, Posterize pst)
        {
            shader.UpdatePosterize(pst, false);
            shader.RenderPosterize(shader.RT_WORK0, dst);
        }
        public void SNoise(RT dst, SNoise noise)
        {
            shader.UpdateSNoise(noise);
            shader.RenderSNoise(dst, noise);
        }
        public void FNoise(RT dst, FNoise noise)
        {
            shader.UpdateFNoise(noise);
            shader.RenderFNoise(dst);
        }
        public void VNoise(RT dst)
        {
            shader.UpdateVNoise();
            shader.RenderVNoise(dst);
        }
        public void Canvas(Canvas c){ shader.UpdateCanvas(c); }

        public void Swap(int src, RT dst) { shader.Swap(src, dst); }
        public void Default(RT dst) { shader.Swap(shader.RT_WORK0, dst); }
        public void Test(RT dst, Test test)
        { 
            shader.UpdateTest(test); 
            shader.RenderTest(shader.RT_WORK0, dst); 
        }
        public void TestBF(RT dst, TestBF bf, GBlur gb)
        { 
            shader.UpdateGBlur(gb); 
            shader.RenderGBlur(shader.RT_WORK0, shader.RT_WORK2, gb); 
            shader.UpdateTestBF(bf); 
            shader.RenderTestBF(shader.RT_WORK0, dst); 
        }
    }
}


