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
	float4 center = smpl(i.uv);
	float4 colorSum = 0.0;

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
			float4 neighbor1 = smpl(i.uv + offset);
			float4 neighbor2 = smpl(i.uv - offset);
			float4 distance1 = hsvDistance(center, neighbor1);
			float4 distance2 = hsvDistance(center, neighbor2);
			float4 color = lerp(neighbor1, neighbor2, step(distance2, distance1));
			colorSum += color * 2.0;
		}
		offset.y += UV_SIZE.y;
	}

	return colorSum / _SNNWeight;
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
// Unsharp Mask
//////////////////////////////////////////////////////////////////////////////////////////////////
int _UnsharpMaskLOD;
int _UnsharpMaskTileSize;
int _UnsharpMaskSize;
float _UnsharpMaskInvDomainSigma;
float _UnsharpMaskDomainVariance;
float _UnsharpMaskDomainBias;
float _UnsharpMaskMean;
float _UnsharpMaskSharpness;

float unsharpMask(float2 uv)
{
	float lumSum = 0.0;
	float weightSum = 0.0;
	for (int y = 0; y < _UnsharpMaskSize; ++y) {
		for (int x = 0; x < _UnsharpMaskSize; ++x) {
			float2 offset = float2(x, y) * float(_UnsharpMaskTileSize) - _UnsharpMaskMean;
			float2 domain = offset * _UnsharpMaskInvDomainSigma * _UnsharpMaskDomainBias;
			float domainWeight = exp(-0.5 * dot(domain, domain)) * _UnsharpMaskDomainVariance;

			float lumNeighbor = rgb2lum(smpl(uv + offset * UV_SIZE, _UnsharpMaskLOD).rgb);
			lumSum += lumNeighbor * domainWeight;
			weightSum += domainWeight;
		}
	}
	float neighbor = lumSum / weightSum;
	float center = rgb2lum(smpl(uv, _UnsharpMaskLOD).rgb);
	float unsharp = (center - neighbor) * _UnsharpMaskSharpness;

	return saturate(unsharp);
}

float4 fragUnsharpMask(v2f_img i) : SV_Target{
	return unsharpMask(i.uv); 
}

#endif