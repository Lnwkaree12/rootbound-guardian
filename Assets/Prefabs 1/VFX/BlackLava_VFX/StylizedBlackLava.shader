Shader "Universal Render Pipeline/Custom/StylizedBlackLava"
{
    Properties
    {
        [Header(Black Lava Palette)]
        _BaseColor ("Crust (Obsidian Pitch Black #08080a)", Color) = (0.031, 0.031, 0.039, 1.0)
        _MidColor ("Mid Magma (Dark Molten Charcoal #1a1421)", Color) = (0.102, 0.078, 0.129, 1.0)
        _CoreColor ("Core Blobs (Molten Dark Core #2e1f3d)", Color) = (0.180, 0.122, 0.239, 1.0)
        _HighlightColor ("Vein Glint / Hot Seams (#52336b)", Color) = (0.322, 0.200, 0.420, 1.0)
        _RimColor ("Edge Contact Foam / Rim (#241f2e)", Color) = (0.141, 0.122, 0.180, 1.0)

        [Header(Glow and Emission)]
        _EmissionIntensity ("Emission Glow Intensity", Range(0.1, 4.0)) = 1.2
        _PulseSpeed ("Breathe Glow Speed", Range(0.0, 5.0)) = 1.4
        _PulseAmount ("Breathe Glow Amount", Range(0.0, 0.5)) = 0.15

        [Header(Specular Wet Liquid Sheen)]
        _Glossiness ("Glossy Wetness", Range(1.0, 64.0)) = 24.0
        _SpecularStrength ("Specular Highlight Strength", Range(0.0, 2.0)) = 0.85

        [Header(Stylized Blobs Pattern)]
        _WorldScale ("Lava Scale (World Units)", Range(0.02, 1.0)) = 0.18
        _FlowSpeed ("Flow Motion Speed", Range(0.0, 1.5)) = 0.22
        _FlowDirection ("Flow Direction (X, Z)", Vector) = (0.35, 0.93, 0, 0)
        
        [Header(Toon Cutoff Steps)]
        _CrustStep ("Crust / Mid Magma Boundary", Range(0.1, 0.9)) = 0.42
        _OrangeStep ("Mid / Core Blob Boundary", Range(0.1, 0.9)) = 0.62
        _HotSpotStep ("Core / Hot Seam Boundary", Range(0.1, 0.9)) = 0.78
        _StepSmoothness ("Cartoon Edge Softness", Range(0.01, 0.2)) = 0.05

        [Header(Gentle Surface Waves)]
        _WaveHeight ("Wave Height", Range(0.0, 0.3)) = 0.06
        _WaveFrequency ("Wave Frequency", Range(0.1, 3.0)) = 1.2
        _WaveSpeed ("Wave Speed", Range(0.0, 4.0)) = 1.5

        [Header(Contact Edge Foam)]
        _FoamDistance ("Contact Foam Width", Range(0.01, 1.2)) = 0.4
        _FoamSharpness ("Contact Foam Sharpness", Range(1.0, 10.0)) = 2.5
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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
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
                float3 worldNormal  : NORMAL;
                float4 screenPos    : TEXCOORD2;
                float fogFactor     : TEXCOORD3;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _MidColor;
                float4 _CoreColor;
                float4 _HighlightColor;
                float4 _RimColor;
                float4 _FlowDirection;
                float _EmissionIntensity;
                float _PulseSpeed;
                float _PulseAmount;
                float _Glossiness;
                float _SpecularStrength;
                float _WorldScale;
                float _FlowSpeed;
                float _CrustStep;
                float _OrangeStep;
                float _HotSpotStep;
                float _StepSmoothness;
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

            // Return min distance (F1) and second min distance (F2) for cartoon cellular edges
            float2 cuteVoronoi2D(float2 p, float time)
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
                        float2 animatedOffset = 0.5 + 0.42 * sin(time + 6.2831853 * r);
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

                // Soft sine wave motion for gentle squishy liquid feel
                float wave1 = sin(wPos.x * _WaveFrequency + _Time.y * _WaveSpeed) * 0.5;
                float wave2 = cos(wPos.z * (_WaveFrequency * 0.85) + _Time.y * (_WaveSpeed * 0.9)) * 0.5;
                float totalWave = (wave1 + wave2) * _WaveHeight;

                float3 posOS = input.positionOS.xyz;
                posOS.y += totalWave;

                VertexPositionInputs vertexInput = GetVertexPositionInputs(posOS);
                output.positionCS = vertexInput.positionCS;
                output.worldPos = vertexInput.positionWS;
                output.worldNormal = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;
                output.screenPos = ComputeScreenPos(output.positionCS);
                output.fogFactor = ComputeFogFactor(output.positionCS.z);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // World-space UVs so the pattern is completely independent of plane stretching
                float2 wUV = input.worldPos.xz * _WorldScale;
                float2 flowDir = normalize(_FlowDirection.xy + float2(0.001, 0.001));
                float timeVal = _Time.y * _FlowSpeed;

                // Main flowing cells
                float2 uv1 = wUV + flowDir * timeVal;
                float2 v1 = cuteVoronoi2D(uv1, _Time.y * 0.6);

                // Secondary swirling cells drifting at a slight angle
                float2 uv2 = wUV * 1.45 - float2(flowDir.y, -flowDir.x) * (timeVal * 0.4);
                float2 v2 = cuteVoronoi2D(uv2 + float2(2.3, 5.7), _Time.y * 0.4);

                // Cel / blob factor: F2 - F1 gives magma cell borders, (1 - F1) gives glowing blob centers
                float blobCenter = 1.0 - v1.x;
                float cellBorder = saturate(v1.y - v1.x);
                float secondaryBlob = 1.0 - v2.x;

                // Combined cute pattern: soft rounded molten magma blobs floating on surface
                float magmaHeat = blobCenter * 0.65 + secondaryBlob * 0.25 + cellBorder * 0.2;

                // Toon color banding with smoothstep for clean minimal aesthetic
                half4 col = _BaseColor;

                // Step 1: Dark Mid-magma layer
                float midMask = smoothstep(_CrustStep - _StepSmoothness, _CrustStep + _StepSmoothness, magmaHeat);
                col = lerp(col, _MidColor, midMask);

                // Step 2: Molten core blobs
                float coreMask = smoothstep(_OrangeStep - _StepSmoothness, _OrangeStep + _StepSmoothness, magmaHeat);
                col = lerp(col, _CoreColor, coreMask);

                // Step 3: Hot glint / highlight seams in the center of the molten blobs
                float hotSpotMask = smoothstep(_HotSpotStep - _StepSmoothness, _HotSpotStep + _StepSmoothness, magmaHeat);
                col = lerp(col, _HighlightColor, hotSpotMask);

                // Gentle breathing pulsation for molten lava
                float pulse = 1.0 + sin(_Time.y * _PulseSpeed) * _PulseAmount;
                col.rgb *= pulse * _EmissionIntensity;

                // Wet glossy liquid specular highlight
                float3 viewDir = normalize(GetCameraPositionWS() - input.worldPos);
                Light mainLight = GetMainLight();
                float3 halfDir = normalize(mainLight.direction + viewDir);
                float NdotH = saturate(dot(input.worldNormal, halfDir));
                float spec = pow(NdotH, _Glossiness) * _SpecularStrength;
                col.rgb += half3(spec, spec, spec);

                // Contact shore foam / edge glow (near rocks, pillars, walls)
                #if defined(_ADDITIONAL_LIGHTS) || 1
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                float rawDepth = SampleSceneDepth(screenUV);
                float sceneDepth = LinearEyeDepth(rawDepth, _ZBufferParams);
                float surfaceDepth = LinearEyeDepth(input.positionCS.z, _ZBufferParams);
                float depthDiff = sceneDepth - surfaceDepth;

                if (depthDiff > 0.0001 && depthDiff < _FoamDistance)
                {
                    float foamFactor = 1.0 - saturate(depthDiff / _FoamDistance);
                    float foamWiggle = sin((input.worldPos.x + input.worldPos.z) * 3.5 + _Time.y * 3.0) * 0.12;
                    foamFactor = pow(saturate(foamFactor + foamWiggle), _FoamSharpness);
                    col.rgb = lerp(col.rgb, _RimColor.rgb * (_EmissionIntensity * 1.25), foamFactor);
                }
                #endif

                // Unity Fog
                col.rgb = MixFog(col.rgb, input.fogFactor);

                return col;
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
