Shader "UI/RadialSegmentedBar/BuiltIn"
{
    Properties
    {
        [HideInInspector] _MainTex("DONT_USE", 2D) = "white" {}

        _StencilComp("Stencil Comparison", Float) = 8
        _Stencil("Stencil ID", Float) = 0
        _StencilOp("Stencil Operation", Float) = 0
        _StencilWriteMask("Stencil Write Mask", Float) = 255
        _StencilReadMask("Stencil Read Mask", Float) = 255
        _ColorMask("Color Mask", Float) = 15

        _Tint("Tint", Color) = (1,1,1,1)

        _OverlayTexture("OverlayTexture", 2D) = "white" {}
        _InnerTexture("InnerTexture", 2D) = "white" {}
        _BorderTexture("BorderTexture", 2D) = "white" {}
        _EmptyTexture("EmptyTexture", 2D) = "white" {}
        _SpaceTexture("SpaceTexture", 2D) = "white" {}

        [HDR] _OverlayColor("OverlayColor", Color) = (1,1,1,1)
        [HDR] _InnerColor("InnerColor", Color) = (1,0,0,1)
        [HDR] _EmptyColor("EmptyColor", Color) = (0.05,0.05,0.05,1)
        [HDR] _BorderColor("BorderColor", Color) = (1,1,1,1)
        [HDR] _SpaceColor("SpaceColor", Color) = (1,1,1,0)

        [Toggle] _AlternateInnerColorsEnabled("Alternate Inner Colors", Float) = 0
        [HDR] _InnerAltColor("Inner Alt Color", Color) = (1,1,1,1)

        _SegmentCount("SegmentCount", Float) = 5
        _RemoveSegments("RemoveSegments", Float) = 1

        _SegmentSpacing("Spacing", Float) = 0.04
        _Arc("Arc", Float) = 0.1
        _Radius("Radius", Float) = 0.35
        _LineWidth("LineWidth", Float) = 0.1
        _Rotation("Rotation", Float) = 0
        _Offset("Offset", Vector) = (0,0,0,0)

        _BorderSpacing("BorderSpacing", Float) = 0.02
        _BorderWidth("BorderWidth", FLoat) = 0.02

        _RemoveBorder("RemoveBorder", Float) = 0

        [Toggle] _ContentNoiseEnabled("ContentNoiseEnabled", Float) = 0
        _ContentNoiseScale("ContentNoiseScale", Float) = 100
        _ContentNoiseStrength("ContentNoiseStrength", Float) = 1
        _ContentNoiseOffset("ContentNoiseOffset", Vector) = (0,0,0,0)

        [Toggle] _EmptyNoiseEnabled("EmptyNoiseEnabled", Float) = 0
        _EmptyNoiseScale("EmptyNoiseScale", Float) = 100
        _EmptyNoiseStrength("EmptyNoiseStrength", Float) = 1
        _EmptyNoiseOffset("EmptyNoiseOffset", Vector) = (0,0,0,0)

        [Toggle] _OverlayNoiseEnabled("OverlayNoiseEnabled", Float) = 0
        _OverlayNoiseScale("OverlayNoiseScale", Float) = 100
        _OverlayNoiseStrength("OverlayNoiseStrength", Float) = 1
        _OverlayNoiseOffset("OverlayNoiseOffset", Vector) = (0,0,0,0)


        _OverlayTextureOpacity("OverlayTextureOpacity", Float) = 1
        _OverlayTextureTiling("OverlayTextureTiling", Vector) = (1,1,1,1)
        _OverlayTextureOffset("OverlayTextureOffset", Vector) = (0,0,0,0)

        _SpaceTextureOpacity("SpaceTextureOpacity", Float) = 1
        [Toggle] _AlignSpaceTexture("AlignSpaceTexture", Float) = 1
        _SpaceTextureTiling("SpaceTextureTiling", Vector) = (1,1,1,1)
        _SpaceTextureOffset("SpaceTextureOffset", Vector) = (0,0,0,0)

        _BorderTextureOpacity("BorderTextureOpacity", Float) = 1
        [Toggle] _BorderTextureScaleWithSegments("BorderTextureScaleWithSegments", Float) = 1
        [Toggle] _AlignBorderTexture("AlignBorderTexture", Float) = 1
        _BorderTextureTiling("BorderTextureTiling", Vector) = (1,1,1,1)
        _BorderTextureOffset("BorderTextureOffset", Vector) = (0,0,0,0)

        _EmptyTextureOpacity("EmptyTextureOpacity", Float) = 1
        [Toggle] _EmptyTextureScaleWithSegments("EmptyTextureScaleWithSegments", Float) = 1
        [Toggle] _AlignEmptyTexture("AlignEmptyTexture", Float) = 1
        _EmptyTextureTiling("EmptyTextureTiling", Vector) = (1,1,1,1)
        _EmptyTextureOffset("EmptyTextureOffset", Vector) = (0,0,0,0)

        _InnerTextureOpacity("InnerTextureOpacity", Float) = 1
        [Toggle] _InnerTextureScaleWithSegments("InnerTextureScaleWithSegments", Float) = 1
        [Toggle] _AlignInnerTexture("AlignInnerTexture", Float) = 1
        _InnerTextureTiling("InnerTextureTiling", Vector) = (1,1,1,1)
        _InnerTextureOffset("InnerTextureOffset", Vector) = (0,0,0,0)

        _InnerGradient("InnerGradient", 2D) = "white" {}
        [Toggle] _InnerGradientEnabled("InnerGradientEnabled", Float) = 1
        [Toggle] _ValueAsGradientTimeInner("ValueAsGradientTimeInner", Float) = 0

        _EmptyGradient("EmptyGradient", 2D) = "white" {}
        [Toggle] _EmptyGradientEnabled("EmptyGradientEnabled", Float) = 1
        [Toggle] _ValueAsGradientTimeEmpty("ValueAsGradientTimeEmpty", Float) = 0

        _VariableWidthCurve("VariableWidthCurve", 2D) = "white" {}

        [Toggle] _FillClockwise("FillClockwise", Float) = 0

        /*[Toggle(INNER_TEXTURE_ON)] _InnerTextureEnabled("InnerTextureEnabled", Float) = 0
        [Toggle(SPACE_TEXTURE_ON)] _SpaceTextureEnabled("SpaceTextureEnabled", Float) = 0
        [Toggle(BORDER_TEXTURE_ON)] _BorderTextureEnabled("BorderTextureEnabled", Float) = 0
        [Toggle(EMPTY_TEXTURE_ON)] _EmptyTextureEnabled("EmptyTextureEnabled", Float) = 0
        [Toggle(OVERLAY_TEXTURE_ON)] _OverlayTextureEnabled("OverlayTextureEnabled", Float) = 0*/
    }
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent" "RenderType" = "Transparent"
        }
        LOD 100
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest[unity_GUIZTestMode]

        Pass
        {

            Stencil
            {
                Ref[_Stencil]
                Comp[_StencilComp]
                Pass[_StencilOp]
                ReadMask[_StencilReadMask]
                WriteMask[_StencilWriteMask]
            }
            ColorMask[_ColorMask]

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local __ INNER_TEXTURE_ON
            #pragma multi_compile_local __ SPACE_TEXTURE_ON
            #pragma multi_compile_local __ BORDER_TEXTURE_ON
            #pragma multi_compile_local __ EMPTY_TEXTURE_ON
            #pragma multi_compile_local __ OVERLAY_TEXTURE_ON

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct VertexIn
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct VertexOut
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            	fixed4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            sampler2D _OverlayTexture;
            sampler2D _InnerTexture;
            sampler2D _BorderTexture;
            sampler2D _EmptyTexture;
            sampler2D _SpaceTexture;

            float4 _Tint;

            float4 _OverlayColor;
            float4 _InnerColor;
            float4 _EmptyColor;
            float4 _BorderColor;
            float4 _SpaceColor;

            float  _AlternateInnerColorsEnabled;
            float4 _InnerAltColor;

            float _SegmentCount;
            float _RemoveSegments;

            float _SegmentSpacing;
            float _Arc;
            float _Radius;
            float _LineWidth;
            float _Rotation;
            float4 _Offset;

            float _BorderSpacing;
            float _BorderWidth;

            float _RemoveBorder;

            float4 _ContentNoiseOffset;
            float _ContentNoiseScale;
            float _ContentNoiseStrength;
            float _ContentNoiseEnabled;

            float4 _EmptyNoiseOffset;
            float _EmptyNoiseScale;
            float _EmptyNoiseStrength;
            float _EmptyNoiseEnabled;

            float4 _OverlayNoiseOffset;
            float _OverlayNoiseScale;
            float _OverlayNoiseStrength;
            float _OverlayNoiseEnabled;

            float _OverlayTextureOpacity;
            float4 _OverlayTextureTiling;
            float4 _OverlayTextureOffset;

            float _SpaceTextureOpacity;
            float _AlignSpaceTexture;
            float4 _SpaceTextureTiling;
            float4 _SpaceTextureOffset;

            float _BorderTextureOpacity;
            float _AlignBorderTexture;
            float _BorderTextureScaleWithSegments;
            float4 _BorderTextureTiling;
            float4 _BorderTextureOffset;

            float _EmptyTextureOpacity;
            float _AlignEmptyTexture;
            float _EmptyTextureScaleWithSegments;
            float4 _EmptyTextureTiling;
            float4 _EmptyTextureOffset;

            float _InnerTextureOpacity;
            float _AlignInnerTexture;
            float _InnerTextureScaleWithSegments;
            float4 _InnerTextureTiling;
            float4 _InnerTextureOffset;

            sampler2D _InnerGradient;
            float _InnerGradientEnabled;
            float _ValueAsGradientTimeInner;

            sampler2D _EmptyGradient;
            float _EmptyGradientEnabled;
            float _ValueAsGradientTimeEmpty;

            sampler2D _VariableWidthCurve;
            float _FillClockwise;

            //rotate uvs using radians
            //source: https://forum.unity.com/threads/rotation-of-texture-uvs-directly-from-a-shader.150482/
            float2 rotateuv(float2 uv, float2 center, float rotation)
            {
                uv -= center;
                float s = sin(rotation);
                float c = cos(rotation);
                float2x2 rotMat = float2x2(c, -s, s, c);
                rotMat *= .5;
                rotMat += .5;
                rotMat = rotMat * 2 - 1;
                float2 res = mul(uv, rotMat);
                res += center;
                return res;
            }

            //source: https://stackoverflow.com/a/3451607/3987342
            float remap(float value, float2 i, float2 o)
            {
                return o.x + (value - i.x) * (o.y - o.x) / (i.y - i.x);
            }

            float4 gradient(float4 colors[8], float colorCount, float2 alphas[8], float alphaCount, float t,
                             bool isFixed)
            {
                if (isFixed)
                {
                    int index = 0;
                    int currentindex = 0;
                    while (currentindex < colorCount && t > colors[currentindex].a)
                    {
                        index = currentindex;
                        currentindex++;
                    }
                    if ((index != 0 || t >= colors[index].a) && index < colorCount - 1)
                    {
                        index++;
                    }
                    float4 color = colors[index];

                    index = 0;
                    currentindex = 0;
                    while (currentindex < alphaCount && t > alphas[currentindex].y)
                    {
                        index = currentindex;
                        currentindex++;
                    }
                    if ((index != 0 || t >= alphas[index].y) && index < alphaCount - 1)
                    {
                        index++;
                    }
                    color.a = alphas[index].x;

                    return color;
                }
                else
                {
                    float index = 0;
                    int currentindex = 0;
                    while (currentindex < colorCount && t > colors[currentindex].a)
                    {
                        index = currentindex;
                        currentindex++;
                    }
                    float4 color = colors[index];
                    if ((index != 0 || t > colors[index].a) && index != colorCount - 1)
                    {
                        color = lerp(colors[index], colors[index + 1],
                                       remap(t, float2(colors[index].a, colors[index + 1].a), float2(0, 1)));
                    }

                    index = 0;
                    currentindex = 0;
                    while (currentindex < alphaCount && t > alphas[currentindex].y)
                    {
                        index = currentindex;
                        currentindex++;
                    }
                    color.a = alphas[index].x;
                    if ((index != 0 || t > alphas[index].y) && index != colorCount - 1)
                    {
                        color.a = lerp(alphas[index].x, alphas[index + 1].x,
                                           remap(t, float2(alphas[index].y, alphas[index + 1].y), float2(0, 1)));
                    }
                    return color;
                }
            }

            float4 gradient(float4 color1, float4 color2, float2 alpha1, float2 alpha2, float t)
            {
                if (color1.a > color2.a)
                {
                    float4 tmp = color1;
                    color1 = color2;
                    color2 = tmp;
                }
                if (alpha1.y > alpha2.y)
                {
                    float2 tmp = alpha1;
                    alpha1 = alpha2;
                    alpha2 = tmp;
                }
                float4 color = color1;

                t = clamp(t, 0, 1);
                if (t > color2.a)
                {
                    color = color2;
                }
                else if (t > color1.a)
                {
                    color = lerp(color1, color2, remap(t, float2(color1.a, color2.a), float2(0, 1)));
                }

                color.a = alpha1.x;
                if (t > alpha2.y)
                {
                    color.a = alpha2.x;
                }
                else if (t > alpha1.y)
                {
                    color.a = lerp(alpha1.x, alpha2.x, remap(t, float2(alpha1.y, alpha2.y), float2(0, 1)));
                }

                return color;
            }

            float mod(float a, float b)
            {
                return a % b;
            }

            //inverse lerp function
            //source: shader graph
            float ilerp(float a, float b, float t)
            {
                return (t - a) / (b - a);
            }

            float2 hash(float2 x) // replace this by something better
            {
                x = frac(x * 0.3183099 + 0.1) * 17.0;
                float a = frac(x.x * x.y * (x.x + x.y)); // [0..1]
                a *= 2.0 * 3.14159; // [0..2PI]
                return float2(sin(a), cos(a));
            }

            //source: https://www.shadertoy.com/view/4dS3Wd
            float noise(float2 uv, float scale)
            {
                float2 scaleduv = uv * (scale / 10);
                float2 i = floor(scaleduv);
                float2 f = frac(scaleduv);

                float2 u = f * f * (3.0 - 2.0 * f);

                return lerp(lerp(dot(hash(i + float2(0.0, 0.0)), f - float2(0.0, 0.0)),
                                         dot(hash(i + float2(1.0, 0.0)), f - float2(1.0, 0.0)), u.x),
                                    lerp(dot(hash(i + float2(0.0, 1.0)), f - float2(0.0, 1.0)),
                                                         dot(hash(i + float2(1.0, 1.0)), f - float2(1.0, 1.0)),
                                                         u.x),
                                    u.y);
            }

            float betterNoise(float2 uv, float scale)
            {
                float2x2 m = float2x2(1.6, 1.2, -1.2, 1.6);
                float f = 0.5 + 0.5 * noise(uv, scale);
                uv = mul(m, uv);
                f += 0.2500 * noise(uv, scale);
                uv = mul(m, uv);
                f += 0.1250 * noise(uv, scale);
                uv = mul(m, uv);
                f += 0.0625 * noise(uv, scale);
                uv = mul(m, uv);
                return f;
            }

            float myNoise(float2 uv, float2 offset, float scale, float strength, float enabled)
            {
                float res = 1;
                if (enabled >= 1.0f)
                {
                    res = clamp(mul(betterNoise(uv + offset, scale), strength) + (1 - strength), 0, 1);
                }
                return res;
            }

            //source: shader graph
            float2 polarCoordinates(float2 uv, float2 center, float radialScale, float lengthScale)
            {
                float2 delta = uv - center;
                float radius = length(delta) * 2 * radialScale;
                float angle = atan2(delta.x, delta.y) * 1.0 / 6.28 * lengthScale;
                return float2(radius, angle);
            }

            VertexOut vert(VertexIn v)
            {
                VertexOut o;

            	UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v,o);

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
            	o.color = v.color * _Tint;
                return o;
            }

            fixed4 frag(VertexOut i) : SV_Target
            {
                float pi = 3.14159;
                float pi2 = pi * 2;
                float2 halfuv = float2(.5, .5);
                float2 transuv = i.uv + _Offset - halfuv;
                float transuvlen = length(transuv);
                float divpisegc = pi / _SegmentCount;

                //LINES - affected by arc
                float2 rotateduv01 = rotateuv(transuv, float2(0, 0), radians(_Rotation));
                float arcpi2 = _Arc * pi2;
                float atanRes = atan2(rotateduv01.y, rotateduv01.x);
                atanRes =
                    _FillClockwise * -atanRes +
                    (1 - _FillClockwise) * atanRes;
                float calc000 = pi + atanRes - arcpi2;
                float lineMul = calc000 * (pi2 / (pi2 - arcpi2));
                float lineAdd = lineMul + divpisegc;
                float lines = mul(transuvlen, abs(sin(mod(lineAdd, 2 * divpisegc) - divpisegc)));

                float sineLines = transuvlen * sin(mod(lineAdd, 2 * divpisegc) - divpisegc);

                //ARC - rest
                float smoothArc = clamp(1 - lineMul / fwidth(lineMul), 0, 1);
                float remapArc = round(clamp(remap(_Arc, float2(0, 0.001), float2(2, 1)), 0, 1));
                float finalArc = clamp(smoothArc - remapArc, 0, 1);

                //VARIABLE_LINE_WIDTH

                float fragmentLineWidth = _LineWidth * tex2D(_VariableWidthCurve,
                                                           float2(remap(calc000, float2(0, pi2 - arcpi2),
                                            float2(0, 1)), 0)).r;

                //INNER_SPACING
                float innerspacing = clamp(finalArc + mul(
                                    1 - clamp(
                                        (lines - _SegmentSpacing) / fwidth(lines - _SegmentSpacing),
                                        0, 1),
                                    mul(clamp(remap(_SegmentSpacing, float2(0, 0.001), float2(0, 1)),
                                                             0,
                                                             1),
                                                       round(clamp(
                                                           remap(_SegmentCount, float2(0, 1),
                                                               float2(0, 0.5)),
                                                           0,
                                                           1)))), 0, 1);

                //INNER_CIRCLE
                float preCirc = (transuvlen - _Radius);
                float sdfCircle = abs(preCirc) - fragmentLineWidth;
                float circle = 1 - clamp((sdfCircle) / fwidth(preCirc), 0, 1);

                float segcirc = circle - innerspacing;

                float calc001 = remap(mul(_RemoveSegments, divpisegc), float2(0, pi),
                                                                    float2(divpisegc, divpisegc + pi2)) - lineAdd;
                float removedsegments = lerp(clamp(calc001 / fwidth(calc001), 0, 1),
                                                    smoothstep(0, 0.001, calc001 / fwidth(calc001)),
                                                    clamp(ilerp(.0001, .0002, _RemoveSegments), 0,
             1));

                float remsegcirc = clamp(segcirc - removedsegments, 0, 1);

                //PULSATING


                //float4 pulsating = 0;
                //if (_PulsateWhenLow >= 1)
                //{
                //	//TODO: change to lerp
                //	pulsating = gradient(
                //		float4(.01, .01, .01, 0),
                //		float4(1, 1, 1, .9),
                //		float2(1, 0),
                //		float2(1, 1),
                //		remap(
                //			sin(_Time * _PulseSpeed * 20),
                //			float2(-1, 1),
                //			float2(0,
                //				1 - clamp(
                //					remap(_SegmentCount - _RemoveSegments,
                //						float2(0, _SegmentCount * _PulseActivationThreshold),
                //						float2(0, 1)
                //					),
                //					0, 1
                //				)
                //				)
                //		));

                //}

                //INNER_GRADIENT
                float4 innerGradient = float4(1, 1, 1, 1);

                #ifdef INNER_TEXTURE_ON

				/*float2 selectedInnerGradientCoord;
				if (_InnerGradientEnabled >= 1) {
					if (_ValueAsGradientTimeInner >= 1)
					{
						selectedInnerGradientCoord = float2(clamp(1 - (_SegmentCount - _RemoveSegments) / _SegmentCount, 0.005, 0.995), 0);
					}
					else
					{
						selectedInnerGradientCoord =
							float2(1.0 / (1 - _Arc), 1) * (rotateuv(polarCoordinates(rotateuv(transuv, float2(0, 0),radians(_Rotation - 90)), float2(0, 0),1,1), float2(0, 0),pi / 2) + float2(.5, 0));
					}

					innerGradient = tex2D(_InnerGradient, selectedInnerGradientCoord);*/

				innerGradient =
					//_InnerGradientEnabled == true
					_InnerGradientEnabled * tex2D(_InnerGradient,
						//_ValueAsGradientTimeInner == true
						_ValueAsGradientTimeInner * float2(clamp(1 - (_SegmentCount - _RemoveSegments) / _SegmentCount, 0.005, 0.995), 0) +
						//_ValueAsGradientTimeInner == false
						(1 - _ValueAsGradientTimeInner) * float2(1.0 / (1 - _Arc), 1) * (rotateuv(polarCoordinates(rotateuv(transuv, float2(0, 0), radians(_Rotation - 90)), float2(0, 0), 1, 1), float2(0, 0), pi / 2) + float2(.5, 0))) +
					(1 - _InnerGradientEnabled) * float4(1, 1, 1, 1);
				//}

                #endif

                // INNER_TEXTURE/COLOR
				float4 innerColor;

				if (_AlternateInnerColorsEnabled > 0.5)
				{
				    float2 dir = normalize(transuv);
				    float rawAngle = atan2(dir.y, dir.x);
				    if (rawAngle < 0)
				    	rawAngle += pi2;

				    float sectorSize = pi2 / max(_SegmentCount, 1e-4);
				    float segIndex = floor(rawAngle / sectorSize);

				    innerColor = (fmod(segIndex, 2.0) < 0.5) ? _InnerColor : _InnerAltColor;
				}
				else
				{
				    innerColor = _InnerColor;
				}

                #ifdef INNER_TEXTURE_ON

				float lengthScale = _InnerTextureScaleWithSegments * _SegmentCount + _InnerTextureTiling.x;
				float4 innerTextureSample =
					_AlignInnerTexture * tex2D(_InnerTexture, rotateuv(float2(remap((transuvlen - _Radius) + fragmentLineWidth, float2(-0.0001, +fragmentLineWidth * 2), float2(0.05, 0.95)),
					(1 / (1 - _Arc)) * (lengthScale / 2 - polarCoordinates(rotateuv(rotateuv(transuv, float2(0, 0), radians(_Rotation - 90)), float2(0, 0), radians(_InnerTextureOffset.x)), float2(0, 0), 1, lengthScale).y - lengthScale * _Arc)), float2(.5, .5), pi / 2)) +
					(1 - _AlignInnerTexture) * tex2D(_InnerTexture, i.uv * float2(_InnerTextureTiling.x, _InnerTextureTiling.y) + float2(_InnerTextureOffset.x, _InnerTextureOffset.y));
				/*if (_AlignInnerTexture >= 1)
				{
					if (_InnerTextureScaleWithSegments >= 1)
					{
						lengthScale = _SegmentCount + _InnerTextureTiling.x;
					}
					else
					{
						lengthScale = _InnerTextureTiling.x;
					}

					innerTextureSample = tex2D(_InnerTexture, rotateuv(float2(remap((transuvlen - _Radius) + fragmentLineWidth, float2(-0.0001, +fragmentLineWidth * 2), float2(0.05, 0.95)),
					(1 / (1 - _Arc)) * (lengthScale / 2 - polarCoordinates(rotateuv(rotateuv(transuv, float2(0,0), radians(_Rotation - 90)), float2(0,0), radians(_InnerTextureOffset.x)),float2(0,0), 1, lengthScale).y - lengthScale * _Arc)), float2(.5, .5),pi / 2));
				}
				else
				{
					innerTextureSample = tex2D(_InnerTexture, i.uv * float2(_InnerTextureTiling.x, _InnerTextureTiling.y) + float2(_InnerTextureOffset.x, _InnerTextureOffset.y));
				}*/

				float4 gradientModifiedInnerColor = _InnerColor * innerGradient;

				innerColor = lerp(gradientModifiedInnerColor, innerTextureSample * gradientModifiedInnerColor, _InnerTextureOpacity);

                #endif

                //NOISE
                float innerNoise =
                    _ContentNoiseEnabled * myNoise(i.uv, _ContentNoiseOffset, _ContentNoiseScale, _ContentNoiseStrength,
           _ContentNoiseEnabled) +
                    (1 - _ContentNoiseEnabled) * 1;
                /*if (_ContentNoiseEnabled >= 1)
                {
                    innerNoise = myNoise(i.uv, _ContentNoiseOffset, _ContentNoiseScale, _ContentNoiseStrength, _ContentNoiseEnabled);
                }*/

                float emptyNoise = _EmptyNoiseEnabled * myNoise(i.uv, _EmptyNoiseOffset, _EmptyNoiseScale,
                     _EmptyNoiseStrength, _EmptyNoiseEnabled) +
                    (1 - _EmptyNoiseEnabled) * 1;
                /*if (_EmptyNoiseEnabled >= 1)
                {
                    emptyNoise = myNoise(i.uv, _EmptyNoiseOffset, _EmptyNoiseScale, _EmptyNoiseStrength, _EmptyNoiseEnabled);
                }*/

                float overlayNoise = _OverlayNoiseEnabled * myNoise(i.uv, _OverlayNoiseOffset, _OverlayNoiseScale,
                              _OverlayNoiseStrength, _OverlayNoiseEnabled) +
                    (1 - _OverlayNoiseEnabled) * 1;
                /*if (_OverlayNoiseEnabled >= 1)
                {
                    overlayNoise = myNoise(i.uv, _OverlayNoiseOffset, _OverlayNoiseScale, _OverlayNoiseStrength, _OverlayNoiseEnabled);
                }*/

                //TODO: apply pulsating color earlier in texture phase
                //INNER_CIRCLE_FINAL
                float4 innerCircleFinal = float4(innerColor.rgb, 1) * remsegcirc * innerColor.a * innerNoise;


                //EMPTY_SHAPE
                float emptyShape = clamp(clamp(clamp(segcirc, 0, 1) - remsegcirc, 0, 1) * (1 - _RemoveBorder), 0, 1);

                //EMPTY_GRADIENT
                float4 emptyGradient = float4(1, 1, 1, 1);

                #ifdef EMPTY_TEXTURE_ON

				float2 selectedEmptyGradientCoord;
				if (_EmptyGradientEnabled >= 1) {
					if (_ValueAsGradientTimeEmpty >= 1)
					{
						selectedEmptyGradientCoord = float2(clamp(1 - (_SegmentCount - _RemoveSegments) / _SegmentCount, 0.005, 0.995), 0);
					}
					else
					{
						selectedEmptyGradientCoord = float2(1 / (1 - _Arc), 1) * (rotateuv(
						polarCoordinates(
						rotateuv(transuv, float2(0, 0),radians(90 + _Rotation)),
						float2(0, 0),1,-1
						),
						float2(0, 0),pi / 2
						) + float2(.5, 0));
					}

					emptyGradient = tex2D(_EmptyGradient, selectedEmptyGradientCoord);
				}

                #endif

                //EMPTY_TEXTURE/COLOR
                float4 emptyColor = _EmptyColor;

                #ifdef EMPTY_TEXTURE_ON

				float4 emptyTextureSample;
				if (_AlignEmptyTexture >= 1)
				{
					float lengthScale;
					if (_EmptyTextureScaleWithSegments >= 1)
					{
						lengthScale = _SegmentCount + _EmptyTextureTiling.x;
					}
					else
					{
						lengthScale = _EmptyTextureTiling.x;
					}
					emptyTextureSample = tex2D(_EmptyTexture, rotateuv(float2(remap((transuvlen - _Radius) + fragmentLineWidth, float2(-.0001, +fragmentLineWidth * 2), float2(0.05, 0.95)),
						(1 / (1 - _Arc)) * (lengthScale / 2 - polarCoordinates(rotateuv(rotateuv(transuv, float2(0,0), radians(_Rotation - 90)), float2(0,0), radians(_EmptyTextureOffset.x)),float2(0,0), 1, lengthScale).y - lengthScale * _Arc)), float2(.5, .5),pi / 2));
				}
				else
				{
					emptyTextureSample = tex2D(_EmptyTexture, i.uv * float2(_EmptyTextureTiling.x, _EmptyTextureTiling.y) + float2(_EmptyTextureOffset.x, _EmptyTextureOffset.y));
				}

				float4 gradientModifiedEmptyColor = _EmptyColor * emptyGradient;

				emptyColor = lerp(gradientModifiedEmptyColor, emptyTextureSample * gradientModifiedEmptyColor, _EmptyTextureOpacity);

                #endif

                //EMPTY_FINAL
                float4 emptyFinal = emptyShape * emptyColor.a * float4(emptyColor.rgb, 1) * emptyNoise;


                //BORDER_SPACING
                float borderSpacing = mul(
                    1 - clamp((lines - (_SegmentSpacing - _BorderSpacing)) / fwidth(
                                                lines - (_SegmentSpacing -
                                                    _BorderSpacing)), 0, 1),
                    mul(clamp(remap((_SegmentSpacing - _BorderSpacing), float2(0, 0.001), float2(0, 1)), 0, 1),
                                                    round(clamp(
                                                        remap(_SegmentCount, float2(0, 1),
    float2(0, 0.5)), 0, 1))));

                float borderSpacing_gap = clamp(borderSpacing - finalArc, 0, 1);
                float borderSpacing_border = clamp(borderSpacing + finalArc, 0, 1);

                //BORDER_SHAPE
                //float borderCircle = 1 - clamp((abs(preCirc) - (_BorderWidth + _LineWidth)) / fwidth(preCirc), 0, 1);
                float borderDef = sdfCircle - _BorderWidth;
                float borderCircle = lerp(0, clamp(1 - borderDef / fwidth(borderDef), 0, 1), 1 - step(0.01, borderDef));
                float borderShape = clamp(
                    clamp((borderCircle - borderSpacing_border) - (removedsegments * _RemoveBorder), 0, 1) - clamp(
                        segcirc, 0, 1), 0, 1);

                //BORDER_TEXTURE/COLOR
                float4 borderColor = _BorderColor;

                #ifdef BORDER_TEXTURE_ON

				float4 borderTextureSample;
				if (_AlignBorderTexture >= 1)
				{
					float lengthScale;
					if (_BorderTextureScaleWithSegments >= 1)
					{
						lengthScale = _SegmentCount + _BorderTextureTiling.x;
					}
					else
					{
						lengthScale = _BorderTextureTiling.x;
					}
					borderTextureSample = tex2D(_BorderTexture, rotateuv(float2(remap((transuvlen - _Radius) + _BorderWidth + fragmentLineWidth, float2(-.0001, (_BorderWidth + fragmentLineWidth) * 2), float2(0.05, 0.95)),
						(1 / (1 - _Arc)) * (lengthScale / 2 - polarCoordinates(rotateuv(rotateuv(transuv, float2(0,0), radians(_Rotation - 90)), float2(0,0), radians(_BorderTextureOffset.x)),float2(0,0), 1, lengthScale).y - lengthScale * _Arc)), float2(.5, .5), pi / 2));
				}
				else
				{
					borderTextureSample = tex2D(_BorderTexture, i.uv * float2(_BorderTextureTiling.x, _BorderTextureTiling.y) + float2(_BorderTextureOffset.x, _BorderTextureOffset.y));
				}

				borderColor = lerp(_BorderColor, borderTextureSample * _BorderColor, _BorderTextureOpacity);

                #endif

                //BORDER_FINAL
                float4 borderFinal = borderShape * borderColor.a * float4(borderColor.rgb, 1);

                //SPACE_TEXTURE/COLOR
                float4 spaceColor = _SpaceColor;

                #ifdef SPACE_TEXTURE_ON

				float4 spaceTextureSample;
				if (_AlignSpaceTexture >= 1)
				{
					spaceTextureSample = tex2D(_SpaceTexture, float2(clamp((1 - sineLines - (1 - (_SegmentSpacing - _BorderSpacing))) * (.5 / (_SegmentSpacing - _BorderSpacing)), 0.01, 0.99),
						remap((transuvlen - _Radius) + _BorderWidth + fragmentLineWidth, float2(-.0001, (_BorderWidth + fragmentLineWidth) * 2), float2(0.05, 0.95))));
				}
				else
				{
					spaceTextureSample = tex2D(_SpaceTexture, i.uv * float2(_SpaceTextureTiling.x, _SpaceTextureTiling.y) + float2(_SpaceTextureOffset.x, _SpaceTextureOffset.y));
				}
				spaceColor = lerp(_SpaceColor, _SpaceColor * spaceTextureSample, _SpaceTextureOpacity);

                #endif


                //SPACE_SHAPE
                float spaceShape = clamp(borderCircle * borderSpacing_gap - (removedsegments * _RemoveBorder), 0, 1);

                //SPACE_FINAL
                float4 spaceFinal = spaceShape * spaceColor.a * float4(spaceColor.rgb, 1);

                //OVERLAY_TEXTURE/COLOR
                float4 overlayColor = _OverlayColor;

                #ifdef OVERLAY_TEXTURE_ON

				overlayColor = lerp(_OverlayColor, _OverlayColor * tex2D(_OverlayTexture, i.uv * float2(_OverlayTextureTiling.x, _OverlayTextureTiling.y) + float2(_OverlayTextureOffset.x, _OverlayTextureOffset.y)), _OverlayTextureOpacity);

                #endif

                //OVERLAY_FINAL
                float4 finalColor = innerCircleFinal + emptyFinal + spaceFinal + borderFinal;
                float4 overlayFinal = finalColor * overlayNoise * float4(1, 1, 1, overlayColor.a) * float4(
                    overlayColor.rgb, 1);

                //FINAL
                return overlayFinal * i.color;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}