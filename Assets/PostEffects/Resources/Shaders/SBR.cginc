#ifndef SBR_INCLUDED
#define SBR_INCLUDED

//////////////////////////////////////////////////////////////////////////////////////////////////
// Stroke Based Rendering
//////////////////////////////////////////////////////////////////////////////////////////////////
static const int SBR_LAYER_MAX = 10;

sampler2D _RT_SBR_HSV;
// レイヤ数
int _SBRLayerCount;
// レイヤ数の逆数
float _SBRInvLayerCount;
float _SBRLayerEnable[SBR_LAYER_MAX];
// サンプリングの半径
float _SBRRadius[SBR_LAYER_MAX];
// x:昇順のレイヤ進捗率。0.0～1.0
// y:降順のレイヤ番号。layerCount-1 ～ 0
// z:降順のレイヤ進捗率。1.0～0.0
float4 _SBRProgress[SBR_LAYER_MAX];
// 画素数と格子数を相互に変換する変数
// x:tex2grid.x, ytex2grid.y, z:grid2tex.x, w:grid2tex.w
float4 _SBRTex2Grid[SBR_LAYER_MAX];

// 閾値上限、下限
float _SBRDetailThresholdHigh[SBR_LAYER_MAX];
float _SBRDetailThresholdLow[SBR_LAYER_MAX];
// 筆致の幅、長さ、不透明度
float _SBRStrokeWidth[SBR_LAYER_MAX];
float _SBRStrokeLen[SBR_LAYER_MAX];
float _SBRStrokeOpacity[SBR_LAYER_MAX];
// 筆致の長さの揺れ
float _SBRStrokeLenRand[SBR_LAYER_MAX];
// 筆致の位置のかすれ、筆致のかすれの強さ
float4 _SBRScratchSize[SBR_LAYER_MAX];
float _SBRScratchOpacity[SBR_LAYER_MAX];
// 同系色とみなす許容範囲。塗りのはみ出しを防ぐ
float4 _SBRTolerance[SBR_LAYER_MAX];
// マスク
float _SBRMaskType[SBR_LAYER_MAX];
// カラーグレーディング
float4 _SBRAdd[SBR_LAYER_MAX], _SBRMul[SBR_LAYER_MAX];

float _SBRInvGridX[SBR_LAYER_MAX];
float _SBRInvGridY[SBR_LAYER_MAX];

