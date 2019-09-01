#ifndef COLOR_INCLUDED
#define COLOR_INCLUDED

//////////////////////////////////////////////////////////////////////////////////////////////////
// Color convert
//////////////////////////////////////////////////////////////////////////////////////////////////
float hue2rad(float h1, float h2)
{
	h1 = h1 * 2.0 - 1.0;
	h2 = h2 * 2.0 - 1.0;
	return atan2(h2, h1);
}
float3 hue2rgb(float h)
{
	h = frac(h + 1.0) * 6.0;
	float r = abs(h - 3.0) - 1.0;
	float g = 2.0 - abs(h - 2.0);
	float b = 2.0 - abs(h - 4.0);
	return saturate(float3(r, g, b));
}
float3 hue2rgb2(float h1, float h2)
{
	float h = hue2rad(h1, h2) * INV_PI2;
	return hue2rgb(h);
}
float3 rgb2hcv(float3 rgb)
{
	static const float4 k = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
	float4 p = lerp(float4(rgb.bg, k.wz), float4(rgb.gb, k.xy), step(rgb.b, rgb.g));
	float4 q = lerp(float4(p.xyw, rgb.r), float4(rgb.r, p.yzx), step(p.x, rgb.r));
	float c = q.x - min(q.w, q.y);
	float h = abs((q.w - q.y) / (6.0 * c + EPSILON) + q.z);
	return float3(h, c, q.x);
}
// 色相を直線ではなく円で指定できるようにする
// 直線だと両端にまたがる色域の指定が面倒なため
float4 rgb2hcv2(float3 rgb)
{
	float3 hcq = rgb2hcv(rgb);
	float rad = hcq.x * PI2;
	float h1 = cos(rad) * 0.5 + 0.5;
	float h2 = sin(rad) * 0.5 + 0.5;
	return float4(h1, h2, hcq.y, hcq.z);
}
float3 rgb2hsv(float3 rgb)
{
	float3 hcv = rgb2hcv(rgb);
	float s = hcv.y / (hcv.z + EPSILON);
	return float3(hcv.x, s, hcv.z);
}
float4 rgb2hsv2(float3 rgb)
{
	float4 hcv = rgb2hcv2(rgb);
	float s = hcv.z / (hcv.w + EPSILON);
	return float4(hcv.x, hcv.y, s, hcv.w);
}
float3 hsv2rgb(float3 hsv)
{
	float3 rgb = hue2rgb(hsv.x);
	return ((rgb - 1.0) * hsv.y + 1.0) * hsv.z;
}
float3 hsv2rgb2(float4 hsv)
{
	float3 rgb = hue2rgb2(hsv.x, hsv.y);
	return ((rgb - 1.0) * hsv.z + 1.0) * hsv.w;
}
float3 rgb2hsl(float3 rgb)
{
	float3 hcv = rgb2hcv(rgb);
	float l = hcv.z - hcv.y * 0.5;
	float s = hcv.y / (1.0 - abs(l * 2.0 - 1.0) + EPSILON);
	return float3(hcv.x, s, l);
}
float4 rgb2hsl2(float3 rgb)
{
	float4 hcv = rgb2hcv2(rgb);
	float l = hcv.w - hcv.z * 0.5;
	float s = hcv.z / (1.0 - abs(l * 2.0 - 1.0) + EPSILON);
	return float4(hcv.x, hcv.y, s, l);
}
float3 hsl2rgb(float3 hsl)
{
	float3 rgb = hue2rgb(hsl.x);
	float c = (1.0 - abs(2.0 * hsl.z - 1.0)) * hsl.y;
	return (rgb - 0.5) * c + hsl.z;
}
float3 hsl2rgb2(float4 hsl)
{
	float3 rgb = hue2rgb2(hsl.x, hsl.y);
	float c = (1.0 - abs(2.0 * hsl.w - 1.0)) * hsl.z;
	return (rgb - 0.5) * c + hsl.w;
}
float3 rgb2yuv(float3 rgb)
{
	static const float3x3 mat = { {0.299, 0.587, 0.114}, {-0.147, -0.289, 0.436}, {0.615, -0.515, -0.100} };
	return mul(mat, rgb);
}
float3 yuv2rgb(float3 yuv)
{
	static const float3x3 mat = { {1.00, 0.00, 1.14}, {1.00, -0.39, -0.58}, {1.00, 2.03, 0.00} };
	return mul(mat, yuv);
}
float3 rgb2xyz(float3 rgb)
{
	static const float inv12_92 = 1.0 / 12.92, inv1_055 = 1.0 / 1.055;
	static const float3x3 mat = { {0.4124, 0.3576, 0.1805}, {0.2126, 0.7152, 0.0722}, { 0.0193, 0.1192, 0.9505 } };
	float3 xyz = lerp(rgb * inv12_92, pow((rgb + 0.055) * inv1_055, 2.4), step(0.04045, rgb));
	return mul(mat, xyz * 100.0);
}
float3 xyz2lab(float3 xyz)
{
	static const float inv116 = 1.0 / 116.0;
	static const float3 invCIE_D65 = float3(1.0 / 95.047, 1.0 / 100, 1.0 / 108.883);
	xyz *= invCIE_D65;
	float3 lab = lerp((7.787 * xyz) + (16.0 * inv116), pow(xyz, 0.33333), step(0.008856, xyz));
	return float3((116.0 * lab.y) - 16.0, 500.0 * (lab.x - lab.y), 200.0 * (lab.y - lab.z));
}
float3 rgb2lab(float3 rgb)
{
	static const float inv255 = 1.0 / 255.0;
	float3 lab = xyz2lab(rgb2xyz(rgb));
	return float3(lab.x * 0.01, 0.5 + lab.y * inv255, 0.5 + lab.z * inv255);
}
float3 lab2xyz(float3 lab)
{
	static const float inv116 = 1.0 / 116.0, inv7_787 = 1.0 / 7.787;
	static const float3 CIE_D65 = float3(95.047, 100.000, 108.883);
	float3 xyz = (lab.x + 16.0) * inv116;
	xyz.x += lab.y * 0.002;
	xyz.z -= lab.z * 0.005;		
	xyz = lerp((xyz - 16.0 * inv116) * inv7_787, xyz * xyz * xyz, step(0.206897, xyz));
	return xyz * CIE_D65;
}
float3 xyz2rgb(float3 xyz)
{
	static const float exponent = 1.0 / 2.4;
	static const float3x3 mat = { {3.2406, -1.5372, -0.4986}, {-0.9689, 1.8758, 0.0415}, {0.0557, -0.2040, 1.0570} };
	xyz = mul(mat, xyz * 0.01);
	return lerp(xyz * 12.92, pow(xyz, exponent) * 1.055 - 0.055, step(0.0031308, xyz));
}
float3 lab2rgb(float3 lab)
{
	float3 xyz = lab2xyz(float3(100.0 * lab.x, 255.0 * (lab.y - 0.5), 255.0 * (lab.z - 0.5)));
	return xyz2rgb(xyz);
}

