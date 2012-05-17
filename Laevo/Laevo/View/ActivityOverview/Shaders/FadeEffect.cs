using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;


namespace Laevo.View.ActivityOverview.Shaders
{
	public class FadeEffect : ShaderEffect
	{
		static readonly PixelShader Shader
			= new PixelShader { UriSource = new Uri( @"pack://application:,,,/View/ActivityOverview/Shaders/FadeEffect.ps" ) };


		public FadeEffect()
		{
			PixelShader = Shader;

			UpdateShaderValue( InputProperty );
		}


		public static readonly DependencyProperty InputProperty = RegisterPixelShaderSamplerProperty( "Input", typeof( FadeEffect ), 0 );
		public Brush Input
		{
			get { return (Brush)GetValue( InputProperty ); }
			set { SetValue( InputProperty, value ); }
		}
	}
}
