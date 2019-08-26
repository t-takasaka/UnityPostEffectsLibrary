#ifndef CANVAS_INCLUDED
#define CANVAS_INCLUDED

//////////////////////////////////////////////////////////////////////////////////////////////////
// Canvas Effect
//////////////////////////////////////////////////////////////////////////////////////////////////
float _RuledLineDensity;
float _RuledLineInvSize;
float4 _RuledLineRotMat;
float _WrinkleDensity;

// 別途RTを用意するコストを避けたいのでノイズの生成処理を分離
float2 genWrinkleTextureNoise(sampler2D rt, float2 uv, int index)
{
	float2 h = float2(UV_SIZE.x, 0.0);
	float2 v = float2(0.0, UV_SIZE.y);
	float4 noise1 = smpl(rt, uv + h);
	float4 noise2 = smpl(rt, uv - h);
	float4 noise3 = smpl(rt, uv + v);
	float4 noise4 = smpl(rt, uv - v);

	return float2(noise1[index] - noise2[index], noise3[index] - noise4[index]);
}

// 皴の付いた紙質を加える
float4 addWrinkleTexture(float2 uv, float2 noise, float4 color)
{
	float time = _Time.y;
	float2 strength = 100.0 * UV_SIZE;

	float2 range = strength * 2.0;
	float2 low = 0.5 - strength, high = 0.5 + strength;
	noise = (noise + 1.0) * 0.5; // 0.0～1.0
	noise = (clamp(noise, low, high) - low) / range;

	float2 c = cos(noise * PI), s = sin(noise * PI);
	float3 normalMap = float3(c.x, s.x + s.y, c.y);
	float3 lightDir = float3(cos(time), 0.5, sin(time));
	float bumpMap = dot(normalize(normalMap), normalize(lightDir));

	color.rgb = blendOverlay(color.rgb, bumpMap);
	return color;
}
// 罫線の紙質を加える
float4 addRuledLineTexture(float2 uv, float4 color)
{
	float2 coord = cos(_RuledLineInvSize * mul(_RuledLineRotMat, uv));
	float4 intensity = smoothstep(0.9, 1.0, max(coord.x, coord.y));
	float density = pigmentDensity(intensity, _RuledLineDensity);
	return colorModification(color, density);
}

//wip
float4 fragWrinkle(v2f_img i) : SV_Target
{
	float WrinkleSize = 10.0;

	// 大きさをUVサイズに合わせる
	float2 wrinkleSizeSize = WrinkleSize * UV_SIZE;

	// ランダム値の範囲を0.0～1.0から-1.0～1.0に変更
	float4 random = smpl(_RT_SNOISE, i.uv) * 2.0 - 1.0;
	float2 offset = wrinkleSizeSize * random.xy;
	// オフセットを加えてランダム値を再取得
	random = smpl(_RT_SNOISE, i.uv + offset) * 2.0 - 1.0;
	return dot(normalize(random.xy), normalize(offset));
}

#endif