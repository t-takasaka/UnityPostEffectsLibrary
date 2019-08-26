#ifndef EDGE_INCLUDED
#define EDGE_INCLUDED

//////////////////////////////////////////////////////////////////////////////////////////////////
// Sobel
//////////////////////////////////////////////////////////////////////////////////////////////////
float _SobelCarryDigit;
float _SobelInvCarryDigit;

static const float sobel3x3H[9] = { -1, 0, 1, -2, 0, 2, -1, 0, 1 };
static const float sobel3x3V[9] = { -1, -2, -1, 0, 0, 0, 1, 2, 1 };
static const float4 sobel3x3[9] = 
{
	{ -1, -1, -1, -1 }, { 0, -1,  0, -2 }, { 1, -1,  1, -1 }, 
	{ -1,  0, -2,  0 }, { 0,  0,  0,  0 }, { 1,  0,  2,  0 }, 
	{ -1,  1, -1,  1 }, { 0,  1,  0,  2 }, { 1,  1,  1,  1 }
};
static const float4 sobel3x3div4[9] = 
{
	{ -1, -1, -0.25, -0.25 }, { 0, -1,  0.00, -0.50 }, { 1, -1,  0.25, -0.25 },
	{ -1,  0, -0.50,  0.00 }, { 0,  0,  0.00,  0.00 }, { 1,  0,  0.50,  0.00 },
	{ -1,  1, -0.25,  0.25 }, { 0,  1,  0.00,  0.50 }, { 1,  1,  0.25,  0.25 }
};

inline void sobel3(sampler2D tex, float2 uv, int index, inout float3 h, inout float3 v)
{
	float4 gradient = sobel3x3div4[index];
	float3 rgb = smpl(tex, uv + (gradient * UV_SIZE)).rgb;
	h += rgb * gradient.z;
	v += rgb * gradient.w;
}
float4 fragSobel3(v2f_img i) : SV_Target
{
	sampler2D tex = _MainTex;
	float3 h = 0.0, v = 0.0;

	sobel3(tex, i.uv, 0, h, v);
	sobel3(tex, i.uv, 1, h, v);
	sobel3(tex, i.uv, 2, h, v);
	sobel3(tex, i.uv, 3, h, v);
	sobel3(tex, i.uv, 4, h, v);
	sobel3(tex, i.uv, 5, h, v);
	sobel3(tex, i.uv, 6, h, v);
	sobel3(tex, i.uv, 7, h, v);
	sobel3(tex, i.uv, 8, h, v);

	float2 sum = float2(h.x + h.y + h.z, v.x + v.y + v.z);
	float len = length(sum);
	float4 ret = float4(dot(h, h), dot(v, v), dot(h, v), len);

	return ret * _SobelCarryDigit; 
}

//////////////////////////////////////////////////////////////////////////////////////////////////
// Tangent Flow Map 
//////////////////////////////////////////////////////////////////////////////////////////////////
float4 fragTFM(v2f_img i) : SV_Target
{
	float4 sst = smpl(i.uv) * _SobelInvCarryDigit;
	float E = sst.x, F = sst.z, G = sst.y;

	// Structure Tensorの固有値
	float S = sqrt(((E - G) * (E - G)) + (F * F * 4.0));
	float2 eigenvalue = float2(E + G + S, E + G - S) * 0.5;

	// Structure Tensorの固有ベクトル
	// 勾配方向
	//float2 gradient = float2(F, eigenvalue.x - E);
	// 接線方向
	float2 tangent = float2(eigenvalue.x - E, -F);
	// 接線が全く取れない場合はダミーデータを入れる
	float2 invalid = step(dot(tangent, tangent), 0.0).xx;
	static const float2 dummy = float2(0.0, 1.0);
	tangent = lerp(tangent, dummy, invalid);
	tangent = normalize(tangent);
	//半回転分だけあればよいのでxは0.0～1.0、yは-1.0～1.0
	tangent = clamp(tangent, float2(0.0, -1.0), float2(1.0, 1.0));

	// 異方性
	float anisotropy = (eigenvalue.x - eigenvalue.y) / (eigenvalue.x + eigenvalue.y);
	anisotropy = max(anisotropy, 0.0);

	// 勾配の大きさ（＝接線と注目画素の距離）
	float len = smpl(_RT_SOBEL, i.uv).w * _SobelInvCarryDigit;
	float bluredLen = sst.w;

	static const float invLen = 1.0 / (length(float2(3.0, 3.0)) * 2.0);
	len = (len + bluredLen) * invLen;

	return float4(tangent.x, tangent.y, anisotropy, len);
}

