﻿using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using System;
using System.IO;
using System.Runtime.InteropServices;


namespace ElasticsearchWorker.Core
{
    public class ElasticsearchServiceSettings : IElasticsearchServiceSettings
    {
        //Init only via static methods
        private ElasticsearchServiceSettings(){}

        //settings
        protected CloudStorageAccount _StorageAccount;
        protected string _NodeName;
        protected string _UseElasticLocalDataFolder;
        protected string _JavaInstaller;
        protected string _JavaDownloadURL;
        protected string _JavaDownloadType;
        protected string _ElasticsearchInstaller;
        protected string _ElasticsearchDownloadURL;
        protected string _ElasticsearchDownloadType;
        protected string _ElasticsearchPluginContainer;
        protected string _DataShareName;
        protected string _DataShareDrive;
        protected string _EndpointName;
        protected string _DownloadDirectory;
        protected string _LogDirectory;
        protected string _DataDirectory;
        protected string _ElasticsearchDirectory;
        protected string _RootDirectory;
        protected string _TempDirectory;
        protected bool _IsAzure;
        protected bool _IsEmulated;
        protected int _ComputedHeapSize;
        protected bool _EnableDataBootstrap;
        protected string _DataBootstrapDirectory;

        /// <summary>
        /// Init with a storage account
        /// </summary>
        /// <param name="account">Cloud Storage Account</param>
        public static ElasticsearchServiceSettings FromStorage(CloudStorageAccount account)
        { 
            var settings = new ElasticsearchServiceSettings()
            {
                _StorageAccount = account,
                _NodeName = RoleEnvironment.CurrentRoleInstance.Id,
                _UseElasticLocalDataFolder = RoleEnvironment.GetConfigurationSettingValue("UseElasticLocalDataFolder"),
                _JavaInstaller = RoleEnvironment.GetConfigurationSettingValue("JavaInstallerName"),
                _JavaDownloadURL = RoleEnvironment.GetConfigurationSettingValue("JavaDownloadURL"),
                _JavaDownloadType = RoleEnvironment.GetConfigurationSettingValue("JavaDownloadType"),
                _ElasticsearchInstaller = RoleEnvironment.GetConfigurationSettingValue("ElasticsearchZip"),
                _ElasticsearchDownloadURL = RoleEnvironment.GetConfigurationSettingValue("ElasticsearchDownloadURL"),
                _ElasticsearchDownloadType = RoleEnvironment.GetConfigurationSettingValue("ElasticsearchDownloadType"),
                _ElasticsearchPluginContainer = RoleEnvironment.GetConfigurationSettingValue("ElasticsearchPluginContainer"),
                _DataShareName = RoleEnvironment.GetConfigurationSettingValue("ShareName"),
                _DataShareDrive = RoleEnvironment.GetConfigurationSettingValue("ShareDrive"),
                _EndpointName = RoleEnvironment.GetConfigurationSettingValue("EndpointName"),
                _DownloadDirectory = RoleEnvironment.GetLocalResource("ArchiveRoot").RootPath,
                _LogDirectory = RoleEnvironment.GetLocalResource("LogRoot").RootPath,
                _DataDirectory = RoleEnvironment.GetLocalResource("ElasticDataRoot").RootPath,
                _ElasticsearchDirectory = RoleEnvironment.GetLocalResource("ElasticRoot").RootPath,
                _RootDirectory = Environment.GetEnvironmentVariable("ROLEROOT"),
                _TempDirectory = RoleEnvironment.GetLocalResource("CustomTempRoot").RootPath,
                _DataBootstrapDirectory = RoleEnvironment.GetLocalResource("DataBootstrapDirectory").RootPath,

            };

            bool.TryParse(RoleEnvironment.GetConfigurationSettingValue("EnableDataBootstrap"), out settings._EnableDataBootstrap);

            if (string.IsNullOrWhiteSpace(settings._DataBootstrapDirectory) && settings._EnableDataBootstrap)
            {
                settings._EnableDataBootstrap = false;
            }


            if (!settings._RootDirectory.EndsWith(@"\"))
            {
                settings._RootDirectory += @"\";
            }

            //Set root to approot=App directory
            settings._RootDirectory = Path.Combine(settings._RootDirectory, "approot");

            //Calculate heap size
            MEMORYSTATUSEX memoryStatus = new MEMORYSTATUSEX();
            GlobalMemoryStatusEx(memoryStatus);

            var totalPhycialBytesInMB = memoryStatus.ullTotalPhys / 1024L / 1024L;

            //TODO: calculate the lost result which could cause this to throw;
            settings._ComputedHeapSize = Convert.ToInt32(totalPhycialBytesInMB / 2); 

            return settings;
        }

        /// <summary>
        /// Init with a connection string
        /// </summary>
        /// <param name="connectionString">Storage Connection String</param>
        public static ElasticsearchServiceSettings FromStorage(string connectionString)
        {
            var account = CloudStorageAccount.Parse(connectionString);
            return FromStorage(account);
        }

        public CloudStorageAccount StorageAccount{ get { return _StorageAccount; } }
        public string NodeName { get { return _NodeName; } }
        public string UseElasticLocalDataFolder { get { return _UseElasticLocalDataFolder; } }
        public string JavaInstaller { get { return _JavaInstaller; } }
        public string JavaDownloadURL { get { return _JavaDownloadURL; } }
        public string JavaDownloadType { get { return _JavaDownloadType; } }
        public string ElasticsearchInstaller { get { return _ElasticsearchInstaller; } }
        public string ElasticsearchDownloadURL { get { return _ElasticsearchDownloadURL; } }
        public string ElasticsearchPluginContainer { get { return _ElasticsearchPluginContainer; } }
        public string ElasticsearchDownloadType { get { return _ElasticsearchDownloadType; } }
        public string DataShareName { get { return _DataShareName; } }
        public string DataShareDrive { get { return _DataShareDrive; } }
        public string EndpointName { get { return _EndpointName; } }
        public string DownloadDirectory { get { return _DownloadDirectory; } }
        public string LogDirectory { get { return _LogDirectory; } }
        public string DataDirectory { get { return _DataDirectory; } }
        public string ElasticsearchDirectory { get { return _ElasticsearchDirectory; } }
        public string RootDirectory { get { return _RootDirectory; } }
        public string TempDirectory { get { return _TempDirectory; } }
        public bool IsAzure { get { return RoleEnvironment.IsAvailable; } }
        public bool IsEmulated { get { return RoleEnvironment.IsEmulated; } }
        public int ComputedHeapSize { get { return _ComputedHeapSize; } }
        public bool EnableDataBootstrap { get { return _EnableDataBootstrap;  } }
        public string DataBootstrapDirectory { get { return _DataBootstrapDirectory; } }
        public string GetExtra(string key)
        {
            return RoleEnvironment.GetConfigurationSettingValue(key);
        }


        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private class MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
            public MEMORYSTATUSEX()
            {
                this.dwLength = (uint)Marshal.SizeOf(this);
            }
        }


        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);
    }
}
