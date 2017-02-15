using Amazon.S3;
using Amazon.S3.Model;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.IO.Compression;

namespace mediaSync
{
    class syncFiles
    {
        XDocument _xmlServer;
        AmazonS3Client client;
        static EventLog eventLog1;
        string _mediaPath;
        string _bucket;
        string _accessKey;
        string _secretKey;

        /// <summary>
        /// Constructor de la clase syncMedia
        /// </summary>
        /// <param name="mediaPath"> Ruta donde se descargarán los ficheros</param>
        /// <param name="bucket">Nombre del bucket de S3</param>
        public syncFiles(string mediaPath, string bucket, string accessKey, string secretKey)
        {
            this._mediaPath = mediaPath;
            this._bucket = bucket;
            this._accessKey = accessKey;
            this._secretKey = secretKey;

            //Log de eventos
            eventLog1 = new EventLog();
            if (!System.Diagnostics.EventLog.SourceExists("mediaSync"))
            {
                System.Diagnostics.EventLog.CreateEventSource(
                    "mediaSync", "Application");
            }
            // configure the event log instance to use this source name
            eventLog1.Source = "mediaSync";
        }

        /// <summary>
        /// Inicia la comprobación de sincronización
        /// </summary>
        public void checkSync()
        {
            //generar xml del servidor
            listingObjects();
            //descargar contenido
            checkObjects();
            //eliminar archivos incorrectos
            deleteObjects();
         
        }

        private void listingObjects()
        {
            //ruta de archivo cache
            string localXml = Path.Combine(Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath), "local.xml");
            //Configuración del cliente de Amazon
            AmazonS3Config conf = new AmazonS3Config();
            //conf.ServiceURL = "http://s3-eu-west-1.amazonaws.com";
            conf.ServiceURL = "https://s3.amazonaws.com";

            AmazonS3Client client;

            client = new AmazonS3Client(_accessKey, _secretKey, conf);

            // List all objects
            ListObjectsRequest listRequest = new ListObjectsRequest
            {
                BucketName = _bucket,
            };

            ListObjectsResponse listResponse;
            //creación documento server.xml
            XDocument doc = new XDocument(
            new XDeclaration("1.0", "UTF-8", "yes"),
            //root
            new XElement("media")
            );

            doc.Element("media").Add(new XElement("files"));
            doc.Element("media").Add(new XElement("folders"));
            string ruta = _mediaPath;
            //guardar
            do
            {
                // Get a list of objects
                listResponse = client.ListObjects(listRequest);
                foreach (S3Object obj in listResponse.S3Objects)
                {
                    string pathKey = Path.Combine(ruta, obj.Key.Replace('/', '\\'));
                    if (Path.HasExtension(pathKey))
                    {
                        //Console.WriteLine("File: " + pathKey);
                        doc.Element("media").Element("files").Add(new XElement("object", new XAttribute("path", pathKey), new XAttribute("key", obj.Key), new XAttribute("size", obj.Size), new XAttribute("lastModified", obj.LastModified), new XAttribute("Etag", obj.ETag.Replace("\"", ""))));
                    }
                    else
                    {
                        //Console.WriteLine("Folder: " + pathKey);
                        doc.Element("media").Element("folders").Add(new XElement("object", new XAttribute("path", pathKey), new XAttribute("key", obj.Key), new XAttribute("size", obj.Size), new XAttribute("lastModified", obj.LastModified), new XAttribute("Etag", obj.ETag.Replace("\"", ""))));
                    }
                    
                }

                // Set the marker property
                listRequest.Marker = listResponse.NextMarker;
            } while (listResponse.IsTruncated);

            //Console.Write(doc);
            _xmlServer = doc;
            
            doc.Save(localXml);
        }

        private void checkObjects()
        {
            //obtiene lista de archivos
            IEnumerable<XElement> files = (from item in _xmlServer.Element("media").Element("files").Descendants("object")
                                          select item).ToList();  

            //recorre la lista de archivos y si no existe lo descarga
            foreach (XElement file in files)
            {
                string path = file.Attribute("path").Value;
                string key = file.Attribute("key").Value;
                string etag = file.Attribute("Etag").Value;
                //comprobar si no existe para descargar
                if (!File.Exists(path))
                {
                    //descargar
                    downloadObject(path, key, etag);
                    
                }
                
            }
            
        }