float rgb2lum(float3 rgb) { return dot(float3(0.299, 0.587, 0.114), rgb); }
float rgb2avg(float3 rgb) { return (rgb.r + rgb.g + rgb.b) * 0.333; }

inline float4 fragRGB2HSV(v2f_img i) : SV_Target{ return rgb2hsv2(tex2Dlod(_MainTex, float4(i.uv, 0, 0)).rgb); }
inline float4 fragHSV2RGB(v2f_img i) : SV_Target{ return float4(hsv2rgb2(tex2Dlod(_MainTex, float4(i.uv, 0, 0))), 1.0); }
inline float4 fragRGB2HSL(v2f_img i) : SV_Target{ return rgb2hsl2(tex2Dlod(_MainTex, float4(i.uv, 0, 0)).rgb); }
inline float4 fragHSL2RGB(v2f_img i) : SV_Target{ return float4(hsl2rgb2(tex2Dlod(_MainTex, float4(i.uv, 0, 0))), 1.0); }
inline float4 fragRGB2YUV(v2f_img i) : SV_Target{ return float4(rgb2yuv(tex2Dlod(_MainTex, float4(i.uv, 0, 0)).rgb), 1.0); }
inline float4 fragYUV2RGB(v2f_img i) : SV_Target{ return float4(yuv2rgb(tex2Dlod(_MainTex, float4(i.uv, 0, 0))), 1.0); }
inline float4 fragRGB2LAB(v2f_img i) : SV_Target{ return float4(rgb2lab(tex2Dlod(_MainTex, float4(i.uv, 0, 0)).rgb), 1.0); }
inline float4 fragLAB2RGB(v2f_img i) : SV_Target{ return float4(lab2rgb(tex2Dlod(_MainTex, float4(i.uv, 0, 0))), 1.0); }

float _HSV_H1, _HSV_H2, _HSV_S, _HSV_V;
inline float4 fragDebugHSV(v2f_img i) : SV_Target
{
	return float4(hsv2rgb2(float4(_HSV_H1, _HSV_H2, _HSV_S, _HSV_V)), 1.0);
}

//////////////////////////////////////////////////////////////////////////////////////////////////
// Mask
//////////////////////////////////////////////////////////////////////////////////////////////////
inline float maskDepth(float depth) { return step(depth, 0.999); }
inline float maskDepth(float2 uv) { return maskDepth(smplDepthL01(uv)); }

