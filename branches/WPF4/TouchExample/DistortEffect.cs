using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Effects;
using System.Windows;
using System.Windows.Media;

namespace WpfShaderTest
{
    class DistortEffect : ShaderEffect
    {
        public DistortEffect()
        {
            PixelShader = _shader;
            this.PaddingTop = 0;
            this.PaddingBottom = 0;
            this.PaddingLeft = 0;
            this.PaddingRight = 0;
            UpdateShaderValue(InputProperty);
        }

        public Brush Input
        {
            get {
                Brush b = (Brush)GetValue(InputProperty);
                return b; 
            }
            set {
                SetValue(InputProperty, value); 
            }
        }

        public static readonly DependencyProperty InputProperty =
            ShaderEffect.RegisterPixelShaderSamplerProperty(
                    "Input",
                    typeof(DistortEffect),
                    0);

        private static PixelShader _shader =
            new PixelShader() { UriSource = new Uri("C:\\distort.ps") };
    }
}
