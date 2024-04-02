using Fiddler;
using System.IO;
using System.Windows.Forms;
using System;
using System.Reflection;
using Newtonsoft.Json.Linq;
using Microsoft.Win32;
using System.Threading.Tasks;
using DiscordRPC;

namespace vtm_bloodhunt_rpc
{
    public class Listener
    {

        private static readonly string assemblyDirectory = GetAssemblyDirectory();

        private static string lastQueue;

        private static void BeforeRequest(Session session)
        {
            session.bBufferResponse = true;
            session.utilDecodeRequest();
            if (session.uriContains("sharkmob.cloud/presence/clientpresence/location"))
            {
                JObject request = JObject.Parse(session.GetRequestBodyAsString());
                string location = request["location"].ToString();
                string detail;
                switch (location)
                {
                    case "Elysium":
                        detail = "In Elysium";
                        break;
                    case "Match":
                        detail = "In a Match";
                        break;
                    default:
                        detail = location;
                        break;
                }
                Main.instance.client.UpdateDetails(detail);
                Main.instance.client.UpdateState(lastQueue);
                Main.instance.client.UpdateStartTime(DateTime.UtcNow);
            }

            if (session.uriContains("sharkmob.cloud/session/sessions/placements") && !session.uriContains("cancel"))
            {
                JObject request = JObject.Parse(session.GetRequestBodyAsString());
                if (request["gAMEMODEId"] != null)
                {
                    string gameModeId = request["gAMEMODEId"].ToString();
                    Main.instance.client.UpdateStartTime(DateTime.UtcNow);
                    Main.instance.client.UpdateDetails("Queueing");

                    if (gameModeId.Contains("brMain"))
                    {
                        Main.instance.client.UpdateState("Solos");
                        lastQueue = "Solos";
                    }
                    else if (gameModeId.Contains("brDuo"))
                    {
                        Main.instance.client.UpdateState("Duos");
                        lastQueue = "Duos";
                    }
                    else if (gameModeId.Contains("brTrio"))
                    {
                        Main.instance.client.UpdateState("Trios");
                        lastQueue = "Trios";
                    }
                    else if (gameModeId.Contains("TDM"))
                    {
                        Main.instance.client.UpdateState("Team Deathmatch");
                        lastQueue = "Team Deathmatch";
                    }
                    else
                    {
                        Main.instance.client.UpdateState("Unknown");
                    }
                }
            }
            
            if (session.uriContains("sharkmob.cloud/session/sessions/placements/cancel"))
            {
                JObject request = JObject.Parse(session.GetRequestBodyAsString());
                if (request["gAMEMODEId"] != null)
                {
                    string gameModeId = request["gAMEMODEId"].ToString();
                    Main.instance.client.UpdateStartTime(DateTime.UtcNow);
                    Main.instance.client.UpdateDetails("Idling");

                    if (gameModeId.Contains("brMain"))
                    {
                        Main.instance.client.UpdateState("Solos");
                    }
                    else if (gameModeId.Contains("brDuo"))
                    {
                        Main.instance.client.UpdateState("Duos");
                    }
                    else if (gameModeId.Contains("brTrio"))
                    {
                        Main.instance.client.UpdateState("Trios");
                    }
                    else if (gameModeId.Contains("TDM"))
                    {
                        Main.instance.client.UpdateState("Trios");
                    }
                    else
                    {
                        Main.instance.client.UpdateState("Unknown");
                    }
                }
            }
        }

        private static string GetAssemblyDirectory()
        {
            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            return Path.GetDirectoryName(path);
        }

        public void StartCore()
        {
            AttachEventListeners();
            EnsureRootCertificate();
            StartupFiddlerCore();
        }

        public Task StopCore()
        {
            ClearProxy();

            if (FiddlerApplication.IsStarted())
            {
                FiddlerApplication.Shutdown();
            }

            return Task.CompletedTask;
        }

        private void ClearProxy()
        {
            const string userRoot = "HKEY_CURRENT_USER";
            const string subkey = "Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings";
            const string keyName = userRoot + "\\" + subkey;

            int proxyEnable = (int)Registry.GetValue(keyName, "ProxyEnable", 0);

            if (proxyEnable == 1)
            {
                Registry.SetValue(keyName, "ProxyEnable", 0, RegistryValueKind.DWord);
            }
        }

        private static void EnsureRootCertificate()
        {
            try
            {
                BCCertMaker.BCCertMaker certProvider = new BCCertMaker.BCCertMaker();
                CertMaker.oCertProvider = certProvider;

                string rootCertificatePath = Path.Combine(assemblyDirectory, "..", "..", "RootCertificate.p12");
                string rootCertificatePassword = "S0m3T0pS3cr3tP4ssw0rd";
                if (!File.Exists(rootCertificatePath))
                {
                    certProvider.CreateRootCertificate();
                    certProvider.WriteRootCertificateAndPrivateKeyToPkcs12File(rootCertificatePath, rootCertificatePassword);
                }
                else
                {
                    certProvider.ReadRootCertificateAndPrivateKeyFromPkcs12File(rootCertificatePath, rootCertificatePassword);
                }

                if (!CertMaker.rootCertIsTrusted())
                {
                    CertMaker.trustRootCert();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "There was an Error (EnsureRootCertificate)!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void AttachEventListeners()
        {
            FiddlerApplication.BeforeRequest += BeforeRequest;
        }

        private static void StartupFiddlerCore()
        {
            FiddlerCoreStartupSettings startupSettings =
                new FiddlerCoreStartupSettingsBuilder()
                    .RegisterAsSystemProxy()
                    .DecryptSSL()
                    .MonitorAllConnections()
                    .Build();

            FiddlerApplication.Startup(startupSettings);
        }
    }
}
