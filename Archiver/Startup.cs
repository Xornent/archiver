using System;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using Windows.Management.Deployment;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.DataTransfer.ShareTarget;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Streams;
using System.Data;
using System.IO;
using System.Windows;

namespace Archiver
{
    public class StartUp
    {
        [STAThread]
        public static void Main(string[] cmdArgs)
        {
            /*
            // if app isn't running with identity, register its sparse package
            if (!ExecutionMode.IsRunningWithIdentity()) {
                // TODO - update the value of externalLocation to match the output location of
                // your VS Build binaries and the value of 
                // - sparsePkgPath to match the path to your signed Sparse Package (.msix). 
                // - Note that these values cannot be relative paths and must be complete paths
                string externalLocation = System.Windows.Forms.Application.StartupPath + @"\";
                string sparsePkgPath = System.Windows.Forms.Application.StartupPath + @"\archiver.msix";

                //Attempt registration
                if (registerSparsePackage(externalLocation, sparsePkgPath)) {
                    // Registration succeded, restart the app to run with identity
                    MessageBox.Show("The package is successfully installed. Please reboot your computer");
                    // System.Diagnostics.Process.Start(externalLocation + "archiver.exe", arguments: cmdArgs?.ToString());
                    
                } else //Registration failed, run without identity
                  {
                    Debug.WriteLine("Package Registration failed, running WITHOUT Identity");
                    SingleInstanceManager wrapper = new SingleInstanceManager();
                    wrapper.Run(cmdArgs);
                }

            } else // App is registered and running with identity, handle launch and activation
              {
                // Handle Sparse Package based activation e.g Share target activation or clicking on a Tile
                // Launching the .exe directly will have activationArgs == null
                var activationArgs = AppInstance.GetActivatedEventArgs();
                if (activationArgs != null) {
                    switch (activationArgs.Kind) {
                        case ActivationKind.Launch:
                            HandleLaunch(activationArgs as LaunchActivatedEventArgs);
                            break;
                        default:
                            HandleLaunch(null);
                            break;
                    }

                }
                // This is a direct exe based launch e.g. double click app .exe or desktop
                // shortcut pointing to .exe
                else {
                    SingleInstanceManager singleInstanceManager = new SingleInstanceManager();
                    singleInstanceManager.Run(cmdArgs);
                }
            }
            */

            bool shouldRegisterB = true;
            string shouldRegister = System.Windows.Forms.Application.StartupPath + @"\should-register";
            if (File.Exists(shouldRegister)) {
                using (FileStream fs = new FileStream(shouldRegister, FileMode.Open)) {
                    using (StreamReader sr = new StreamReader(fs)) {
                        string end = sr.ReadToEnd()
                            .Replace("\r", "")
                            .Replace("\n", "");
                        if (end == "false") shouldRegisterB = false;
                    }
                }
            }

            if (shouldRegisterB) {
                // TODO - update the value of externalLocation to match the output location of
                // your VS Build binaries and the value of 
                // - sparsePkgPath to match the path to your signed Sparse Package (.msix). 
                // - Note that these values cannot be relative paths and must be complete paths
                string externalLocation = System.Windows.Forms.Application.StartupPath + @"\";
                string sparsePkgPath = System.Windows.Forms.Application.StartupPath + @"\archiver.msix";

                //Attempt registration
                if (registerSparsePackage(externalLocation, sparsePkgPath)) {
                    // Registration succeded, restart the app to run with identity
                    MessageBox.Show("The package is successfully installed. Please reboot your computer");
                    // System.Diagnostics.Process.Start(externalLocation + "archiver.exe", arguments: cmdArgs?.ToString());
                    var writer = File.CreateText(shouldRegister);
                    writer.WriteLine("false");
                    writer.Flush();
                    writer.Close();

                    SingleInstanceManager wrapper = new SingleInstanceManager();
                    wrapper.Run(cmdArgs);

                } else //Registration failed, run without identity
                {
                    SingleInstanceManager wrapper = new SingleInstanceManager();
                    wrapper.Run(cmdArgs);
                }
            } else {
                SingleInstanceManager wrapper = new SingleInstanceManager();
                wrapper.Run(cmdArgs);
            }
        }

