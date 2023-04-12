using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Design;
using System.Globalization;
using Task = System.Threading.Tasks.Task;
using System.Management.Automation;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using NuGet.Protocol.Core.Types;
using NuGet.Configuration;
using System.Threading;
using NuGet.Versioning;
using NuGet.Protocol;
using NuGet.Common;
using System.Linq;
using System.IO;
using System.Xml.Linq;
using Microsoft.Build.Construction;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Imaging;
using System.Windows;
using System.Windows.Forms;
using System.Drawing;
using Newtonsoft.Json;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Diagnostics;
using EnvDTE;
using System.Timers;

namespace MonitorNugetUpdates
{
    /// <summary>
    /// Command handler
    /// </summary>
    /// 


    // ...



    internal sealed class UpdatesMonitor
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("69d35a84-fc9a-4390-a122-0e4b5b42b527");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;
        private System.Timers.Timer _timer;
        private bool _isMessageShown = false;
        private DateTime _lastDisplayedTime = DateTime.MinValue;
        private static bool _isEncounteredProblemWhileConnecting = false;

        public enum __VSSNCONSTANTS
        {
            SN_NOTIFICATIONS_INFORMATION = 1
        }

        public enum __VSSNFLAGS
        {
            VSSNFLAGS_NONE = 0
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetWindowRect(HandleRef hWnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdatesMonitor"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private UpdatesMonitor(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static UpdatesMonitor Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in UpdatesMonitor's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new UpdatesMonitor(package, commandService);

            //await ExecuteAsync();
            // await ListPackageVersionsAsync();
            await Instance.ExecuteForUpdateCheckAsync();
            Instance.StartTimer();
            //ShowMessage("Lot of linessssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssssss\nsssssssssssssssssssssssssssssssssssssssssssss\nssssssssssssssssssssssssss");
        }

        public static async Task ListPackageVersionsAsync()
        {
            // This code region is referenced by the NuGet docs. Please update the docs if you rename the region
            // or move it to a different file.
            #region ListPackageVersions
            ILogger logger = NullLogger.Instance;
            CancellationToken cancellationToken = CancellationToken.None;

            SourceCacheContext cache = new SourceCacheContext();
            SourceRepository repository = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
            FindPackageByIdResource resource = await repository.GetResourceAsync<FindPackageByIdResource>();

            IEnumerable<NuGetVersion> versions = await resource.GetAllVersionsAsync(
                "Newtonsoft.Json",
                cache,
                logger,
                cancellationToken);
            NuGetVersion latestVersion = versions.Last();

            foreach (NuGetVersion version in versions)
            {
                Console.WriteLine($"Found version {version}");
            }
            #endregion
        }

        //private static async Task ExecuteAsync()
        //{
        //    using (PowerShell powerShell = PowerShell.Create())
        //    {
        //        // Source functions.//get from user
        //        powerShell.AddCommand("Set-Location").AddParameter("Path", "C:\\Users\\ag7\\Documents\\udcwindows\\Build\\");
        //        powerShell.AddScript("Get-Content C:\\Users\\ag7\\Documents\\udcwindows\\Build\\UDClient.sln | where { $_ -match \"Project.+, \"\"(.+)\\\\([^\\\\]+).vcxproj\"\", \" }");
        //        string script = "Get-Content C:\\Users\\ag7\\Documents\\udcwindows\\Build\\UDClient.sln | where { $_ -match \"Project.+, \"\"(.+)\\\\([^\\\\]+).vcxproj\"\", \" } | foreach { \"$($matches[1])\\packages.config\" } | % { Get-Content $_ | Find \"<package id\" } | Sort-Object -Unique";
        //        powerShell.AddScript(script);
        //        // invoke execution on the pipeline (collecting output)
        //        try
        //        {
        //            Collection<PSObject> PSOutput = powerShell.Invoke();
        //            foreach (PSObject outputItem in PSOutput)
        //            {
        //                // if null object was dumped to the pipeline during the script then a null object may be present here
        //                outputItem.
        //                if (outputItem != null)
        //                {
        //                    Console.WriteLine($"Output line: [{outputItem}]");
        //                }
        //            }