// 元画像だと勾配の大きい箇所がマスクできない。強めに平滑化してから使った方がいい
float inRangeHSVSub(float4 hsv, float4 rangeH, float4 rangeSV)
{
	float v = saturate(hsv.w);
	float inRangeH1 = step(hsv.x, rangeH.x) * step(rangeH.y, hsv.x);
	float inRangeH2 = step(hsv.y, rangeH.z) * step(rangeH.w, hsv.y);
	float inRangeS = step(hsv.z, rangeSV.x) * step(rangeSV.y, hsv.z);
	float inRangeV = step(v, rangeSV.z) * step(rangeSV.w, v);

	return inRangeH1 * inRangeH2 * inRangeS * inRangeV;
}
// マスク色の範囲内なら1.0、範囲外なら0.0
float inRangeHSV(float4 hsv, float4 rangeH, float4 rangeSV)
{
	static const float steps = 8.0;
	static const float invSteps = 1.0 / steps;

	hsv.xy = floor(hsv.xy * steps) * invSteps;
	return inRangeHSVSub(hsv, rangeH, rangeSV);
}

//////////////////////////////////////////////////////////////////////////////////////////////////
// Blend
//////////////////////////////////////////////////////////////////////////////////////////////////
inline float3 blendNormal(float3 base, float3 ref) { return ref; }
inline float3 blendMultiple(float3 base, float3 ref) { return base * ref; }
inline float3 blendScreen(float3 base, float3 ref) { return 1.0 - (1.0 - base) * (1.0 - ref); }
inline float3 blendOverlay(float3 base, float3 ref)
{
	return lerp(base * ref * 2.0, 1.0 - (1.0 - base) * (1.0 - ref) * 2.0, step(0.5, base));
}
inline float3 blendHardlight(float3 base, float3 ref)
{
	return lerp(base * ref * 2.0, 1.0 - (1.0 - base) * (1.0 - ref) * 2.0, step(0.5, ref));
}
inline float3 blendSoftLight(float3 base, float3 ref)
{
	float3 a = base - (1.0 - 2.0 * ref) * base * (1.0 - base);
	float3 b = base + (2.0 * ref - 1.0) * base * ((16.0 * base - 12.0) * base + 3.0);
	float3 c = base + (2.0 * ref - 1.0) * (sqrt(base) - base);
	return lerp(a, lerp(b, c, step(0.25, base)), step(0.5, ref));
}
inline float3 blendVividLight(float3 base, float3 ref)
{
	return lerp(1.0 - (1.0 - base) / (2.0 * ref), base / (2.0 * (1.0 - ref)), step(0.5, ref));
}
inline float3 blendLinearLight(float3 base, float3 ref){ return 2.0 * ref + base - 1.0; }
inline float3 blendPinLight(float3 base, float3 ref)
{
	float3 a = 2.0 * ref - 1.0, b = 2.0 * ref, c = base;
	return lerp(a, lerp(b, c, step(0.5 * base, ref)), step(base, 2.0 * ref - 1.0));
}
inline float3 blendLinearBurn(float3 base, float3 ref) { return base + ref - 1.0; }
inline float3 blendLinearDodge(float3 base, float3 ref) { return base + ref; }
inline float3 blendColorBurn(float3 base, float3 ref) { return 1.0 - (1.0 - base) / ref; }
inline float3 blendColorDodge(float3 base, float3 ref) { return base / (1.0 - ref); }

inline float3 blendDarken(float3 base, float3 ref) { return min(base, ref); }
inline float3 blendLighten(float3 base, float3 ref) { return max(base, ref); }
inline float3 blendDarkerColor(float3 base, float3 ref)
{
	return lerp(ref, base, step(base.r + base.g + base.b, ref.r + ref.g + ref.b));
}
inline float3 blendLighterColor(float3 base, float3 ref)
{
	return lerp(ref, base, step(ref.r + ref.g + ref.b, base.r + base.g + base.b));
}
inline float3 blendHardMix(float3 base, float3 ref){ return floor(ref + base); }
inline float3 blendDifference(float3 base, float3 ref){ return abs(base - ref); }
inline float3 blendExclusion(float3 base, float3 ref){ return ref + base - 2.0 * ref * base; }
inline float3 blendSubtract(float3 base, float3 ref){ return ref - base; }
inline float3 blendDivide(float3 base, float3 ref){ return ref / base; }

