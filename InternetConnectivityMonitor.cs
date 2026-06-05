using System;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BubbyPlanetShowroom
{
    public sealed class InternetConnectivityMonitor : IDisposable
    {
        private readonly Form uiOwner;
        private readonly TimeSpan checkInterval;
        private readonly CancellationTokenSource cts = new CancellationTokenSource();
        private InternetStatusForm? statusForm;
        private bool lastOnlineState = true;
        private Task? loopTask;

        public InternetConnectivityMonitor(Form uiOwner, TimeSpan? checkInterval = null)
        {
            this.uiOwner = uiOwner;
            this.checkInterval = checkInterval ?? TimeSpan.FromSeconds(3);
        }

        public void Start()
        {
            NetworkChange.NetworkAvailabilityChanged += OnNetworkAvailabilityChanged;
            loopTask = Task.Run(() => MonitorLoopAsync(cts.Token));
            _ = TriggerImmediateCheckAsync();
        }

        private void OnNetworkAvailabilityChanged(object? sender, NetworkAvailabilityEventArgs e)
        {
            _ = TriggerImmediateCheckAsync();
        }

        private async Task TriggerImmediateCheckAsync()
        {
            bool online = await IsInternetAvailableAsync(cts.Token).ConfigureAwait(false);
            UpdateUi(online);
        }

        private async Task MonitorLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                bool online = await IsInternetAvailableAsync(token).ConfigureAwait(false);
                UpdateUi(online);

                try
                {
                    await Task.Delay(checkInterval, token).ConfigureAwait(false);
                }
                catch (TaskCanceledException)
                {
                    return;
                }
            }
        }

        private void UpdateUi(bool online)
        {
            if (online == lastOnlineState)
                return;

            lastOnlineState = online;

            if (uiOwner.IsDisposed)
                return;

            uiOwner.BeginInvoke(new Action(() =>
            {
                if (uiOwner.IsDisposed)
                    return;

                if (!online)
                    ShowOfflinePopup();
                else
                    HideOfflinePopup();
            }));
        }

        private void ShowOfflinePopup()
        {
            if (statusForm != null && !statusForm.IsDisposed)
            {
                statusForm.TopMost = true;
                statusForm.Activate();
                return;
            }

            statusForm = new InternetStatusForm();
            statusForm.FormClosed += (s, e) => statusForm = null;
            statusForm.Show(uiOwner);
            statusForm.BringToFront();
        }

        private void HideOfflinePopup()
        {
            if (statusForm == null || statusForm.IsDisposed)
                return;

            statusForm.AllowClose();
            statusForm.Close();
            statusForm = null;
        }

        private static async Task<bool> IsInternetAvailableAsync(CancellationToken token)
        {
            if (!NetworkInterface.GetIsNetworkAvailable())
                return false;

            try
            {
                using var client = new HttpClient
                {
                    Timeout = TimeSpan.FromSeconds(2)
                };

                using var request = new HttpRequestMessage(HttpMethod.Get, "http://clients3.google.com/generate_204");
                using HttpResponseMessage response = await client.SendAsync(request, token).ConfigureAwait(false);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            NetworkChange.NetworkAvailabilityChanged -= OnNetworkAvailabilityChanged;
            cts.Cancel();

            try { loopTask?.Wait(TimeSpan.FromSeconds(1)); } catch { }

            if (uiOwner.IsDisposed)
                return;

            uiOwner.BeginInvoke(new Action(() =>
            {
                if (statusForm != null && !statusForm.IsDisposed)
                {
                    statusForm.AllowClose();
                    statusForm.Close();
                    statusForm = null;
                }
            }));

            cts.Dispose();
        }
    }
}
