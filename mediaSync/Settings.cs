using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;
using mediaSync.Properties;

namespace mediaSync
{
    public partial class Settings : Form
    {   
        //Declaración de variables
        static EventLog eventLog1;

        /// <summary>
        /// Constructor
        /// </summary>
        public Settings()
        {
            InitializeComponent();
            eventLog1 = new EventLog();
            this.Icon = Resources.favicon;
            if (!System.Diagnostics.EventLog.SourceExists("mediaSync"))
            {
                System.Diagnostics.EventLog.CreateEventSource(
                    "mediaSync", "Application");
            }
            // configure the event log instance to use this source name
            eventLog1.Source = "mediaSync";
            
        }
        /// <summary>
        /// Muestra un cuadro de confirmación. Si se confirma realizan los cambios en el watchdog y se almacena en el fichero de preferencias.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSave_Click(object sender, EventArgs e)
        {
            //mostramos un dialogo de confirmación

            DialogResult result = MessageBox.Show("Do you want to save changes?", "Confirmation", MessageBoxButtons.YesNo,MessageBoxIcon.Question);
            
            if (result==DialogResult.Yes)
            {   
                //realizamos los cambios
                mediaSync.mediaPath = txtMediaPath.Text;
                mediaSync.bucket = txtBucketName.Text;
                //preparamos los valores para guardarlos
                AppPreferences aps = new AppPreferences();
                aps._MediaPath = txtMediaPath.Text;
                aps._Bucket = txtBucketName.Text;
                aps._AccessKey = txtAccessKey.Text;
                aps._SecretKey = txtSecretKey.Text;
                //guardamos los valores
                XmlSerializer mySerializer = new XmlSerializer(typeof(AppPreferences));
                StreamWriter myWriter = null;
                try
                {
                    string settingsPath = Path.Combine(Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath), "settings.xml");
                    myWriter = new StreamWriter(settingsPath);
                    mySerializer.Serialize(myWriter, aps);
                    myWriter.Close();
                    myWriter.Dispose();

                }
                catch (IOException ex)
                {
                    eventLog1.WriteEntry("Error saving preferences: " + ex.Message, EventLogEntryType.Error);
                }
                this.Close();//se cierra el fichero
            
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Inicializa los valores de los controles
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Settings_Load(object sender, EventArgs e)
        {
            txtMediaPath.Text = mediaSync.mediaPath;
            txtBucketName.Text = mediaSync.bucket;
            txtAccessKey.Text = mediaSync.accessKey;
            txtSecretKey.Text = mediaSync.secretKey;
        }

       

    }
}
