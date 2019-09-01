#ifndef WCR_INCLUDED
#define WCR_INCLUDED

//////////////////////////////////////////////////////////////////////////////////////////////////
// Watercolor Rendering
//////////////////////////////////////////////////////////////////////////////////////////////////
float _WCRBleeding;
float _WCROpacity;
float _WCRHandTremorLen;
float _WCRHandTremorScale;
float _WCRHandTremorDrawCount;
float _WCRHandTremorInvDrawCount;
float _WCRHandTremorOverlapCount;
float _WCRPigmentDispersionScale;
float _WCRTurbulenceFowScale1, _WCRTurbulenceFowScale2;
float _WetInWetLenRatio, _WetInWetInvLenRatio;
float _WetInWetLow, _WetInWetHigh;
float _WetInWetDarkToLight;
float _WetInWetHueSimilarity;
float _EdgeDarkingLenRatio, _EdgeDarkingInvLenRatio;
float _EdgeDarkingSize;
float _EdgeDarkingScale;

// 手振れを加える
float4 addHandTremor(float2 uv, float index, float4 color)
{
	float invDivNum = _WCRHandTremorInvDrawCount;
	float num = invDivNum * index;
	float4 noise1 = smpl(_RT_WORK6, frac(uv + num));
	float4 noise2 = smpl(_RT_WORK6, frac(1.0 - (uv + num)));
	// ランダム値の範囲を0.0～1.0から-1.0～1.0に変更
	float2 offset1 = float2(noise1.x, noise2.x) * 2.0 - 1.0;
	float2 offset2 = float2(noise1.y, noise2.y) * 2.0 - 1.0;
	// 筆致全体の大きな歪みと、線の細かな手振れを足し合わせる
	float2 offset = offset1 + offset2;
	// 手振れの大きさをUVサイズに合わせる
	offset *= _WCRHandTremorLen * UV_SIZE;

	// 近傍画素がマスク領域なら歪ませない
	float mask = smpl(_RT_MASK, uv + offset);
	// retではなく注目画素を返す（retは真白の場合があるため）
	if (mask == 1.0) { return smpl(uv); }

	float4 colorNeighbor = smpl(uv + offset);
	float3 hsvNeighbor = rgb2hsv(colorNeighbor.rgb);
	float lumNeighbor = hsvNeighbor.z;

	// アルゴリズムの性質で真白の上に塗り重ねることができないので、
	// その場合はサンプルした色をそのまま塗る
	float isWhite = step(1.0, rgb2hsv(color).z);

	// 領域の端に塗りが重なる部分を作るため、諧調ごとに区分する
	float overlapCount = _WCRHandTremorOverlapCount;
	float overlapCountLow = (ceil(overlapCount * 0.5) - 1) * invDivNum;
	float overlapCountHigh = (floor(overlapCount * 0.5) + 1) * invDivNum;
	float low = step(max(0.0, num - overlapCountLow), lumNeighbor);
	float high = step(lumNeighbor, min(1.0, num + overlapCountHigh));
	// 区分の範囲外なら処理しない
	// TODO：注目画素が真黒のときに塗り漏れが起きる？？？
	//if (low * high == 0.0) { return lerp(color, colorNeighbor, isWhite); }
	if (low * high == 0.0) { return color; }

	float4 intensity = abs(colorNeighbor - color);
	float4 mixed = colorModification(intensity, color, _WCRHandTremorScale);
	return lerp(mixed, colorNeighbor, isWhite);
}
// 顔料の偏りを加える
float4 addTurbulenceFow(float2 uv, float4 color)
{
	float2 noise = smpl(_RT_WORK6, uv).zw;
	float3 hsv = rgb2hsv(color.rgb);

	// 彩度が0.0だと真黒になるのでEPSILONとmaxを取る
	noise.x = max(EPSILON, frac(noise.x));
	hsv.y = noise.x;
	float3 intensity = abs(hsv2rgb(hsv) - color.rgb);
	color.rgb = colorModification(intensity, color.rgb, _WCRTurbulenceFowScale1);

	return color;
}

