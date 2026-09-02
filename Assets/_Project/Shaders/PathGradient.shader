Shader "CleanPath/PathGradient"
{
    Properties
    {
        _ColorNear ("Color Near", Color) = (0.93, 0.28, 0.55, 1)
        _ColorFar ("Color Far", Color) = (0.93, 0.28, 0.55, 0)
        _PlaneHalfLength ("Plane Half Length", Float) = 5
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "PathGradient"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _ColorNear;
                float4 _ColorFar;
                float _PlaneHalfLength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionOS : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionOS = input.positionOS.xyz;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float len = max(_PlaneHalfLength * 2.0, 0.001);
                float t = saturate((input.positionOS.z + _PlaneHalfLength) / len);
                return lerp(_ColorNear, _ColorFar, t);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
