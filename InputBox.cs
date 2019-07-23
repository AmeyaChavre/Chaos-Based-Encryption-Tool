using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ChaoticEncryption
{
    /// <summary>
    /// used to request a seed value from the user.
    /// </summary>
    public partial class SeedInputBox : Form
    {
        /// <summary>
        /// the entered seed value.
        /// </summary>
        public int SeedValue
        {
            get { return int.Parse(txtSeed.Text); }
        }

        /// <summary>
        /// constructor.
        /// </summary>
        public SeedInputBox()
        {
            InitializeComponent();

            // enable the "OK" button when a seed value has been entered.
            Application.Idle += delegate
            {
                if (!String.IsNullOrWhiteSpace(txtSeed.Text))
                    buttonOK.Enabled = true;
                else
                    buttonOK.Enabled = false;
            };
        }

        /// <summary>
        /// validate that the seed is an integer.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtSeed_Validating(object sender, CancelEventArgs e)
        {
            int value = 0;
            if (!int.TryParse(txtSeed.Text, out value))
            {
                e.Cancel = true;
                MessageBox.Show("Seed Value must be an Integer");
            }
        }
    }
}
