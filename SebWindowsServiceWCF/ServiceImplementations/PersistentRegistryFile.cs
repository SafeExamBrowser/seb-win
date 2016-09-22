using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using SEBWindowsServiceContracts;

namespace SebWindowsServiceWCF.ServiceImplementations
{
    /// <summary>
    /// The class that gets serialized, this is a subclass for 
    /// </summary>
    [Serializable]
    public class FileContent
    {
        public Dictionary<RegistryIdentifiers, object> RegistryValues
        { get; set; }
        public string Username
        { get; set; }
        public string SID
        { get; set; }
        public bool EnableWindowsUpdate
        { get; set; }
    }

    public class PersistentRegistryFile : IDisposable
    {
        private readonly string _filePath;
        public FileContent FileContent = new FileContent()
        {
            EnableWindowsUpdate = false, 
            RegistryValues = new Dictionary<RegistryIdentifiers, object>(), 
            Username = "",
            SID = ""
        };

        /// <summary>
        /// Create an in-memory instance of a persistent registry file.
        /// If a file is existing it gets automatically loaded into memory
        /// </summary>
        /// <param name="username">The username of the currently logged in user - needed to identify the correct registry key path</param>
        /// <param name="sid">The sid of the currently logged in user - needed to identify the correct registry key path</param>
        public PersistentRegistryFile(string username = null, string sid = null)
        {
            try
            {
                //The file is stored where the executable of the service is
                _filePath = String.Format(@"{0}\sebregistry.srg", Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            }
            catch (Exception ex)
            {
                Logger.Log(ex,"Unable to build path for persistent registry file");
                throw;
            }
            

            if (username != null)
                this.FileContent.Username = username;

            if (sid != null)
                this.FileContent.SID = sid;

            if (File.Exists(_filePath))
            {
                Logger.Log("Loading persistent registry file");
                Load();
            }
            else
            {
                Logger.Log("Persistent registry file doesn't exist at path "+_filePath);
            }
        }

        /// <summary>
        /// Loads the content of a saved file into memory
        /// </summary>
        private void Load()
        {
            FileStream stream = null;
            try
            {
                stream = File.OpenRead(_filePath);
                var deserializer = new BinaryFormatter();
                FileContent = (FileContent)deserializer.Deserialize(stream);
                stream.Close();
            }
            catch (Exception ex)
            {
                if (stream != null)
                    stream.Close();
                Logger.Log(ex, String.Format("Unable to open persistent registry file:{0}",_filePath));
            }
        }

        /// <summary>
        /// Saves the currently stored registry information into a binary encoded file
        /// Throws Exception if something goes wrong
        /// </summary>
        public void Save()
        {
            FileStream stream = null;
            try
            {
                Delete();
                stream = File.OpenWrite(_filePath);
                var serializer = new BinaryFormatter();
                serializer.Serialize(stream, FileContent);
                stream.Close();
            }
            catch (Exception ex)
            {
                if(stream != null)
                    stream.Close();
                Logger.Log(ex, String.Format("Unable to save persistent registry file: {0}",_filePath));
                throw;
            }
        }

        /// <summary>
        /// Delete the persistens registry file if it exists
        /// Throws Exception if something goes wrong
        /// </summary>
        public void Delete()
        {
            try
            {
                if (File.Exists(_filePath))
                    File.Delete(_filePath);
            }
            catch (Exception ex)
            {
                Logger.Log(ex, String.Format("Unable to delete persistent registry reset file: {0}",_filePath));
                throw ex;
            }
        }

        public void Dispose()
        {
            this.FileContent = null;
        }
    }
}
