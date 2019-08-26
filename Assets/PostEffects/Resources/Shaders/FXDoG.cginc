#ifndef FXDOG_INCLUDED
#define FXDOG_INCLUDED

//////////////////////////////////////////////////////////////////////////////////////////////////
// Flow based eXtended Difference of Gaussians 
//////////////////////////////////////////////////////////////////////////////////////////////////
float _FXDoGGradientMaxLen;
float _FXDoGTangentMaxLen;
float _FXDoGSharpness;
float _FXDoGSmoothRange;
float _FXDoGThresholdSlope;
float _FXDoGThreshold;
float _FXDoGInvGradientSigmaSq2L;
float _FXDoGInvGradientSigmaSq2S;
float _FXDoGInvTangentSigmaSq2;

float2 convFXDoGGradient(float2 uv, float2 stepDir, float stepLen) 
{
	// RGBだと色差がある個所でノイズが発生するため輝度を使う
	float2 colorSum = smplLum(uv).xx;
	float2 weightSum = 1.0;
	float2 invSigmaSq2 = float2(_FXDoGInvGradientSigmaSq2L, _FXDoGInvGradientSigmaSq2S);

	for (float totalLen = stepLen; totalLen <= _FXDoGGradientMaxLen; totalLen += stepLen)
	{
		float2 weight = getDomainWeight(totalLen, invSigmaSq2, 1.0);

		// 反対方向に二個所をサンプルする
		float2 offset = totalLen * stepDir;
		float color1 = smplLum(uv + offset).x;
		float color2 = smplLum(uv - offset).x;
		float color = (color1 + color2) * 0.5;
		colorSum += color * weight;
		weightSum += weight;
	}

	return colorSum / weightSum;
}
// 高速化のため、平滑化フィルタを勾配・接線方向の2パスに分離する。1パス目は勾配方向
float4 fragFXDoGGradient(v2f_img i) : SV_Target
{
	float2 stepDir;
	float stepLen;
	calcStep(i.uv, 1.0, stepDir, stepLen);

	float2 colorSum = convFXDoGGradient(i.uv, stepDir, stepLen);

	// 二つのフィルタの差分を強調したアンシャープマスクを作る
	// colorSum.xがカーネルの小さい（振幅の大きい）フィルタの輝度
	// colorSum.yがカーネルの大きい（振幅の小さい）フィルタの輝度
	float unsharpMask = _FXDoGSharpness * (colorSum.x - colorSum.y);

	//アンシャープマスクを足して鮮鋭化する
	return colorSum.x + unsharpMask;
}

void convFXDoGTangent(float2 uv, float4 tangent, inout float colorSum, inout float weightSum)
{
	float2 offset = 0.0;
	float2 totalStep = 0.0;
	float invSigmaSq2 = _FXDoGInvTangentSigmaSq2;

	for (float totalLen = 0.0; totalLen < _FXDoGTangentMaxLen;)
	{
		// 接線方向の1ステップ分の距離
		float2 stepLen = getTangentStepLen(uv, tangent, offset);
		totalLen += stepLen.x + stepLen.y;

		float weight = getDomainWeight(totalLen, invSigmaSq2, 1.0);
		// 輝度をサンプルする
		// ※1パス目が輝度を返しているので輝度専用の関数は使わない
		float color = smpl(uv + offset).x;
		colorSum += color * weight;
		weightSum += weight;
	}
}
// 高速化のため、平滑化フィルタを勾配・接線方向の2パスに分離する。2パス目は接線方向
float4 fragFXDoGTangent(v2f_img i) : SV_Target
{
	float4 tfm = smpl(_RT_TFM, i.uv);
	// 1ステップ分の方向
	float2 stepDir = tfm.xy;

	float4 tangent = 0.0;
	tangent.zw = stepDir * UV_SIZE;

	float colorSum = smpl(i.uv).x;
	float weightSum = 1.0;

	// 反対方向に二個所をサンプルする
	convFXDoGTangent(i.uv, tangent, colorSum, weightSum);
	convFXDoGTangent(i.uv, -tangent, colorSum, weightSum);
	colorSum /= weightSum;

	return colorSum;
}

#endif