float2 calcNeighborTexPosSBR(float2 texPos, int layer, int x, int y)
{
	// テクスチャ上の注目画素を格子上に割り当てる
	// 格子ごとに中央の画素一つだけを参照したいので、端数は切り捨てて0.5を足す
	float2 tex2grid = _SBRTex2Grid[layer].xy;
	float2 gridCenter = floor(texPos * tex2grid) + 0.5;

	// 近傍画素の位置（＝隣接する格子の中央）
	float2 gridNeighbor = gridCenter + float2(x, y);

	// 近傍画素の位置を格子上の位置からテクスチャ上の位置に戻す
	float2 grid2tex = _SBRTex2Grid[layer].zw;
	return gridNeighbor * grid2tex;
}
float validateDetailSBR(int layer, float detail)
{
	float detailThresholdHigh = _SBRDetailThresholdHigh[layer];
	float detailThresholdLow = _SBRDetailThresholdLow[layer];
	float thresholdLow = step(detailThresholdLow, detail);
	float thresholdHigh = step(detail, detailThresholdHigh);
	return thresholdLow * thresholdHigh;
}
float validateToleranceSBR(int layer, float4 hsv, float4 hsvNeighbor)
{
	float4 tolerance = _SBRTolerance[layer];
	float4 thresholdHSV = step(abs(hsvNeighbor - hsv), tolerance);
	return thresholdHSV.x * thresholdHSV.y * thresholdHSV.z * thresholdHSV.w;
}
float2 calcSimilaritySBR(float2 texPos, float2 texNeighbor, float2 tangent)
{
	// コサイン類似度の簡単化のため単位ベクトルにする
	tangent = normalize(tangent);
	// 勾配（接線の直交ベクトル）
	float2 gradient = orthogonalize(tangent);

	// 近傍画素から注目画素へのベクトル
	float2 orientation = texPos - texNeighbor;
	float tangentSimilarity = dot(orientation, tangent);
	float gradientSimilarity = dot(orientation, gradient);

	return float2(gradientSimilarity, tangentSimilarity);
}
float2 createStrokeSBR(int layer, float4 gridRand, float detail, float2 similarity)
{
	float2 strokeSize = float2(_SBRStrokeWidth[layer], _SBRStrokeLen[layer]);
	float strokeLenRand = _SBRStrokeLenRand[layer];

	float2 size = float2(1.0, 1.0);
	// 筆致のサイズの手振れ。細部は歪みを抑える
	float revDetail = 1.0 - detail;
	size.y += (gridRand.z - 0.5) * strokeLenRand * (revDetail * 10.0);

	// strokeが0.0～1.0の範囲内のときに描き込む
	// 筆のサイズ（＝分母）が増えたら描き込みも増やす
	size *= strokeSize;

	return similarity / size;
}
float2 addTrajectorySBR(float4 gridRand, float detail, float2 stroke)
{
	// 二次関数と三次関数を混ぜて若干曲げる
	float trajectory = (stroke.y * stroke.y) + (stroke.y * stroke.y * stroke.y * 2.0);
	// 細部は歪みを抑える
	float revDetail = 1.0 - detail;
	trajectory *= revDetail;
	// 曲げる方向を筆致ごとに変える
	stroke.x += trajectory * sign(gridRand.y - gridRand.w);

	return stroke;
}
float addShapeSBR(int layer, float2 stroke)
{
	float strokeOpacity = _SBRStrokeOpacity[layer];

	// 筆致の角を丸くするため、四隅の濃度を下げる
	float2 shape = stroke * (1.0 - stroke);
	float weight = shape.x * shape.y;
	// 筆致の先頭は後尾より若干細くする
	weight -= stroke.y * 0.05;
	weight = step(0.005, weight);

	return weight * strokeOpacity;
}
float addScratchSBR(int layer, float4 gridRand, float2 stroke, float weight)
{
	float2 scratchSize = _SBRScratchSize[layer].xy;
	float2 invScratchSize = _SBRScratchSize[layer].zw;
	float scratchOpacity = _SBRScratchOpacity[layer];

	// 筆致に擦れ相当のノイズを加える
	// 幅方向は同じ画素を参照させて線状の擦れを付ける
	float2 scratchPos = floor(stroke * scratchSize) * invScratchSize;
	// シードは格子ごとに変えて同じ筆致が出ないようにする
	float2 scratch = rand(scratchPos, gridRand.xz, 1.0);
	weight -= (1.0 - (scratch.x + scratch.y)) * weight * scratchOpacity;

	return weight;
}
float4 colorGradingSBR(int layer, float4 gridRand, float4 hsvNeighbor)
{
	float4 addition = _SBRAdd[layer];
	float4 multiplication = _SBRMul[layer];

	hsvNeighbor += addition;
	hsvNeighbor *= multiplication;
	return hsvNeighbor;
}

