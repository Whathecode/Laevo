using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Laevo.ViewModel.User.Binding;
using Microsoft.Win32;
using Whathecode.System.ComponentModel.NotifyPropertyFactory.Attributes;
using Whathecode.System.Windows.Aspects.ViewModel;
using Whathecode.System.Windows.Input.CommandFactory.Attributes;


namespace Laevo.ViewModel.User
{
	[ViewModel( typeof( Binding.Properties ), typeof( Commands ) )]
	public class UserViewModel : AbstractViewModel
	{
		static readonly Size ImageSize = new Size( 30, 30 );

		internal readonly Model.User User;

		[NotifyProperty( Binding.Properties.Name )]
		public string Name { get; set; }

		[NotifyProperty( Binding.Properties.Image )]
		public ImageSource Image { get; private set; }

		public UserViewModel( Model.User user )
		{
			User = user;
			Name = user.Name;
			Image = user.Image;
		}

		[CommandExecute( Commands.ChooseImage )]
		public void ChoosePhoto()
		{
			var choosePhoto = new OpenFileDialog
			{
				DefaultExt = ".png",
				Filter = "Images (*.png, *.jpeg, *.jpg, *.gif) | *.png; *.jpeg; *.jpg; *.gif"
			};

			if ( choosePhoto.ShowDialog() == true )
			{
				var bitmap = BitmapHelper.ChangeBitmapDpi( new BitmapImage( new Uri( choosePhoto.FileName ) ) );
				var resizedBitmap = BitmapHelper.ResizeBitmap( bitmap, ImageSize );
				var croppedBitmap = BitmapHelper.CroppBitmap( resizedBitmap, ImageSize );
				Image = croppedBitmap;
			}
		}


		[CommandExecute( Commands.RemoveImage )]
		public void RemoveImage()
		{
			Image = null;
		}

		protected override void FreeUnmanagedResources()
		{
			// Nothing to do.
		}

		public override void Persist()
		{
			User.Name = Name;
			User.Image = Image;
		}
	}
}