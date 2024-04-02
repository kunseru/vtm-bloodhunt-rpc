using Fiddler;
using System.IO;
using System.Windows.Forms;
using System;
using System.Reflection;
using Newtonsoft.Json.Linq;
using Microsoft.Win32;
using System.Threading.Tasks;

namespace vtm_bloodhunt_rpc
{
    public class Listener
    {

        private static readonly string assemblyDirectory = GetAssemblyDirectory();

        private static void BeforeRequest(Session session)
        {
            session.bBufferResponse = true;
            session.utilDecodeRequest();
            if (session.uriContains("sharkmob.cloud/presence/clientpresence/location"))
            {
                JObject response = JObject.Parse(session.GetRequestBodyAsString());
                switch (response["location"].ToString())
                {
                    case "Elysium":
                        Main.instance.client.UpdateStartTime(DateTime.Now);
                        Main.instance.client.UpdateEndTime(DateTime.Now + TimeSpan.FromDays(365));
                        Main.instance.client.UpdateState("In Elysium");
                        break;
                    case "Match":
                        Main.instance.client.UpdateStartTime(DateTime.Now);
                        Main.instance.client.UpdateEndTime(DateTime.Now + TimeSpan.FromDays(365));
                        Main.instance.client.UpdateState("In a Match");
                        break;
                    default:
                        Main.instance.client.UpdateStartTime(DateTime.Now);
                        Main.instance.client.UpdateEndTime(DateTime.Now + TimeSpan.FromDays(365));
                        Main.instance.client.UpdateState(response["location"].ToString());
                        break;
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
