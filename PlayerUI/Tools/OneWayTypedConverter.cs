using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace PlayerUI
{

	/// <summary>
	/// Allows for easy building of one way XAML binding converters without adding them to resources.
	/// </summary>
	/// <typeparam name="Tinput">input type</typeparam>
	/// <typeparam name="Toutput">output type</typeparam>
	/// <example>
	/// XAML:
	/// &lt; MenuItem Header="menu" Visibility="{Binding AnaliticsMenuActive, Mode=OneWay, Converter={local:MyBooleanToVisibilityConverter}, FallbackValue=Hidden}" &gt;
	/// assuming local is a defined namespace where the binding is defined:
	/// &lt; Window ... xmlns:local="clr-namespace:PlayerUI" /&gt;
	/// C#:
	/// [ValueConversion(typeof(bool), typeof(System.Windows.Visibility)]		// marker attribute, not really needed
	/// public class MyBooleanToVisibilityConverter : OneWayTypedConverter &lt; bool, System.Windows.Visibility &gt;
	///	{
	///		protected override Visibility Convert(bool value, object parameter, CultureInfo culture) { return value ? Visibility.Visible : Visibility.Collapsed; }
	/// }
	/// </example>
	public abstract class OneWayTypedConverter<Tinput, Toutput> : MarkupExtension, IValueConverter
	{
		public override object ProvideValue(IServiceProvider serviceProvider) { return this; }

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return Convert((Tinput)value, parameter, culture);
		}

		protected abstract Toutput Convert(Tinput value, object parameter, CultureInfo culture);

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) { throw new NotImplementedException(); }
	}


}
