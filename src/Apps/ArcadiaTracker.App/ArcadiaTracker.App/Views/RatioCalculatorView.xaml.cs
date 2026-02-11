using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Extensions.DependencyInjection;
using GameCompanion.Module.StarRupture.Models;
using GameCompanion.Module.StarRupture.Services;

namespace ArcadiaTracker.App.Views;

/// <summary>
/// Interaction logic for RatioCalculatorView.xaml
/// </summary>
public partial class RatioCalculatorView : UserControl
{
    private readonly RatioCalculatorService _calculatorService;
    private StarRuptureSave? _currentSave;

    public RatioCalculatorView()
    {
        InitializeComponent();
        _calculatorService = App.Services.GetRequiredService<RatioCalculatorService>();
        LoadAvailableItems();
    }

    private void LoadAvailableItems()
    {
        var items = _calculatorService.GetAvailableItems();
        ItemComboBox.ItemsSource = items;
        if (items.Count > 0)
        {
            ItemComboBox.SelectedIndex = 0;
        }
    }

    public void SetCurrentSave(StarRuptureSave? save)
    {
        _currentSave = save;
        Calculate();
    }

    private void ItemComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        Calculate();
    }

    private void RateTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        // Debounce could be added here for better UX
    }

    private void CalculateButton_Click(object sender, RoutedEventArgs e)
    {
        Calculate();
    }

    private void Calculate()
    {
        if (ItemComboBox.SelectedItem is not string selectedItem)
        {
            StatusText.Text = "Please select an item";
            return;
        }

        if (!double.TryParse(RateTextBox.Text, out var targetRate) || targetRate <= 0)
        {
            StatusText.Text = "Please enter a valid rate";
            return;
        }

        var result = _calculatorService.CalculateRatio(selectedItem, targetRate, _currentSave);

        if (result.IsSuccess && result.Value != null)
        {
            RequirementsList.ItemsSource = result.Value.Requirements;
            ComparisonList.ItemsSource = result.Value.CurrentVsRequired;

            if (result.Value.CurrentVsRequired.Count > 0)
            {
                NoComparisonText.Visibility = Visibility.Collapsed;

                if (result.Value.CanAchieveTarget)
                {
                    StatusText.Text = "Target achievable with current build!";
                    StatusText.Foreground = (Brush)FindResource("SuccessBrush");
                    StatusBanner.Background = new SolidColorBrush(Color.FromArgb(40, 0, 200, 100));
                }
                else
                {
                    StatusText.Text = result.Value.BottleneckReason ?? "Cannot achieve target";
                    StatusText.Foreground = (Brush)FindResource("ErrorBrush");
                    StatusBanner.Background = new SolidColorBrush(Color.FromArgb(40, 200, 50, 50));
                }
            }
            else
            {
                NoComparisonText.Visibility = Visibility.Visible;
                StatusText.Text = "Load a save to compare";
                StatusText.Foreground = (Brush)FindResource("TextSecondaryBrush");
                StatusBanner.Background = (Brush)FindResource("SurfaceLightBrush");
            }
        }
        else
        {
            StatusText.Text = result.Error ?? "Calculation failed";
            StatusText.Foreground = (Brush)FindResource("ErrorBrush");
        }
    }
}