float4 fragHandTremor(v2f_img i) : SV_Target
{
	// 注目画素の色ではなく真白から塗り重ねる
	float4 ret = 1.0;

	for (float index = 0.0; index < _WCRHandTremorDrawCount; index += 1.0)
	{
		ret = addHandTremor(i.uv, index, ret);
	}

	// 注目画素がマスク領域なら歪ませない
	//float mask = smpl(_RT_MASK, i.uv);
	//ret = lerp(ret, smpl(i.uv), mask);

	// 顔料の偏りを加える
	ret = addTurbulenceFow(i.uv, ret);

	return ret;
}

// 近傍画素が境界線上にあるかの判定
float isEdgeWCR(float2 uv)
{
	static const float threshold = 0.05;
	return step(threshold, smpl(_RT_SOBEL, uv).w * _SobelInvCarryDigit);
}
// 近傍画素が境界線の反対側にあるかの判定
// edge.x:境界線上か, edge.y:境界線に達したことがあるか, edge.z:境界線の反対側までの距離
void isEdgeWCR(float2 uv, float stepLen, float2 stepDir, inout float2 offset, inout float3 edge)
{
	float maxLen = _WCRBleeding;
	float invMaxLen = 1.0 / maxLen;
	for (float totalLen = stepLen; totalLen <= maxLen; totalLen += stepLen)
	{
		offset = totalLen * stepDir;

		edge.x = isEdgeWCR(uv + offset);
		float isNotEdge = 1.0 - edge.x;
		float isNotReach = 1.0 - edge.y;
		// 境界線に達していない
		if (isNotEdge * isNotReach == 1.0) { continue; }

		edge.y = 1.0;
		// 境界線を越えていない
		if (edge.x == 1.0) { continue; }

		edge.z = (maxLen - totalLen) * invMaxLen;
		return;
	}
}

void hueDistanceWCR(float2 uv, float4 color, inout float hueDist, inout float isDark)
{
	float4 colorNeighbor = smpl(uv);
	// hueの距離を知りたいので極座標系のHSVに変換する
	float4 hsvNeighbor = rgb2hsv2(colorNeighbor.rgb);
	float4 hsv = rgb2hsv2(color.rgb);

	// 色相環360度のうち注目画素と近傍画素の距離
	hueDist = hueDistance(hsvNeighbor, hsv);
	// 近傍画素が注目画素より暗いか
	isDark = step(hsvNeighbor.w - hsv.w, 0.0);
}

// 境界線付近を滲ませる
float convWetInWet(float2 uv, float dist, float hueDist, float isDark, inout float4 color)
{
	// 近傍画素のオフセット込みのUVでランダム値をサンプルする
	float noise1 = smpl(_RT_WORK7, uv.xy).x + smpl(_RT_WORK7, 1.0 - uv.xy).x;
	float noise2 = smpl(_RT_WORK7, uv.yx).x + smpl(_RT_WORK7, 1.0 - uv.yx).x;
	// ランダム値の範囲を0.0～1.0から-1.0～1.0に変更
	float2 noise = float2(noise1, noise2);
	float4 colorNeighbor = smpl(uv);
	float3 hsvNeighbor = rgb2hsv(colorNeighbor);

	// 注目画素が滲みの届く範囲内にあるか
	dist = max(0.0, dist - _WetInWetLenRatio) * _WetInWetInvLenRatio;
	float spike = noise * dist;
	// 注目画素と近傍画素が色相環で範囲内（degree指定）にあるか
	float withinHueRange = step(hueDist, _WetInWetHueSimilarity);
	// 暗い箇所から明るい箇所に滲むか、明るい箇所から暗い箇所へ滲むか
	float darkOrLight = lerp(1.0 - isDark, isDark, _WetInWetDarkToLight);
	// 顔や指先などの細部を滲ませたくないケースが想定されるため
	// 近傍画素の輝度が閾値の上限、下限の範囲外なら滲ませない
	// 閾値が目立たないように上限下限の両端を補間する
	float low = _WetInWetLow, high = _WetInWetHigh;
	float thresholdLow = smoothstep(low, low + 0.1, hsvNeighbor.z);
	float thresholdHigh = 1.0 - smoothstep(high - 0.1, high, hsvNeighbor.z);
	float threshold = thresholdLow * thresholdHigh;

	float weight = spike * withinHueRange * darkOrLight * threshold;
	color = lerp(color, colorNeighbor, weight);

	// 他のエフェクトと領域を被らせないためのフラグ
	// ※境界線を跨いだ反対側の領域も加工されたくないので色相のみで判別する
	return withinHueRange * threshold;
}
float4 addWetInWet(float2 uv, float2 stepLen, float2 stepDir, float4 color, 
					inout float dist, inout float isWetInWet)
{
	// 境界線を越えた対岸側のオフセット
	float2 offset = 0.0;
	// オフセットまでの距離。0.0～1.0
	float3 edge = 0.0;

	isEdgeWCR(uv, stepLen, stepDir, offset, edge);
	dist += edge.z;

	if (edge.z > 0.0)
	{
		float2 uvNeighbor = uv + offset;

		// 近傍画素がマスク領域なら滲ませない
		float mask = smpl(_RT_MASK, uvNeighbor);
		if (mask == 1.0) { return color; }

		float hueDist = 0.0, isDark = 0.0;
		hueDistanceWCR(uvNeighbor, color, hueDist, isDark);
		isWetInWet += convWetInWet(uvNeighbor, edge.z, hueDist, isDark, color);
	}

	return color;
}

