using System.Collections.ObjectModel;
using System.Windows;

namespace Laevo.View.ActivityOverview.Labels
{
	internal interface ILabels
	{
		ObservableCollection<FrameworkElement> Labels { get; }

		void UpdatePositions();
	}
}
