Shader "Universal Render Pipeline/Custom/StylizedWavingGrass"
{
    Properties
    {
        [Header(Grass Colors)]
        _BaseColor ("Root / Base Color", Color) = (0.28, 0.44, 0.16, 1.0)
        _TopColor ("Tip / Sunny Color", Color) = (0.58, 0.78, 0.24, 1.0)
        _Smoothness ("Smoothness", Range(0.0, 1.0)) = 0.15

        [Header(Wind Sway Settings)]
        _WindSpeed ("Wind Speed", Range(0.1, 8.0)) = 2.4
        _WindStrength ("Wind Strength / Amplitude", Range(0.0, 1.0)) = 0.22
        _WindTurbulence ("Spatial Gust Turbulence", Range(0.1, 5.0)) = 1.6
        _WindDirection ("Wind Direction (X, Z)", Vector) = (1.0, 0.35, 0, 0)
        _HeightThreshold ("Grass Height Threshold", Range(0.1, 1.0)) = 0.42
        _FlutterSpeed ("Tip Flutter Speed", Range(0.5, 12.0)) = 4.5
        _FlutterAmount ("Tip Flutter Amount", Range(0.0, 0.5)) = 0.08
    }

    SubShader
    {
        Tags 
        { 
            "RenderType"="Opaque" 
            "RenderPipeline"="UniversalPipeline" 
            "Queue"="Geometry" 
        }
        LOD 200
        Cull Off // Double-sided grass blades

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 uv           : TEXCOORD0;
                float4 color        : COLOR;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
                float  heightRatio  : TEXCOORD2;
                float  fogFactor    : TEXCOORD3;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _TopColor;
                float4 _WindDirection;
                float  _Smoothness;
                float  _WindSpeed;
                float  _WindStrength;
                float  _WindTurbulence;
                float  _HeightThreshold;
                float  _FlutterSpeed;
                float  _FlutterAmount;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                // 1. Calculate height mask (root = 0, tip = 1)
                float rawHeight = max(0.0, input.positionOS.y);
                float hRatio = saturate(rawHeight / _HeightThreshold);
                // Quadratic curve: roots stay firmly planted, upper blades sway naturally
                float swayMask = hRatio * hRatio;

                // 2. World position for spatial wave offset
                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);

                // 3. Multi-frequency wind sway calculation
                float2 wDir = normalize(_WindDirection.xy);
                float spatialPhase = worldPos.x * _WindTurbulence + worldPos.z * (_WindTurbulence * 0.75);

                // Main gentle gust wave
                float waveMain = sin(_Time.y * _WindSpeed + spatialPhase);
                // Secondary cross wave for organic wobble
                float waveCross = cos(_Time.y * (_WindSpeed * 0.8) - spatialPhase * 0.6) * 0.45;
                // High-frequency tip flutter
                float waveFlutter = sin(_Time.y * _FlutterSpeed + spatialPhase * 2.2) * _FlutterAmount;

                float totalDisplacement = (waveMain + waveCross + waveFlutter) * _WindStrength * swayMask;

                // 4. Apply displacement along wind direction in Object Space
                input.positionOS.x += wDir.x * totalDisplacement;
                input.positionOS.z += wDir.y * totalDisplacement;
                // Slight downward compression when bending strongly (preserves perceived length)
                input.positionOS.y -= abs(totalDisplacement) * 0.3 * swayMask;

                // 5. Output positions & normals
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.heightRatio = hRatio;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Gradient color from root to tip
                half3 albedo = lerp(_BaseColor.rgb, _TopColor.rgb, input.heightRatio);

                // Sample Main Light
                Light mainLight = GetMainLight();
                half3 lightDir = normalize(mainLight.direction);
                half NdotL = saturate(dot(normalize(input.normalWS), lightDir));
                
                // Two-sided soft diffuse lighting
                half wrapLight = NdotL * 0.6 + 0.4;
                half3 directLight = albedo * mainLight.color * wrapLight * mainLight.shadowAttenuation;

                // Ambient light
                half3 ambient = SampleSH(input.normalWS) * albedo;

                half3 finalColor = directLight + ambient;

                // Apply Fog
                finalColor = MixFog(finalColor, input.fogFactor);

                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }

        // Shadow Caster Pass
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Off

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _TopColor;
                float4 _WindDirection;
                float  _Smoothness;
                float  _WindSpeed;
                float  _WindStrength;
                float  _WindTurbulence;
                float  _HeightThreshold;
                float  _FlutterSpeed;
                float  _FlutterAmount;
            CBUFFER_END

            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output;
                float rawHeight = max(0.0, input.positionOS.y);
                float hRatio = saturate(rawHeight / _HeightThreshold);
                float swayMask = hRatio * hRatio;

                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);
                float2 wDir = normalize(_WindDirection.xy);
                float spatialPhase = worldPos.x * _WindTurbulence + worldPos.z * (_WindTurbulence * 0.75);
                float totalDisplacement = (sin(_Time.y * _WindSpeed + spatialPhase) + cos(_Time.y * (_WindSpeed * 0.8) - spatialPhase * 0.6) * 0.45) * _WindStrength * swayMask;

                input.positionOS.x += wDir.x * totalDisplacement;
                input.positionOS.z += wDir.y * totalDisplacement;
                input.positionOS.y -= abs(totalDisplacement) * 0.3 * swayMask;

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _MainLightPosition.xyz));
                return output;
            }

            half4 ShadowPassFragment(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        // DepthOnly Pass for URP Post-Processing and SSAO/Depth Texture
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }

            ZWrite On
            ColorMask R
            Cull Off

            HLSLPROGRAM
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _TopColor;
                float4 _WindDirection;
                float  _Smoothness;
                float  _WindSpeed;
                float  _WindStrength;
                float  _WindTurbulence;
                float  _HeightThreshold;
                float  _FlutterSpeed;
                float  _FlutterAmount;
            CBUFFER_END

            Varyings DepthOnlyVertex(Attributes input)
            {
                Varyings output;
                float rawHeight = max(0.0, input.positionOS.y);
                float hRatio = saturate(rawHeight / _HeightThreshold);
                float swayMask = hRatio * hRatio;

                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);
                float2 wDir = normalize(_WindDirection.xy);
                float spatialPhase = worldPos.x * _WindTurbulence + worldPos.z * (_WindTurbulence * 0.75);
                float totalDisplacement = (sin(_Time.y * _WindSpeed + spatialPhase) + cos(_Time.y * (_WindSpeed * 0.8) - spatialPhase * 0.6) * 0.45) * _WindStrength * swayMask;

                input.positionOS.x += wDir.x * totalDisplacement;
                input.positionOS.z += wDir.y * totalDisplacement;
                input.positionOS.y -= abs(totalDisplacement) * 0.3 * swayMask;

                output.positionCS = TransformWorldToHClip(TransformObjectToWorld(input.positionOS.xyz));
                return output;
            }

            half4 DepthOnlyFragment(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
