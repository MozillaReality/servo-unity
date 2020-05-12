Shader "TextureOverlayNoLight" {
	Properties {
		_Color ("Main Color", Color) = (1,1,1,1)
		_MainTex ("Base (RGB)", 2D) = "white" { }
	}
	SubShader {
		Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
		Pass {
			Material {
				Diffuse [_Color]
			}
			Lighting Off
			ZWrite Off
		    ZTest Always
			Blend SrcAlpha OneMinusSrcAlpha
			SeparateSpecular Off
			SetTexture [_MainTex] {
				constantColor [_Color]
				Combine texture * constant, texture * constant
			}
		}
	}
}