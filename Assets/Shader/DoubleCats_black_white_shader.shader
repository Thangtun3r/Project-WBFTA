Shader "Effect/ImpactFrameURP"
{
	Properties
	{
		_Threshold("Threshold", Range(0, 1)) = 0.5
		_SmoothWidth("Smooth Width", Range(0.0001, 0.2)) = 0.02
		_Invert("Invert", Range(0, 1)) = 0
		_WhiteColor("White Color", Color) = (1,1,1,1)
		_BlackColor("Black Color", Color) = (0,0,0,1)
	}

	SubShader
	{
		Tags
		{
			"RenderPipeline"="UniversalPipeline"
			"Queue"="Overlay"
		}

		Pass
		{
			Name "ImpactFrame"
			ZWrite Off
			ZTest Always
			Cull Off
			Blend Off

			HLSLPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

			float _Threshold;
			float _SmoothWidth;
			float _Invert;
			float4 _WhiteColor;
			float4 _BlackColor;
			float _ImpactFrameIntensity;

			half4 Frag(Varyings input) : SV_Target
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

				float2 uv = input.texcoord;
				half4 source = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

				half gray = dot(source.rgb, half3(0.299h, 0.587h, 0.114h));
				half mask = smoothstep(_Threshold, _Threshold + _SmoothWidth, gray);
				mask = lerp(mask, 1.0h - mask, saturate(_Invert));

				half4 impactColor = lerp(_BlackColor, _WhiteColor, mask);
				half intensity = saturate(_ImpactFrameIntensity);
				return lerp(source, impactColor, intensity);
			}
			ENDHLSL
		}
	}
}
