using DiscordRPC;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using vtm_bloodhunt_rpc.Properties;

namespace vtm_bloodhunt_rpc
{
    public partial class Main : ApplicationContext
    {

        public static Main instance { get; private set; }

        public DiscordRpcClient client = new DiscordRpcClient("970069154759594074");

        public NotifyIcon trayIcon { get; }

        public Listener listener { get; } = new Listener();

        public RichPresence presence = new RichPresence()
        {
            State = "Waiting for State",
            Assets = new Assets()
            {
                LargeImageKey = "bloodhunt",
                LargeImageText = "Vampire: The Masquerade - Bloodhunt"
            },
            Buttons = new DiscordRPC.Button[]
            {
                new DiscordRPC.Button() { Label = "Play Bloodhunt!", Url = "https://bloodhunt.com" }
            }
        };

        public Main()
        {
            instance = this;
            trayIcon = new NotifyIcon()
            {
                Text = "Bloodhunt Rich Presence",
                Icon = Icon.FromHandle(Resources.Idle.GetHicon()),
                ContextMenu = new ContextMenu(new MenuItem[] {
                    new MenuItem("Discord Rich Presence", new MenuItem[] {
                        new MenuItem("Enable", EnablePresence) { Enabled = true },
                        new MenuItem("Disable", DisablePresence) { Enabled = false },
                    }),
                    new MenuItem("Developer", Developer),
                    new MenuItem("Exit", Exit)
                }),
                Visible = true
            };
        }

        private void Developer(object sender, EventArgs e)
        {
            Process.Start("https://github.com/kunseru");
        }

        private async void Exit(object sender, EventArgs e)
        {
            await listener.StopCore();
            Environment.Exit(0);
        }

        private async void DisablePresence(object sender, EventArgs e)
        {
            trayIcon.Icon = Icon.FromHandle(Resources.Idle.GetHicon());
            MenuItem enableItem = trayIcon.ContextMenu.MenuItems
                .Cast<MenuItem>()
                .SelectMany(item => item.MenuItems.Cast<MenuItem>())
                .FirstOrDefault(item => item.Text == "Enable");

            MenuItem disableItem = trayIcon.ContextMenu.MenuItems
                .Cast<MenuItem>()
                .SelectMany(item => item.MenuItems.Cast<MenuItem>())
                .FirstOrDefault(item => item.Text == "Disable");

            await listener.StopCore();

            enableItem.Enabled = true;
            disableItem.Enabled = false;

            client.Dispose();
        }

        private void EnablePresence(object sender, EventArgs e)
        {
            trayIcon.Icon = Icon.FromHandle(Resources.AppIcon.GetHicon());

            MenuItem enableItem = trayIcon.ContextMenu.MenuItems
                .Cast<MenuItem>()
                .SelectMany(item => item.MenuItems.Cast<MenuItem>())
                .FirstOrDefault(item => item.Text == "Enable");

            MenuItem disableItem = trayIcon.ContextMenu.MenuItems
                .Cast<MenuItem>()
                .SelectMany(item => item.MenuItems.Cast<MenuItem>())
                .FirstOrDefault(item => item.Text == "Disable");

            listener.StartCore();

            client.SetPresence(presence);
            client.Initialize();

            enableItem.Enabled = false;
            disableItem.Enabled = true;
        }
    }
}