        //            // check the other output streams (for example, the error stream)
        //            if (powerShell.Streams.Error.Count > 0)
        //            {
        //                // error records were written to the error stream.
        //                // Do something with the error
        //            }
        //        }
        //        catch (Exception ex)
        //        {

        //        }

        //        // loop through each output object item

        //    }
        //}

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            string message = string.Format(CultureInfo.CurrentCulture, "Inside {0}.MenuItemCallback()", this.GetType().FullName);
            string title = "UpdatesMonitor";

            // Show a message box to prove we were here
            VsShellUtilities.ShowMessageBox(
                this.package,
                message,
                title,
                OLEMSGICON.OLEMSGICON_INFO,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }

        private async Task ExecuteForUpdateCheckAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            try
            {
                DTE dte = (DTE)GetService(typeof(DTE));
                string solutionPath = dte.Solution.FullName;

                var nugetConfigPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "NuGet",
                    "NuGet.Config");
                if (!File.Exists(nugetConfigPath))
                {
                    //Console.WriteLine("nuget.config file not found.");
                    return;
                }


                IEnumerable<NugetPackageInfo> installedPackages = null;


                //ISettings settings = new Settings(parentNugetConfigPath);
                //PackageSourceProvider pSP = new PackageSourceProvider(settings);
                //var v3 = Repository.Provider.GetCoreV3();
                //var sourceRepositoryProvider = new SourceRepositoryProvider(pSP,
                //    v3);
                //var sourceRepository = sourceRepositoryProvider.GetRepositories().FirstOrDefault();
                //if (sourceRepository == null)
                //{
                //    //Console.WriteLine("No package sources found in nuget.config.");
                //    return;
                //}
                var solution = SolutionFile.Parse(solutionPath);
                //var metadataResource = await sourceRepository.GetResourceAsync<MetadataResource>();

                foreach (var project in solution.ProjectsInOrder)
                {
                    // Console.WriteLine($"Project: {project.ProjectName}");
                    var pathToProject = Path.GetDirectoryName(project.RelativePath);
                    var packagesConfigPath = Path.Combine(
                        Path.GetDirectoryName(solutionPath),
                        pathToProject,
                        "packages.config");
                    if (!File.Exists(packagesConfigPath))
                    {
                        Console.WriteLine(" packages.config file not found.");
                        continue;
                    }

                    var packagesConfig = XDocument.Load(packagesConfigPath);
                    if (installedPackages == null)
                    {
                        installedPackages = packagesConfig
         .Element("packages")
         .Elements("package")
         .Select(p => new NugetPackageInfo
         {
             Id = p.Attribute("id").Value,
             Version = p.Attribute("version").Value
         })
         .Distinct();
                    }
                    else
                    {
                        installedPackages = installedPackages.Concat(packagesConfig
                 .Element("packages")
                 .Elements("package")
                 .Select(p => new NugetPackageInfo
                 {
                     Id = p.Attribute("id").Value,
                     Version = p.Attribute("version").Value
                 }));



                    }
                }

