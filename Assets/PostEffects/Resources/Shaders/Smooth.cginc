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
	float4 hsv = posterizeHSV(rgb2hsv(color.rgb));
	return lerp(float4(hsv2rgb(hsv), 1.0), hsv, _PosterizeReturnHSV);
}

//////////////////////////////////////////////////////////////////////////////////////////////////
// Symmetric Nearest Neighbor
//////////////////////////////////////////////////////////////////////////////////////////////////
int _SNNRadius;

float4 convSNN(float4 c0, float2 uv, float2 offset)
{
	float4 c1 = smpl(uv + offset);
	float4 c2 = smpl(uv - offset);
	float4 d1 = hsvDistance(c0, c1);
	float4 d2 = hsvDistance(c0, c2);
	return lerp(c1, c2, step(d2, d1));
}
float4 fragSNN(v2f_img i) : SV_Target
{
	int radius = _SNNRadius;
	float4 center = smpl(i.uv);
	float4 colorSum = 0.0;

	float2 offset = -radius * UV_SIZE;
	// 中心から点対称に二点を取得して、中心に近い点を採用する
	// 点対称に取得するのでyは片側だけで足りる
	for (int y = 0; y <= radius; ++y)
	{
		offset.x = -radius * UV_SIZE.x;

		for (int x = -radius; x <= radius; ++x)
		{
			if (step(y, 0) * step(x, 0) == 1.0) { continue; }

			offset.x += UV_SIZE.x;
			colorSum += convSNN(center, i.uv, offset) * 2.0;
		}
		offset.y += UV_SIZE.y;
	}

	float weightSum = (radius * (radius * 2 + 1) + radius) * 2.0;
	colorSum /= weightSum;

	return colorSum;
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


#endif