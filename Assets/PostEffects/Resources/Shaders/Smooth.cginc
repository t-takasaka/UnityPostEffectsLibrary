#ifndef SMOOTH_INCLUDED
#define SMOOTH_INCLUDED

//////////////////////////////////////////////////////////////////////////////////////////////////
// Posterize
//////////////////////////////////////////////////////////////////////////////////////////////////
float _PosterizeBins;
float _PosterizeInvBins;
float _PosterizeReturnHSV;

float3 posterizeLAB(float3 lab)
{
	lab.x = floor(lab.x * _PosterizeBins + 0.5) * _PosterizeInvBins;
	return lab;
}
float4 posterizeHSV(float4 hsv)
{
	hsv.w = floor(hsv.w * _PosterizeBins + 0.5) * _PosterizeInvBins;
	return hsv;
}
float4 fragPosterize(v2f_img i) : SV_Target
{
	float4 color = smpl(i.uv);
	float3 lab = posterizeLAB(rgb2lab(color.rgb));
	color.rgb = lab2rgb(lab);

	return lerp(color, rgb2hsv2(color.rgb), _PosterizeReturnHSV);
}

//////////////////////////////////////////////////////////////////////////////////////////////////
// Symmetric Nearest Neighbor
//////////////////////////////////////////////////////////////////////////////////////////////////
int _SNNRadius;
float _SNNWeight;

float4 fragSNN(v2f_img i) : SV_Target
{
	// 前段でHSV空間にしている
	float4 hsvCenter = smpl(i.uv);
	float4 hsvSum = 0.0;

	float2 offset = -_SNNRadius * UV_SIZE;
	// 中心から点対称に二点を取得して、中心に近い点を採用する
	// 点対称に取得するのでyは片側だけで足りる
	for (int y = 0; y <= _SNNRadius; ++y)
	{
		offset.x = -_SNNRadius * UV_SIZE.x;

		for (int x = -_SNNRadius; x <= _SNNRadius; ++x)
		{
			if (step(y, 0) * step(x, 0) == 1.0) { continue; }

			offset.x += UV_SIZE.x;
			float4 hsvNeighbor1 = smpl(i.uv + offset);
			float4 hsvNeighbor2 = smpl(i.uv - offset);
			float4 distance1 = hsvDistance(hsvCenter, hsvNeighbor1);
			float4 distance2 = hsvDistance(hsvCenter, hsvNeighbor2);
			float4 hsv = lerp(hsvNeighbor1, hsvNeighbor2, step(distance2, distance1));
			hsvSum += hsv * 2.0;
		}
		offset.y += UV_SIZE.y;
	}

	return float4(hsv2rgb2(hsvSum / _SNNWeight), 1.0);
}

//////////////////////////////////////////////////////////////////////////////////////////////////
// Gaussian Blur
//////////////////////////////////////////////////////////////////////////////////////////////////
int _GBlurLOD;
int _GBlurTileSize;
int _GBlurSize;
float _GBlurInvDomainSigma;
float _GBlurDomainVariance;
float _GBlurDomainBias;
float _GBlurMean;
float _GBlurOffsetX[256];
float _GBlurOffsetY[256];
float _GBlurDomainWeight[256];

// 補間済みの縮小バッファを参照してサンプル数を減らし、高速化する
// フラグメントシェーダの起動コストと比較して十分速いのでセパラブルにはしない
float4 fragGBlur(v2f_img i) : SV_Target
{
	float4 colorSum = 0.0;
	for (int y = 0; y < _GBlurSize; ++y) {
		for (int x = 0; x < _GBlurSize; ++x) {
			float2 offset = float2(x, y) * float(_GBlurTileSize) - _GBlurMean;
			float2 domain = offset * _GBlurInvDomainSigma * _GBlurDomainBias;
			float domainWeight = exp(-0.5 * dot(domain, domain)) * _GBlurDomainVariance;

			float4 colorNeighbor = smpl(i.uv + offset * UV_SIZE, _GBlurLOD);
			colorSum += colorNeighbor * domainWeight;
		}
	}
	return colorSum / colorSum.a;
}

// 事前に計算しておくパターン
// LODが低くSmpleLenが多い場合は配列外アクセスが発生するので注意
float4 fragGBlur2(v2f_img i) : SV_Target
{
	float4 colorSum = 0.0;
	int size = _GBlurSize * _GBlurSize;
	for (int index = 0; index < size; ++index) {
		float2 offset = float2(_GBlurOffsetX[index], _GBlurOffsetY[index]);
		float domainWeight = _GBlurDomainWeight[index];
		float4 colorNeighbor = smpl(i.uv + offset * UV_SIZE, _GBlurLOD);
		colorSum += colorNeighbor * domainWeight;
	}
	return colorSum / colorSum.a;
}

//////////////////////////////////////////////////////////////////////////////////////////////////
// Sharpen
//////////////////////////////////////////////////////////////////////////////////////////////////
int _SharpenLOD;
int _SharpenTileSize;
int _SharpenSize;
float _SharpenInvDomainSigma;
float _SharpenDomainVariance;
float _SharpenDomainBias;
float _SharpenMean;
float _SharpenSharpness;

float unsharpBlur(float2 uv)
{
	float lumSum = 0.0;
	float weightSum = 0.0;
	for (int y = 0; y < _SharpenSize; ++y) {
		for (int x = 0; x < _SharpenSize; ++x) {
			float2 offset = float2(x, y) * float(_SharpenTileSize) - _SharpenMean;
			float2 domain = offset * _SharpenInvDomainSigma * _SharpenDomainBias;
			float domainWeight = exp(-0.5 * dot(domain, domain)) * _SharpenDomainVariance;

			float lumNeighbor = smplLum(uv + offset * UV_SIZE, _SharpenLOD);
			lumSum += lumNeighbor * domainWeight;
			weightSum += domainWeight;
		}
	}

	return lumSum / weightSum;
}
float4 fragSharpen(v2f_img i) : SV_Target{
	float neighbor = unsharpBlur(i.uv);
	float4 center = smpl(i.uv, _SharpenLOD);
	float4 color = center + (center - neighbor) * _SharpenSharpness;

	return saturate(color);
}

float unsharpMask(float2 uv)
{
	float neighbor = unsharpBlur(uv);
	float center = smplLum(uv, _SharpenLOD);
	float unsharp = (center - neighbor) * _SharpenSharpness;

	return saturate(unsharp);
}
float4 fragUnsharpMask(v2f_img i) : SV_Target{ return unsharpMask(i.uv); }

#endif