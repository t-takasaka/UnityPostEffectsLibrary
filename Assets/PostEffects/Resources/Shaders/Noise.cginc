#ifndef NOISE_INCLUDED
#define NOISE_INCLUDED

//////////////////////////////////////////////////////////////////////////////////////////////////
// White Noise
//////////////////////////////////////////////////////////////////////////////////////////////////
static const float RAND_SEED = 79;
static const float2 RAND_SEED2 = float2(1187, 1543);
static const float3 RAND_SEED3 = float3(1259, 1459, 2293);
static const float4 RAND_SEED4 = float4(607, 1039, 2609, 3571);
static const float4 RAND_SEED8[2] = { 929, 1051, 1279, 1409, 1693, 2029, 2309, 3299 };
static const float2 RAND_DOT = float2(12.9898, 78.233);
static const float RAND_FRAC = 43758.5453;

inline float randHelper(float2 uv, float scale)
{
	float2 pos = floor(uv * TEX_SIZE * (1.0 / scale));
	pos *= UV_SIZE * scale;
	return dot(pos, RAND_DOT);
}
// 0.0～1.0の一様分布
// 1画素ごとにランダム値を振りたい場合はscaleは1。隣接する2画素ごとなら2
inline float rand(float2 uv, float seed, float scale = 1.0)
{ 
	return frac(sin(randHelper(uv, scale) + seed) * RAND_FRAC);
}
inline float2 rand(float2 uv, float2 seed, float scale = 1.0) 
{ 
	return frac(sin(randHelper(uv, scale) + seed) * RAND_FRAC);
}
inline float3 rand(float2 uv, float3 seed, float scale = 1.0) { 
	return frac(sin(randHelper(uv, scale) + seed) * RAND_FRAC);
}
inline float4 rand(float2 uv, float4 seed, float scale = 1.0) { 
	return frac(sin(randHelper(uv, scale) + seed) * RAND_FRAC);
}

//////////////////////////////////////////////////////////////////////////////////////////////////
// Gaussian Noise
//////////////////////////////////////////////////////////////////////////////////////////////////
float genGNoise2(float2 uv, float scale)
{
	float t = 1.0;// _Time.y;
	float r0 = rand(uv, RAND_SEED8[0].xy * t, scale);
	float r1 = rand(uv, RAND_SEED8[0].yz * t, scale);
	return (r0 + r1) * 0.5;
}
float genGNoise4(float2 uv, float scale)
{
	float t = 1.0;// _Time.y;
	float r0 = rand(uv, RAND_SEED8[0].xy * t, scale);
	float r1 = rand(uv, RAND_SEED8[0].yz * t, scale);
	float r2 = rand(uv, RAND_SEED8[0].zw * t, scale);
	float r3 = rand(uv, RAND_SEED8[0].wx * t, scale);
	return (r0 + r1 + r2 + r3) * 0.25;
}
float genGNoise8(float2 uv, float scale)
{
	float t = 1.0;// _Time.y;
	float r0 = rand(uv, RAND_SEED8[0].xy * t, scale);
	float r1 = rand(uv, RAND_SEED8[0].yz * t, scale);
	float r2 = rand(uv, RAND_SEED8[0].zw * t, scale);
	float r3 = rand(uv, RAND_SEED8[0].wx * t, scale);
	float r4 = rand(uv, RAND_SEED8[1].xy * t, scale);
	float r5 = rand(uv, RAND_SEED8[1].yz * t, scale);
	float r6 = rand(uv, RAND_SEED8[1].zw * t, scale);
	float r7 = rand(uv, RAND_SEED8[1].wx * t, scale);
	return (r0 + r1 + r2 + r3 + r4 + r5 + r6 + r7) * 0.125;
}
float4 fragGNoise(v2f_img i) : SV_Target
{
	float4 noise;
	noise.x = genGNoise8(i.uv, 1.0);
	noise.y = genGNoise8(i.uv, 1.0);
	noise.z = genGNoise8(i.uv, 1.0);
	noise.y = genGNoise8(i.uv, 1.0);
	return noise;
}

//////////////////////////////////////////////////////////////////////////////////////////////////
// Simplex Noise
//////////////////////////////////////////////////////////////////////////////////////////////////
static const float SNOIZE_F3 = 1.0 / 3.0;
static const float SNOIZE_G3 = 1.0 / 6.0;
float4 _SNOIZE_SIZE;
float4 _SNOIZE_SCALE;
float4 _SNOIZE_SPEED;

