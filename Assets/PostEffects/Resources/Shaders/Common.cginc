#ifndef COMMON_INCLUDED
#define COMMON_INCLUDED

sampler2D _MainTex;
sampler2D _DebugTex;
sampler2D _CameraDepthTexture;
sampler2D _CameraDepthNormalsTexture;

float4 _MainTex_ST;
float4 _MainTex_TexelSize;
float4 _DebugTex_ST;
float4 _DebugTex_TexelSize;
float4 _CameraDepthTexture_ST;
float4 _CameraDepthTexture_TexelSize;
float4 _CameraDepthNormalsTexture_ST;
float4 _CameraDepthNormalsTexture_TexelSize;

//////////////////////////////////////////////////////////////////////////////////////////////////
// Define
//////////////////////////////////////////////////////////////////////////////////////////////////
static const float PI = UNITY_PI;
static const float PI2 = UNITY_TWO_PI;
static const float INV_PI = UNITY_INV_PI;
static const float INV_PI2 = UNITY_INV_TWO_PI;
static const float HALF_PI = UNITY_HALF_PI;
static const float SQRT2 = sqrt(2);
static const float INV_SQRT2 = 1.0 / SQRT2;
static const float EPSILON = 1e-10;
static const float COS90 = cos(radians(90)); // 0.0
static const float SIN90 = sin(radians(90)); // 1.0
static const float4x4 NEIGHBOR8_UNITVEC = 
{
	{ { -INV_SQRT2, -INV_SQRT2 }, { 0, -1 } }, { { INV_SQRT2, -INV_SQRT2 }, { -1, 0 } }, 
	{ { 1, 0 }, { -INV_SQRT2, INV_SQRT2 } }, { { 0, 1 }, { INV_SQRT2, INV_SQRT2 } }
};
static const float4x4 NEIGHBOR8_UNITVEC_ORTHO = 
{
	{ { INV_SQRT2, -INV_SQRT2 }, { 1, 0 } }, { { INV_SQRT2, INV_SQRT2 }, { 0, -1 } },
	{ { 0, 1 }, { -INV_SQRT2, -INV_SQRT2 } }, { { -1, 0 }, { -INV_SQRT2, INV_SQRT2 } }
};

static const float2 TEX_SIZE = _MainTex_TexelSize.zw;
static const float2 UV_SIZE = _MainTex_TexelSize.xy;
static const float2 TEX2UV = _MainTex_TexelSize.xy;
static const float2 UV2TEX = _MainTex_TexelSize.zw;
static const float ASPECT = _MainTex_TexelSize.w / _MainTex_TexelSize.z;
static const float4x4 TEX2UV4x4 = { {TEX2UV, TEX2UV}, {TEX2UV, TEX2UV}, {TEX2UV, TEX2UV}, {TEX2UV, TEX2UV} };

// GL_MAX_TEXTURE_IMAGE_UNITSを参考
sampler2D _RT_WORK0, _RT_WORK1, _RT_WORK2, _RT_WORK3, _RT_WORK4, _RT_WORK5, _RT_WORK6, _RT_WORK7;
sampler2D _RT_CACHE0, _RT_CACHE1;
sampler2D _RT_MASK;
sampler2D _RT_ORIG;
sampler2D _RT_TFM; // _RT_WORK2
sampler2D _RT_SOBEL; // _RT_WORK3
sampler2D _RT_OUTLINE; // _RT_WORK4
sampler2D _RT_SNOISE; // _RT_WORK6
sampler2D _RT_FNOISE; // _RT_WORK7

// GL_MAX_COLOR_ATTACHMENTSを参考
struct output2 { float4 rt[2] : COLOR0; };
struct output3 { float4 rt[3] : COLOR0; };
struct output4 { float4 rt[4] : COLOR0; };

//////////////////////////////////////////////////////////////////////////////////////////////////
// Common
//////////////////////////////////////////////////////////////////////////////////////////////////
inline float gamma(float g, float x) { return pow(x, 1.0 / g); }
inline float bias(float b, float x) { return pow(x, log(b) / log(0.5)); }
inline float gain(float g, float x)
{ 
	return (x < 0.5) ? bias(1.0 - g, 2.0 * x) * 0.5 : 1.0 - bias(1.0 - g, 2.0 - 2.0 * x) * 0.5; 
}

inline float4x4 to4x4(float2 v) { return float4x4(v, v, v, v, v, v, v, v); }

inline float summation(float4 v) { return v.x + v.y + v.z + v.w; }
inline float summation(float4x4 v)
{ 
	return summation(v[0]) + summation(v[1]) + summation(v[2]) + summation(v[3]); 
}

