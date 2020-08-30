using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using System.ServiceProcess;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace ServiceManager
{
    public partial class Form1 : Form
    {
        [System.ServiceProcess.ServiceProcessDescription("SPStatus")]
        public System.ServiceProcess.ServiceControllerStatus Status { get; }
        private string OAuth2;
        private string credentials;
        private ServiceController DCTR_servise = new ServiceController("DCTR_Cliest");
        public Form1()
        {
            InitializeComponent();

            serviceStatus(DCTR_serviseStatus());

            // check google-secret file
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            credentials = appData + "\\DCTRsystem\\client_secret.json";
            if (!System.IO.File.Exists(credentials))
            {
                button2.Enabled = false;
                labInfo.Text = "Select the google-secret file";
                return;
            }

            // check authorization data
            OAuth2 = appData + "\\DCTRsystem\\Google.Apis.Auth.OAuth2.Responses.TokenResponse-" + Environment.UserName;

            if (File.Exists(OAuth2))
            {
                button2.Text = "Logout";
            }
        }

        private bool DCTR_serviseStatus()
        {
            if (DCTR_servise.Status.Equals(ServiceControllerStatus.Stopped))
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        private void filesListBox_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop, false) == true)
            {
                e.Effect = DragDropEffects.All;
            }
            else
            {
                this.Cursor = Cursors.No;
            }
        }

        private void ListDragTarget_DragDrop(object sender, System.Windows.Forms.DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            this.panel1.Visible = false;

            checkJSON(files[0]);
        }

        private void DragTarget_DragOver(object sender, System.Windows.Forms.DragEventArgs e)
        {
            // Файл поднесли, но не отпустили
            this.panel1.Visible = true;
            
        }

        private void buttonStop_Click(object sender, EventArgs e)
        {
            DCTR_servise.Stop();
            waitServiceStatus(ServiceControllerStatus.Stopped);
            serviceStatus(false);
            buttonStart.BackgroundImage = new Bitmap(Properties.Resources.start);
            buttonStop.BackgroundImage = new Bitmap(Properties.Resources.stopGr);
        }

        private void buttonStart_Click(object sender, EventArgs e)
        {
            DCTR_servise.Start();
            waitServiceStatus(ServiceControllerStatus.Running);
            serviceStatus(true);
            buttonStart.BackgroundImage = new Bitmap(Properties.Resources.startGr);
            buttonStop.BackgroundImage = new Bitmap(Properties.Resources.stop);
        }

        private void waitServiceStatus(ServiceControllerStatus status)
        {

            while (true)
            {
                try
                {
                    DCTR_servise.WaitForStatus(status, TimeSpan.FromMilliseconds(500));
                    break;
                }
                catch (System.ServiceProcess.TimeoutException)
                {
                    continue;
                }
            }
        }

        private void serviceStatus(bool enabled)
        {
            this.buttonStart.Enabled = !enabled;
            this.buttonStop.Enabled = enabled;

            this.labelStart.Enabled = !enabled;
            this.labelStop.Enabled = enabled;

            if (enabled)
            {
                buttonStart.BackgroundImage = new Bitmap(Properties.Resources.startGr);
                buttonStop.BackgroundImage = new Bitmap(Properties.Resources.stop);
            }
            else
            {
                buttonStart.BackgroundImage = new Bitmap(Properties.Resources.start);
                buttonStop.BackgroundImage = new Bitmap(Properties.Resources.stopGr);
            }

        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog OpenSecret = new OpenFileDialog();
            OpenSecret.Filter = "Files JSON|*.json|All files|*.*";
            
            if (OpenSecret.ShowDialog() == DialogResult.Cancel)
            {
                return;
            }

            checkJSON(OpenSecret.FileName);
        }

        private void checkJSON(string fileName)
        {
            string jsonString = File.ReadAllText(fileName);
            GoogleSecret gsec = JsonConvert.DeserializeObject<GoogleSecret>(jsonString);

            if (gsec.installed.listError.Count > 0)
            {
                MessageBox.Show("Invalid parameters in the file", "Error parametrs",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            using (StreamWriter file = File.CreateText(credentials))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(file, gsec);
            }

            button2_Click(new object(), EventArgs.Empty);
            button2.Enabled = true;
            labInfo.Text = "";
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (File.Exists(OAuth2))
            {
                File.Delete(OAuth2);
                button2.Text = "Login";
            } else
            {
                var usrCredential = GoogleDrive.GetUserCredential();
                button2.Text = "Logout";
            }
        }
    }

    class GoogleSecret
    {
        public GInstalled installed { get; set; }
    }

    class GInstalled
    {
        private string _client_id;
        private string _project_id;
        private string _auth_uri;
        private string _token_uri;
        private string _apx509cert_url;
        private string _client_secret;
        private List<string> _errorParam = new List<string>();
        public string client_id
        {
            get => _client_id;
            set
            {
                if (value.Length != 73)
                {
                    _errorParam.Add("client_id");
                }
                _client_id = value;
            }
        }
        public string project_id
        {
            get => _project_id;
            set
            {
                if (value.Length != 18)
                {
                    _errorParam.Add("project_id");
                }
                _project_id = value;
            }
        }
        public string auth_uri
        {
            get => _auth_uri;
            set
            {
                if (value.Length != 41)
                {
                    _errorParam.Add("auth_uri");
                }
                _auth_uri = value;
            }
        }
        public string token_uri
        {
            get => _token_uri;
            set
            {
                if (value.Length != 35)
                {
                    _errorParam.Add("token_uri");
                }
                _token_uri = value;
            }
        }
        public string auth_provider_x509_cert_url
        {
            get => _apx509cert_url;
            set
            {
                if (value.Length != 42)
                {
                    _errorParam.Add("auth_provider_x509_cert_url");
                }
                _apx509cert_url = value;
            }
        }
        public string client_secret
        {
            get => _client_secret;
            set
            {
                if (value.Length != 24)
                {
                    _errorParam.Add("client_secret");
                }
                _client_secret = value;
            }
        }
        public List<string> redirect_uris { get; set; }

        [JsonIgnore]
        public List<string> listError {get => _errorParam;}
    }


}
