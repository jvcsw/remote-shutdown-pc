namespace Karpach.RemoteShutdown.Controller
{
    using System.Linq;
    using System.Windows.Forms;
    using Karpach.RemoteShutdown.Controller.Helpers;
    using Karpach.RemoteShutdown.Controller.Properties;

    public partial class SettingsForm : Form
    {
        public TrayCommandType CommandType => (TrayCommandType) cbxTrayCommand.SelectedValue;

        public bool AutoStart => chkAutoLoad.Checked;

        public bool CheckBlockingProcesses => chkBlockingProcesses.Checked;

        public int Port => int.Parse(txtPort.Text);

        public string SecretCode => txtSecretCode.Text;

        public SettingsForm(ITrayCommandHelper trayCommandHelper)
        {
            InitializeComponent();
            this.txtSecretCode.Text = Settings.Default.SecretCode;
            this.txtPort.Text = Settings.Default.RemotePort.ToString();
            this.chkAutoLoad.Checked = Settings.Default.AutoStart;
            this.chkBlockingProcesses.Checked = Settings.Default.CheckBlockingProcesses;
            this.cbxTrayCommand.DisplayMember = "Name";
            this.cbxTrayCommand.ValueMember = "CommandType";
            this.cbxTrayCommand.DataSource = trayCommandHelper.Commands;
            this.cbxTrayCommand.SelectedItem = trayCommandHelper.Commands.SingleOrDefault(c => (int)c.CommandType == Settings.Default.DefaultCommand);
        }        

        private void txtPort_Validating(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!int.TryParse(txtPort.Text, out int port))
            {
                e.Cancel = false;
            }
        }
    }
}
