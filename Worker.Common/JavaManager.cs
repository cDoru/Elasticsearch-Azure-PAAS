﻿using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace ElasticsearchWorker
{
    public class JavaManager : SoftwareManager
    {
        public const string JAVA_HOME = "JAVA_HOME";
        public const string JAVA_REGISTRY_PATH = "HKEY_LOCAL_MACHINE\\SOFTWARE\\JavaSoft\\Java Runtime Environment";
        public const string INSTALL_LOG_FILE = "jdk.txt";

        public JavaManager(IElasticsearchServiceSettings settings )
            :base(settings,settings.JavaInstaller)
        {
            if (!string.IsNullOrWhiteSpace(settings.JavaDownloadType) && settings.JavaDownloadType == "storage")
            {
                _installer = new StorageArtifact(settings.JavaDownloadURL, settings.JavaInstaller, settings.StorageAccount);
            }
            else
            {
                _installer = new WebArtifact(settings.JavaDownloadURL, settings.JavaInstaller);
            }
            
        }

        public override Task EnsureConfigured()
        {
            return Task.Factory.StartNew(() =>
            {
                //if no binary then java is definitely not installed
                DownloadIfNotExists();

                //If java registry keys are not set run the installer
                InstallIfNeeded();

                //Java home is not confugerd so lets do that
                SetJavaHomeIfNeeded();

            });
        


         
            
        }

        protected virtual bool JavaHomeConfigured()
        {
            var javaHome = Environment.GetEnvironmentVariable(JAVA_HOME, EnvironmentVariableTarget.Machine);
            var configured = !string.IsNullOrWhiteSpace(javaHome);
            return configured;
        }

        protected virtual string JavaVersion()
        {
            var value = (string)Registry.GetValue(JAVA_REGISTRY_PATH,"CurrentVersion",string.Empty);
            return value;
        }

        protected virtual void InstallIfNeeded()
        {
            if (string.IsNullOrWhiteSpace(JavaVersion()))
            {
                var installLog = Path.Combine(_logRoot, INSTALL_LOG_FILE);
                var pi = new ProcessStartInfo(_binaryArchive)
                {
                    Arguments = " /s /L " + installLog,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    Verb = "runas"
                };

                Trace.TraceInformation("Java not installed. Starting installer");
                var installer = Process.Start(pi);
                var error = installer.StandardError.ReadToEnd();
                installer.WaitForExit();

                if (installer.ExitCode > 0)
                {
                    throw new Exception("Java installation failed: " + error);
                }

                Trace.TraceInformation("Java installer complete");
            }
        }

        protected virtual void SetJavaHomeIfNeeded()
        {
            if (!JavaHomeConfigured())
            {
                var javaHome = GetJavaHomeFromReg();

                Trace.TraceInformation("Configuring JAVA_HOME");
                Environment.SetEnvironmentVariable(JAVA_HOME, javaHome, EnvironmentVariableTarget.Machine);
            }
        }

        public virtual string GetJavaHomeFromReg()
        {
            var javaVersion = JavaVersion();
            var path = Path.Combine(JAVA_REGISTRY_PATH, javaVersion);
            var javaHome = (string)Registry.GetValue(path, "JavaHome", string.Empty);
            return javaHome;
        }
    }
}
