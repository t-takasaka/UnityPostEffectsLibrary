#ifndef AKF_INCLUDED
#define AKF_INCLUDED

//////////////////////////////////////////////////////////////////////////////////////////////////
// Anisotropic Kuwahara Filter
//////////////////////////////////////////////////////////////////////////////////////////////////
// 楕円の半径
float _AKFRadius, _AKFMaskRadius;
// 鮮鋭度合
float _AKFSharpness; 
// 分割領域の他領域との重複量
float _AKFOverlapX, _AKFOverlapY;
// フィルタのサンプルステップ
int _AKFSampleStep;
// 楕円の分割数。固定
static const int AKF_DIV_NUM = 8;

inline float2 getPosEvenAKF(float x, float y, float2x2 mat){ return mul(float2(x, y), mat); }
inline float2 getPosOddAKF(float x, float y){ return float2(x - y, x + y) * SQRT2 * 0.5; }
inline void sampleAKF(float2 uv, float x, float y, inout float4 color, inout float4 colorSq)
{
	float2 offset = float2(x, y) * UV_SIZE;
	color = smpl(_RT_ORIG, uv + offset);
	colorSq = color * color;
}
inline float4 initWeightAKF(float2 range, float2 pos)
{
	return float4(range.x + pos.y, range.y - pos.x, range.x - pos.y, range.y + pos.x); 
}
inline void calcWeightSubAKF(float2 range, float2 pos, inout float4 weight)
{
	// 切片内の画素は円中心からの距離を重みにする
	// 点対称で二点、半周逆側もをサンプルするので、分割数の半分だけずらした重みも用意する
	weight = initWeightAKF(range, pos);

	// 切片外の画素の重みは0にする
	weight = max(weight, 0.0);
	// 逆三角形の中心線に近い画素ほど大きい重みを付ける
	weight *= weight;
}
inline void calcWeightAKF(float2 pos, inout float4 weight[2])
{
	// 扇形の切片
	float2 range = _AKFOverlapX - (_AKFOverlapY * pos * pos);

	calcWeightSubAKF(range, pos, weight[0]);
	calcWeightSubAKF(range, -pos, weight[1]);
}
inline void sumWeightSubAKF(float4 color, float4 colorSq, float distWeight, inout float4 weight,
								inout float4x4 meanSum, inout float4x4 sqMeanSum)
{
	weight *= distWeight;
	meanSum += float4x4(color * weight.x, color * weight.y, color * weight.z, color * weight.w);
	sqMeanSum += float4x4(colorSq * weight.x, colorSq * weight.y, colorSq * weight.z, colorSq * weight.w);
}
inline void sumWeightAKF(float4 color[2], float4 colorSq[2], float distWeight, inout float4 weight[2],
								inout float4x4 meanSum, inout float4x4 sqMeanSum)
{
	sumWeightSubAKF(color[0], colorSq[0], distWeight, weight[0], meanSum, sqMeanSum);
	sumWeightSubAKF(color[1], colorSq[1], distWeight, weight[1], meanSum, sqMeanSum);
}
void convAKF(float2 uv, int2 boundingBox, float2x2 toUnitCircleMat, 
	inout float4 weightSum, inout float4x4 meanEvenSum, inout float4x4 sqMeanEvenSum,
	inout float4x4 meanOddSum, inout float4x4 sqMeanOddSum)
{
	// Unity側で保存直後に0が渡されることがある
	// 後段のfor文で無限ループになるため最低1を入れておく
	int sampleStep = max(1, _AKFSampleStep);

	// 処理量を半分にするため中心から点対称にサンプルするので、yは片側だけで足りる
	for (int y = 0; y <= boundingBox.y; y += sampleStep)
	{
		for (int x = -boundingBox.x; x <= boundingBox.x; x += sampleStep)
		{
			// 点対称にサンプルするので、中央横方向のxは片側だけで足りる
			if (step(y, 0) * step(x, -1) == 1.0) { continue; }

			// 偶数番目の分割領域に含まれる画素
			float2 posEven = getPosEvenAKF(x, y, toUnitCircleMat);
			float dist = length(posEven);

			// 単位円から外れている画素は処理しない
			if (dist > 1.0) { continue; }

			// 点対称に二点をサンプル
			float4 color[2], colorSq[2];
			sampleAKF(uv, x, y, color[0], colorSq[0]);
			sampleAKF(uv, -x, -y, color[1], colorSq[1]);

			// 奇数番目の分割領域に含まれる画素
			float2 posOdd = getPosOddAKF(posEven.x, posEven.y);

			// 切片内の画素は円中心からの距離を重みにする
			// 点対称で二点、半周逆側もをサンプルするので、分割数の半分だけずらした重みも用意する
			float4 weightEven[2], weightOdd[2];
			calcWeightAKF(posEven, weightEven);
			calcWeightAKF(posOdd, weightOdd);
			// 重みの合計
			float totalWeight = summation(weightEven[0]) + summation(weightOdd[0]);

			// 外周付近の重みを下げる
			float distWeight = exp(-PI * dist) / totalWeight;

			sumWeightAKF(color, colorSq, distWeight, weightEven, meanEvenSum, sqMeanEvenSum);
			sumWeightAKF(color, colorSq, distWeight, weightOdd, meanOddSum, sqMeanOddSum);
			weightSum.xy += weightEven[0].xy + weightEven[0].zw;
			weightSum.zw += weightOdd[0].xy + weightOdd[0].zw;
		}
	}
}
float2x2 getRotatedEllipseToUnitCircleMat(float2 eclipseAxes, float2 rotate)
{
	// 楕円を単位円に縮小する行列	
	float2 invEclipseAxes = 1.0 / eclipseAxes;
	float2x2 downScaleMat = { { invEclipseAxes.x, 0.0 }, { 0.0, invEclipseAxes.y } };
	// 楕円の傾きを打ち消す行列
	float2x2 reverseRotationMat = { {rotate.x, -rotate.y}, {rotate.y, rotate.x} };
	// 楕円を単位円に変換する行列
	return mul(reverseRotationMat, downScaleMat);
}
int2 getBoundingBoxAKF(float2 eclipseAxes, float2 rotate)
{
	// 楕円を囲むバウンディングボックスのサイズ
	// 楕円の式は x^2/a^2 + y^2/b^2 = 1
	// 傾いてるので (x*cos(phi)-y*sin(phi))^2/a^2 + (x*sin(phi)-y*cos(phi))^2/b^2 = 1
	// 水平方向の極値は qrt((x*cos(phi)-y*sin(phi))^2/a^2 + (x*sin(phi)-y*cos(phi))^2/b^2)
	// 垂直方向はcosとsinを入れ替える
	float2 eclipseAxesSq = eclipseAxes * eclipseAxes, rotateSq = rotate * rotate;
	return int2(sqrt(dot(eclipseAxesSq, rotateSq.xy)), sqrt(dot(eclipseAxesSq, rotateSq.yx)));
}
void mergeAKF(float4 weightSum, float4x4 meanEvenSum, float4x4 sqMeanEvenSum, 
				float4x4 meanOddSum, float4x4 sqMeanOddSum,
				inout float3 mean[AKF_DIV_NUM], inout float3 sqMean[AKF_DIV_NUM], 
				inout float weight[AKF_DIV_NUM])
{
	//[unroll] 
	for (int j = 0; j < 4; ++j)
	{
		int k = j * 2;
		mean[k] = meanEvenSum[j].xyz;
		mean[k + 1] = meanOddSum[j].xyz;
		sqMean[k] = sqMeanEvenSum[j].xyz;
		sqMean[k + 1] = sqMeanOddSum[j].xyz;
		int l = fmod(j, 2);
		weight[k] = weightSum[l];
		weight[k + 1] = weightSum[l + 2];
	}
}
float4 calcColorAKF(float weight[AKF_DIV_NUM], float3 mean[AKF_DIV_NUM], float3 sqMean[AKF_DIV_NUM])
{
	float3 colorSum = 0.0;
	float colorWeight = 0.0;

	//[unroll] 
	for (int j = 0; j < AKF_DIV_NUM; ++j)
	{
		float invWeight = 1.0 / weight[j];
		mean[j] *= invWeight;
		// 二乗の平均
		sqMean[j] *= invWeight;
		// 平均の二乗
		float3 meanSq = mean[j] * mean[j];

		// 分散の二乗 = 二乗の平均 - 平均の二乗
		sqMean[j] = abs(sqMean[j] - meanSq);
		// 分散
		float distribute = sqrt(sqMean[j].r + sqMean[j].g + sqMean[j].b);
		// 分散の大きい領域ほど重みを減らす
		float w = pow(distribute, -_AKFSharpness);

		// 平均色に重みを掛けて足し合わせる
		colorSum += float3(mean[j] * w);
		colorWeight += w;
	}

	// 平均色の総和を重みの総和で割る
	return float4(colorSum / colorWeight, 1.0);
}
float4 fragAKF(v2f_img i) : SV_Target
{
	float4 tfm = smpl(_RT_TFM, i.uv);
	float2 tangent = tfm.xy;
	float anisotropy = tfm.z;

	// マスク領域は半径を減らす（平滑化の度合いを下げる）
	float mask = smpl(_RT_MASK, i.uv);
	float radius = lerp(_AKFRadius, _AKFMaskRadius, mask);
	// 異方性が大きいほど離心率の高い楕円にする
	float2 eclipseAxes = getEclipseAxis(anisotropy, 1.0) * radius;
	// 接線方向を楕円の傾きにする
	float2 rotate = tangent;

	float2x2 toUnitCircleMat = getRotatedEllipseToUnitCircleMat(eclipseAxes, rotate);
	int2 boundingBox = getBoundingBoxAKF(eclipseAxes, rotate);

	// 分散は sqrt((二乗の平均)-(平均の二乗)) で求まる
	float4 weightSum = 0.0;
	float4x4 meanEvenSum = 0.0, sqMeanEvenSum = 0.0;
	float4x4 meanOddSum = 0.0, sqMeanOddSum = 0.0;

	convAKF(i.uv, boundingBox, toUnitCircleMat, weightSum, meanEvenSum, sqMeanEvenSum, meanOddSum, sqMeanOddSum);

	float weight[AKF_DIV_NUM];
	float3 mean[AKF_DIV_NUM], sqMean[AKF_DIV_NUM];
	mergeAKF(weightSum, meanEvenSum, sqMeanEvenSum, meanOddSum, sqMeanOddSum, mean, sqMean, weight);

	float4 colorSum = calcColorAKF(weight, mean, sqMean);
	return colorSum;
}

#endif