        /// <summary>
        /// Función que descarga un archivo
        /// </summary>
        /// <param name="path">Ruta local del archivo</param>
        /// <param name="key">Nombre (ruta) del archivo en el servidor</param>
        /// <param name="etag">Etiqueta Etag (hash) del archivo en el servidor</param>
        private void downloadObject(string path, string key, string etag)
        {
            bool descargado = false;
            //Configuración del cliente de Amazon
            AmazonS3Config conf = new AmazonS3Config();
            conf.ServiceURL = "https://s3.amazonaws.com";
            try
            {
                client = new AmazonS3Client(_accessKey, _secretKey, conf);
            }
            catch (Exception ex)
            {
                eventLog1.WriteEntry("Error Amazon S3client: " + ex.Message, EventLogEntryType.Error);
            }

            //Configuración de Bucket y clave
            GetObjectRequest request = new GetObjectRequest
            {
                BucketName = _bucket,
                Key = key
            };

            Console.WriteLine("Descargando " + path + " ...");
            
            try
            {
                //se inicia la descarga
                using (GetObjectResponse response = client.GetObject(request))
                {
                    //se guarda el fichero con el nombre y ruta final
                    response.WriteResponseStreamToFile(path);
                    descargado=true;
                }
                   

            }
            //control de excepciones de amazon
            catch (Amazon.S3.AmazonS3Exception as3ex)
            {//comprobamos que la excepción sea que el fichero no se ha encontrado
                descargado = false;
                eventLog1.WriteEntry("Error Amazon S3client: " + as3ex.Message, EventLogEntryType.Error);
                Console.WriteLine("Error Amazon " + as3ex.ErrorCode);
            }

            catch (Exception ex)
            {
                descargado = false;
                eventLog1.WriteEntry("Error Amazon S3client: " + ex.Message, EventLogEntryType.Error);
                Console.WriteLine("Error detectado:" + ex.Message);
            }

            //comprobamos si el fichero se ha descargado correctamente
            if (descargado)
            {
                Console.WriteLine(key + " descargado en :" + path);
                //calcular hash del archivo descargado
                var md5 = MD5.Create();
                var stream = File.OpenRead(path);
                string fileHash = BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLower();
                stream.Dispose(); //es necesario liberar el archivo para poder eliminarlo

                //comprobar si no coinciden los hash, para eliminar el fichero
                if (!fileHash.Equals(etag))
                {
                    Console.WriteLine("Hash incorrecto, se eliminará el fichero: " + path);
                    try
                    {
                        File.Delete(path);
                    }
                    catch (IOException exio)
                    {
                        Console.WriteLine(exio.Message);
                    }
                }

            }
            
        }

        /// <summary>
        /// Funcion que elimina los archivos/directorios locales que no existen en el servidor
        /// </summary>
        private void deleteObjects()
        {
            DirectoryInfo root = new DirectoryInfo(_mediaPath);
            
            CheckDirectoryTree(root);
        }

        /// <summary>
        /// Función que recorre un arbol de directorios y va eliminando todos sus archivos
        /// </summary>
        /// <param name="root">ruta del directorio raiz</param>
        private void CheckDirectoryTree(DirectoryInfo root)
        {
            FileInfo[] files = null;
            DirectoryInfo[] subDirs = null;

            //obtiene todos los archivos
            try
            {
                files = root.GetFiles("*.*");
            }
            catch (UnauthorizedAccessException e)
            {
                Console.WriteLine(e.Message);
            }

            catch (System.IO.DirectoryNotFoundException e)
            {
                Console.WriteLine(e.Message);
            }

            if (files != null)
            {
                //borra los archivos
                foreach (FileInfo fi in files)
                {
                    deleteObject(fi.FullName, false);
                }

                //obtiene y recorre los subdirectorios de forma recursiva
                subDirs = root.GetDirectories();

                foreach (DirectoryInfo dirInfo in subDirs)
                {
                    // Resursive call for each subdirectory.
                    string path = dirInfo.FullName + @"\";
                    bool deleted=deleteObject(path, true);
                    if (!deleted)
                    {
                        CheckDirectoryTree(dirInfo);
                    }
                    
                }
            }    
        }

        /// <summary>
        /// Funcion que elimina un directorio o un archivo especificado
        /// </summary>
        /// <param name="localObject">Ruta del archivo/directorio</param>
        /// <param name="isFolder">Variable que indica si es o no un directorio</param>
        /// <returns>True-> se ha eliminado | False ->no se ha eliminado</returns>
        private bool deleteObject(string localObject, bool isFolder)
        {
            bool exist = false;
            bool deleted = false;

            if (isFolder)
            {
                //obtiene la lista de directorios del servidor
                IEnumerable<XElement> folders = (from item in _xmlServer.Element("media").Element("folders").Descendants("object")
                                               select item).ToList();
                
                //recorre la lista para comprobar que exista el directorio buscado
                foreach (XElement folder in folders)
                {
                    string path = folder.Attribute("path").Value;
                    if (path.Equals(localObject))
                    {
                        exist = true;
                        break;
                    }

                }
            }
            else
            {
                //obtiene la lista de archivos del servidor
                IEnumerable<XElement> files = (from item in _xmlServer.Element("media").Element("files").Descendants("object")
                                               select item).ToList();
                
                //recorre la lista para comprobar que exista el archivo buscado
                foreach (XElement file in files)
                {
                    string path = file.Attribute("path").Value;

                    if (path.Equals(localObject))
                    {
                        exist = true;
                        break;
                    }

                }
            }
            
            //si no existe el objeto buscado se elimina
            if (!exist)
            {
                if (isFolder)
                {
                    DirectoryInfo folderToDelete = new DirectoryInfo(localObject);
                    if (folderToDelete.Exists)
                    {
                        folderToDelete.Delete(true);
                        deleted = true;
                        Console.WriteLine("Carpeta eliminada: " + localObject);    
                    }
                    
                }
                else
                {
                    FileInfo fileToDelete = new FileInfo(localObject);
                    if (fileToDelete.Exists)
                    {
                        deleted = true;
                        fileToDelete.Delete();
                    }
                    
                    Console.WriteLine("Archivo eliminado: " + localObject);
                }
                
            }
            return deleted;
        }

    }
}