inline float3 blendHue(float3 base, float3 ref)
{
	float3 hsv = rgb2hsv(base);
	hsv.x = rgb2hsv(ref).x;
	return hsv2rgb(hsv);
}
inline float3 blendSaturation(float3 base, float3 ref)
{
	float3 hsv = rgb2hsv(base);
	hsv.y = rgb2hsv(ref).y;
	return hsv2rgb(hsv);
}
inline float3 blendColor(float3 base, float3 ref)
{
	float3 hsv = rgb2hsv(ref);
	hsv.z = rgb2hsv(base).z;
	return hsv2rgb(hsv);
}
inline float3 blendLuminosity(float3 base, float3 ref)
{
	float baseLum = rgb2lum(base.rgb);
	float refLum = rgb2lum(ref.rgb);
	float lum = refLum - baseLum;
	float3 rgb = base + lum;
	float minRGB = min(min(rgb.r, rgb.g), rgb.b);
	float maxRGB = max(max(rgb.r, rgb.g), rgb.b);
	float3 a = refLum + ((rgb - refLum) * refLum) / (refLum - minRGB);
	float3 b = refLum + ((rgb - refLum) * (1.0 - refLum)) / (maxRGB - refLum);
	return lerp(a, lerp(b, rgb, step(1.0, maxRGB)), step(0.0, minRGB));
}
inline float4 blendAlpha(float4 base, float4 ref, float3 composited)
{
	float a1 = ref.a * base.a;
	float a2 = ref.a * (1.0 - base.a);
	float a3 = (1.0 - ref.a) * base.a;
	float a = a1 + a2 + a3;
	float3 rgb = (a1 * composited.rgb + a2 * ref.rgb + a3 * base.rgb) / a;
	return float4(rgb, a);
}

//////////////////////////////////////////////////////////////////////////////////////////////////
// Util
//////////////////////////////////////////////////////////////////////////////////////////////////
inline float hsvDistance(float4 hsv1, float4 hsv2)
{ 
	float4 dist = hsv1 - hsv2;
	return dot(dist, dist);
}
float hueDistance(float4 hsv1, float4 hsv2)
{
	float rad1 = hue2rad(hsv1.x, hsv1.y);
	float rad2 = hue2rad(hsv2.x, hsv2.y);
	return degrees(abs(rad1 - rad2));
}
inline float4 pigmentDensity(float intensity, float scaleFactor)
{
	return scaleFactor * intensity;
}
inline float4 colorModification(float4 color, float density)
{
	return saturate(color - (color - color * color) * density);
}
inline float colorModification(float color, float density)
{
	return saturate(color - (color - color * color) * density);
}
float4 colorModification(float src, float4 dst, float scale)
{
	float intensity = src;
	float density = pigmentDensity(intensity, scale);
	return colorModification(dst, density);
}
float colorModification(float src, float dst, float scale)
{
	float intensity = src;
	float density = pigmentDensity(intensity, scale);
	return colorModification(dst, density);
}
float3 colorModification(float3 src, float3 dst, float scale)
{
	float3 ret;
	ret.r = colorModification(src.r, dst.r, scale);
	ret.g = colorModification(src.g, dst.g, scale);
	ret.b = colorModification(src.b, dst.b, scale);
	return ret;
}
float4 colorModification(float4 src, float4 dst, float scale)
{
	float4 ret;
	ret.r = colorModification(src.r, dst.r, scale);
	ret.g = colorModification(src.g, dst.g, scale);
	ret.b = colorModification(src.b, dst.b, scale);
	ret.a = colorModification(src.a, dst.a, scale);
	return ret;
}

//////////////////////////////////////////////////////////////////////////////////////////////////
// Entry
//////////////////////////////////////////////////////////////////////////////////////////////////
float _CCOpacity;
float _CCMulLum, _CCAddLum;
float _CCInBlack, _CCInGamma, _CCInWhite;
float _CCOutBlack, _CCOutWhite;

output2 fragEntry(v2f_img i) : SV_Target
{
	float4 color = smpl(_MainTex, i.uv);
	float4 mask = smpl(_RT_MASK, i.uv);

	// マスク領域は色補正を反映しない
	if(mask.x < 0.5)
	{
		// ガンマ補正
		static const float inv255 = 1.0 / 255.0;
		float4 input = (max(0.0, (color * 255.0) - _CCInBlack)) / (_CCInWhite - _CCInBlack);
		float4 gamma = pow(input, _CCInGamma);
		color = (gamma * (_CCOutWhite - _CCOutBlack) + _CCOutBlack) * inv255;

		// コントラストと輝度の補整
		float3 lab = rgb2lab(color.rgb);
		lab.x = lab.x * _CCMulLum + _CCAddLum;
		color.rgb = saturate(lab2rgb(lab));
	}

	output2 o;
	o.rt[0] = color;
	o.rt[1] = color; // _RT_WORK1は元画像用に予約
	return o;
}


#endif