float3 gradSNoise(float3 pos)
{
	pos = frac(pos * RAND_SEED3 * 0.01);
	pos += dot(pos, pos.yxz + RAND_SEED);
	pos = frac((pos.xzy + pos.yxz) * pos.zyx);
	return (pos - 0.5) * 2.0;
}
float contribSNoise(float3 pos, float3 offset, float bias, float3 skew)
{
	float3 p = pos - offset + SNOIZE_G3 * bias;
	float3 g = gradSNoise(skew + offset);
	float weight = max(0.0, 0.6 - dot(p, p));
	// 曲線のつなぎ目が目立たないように補間
	return dot(p, g) * weight * weight * weight * weight;
}
float genSNoise(float3 pos, float scale)
{
	// 正方形を斜めに分割した二等辺三角形を正三角形に歪める
	float3 skewFactor = (pos.x + pos.y + pos.z) * SNOIZE_F3;
	float3 skew = floor(pos + skewFactor);
	float3 unskewFactor = (skew.x + skew.y + skew.z) * SNOIZE_G3;
	float3 unskew = skew - unskewFactor;

	float3 dist1 = pos - unskew;
	float3 dist2 = step(0.0, dist1.xyz - dist1.yzx);

	// 三角錐の各頂点オフセット
	float3 offset1 = 0.0;
	float3 offset2 = 0.0 + dist2.xyz * (1.0 - dist2.zxy);
	float3 offset3 = 1.0 - dist2.zxy * (1.0 - dist2.xyz);
	float3 offset4 = 1.0;

	// 各頂点の影響量
	float c1 = contribSNoise(dist1, offset1, 0.0, skew);
	float c2 = contribSNoise(dist1, offset2, 1.0, skew);
	float c3 = contribSNoise(dist1, offset3, 2.0, skew);
	float c4 = contribSNoise(dist1, offset4, 3.0, skew);

	return (c1 + c2 + c3 + c4) * scale;
}
// TODO：要高速化。genSNoiseが4個の精度で済む呼び出し元の洗い出しと差し替え
float fBmSNoise(float2 uv, float seed, float size, float scale, float speed)
{
	// アスペクト比調整
	uv.x *= TEX_SIZE.x / TEX_SIZE.y;
	float3 pos = float3(uv, _Time.x * speed) * size + seed;

	static const float4 frequency1 = float4(1 << 0, 1 << 1, 1 << 2, 1 << 3);
	static const float4 amplitude1 = 1.0 / frequency1;
	float4 noise1;
	noise1.x = genSNoise(pos * frequency1.x, scale);
	noise1.y = genSNoise(pos * frequency1.y, scale);
	noise1.z = genSNoise(pos * frequency1.z, scale);
	noise1.w = genSNoise(pos * frequency1.w, scale);
	//return 1.0 + dot(amplitude1, noise1);

	static const float4 frequency2 = float4(1<<4, 1<<5, 1<<6, 1<<7);
	static const float4 amplitude2 = 1.0 / frequency2;
	float4 noise2;
	noise2.x = genSNoise(pos * frequency2.x, scale);
	noise2.y = genSNoise(pos * frequency2.y, scale);
	noise2.z = genSNoise(pos * frequency2.z, scale);
	noise2.w = genSNoise(pos * frequency2.w, scale);
	return (1.0 + dot(amplitude1, noise1) + dot(amplitude2, noise2)) * 0.5;
}
float4 fragSNoise(v2f_img i) : SV_Target
{
	float x = fBmSNoise(i.uv, RAND_SEED4.x, _SNOIZE_SIZE.x, _SNOIZE_SCALE.x, _SNOIZE_SPEED.x);
	float y = fBmSNoise(i.uv, RAND_SEED4.y, _SNOIZE_SIZE.y, _SNOIZE_SCALE.y, _SNOIZE_SPEED.y);
	float z = fBmSNoise(i.uv, RAND_SEED4.w, _SNOIZE_SIZE.z, _SNOIZE_SCALE.z, _SNOIZE_SPEED.z);
	float w = fBmSNoise(i.uv, RAND_SEED4.z, _SNOIZE_SIZE.w, _SNOIZE_SCALE.w, _SNOIZE_SPEED.w);

	return saturate(float4(x, y, z, w));
}
float4x4 getSNoise4x4(float2 uv)
{
	float4x4 noise = 
	{
		smpl(_RT_SNOISE, uv.xy), smpl(_RT_SNOISE, 1.0 - uv.xy),
		smpl(_RT_SNOISE, uv.yx), smpl(_RT_SNOISE, 1.0 - uv.yx)
	};
	// インスペクタのスライダごとに機能と紐付けたいので転置する
	// （一つの機能を複数のスライダに紐付けると調整が複雑になるため）
	float4x4 noiseT = transpose(noise);

	return noiseT;
}