        static void HandleLaunch(LaunchActivatedEventArgs args)
        {
            Debug.Listeners.Add(new TextWriterTraceListener(Console.Out));
            Debug.AutoFlush = true;
            Debug.Indent();
            Debug.WriteLine("WPF App using a Sparse Package");

            SingleInstanceManager singleInstanceManager = new SingleInstanceManager();
            singleInstanceManager.Run(Environment.GetCommandLineArgs());
        }

        private static bool registerSparsePackage(string externalLocation, string sparsePkgPath)
        {
            bool registration = false;
            try {
                Uri externalUri = new Uri(externalLocation);
                Uri packageUri = new Uri(sparsePkgPath);

                Console.WriteLine("exe Location {0}", externalLocation);
                Console.WriteLine("msix Address {0}", sparsePkgPath);

                Console.WriteLine("  exe Uri {0}", externalUri);
                Console.WriteLine("  msix Uri {0}", packageUri);

                PackageManager packageManager = new PackageManager();

                //Declare use of an external location
                var options = new AddPackageOptions();
                options.ExternalLocationUri = externalUri;

                Windows.Foundation.IAsyncOperationWithProgress<DeploymentResult, DeploymentProgress> deploymentOperation = packageManager.AddPackageByUriAsync(packageUri, options);

                ManualResetEvent opCompletedEvent = new ManualResetEvent(false); // this event will be signaled when the deployment operation has completed.

                deploymentOperation.Completed = (depProgress, status) => { opCompletedEvent.Set(); };

                Console.WriteLine("Installing package {0}", sparsePkgPath);

                Debug.WriteLine("Waiting for package registration to complete...");

                opCompletedEvent.WaitOne();

                if (deploymentOperation.Status == Windows.Foundation.AsyncStatus.Error) {
                    Windows.Management.Deployment.DeploymentResult deploymentResult = deploymentOperation.GetResults();
                    Debug.WriteLine("Installation Error: {0}", deploymentOperation.ErrorCode);
                    Debug.WriteLine("Detailed Error Text: {0}", deploymentResult.ErrorText);

                } else if (deploymentOperation.Status == Windows.Foundation.AsyncStatus.Canceled) {
                    Debug.WriteLine("Package Registration Canceled");
                } else if (deploymentOperation.Status == Windows.Foundation.AsyncStatus.Completed) {
                    registration = true;
                    Debug.WriteLine("Package Registration succeeded!");
                } else {
                    Debug.WriteLine("Installation status unknown");
                }
            } catch (Exception ex) {
                Console.WriteLine("AddPackageSample failed, error message: {0}", ex.Message);
                Console.WriteLine("Full Stacktrace: {0}", ex.ToString());

                return registration;
            }

            return registration;
        }

        private static void removeSparsePackage() //example of how to uninstall a Sparse Package
        {
            PackageManager packageManager = new PackageManager();
            Windows.Foundation.IAsyncOperationWithProgress<DeploymentResult, DeploymentProgress> deploymentOperation =
                packageManager.RemovePackageAsync("*Archiver_7d7zxcg7htc30");
            ManualResetEvent opCompletedEvent = new ManualResetEvent(false); // this event will be signaled when the deployment operation has completed.

            deploymentOperation.Completed = (depProgress, status) => { opCompletedEvent.Set(); };

            Debug.WriteLine("Uninstalling package ..");
            opCompletedEvent.WaitOne();
        }

        private static bool isInstalled()
        {
            PackageManager packageManager = new PackageManager();
            var package = packageManager.FindPackage("Archiver_7d7zxcg7htc30");
            return package != null;
        }
    }
}
