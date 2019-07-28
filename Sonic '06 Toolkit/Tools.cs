﻿using System;
using System.IO;
using System.Net;
using System.Text;
using System.Linq;
using HedgeLib.Sets;
using System.Diagnostics;
using System.Windows.Forms;

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

namespace Sonic_06_Toolkit.Tools
{
    class ADX
    {
        static ProcessStartInfo adxSession;

        public static void ConvertToWAV(int state, string args, string selectedADX)
        {
            if (state == 0)
            {
                adxSession = new ProcessStartInfo(Properties.Settings.Default.adx2wavFile, $"\"{args}\" \"{Path.GetDirectoryName(args)}\\{Path.GetFileNameWithoutExtension(args)}.wav\"")
                {
                    WorkingDirectory = Path.GetDirectoryName(args),
                    WindowStyle = ProcessWindowStyle.Hidden
                };
            }
            else if (state == 1)
            {
                adxSession = new ProcessStartInfo(Properties.Settings.Default.adx2wavFile, $"\"{Path.Combine(Global.currentPath, selectedADX)}\" \"{Path.GetDirectoryName(Path.Combine(Global.currentPath, selectedADX))}\\{Path.GetFileNameWithoutExtension(Path.Combine(Global.currentPath, selectedADX))}.wav\"")
                {
                    WorkingDirectory = Global.currentPath,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
            }

            Begin(state);
        }

        public static void ConvertToADX(int state, string selectedWAV)
        {
            if (state == 2)
            {
                adxSession = new ProcessStartInfo(Properties.Settings.Default.criconverterFile, $"\"{Path.Combine(Global.currentPath, selectedWAV)}\" \"{Path.GetDirectoryName(Path.Combine(Global.currentPath, selectedWAV))}\\{Path.GetFileNameWithoutExtension(Path.Combine(Global.currentPath, selectedWAV))}.adx\" -codec=adx -volume=" + ADX_Studio.vol + " -downmix=" + ADX_Studio.downmix + ADX_Studio.ignoreLoop + ADX_Studio.removeLoop)
                {
                    WorkingDirectory = Global.currentPath,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
            }

            Begin(state);
        }

        static void Begin(int state)
        {
            if (Debugger.unsafeState == true) { MessageBox.Show("CriWare tools are missing. Please restart Sonic '06 Toolkit and try again.", "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            else
            {
                if (File.Exists(Properties.Settings.Default.adx2wavFile) || File.Exists(Properties.Settings.Default.criconverterFile))
                {
                    var Convert = Process.Start(adxSession);
                    var convertDialog = new Status(state, "ADX");
                    var parentLeft = Main.FormLeft + ((Main.FormWidth - convertDialog.Width) / 2);
                    var parentTop = Main.FormTop + ((Main.FormHeight - convertDialog.Height) / 2);
                    if (state == 0) convertDialog.StartPosition = FormStartPosition.CenterScreen;
                    else convertDialog.Location = new System.Drawing.Point(parentLeft, parentTop);
                    convertDialog.Show();
                    Convert.WaitForExit();
                    Convert.Close();
                    convertDialog.Close();
                }
                else { MessageBox.Show("CriWare tools are missing. Please restart Sonic '06 Toolkit and try again.", "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            }
        }
    }

    class ARC
    {
        static ProcessStartInfo arcSession;
        public static string getLocation;
        public static string failsafeCheck; //Unpacked ARCs will have a unique directory to prevent overwriting.

        public static void Unpack(int state, string args, string filename)
        {
            if (state == 0)
            {
                if (!Properties.Settings.Default.unpackAndLaunch)
                {
                    //Sets up the BASIC application and executes the unpacking process.
                    var basicWrite = File.Create($"{Properties.Settings.Default.toolsPath}unpack.bat");
                    var basicSession = new UTF8Encoding(true).GetBytes($"\"{Properties.Settings.Default.unpackFile}\" \"{args}\"");
                    basicWrite.Write(basicSession, 0, basicSession.Length);
                    basicWrite.Close();

                    arcSession = new ProcessStartInfo($"{Properties.Settings.Default.toolsPath}unpack.bat")
                    {
                        WorkingDirectory = Properties.Settings.Default.toolsPath,
                        WindowStyle = ProcessWindowStyle.Hidden
                    };

                    Begin(state);
                }
                else { Extract(state, args, filename); }
            }
            else if (state == 1)
            {
                Extract(state, args, filename);
            }
        }

        static void Extract(int state, string args, string filename)
        {
            byte[] bytes;

            if (state == 1) bytes = File.ReadAllBytes(filename).Take(4).ToArray();
            else bytes = File.ReadAllBytes(args).Take(4).ToArray();

            var hexString = BitConverter.ToString(bytes); hexString = hexString.Replace("-", " ");

            if (hexString != "55 AA 38 2D") MessageBox.Show("Invalid ARC file detected.", "File Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            else
            {
                string unpackBuildSession;
                byte[] basicSession;
                byte[] metadataSession;
                failsafeCheck = Path.GetRandomFileName();

                //Builds the main string which locates the ARC's final unpack directory.
                if (state == 1) { unpackBuildSession = $"{Properties.Settings.Default.archivesPath}{Global.sessionID}\\{failsafeCheck}\\{Path.GetFileNameWithoutExtension(filename)}\\"; }
                else { unpackBuildSession = $"{Properties.Settings.Default.archivesPath}{Global.sessionID}\\{failsafeCheck}\\{Path.GetFileNameWithoutExtension(args)}\\"; }

                if (!Directory.Exists(unpackBuildSession))
                {
                    Directory.CreateDirectory(unpackBuildSession);
                    getLocation = unpackBuildSession;
                }

                //Establishes the failsafe directory and copies the ARC prepare for the unpacking process.
                string arcBuildSession = $"{Properties.Settings.Default.archivesPath}{Global.sessionID}\\{failsafeCheck}\\";

                if (!Directory.Exists(arcBuildSession)) Directory.CreateDirectory(arcBuildSession);

                if (state == 1)
                {
                    if (File.Exists(filename)) File.Copy(filename, $"{arcBuildSession}{Path.GetFileName(filename)}", true);
                }
                else
                {
                    if (File.Exists(args)) File.Copy(args, $"{arcBuildSession}{Path.GetFileName(args)}", true);
                }

                //Sets up the BASIC application and executes the unpacking process.
                var basicWrite = File.Create($"{Properties.Settings.Default.toolsPath}unpack.bat");

                if (state == 1) basicSession = new UTF8Encoding(true).GetBytes($"\"{Properties.Settings.Default.unpackFile}\" \"{arcBuildSession}{Path.GetFileName(filename)}\"");
                else basicSession = new UTF8Encoding(true).GetBytes($"\"{Properties.Settings.Default.unpackFile}\" \"{arcBuildSession}{Path.GetFileName(args)}\"");

                basicWrite.Write(basicSession, 0, basicSession.Length);
                basicWrite.Close();

                arcSession = new ProcessStartInfo($"{Properties.Settings.Default.toolsPath}unpack.bat")
                {
                    WorkingDirectory = Properties.Settings.Default.toolsPath,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                //Writes metadata to the unpacked directory to ensure the original path is remembered.
                var metadataWrite = File.Create($"{arcBuildSession}metadata.ini");

                if (state == 1) metadataSession = new UTF8Encoding(true).GetBytes(filename);
                else metadataSession = new UTF8Encoding(true).GetBytes(args);

                metadataWrite.Write(metadataSession, 0, metadataSession.Length);
                metadataWrite.Close();

                Begin(state);
            }
        }

        public static void Repack(string tabText, string repackBuildSession, string metadata)
        {
            //Sets up the BASIC application and executes the repacking process.
            var basicWrite = File.Create(Properties.Settings.Default.toolsPath + "repack.bat");
            if (tabText.Contains(".arc"))
            {
                var basicSession = new UTF8Encoding(true).GetBytes($"\"{Properties.Settings.Default.repackFile}\" \"{repackBuildSession}{Path.GetFileNameWithoutExtension(metadata)}\"");
                basicWrite.Write(basicSession, 0, basicSession.Length);
                basicWrite.Close();
            }
            else { MessageBox.Show("Please use the Repack ARC As option.", "Free Mode", MessageBoxButtons.OK, MessageBoxIcon.Information); }

            arcSession = new ProcessStartInfo($"{Properties.Settings.Default.toolsPath}repack.bat")
            {
                WorkingDirectory = Properties.Settings.Default.toolsPath,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            Begin(1);
        }

        public static void RepackAs(string tabText, string repackBuildSession, string metadata, string destination)
        {
            //Sets up the BASIC application and executes the repacking process.
            var basicWrite = File.Create(Properties.Settings.Default.toolsPath + "repack.bat");
            if (tabText.Contains(".arc"))
            {
                var basicSession = new UTF8Encoding(true).GetBytes($"\"{Properties.Settings.Default.repackFile}\" \"{repackBuildSession}{Path.GetFileNameWithoutExtension(metadata)}\"");
                basicWrite.Write(basicSession, 0, basicSession.Length);
                basicWrite.Close();
            }
            else
            {
                if (metadata.EndsWith(@"\"))
                {
                    var basicSession = new UTF8Encoding(true).GetBytes($"\"{Properties.Settings.Default.arctoolFile}\" -i \"{metadata.Remove(metadata.Length - 1)}\" -c \"{destination}\"");
                    basicWrite.Write(basicSession, 0, basicSession.Length);
                    basicWrite.Close();
                }
                else
                {
                    var basicSession = new UTF8Encoding(true).GetBytes($"\"{Properties.Settings.Default.arctoolFile}\" -i \"{metadata}\" -c \"{destination}\"");
                    basicWrite.Write(basicSession, 0, basicSession.Length);
                    basicWrite.Close();
                }
            }

            arcSession = new ProcessStartInfo($"{Properties.Settings.Default.toolsPath}repack.bat")
            {
                WorkingDirectory = Properties.Settings.Default.toolsPath,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            Begin(1);

            if (tabText.Contains(".arc"))
            {
                string archivePath = $"{repackBuildSession}{Path.GetFileName(metadata)}";
                if (File.Exists(archivePath)) File.Copy(archivePath, destination, true);
            }
        }

        public static void Merge(int state, string arc1, string arc2, string output)
        {
            var unpackDialog = new Status(state, "ARC");
            var parentLeft = Main.FormLeft + ((Main.FormWidth - unpackDialog.Width) / 2);
            var parentTop = Main.FormTop + ((Main.FormHeight - unpackDialog.Height) / 2);
            unpackDialog.Location = new System.Drawing.Point(parentLeft, parentTop);
            unpackDialog.Show();

            string tempPath = $"{Global.applicationData}\\Temp\\{Path.GetRandomFileName()}";
            var tempData = new DirectoryInfo(tempPath);
            Directory.CreateDirectory(tempPath);
            File.Copy(arc1, Path.Combine(tempPath, Path.GetFileName(arc1)));

            arcSession = new ProcessStartInfo(Properties.Settings.Default.arctoolFile, $"-d \"{Path.Combine(tempPath, Path.GetFileName(arc1))}\"")
            {
                WorkingDirectory = Properties.Settings.Default.toolsPath,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            var Unpack1 = Process.Start(arcSession);
            Unpack1.WaitForExit();
            Unpack1.Close();

            File.Delete(Path.Combine(tempPath, Path.GetFileName(arc1)));

            if (Path.GetFileNameWithoutExtension(arc1) != Path.GetFileNameWithoutExtension(arc2)) { Directory.Move(Path.Combine(tempPath, Path.GetFileNameWithoutExtension(arc1)), Path.Combine(tempPath, Path.GetFileNameWithoutExtension(arc2))); }

            File.Copy(arc2, Path.Combine(tempPath, Path.GetFileName(arc2)));

            arcSession = new ProcessStartInfo(Properties.Settings.Default.arctoolFile, $"-d \"{Path.Combine(tempPath, Path.GetFileName(arc2))}\"")
            {
                WorkingDirectory = Properties.Settings.Default.toolsPath,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            var Unpack2 = Process.Start(arcSession);
            Unpack2.WaitForExit();
            Unpack2.Close();

            File.Delete(Path.Combine(tempPath, Path.GetFileName(arc2)));

            arcSession = new ProcessStartInfo(Properties.Settings.Default.arctoolFile, $"-f -i \"{Path.Combine(tempPath, Path.GetFileNameWithoutExtension(arc2))}\" -c \"{output}\"")
            {
                WorkingDirectory = Properties.Settings.Default.toolsPath,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            var Repack1 = Process.Start(arcSession);
            Repack1.WaitForExit();
            Repack1.Close();

            try
            {
                if (Directory.Exists(tempPath))
                {
                    foreach (FileInfo file in tempData.GetFiles())
                    {
                        file.Delete();
                    }
                    foreach (DirectoryInfo directory in tempData.GetDirectories())
                    {
                        directory.Delete(true);
                    }
                }
            }
            catch { Notification.Dispose(); return; }

            unpackDialog.Close();
        }

        static void Begin(int state)
        {
            var ARC = Process.Start(arcSession);
            var unpackDialog = new Status(state, "ARC");
            var parentLeft = Main.FormLeft + ((Main.FormWidth - unpackDialog.Width) / 2);
            var parentTop = Main.FormTop + ((Main.FormHeight - unpackDialog.Height) / 2);
            if (state == 0) unpackDialog.StartPosition = FormStartPosition.CenterScreen;
            else unpackDialog.Location = new System.Drawing.Point(parentLeft, parentTop);
            unpackDialog.Show();
            ARC.WaitForExit();
            ARC.Close();
            unpackDialog.Close();
        }
    }

    class AT3
    {
        static ProcessStartInfo at3Session;

        public static void ConvertToWAV(int state, string args, string selectedAT3)
        {
            if (state == 0)
            {
                at3Session = new ProcessStartInfo(Properties.Settings.Default.at3File, $"-d \"{args}\" \"{Path.GetDirectoryName(args)}\\{Path.GetFileNameWithoutExtension(args)}.wav\"")
                {
                    WorkingDirectory = Path.GetDirectoryName(args),
                    WindowStyle = ProcessWindowStyle.Hidden
                };
            }
            else if (state == 1)
            {
                at3Session = new ProcessStartInfo(Properties.Settings.Default.at3File, $"-d \"{Path.Combine(Global.currentPath, selectedAT3)}\" \"{Path.GetDirectoryName(Path.Combine(Global.currentPath, selectedAT3))}\\{Path.GetFileNameWithoutExtension(Path.Combine(Global.currentPath, selectedAT3))}.wav\"")
                {
                    WorkingDirectory = Global.currentPath,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
            }

            Begin(state);
        }

        public static void ConvertToAT3(int state, string selectedWAV)
        {
            if (state == 2)
            {
                at3Session = new ProcessStartInfo(Properties.Settings.Default.at3File, $"-e {AT3_Studio.wholeLoop}{AT3_Studio.beginLoop}{AT3_Studio.startLoop}{AT3_Studio.endLoop}\"{Path.Combine(Global.currentPath, selectedWAV)}\" \"{Path.GetDirectoryName(Path.Combine(Global.currentPath, selectedWAV))}\\{Path.GetFileNameWithoutExtension(Path.Combine(Global.currentPath, selectedWAV))}.at3\"")
                {
                    WorkingDirectory = Global.currentPath,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
            }

            Begin(state);
        }

        static void Begin(int state)
        {
            if (Debugger.unsafeState == true) { MessageBox.Show("SONY tools are missing. Please restart Sonic '06 Toolkit and try again.", "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            else
            {
                if (File.Exists(Properties.Settings.Default.at3File))
                {
                    var Convert = Process.Start(at3Session);
                    var convertDialog = new Status(state, "AT3");
                    var parentLeft = Main.FormLeft + ((Main.FormWidth - convertDialog.Width) / 2);
                    var parentTop = Main.FormTop + ((Main.FormHeight - convertDialog.Height) / 2);
                    if (state == 0) convertDialog.StartPosition = FormStartPosition.CenterScreen;
                    else convertDialog.Location = new System.Drawing.Point(parentLeft, parentTop);
                    convertDialog.Show();
                    Convert.WaitForExit();
                    Convert.Close();
                    convertDialog.Close();
                }
                else { MessageBox.Show("SONY tools are missing. Please restart Sonic '06 Toolkit and try again.", "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            }
        }
    }

    class CSB
    {
        static ProcessStartInfo csbSession;

        public static void Packer(int state, string args, string selectedCSB)
        {
            if (state == 0)
            {
                csbSession = new ProcessStartInfo(Properties.Settings.Default.csbFile, $"\"{args}\"")
                {
                    WorkingDirectory = Path.GetDirectoryName(args),
                    WindowStyle = ProcessWindowStyle.Hidden
                };
            }
            else if (new[] { 1, 2 }.Contains(state))
            {
                csbSession = new ProcessStartInfo(Properties.Settings.Default.csbFile, $"\"{Path.Combine(Global.currentPath, selectedCSB)}\"")
                {
                    WorkingDirectory = Global.currentPath,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
            }
            else if (state == 3)
            {
                csbSession = new ProcessStartInfo(Properties.Settings.Default.csbFile, $"\"{Path.Combine(Global.currentPath, selectedCSB)}\"")
                {
                    WorkingDirectory = Global.currentPath,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
            }

            Begin(state);
        }

        static void Begin(int state)
        {
            if (Debugger.unsafeState == true) { MessageBox.Show("SonicAudioTools are missing. Please restart Sonic '06 Toolkit and try again.", "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            else
            {
                if (File.Exists(Properties.Settings.Default.csbFile))
                {
                    var Unpack = Process.Start(csbSession);
                    var unpackDialog = new Status(state, "CSB");
                    var parentLeft = Main.FormLeft + ((Main.FormWidth - unpackDialog.Width) / 2);
                    var parentTop = Main.FormTop + ((Main.FormHeight - unpackDialog.Height) / 2);
                    if (state == 0) unpackDialog.StartPosition = FormStartPosition.CenterScreen;
                    else unpackDialog.Location = new System.Drawing.Point(parentLeft, parentTop);
                    unpackDialog.Show();
                    Unpack.WaitForExit();
                    Unpack.Close();
                    unpackDialog.Close();
                }
                else { MessageBox.Show("SonicAudioTools are missing. Please restart Sonic '06 Toolkit and try again.", "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            }
        }
    }

    class DDS
    {
        static ProcessStartInfo ddsSession;

        public static void Convert(int state, string args, string selectedDDS)
        {
            if (state == 0)
            {
                //Sets up the BASIC application and executes the converting process.
                ddsSession = new ProcessStartInfo(Properties.Settings.Default.directXFile, $"\"{args}\" -ft PNG{DDS_Studio.useGPU} -singleproc{DDS_Studio.forceDirectX10} -f R8G8B8A8_UNORM")
                {
                    WorkingDirectory = Global.currentPath,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
            }
            else if (state == 1)
            {
                //Sets up the BASIC application and executes the converting process.
                ddsSession = new ProcessStartInfo(Properties.Settings.Default.directXFile, $"\"{Path.Combine(Global.currentPath, selectedDDS)}\" -ft PNG{DDS_Studio.useGPU} -singleproc{DDS_Studio.forceDirectX10} -f R8G8B8A8_UNORM")
                {
                    WorkingDirectory = Global.currentPath,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
            }

            Begin(state);
        }

        static void Begin(int state)
        {
            if (Debugger.unsafeState == true) { MessageBox.Show("DirectX files are missing. Please restart Sonic '06 Toolkit and try again.", "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            else
            {
                if (File.Exists(Properties.Settings.Default.directXFile))
                {
                    var Convert = Process.Start(ddsSession);
                    var convertDialog = new Status(state, "DDS");
                    var parentLeft = Main.FormLeft + ((Main.FormWidth - convertDialog.Width) / 2);
                    var parentTop = Main.FormTop + ((Main.FormHeight - convertDialog.Height) / 2);
                    if (state == 0) convertDialog.StartPosition = FormStartPosition.CenterScreen;
                    else convertDialog.Location = new System.Drawing.Point(parentLeft, parentTop);
                    convertDialog.Show();
                    Convert.WaitForExit();
                    Convert.Close();
                    convertDialog.Close();
                }
                else { MessageBox.Show("DirectX files are missing. Please restart Sonic '06 Toolkit and try again.", "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            }
        }
    }

    class LUB
    {
        static ProcessStartInfo lubSession;
        static string failsafeCheck;

        public static void WriteDecompiler(int state)
        {
            //Gets the failsafe directory.
            if (!Directory.Exists($"{Properties.Settings.Default.unlubPath}{Global.sessionID}")) Directory.CreateDirectory($"{Properties.Settings.Default.unlubPath}{Global.sessionID}");
            if (state == 1 || state == 2) failsafeCheck = Global.getStorage;
            else failsafeCheck = Path.GetRandomFileName();

            //Writes the decompiler to the failsafe directory to ensure any LUBs left over from other open archives aren't copied over to the selected archive.
            if (!Directory.Exists($"{Properties.Settings.Default.unlubPath}{Global.sessionID}\\{failsafeCheck}")) Directory.CreateDirectory($"{Properties.Settings.Default.unlubPath}{Global.sessionID}\\{failsafeCheck}");
            if (!Directory.Exists($"{Properties.Settings.Default.unlubPath}{Global.sessionID}\\{failsafeCheck}\\lubs")) Directory.CreateDirectory($"{Properties.Settings.Default.unlubPath}{Global.sessionID}\\{failsafeCheck}\\lubs");
            if (!File.Exists($"{Properties.Settings.Default.unlubPath}{Global.sessionID}\\{failsafeCheck}\\unlub.jar")) File.WriteAllBytes($"{Properties.Settings.Default.unlubPath}{Global.sessionID}\\{failsafeCheck}\\unlub.jar", Properties.Resources.unlub);
            if (!File.Exists($"{Properties.Settings.Default.unlubPath}{Global.sessionID}\\{failsafeCheck}\\unlub.bat"))
            {
                var decompilerWrite = File.Create($"{Properties.Settings.Default.unlubPath}{Global.sessionID}\\{failsafeCheck}\\unlub.bat");
                var decompilerText = new UTF8Encoding(true).GetBytes("cd \".\\lubs\"\nfor /r %%i in (*.lub) do java -jar ..\\unlub.jar \"%%~dpni.lub\" > \"%%~dpni.lua\"\nxcopy \".\\*.lua\" \"..\\luas\" /y /i\ndel \".\\*.lua\" /q\n@ECHO OFF\n:delete\ndel /q /f *.lub\n@ECHO OFF\n:rename\ncd \"..\\luas\"\nrename \"*.lua\" \"*.lub\"\nexit");
                decompilerWrite.Write(decompilerText, 0, decompilerText.Length);
                decompilerWrite.Close();
            }
        }

        public static void Decompile(int state, string args, string LUB)
        {
            WriteDecompiler(state);

            //Checks if the first file to be processed is blacklisted. If so, abort the operation to ensure the file doesn't get corrupted.
            if (state == 0)
            {
                if (Path.GetFileName(args) == "standard.lub")
                {
                    MessageBox.Show("File: standard.lub\n\nThis file cannot be decompiled; attempts to do so will render the file unusable.", "Blacklisted file detected!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
            else if (state == 1)
            {
                if (Path.GetFileName(LUB) == "standard.lub")
                {
                    MessageBox.Show("File: standard.lub\n\nThis file cannot be decompiled; attempts to do so will render the file unusable.", "Blacklisted file detected!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
            else if (state == 2)
            {
                if (File.Exists($"{Global.currentPath}standard.lub"))
                {
                    MessageBox.Show("File: standard.lub\n\nThis file cannot be decompiled; attempts to do so will render the file unusable.", "Blacklisted file detected!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            if (state == 0)
            {
                //Checks if any of the blacklisted files are present. If so, warn the user about modifying the files.
                if (Path.GetFileName(args) == "render_shadowmap.lub") MessageBox.Show("File: render_shadowmap.lub\n\nEditing this file may render this archive unusable. Please edit this at your own risk!", "Blacklisted file detected!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                if (Path.GetFileName(args) == "game.lub") MessageBox.Show("File: game.lub\n\nEditing this file may render it unusable. Please edit this at your own risk!", "Blacklisted file detected!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                if (Path.GetFileName(args) == "object.lub") MessageBox.Show("File: object.lub\n\nEditing this file may render it unusable. Please edit this at your own risk!", "Blacklisted file detected!", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                //Checks the header for each file to ensure that it can be safely decompiled.
                if (File.Exists(args))
                {
                    if (File.ReadAllLines(args)[0].Contains("LuaP"))
                    {
                        File.Copy(args, Path.Combine($"{Properties.Settings.Default.unlubPath}{Global.sessionID}\\{failsafeCheck}\\lubs\\", Path.GetFileName(args)), true);
                    }
                    else { return; }
                }
            }
            else if (state == 1)
            {
                //Checks if any of the blacklisted files are present. If so, warn the user about modifying the files.
                if (Path.GetFileName(LUB) == "render_shadowmap.lub") MessageBox.Show("File: render_shadowmap.lub\n\nEditing this file may render this archive unusable. Please edit this at your own risk!", "Blacklisted file detected!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                if (Path.GetFileName(LUB) == "game.lub") MessageBox.Show("File: game.lub\n\nEditing this file may render it unusable. Please edit this at your own risk!", "Blacklisted file detected!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                if (Path.GetFileName(LUB) == "object.lub") MessageBox.Show("File: object.lub\n\nEditing this file may render it unusable. Please edit this at your own risk!", "Blacklisted file detected!", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                File.Copy(Path.Combine(Global.currentPath, Path.GetFileName(LUB)), Path.Combine($"{Properties.Settings.Default.unlubPath}{Global.sessionID}\\{failsafeCheck}\\lubs\\", Path.GetFileName(LUB)), true);
            }
            else if (state == 2)
            {
                //Checks if any of the blacklisted files are present. If so, warn the user about modifying the files.
                if (File.Exists($"{Global.currentPath}render_shadowmap.lub")) MessageBox.Show("File: render_shadowmap.lub\n\nEditing this file may render this archive unusable. Please edit this at your own risk!", "Blacklisted file detected!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                if (File.Exists($"{Global.currentPath}game.lub")) MessageBox.Show("File: game.lub\n\nEditing this file may render it unusable. Please edit this at your own risk!", "Blacklisted file detected!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                if (File.Exists($"{Global.currentPath}object.lub")) MessageBox.Show("File: object.lub\n\nEditing this file may render it unusable. Please edit this at your own risk!", "Blacklisted file detected!", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                //Checks the header for each file to ensure that it can be safely decompiled.
                foreach (string listLUBs in Directory.GetFiles(Tools.Global.currentPath, "*.lub", SearchOption.TopDirectoryOnly))
                {
                    if (File.Exists(listLUBs))
                    {
                        if (File.ReadAllLines(listLUBs)[0].Contains("LuaP"))
                        {
                            File.Copy(listLUBs, Path.Combine($"{Properties.Settings.Default.unlubPath}{Global.sessionID}\\{failsafeCheck}\\lubs\\", Path.GetFileName(listLUBs)), true);
                        }
                    }
                }
            }

            lubSession = new ProcessStartInfo($"{Properties.Settings.Default.unlubPath}{Global.sessionID}\\{failsafeCheck}\\unlub.bat")
            {
                WorkingDirectory = $"{Properties.Settings.Default.unlubPath}{Global.sessionID}\\{failsafeCheck}",
                WindowStyle = ProcessWindowStyle.Hidden
            };

            Begin(state, args);
        }

        static void Begin(int state, string args)
        {
            if (Debugger.unsafeState == true) { MessageBox.Show("unlub files are missing. Please restart LUB Studio and try again.", "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            else
            {
                if (File.Exists($"{Properties.Settings.Default.unlubPath}{Global.sessionID}\\{failsafeCheck}\\unlub.jar"))
                {
                    var Decompile = Process.Start(lubSession);
                    var decompileDialog = new Status(state, "LUB");
                    var parentLeft = Main.FormLeft + ((Main.FormWidth - decompileDialog.Width) / 2);
                    var parentTop = Main.FormTop + ((Main.FormHeight - decompileDialog.Height) / 2);
                    if (state == 0) decompileDialog.StartPosition = FormStartPosition.CenterScreen;
                    else decompileDialog.Location = new System.Drawing.Point(parentLeft, parentTop);
                    decompileDialog.Show();
                    Decompile.WaitForExit();
                    Decompile.Close();

                    if (state == 0)
                    {
                        //Copies all LUBs to the final directory, then erases leftovers.
                        foreach (string LUB in Directory.GetFiles($"{Properties.Settings.Default.unlubPath}{Global.sessionID}\\{failsafeCheck}\\luas\\", "*.lub", SearchOption.TopDirectoryOnly))
                        {
                            if (File.Exists(LUB))
                            {
                                File.Copy(Path.Combine($"{Properties.Settings.Default.unlubPath}{Global.sessionID}\\{failsafeCheck}\\luas\\", Path.GetFileName(LUB)), args, true);
                                File.Delete(LUB);
                            }
                        }
                    }
                    else if (state == 1)
                    {
                        //Copies all LUBs to the final directory, then erases leftovers.
                        foreach (string LUB in Directory.GetFiles($"{Properties.Settings.Default.unlubPath}{Global.sessionID}\\{failsafeCheck}\\luas\\", "*.lub", SearchOption.TopDirectoryOnly))
                        {
                            if (File.Exists(LUB))
                            {
                                File.Copy(Path.Combine($"{Properties.Settings.Default.unlubPath}{Global.sessionID}\\{failsafeCheck}\\luas\\", Path.GetFileName(LUB)), Path.Combine(Global.currentPath, Path.GetFileName(LUB)), true);
                                File.Delete(LUB);
                            }
                        }
                    }
                    else if (state == 2)
                    {
                        //Copies all LUBs to the final directory, then erases leftovers.
                        foreach (string LUB in Directory.GetFiles($"{Properties.Settings.Default.unlubPath}{Global.sessionID}\\{failsafeCheck}\\luas\\", "*.lub", SearchOption.TopDirectoryOnly))
                        {
                            if (File.Exists(LUB))
                            {
                                File.Copy(Path.Combine($"{Properties.Settings.Default.unlubPath}{Global.sessionID}\\{failsafeCheck}\\luas\\", Path.GetFileName(LUB)), Path.Combine(Global.currentPath, Path.GetFileName(LUB)), true);
                                File.Delete(LUB);
                            }
                        }
                    }

                    decompileDialog.Close();
                }
                else { MessageBox.Show("unlub files are missing. Please restart LUB Studio and try again.", "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            }
        }
    }

    class MST
    {
        static ProcessStartInfo mstSession;

        public static void Export(int state, string args, string selectedMST)
        {
            if (state == 0)
            {
                //Sets up the BASIC application and executes the converting process.
                mstSession = new ProcessStartInfo(Properties.Settings.Default.mstFile, $"\"{args}\"")
                {
                    WorkingDirectory = Path.GetDirectoryName(args),
                    WindowStyle = ProcessWindowStyle.Hidden
                };
            }
            else if (state == 1)
            {
                //Sets up the BASIC application and executes the converting process.
                mstSession = new ProcessStartInfo(Properties.Settings.Default.mstFile, $"\"{Path.Combine(Global.currentPath, selectedMST)}\"")
                {
                    WorkingDirectory = Global.currentPath,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
            }

            Begin();
        }

        public static void Import(int state, string selectedXML)
        {
            if (state == 1)
            {
                //Sets up the BASIC application and executes the converting process.
                mstSession = new ProcessStartInfo(Properties.Settings.Default.mstFile, $"\"{Path.Combine(Global.currentPath, selectedXML)}\"")
                {
                    WorkingDirectory = Global.currentPath,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
            }

            Begin();
        }

        static void Begin()
        {
            if (Debugger.unsafeState == true) { MessageBox.Show("mst06 files are missing. Please restart Sonic '06 Toolkit and try again.", "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            else
            {
                if (File.Exists(Properties.Settings.Default.mstFile))
                {
                    var Decode = Process.Start(mstSession);
                    Decode.WaitForExit();
                    Decode.Close();
                }
                else { MessageBox.Show("mst06 files are missing. Please restart Sonic '06 Toolkit and try again.", "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            }
        }
    }

    class PNG
    {
        static ProcessStartInfo pngSession;

        public static void Convert(int state, string args, string selectedPNG)
        {
            if (state == 0)
            {
                //Sets up the BASIC application and executes the converting process.
                pngSession = new ProcessStartInfo(Properties.Settings.Default.directXFile, $"\"{args}\" -ft DDS{DDS_Studio.useGPU} -singleproc{DDS_Studio.forceDirectX10} -f R8G8B8A8_UNORM")
                {
                    WorkingDirectory = Global.currentPath,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
            }
            else if (state == 1)
            {
                //Sets up the BASIC application and executes the converting process.
                pngSession = new ProcessStartInfo(Properties.Settings.Default.directXFile, $"\"{Path.Combine(Global.currentPath, selectedPNG)}\" -ft DDS{DDS_Studio.useGPU} -singleproc{DDS_Studio.forceDirectX10} -f R8G8B8A8_UNORM")
                {
                    WorkingDirectory = Global.currentPath,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
            }

            Begin(state);
        }

        static void Begin(int state)
        {
            if (Debugger.unsafeState == true) { MessageBox.Show("DirectX files are missing. Please restart Sonic '06 Toolkit and try again.", "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            else
            {
                if (File.Exists(Properties.Settings.Default.directXFile))
                {
                    var Convert = Process.Start(pngSession);
                    var convertDialog = new Status(state, "DDS");
                    var parentLeft = Main.FormLeft + ((Main.FormWidth - convertDialog.Width) / 2);
                    var parentTop = Main.FormTop + ((Main.FormHeight - convertDialog.Height) / 2);
                    if (state == 0) convertDialog.StartPosition = FormStartPosition.CenterScreen;
                    else convertDialog.Location = new System.Drawing.Point(parentLeft, parentTop);
                    convertDialog.Show();
                    Convert.WaitForExit();
                    Convert.Close();
                    convertDialog.Close();

                    //Global.ddsState = null;
                }
                else { MessageBox.Show("DirectX files are missing. Please restart Sonic '06 Toolkit and try again.", "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            }
        }
    }

    class SET
    {
        public static void Export(int state, string args, string SET)
        {
            if (state == 0)
            {
                if (File.Exists(args))
                {
                    var readSET = new S06SetData();
                    readSET.Load(args);
                    readSET.ExportXML($"{Path.GetFileNameWithoutExtension(args)}.xml");
                }
            }
            else if (state == 1)
            {
                if (File.Exists($"{Global.currentPath}{Path.GetFileName(SET)}"))
                {
                    var readSET = new S06SetData();
                    readSET.Load($"{Global.currentPath}{Path.GetFileName(SET)}");
                    readSET.ExportXML($"{Global.currentPath}{Path.GetFileNameWithoutExtension(SET)}.xml");
                }
            }
        }

        public static void Import(string XML)
        {
            if (Properties.Settings.Default.backupSET == true) if (File.Exists($"{Global.currentPath}{Path.GetFileNameWithoutExtension(XML)}.set")) File.Copy($"{Global.currentPath}{Path.GetFileNameWithoutExtension(XML)}.set", $"{Global.currentPath}{Path.GetFileNameWithoutExtension(XML)}.set.bak", true);

            if (File.Exists($"{Global.currentPath}{Path.GetFileNameWithoutExtension(XML)}.set")) File.Delete($"{Global.currentPath}{Path.GetFileNameWithoutExtension(XML)}.set");

            var readXML = new S06SetData();
            readXML.ImportXML($"{Global.currentPath}{XML}");
            readXML.Save($"{Global.currentPath}{Path.GetFileNameWithoutExtension(XML)}.set");

            if (Properties.Settings.Default.deleteXML == true) if (File.Exists($"{Global.currentPath}{XML}")) File.Delete($"{Global.currentPath}{XML}");
        }
    }

    class XMA
    {
        static ProcessStartInfo xmaSession;

        public static void ConvertToWAV(int state, string args, string selectedXMA)
        {
            if (state == 0)
            {
                //Sets up the BASIC application and executes the converting process.
                xmaSession = new ProcessStartInfo(Properties.Settings.Default.towavFile, $"\"{args}\"")
                {
                    WorkingDirectory = Path.GetDirectoryName(args),
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                Begin(state, args);
            }
            else if (state == 1)
            {
                //Sets up the BASIC application and executes the converting process.
                xmaSession = new ProcessStartInfo(Properties.Settings.Default.towavFile, $"\"{Path.Combine(Global.currentPath, Path.GetFileName(selectedXMA))}\"")
                {
                    WorkingDirectory = Global.currentPath,
                    WindowStyle = ProcessWindowStyle.Hidden,
                };

                Begin(state, selectedXMA);
            }
        }

        public static void ConvertToXMA(int state, string selectedWAV)
        {
            if (state == 2)
            {
                //Sets up the BASIC application and executes the converting process.
                xmaSession = new ProcessStartInfo(Properties.Settings.Default.xmaencodeFile, $"\"{Path.Combine(Global.currentPath, selectedWAV)}\" {XMA_Studio.wholeLoop}")
                {
                    WorkingDirectory = Global.currentPath,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
            }

            Begin(state, selectedWAV);
        }

        public static void PatchXMA(int state, string selectedXMA)
        {
            Begin(state, selectedXMA);
        }

        static void Begin(int state, string selectedFile)
        {
            if (Debugger.unsafeState == true) { MessageBox.Show("XMA Encode 2008 files are missing. Please restart Sonic '06 Toolkit and try again.", "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            else
            {
                if (File.Exists(Properties.Settings.Default.xmaencodeFile))
                {
                    if (state == 2)
                    {
                        string originalXMA = $"{Path.Combine(Global.currentPath, Path.GetFileNameWithoutExtension(selectedFile))}.xma";
                        if (File.Exists($"{Path.Combine(Global.currentPath, Path.GetFileNameWithoutExtension(selectedFile))}.xma"))
                        {
                            File.Copy(originalXMA, $"{originalXMA}.bak", true);
                            File.Delete(originalXMA);
                        }
                    }
                    else if (state == 3)
                    {
                        byte[] bytes = File.ReadAllBytes(selectedFile).ToArray();
                        var hexString = BitConverter.ToString(bytes); hexString = hexString.Replace("-", " ");
                        if (hexString.Contains("58 4D 41 32 24 00 00 00 03 01 00 00 00 00 00 00 00 00 00 00 00 00 BB 80 00 FF 00 00 00 00 4C 00 00 00 48 B6 00 00 00 01 01 00 00 01 73 65 65 6B 04 00 00 00 00 00 4C 00"))
                        {
                            MessageBox.Show("This XMA has already been patched.", "XMA Patched", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            return;
                        }
                        else
                        {
                            try
                            {
                                ByteArray.ByteArrayToFile(selectedFile, ByteArray.StringToByteArray("584D4132240000000301000000000000000000000000BB8000FF000000004C00000048B600000001010000017365656B0400000000004C00"));
                                MessageBox.Show("The selected XMA has been patched.", "XMA Patched", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                return;
                            }
                            catch
                            {
                                MessageBox.Show("An error occurred when patching the XMA.", "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                Notification.Dispose();
                                return;
                            }
                        }
                    }
                    else if (state == 1)
                    {
                        byte[] bytes = File.ReadAllBytes($"{Path.Combine(Global.currentPath, Path.GetFileName(selectedFile))}").ToArray();
                        var hexString = BitConverter.ToString(bytes); hexString = hexString.Replace("-", " ");
                        if (hexString.Contains("58 4D 41 32 24 00 00 00 03 01 00 00 00 00 00 00 00 00 00 00 00 00 BB 80 00 FF 00 00 00 00 4C 00 00 00 48 B6 00 00 00 01 01 00 00 01 73 65 65 6B 04 00 00 00 00 00 4C 00"))
                        {
                            FileInfo fi = new FileInfo($"{Path.Combine(Global.currentPath, Path.GetFileName(selectedFile))}");
                            FileStream fs = fi.Open(FileMode.Open);

                            long bytesToDelete = 56;
                            fs.SetLength(Math.Max(0, fi.Length - bytesToDelete));

                            fs.Close();
                        }

                        state = 3;
                    }
                    else if (state == 0)
                    {
                        byte[] bytes = File.ReadAllBytes(selectedFile).ToArray();
                        var hexString = BitConverter.ToString(bytes); hexString = hexString.Replace("-", " ");
                        if (hexString.Contains("58 4D 41 32 24 00 00 00 03 01 00 00 00 00 00 00 00 00 00 00 00 00 BB 80 00 FF 00 00 00 00 4C 00 00 00 48 B6 00 00 00 01 01 00 00 01 73 65 65 6B 04 00 00 00 00 00 4C 00"))
                        {
                            FileInfo fi = new FileInfo(selectedFile);
                            FileStream fs = fi.Open(FileMode.Open);

                            long bytesToDelete = 56;
                            fs.SetLength(Math.Max(0, fi.Length - bytesToDelete));

                            fs.Close();
                        }

                        state = 4;
                    }
                    
                    var Convert = Process.Start(xmaSession);
                    var convertDialog = new Status(state, "XMA");
                    var parentLeft = Main.FormLeft + ((Main.FormWidth - convertDialog.Width) / 2);
                    var parentTop = Main.FormTop + ((Main.FormHeight - convertDialog.Height) / 2);
                    if (state == 0 || state == 4) convertDialog.StartPosition = FormStartPosition.CenterScreen;
                    else convertDialog.Location = new System.Drawing.Point(parentLeft, parentTop);
                    convertDialog.Show();
                    Convert.WaitForExit();
                    Convert.Close();

                    if (state == 2)
                    {
                        if (Properties.Settings.Default.patchXMA == true)
                        {
                            try
                            {
                                ByteArray.ByteArrayToFile($"{Path.Combine(Global.currentPath, Path.GetFileNameWithoutExtension(selectedFile))}.xma", ByteArray.StringToByteArray("584D4132240000000301000000000000000000000000BB8000FF000000004C00000048B600000001010000017365656B0400000000004C00"));
                                state = 2;
                            }
                            catch
                            {
                                MessageBox.Show("An error occurred when patching the XMA.", "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                Notification.Dispose();
                                state = 2;
                            }
                        }
                    }
                    else if (state == 3)
                    {
                        try
                        {
                            ByteArray.ByteArrayToFile($"{Path.Combine(Global.currentPath, Path.GetFileNameWithoutExtension(selectedFile))}.xma", ByteArray.StringToByteArray("584D4132240000000301000000000000000000000000BB8000FF000000004C00000048B600000001010000017365656B0400000000004C00"));
                            state = 1;
                        }
                        catch
                        {
                            MessageBox.Show("An error occurred when patching the XMA.", "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            Notification.Dispose();
                            state = 1;
                        }
                    }
                    else if (state == 4)
                    {
                        try
                        {
                            ByteArray.ByteArrayToFile(selectedFile, ByteArray.StringToByteArray("584D4132240000000301000000000000000000000000BB8000FF000000004C00000048B600000001010000017365656B0400000000004C00"));
                            state = 1;
                        }
                        catch
                        {
                            MessageBox.Show("An error occurred when patching the XMA.", "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            Notification.Dispose();
                            state = 1;
                        }
                    }

                    convertDialog.Close();
                }
                else { MessageBox.Show("XMA Encode 2008 files are missing. Please restart Sonic '06 Toolkit and try again.", "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            }
        }
    }

    class XNO
    {
        static ProcessStartInfo xnoSession;
        static string failsafeCheck;

        static void WriteConverter(int state)
        {
            #region Getting current ARC failsafe...
            //Gets the failsafe directory.
            if (!Directory.Exists($"{Properties.Settings.Default.unlubPath}{Global.sessionID}")) Directory.CreateDirectory($"{Properties.Settings.Default.unlubPath}{Global.sessionID}");
            if (state == 1) failsafeCheck = Global.getStorage;
            else failsafeCheck = Path.GetRandomFileName();
            #endregion

            #region Writing converter...
            //Writes the decompiler to the failsafe directory to ensure any XNOs left over from other open archives aren't copied over to the selected archive.
            if (!Directory.Exists($"{Properties.Settings.Default.xnoPath}{Global.sessionID}\\{failsafeCheck}")) Directory.CreateDirectory($"{Properties.Settings.Default.xnoPath}{Global.sessionID}\\{failsafeCheck}");
            if (!File.Exists($"{Properties.Settings.Default.xnoPath}{Global.sessionID}\\{failsafeCheck}\\xno2dae.exe")) File.WriteAllBytes($"{Properties.Settings.Default.xnoPath}{Global.sessionID}\\{failsafeCheck}\\xno2dae.exe", Properties.Resources.xno2dae);
            #endregion
        }

        public static void Convert(int state, string args, string selectedXNO)
        {
            WriteConverter(state);

            string checkedBuildSession = $"{Properties.Settings.Default.xnoPath}{Global.sessionID}\\{failsafeCheck}\\xno2dae.exe";
            var checkedWrite = File.Create($"{Properties.Settings.Default.xnoPath}{Global.sessionID}\\{failsafeCheck}\\xno2dae.bat");

            if (state == 0)
            {
                var checkedText = new UTF8Encoding(true).GetBytes($"\"{checkedBuildSession}\" \"{args}\"");
                checkedWrite.Write(checkedText, 0, checkedText.Length);
                checkedWrite.Close();
            }
            else if (state == 1)
            {
                var checkedText = new UTF8Encoding(true).GetBytes($"\"{checkedBuildSession}\" \"{selectedXNO}\"");
                checkedWrite.Write(checkedText, 0, checkedText.Length);
                checkedWrite.Close();
            }

            //Sets up the BASIC application and executes the conversion process.
            xnoSession = new ProcessStartInfo($"{Properties.Settings.Default.xnoPath}{Global.sessionID}\\{failsafeCheck}\\xno2dae.bat")
            {
                WorkingDirectory = Global.currentPath,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            Begin(state);
        }

        public static void Animate(int state, string selectedXNO, string selectedXNM)
        {
            WriteConverter(state);

            string checkedBuildSession = $"{Properties.Settings.Default.xnoPath}{Global.sessionID}\\{failsafeCheck}\\xno2dae.exe";
            var checkedWrite = File.Create($"{Properties.Settings.Default.xnoPath}{Global.sessionID}\\{failsafeCheck}\\xno2dae.bat");

            var checkedText = new UTF8Encoding(true).GetBytes($"\"{checkedBuildSession}\" \"{selectedXNO}\" \"{selectedXNM}\"");
            checkedWrite.Write(checkedText, 0, checkedText.Length);
            checkedWrite.Close();

            //Sets up the BASIC application and executes the conversion process.
            xnoSession = new ProcessStartInfo($"{Properties.Settings.Default.xnoPath}{Global.sessionID}\\{failsafeCheck}\\xno2dae.bat")
            {
                WorkingDirectory = Global.currentPath,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            Begin(state);
        }

        static void Begin(int state)
        {
            if (Debugger.unsafeState == true) { MessageBox.Show("xno2dae files are missing. Please restart Sonic '06 Toolkit and try again.", "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            else
            {
                if (File.Exists(Properties.Settings.Default.xnoFile) || Debugger.unsafeState == false)
                {
                    var Convert = Process.Start(xnoSession);
                    var convertDialog = new Status(state, "XNO");
                    var parentLeft = Main.FormLeft + ((Main.FormWidth - convertDialog.Width) / 2);
                    var parentTop = Main.FormTop + ((Main.FormHeight - convertDialog.Height) / 2);
                    if (state == 0) convertDialog.StartPosition = FormStartPosition.CenterScreen;
                    else convertDialog.Location = new System.Drawing.Point(parentLeft, parentTop);
                    convertDialog.Show();
                    Convert.WaitForExit();
                    Convert.Close();
                    convertDialog.Close();
                }
                else { MessageBox.Show("xno2dae files are missing. Please restart Sonic '06 Toolkit and try again.", "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            }
        }
    }

    class ByteArray
    {
        public static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

        public static bool ByteArrayToFile(string fileName, byte[] byteArray)
        {
            using (var fs = new FileStream(fileName, FileMode.Append, FileAccess.Write))
            {
                fs.Write(byteArray, 0, byteArray.Length);
                return true;
            }
        }
    }

    class Notification
    {
        public static void Dispose()
        {
            Status statusForm = Application.OpenForms["Status"] != null ? (Status)Application.OpenForms["Status"] : null;

            if (statusForm != null)
            {
                try
                {
                    statusForm = (Status)Application.OpenForms["Status"];
                    statusForm.Close();
                }
                catch { }
            }
        }
    }

    public class TimedWebClient : WebClient
    {
        public int Timeout { get; set; }

        public TimedWebClient()
        {
            Timeout = 100000;
        }

        protected override WebRequest GetWebRequest(Uri address)
        {
            var objWebRequest = base.GetWebRequest(address);
            objWebRequest.Timeout = Timeout;
            return objWebRequest;
        }
    }

    public class Global
    {
        public static string versionNumber = "2.0";
        public static string versionNumberLong = "Version " + versionNumber;
        public static string serverStatus;
        public static string currentPath;
        public static string updateState;
        public static string getStorage;

        public static string applicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        public static int sessionID;
        public static int getIndex;

        public static bool javaCheck = true;
        public static bool gameChanged = false;
    }
}