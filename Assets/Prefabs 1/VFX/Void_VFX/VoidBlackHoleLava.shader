Shader "Universal Render Pipeline/Custom/VoidBlackHoleLava"
{
    Properties
    {
        [Header(Singularity and Void Core)]
        _CoreColor ("Singularity Core (Pitch Black #05000a)", Color) = (0.020, 0.0, 0.039, 1.0)
        _PlasmaColor ("Dark Matter Plasma Swirls (#120326)", Color) = (0.071, 0.012, 0.149, 1.0)
        _CosmicPurple ("Dark Cosmic Purple Arcs (#6b11ff)", Color) = (0.420, 0.067, 1.0, 1.0)
        _CosmicCyan ("Event Horizon Cyan Glow (#00f0ff)", Color) = (0.0, 0.941, 1.0, 1.0)
        _RimContactColor ("Void Shore Contact Rim", Color) = (0.420, 0.067, 1.0, 1.0)

        [Header(Glow and Pulse)]
        _EmissionIntensity ("Void Emission Glow Intensity", Range(0.5, 8.0)) = 2.8
        _PulseSpeed ("Singularity Pulse Speed", Range(0.0, 5.0)) = 1.5
        _PulseAmount ("Singularity Pulse Amount", Range(0.0, 0.5)) = 0.22

        [Header(Accretion Vortex and Distortion)]
        _WorldScale ("Noise World Scale", Range(0.02, 1.0)) = 0.16
        _SwirlSpeed ("Vortex Swirl Speed", Range(0.0, 3.0)) = 0.75
        _DistortionStrength ("Procedural Noise Distortion", Range(0.0, 2.0)) = 0.55
        _SingularityRadius ("Singularity Core Radius", Range(0.01, 0.6)) = 0.22
        _EventHorizonWidth ("Event Horizon Ring Width", Range(0.01, 0.3)) = 0.06

        [Header(Surface Fluctuation)]
        _WaveHeight ("Surface Wave Height", Range(0.0, 0.3)) = 0.05
        _WaveFrequency ("Wave Frequency", Range(0.1, 3.0)) = 1.4
        _WaveSpeed ("Wave Speed", Range(0.0, 4.0)) = 1.2

        [Header(Depth Foam and Edge Glow)]
        _FoamDistance ("Contact Void Rim Width", Range(0.01, 1.2)) = 0.4
        _FoamSharpness ("Contact Void Rim Sharpness", Range(1.0, 10.0)) = 2.5
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

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float3 worldPos     : TEXCOORD1;
                float4 screenPos    : TEXCOORD2;
                float fogFactor     : TEXCOORD3;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _CoreColor;
                float4 _PlasmaColor;
                float4 _CosmicPurple;
                float4 _CosmicCyan;
                float4 _RimContactColor;
                float _EmissionIntensity;
                float _PulseSpeed;
                float _PulseAmount;
                float _WorldScale;
                float _SwirlSpeed;
                float _DistortionStrength;
                float _SingularityRadius;
                float _EventHorizonWidth;
                float _WaveHeight;
                float _WaveFrequency;
                float _WaveSpeed;
                float _FoamDistance;
                float _FoamSharpness;
            CBUFFER_END

            // Procedural Voronoi Hash
            float2 hash2(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
                return frac(sin(p) * 43758.5453123);
            }

            // Cellular Voronoi with organic motion
            float2 voidVoronoi2D(float2 p, float time)
            {
                float2 ip = floor(p);
                float2 fp = frac(p);
                float d1 = 8.0;
                float d2 = 8.0;

                for (int j = -1; j <= 1; j++)
                {
                    for (int i = -1; i <= 1; i++)
                    {
                        float2 b = float2(i, j);
                        float2 r = hash2(ip + b);
                        float2 animatedOffset = 0.5 + 0.45 * sin(time + 6.2831853 * r);
                        float2 diff = b + animatedOffset - fp;
                        float dist = length(diff);

                        if (dist < d1)
                        {
                            d2 = d1;
                            d1 = dist;
                        }
                        else if (dist < d2)
                        {
                            d2 = dist;
                        }
                    }
                }
                return float2(d1, d2);
            }

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                VertexPositionInputs baseInputs = GetVertexPositionInputs(input.positionOS.xyz);
                float3 wPos = baseInputs.positionWS;

                // Subtle undulation with slight gravitational sag toward the center
                float wave1 = sin(wPos.x * _WaveFrequency + _Time.y * _WaveSpeed) * 0.5;
                float wave2 = cos(wPos.z * (_WaveFrequency * 0.85) + _Time.y * (_WaveSpeed * 0.9)) * 0.5;
                float totalWave = (wave1 + wave2) * _WaveHeight;

                float3 posOS = input.positionOS.xyz;
                posOS.y += totalWave;

                VertexPositionInputs vertexInput = GetVertexPositionInputs(posOS);
                output.positionCS = vertexInput.positionCS;
                output.worldPos = vertexInput.positionWS;
                output.uv = input.uv;
                output.screenPos = ComputeScreenPos(output.positionCS);
                output.fogFactor = ComputeFogFactor(output.positionCS.z);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Polar vortex coordinate relative to UV center (0.5, 0.5)
                float2 centerUV = input.uv - float2(0.5, 0.5);
                float r = length(centerUV);
                float angle = atan2(centerUV.y, centerUV.x);

                // Gravitational vortex twist
                float vortexTwist = (1.0 / max(r + 0.12, 0.08)) * (_SwirlSpeed * _Time.y * 1.5);
                float distortedAngle = angle + vortexTwist;
                float2 polarUV = float2(cos(distortedAngle), sin(distortedAngle)) * r;

                // World-space noise distortion
                float2 wUV = input.worldPos.xz * _WorldScale;
                float2 vNoise1 = voidVoronoi2D(wUV * 1.5 + polarUV * _DistortionStrength, _Time.y * 0.8);
                float2 vNoise2 = voidVoronoi2D(polarUV * 3.0 + wUV * 0.5, _Time.y * 0.5);

                float plasmaNoise = saturate((vNoise1.y - vNoise1.x) * 1.3 + (1.0 - vNoise2.x) * 0.7);

                // Event Horizon and Singularity Core calculation
                // Core: radius < _SingularityRadius (Absolute Void Pitch Black)
                float coreCutoff = smoothstep(_SingularityRadius, _SingularityRadius + 0.04, r);

                // Thin glowing Event Horizon ring of cosmic cyan
                float eventHorizonDist = abs(r - _SingularityRadius);
                float eventHorizonGlow = 1.0 - saturate(eventHorizonDist / _EventHorizonWidth);
                eventHorizonGlow = pow(eventHorizonGlow, 3.0);

                // Dark matter plasma swirl gradient
                half4 col = lerp(_CoreColor, _PlasmaColor, coreCutoff);

                // Overlay Cosmic Purple filaments and noise arcs
                float purpleMask = smoothstep(0.35, 0.75, plasmaNoise) * coreCutoff;
                col = lerp(col, _CosmicPurple, purpleMask * 0.85);

                // Add Event Horizon intense cyan flare ring
                col.rgb += _CosmicCyan.rgb * (eventHorizonGlow * 1.6);

                // Subtle cosmic cyan arcs on plasma ridge highlights
                float cyanHighlight = smoothstep(0.78, 0.95, plasmaNoise) * coreCutoff;
                col.rgb += _CosmicCyan.rgb * (cyanHighlight * 0.75);

                // Pulsing emission
                float pulse = 1.0 + sin(_Time.y * _PulseSpeed) * _PulseAmount;
                // Singularity core remains dark, outer plasma radiates emission
                col.rgb *= lerp(0.35, pulse * _EmissionIntensity, coreCutoff);

                // Depth shore foam / contact edge glow
                #if defined(_ADDITIONAL_LIGHTS) || 1
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                float rawDepth = SampleSceneDepth(screenUV);
                float sceneDepth = LinearEyeDepth(rawDepth, _ZBufferParams);
                float surfaceDepth = LinearEyeDepth(input.positionCS.z, _ZBufferParams);
                float depthDiff = sceneDepth - surfaceDepth;

                if (depthDiff > 0.0001 && depthDiff < _FoamDistance)
                {
                    float foamFactor = 1.0 - saturate(depthDiff / _FoamDistance);
                    float foamWiggle = sin((input.worldPos.x + input.worldPos.z) * 4.0 + _Time.y * 3.5) * 0.15;
                    foamFactor = pow(saturate(foamFactor + foamWiggle), _FoamSharpness);
                    
                    half3 rimColor = lerp(_RimContactColor.rgb, _CosmicCyan.rgb, 0.45);
                    col.rgb = lerp(col.rgb, rimColor * (_EmissionIntensity * 1.4), foamFactor);
                }
                #endif

                // Fog integration
                col.rgb = MixFog(col.rgb, input.fogFactor);

                return col;
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
