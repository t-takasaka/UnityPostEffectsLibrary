using UnityEngine;

namespace UnityPostEffecs
{
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
        public void OnGUI() { needsUpdate = true; }

        public void OnRenderImage(RenderTexture src, RenderTexture dst)
        {
            if (!manager.initialized) { return; }

            manager.Render(src, dst, helper.currentEffect, needsUpdate);
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


