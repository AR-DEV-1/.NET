﻿using CmlLib.Utils;
using Newtonsoft.Json.Linq;
using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace CmlLib.Core.Installer
{
    public class MJava
    {
        public static string DefaultRuntimeDirectory = Path.Combine(MinecraftPath.GetOSDefaultPath(), "runtime");

        public event ProgressChangedEventHandler ProgressChanged;
        public event EventHandler DownloadCompleted;
        public string RuntimeDirectory { get; private set; }

        IProgress<ProgressChangedEventArgs> pProgressChanged;

        public MJava() : this(DefaultRuntimeDirectory) { }

        public MJava(string runtimePath)
        {
            RuntimeDirectory = runtimePath;
        }

        public string GetDefaultBinaryName()
        {
            var binaryName = "java";
            if (MRule.OSName == MRule.Windows)
                binaryName = "javaw.exe";
            return binaryName;
        }

        public string CheckJava()
        {
            var binaryName = GetDefaultBinaryName();
            return CheckJava(binaryName);
        }

        public async Task<string> CheckJavaAsync()
        {
            var binaryName = GetDefaultBinaryName();
            return await CheckJavaAsync(binaryName);
        }

        public string CheckJava(string binaryName)
        {
            pProgressChanged = new Progress<ProgressChangedEventArgs>(
                (e) => ProgressChanged?.Invoke(this, e));

            var javapath = Path.Combine(RuntimeDirectory, "bin", binaryName);

            if (!File.Exists(javapath))
            {
                var javaUrl = GetJavaUrl();
                var lzmaPath = DownloadJavaLzma(javaUrl);

                DecompressJavaFile(lzmaPath);

                if (!File.Exists(javapath))
                    throw new Exception("Failed Download");

                if (MRule.OSName != MRule.Windows)
                    IOUtil.Chmod(javapath, IOUtil.Chmod755);
            }

            return javapath;
        }

        public async Task<string> CheckJavaAsync(string binaryName)
        {
            pProgressChanged = new Progress<ProgressChangedEventArgs>(
    (e) => ProgressChanged?.Invoke(this, e));

            var javapath = Path.Combine(RuntimeDirectory, "bin", binaryName);

            if (!File.Exists(javapath))
            {
                var javaUrl = await GetJavaUrlAsync();
                var lzmaPath = await DownloadJavaLzmaAsync(javaUrl);

                var decompressTask = Task.Run(() => DecompressJavaFile(lzmaPath));
                await decompressTask;

                if (!File.Exists(javapath))
                    throw new Exception("Failed Download");

                if (MRule.OSName != MRule.Windows)
                    IOUtil.Chmod(javapath, IOUtil.Chmod755);
            }

            return javapath;
        }

        private string GetJavaUrl()
        {
            using (var wc = new WebClient())
            {
                var json = wc.DownloadString(MojangServer.LauncherMeta);
                return parseLauncherMetadata(json);
            }
        }

        private async Task<string> GetJavaUrlAsync()
        {
            using (var wc = new WebClient())
            {
                var json = await wc.DownloadStringTaskAsync(MojangServer.LauncherMeta);
                return parseLauncherMetadata(json);
            }
        }

        private string parseLauncherMetadata(string json)
        {
            var job = JObject.Parse(json)[MRule.OSName];
            var javaUrl = job[MRule.Arch]?["jre"]?["url"]?.ToString();

            if (string.IsNullOrEmpty(javaUrl))
                throw new PlatformNotSupportedException("Downloading JRE on current OS is not supported. Set JavaPath manually.");
            return javaUrl;
        }

        private string DownloadJavaLzma(string javaUrl)
        {
            Directory.CreateDirectory(RuntimeDirectory);
            var lzmapath = Path.Combine(Path.GetTempPath(), "jre.lzma");

            var webdownloader = new WebDownload();
            webdownloader.DownloadProgressChangedEvent += Downloader_DownloadProgressChangedEvent;
            webdownloader.DownloadFile(javaUrl, lzmapath);
            DownloadCompleted?.Invoke(this, new EventArgs());

            return lzmapath;
        }

        private async Task<string> DownloadJavaLzmaAsync(string javaUrl)
        {
            Directory.CreateDirectory(RuntimeDirectory);
            var lzmapath = Path.Combine(Path.GetTempPath(), "jre.lzma");

            using (var wc = new WebClient())
            {
                wc.DownloadProgressChanged += Downloader_DownloadProgressChangedEvent;
                await wc.DownloadFileTaskAsync(javaUrl, lzmapath);
            }

            DownloadCompleted?.Invoke(this, new EventArgs());

            return lzmapath;
        }

        private void DecompressJavaFile(string lzmaPath)
        {
            var zippath = Path.Combine(Path.GetTempPath(), "jre.zip");
            SevenZipWrapper.DecompressFileLZMA(lzmaPath, zippath);

            var z = new SharpZip(zippath);
            z.ProgressEvent += Z_ProgressEvent;
            z.Unzip(RuntimeDirectory);
        }

        private void Z_ProgressEvent(object sender, int e)
        {
            pProgressChanged.Report(new ProgressChangedEventArgs(50 + e / 2, null));
        }

        private void Downloader_DownloadProgressChangedEvent(object sender, ProgressChangedEventArgs e)
        {
            ProgressChanged?.Invoke(this, new ProgressChangedEventArgs(e.ProgressPercentage / 2, null));
        }
    }
}