//////////////////////////////////////////////////////////////////////////////////////////////////
// Flow Noise
//////////////////////////////////////////////////////////////////////////////////////////////////
float4 _FNOIZE_SIZE;
float4 _FNOIZE_SCALE;
float4 _FNOIZE_SPEED;

float domainWarpingFNoise(float2 uv, float3 seed, float3 size, float3 scale, float3 speed)
{
	float2 offset;
	offset.x = fBmSNoise(uv, seed.x, size.x, scale.x, speed.x);
	offset.y = fBmSNoise(uv, seed.y, size.y, scale.y, speed.y);
	offset = (abs((offset - 0.5) * 2.0) - 0.5) * 0.05;

	return fBmSNoise(uv + offset, seed.z, size.z, scale.z, speed.z);
}
float4 fragFNoise(v2f_img i) : SV_Target
{
	float3 seed = RAND_SEED3 * 0.001;
	float3 size = _FNOIZE_SIZE.xyz;
	float3 scale = _FNOIZE_SCALE.xyz;
	float3 speed = _FNOIZE_SPEED.xyz;
	// 入力項目が多くなるのでひとまず使い回す。個別に変更する必要が出たら修正する
	float x = domainWarpingFNoise(i.uv, seed * RAND_SEED4.x, size, scale, speed);
	float y = domainWarpingFNoise(i.uv, seed * RAND_SEED4.y, size, scale, speed);
	float z = domainWarpingFNoise(i.uv, seed * RAND_SEED4.z, size, scale, speed);
	float w = domainWarpingFNoise(i.uv, seed * RAND_SEED4.w, size, scale, speed);

	return saturate(float4(x, y, z, w));
}
inline float4 getFNoise(float2 uv) { return smpl(_RT_FNOISE, uv.xy); }

//////////////////////////////////////////////////////////////////////////////////////////////////
// Voronoi noise
//////////////////////////////////////////////////////////////////////////////////////////////////
float genVNoise(float2 uv, float size, float speed)
{
	float ret = 2.0;
	float time = _Time.y * speed;
	float2 uv1 = floor(uv * size);
	float2 uv2 = frac(uv * size);

	for (int x = -1; x <= 1; x++) {
		for (int y = -1; y <= 1; y++) {
			float2 offset1 = float2(x, y);
			float2 offset2 = rand(uv1 + offset1, RAND_SEED2, 1.0);
			offset2 = (sin(time + PI2 * offset2) + 1.0) * 0.5;
			float dist = distance(uv2, offset1 + offset2);
			ret = min(ret, dist);
		}
	}
	return ret;
}
float fBmVNoise(float2 uv, float size, float speed)
{
	static const float4 frequency = float4(1 << 0, 1 << 1, 1 << 2, 1 << 3);
	static const float4 amplitude = 0.25;// 1.0 / frequency;
	// アスペクト比調整
	uv.x *= TEX_SIZE.x / TEX_SIZE.y;

	float noise = genVNoise(uv * frequency.x, size, speed) * amplitude.x;
	noise += genVNoise(uv * frequency.y, size, speed) * amplitude.y;
	noise += genVNoise(uv * frequency.z, size, speed) * amplitude.z;
	noise += genVNoise(uv * frequency.w, size, speed) * amplitude.w;
	return noise;
}
float fBmMix(float2 uv, float speed)
{
	float snoise = fBmSNoise(uv, 1.0, 20.0, 5.0, speed);
	float vnoise = fBmVNoise((uv - 0.5) * 2.0, 0.7, speed);
	return saturate(((snoise - vnoise) / (1.0 - vnoise)) * 2.0);
}

float4 fragVNoise(v2f_img i) : SV_Target
{
	float size = 2.0, speed = 1.0;

//	return fBmMix(i.uv, speed);

	float x = fBmVNoise(i.uv + RAND_SEED4.xy, size, speed);
	float y = fBmVNoise(i.uv + RAND_SEED4.yz, size, speed);
	float z = fBmVNoise(i.uv + RAND_SEED4.zw, size, speed);
	float w = fBmVNoise(i.uv + RAND_SEED4.wx, size, speed);

	return saturate(float4(x, y, z, w));
}



#endif