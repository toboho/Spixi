﻿using SPIXI.iOS.Classes;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportRenderer(typeof(WebView), typeof(SpixiWebViewRenderer))]
namespace SPIXI.iOS.Classes
{
    internal class SpixiWebViewRenderer : Xamarin.Forms.Platform.iOS.WkWebViewRenderer
    {
        protected override void OnElementChanged(VisualElementChangedEventArgs e)
        {
            base.OnElementChanged(e);

            var view = NativeView as WebKit.WKWebView;
            
            if (view != null)
            {
                view.Configuration.DataDetectorTypes = WebKit.WKDataDetectorTypes.None;
                view.ScrollView.ScrollEnabled = false;
                view.ScrollView.Bounces = false;
            }
        }
    }
}