using System;
using UnityEngine;

namespace UnityPostEffecs
{
    using ET = PostEffects.EffectType;

    [ExecuteInEditMode, ImageEffectAllowedInSceneView, RequireComponent(typeof(Camera))]
    public class PostEffects : MonoBehaviour
    {
        public enum EffectType
        {
            None, SBR, WCR, BF, AKF, SNN, FXDoG, Outline, 
            Mask, Sobel, SST, TFM, LIC, GBlur, Posterize, 
            SNoise, FNoise, VNoise, Test, TestBF
        }

        private PostEffetcsManager manager = new PostEffetcsManager();
        private PostEffectsHelper helper = new PostEffectsHelper();
        private bool needsUpdate = true;

        [Header("Common Options")]
        [SerializeField] internal CommonOptions CommonParameters = new CommonOptions();

        [Header("Stroke Based Rendering")]
        [SerializeField] internal bool SBREnable = false;
        [SerializeField] internal InsSBR SBRParameters = new InsSBR();

        [Header("Watercolor Rendering")]
        [SerializeField] internal bool WCREnable = false;
        [SerializeField] internal InsWCR WCRParameters = new InsWCR();

        [Header("Anisotropic Kuwahara Filter")]
        [SerializeField] internal bool AKFEnable = false;
        [SerializeField] internal InsAKF AKFParameters = new InsAKF();

        [Header("Symmetric Nearest Neighbor")]
        [SerializeField] internal bool SNNEnable = false;
        [SerializeField] internal InsSNN SNNParamters = new InsSNN();

        [Header("Bilateral Filter")]
        [SerializeField] internal bool BFEnable = false;
        [SerializeField] internal InsBF BFParameters = new InsBF();

        [Header("Outline")]
        [SerializeField] internal bool OutlineEnable = false;
        [SerializeField] internal InsOutline OutlineParameters = new InsOutline();

        [Header("Flow based eXtended Difference of Gaussians ")]
        [HideInInspector][SerializeField] internal bool FXDoGEnable = false;
        [HideInInspector][SerializeField] internal InsFXDoG FXDoGParamters = new InsFXDoG();

        [Header("Mobile Options")]
        [SerializeField] internal MobileOptions MobileParameters = new MobileOptions();

        [Header("Debug Options")]
        [SerializeField] internal DebugOptions DebugParameters = new DebugOptions();

        private void Awake()
        {
            helper.Init(this);
            helper.SetFrameRate(MobileParameters.FrameRate);
            helper.SetResolutionScale(MobileParameters.ResolutionScale);
        }

        private void Start()
        {
            manager.Init(this);

            helper.SetModels(CommonParameters.ModelNames);
            string faceShaderName = manager.GetFaceShaderName();
            var faceMeshNames = CommonParameters.MaskParameters.FaceMeshNames;
            var faceMaterialNames = CommonParameters.MaskParameters.FaceMaterialNames;
            helper.SetMask(faceMeshNames, faceMaterialNames, faceShaderName);

            string bodyShaderName = manager.GetBodyShaderName();
            var bodyMeshNames = CommonParameters.MaskParameters.BodyMeshNames;
            var bodyMaterialNames = CommonParameters.MaskParameters.BodyMaterialNames;
            helper.SetMask(bodyMeshNames, bodyMaterialNames, bodyShaderName);

            helper.SetFacialExpression(DebugParameters.FaceMeshName);

            helper.DisableOutline();
            helper.EnableUpdateWhenOffscreen(true);
            helper.ValidateEnableFlags(this);
        }

        private void Update()
        {
            if (!helper.initialized) { return; }
            if (DebugParameters.FacialExpression){ helper.UpdateFacialExpression(); }
        }

        public void OnValidate()
        {
            if (!helper.initialized) { return; }
            helper.SetFrameRate(MobileParameters.FrameRate);
            helper.ValidateEnableFlags(this);
            manager.Validate();

            needsUpdate = true;
        }

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