// 境界線の色を濃くする
float4 addEdgeDarking(float2 uv, float2 stepDir, float4 color, float dist, float isWetInWet)
{
	float isNotEdge = 1.0 - isEdgeWCR(uv);
	if (isNotEdge + isWetInWet >= 1.0) { return color; }

	float3 color1 = smpl(uv + stepDir).rgb;
	float3 color2 = smpl(uv - stepDir).rgb;
	float sum0 = color.r + color.g + color.b;
	float sum1 = color1.r + color1.g + color1.b;
	float sum2 = color2.r + color2.g + color2.b;

	// 塗り漏れの周囲は処理しない
	// TODO：判定が甘い。要検証
	const static float whiteThreshold = 2.9;
	if (max(sum0, max(sum1, sum2)) >= whiteThreshold){ return (color + 1.0) * 0.5; }

	float3 intensity = abs(color1 - color2);
	color.rgb = colorModification(intensity, color.rgb, _EdgeDarkingScale);

	// 境界線付近も若干濃くする（1.0超になり得るがsaturateはしない）
	color.rgb = colorModification(dist, color.rgb, _EdgeDarkingSize);

	return color;
}
// 顔料の散らばりを加える
float4 addPigmentDispersion(float2 uv, float4 color)
{
	//float noise1 = genGNoise4(uv, 1.0);
	float noise2 = genGNoise4(uv, 2.0);
	float noise3 = genGNoise4(uv, 3.0);
	float noise4 = genGNoise4(uv, 4.0);
	float noise = (noise2 + noise3 + noise4);

	float3 hsl = rgb2hsl(color.rgb);
	hsl.y = max(EPSILON, frac(hsl.y + noise));
	float3 intensity = abs(hsl2rgb(hsl) - color.rgb);
	// 明度が低いか彩度が高い箇所に偏らせる
	intensity *= smoothstep(0.0, 1.0, max(hsl.y, (1.0 - hsl.z)));
	color.rgb = colorModification(intensity, color.rgb, _WCRPigmentDispersionScale);

	return color;
}

float4 fragWCR(v2f_img i) : SV_Target
{
	float4 color = smpl(i.uv);
	color = lerp(1.0.xxxx, color, _WCROpacity);
	float4 ret = color;
	float isWetInWet = 0.0;
	float dist = 0.0;

	float4 tfm = smpl(_RT_TFM, i.uv);
	float2 stepDir = tfm.xy;
	stepDir = orthogonalize(stepDir);

	// マスク領域は滲ませない
	float2 absStepDir = abs(stepDir);
	float stepLen = 1.0 / max(absStepDir.x, absStepDir.y);
	stepDir *= UV_SIZE;

	// 反対方向に二個所をサンプルする
	ret = addWetInWet(i.uv, stepLen, stepDir, ret, dist, isWetInWet);
	ret = addWetInWet(i.uv, stepLen, -stepDir, ret, dist, isWetInWet);

	// 境界線を濃くする
	ret = addEdgeDarking(i.uv, stepDir, ret, dist, isWetInWet);

	// 顔料の散らばりを加える
	ret = addPigmentDispersion(i.uv, ret);

	// 紙の質感を加える
	float2 wrinkle = genWrinkleTextureNoise(_RT_WORK7, i.uv, 3);
	ret = addWrinkleTexture(i.uv, wrinkle, ret);

	return ret;
}

#endif