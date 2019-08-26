using UnityEngine;

namespace UnityPostEffecs
{
    public class CanvasUI : MonoBehaviour
    {
        private static GameObject model;
        private static CanvasGroup canvas;
        private static PostEffects postEffects;

        private static readonly Vector3 moveWeightX = new Vector3(0.1f, 0.0f, 0.0f);
        private static readonly Vector3 moveWeightY = new Vector3(0.0f, 0.1f, 0.0f);
        private static readonly Vector3 moveWeightZ = new Vector3(0.0f, 0.0f, 0.1f);
        private static readonly Vector3 rotateWeight = new Vector3(0.0f, 10.0f, 0.0f);
        private static readonly Vector3 scaleWeight = new Vector3(0.01f, 0.01f, 0.01f);

        private void Start()
        {
            canvas = GameObject.Find("Canvas").GetComponent<CanvasGroup>();
            // í èÌÇÕ"Main Camera"ÅBARFoundationÇ»ÇÁ"AR Camera"
            var camera = GameObject.Find("Main Camera").GetComponent<Camera>();
            //var camera = GameObject.Find("AR Camera").GetComponent<Camera>();
            postEffects = camera.GetComponent<PostEffects>();

            // êÊì™ÇÃàÍêlÇïRïtÇØÇÈ
            string[] modelNames = postEffects.GetModelNames();
            model = GameObject.Find(modelNames[0]);
        }

        public void OnPosXPlus() { model.transform.position += moveWeightX; }
        public void OnPosXMinus() { model.transform.position -= moveWeightX; }
        public void OnPosYPlus() { model.transform.position += moveWeightY; }
        public void OnPosYMinus() { model.transform.position -= moveWeightY; }
        public void OnPosZPlus() { model.transform.position += moveWeightZ; }
        public void OnPosZMinus() { model.transform.position -= moveWeightZ; }
        public void OnScalePlus() { model.transform.localScale += scaleWeight; }
        public void OnScaleMinus() { model.transform.localScale -= scaleWeight; }
        public void OnRotatePlus() { model.transform.eulerAngles += rotateWeight; }
        public void OnHideShow() { canvas.alpha = 1.0f - canvas.alpha; }
        public void OnEffectChange() { postEffects.ChangeEffect(); }
        public void OnEffectInc() { postEffects.IncEffect(); }
        public void OnEffectDec() { postEffects.DecEffect(); }
        public void OnLumInc() { postEffects.IncLum(); }
        public void OnLumDec() { postEffects.DecLum(); }
    }
}