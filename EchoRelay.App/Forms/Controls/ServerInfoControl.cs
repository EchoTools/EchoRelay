using EchoRelay.Core.Monitoring;
using EchoRelay.Core.Utils;
using Newtonsoft.Json;
using Server = EchoRelay.Core.Server.Server;

namespace EchoRelay.App.Forms.Controls
{
    public partial class ServerInfoControl : UserControl
    {
        ApiManager _apiManager = ApiManager.Instance;
        public ServerInfoControl()
        {
            InitializeComponent();
            UpdateServerInfo(null, false);
        }

        public void UpdateServerInfo(Server? server, bool updateServiceConfig)
        {
            // Set all of our connection related label text.
            lblServerStatus.Text = server != null ? "Running" : "Not started";
            lblServerPort.Text = server?.Settings?.Port.ToString() ?? "Not started";
            lblConfigPeers.Text = (server?.ConfigService.Peers.Count ?? 0).ToString();
            lblLoginPeers.Text = (server?.LoginService.Peers.Count ?? 0).ToString();
            lblMatchingPeers.Text = (server?.MatchingService.Peers.Count ?? 0).ToString();
            lblServerDBPeers.Text = (server?.ServerDBService.Peers.Count ?? 0).ToString();
            lblTransactionPeers.Text = (server?.TransactionService.Peers.Count ?? 0).ToString();

            // If we wished to update the service config, set it now. We always update if no server is provided (to clear out the textbox).
            if (updateServiceConfig || server == null)
            {
                if (server != null)
                {
                    string hostName = server.PublicIPAddress?.ToString() ?? "localhost";
                    rtbGeneratedServiceConfig.Text = JsonConvert.SerializeObject(server.Settings.GenerateServiceConfig(hostName, serverConfig: true), Formatting.Indented, StreamIO.JsonSerializerSettings);
                }
                else
                {
                    rtbGeneratedServiceConfig.Text = "";
                }
            }
            
            
            _apiManager.peerStatsObject.ServerIp = server?.PublicIPAddress?.ToString() ?? "localhost";
            _apiManager.peerStatsObject.Login = server?.LoginService.Peers.Count ?? 0;
            _apiManager.peerStatsObject.Matching = server?.MatchingService.Peers.Count ?? 0;
            _apiManager.peerStatsObject.Config = server?.ConfigService.Peers.Count ?? 0;
            _apiManager.peerStatsObject.Transaction = server?.TransactionService.Peers.Count ?? 0;
            _apiManager.peerStatsObject.ServerDb = server?.ServerDBService.Peers.Count ?? 0;
            Task.Run(() => _apiManager.PeerStats.EditPeerStats(_apiManager.peerStatsObject, server?.PublicIPAddress?.ToString() ?? "localhost"));

        }

        private void btnCopyServiceConfig_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(rtbGeneratedServiceConfig.Text))
                return;

            Clipboard.SetText(rtbGeneratedServiceConfig.Text);
        }

        private void btnSaveServiceConfig_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(rtbGeneratedServiceConfig.Text))
                return;

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Service config (.json)|*.json";
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                File.WriteAllText(saveFileDialog.FileName, rtbGeneratedServiceConfig.Text);
            }
        }
    }
}
