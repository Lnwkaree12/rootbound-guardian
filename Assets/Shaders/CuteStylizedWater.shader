Shader "Universal Render Pipeline/Custom/CuteStylizedWater"
{
    Properties
    {
        [Header(Water Colors)]
        _ShallowColor ("Shallow Turquoise", Color) = (0.22, 0.82, 0.92, 0.85)
        _DeepColor ("Deep Sapphire", Color) = (0.05, 0.32, 0.65, 0.95)
        _SunHighlight ("Sunlight Warm Highlight", Color) = (0.50, 0.95, 1.0, 1.0)

        [Header(Animated Sun Caustics)]
        _CausticColor ("Caustic Web Highlight", Color) = (0.85, 1.0, 1.0, 0.90)
        _CausticScale ("Caustic World Scale", Range(0.1, 4.0)) = 0.95
        _CausticSpeed ("Caustic Flow Speed", Range(0.0, 2.0)) = 0.35
        _CausticStrength ("Caustic Intensity", Range(0.0, 1.5)) = 0.65

        [Header(Edge Contact Foam)]
        _FoamColor ("Contact Foam Color", Color) = (1.0, 1.0, 1.0, 0.95)
        _FoamDistance ("Contact Foam Depth Width", Range(0.01, 0.5)) = 0.12
        _MeshEdgeFoam ("Mesh Boundary Foam Width", Range(0.0, 0.1)) = 0.035

        [Header(Sun Specular Reflection)]
        _SpecularColor ("Sun Specular Color", Color) = (1.0, 1.0, 0.92, 1.0)
        _Glossiness ("Sun Specular Gloss", Range(10.0, 120.0)) = 55.0

        [Header(Gentle Surface Waves)]
        _WaveHeight ("Surface Wave Height", Range(0.0, 0.15)) = 0.025
        _WaveFrequency ("Wave Frequency", Range(0.1, 4.0)) = 1.5
        _WaveSpeed ("Wave Speed", Range(0.0, 4.0)) = 1.3
    }

    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "RenderPipeline"="UniversalPipeline" 
            "Queue"="Transparent" 
        }
        LOD 200

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back
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
                float4 screenPos    : TEXCOORD2;
                float fogFactor     : TEXCOORD3;
                float3 normalWS     : TEXCOORD4;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _ShallowColor;
                float4 _DeepColor;
                float4 _SunHighlight;
                float4 _CausticColor;
                float4 _FoamColor;
                float4 _SpecularColor;
                float _CausticScale;
                float _CausticSpeed;
                float _CausticStrength;
                float _FoamDistance;
                float _MeshEdgeFoam;
                float _Glossiness;
                float _WaveHeight;
                float _WaveFrequency;
                float _WaveSpeed;
            CBUFFER_END

            // Procedural Voronoi Hash
            float2 hash2(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
                return frac(sin(p) * 43758.5453123);
            }

            // Return min distance (F1) and second min distance (F2) for cellular caustics
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
                        float2 animatedOffset = 0.5 + 0.38 * sin(time + 6.2831853 * r);
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

                // Subtle fluid wave displacement
                float wave1 = sin(wPos.x * _WaveFrequency + _Time.y * _WaveSpeed) * 0.5;
                float wave2 = cos(wPos.z * (_WaveFrequency * 0.9) + _Time.y * (_WaveSpeed * 0.85)) * 0.5;
                float totalWave = (wave1 + wave2) * _WaveHeight;

                float3 posOS = input.positionOS.xyz;
                posOS.y += totalWave;

                VertexPositionInputs vertexInput = GetVertexPositionInputs(posOS);
                output.positionCS = vertexInput.positionCS;
                output.worldPos = vertexInput.positionWS;
                output.uv = input.uv;
                output.screenPos = ComputeScreenPos(output.positionCS);
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Flowing voronoi UVs in world coordinates
                float2 wUV = input.worldPos.xz * _CausticScale;
                float timeVal = _Time.y * _CausticSpeed;

                float2 v1 = cuteVoronoi2D(wUV + float2(timeVal * 0.45, timeVal * 0.35), _Time.y * 0.6);
                float2 v2 = cuteVoronoi2D(wUV * 1.35 - float2(timeVal * 0.3, -timeVal * 0.4), _Time.y * 0.45);

                // Deep to Shallow color variation driven by animated fluid current
                float currentFactor = saturate((1.0 - v1.x) * 0.65 + (1.0 - v2.x) * 0.35);
                half4 col = lerp(_DeepColor, _ShallowColor, currentFactor);

                // Additional sunlit warmth in core flow
                col.rgb = lerp(col.rgb, _SunHighlight.rgb, pow(currentFactor, 2.5) * 0.45);

                // Animated Caustics Network (Shimmering web lines)
                float causticEdges = (v1.y - v1.x) * 0.7 + (v2.y - v2.x) * 0.3;
                float causticMix = smoothstep(0.12, 0.28, causticEdges);
                col.rgb += _CausticColor.rgb * (causticMix * _CausticStrength);

                // Mesh Edge Foam (soft cartoon rim around the perimeter of the pond)
                float edgeX = min(input.uv.x, 1.0 - input.uv.x);
                float edgeY = min(input.uv.y, 1.0 - input.uv.y);
                float meshEdgeDist = min(edgeX, edgeY);

                if (meshEdgeDist < _MeshEdgeFoam)
                {
                    float meshFoam = 1.0 - (meshEdgeDist / _MeshEdgeFoam);
                    float edgeWiggle = sin((input.worldPos.x + input.worldPos.z) * 5.0 + _Time.y * 3.5) * 0.15;
                    meshFoam = pow(saturate(meshFoam + edgeWiggle), 1.8);
                    col.rgb = lerp(col.rgb, _FoamColor.rgb, meshFoam * 0.85);
                    col.a = max(col.a, meshFoam * 0.95);
                }

                // Depth-based contact foam (around rocks, pillars, or characters immersed in water)
                #if defined(_ADDITIONAL_LIGHTS) || 1
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                float rawDepth = SampleSceneDepth(screenUV);
                if (rawDepth > 0.0001 && rawDepth < 0.9999)
                {
                    float sceneDepth = LinearEyeDepth(rawDepth, _ZBufferParams);
                    float surfaceDepth = LinearEyeDepth(input.positionCS.z, _ZBufferParams);
                    float depthDiff = sceneDepth - surfaceDepth;

                    // Only apply if strictly inside thin contact zone
                    if (depthDiff > 0.015 && depthDiff < _FoamDistance)
                    {
                        float depthFoam = 1.0 - (depthDiff / _FoamDistance);
                        float depthWiggle = sin((input.worldPos.x + input.worldPos.z) * 4.0 + _Time.y * 3.0) * 0.15;
                        depthFoam = pow(saturate(depthFoam + depthWiggle), 2.0);
                        col.rgb = lerp(col.rgb, _FoamColor.rgb, depthFoam * _FoamColor.a);
                        col.a = max(col.a, depthFoam * 0.95);
                    }
                }
                #endif

                // Sun Specular reflection
                Light mainLight = GetMainLight();
                float3 viewDir = normalize(GetCameraPositionWS() - input.worldPos);
                float3 halfVector = normalize(mainLight.direction + viewDir);
                float NdotH = saturate(dot(float3(0, 1, 0), halfVector));
                float spec = pow(NdotH, _Glossiness);
                col.rgb += _SpecularColor.rgb * (spec * 0.75) * mainLight.color;

                // Unity Fog
                col.rgb = MixFog(col.rgb, input.fogFactor);

                return col;
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
