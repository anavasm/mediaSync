using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Timers;
using System.Threading.Tasks;
using System.Windows.Forms;
using mediaSync.Properties;
using System.IO;
using System.Diagnostics;
using System.ComponentModel;
using System.Globalization;
using System.Xml.Serialization;
using System.Xml.Linq;

namespace mediaSync
{
    class mediaSync : IDisposable
    {
        /// <summary>
		/// The NotifyIcon object.
		/// </summary>
		static NotifyIcon ni;
        
        //declaración  de variables
        private static System.Timers.Timer syncTimer;
        static EventLog eventLog1;
        
        //App preferences
        public static string mediaPath;
        public static string bucket;
        public static string accessKey;
        public static string secretKey;

		/// <summary>
		/// Initializes a new instance of the <see cref="ProcessIcon"/> class.
		/// </summary>
		public mediaSync()
		{
			// Instantiate the NotifyIcon object.
			ni = new NotifyIcon();
            eventLog1 = new EventLog();
            // create an event source, specifying the name of a log that 
            // does not currently exist to create a new, custom log 
            if (!System.Diagnostics.EventLog.SourceExists("mediaSync"))
            {
                System.Diagnostics.EventLog.CreateEventSource(
                    "mediaSync", "Application");
            }
            // configure the event log instance to use this source name
            eventLog1.Source = "mediaSync";
            //inicialización
            loadPreferences();

		}
        /// <summary>
        /// Muestra NotifyIcon, crea el menú contextual e inicial el intervalo de tiempo.
        /// </summary>
        public void Display()
        {
          
            ni.Visible = true;

            // Attach a context menu.
            ni.ContextMenuStrip = new ContextMenus().Create();

            eventLog1.WriteEntry("Service started successfully", EventLogEntryType.Information);

            //Timer para el control del tiempo entre llamadas de acción Sync.
            syncTimer = new System.Timers.Timer();
            //Intervalo de tiempo entre llamadas de acción sync.
            syncTimer.Interval = 6000;
            //Evento a ejecutar cuando se cumple el tiempo de acción Sync.
            syncTimer.Elapsed += new System.Timers.ElapsedEventHandler(syncTimer_Elapsed);
            //Habilitar el Timer de acción Sync.
            syncTimer.Enabled = true;

            //fix de error de visibilidad
            ni.Visible = true;
 
        }

        /// <summary>
        /// Evento del timer de sincronización
        /// </summary>
        void syncTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            //Detiene el Timer
            syncTimer.Enabled = false;
            //se ejecuta las comprobaciones
            Console.WriteLine("Hilo syncMedia ejecutado - " + DateTime.Now);
            //eventLog1.WriteEntry("Hilo syncMedia ejecutado - " + DateTime.Now, EventLogEntryType.Information);
            checkSync();
            //habilita el Timer nuevamente.
            syncTimer.Enabled = true;
        }

        /// <summary>
        /// Función que comprueba la sincronización de archivos con el servidor
        /// </summary>
        private void checkSync()
        {
           
            try
            {
                syncFiles sincronizar = new syncFiles(mediaPath, bucket, accessKey, secretKey);
                sincronizar.checkSync();

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

        }            

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources
        /// </summary>
        public void Dispose()
        {
            // When the application closes, this will remove the icon from the system tray immediately.
            ni.Dispose();
        }

        /// <summary>
        /// Carga las preferencias de configuración. Primero del fichero personalizado, si existe, si no se cargan los valores estándar.
        /// </summary>
        private void loadPreferences()
        {
            string settingsPath = Path.Combine(Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath), "settings.xml");
            ni.Icon = Resources.favicon;
            ni.Text = "MediaSync";
            if (File.Exists(settingsPath))
            {
                AppPreferences aps;

                XmlSerializer mySerializer = new XmlSerializer(typeof(AppPreferences));
                FileStream myFileStream = new FileStream(settingsPath, FileMode.Open);

                aps = (AppPreferences)mySerializer.Deserialize(myFileStream);

                mediaPath = aps._MediaPath;
                bucket = aps._Bucket;
                accessKey = aps._AccessKey;
                secretKey = aps._SecretKey;

                myFileStream.Close();
            }
            else
            {
                mediaPath = @"C:\textMedia\";
                bucket = "test";
                accessKey = "enter_your_accessKey";
                secretKey = "enter_your_secretKey";

            }
        }

    }

}
