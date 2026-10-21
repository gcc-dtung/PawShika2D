// Như UI/Default + vệt sáng chéo quét qua (chỉ trong phần sprite có alpha): icon Shop.
// Vệt tính theo vị trí trên màn hình (không theo UV) nên không lệch khi sprite nằm trong atlas,
// và mọi icon dùng chung 1 material sáng lần lượt từ trái sang phải. Chạy theo _Time, không cần script.
Shader "PawBlock/UI/Shine"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _ShineColor ("Shine Color", Color) = (1,1,1,0.85)
        _ShineWidth ("Shine Width (phần bề ngang màn hình)", Range(0.01, 0.5)) = 0.08
        _ShineSlant ("Shine Slant (âm = vệt hình /)", Range(-2, 2)) = -0.5
        _ShineSweep ("Sweep Time (giây quét hết màn)", Float) = 0.9
        _ShinePeriod ("Period (giây mỗi lần lặp)", Float) = 3

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
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
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
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

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
                float4 screenPos : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;
            fixed4 _ShineColor;
            float _ShineWidth;
            float _ShineSlant;
            float _ShineSweep;
            float _ShinePeriod;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.screenPos = ComputeScreenPos(OUT.vertex);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                half4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;

                // Toạ độ theo bề ngang màn hình (0..1), cộng thêm y để vệt nghiêng
                float2 uv = IN.screenPos.xy / IN.screenPos.w;
                float hw = _ScreenParams.y / _ScreenParams.x;
                float p = uv.x + uv.y * hw * _ShineSlant;
                float minP = min(0, hw * _ShineSlant) - _ShineWidth;
                float maxP = 1 + max(0, hw * _ShineSlant) + _ShineWidth;
                float t = saturate(fmod(_Time.y, _ShinePeriod) / _ShineSweep);
                float band = saturate(1 - abs(p - lerp(minP, maxP, t)) / _ShineWidth);
                color.rgb += _ShineColor.rgb * (band * band * _ShineColor.a);

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
