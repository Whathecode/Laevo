using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Laevo.ViewModel.Notification.Binding;
using Whathecode.System.ComponentModel.NotifyPropertyFactory.Attributes;
using Whathecode.System.Windows.Aspects.ViewModel;
using Whathecode.System.Windows.Input.CommandFactory.Attributes;


namespace Laevo.ViewModel.Notification
{
	[Flags]
	public enum AnimationType
	{
		Slide,
		Fade
	}

	[ViewModel( typeof( Binding.Properties ), typeof( Commands ) )]
	public class NotificationViewModel
	{
		public event EventHandler<EventArgs> NewActivityCreating;

		public event EventHandler<EventArgs> NewTaskCreating;

		/// <summary>
		/// Event triggered when a notification pop-up panel needs to be closed.
		/// </summary>
		public event EventHandler<EventArgs> PopupHiding;

		public event EventHandler<EventArgs> NotificationRemoving;


		[NotifyProperty( Binding.Properties.Animation )]
		public AnimationType Animation { get; private set; }

		[NotifyProperty( Binding.Properties.Image )]
		public ImageSource Image { get; private set; }

		[NotifyProperty( Binding.Properties.Interrupter )]
		public string Interrupter { get; private set; }

		[NotifyProperty( Binding.Properties.Summary )]
		public string Summary { get; set; }

		[NotifyProperty( Binding.Properties.Index )]
		public int Index { get; set; }

		[NotifyProperty( Binding.Properties.ImportanceLevel )]
		public ImportanceLevel ImportanceLevel { get; set; }

		public NotificationViewModel()
		{
			// Initialize notification view model with the dummy data. 
			Interrupter = "Dominik Grondziowski";
			Animation = AnimationType.Fade;
			Summary = "Some Interruption summary. Some Interruption summary. Some Interruption summary Some Interruption summary." +
			          " Some Interruption summary. Some Interruption summary";
			var uriSource = new Uri( @"/Laevo;component/View/Common/Images/Alert.png", UriKind.Relative );
			Image = new BitmapImage( uriSource );
		}

		[CommandExecute( Commands.NewActivity )]
		public void NewActivity()
		{
			NotificationRemoving( this, new EventArgs() );
			NewActivityCreating( this, new EventArgs() );
			PopupHiding( this, new EventArgs() );
		}

		[CommandExecute( Commands.NewTask )]
		public void NewTask()
		{
			NotificationRemoving( this, new EventArgs() );
			NewTaskCreating( this, new EventArgs() );
			PopupHiding( this, new EventArgs() );
		}

		[CommandExecute( Commands.Dissmiss )]
		public void Dissmiss()
		{
			PopupHiding( this, new EventArgs() );
		}

		[CommandExecute( Commands.Remove )]
		public void Remove()
		{
			NotificationRemoving( this, new EventArgs() );
			PopupHiding( this, new EventArgs() );
		}
	}
}