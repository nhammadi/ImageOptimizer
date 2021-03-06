﻿using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;

namespace MadsKristensen.ImageOptimizer
{
    public class Compressor
    {
        static readonly string[] _supported = { ".png", ".jpg", ".jpeg", ".gif" };
        string _cwd;

        public Compressor()
        {
            _cwd = Path.Combine(Path.GetDirectoryName(GetType().Assembly.Location), @"Resources\Tools\");
        }

        public Compressor(string cwd)
        {
            _cwd = cwd;
        }

        public CompressionResult CompressFileAsync(string fileName, bool lossy)
        {
            string targetFile = Path.ChangeExtension(Path.GetTempFileName(), Path.GetExtension(fileName));

            ProcessStartInfo start = new ProcessStartInfo("cmd")
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                WorkingDirectory = _cwd,
                Arguments = GetArguments(fileName, targetFile, lossy),
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            var stopwatch = Stopwatch.StartNew();

            using (var process = Process.Start(start))
            {
                process.WaitForExit();
            }

            stopwatch.Stop();

            return new CompressionResult(fileName, targetFile, stopwatch.Elapsed);
        }

        private static string GetArguments(string sourceFile, string targetFile, bool lossy)
        {
            if (!Uri.IsWellFormedUriString(sourceFile, UriKind.RelativeOrAbsolute) && !File.Exists(sourceFile))
                return null;

            string ext;

            try
            {
                ext = Path.GetExtension(sourceFile).ToLowerInvariant();
            }
            catch (ArgumentException ex)
            {
                Logger.Log(ex);
                return null;
            }

            switch (ext)
            {
                case ".png":
                    if (lossy)
                        return string.Format(CultureInfo.CurrentCulture, "/c png-lossy.cmd \"{0}\" \"{1}\"", sourceFile, targetFile);

                    return string.Format(CultureInfo.CurrentCulture, "/c png-lossless.cmd \"{0}\" \"{1}\"", sourceFile, targetFile);

                case ".jpg":
                case ".jpeg":
                    if (lossy)
                        return string.Format(CultureInfo.CurrentCulture, "/c cjpeg -quality 80,60 -dct float -smooth 5 -outfile \"{1}\" \"{0}\"", sourceFile, targetFile);

                    return string.Format(CultureInfo.CurrentCulture, "/c jpegtran -copy none -optimize -progressive -outfile \"{1}\" \"{0}\"", sourceFile, targetFile);
                //return string.Format(CultureInfo.CurrentCulture, "/c guetzli_windows_x86-64 \"{0}\" \"{1}\"", sourceFile, targetFile);

                case ".gif":
                    return string.Format(CultureInfo.CurrentCulture, "/c gifsicle -O3 --batch --colors=256 \"{0}\" --output=\"{1}\"", sourceFile, targetFile);
            }

            return null;
        }

        public static bool IsFileSupported(string fileName)
        {
            string ext = Path.GetExtension(fileName);

            return _supported.Any(s => s.Equals(ext, StringComparison.OrdinalIgnoreCase));
        }
    }
}