// 異方性を楕円に見立てる。異方性が大きいほど長径を長く、短径を短くする
inline float2 getEclipseAxis(float anisotropy, float alpha = 1.0)
{
	float majorAxis = anisotropy + alpha;
	float minorAxis = alpha / (anisotropy + alpha);
	return float2(majorAxis, minorAxis);
}

void calcStep(float2 uv, float2 isGradient, inout float2 stepDir, inout float stepLen)
{
	float4 tfm = smpl(_RT_TFM, uv);

	// 1ステップ分の方向
	stepDir = tfm.xy;
	stepDir = lerp(stepDir, orthogonalize(stepDir), isGradient);
	//eclipseAxis = getEclipseAxis(tfm.z, 1.0);

	// 1ステップ分の距離
	// 近傍補間の際にエッジを跨いで平滑化されるのを避けるため、
	// 勾配方向xyのうち片方（大きい方）が画素中央ごとを指すように距離を計算する
	// 近隣画素との二点のみで補間されるようになり、エッジを跨いだ平滑化が防げる
	float2 absStepDir = abs(stepDir);
	// 方向と距離の積で丁度1画素分だけずらしたいので、距離は勾配の逆数にしておく
	// 方向で勾配を掛けているので、距離は勾配の逆数
	stepLen = 1.0 / max(absStepDir.x, absStepDir.y);
	// 1画素分の大きさにする
	stepDir *= UV_SIZE;
}

float2 getTangentStepLen(float2 uv, inout float4 tangent, inout float2 offset)
{
	tangent.xy = smpl(_RT_TFM, uv + offset).xy;
	tangent.xy *= sign(dot(tangent.xy, tangent.zw));

	// 接線に沿わせて距離と方向を都度計算する
	float2 stepDir = tangent.xy * UV_SIZE;

	// 少数点分を捨てて、画素の端から距離を計算する
	// floor(x)はx以下の最大の整数を返す
	// offsetが負のとき、例えば-0.9の場合は0.1を返す
	// paddingは-0.1になるので前回分の-0.9と合わせて-1.0
	// offsetが負なら画素は（端に詰めるのではなく）1つ分進む
	float padding = -(offset - floor(offset));
	// 1画素分進める（接線の大きさではなく符号。1.0か-1.0）
	float pixelUnit = sign(tangent.xy);
	// 画素中央を指したいので0.5だけ進める
	// 負の方向の場合、paddingで1画素分進んでいるため符号関係なく0.5でよい
	float pixelHalf = 0.5;
	// 方向（接線）と距離の積を定数距離にしたいので、距離は接線の逆数と掛けておく
	// （UV_SIZEが1画素分。方向で接線を掛けているので、距離は接線の逆数）
	float2 len = (padding + pixelUnit + pixelHalf) * (1.0 / tangent.xy);
	float2 absLen = abs(len);
	float2 absTangent = abs(tangent.xy);

	float2 stepLenX = float2(absLen.x, 0.0);
	float2 stepLenY = float2(0.0, absLen.y);
//	float2 stepLen = lerp(stepLenX, stepLenY, step(absTangent.x, absTangent.y).xx);
	float2 stepLen = (absTangent.x <= absTangent.y) ? stepLenY : stepLenX;
	offset += stepDir * (stepLen.x + stepLen.y);

	// 次のステップ用に古い接線情報をキャッシュ
	tangent.zw = tangent.xy;

	return stepLen;
}

// Gσ(x) = exp(-(x^2) / (2 * σ^2)) の -(x^2) と exp
// 指数に負値を渡して x が大きいほど重みを下げる
// ＝距離差、色差などが近い画素の間だけで平滑化する
inline float getGaussianWeight(float x, float variance) { return exp(-(x * x) * variance); }
inline float getGaussianWeight(float3 x, float variance) { return exp(-dot(x, x) * variance); }
inline float getGaussianWeight(float4 x, float variance) { return exp(-dot(x, x) * variance); }
inline float2 getGaussianWeight(float x, float2 variance) { return exp(-(x * x) * variance); }

