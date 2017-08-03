Shader "FlatShader" {
    Properties {   
		_MainTex ("Base (RGB)", 2D) = "black" {}
    }
    SubShader {       
		//Lighting Off
        Pass {
           // BindChannels {
           //     Bind "Color", color
           // }      

           Color (1,0,0,1) 
                   
			SetTexture [_MainTex] {
			//	combine previous, previous
                combine previous * texture
           //     combine previous * texture
           //     combine  texture
             }

           // SetTexture [_MainTex] {
           //     combine previous * texture
           // }
        }
    }
	// FallBack "ForwardBase"
}

