// Tutorial dimmer for UGUI Image: fills the entire rectangle with _Color and cuts up to ~8
// rectangular holes (highlighted targets) with a soft edge of _Feather. Holes are given in the
// Image rect normalized UV (0..1) via the _Holes array; the active count is _HoleCount (both
// set by TutorialOverlayView through SetVectorArray/SetInt). Inside a hole alpha = 0 (target is
// fully visible), outside = _Color.a. The standard UGUI block (Mask / RectMask2D) is preserved.
Shader "Project/UI/TutorialCutout"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Dim tone (multiplied by Image.color)", Color) = (0, 0, 0, 0.75)
        _Feather ("Hole edge softness (UV fraction)", Range(0.0001, 0.1)) = 0.01

        [NoScaleOffset] _MaskTex ("Hole mask (sprite alpha; set by TutorialOverlayView)", 2D) = "white" {}
        _MaskRect ("Mask region in UV (set by TutorialOverlayView)", Vector) = (0, 0, 0, 0)
        _MaskUv ("Mask sprite sub-UV (set by TutorialOverlayView)", Vector) = (0, 0, 1, 1)
        _MaskEnabled ("Mask enabled (set by TutorialOverlayView)", Float) = 0

        // --- Standard UGUI block (Mask / RectMask2D) ---
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #define TUTORIAL_MAX_HOLES 8

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float _Feather;

            // xy = min (bottom-left), zw = max (top-right), in Image rect UV (0..1).
            float4 _Holes[TUTORIAL_MAX_HOLES];
            int _HoleCount;

            // Shaped mask hole (non-rectangular): _MaskTex alpha inside _MaskRect cuts the dim.
            sampler2D _MaskTex;
            float4 _MaskRect;   // Mask region in Image rect UV (0..1), xy=min zw=max.
            float4 _MaskUv;     // Sprite sub-UV in its texture (atlas-aware), xy=min zw=max.
            int _MaskEnabled;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = v.texcoord;
                OUT.color = v.color * _Color;
                return OUT;
            }

            // 1 outside all holes, 0 inside a hole, with a soft _Feather-wide transition.
            float HoleMask(float2 uv)
            {
                float mask = 1.0;
                for (int i = 0; i < _HoleCount; i++)
                {
                    float4 hole = _Holes[i];
                    // Signed "outside": <0 inside the rectangle, >0 outside.
                    float2 d = max(hole.xy - uv, uv - hole.zw);
                    float outside = max(d.x, d.y);
                    float m = smoothstep(0.0, _Feather, outside);
                    mask = min(mask, m);
                }
                return mask;
            }

            // 1 outside the shaped hole, 0 inside (where the sprite is opaque), with a soft edge.
            float MaskHole(float2 uv)
            {
                if (_MaskEnabled == 0)
                {
                    return 1.0;
                }

                if (uv.x < _MaskRect.x || uv.x > _MaskRect.z || uv.y < _MaskRect.y || uv.y > _MaskRect.w)
                {
                    return 1.0;
                }

                float2 local = (uv - _MaskRect.xy) / max(_MaskRect.zw - _MaskRect.xy, float2(1e-5, 1e-5));
                float2 maskUv = lerp(_MaskUv.xy, _MaskUv.zw, local);
                float a = tex2D(_MaskTex, maskUv).a;
                // Opaque sprite (a~1) -> hole (0); transparent (a~0) -> dim (1).
                return 1.0 - smoothstep(0.5 - _Feather, 0.5 + _Feather, a);
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                half4 color = IN.color;
                color.a *= min(HoleMask(IN.texcoord), MaskHole(IN.texcoord));

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                return color;
            }
        ENDCG
        }
    }
}
