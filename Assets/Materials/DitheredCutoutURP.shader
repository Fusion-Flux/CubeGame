Shader "Custom/DitheredCutoutURP"
{
    Properties
    {
        _MainTex    ("Main Texture (Albedo)", 2D) = "white" {}
        _Color      ("Tint Color", Color) = (1,1,1,1)
        _DitherTex  ("Dither Texture", 2D) = "white" {}
        _Cutoff     ("Alpha Cutoff", Range(0,1)) = 0.5

        // Distance-based fade
        _FadeStart  ("Fade Start Distance", Range(0,20)) = 5
        _FadeEnd    ("Fade End Distance", Range(0,20)) = 0
    }

    SubShader
    {
        // Force this into an alpha-tested queue and render type.
        Tags
        {
            "Queue" = "AlphaTest"
            "RenderType" = "TransparentCutout"
        }

        LOD 200

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            // ---------------------------------------------------------------
            //  1) Pragmas
            // ---------------------------------------------------------------
            #pragma vertex vert
            #pragma fragment frag

            // Include URP core/lighting utilities
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // ---------------------------------------------------------------
            //  2) Textures & Samplers
            // ---------------------------------------------------------------
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            TEXTURE2D(_DitherTex);
            SAMPLER(sampler_DitherTex);

            // ---------------------------------------------------------------
            //  3) Properties (mirrors the shader Properties block)
            // ---------------------------------------------------------------
            float4 _Color;         // RGBA Tint
            float  _Cutoff;        // Not strictly necessary unless you do manual alpha test

            float  _FadeStart;     // Distance at which dither fade-out starts
            float  _FadeEnd;       // Distance at which it's fully faded

            // ---------------------------------------------------------------
            //  4) Vertex data struct
            // ---------------------------------------------------------------
            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 worldPos    : TEXCOORD1;
            };

            // ---------------------------------------------------------------
            //  5) Vertex shader
            // ---------------------------------------------------------------
            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                // Transform object position -> clip space
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS);
                // Just pass the mesh UV along directly
                OUT.uv = IN.uv;
                // Also keep world position so we can measure distance to camera
                OUT.worldPos = TransformObjectToWorld(IN.positionOS);
                return OUT;
            }

            // ---------------------------------------------------------------
            //  6) Fragment shader
            // ---------------------------------------------------------------
            half4 frag (Varyings IN) : SV_Target
            {
                // (A) Sample main texture, apply color tint
                float4 baseColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * _Color;

                // (B) Distance-based fade
                float3 cameraPosWS = GetCameraPositionWS();
                float distanceToCam = distance(IN.worldPos, cameraPosWS);

                // Start fully visible
                float alphaFade = 1.0;

                // If inside the fade zone, linearly reduce alpha
                if (distanceToCam < _FadeStart)
                {
                    float fadeRange    = _FadeStart - _FadeEnd;
                    float distIntoFade = distanceToCam - _FadeEnd; 
                    alphaFade = saturate(distIntoFade / fadeRange);
                }

                // Combine fade factor with texture alpha
                baseColor.a *= alphaFade;

                // (C) Dither test. We’ll tile the dither texture a bit:
                float2 ditherUV = IN.uv * 8.0;
                float ditherVal = SAMPLE_TEXTURE2D(_DitherTex, sampler_DitherTex, ditherUV).r;

                // If ditherVal > alpha, discard pixel => “cutout” effect
                if (ditherVal > baseColor.a)
                    discard;

                // Return final color. We set alpha=1.0 because we’re discarding anyway.
                return half4(baseColor.rgb, 1.0);
            }
            ENDHLSL
        }
    }
}
