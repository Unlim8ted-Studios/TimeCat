Shader "TimeCat/Particles/Unlit" {
 Properties {
  [PerRendererData] _MainTex ("Particle texture", 2D) = "white" {}
  [HDR] _Color ("Tint", Color) = (1,1,1,1)
  [HideInInspector] _SrcBlend ("Source blend", Float) = 5
  [HideInInspector] _DstBlend ("Destination blend", Float) = 10
 }
 SubShader {
  Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
  Pass {
   Name "Particles2D"
   Tags { "LightMode"="Universal2D" }
   Blend [_SrcBlend] [_DstBlend]
   Cull Off ZWrite Off ZTest LEqual
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma target 2.0
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
   CBUFFER_START(UnityPerMaterial)
    float4 _MainTex_ST; half4 _Color;
   CBUFFER_END
   struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
   struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
   Varyings vert(Attributes input) { Varyings output; output.positionCS=TransformObjectToHClip(input.positionOS.xyz); output.uv=TRANSFORM_TEX(input.uv,_MainTex); output.color=input.color*_Color; return output; }
   half4 frag(Varyings input):SV_Target { return SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,input.uv)*input.color; }
   ENDHLSL
  }
 }
 Fallback "Sprites/Default"
}
