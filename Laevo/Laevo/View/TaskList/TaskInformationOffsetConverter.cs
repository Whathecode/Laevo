using System;
using System.Windows;
using System.Windows.Controls;
using Whathecode.System.Windows.Data;


namespace Laevo.View.TaskList
{
	class TaskInformationOffsetConverter : AbstractMultiValueConverter<object, Thickness>
	{
		public override Thickness Convert( object[] values )
		{
			double taskIconWidth = (double)values[ 0 ];
			double taskInfoWidth = (double)values[ 1 ];
			double taskInfoHeight = (double)values[ 2 ];
			double offset = taskInfoWidth - taskIconWidth;

			return new Thickness( -offset, -taskInfoHeight, 0, 0 );
		}

		public override object[] ConvertBack( Thickness value )
		{
			throw new NotSupportedException();
		}
	}
}