inline float4 dot(float4 x1, float4 x2, float4 y)
{
	return float4(dot(x1.xy, y.xy), dot(x1.zw, y.zw), dot(x2.xy, y.xy), dot(x2.zw, y.zw));
}
inline float4x4 dot(float4x4 x1, float4x4 x2, float4x4 y)
{
	return float4x4(dot(x1[0], x2[0], y[0]), dot(x1[1], x2[1], y[1]), 
					dot(x1[2], x2[2], y[2]), dot(x1[3], x2[3], y[3]));
}
inline float4 normalize2(float4 v) { return float4(normalize(v.xy), normalize(v.zw)); }
inline float4x4 normalize(float4x4 v)
{ 
	return float4x4(normalize2(v[0]), normalize2(v[1]), normalize2(v[2]), normalize2(v[3])); 
}

inline float2 orthogonalize(float2 v) { return float2(v.x * COS90 - v.y * SIN90, v.y * COS90 + v.x * SIN90); } //float2(-v.y, v.x)
inline float4 orthogonalize(float4 v) { return float4(orthogonalize(v.xy), orthogonalize(v.zw)); }
inline float4x4 orthogonalize(float4x4 v)
{
	return float4x4(orthogonalize(v[0]), orthogonalize(v[1]), orthogonalize(v[2]), orthogonalize(v[3])); 
}

//////////////////////////////////////////////////////////////////////////////////////////////////
// Sampler
//////////////////////////////////////////////////////////////////////////////////////////////////
inline float4 smpl(sampler2D tex, float2 uv, float lod){ return tex2Dlod(tex, float4(uv, 0.0, lod)); }
inline float4 smpl(sampler2D tex, float2 uv) { return smpl(tex, uv, 0.0);  }
inline float4 smpl(float2 uv, float lod) { return smpl(_MainTex, uv, lod); }
inline float4 smpl(float2 uv) { return smpl(_MainTex, uv, 0.0); }

inline float4 smplLum(sampler2D tex, float2 uv)
{
	float3 rgb = tex2Dlod(tex, float4(uv, 0.0, 0.0)).rgb;
	return dot(float3(0.299, 0.587, 0.114), rgb);
}
inline float4 smplLum(float2 uv) { return smplLum(_RT_ORIG, uv); }

inline float4 smplAvg(sampler2D tex, float2 uv)
{
	float3 rgb = tex2Dlod(tex, float4(uv, 0.0, 0.0)).rgb;
	return (rgb.r + rgb.g + rgb.b) * 0.333;
}
inline float4 smplAvg(float2 uv) { return smplAvg(_RT_ORIG, uv); }

inline float4 smplMono(sampler2D tex, float2 uv)
{
	return saturate(tex2Dlod(tex, float4(uv, 0.0, 0.0)) + 1.0);
}
inline float4 smplMono(float2 uv) { return smplMono(_RT_ORIG, uv); }

inline float2 smplSobel(float2 uv, float2 offset) { return smpl(_RT_SOBEL, uv + offset).xy; }
inline float4 smplSobel(float4 uv, float4 offset)
{ 
	return float4(smplSobel(uv.xy, offset.xy), smplSobel(uv.zw, offset.zw));
}
inline float4x4 smplSobel(float4x4 uv, float4x4 offset)
{
	return float4x4(smplSobel(uv[0], offset[0]), smplSobel(uv[1], offset[1]), 
					smplSobel(uv[2], offset[2]), smplSobel(uv[3], offset[3]));
}

inline float smplDepth(float2 uv){ return tex2Dlod(_CameraDepthTexture, float4(uv, 0.0, 0.0)).x; }
// 深度をカメラからの距離0.0～0.1で返す
inline float smplDepthL01(float2 uv) { return Linear01Depth(smplDepth(uv)); }

//////////////////////////////////////////////////////////////////////////////////////////////////
// Lerp Render Texture
//////////////////////////////////////////////////////////////////////////////////////////////////
sampler2D _LerpBuf;
float _LerpRate;

float4 fragLerp(v2f_img i) : SV_Target
{
	float4 src = smpl(i.uv);
	float4 buf = smpl(_LerpBuf, i.uv);
	return src * _LerpRate + buf * (1.0 - _LerpRate);
}

//////////////////////////////////////////////////////////////////////////////////////////////////
// Debug
//////////////////////////////////////////////////////////////////////////////////////////////////
float4 debugOriginPos(float2 uv)
{
	if ((uv.x < 0.1) && (uv.y < 0.1)) { return float4(1.0, 0.0, 0.0, 1.0); }
	if ((uv.x < 0.1) && (uv.y > 0.9)) { return float4(0.0, 1.0, 0.0, 1.0); }
	if ((uv.x > 0.9) && (uv.y < 0.1)) { return float4(0.0, 0.0, 1.0, 1.0); }
	if ((uv.x > 0.9) && (uv.y > 0.9)) { return float4(1.0, 1.0, 0.0, 1.0); }

	return float4(1, 1, 1, 1); 
}



#endif