void convSBR(float2 texPos, float4 hsv, int layer, inout float4 colorSum)
{
	float layerProgress = _SBRProgress[layer].x;
	float revLayer = _SBRProgress[layer].y;
	float revLayerProgress = _SBRProgress[layer].z;

	int radius = (int)_SBRRadius[layer];
	for (int y = -radius; y <= radius; ++y)
	{
		for (int x = -radius; x <= radius; ++x)
		{
			// 近傍画素のテクスチャ上の位置
			float2 texNeighbor = calcNeighborTexPosSBR(texPos, layer, x, y);
			// 近傍画素のUV上の位置
			float2 uvNeighbor = texNeighbor * TEX2UV;

			float4 tfmNeighbor = smpl(_RT_TFM, uvNeighbor);
			// 近傍画素の接線
			float2 tangent = tfmNeighbor.xy;
			// 接線と注目画素の距離（＝描き込みの緻密さ）
			float detail = tfmNeighbor.w;

			// 描き込み度合いの閾値範囲外なら描かない
			if (validateDetailSBR(layer, detail) == 0.0) { continue; }

			// 近傍画素のHSV色
			float4 hsvNeighbor = smpl(_RT_SBR_HSV, uvNeighbor);

			// 塗り分けの閾値範囲外なら描かない
			if (validateToleranceSBR(layer, hsv, hsvNeighbor) == 0.0) { continue; }

			// 近傍画素から注目画素へのベクトルと、近傍画素自体の勾配および接線の類似度
			// 類似度が高いなら近傍画素の接線の延長線上付近に注目画素がある
			// 注目画素が遠くにある場合は内積の結果が大きくなるので閾値で切る
			float2 similarity = calcSimilaritySBR(texPos, texNeighbor, tangent);

			// 適当な素数から格子単位（≠画素単位）でのランダムな値を作る
			float4 gridRand = rand(uvNeighbor, RAND_SEED4, 1.0);

			// 類似度から幅と長さを決めて、長方形の筆致を作る
			float2 stroke = createStrokeSBR(layer, gridRand, detail, similarity);
			// 筆致を歪めて軌道を加える
			stroke = addTrajectorySBR(gridRand, detail, stroke);

			// 距離の遠い近傍画素なら描かない
			// strokeの平均は0.0付近。0.0～1.0に再配置する
			stroke = saturate(stroke + 0.5);

			// 筆致の四隅を丸めて形状を加える
			float weight = addShapeSBR(layer, stroke);
			// 筆致に擦れを加える
			weight = addScratchSBR(layer, gridRand, stroke, weight);
			weight = saturate(weight);

			// 色相、彩度、明度の補整
			hsvNeighbor = colorGradingSBR(layer, gridRand, hsvNeighbor);

			// HSVからRGBに戻す
			float4 rgbNeighbor = float4(hsv2rgb2(hsvNeighbor), 1.0);
			colorSum = lerp(colorSum, rgbNeighbor, weight);
		}
	}
}

float4 fragSBR(v2f_img i) : SV_Target
{
	// 注目画素の位置を0.0～1.0からテクスチャサイズに変換
	float2 texPos = i.uv * UV2TEX;

	float mask = smpl(_RT_MASK, i.uv);
	float4 hsv = smpl(_RT_SBR_HSV, i.uv);
	float4 colorSum = 1.0;

	// レイヤごとに筆のサイズを分けて塗り重ねる
	for (int layer = 0; layer < _SBRLayerCount; ++layer)
	{
		float maskType = _SBRMaskType[layer];
		float dontDraw = 0.0;

		// 非活性のレイヤは描かない
		dontDraw += 1.0 - _SBRLayerEnable[layer];
		// 非マスク領域且つマスクフラグなら描かない
		dontDraw += (1.0 - mask) * step(abs(maskType - 1.0), 0.0);
		// マスク領域且つ反転マスクフラグなら描かない
		dontDraw += mask * step(2.0, maskType);

		if (dontDraw >= 1.0) { continue; }

		// 畳み込み
		convSBR(texPos, hsv, layer, colorSum);
	}

	// デバッグ用のグリッド表示
	//float2 debugInvGridCount = float2(_SBRInvGridX[0], _SBRInvGridY[0]);
	//float2 grid = fmod(i.uv, debugInvGridCount);
	//if (grid.x < 0.003 || grid.y < 0.003) { colorSum = float4(1, 0, 0, 1); }
	//grid = fmod(i.uv, debugInvGridCount * 10.0);
	//if (grid.x < 0.003 || grid.y < 0.003) { colorSum = float4(0, 1, 0, 1); }

	return colorSum;
};

#endif