                var uniquePackages = installedPackages.Distinct().ToList();
                await Instance.CompareVersionsOfPackagesAsync(uniquePackages);
            }
            catch (Exception e)
            {

            }
        }

        private async Task CompareVersionsOfPackagesAsync(IEnumerable<NugetPackageInfo> installedPackages)
        {
            _isEncounteredProblemWhileConnecting = false;
            Instance._isMessageShown = false;
            try
            {
                var parentNugetConfigPath = Path.Combine(
                   Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                   "NuGet");

                var packageSourceProvider = new PackageSourceProvider(new Settings(parentNugetConfigPath));
                var packageSources = packageSourceProvider.LoadPackageSources();
                var packagesThatNeedUpdate = "";
                var latestVersionString = "";
                List<String> packageNamesList = new List<string>();
                List<String> latestVersionList = new List<string>();

                foreach (var installedPackage in installedPackages)
                {

                    var packageId = installedPackage.Id;
                    var installedVersion = new NuGetVersion(installedPackage.Version);
                    var latestVersion = new NuGetVersion("0.0.0.0");
                    //try catch?
                    foreach (var packageSource in packageSources)
                    {
                        var timeout = TimeSpan.FromSeconds(120); // Timeout after 2 minutes
                        var sourceRepository = Repository.Factory.GetCoreV3(packageSource);
                        var metadataResourceTask = sourceRepository.GetResourceAsync<MetadataResource>();

                        var completedTask = await Task.WhenAny(metadataResourceTask, Task.Delay(timeout));

                        if (completedTask != metadataResourceTask)
                        {
                            _isEncounteredProblemWhileConnecting = true;
                            continue;
                        }
                        else
                        {
                            var metadataResource = await metadataResourceTask;
                            // Handle successful scenario

                            try
                            {
                                latestVersion = await metadataResource.GetLatestVersion(packageId, includePrerelease: false, includeUnlisted: false, sourceCacheContext: null, token: CancellationToken.None, log: NullLogger.Instance);
                            }
                            catch (Exception ex)
                            {
                                latestVersion = null;
                            }

                            if (latestVersion != null)
                            {
                                // Found the latest version of the package in this package source
                                Console.WriteLine($"Found latest version of {packageId} in {packageSource.Name}: {latestVersion.ToFullString()}");
                                if (installedVersion < latestVersion)
                                {
                                    packagesThatNeedUpdate += packageId + "\n";
                                    latestVersionString = latestVersion.ToString();
                                    packageNamesList.Add(packageId);
                                    latestVersionList.Add(latestVersionString);
                                }
                                break;
                            }
                        }

                    }
                }
                //packagesThatNeedUpdate = "There are a few packages that needs update!\n" + packagesThatNeedUpdate;
                string instruction = "To install these packages, Right click your solution and click on Manage Nuget Packages.";
                packagesThatNeedUpdate = "Packages" + "\n" + packagesThatNeedUpdate + "\n" + "\n" + instruction;
                if (packageNamesList.Count > 0)
                {
                    Instance.ShowMessage(packageNamesList, latestVersionList);
                    Instance._lastDisplayedTime = DateTime.Now;
                    Instance._isMessageShown = true;
                }

            }
            catch (Exception e)
            {

            }
        }
        public void ShowMessage(List<String> packagesList, List<String> latestVersionList)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            IServiceProvider serviceProvider = new ServiceProvider(Package.GetGlobalService(typeof(SDTE)) as Microsoft.VisualStudio.OLE.Interop.IServiceProvider);
            var notificationService = new NotificationService(serviceProvider);
            notificationService.ShowMessage("NuGet Package Updates", packagesList, latestVersionList);



        }



        private static object GetService(Type type)
        {
            return Package.GetGlobalService(type);
        }

        public void StartTimer()
        {
            TimeSpan interval;

            if (_isEncounteredProblemWhileConnecting)
            {
                interval = TimeSpan.FromHours(2);
            }
            else if (_isMessageShown == true)
            {
                interval = TimeSpan.FromHours(72);
            }
            else
            {
                return;
            }

            _timer = new System.Timers.Timer(interval.TotalMilliseconds);
            _timer.Elapsed += OnTimerElapsed;
            _timer.AutoReset = false;
            _timer.Start();
        }

        private async void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            if (_isEncounteredProblemWhileConnecting || (_isMessageShown == true && DateTime.Now.Subtract(_lastDisplayedTime).TotalHours > 72))
            {
                // Call the method to check for updates
                await ExecuteForUpdateCheckAsync();
            }

            // Start the timer again
            StartTimer();
        }
    }
        class PackageEqualityComparer : IEqualityComparer<NugetPackageInfo>
    {
        public bool Equals(NugetPackageInfo x, NugetPackageInfo y)
        {
            return x is { } xValue && y is { } yValue &&
                string.Equals((string)(xValue.GetType().GetProperty("Id")?.GetValue(xValue, null)),
                              (string)(yValue.GetType().GetProperty("Id")?.GetValue(yValue, null)),
                              StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode(NugetPackageInfo obj)
        {
            return obj?.GetType().GetProperty("Id")?.GetValue(obj, null)?.GetHashCode() ?? 0;
        }
    }

    public class NugetPackageInfo
    {
        public string Id;
        public string Version;
        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is NugetPackageInfo))
            {
                return false;
            }

            return this.Id == ((NugetPackageInfo)obj).Id;
        }

        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 23 + Id.GetHashCode();
            hash = hash * 23 + Version.GetHashCode();
            return hash;
        }
    }
}


