﻿using System;
using System.IO;
using System.Text;
using System.Linq;
using Ookii.Dialogs;
using System.Diagnostics;
using System.Windows.Forms;
using System.Threading.Tasks;

// Sonic '06 Toolkit is licensed under the MIT License:
/*
 * MIT License

 * Copyright (c) 2019 Gabriel (HyperPolygon64)

 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:

 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.

 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

namespace Toolkit.Tools
{
    class Browsers
    {
        public static string CommonBrowser(bool folder, string title, string filter) {
            if (!folder) {
                OpenFileDialog fileBrowser = new OpenFileDialog {
                    Title = title,
                    Filter = filter,
                    RestoreDirectory = true,
                };
                if (fileBrowser.ShowDialog() == DialogResult.OK) return fileBrowser.FileName;
                else return string.Empty;
            } else {
                VistaFolderBrowserDialog folderBrowser = new VistaFolderBrowserDialog {
                    Description = title,
                    UseDescriptionForTitle = true,
                };
                if (folderBrowser.ShowDialog() == DialogResult.OK) return folderBrowser.SelectedPath;
                else return string.Empty;
            }
        }

        public static string SaveFile(string title, string filter)  {
            SaveFileDialog saveFiles = new SaveFileDialog {
                Title = title,
                Filter = filter,
                RestoreDirectory = true,
            };
            if (saveFiles.ShowDialog() == DialogResult.OK) return saveFiles.FileName;
            else return string.Empty;
        }
    }

    class Verification
    {
        public static bool VerifyMagicNumberCommon(string path) {
            string hexString = BitConverter.ToString(File.ReadAllBytes(path).Take(4).ToArray()).Replace("-", " ");

            if (Path.GetExtension(path).ToLower() == ".arc") {
                if (hexString == "55 AA 38 2D") return true;
                else return false;
            }
            else if (Path.GetExtension(path).ToLower() == ".dds") {
                if (hexString == "44 44 53 20") return true;
                else return false;
            }
            else if (Path.GetExtension(path).ToLower() == ".xno" || Path.GetExtension(path).ToLower() == ".xnm") {
                if (hexString == "4E 58 49 46") return true;
                else return false;
            }
            else if (Path.GetExtension(path).ToLower() == ".csb") {
                if (hexString == "40 55 54 46") return true;
                else return false;
            }
            else if (Path.GetExtension(path).ToLower() == ".adx") {
                if (hexString == "80 00 00 24") return true;
                else return false;
            }
            else if (Path.GetExtension(path).ToLower() == ".xma" || Path.GetExtension(path).ToLower() == ".at3") {
                if (hexString == "52 49 46 46") return true;
                else return false;
            }
            else if (Path.GetExtension(path).ToLower() == ".xex") {
                if (hexString == "58 45 58 32") return true;
                else return false;
            }
            return false;
        }

        public static bool VerifyMagicNumberBINA(string path) {
            string hexString = BitConverter.ToString(File.ReadAllBytes(path).Take(50).ToArray()).Replace("-", " ");

            if (Path.GetExtension(path).ToLower() == ".bin" || Path.GetExtension(path).ToLower() == ".set") {
                if (hexString.Contains("31 42 42 49 4E 41")) return true;
                else return false;
            }
            return false;
        }
    }

    public static class ProcessAsyncHelper
    {
        public static async Task<ProcessResult> ExecuteShellCommand(string command, string arguments, string working, int timeout) {
            var result = new ProcessResult();

            using (var process = new Process()) {
                // If you run bash-script on Linux it is possible that ExitCode can be 255.
                // To fix it you can try to add '#!/bin/bash' header to the script.

                process.StartInfo.FileName = command;
                process.StartInfo.Arguments = arguments;
                process.StartInfo.WorkingDirectory = working;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardInput = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;

                var outputBuilder = new StringBuilder();
                var outputCloseEvent = new TaskCompletionSource<bool>();

                process.OutputDataReceived += (s, e) => {
                    // The output stream has been closed i.e. the process has terminated
                    if (e.Data == null) {
                        outputCloseEvent.SetResult(true);
                    } else {
                        outputBuilder.AppendLine(e.Data);
                    }
                };

                var errorBuilder = new StringBuilder();
                var errorCloseEvent = new TaskCompletionSource<bool>();

                process.ErrorDataReceived += (s, e) => {
                    // The error stream has been closed i.e. the process has terminated
                    if (e.Data == null) {
                        errorCloseEvent.SetResult(true);
                    } else {
                        errorBuilder.AppendLine(e.Data);
                    }
                };

                bool isStarted;

                try { isStarted = process.Start(); }
                catch (Exception error) {
                    // Usually it occurs when an executable file is not found or is not executable

                    result.Completed = true;
                    result.ExitCode = -1;
                    result.Output = error.Message;

                    isStarted = false;
                }

                if (isStarted) {
                    // Reads the output stream first and then waits because deadlocks are possible
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    // Creates task to wait for process exit using timeout
                    var waitForExit = WaitForExitAsync(process, timeout);

                    // Create task to wait for process exit and closing all output streams
                    var processTask = Task.WhenAll(waitForExit, outputCloseEvent.Task, errorCloseEvent.Task);

                    // Waits process completion and then checks it was not completed by timeout
                    if (await Task.WhenAny(Task.Delay(timeout), processTask) == processTask && waitForExit.Result) {
                        result.Completed = true;
                        result.ExitCode = process.ExitCode;

                        // Adds process output if it was completed with error
                        if (process.ExitCode != 0)
                            result.Output = $"{outputBuilder}{errorBuilder}";
                    } else {
                        try {
                            // Kill hung process
                            process.Kill();
                        }
                        catch { }
                    }
                }
            }

            return result;
        }

        private static Task<bool> WaitForExitAsync(Process process, int timeout) { return Task.Run(() => process.WaitForExit(timeout)); }

        public struct ProcessResult {
            public bool Completed;
            public int? ExitCode;
            public string Output;
        }
    }
}