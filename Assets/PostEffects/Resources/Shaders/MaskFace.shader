Shader "Hidden/PostEffectsMaskFace"
{
	CGINCLUDE
#include "UnityCG.cginc"
	ENDCG

	SubShader
	{
		Tags { "Queue" = "Overlay+1000" }

		// マスク自体の色は画面に出さない
		Blend Zero One
		// デバッグ表示用
		//Blend One Zero

		ZTest Always

		Stencil
		{
			Ref 1
			Comp Always
			Pass Replace
			Fail Zero
			ZFail Zero
		}

        Pass
		{
            CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag

			float _MaskType;
            float4 frag(v2f_img i) : SV_Target{ return 1.0; }
            ENDCG
        }
    }
}