// 注目画素と周辺画素の距離差の重み
inline float getDomainWeight(float totalLen, float variance, float bias)
{
	return getGaussianWeight(totalLen * bias, variance);
}
inline float2 getDomainWeight(float totalLen, float2 variance, float bias)
{
	return getGaussianWeight(totalLen * bias, variance);
}

//////////////////////////////////////////////////////////////////////////////////////////////////
// Line Integral Convolution
//////////////////////////////////////////////////////////////////////////////////////////////////
// TODO：深度を掛けた方がいい？
float _LICScale;
float _LICMaxLen;
float _LICInvSigmaSq2;

void convLIC(float2 uv, float4 tangent, inout float colorSum, inout float weightSum)
{
	float2 offset = 0.0;
	for (float totalLen = 0.0; totalLen < _LICMaxLen;)
	{
		float2 stepLen = getTangentStepLen(uv, tangent, offset);
		totalLen += stepLen.x + stepLen.y;

		float weight = getDomainWeight(totalLen, _LICInvSigmaSq2, 1.0);
		float color = step(rand(uv + offset, 0.0, _LICScale), 0.5);
		colorSum += color * weight;
		weightSum += weight;
	}
}
float4 fragLIC(v2f_img i) : SV_Target
{
	float4 tfm = smpl(_RT_TFM, i.uv);
	float4 tangent = 0.0;
	tangent.zw = tfm.xy * UV_SIZE;

	float colorSum = 0.0;
	float weightSum = 1.0;
	convLIC(i.uv, tangent, colorSum, weightSum);
	convLIC(i.uv, -tangent, colorSum, weightSum);

	return colorSum / weightSum;
}

//////////////////////////////////////////////////////////////////////////////////////////////
// Outline
//////////////////////////////////////////////////////////////////////////////////////////////
float _OutlineSize;
float _OutlineInvSize;
float _OutlineOpacity;
float _OutlineDetail;
float _OutlineDensity;
float _OutlineReverse;

float4 fragOutline(v2f_img i) : SV_Target
{
	float4x4 center = to4x4(i.uv);
	// 八近傍への接線と勾配
	float4x4 tangent = NEIGHBOR8_UNITVEC;
	float4x4 gradient = NEIGHBOR8_UNITVEC_ORTHO;

	float result = 0.0;
	for (int distance = 0; distance < _OutlineSize; ++distance)
	{
		float progress = distance * _OutlineInvSize;

		float4x4 x = distance * gradient;
		float4x4 y = distance * tangent * progress;
		float4x4 offset1 = y + x * _OutlineDensity;
		float4x4 offset2 = y - x * _OutlineDensity;

		float4x4 gradient1 = smplSobel(center, offset1 * TEX2UV4x4);
		float4x4 gradient2 = smplSobel(center, offset2 * TEX2UV4x4);
		gradient1 *= _SobelInvCarryDigit;
		gradient2 *= _SobelInvCarryDigit;

		// _RT_SOBELはRGBを内積しているのでなるべく近い値に戻す
		gradient1 = sqrt(gradient1 * 0.33333);
		gradient2 = sqrt(gradient2 * 0.33333);
		// 細部の描き込み。塗りの筆致を潰さない程度に下げる
		gradient1 *= _OutlineDetail;
		gradient2 *= _OutlineDetail;

		// 接線・勾配のコサイン類似度
		float4x4 tangentSimilarity = dot(gradient1, gradient2, tangent);
		float4x4 gradientSimilarity = dot(gradient1, gradient2, gradient);

		// 濃淡を付ける
		float4x4 lum = tangentSimilarity - abs(gradientSimilarity);

		// 注目画素から遠くにある画素ほど薄くする
		lum = clamp(lum, 0.0, _OutlineOpacity);
		lum *= 1.0 - progress;

		result += summation(lum);
	}
	// 確認用。白黒反転	
	result = lerp(result, 1.0 - result, step(1.0, _OutlineReverse));

	return float4(result, result, result, 1.0);
}

//////////////////////////////////////////////////////////////////////////////////////////////////
// Test
//////////////////////////////////////////////////////////////////////////////////////////////////
float _Test0, _Test1, _Test2, _Test3, _Test4, _Test5, _Test6, _Test7, _Test8, _Test9;

float4 fragTest(v2f_img i) : SV_Target
{
	//float4 color = smpl(i.uv);
	//return color;

	return debugOriginPos(i.uv);
	return rand(i.uv, 1234);
}

#endif