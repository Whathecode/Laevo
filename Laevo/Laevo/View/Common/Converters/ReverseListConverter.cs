using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;
using Laevo.ViewModel.Notification;


namespace Laevo.View.Common.Converters
{
	public class ReverseListConverter : MarkupExtension, IValueConverter
{
    private ObservableCollection<NotificationViewModel> _reversedList;

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return this;
    }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {

        _reversedList = new ObservableCollection<NotificationViewModel>();            

        var data = (ObservableCollection<NotificationViewModel>) value;
	    if ( data == null )
		    return _reversedList;

        for (var i = data.Count - 1; i >= 0; i--)
            _reversedList.Add(data[i]);

        data.CollectionChanged += DataCollectionChanged;

        return _reversedList;
    }

    void DataCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {                   
        var data = (ObservableCollection<NotificationViewModel>)sender;

        _reversedList.Clear();
        for (var i = data.Count - 1; i >= 0; i--)
            _reversedList.Add(data[i]);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
}
