using System.Windows;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Core;

namespace ArcadiaTracker.App.Views;

/// <summary>
/// Interactive map view using WebView2 to embed starrupture.tools.
/// Includes ad-blocking to provide a clean experience.
/// </summary>
public partial class MapView : UserControl
{
    private const string MapUrl = "https://starrupture.tools/map";
    private const string ItemsUrl = "https://starrupture.tools/items";
    private const string BuildingsUrl = "https://starrupture.tools/buildings";
    private const string ResearchUrl = "https://starrupture.tools/research";

    // Common ad-related domains to block
    private static readonly string[] BlockedDomains = new[]
    {
        "doubleclick.net",
        "googlesyndication.com",
        "googleadservices.com",
        "google-analytics.com",
        "googletagmanager.com",
        "facebook.net",
        "facebook.com/tr",
        "adservice.google.com",
        "pagead2.googlesyndication.com",
        "ads.google.com",
        "adsense.google.com",
        "adnxs.com",
        "amazon-adsystem.com",
        "advertising.com",
        "taboola.com",
        "outbrain.com",
        "criteo.com",
        "pubmatic.com",
        "rubiconproject.com",
        "openx.net",
        "casalemedia.com",
        "adroll.com",
        "quantserve.com",
        "scorecardresearch.com",
        "bluekai.com",
        "exelator.com",
        "turn.com",
        "mediamath.com",
        "moatads.com",
        "adsrvr.org",
        "bidswitch.net"
    };

    // JavaScript to remove ad elements from the page
    private const string AdBlockScript = @"
        (function() {
            // Common ad selectors
            const adSelectors = [
                '[class*=""ad-""]',
                '[class*=""ads-""]',
                '[class*=""advert""]',
                '[class*=""sponsor""]',
                '[id*=""ad-""]',
                '[id*=""ads-""]',
                '[id*=""advert""]',
                'iframe[src*=""ad""]',
                'iframe[src*=""doubleclick""]',
                'iframe[src*=""googlesyndication""]',
                '.adsbygoogle',
                '[data-ad]',
                '[data-ads]',
                '[aria-label*=""advertisement""]',
                '[aria-label*=""Advertisement""]',
                'ins.adsbygoogle',
                'div[id^=""div-gpt-ad""]',
                'div[class*=""ad-container""]',
                'div[class*=""ad_container""]',
                'aside[class*=""ad""]'
            ];

            function removeAds() {
                adSelectors.forEach(selector => {
                    try {
                        document.querySelectorAll(selector).forEach(el => {
                            el.style.display = 'none';
                            el.remove();
                        });
                    } catch(e) {}
                });
            }

            // Run immediately
            removeAds();

            // Run again after a delay (for dynamically loaded ads)
            setTimeout(removeAds, 1000);
            setTimeout(removeAds, 3000);

            // Watch for new elements being added
            const observer = new MutationObserver(removeAds);
            observer.observe(document.body, { childList: true, subtree: true });
        })();
    ";

    public MapView()
    {
        InitializeComponent();
        InitializeWebViewAsync();
    }

    private async void InitializeWebViewAsync()
    {
        try
        {
            await WebView.EnsureCoreWebView2Async();

            // Block ad-related network requests
            WebView.CoreWebView2.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
            WebView.CoreWebView2.WebResourceRequested += CoreWebView2_WebResourceRequested;

            // Inject ad-blocking script on each navigation
            WebView.CoreWebView2.DOMContentLoaded += CoreWebView2_DOMContentLoaded;
        }
        catch (Exception ex)
        {
            ShowError($"Failed to initialize WebView2: {ex.Message}");
        }
    }

    private void CoreWebView2_WebResourceRequested(object? sender, CoreWebView2WebResourceRequestedEventArgs e)
    {
        var uri = e.Request.Uri.ToLowerInvariant();

        // Block requests to known ad domains
        foreach (var domain in BlockedDomains)
        {
            if (uri.Contains(domain))
            {
                e.Response = WebView.CoreWebView2.Environment.CreateWebResourceResponse(
                    null, 403, "Blocked", "");
                return;
            }
        }
    }

    private async void CoreWebView2_DOMContentLoaded(object? sender, CoreWebView2DOMContentLoadedEventArgs e)
    {
        try
        {
            // Inject ad-blocking JavaScript
            await WebView.CoreWebView2.ExecuteScriptAsync(AdBlockScript);
        }
        catch
        {
            // Ignore script injection errors
        }
    }

    private void WebView_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
    {
        LoadingOverlay.Visibility = Visibility.Visible;
        ErrorOverlay.Visibility = Visibility.Collapsed;
    }

    private void WebView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        LoadingOverlay.Visibility = Visibility.Collapsed;

        if (!e.IsSuccess)
        {
            ShowError($"Navigation failed: {e.WebErrorStatus}");
        }
    }

    private void ShowError(string message)
    {
        ErrorMessage.Text = message;
        ErrorOverlay.Visibility = Visibility.Visible;
        LoadingOverlay.Visibility = Visibility.Collapsed;
    }

    private void MapButton_Click(object sender, RoutedEventArgs e)
    {
        WebView.Source = new Uri(MapUrl);
    }

    private void ItemsButton_Click(object sender, RoutedEventArgs e)
    {
        WebView.Source = new Uri(ItemsUrl);
    }

    private void BuildingsButton_Click(object sender, RoutedEventArgs e)
    {
        WebView.Source = new Uri(BuildingsUrl);
    }

    private void ResearchButton_Click(object sender, RoutedEventArgs e)
    {
        WebView.Source = new Uri(ResearchUrl);
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        WebView.Reload();
    }
}
