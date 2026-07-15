using Serilog;
using System;
using System.Windows.Forms;

namespace PlateLocCommandExecutor
{
    public partial class PlateLocForm : Form
    {
        public PlateLocForm()
        {
            InitializeComponent();

            Log.Debug("InitComponent");
            // Operate in Blocking mode with error dialogs disabled.
            axPlateLoc1.Blocking = false;
            axPlateLoc1.Blocking = true;
        }

        public AxPlateLocLib.AxPlateLoc GetControl()
        {
            return axPlateLoc1;
        }

        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void NoticeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var form = new NoticeForm())
            {
                form.ShowDialog();
            }
        }

        private void axPlateLoc1_Error(object sender, AxPlateLocLib._DPlateLocEvents_ErrorEvent e)
        {
            // Disable error dialogs.
            Log.Debug($"ActiveX error event description: {e.description}");
            e.cancelDisplay = true;
            Log.Debug("cancelDisplay set true.");
        }

        private void profileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            axPlateLoc1.ShowDiagsDialog(true, 0);
        }
    }
}
