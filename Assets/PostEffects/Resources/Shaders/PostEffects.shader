Shader "Hidden/PostEffects"
{
	CGINCLUDE
#include "UnityCG.cginc"
#include "Common.cginc"
#include "Noise.cginc"
#include "Color.cginc"
#include "Canvas.cginc"
#include "Edge.cginc"
#include "Smooth.cginc"
#include "AKF.cginc"
#include "SBR.cginc"
#include "BF.cginc"
#include "WCR.cginc"
#include "FXDoG.cginc"

	ENDCG

	SubShader
	{
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			Name "Entry"
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment fragEntry
			ENDCG
		}

		Pass
		{
			Stencil
			{
				Ref 1
				Comp Equal
			}

			Name "MaskFace"
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment fragMask
			float4 fragMask(v2f_img i) : SV_Target{ return 1.0; }
			ENDCG
		}
		Pass
		{
			Stencil
			{
				Ref 2
				Comp Equal
			}

			Name "MaskBody"
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment fragMask
			float4 fragMask(v2f_img i) : SV_Target{ return 0.5; }
			ENDCG
		}

		Pass
		{
			Name "SBR"
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment fragSBR
			ENDCG
		}
		Pass
		{
			Name "WCR"
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment fragWCR
			ENDCG
		}
		Pass
		{
			Name "HandTremor"
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment fragHandTremor
			ENDCG
		}
		Pass
		{
			Name "BF"
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment fragBF
			ENDCG
		}
		Pass
		{
			Name "FBF"
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment fragFBF
			ENDCG
		}
		Pass
		{
			Name "AKF"
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment fragAKF
			ENDCG
		}
		Pass
		{
			Name "SNN"
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment fragSNN
			ENDCG
		}
		Pass
		{
			Name "Posterize"
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment fragPosterize
			ENDCG
		}
		Pass
		{
			Name "Outline"
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment fragOutline
			ENDCG
		}
		Pass
		{
			Name "FXDoGGradient"
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment fragFXDoGGradient
			ENDCG
		}
		Pass
		{
			Name "FXDoGTangent"
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment fragFXDoGTangent
			ENDCG
		}
		Pass
		{
			Name "TFM"
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment fragTFM
			ENDCG
		}
		Pass
		{
			Name "LIC"
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment fragLIC
			ENDCG
		}
		Pass
		{
			Name "Lerp"
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment fragLerp
			ENDCG
		}
		Pass
		{
			Name "Sobel3"
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment fragSobel3
			ENDCG
		}
		Pass
		{
			Name "GBlur"
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment fragGBlur
			ENDCG
		}
		Pass
		{
			Name "GBlur2"
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment fragGBlur2
			ENDCG
		}
		Pass
		{
			Name "Sharpen"
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment fragSharpen
			ENDCG
		}
		Pass
		{
			Name "UnsharpMask"
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment fragUnsharpMask
			ENDCG
		}
		Pass
		{
			Name "Complementary"
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment fragComplementary
			ENDCG
		}
		Pass
		{
			Name "RGB2HSV"
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment fragRGB2HSV
			ENDCG
		}
		Pass
		{
			Name "HSV2RGB"
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment fragHSV2RGB
			ENDCG
		}
		Pass
		{
			Name "RGB2HSL"
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment fragRGB2HSL
			ENDCG
		}
		Pass
		{
			Name "HSL2RGB"
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment fragHSL2RGB
			ENDCG
		}
		Pass
		{
			Name "RGB2YUV"
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment fragRGB2YUV
			ENDCG
		}
		Pass
		{
			Name "YUV2RGB"
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment fragYUV2RGB
			ENDCG
		}
		Pass
		{
			Name "RGB2LAB"
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment fragRGB2LAB
			ENDCG
		}
		Pass
		{
			Name "LAB2RGB"
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment fragLAB2RGB
			ENDCG
		}
		Pass
		{
			Name "GNoise"
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment fragGNoise
			ENDCG
		}
		Pass
		{
			Name "SNoise"
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment fragSNoise
			ENDCG
		}
		Pass
		{
			Name "FNoise"
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment fragFNoise
			ENDCG
		}
		Pass
		{
			Name "VNoise"
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment fragVNoise
			ENDCG
		}

		Pass
		{
			Name "Test"
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment fragTest
			ENDCG
		}
		Pass
		{
			Name "TestBF"
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment fragTestBF
			ENDCG
		}
	}
}
