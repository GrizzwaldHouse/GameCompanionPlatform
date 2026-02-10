namespace GameCompanion.Engine.UI.Converters;

using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using GameCompanion.Core.Enums;

/// <summary>
/// Converts RiskLevel to a color for display.
/// </summary>
public class RiskLevelToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not RiskLevel level)
            return Brushes.Gray;

        return level switch
        {
            RiskLevel.Low => new SolidColorBrush(Color.FromRgb(46, 204, 113)),      // Green
            RiskLevel.Medium => new SolidColorBrush(Color.FromRgb(241, 196, 15)),   // Yellow/Orange
            RiskLevel.High => new SolidColorBrush(Color.FromRgb(230, 126, 34)),     // Orange
            RiskLevel.Critical => new SolidColorBrush(Color.FromRgb(231, 76, 60)),  // Red
            _ => Brushes.Gray
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Converts RiskLevel to a display name.
/// </summary>
public class RiskLevelToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not RiskLevel level)
            return "Unknown";

        return level switch
        {
            RiskLevel.Low => "Low Risk",
            RiskLevel.Medium => "Medium Risk",
            RiskLevel.High => "High Risk",
            RiskLevel.Critical => "Critical (Read-Only)",
            _ => "Unknown"
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Converts RiskLevel to an icon string.
/// </summary>
public class RiskLevelToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not RiskLevel level)
            return "\uE946"; // Question mark

        return level switch
        {
            RiskLevel.Low => "\uE73E",      // Checkmark
            RiskLevel.Medium => "\uE7BA",   // Warning
            RiskLevel.High => "\uE783",     // Shield
            RiskLevel.Critical => "\uE72E", // Lock
            _ => "\uE946"                   // Question mark
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
