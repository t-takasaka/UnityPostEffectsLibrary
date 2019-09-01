#ifndef BF_INCLUDED
#define BF_INCLUDED

//////////////////////////////////////////////////////////////////////////////////////////////////
// Bilateral Filter
//////////////////////////////////////////////////////////////////////////////////////////////////
float _BFSampleLen;
float _BFOrthogonalize;
float _BFDomainVariance;
float _BFDomainBias;
float _BFRangeVariance;
float _BFRangeBias;
float _BFRangeThreshold;
float _BFStepDirScale;
float _BFStepLenScale;

void calcWeightBF(float4 color, float2 uv, float2 offset, float domainWeight, inout float4 colorSum)
{
	float4 colorNeighbor = smpl(uv + offset);
	float4 range = (colorNeighbor - color) * _BFRangeBias;
	// 注目画素と周辺画素の色差の重み
	float rangeWeight = getGaussianWeight(range, _BFRangeVariance);
	float weight = domainWeight * rangeWeight;
	colorSum += colorNeighbor * weight;
}

// 事前に計算しておくパターン
float _BFRangeWeight[256];
void calcWeightBF2(float4 color, float2 uv, float2 offset, float domainWeight, inout float4 colorSum)
{
	float4 colorNeighbor = smpl(uv + offset);
	float4 range = (colorNeighbor - color);
	// RGBAの4チャンネルだとインデックスは256*4まで必要だが、
	// 重みは負の指数なのでインデックスが大きいとほとんどの場合は0になる
	// （ただしバイアスで指定する値によっては0でない場合もあり得る）
	// ユニフォームの上限があるため、ひとまず255までで打ち切る
	int index = min(floor(dot(range, range) * 255.0), 255);
	float weight = _BFRangeWeight[index];
	colorSum += colorNeighbor * weight;
}

// レンジが極めて近い画素の重みを1.0、それ以外は0.0で決め打ちするパターン
inline void calcWeightBF3(float4 color, float2 uv, float2 offset, float domainWeight, inout float4 colorSum)
{
	float4 colorNeighbor = smpl(uv + offset);
	float4 range = (colorNeighbor - color);
	float weight = step(dot(range, range), _BFRangeThreshold);
	colorSum += colorNeighbor * weight;
}

// 注目画素の接線方向を直線的にサンプルする
float4 fragBF(v2f_img i) : SV_Target
{
	// 1パス目なら90度回転させて勾配方向にする
	float2 isGradient = _BFOrthogonalize.xx;
	float2 stepDir;
	float stepLen;
	calcStep(i.uv, isGradient, stepDir, stepLen);
	stepDir *= _BFStepDirScale;
	// Unity側で保存直後に0が渡されることがある
	// 後段のfor文で無限ループになるため最低1を入れておく
	stepLen *= max(1.0f, _BFStepLenScale);

	float4 color = smpl(i.uv);
	float4 colorSum = color;

	for (float totalLen = stepLen; totalLen <= _BFSampleLen; totalLen += stepLen)
	{
		// 負荷の割に影響が薄いので一旦無効化
		float domainWeight = 1.0;// getDomainWeight(totalLen, _BFDomainVariance, _BFDomainBias);

		// 反対方向に二個所をサンプルする
		float2 offset = totalLen * stepDir;
		calcWeightBF(color, i.uv, offset, domainWeight, colorSum);
		calcWeightBF(color, i.uv, -offset, domainWeight, colorSum);
	}

	return colorSum / colorSum.a;
}

void convFBF(float2 uv, float4 tangent, float4 color, inout float4 colorSum)
{
	float2 offset = 0.0;
	for (float totalLen = 0.0; totalLen < _BFSampleLen;)
	{
		// 接線方向の1ステップ分の距離
		float2 stepLen = getTangentStepLen(uv, tangent, offset);
		// Unity側で保存直後に0が渡されることがある
		// 後段のfor文で無限ループになるため最低1を入れておく
		stepLen *= max(1.0f, _BFStepLenScale);
		totalLen += stepLen.x + stepLen.y;

		// 負荷の割に影響が薄いので一旦無効化
		float domainWeight = 1.0;// getDomainWeight(totalLen, _BFDomainVariance, _BFDomainBias);

		calcWeightBF(color, uv, offset, domainWeight, colorSum);
	}
}
// サンプル画素毎に接線方向の1ステップ分の距離を再計算する。精確だが比較的重くなる
float4 fragFBF(v2f_img i) : SV_Target
{
	float4 tfm = smpl(_RT_TFM, i.uv);
	// 1ステップ分の方向
	float2 stepDir = tfm.xy;
	stepDir *= _BFStepDirScale;

	float4 tangent = 0.0;
	tangent.zw = stepDir * UV_SIZE;

	float4 color = smpl(i.uv);
	float4 colorSum = color;

	// 反対方向に二個所をサンプルする
	convFBF(i.uv, tangent, color, colorSum);
	convFBF(i.uv, -tangent, color, colorSum);

	return colorSum / colorSum.a;
}

//////////////////////////////////////////////////////////////////////////////////////////////////
// 実験用
//////////////////////////////////////////////////////////////////////////////////////////////////
float _TestBF0, _TestBF1, _TestBF2, _TestBF3, _TestBF4, _TestBF5, _TestBF6, _TestBF7, _TestBF8, _TestBF9;

float4 fragTestBF(v2f_img i) : SV_Target
{
	return smpl(i.uv);
}

#endif