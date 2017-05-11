Shader "FlatShader" {
    Properties {   
		_MainTex ("Base (RGB)", 2D) = "black" {}
    }
    SubShader {       
		Lighting Off
        Pass {
            BindChannels {
                Bind "Color", color
            }               
			SetTexture [_MainTex] {
				combine primary, primary*texture
             }
        }
    }
	 FallBack "ForwardBase"
}

