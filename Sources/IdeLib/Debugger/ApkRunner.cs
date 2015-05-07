﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Dot42.AdbLib;
using Dot42.ApkLib;
using Dot42.DeviceLib;
using Dot42.DeviceLib.UI;
using TallComponents.Common.Util;

namespace Dot42.Ide.Debugger
{
    public sealed class ApkRunner : IStartActivityListener
    {
        private readonly IIde ide;
        private readonly string apkPath;
        private readonly string packageName;
        private readonly string activity;
        private readonly bool debuggable;
        private readonly int launchFlags;
        private readonly IIdeOutputPane outputPane;
        private readonly int minSdkVersion;
        private Action<LauncherStates, string> stateUpdate;
        private readonly CancellationTokenSource cancellationTokenSource;

        /// <summary>
        /// Default ctor
        /// </summary>
        public ApkRunner(IIde ide, string apkPath, string packageName, string activity, bool debuggable, int launchFlags)
        {
            this.ide = ide;
            this.apkPath = apkPath;
            this.packageName = packageName;
            this.activity = activity;
            this.debuggable = debuggable;
            this.launchFlags = launchFlags;
            cancellationTokenSource = new CancellationTokenSource();
            outputPane = ide.CreateDot42OutputPane();

            // Load package
            try
            {
                var apk = new ApkFile(apkPath);
                if (!apk.Manifest.TryGetMinSdkVersion(out minSdkVersion))
                    minSdkVersion = -1;
            }
            catch (Exception ex)
            {
                minSdkVersion = -1;
                ErrorLog.DumpError(ex);
            }
        }

        /// <summary>
        /// Cancel the launch
        /// </summary>
        public void Cancel()
        {
            cancellationTokenSource.Cancel();
        }

        /// <summary>
        /// Is a cancel requested?
        /// </summary>
        public bool IsCancelled
        {
            get { return cancellationTokenSource.IsCancellationRequested; }
        }

        /// <summary>
        /// Is the given device compatible with the APK we're trying to run?
        /// </summary>
        public bool DeviceIsCompatible(DeviceItem device)
        {
            return device.IsCompatibleWith(minSdkVersion);
        }

        /// <summary>
        /// Deploy and run the package.
        /// </summary>
        public void Run(DeviceItem device, Action<LauncherStates, string> stateUpdate)
        {
            // Save argument
            this.stateUpdate = stateUpdate;

            // Prepare output
            outputPane.EnsureLoaded();

            // Start the device.
            Task.Factory.StartNew(() => RunOnStartedDevice(device.Device), cancellationTokenSource.Token);
        }

        /// <summary>
        /// Run the packet on the given (already started) device.
        /// </summary>
        private void RunOnStartedDevice(IDevice device)
        {
            // Set state
            stateUpdate(LauncherStates.Deploying, string.Empty);

            // Prepare debug launcher
            Launcher.PrepareForLaunch();

            // Install package
            if (IsCancelled) return;
            var adb = new Adb();
            adb.Logger += LogLine;
            LogLine(string.Format("----- Installing {0}", apkPath));
            adb.InstallApk(device.Serial, apkPath, packageName, Adb.Timeout.InstallApk);
            LogLine(string.Format("----- Installed {0}", apkPath));

            // Prepare for debugger attachment (if debuggable)
            if (IsCancelled) return;
            if (debuggable)
            {
                Launcher.StartLaunchMonitor(ide, device, apkPath, packageName, minSdkVersion, launchFlags, stateUpdate, cancellationTokenSource.Token);
            }

            // Run activity
            if (IsCancelled) return;            
            stateUpdate(LauncherStates.Starting, string.Empty);
            LogLine(string.Format("----- Starting activity {0}", activity));
            adb.StartActivity(device, packageName, activity, debuggable, Adb.Timeout.StartActivity, this);
            LogLine(string.Format("----- Started activity {0}", activity));
            stateUpdate(LauncherStates.Started, string.Empty);
        }


        /// <summary>
        /// Write a message to the output pane.
        /// </summary>
        private void LogLine(string message)
        {
            outputPane.LogLine(message);
        }

        /// <summary>
        /// Command has finished.
        /// </summary>
        void IStartActivityListener.Completed()
        {
        }

        /// <summary>
        /// Should further processing be cancelled?
        /// </summary>
        bool IStartActivityListener.IsCancelled
        {
            get { return false; }
        }
    }
}