        private void UpdateParameters(int width, int height)
        {
            cc.Set(CommonParameters.CCParameters);
            cavas.Set(CommonParameters.CanvasParameters);
            sbr.Set(SBRParameters, manager.SBR_LAYER_MAX, width, height);
            wcr.Set(WCRParameters, CommonParameters.CanvasParameters);
            bf.Set(BFParameters);
            akf.Set(AKFParameters);
            snn.Set(SNNParamters);
            outline.Set(OutlineParameters);
            fxdog.Set(FXDoGParamters);
            lic.Set(DebugParameters);
            gblur.Set(DebugParameters.GBlurParameters);
            pst.Set(DebugParameters.PosterizeParameters);
            snoise.Set(DebugParameters.SimplexNoiseParameters);
            fnoise.Set(DebugParameters.FlowNoiseParameters);
            test.Set(DebugParameters.TestParameters);
            testBF.Set(DebugParameters.TestBFParameters);
        }

        public void OnRenderImage(RenderTexture src, RenderTexture dst)
        {
            if (!manager.initialized) { return; }

            if (needsUpdate){ UpdateParameters(src.width, src.height); }

            manager.Begin(src, cc, needsUpdate);
            manager.Canvas(cavas);
            switch (helper.currentEffect)
            {
                case ET.SBR: manager.SBR(dst, gblur, pst, sbr); break;
                case ET.WCR: manager.WCR(dst, gblur, bf, wcr); break;
                case ET.BF: manager.BF(dst, gblur, bf); break;
                case ET.AKF: manager.AKF(dst, gblur, akf); break;
                case ET.SNN: manager.SNN(dst, pst, snn); break;
                case ET.FXDoG: manager.FXDoG(dst, gblur, fxdog); break;
                case ET.Outline: manager.Outline(dst, outline); break;
                case ET.Mask: manager.Mask(dst); break;
                case ET.Sobel: manager.Sobel(dst); break;
                case ET.SST: manager.SST(dst, gblur); break;
                case ET.TFM: manager.TFM(dst, gblur); break;
                case ET.LIC: manager.LIC(dst, gblur, lic); break;
                case ET.GBlur: manager.GBlur(dst, gblur); break;
                case ET.Posterize: manager.Posterize(dst, pst); break;
                case ET.SNoise: manager.SNoise(dst, snoise); break;
                case ET.FNoise: manager.FNoise(dst, fnoise); break;
                case ET.VNoise: manager.VNoise(dst); break;
                case ET.Test: manager.Test(dst, test); break;
                case ET.TestBF: manager.TestBF(dst, testBF, gblur); break;
                default: manager.Default(dst); break;
            }
            manager.End();

            needsUpdate = false;
        }
        public void SetDefaultParamsSBR() 
        {
            var cc = CommonParameters.CCParameters;
            var gb = DebugParameters.GBlurParameters;
            DefaultParams.SetSBR(ref SBRParameters.Layers, ref cc, ref gb); 
            needsUpdate = true;
        }
        public void SetDefaultParamsWCR() 
        {
            var cc = CommonParameters.CCParameters;
            var gb = DebugParameters.GBlurParameters;
            DefaultParams.SetWCR(ref WCRParameters, ref cc, ref gb, ref BFParameters); 
            needsUpdate = true;
        }
        public void SetDefaultParamsAKF() 
        {
            var gb = DebugParameters.GBlurParameters;
            DefaultParams.SetAKF(ref AKFParameters, ref gb); 
            needsUpdate = true;
        }
        public void SetDefaultParamsBF() 
        {
            var gb = DebugParameters.GBlurParameters;
            DefaultParams.SetBF(ref BFParameters, ref gb); 
            needsUpdate = true;
        }

        public string[] GetModelNames() { return CommonParameters.ModelNames; }
        public void ChangeEffect() 
        { 
            helper.ChangeEffect(this); 
            needsUpdate = true;
        }
        public void IncEffect() 
        { 
            helper.IncEffect(this); 
            needsUpdate = true;
        }
        public void DecEffect() 
        { 
            helper.DecEffect(this); 
            needsUpdate = true;
        }
        public void IncLum() 
        { 
            helper.IncLum(this); 
            needsUpdate = true;
        }
        public void DecLum() 
        { 
            helper.DecLum(this); 
            needsUpdate = true;
        }
    }
}


