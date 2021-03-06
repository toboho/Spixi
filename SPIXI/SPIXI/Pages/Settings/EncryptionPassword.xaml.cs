﻿using SPIXI.Interfaces;
using SPIXI.Meta;
using System;
using System.Web;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace SPIXI
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class EncryptionPassword : SpixiContentPage
    {
		public EncryptionPassword ()
		{
			InitializeComponent ();
            NavigationPage.SetHasNavigationBar(this, false);

            // Load the platform specific home page url
            var source = new UrlWebViewSource();
            source.Url = string.Format("{0}html/settings_encryption.html", DependencyService.Get<IBaseUrl>().Get());
            webView.Source = source;
        }

        private void onNavigated(object sender, WebNavigatedEventArgs e)
        {
            // Deprecated due to WPF, use onLoad
        }

        private void onLoad()
        {

        }

        private void onNavigating(object sender, WebNavigatingEventArgs e)
        {
            string current_url = HttpUtility.UrlDecode(e.Url);

            if (current_url.Equals("ixian:onload", StringComparison.Ordinal))
            {
                onLoad();
            }
            else if (current_url.Equals("ixian:back", StringComparison.Ordinal))
            {
                Navigation.PopAsync(Config.defaultXamarinAnimations);
            }
            else if (current_url.Equals("ixian:error", StringComparison.Ordinal))
            {
                displaySpixiAlert("SPIXI Account", "Please type a password.", "OK");
            }
            else if (current_url.StartsWith("ixian:changepass:", StringComparison.Ordinal))
            {
                string[] split_url = current_url.Split(new string[] { "--1ec4ce59e0535704d4--" }, StringSplitOptions.None);
                string old_password = split_url[1];
                string new_password = split_url[2];
                if (Node.walletStorage.isValidPassword(old_password))
                {
                    Node.walletStorage.writeWallet(new_password);
                    displaySpixiAlert("SPIXI Account", "Password successfully changed.", "OK");
                    Navigation.PopAsync(Config.defaultXamarinAnimations);
                }
                else
                {
                    displaySpixiAlert("SPIXI Account", "Current password is incorrect, please try again.", "OK");
                }
            }
            else
            {
                // Otherwise it's just normal navigation
                e.Cancel = false;
                return;
            }
            e.Cancel = true;

        }

        protected override bool OnBackButtonPressed()
        {
            Navigation.PopAsync(Config.defaultXamarinAnimations);

            return true;
        }
    }
}