using Whathecode.System.ComponentModel.NotifyPropertyFactory.Attributes;
using Whathecode.System.Windows.Aspects.ViewModel;


namespace Laevo.ViewModel.Settings
{
	[ViewModel( typeof( Binding.Properties ), typeof( Binding.Commands ) )]
	class SettingsViewModel : AbstractViewModel
	{
		readonly Model.Settings _settings;

		[NotifyProperty( Binding.Properties.TimeLineRenderScale )]
		public float TimeLineRenderScale { get; set; }


		public SettingsViewModel( Model.Settings model )
		{
			_settings = model;

			TimeLineRenderScale = _settings.TimeLineRenderAtScale;
		}

		protected override void FreeUnmanagedResources()
		{
			// Nothing to do.
		}

		public override void Persist()
		{
			_settings.TimeLineRenderAtScale = TimeLineRenderScale;
		}
	}
}
