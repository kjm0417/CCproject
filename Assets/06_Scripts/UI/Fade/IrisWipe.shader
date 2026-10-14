// 씬 전환용 원형 와이프 (UI Image에 사용)
// _Radius 안쪽은 투명, 바깥쪽은 _Color로 덮음
Shader "UI/IrisWipe"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (0, 0, 0, 1)
        _Center ("Center (UV)", Vector) = (0.5, 0.5, 0, 0)
        _Radius ("Radius (화면 높이 기준)", Float) = 1
        _Aspect ("Aspect (width / height)", Float) = 1.7778
        _Softness ("Edge Softness", Float) = 0.005

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }

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
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            fixed4 _Color;
            float4 _Center;
            float _Radius;
            float _Aspect;
            float _Softness;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                //가로를 화면비만큼 늘려 찌그러지지 않은 원으로 계산
                float2 p = i.uv - _Center.xy;
                p.x *= _Aspect;
                float d = length(p);

                float a = smoothstep(_Radius - _Softness, _Radius, d);
                return fixed4(_Color.rgb, _Color.a * i.color.a * a);
            }
            ENDCG
        }
    }
}
