using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;

/*
 * Cellular Automata Based Encryption
 * 
 * Author: Simon Bridge, January 2012
 * 
 * This work is distributed under the The Code Project Open License (CPOL) 1.02
 * 
 * Source Code and Executable Files can be used in commercial applications;
 * Source Code and Executable Files can be redistributed; and
 * Source Code can be modified to create derivative works.
 * No claim of suitability, guarantee, or any warranty whatsoever is provided. The software is provided "as-is".
 * The Article(s) accompanying the Work may not be distributed or republished without the Author's consent
 * 
 * 
 */

namespace ChaoticEncryption
{
    /// <summary>
    /// main interface form.
    /// </summary>
    public partial class FrmCellEncrypter : Form
    {
        #region Fields

        /// <summary>
        /// local instance of the CAEncryption class.
        /// </summary>
        private CAEncryption _caEncryption;

        #endregion

        /// <summary>
        /// construct the form.
        /// </summary>
        public FrmCellEncrypter()
        {
            InitializeComponent();

            // attach to the progress event:
            CAEncryption.Progress += new EventHandler<ProgressEventArgs>(CAEncryption_Progress);
        }

        /// <summary>
        /// handle the progress event and update the progress bar.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void CAEncryption_Progress(object sender, ProgressEventArgs e)
        {
            this.EasyInvoke(delegate
            {
                progressBar1.Value = e.PercentComplete;
                statusStrip1.Refresh();

            });
        }

        /// <summary>
        /// easy method to handle cross-thread calls on a windows form.
        /// </summary>
        /// <param name="task"></param>
        public void EasyInvoke(MethodInvoker task)
        {
            if (this.InvokeRequired)
                this.Invoke(task);
            else
                task.Invoke();
        }

        #region Properties

        public string PassCode
        {
            get { return txtPasscode.Text; }
            set { txtPasscode.Text = value; }
        }

        public int GenerationCount
        {
            get { return int.Parse(txtGenerations.Text); }
            set { txtGenerations.Text = value.ToString(); }
        }

        public float Size
        {
            get { return float.Parse(txtSize.Text); }
            set { txtSize.Text = value.ToString(); }
        }

        public String PlainText
        {
            get { return txtPlainText.Text; }
            set { txtPlainText.Text = value; }
        }

        public String Encrypted
        {
            get { return txtEncrypted.Text; }
            set { txtEncrypted.Text = value; }
        }

        #endregion

        #region Form Event Handlers

        
        private void buttonInit_Click(object sender, EventArgs e)
        {
         
            _caEncryption = new CAEncryption(PassCode, GenerationCount, Size);

            
            _caEncryption.GenerateCellData();

            
            this.txtMsgLength.Text = _caEncryption.MaximumMessageLength.ToString();

            
            this.pictureBox1.Image = _caEncryption.GetCABitmap();
        }

       
        private void buttonEncrypt_Click(object sender, EventArgs e)
        {
          
            if (_caEncryption != null && !String.IsNullOrWhiteSpace(PlainText))
            {
             
                Encrypted = _caEncryption.EncryptString(PlainText);
            }
        }

        
        private void buttonDecrypt_Click(object sender, EventArgs e)
        {
            if (_caEncryption != null && !String.IsNullOrWhiteSpace(Encrypted))
            {
                PlainText = _caEncryption.DecryptString(Encrypted);
            }
        }

        
        private void encryptFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            
            OpenFileDialog openDlg = new OpenFileDialog();
            openDlg.Title = "Select File To Encrypt";

           
            SaveFileDialog saveDlg = new SaveFileDialog();
            saveDlg.Title = "Save Encrypted File As..";

           
            SeedInputBox seedInput = new SeedInputBox();

          
            if (openDlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
               
                if (seedInput.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                   
                    if (saveDlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                       
                        ThreadStart task = delegate
                        {
                            try
                            {
                                
                                CAEncryption.CreateEncryptedFile(
                                    new System.IO.FileInfo(openDlg.FileName),
                                    saveDlg.FileName,
                                    seedInput.SeedValue
                                );

                                
                                MessageBox.Show("File Encrypted");

                              
                                this.EasyInvoke(delegate { this.progressBar1.Value = 0; });
                            }
                            catch (Exception ex1)
                            {
                                MessageBox.Show(ex1.Message);
                            }
                        };

                        
                        Thread encryptThread = new Thread(task);
                        encryptThread.Start();
                    }
                }
            }
        }

        
        private void decryptFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Title = "Select Encrypted File";

           
            SeedInputBox inputBox = new SeedInputBox();

          
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
               
                if (inputBox.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                  
                    ThreadStart task = new ThreadStart(delegate
                    {
                        
                        CAEncryption.DecryptFile(dlg.FileName, inputBox.SeedValue);
                        
                        
                        this.EasyInvoke(delegate { this.progressBar1.Value = 0; });

                       
                        MessageBox.Show("File Decrypted");

                    });

                   
                    Thread decryptThread = new Thread(task);
                    decryptThread.Start();
                }
            }
        }

        #endregion

    }

}
