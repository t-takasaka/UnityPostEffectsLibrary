using UnityEngine;

namespace UnityPostEffecs
{
    public class Stats : MonoBehaviour
    {
        private float interval = 0.5f;
        private float accum;
        private int frames;
        private float timeLeft;
        private float fps;

        private void Update()
        {
            timeLeft -= Time.deltaTime;
            accum += Time.timeScale / Time.deltaTime;
            ++frames;

            if (0 < timeLeft) { return; }

            fps = accum / frames;
            timeLeft = interval;
            accum = 0;
            frames = 0;
        }

        private void OnGUI()
        {
            GUI.color = Color.black;
            GUI.skin.label.fontSize = 30;
            GUILayout.BeginVertical("box");
            GUILayout.Label("FPS: " + fps.ToString("f2"));
            GUILayout.Label("WIDTH:" + Screen.width.ToString());
            GUILayout.Label("HIGHT:" + Screen.height.ToString());
            GUILayout.EndVertical();
        }
    }
}