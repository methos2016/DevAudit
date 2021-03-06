﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Alpheus;

namespace DevAudit.AuditLibrary
{
    public class MySQLServer : ApplicationServer
    {
        #region Constructors
        public MySQLServer(Dictionary<string, object> server_options, EventHandler<EnvironmentEventArgs> message_handler) : base(server_options, 
            new Dictionary<PlatformID, string[]>()
            {
                { PlatformID.Unix,  new string[] { "find", "@", "*bin", "mysqld" } },
                { PlatformID.Win32NT, new string[] { "@", "bin", "mysqld.exe" } }
            },
            new Dictionary<PlatformID, string[]>()
            {
                { PlatformID.Unix, new string[] { "find", "@", "etc", "*", "mysql", "my.cnf" } },
                { PlatformID.Win32NT, new string[] { "@", "my.ini" } }
            }, new Dictionary<string, string[]>(), new Dictionary<string, string[]>(), message_handler)
        {
            if (this.ApplicationBinary != null)
            {
                this.ApplicationFileSystemMap["mysqld"] = this.ApplicationBinary;
            }
        }
        #endregion

        #region Overriden properties
        public override string ServerId { get { return "mysql"; } }

        public override string ServerLabel { get { return "MySQL"; } }

        public override PackageSource PackageSource => this as PackageSource;
        #endregion

        #region Overriden methods
        protected override Dictionary<string, IEnumerable<OSSIndexQueryObject>> GetModules()
        {
            Dictionary<string, IEnumerable<OSSIndexQueryObject>> m = new Dictionary<string, IEnumerable<OSSIndexQueryObject>>
            {
                {"mysqld", new List<OSSIndexQueryObject> {new OSSIndexQueryObject(this.PackageManagerId, "mysqld", this.Version) }}
            };
            this.ModulePackages = m;
            this.PackageSourceInitialized =  this.ModulesInitialised = true;
            return this.ModulePackages;
        }

        protected override string GetVersion()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            AuditEnvironment.ProcessExecuteStatus process_status;
            string process_output;
            string process_error;
            AuditEnvironment.Execute(this.ApplicationBinary.FullName, "-V", out process_status, out process_output, out process_error);
            sw.Stop();
            if (process_status == AuditEnvironment.ProcessExecuteStatus.Completed)
            {
                this.Version = process_output.Substring(process_output.IndexOf("Ver") + 4);
                this.VersionInitialised = true;
                this.AuditEnvironment.Success("Got {0} version {1} in {2} ms.", this.ApplicationLabel, this.Version, sw.ElapsedMilliseconds);
                return this.Version;
            }
            else
            {
                throw new Exception(string.Format("Did not execute process {0} successfully. Error: {1}.", this.ApplicationBinary.Name, process_error));
            }
        }

        protected override IConfiguration GetConfiguration()
        {
            MySQL mysql = new MySQL(this.ConfigurationFile);
            ;
            if (mysql.ParseSucceded)
            {
                this.Configuration = mysql;
                this.ConfigurationInitialised = true;
            }
            else
            {
                this.AuditEnvironment.Error("Could not parse configuration from {0}.", mysql.FullFilePath);
                if (mysql.LastParseException != null) this.AuditEnvironment.Error(mysql.LastParseException);
                if (mysql.LastIOException != null) this.AuditEnvironment.Error(mysql.LastIOException);
                this.Configuration = null;
                this.ConfigurationInitialised = false;
            }
            return this.Configuration;
        }


        public override IEnumerable<OSSIndexQueryObject> GetPackages(params string[] o)
        {
            if (!this.ModulesInitialised) throw new InvalidOperationException("Modules must be initialised before GetPackages is called.");
            return this.ModulePackages["mysqld"];
        }

        public override bool IsConfigurationRuleVersionInServerVersionRange(string configuration_rule_version, string server_version)
        {
            return (configuration_rule_version == server_version) || configuration_rule_version == ">0";
        }
        

        public override bool IsVulnerabilityVersionInPackageVersionRange(string vulnerability_version, string package_version)
        {
            return vulnerability_version == package_version;
        }
        #endregion
    }
}
