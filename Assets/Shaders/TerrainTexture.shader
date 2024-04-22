Shader"Custom/NewTerrain" {
    Properties {
        _GrassTexture ("Grass Texture", 2D) = "white" {}
        _RockTexture ("Rock Texture", 2D) = "white" {}
        _GrassSlopeThreshold ("Grass Slope Threshold", Range(0,1)) = .5
        _GrassBlendAmount ("Grass Blend Amount", Range(0,1)) = .5
    }
    SubShader {
        Tags { "RenderType"="Opaque" }
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
#include "UnityCG.cginc"
            
struct appdata_t
{
    float4 vertex : POSITION;
    float3 normal : NORMAL;
};
            
struct v2f
{
    float3 pos : TEXCOORD0;
    float4 vertex : SV_POSITION;
};
            
sampler2D _GrassTexture;
sampler2D _RockTexture;
            
float _GrassSlopeThreshold;
float _GrassBlendAmount;
            
v2f vert(appdata_t v)
{
    v2f o;
    // Repeat the textures more often by scaling the texture coordinates
    o.pos = v.normal * 5.0; // You can adjust the scaling factor as needed
    o.vertex = UnityObjectToClipPos(v.vertex);
    return o;
}
            
half4 frag(v2f i) : SV_Target
{
    half slope = 1 - i.pos.y;
    half grassBlendHeight = _GrassSlopeThreshold * (1 - _GrassBlendAmount);
    half grassWeight = 1 - saturate((slope - grassBlendHeight) / (_GrassSlopeThreshold - grassBlendHeight));
    
    // Use the fractional part of the texture coordinates to repeat the textures
    half4 grassColor = tex2D(_GrassTexture, frac(i.vertex.xy));
    half4 rockColor = tex2D(_RockTexture, frac(i.vertex.xy));
    
    half4 finalColor = lerp(rockColor, grassColor, grassWeight);
    return finalColor;
}
            ENDCG
        }
    }
}