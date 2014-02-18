Shader "Custom/Vertex Color"
{
    Properties {
    }
    SubShader 
    {
        Tags { "RenderType" = "Opaque"}       
        pass
        {
        
        	//CHECK THIS LINK!!!
        	//http://answers.unity3d.com/questions/189584/how-to-get-vertex-color-in-a-cg-shader.html
        	
            CGPROGRAM
            #pragma vertex wfiVertCol
            #pragma fragment passThrough
            #include "UnityCG.cginc"
 
            struct VertOut
            {
                float4 position : POSITION;
                float4 color : COLOR;
            };
 
            struct VertIn
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
            };
 
            VertOut wfiVertCol(VertIn input, float3 normal : NORMAL)
            {
                VertOut output;
                output.position = mul(UNITY_MATRIX_MVP,input.vertex);
                output.color = input.color;
                return output;
            }
 
            struct FragOut
            {
                float4 color : COLOR;
            };
 
            FragOut passThrough(float4 color : COLOR)
            {
                FragOut output;
                output.color = color;
                return output;
            }
            ENDCG
 
        }
    }
    FallBack "Diffuse"
}