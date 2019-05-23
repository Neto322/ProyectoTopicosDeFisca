#if UNITY_5_6_OR_NEWER // TreeView implemented in 5.6
#define MY_CE_TREEVIEW_SUPPORT
#endif // UNITY_5_6_OR_NEWER

#if UNITY_5_3_OR_NEWER || UNITY_5_2 || UNITY_5_1 || UNITY_5_0
#define MY_CE_WINDOW_NAMESPACE_SUPPORT
#endif // UNITY_5_3_OR_NEWER || UNITY_5_2 || UNITY_5_1 || UNITY_5_0


using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using MerryYellow.CodeEnchanter.Common;
using System.Runtime.InteropServices;
#if MY_CE_WINDOW_NAMESPACE_SUPPORT
using SimpleWindow = MerryYellow.AutoInputManager.UnityEnchanterGUI.SimpleWindow;
#endif
using Common = MerryYellow.CodeEnchanter.Common;


namespace MerryYellow.AutoInputManager.UnityEnchanterEditor
{

    internal static class NativeMethods
    {
        internal static IntPtr coreCLRDelegate = IntPtr.Zero;
        internal static IntPtr coreCLRHostHandle = IntPtr.Zero;
        internal static uint coreCLRDomainId = 0;

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN // UNITY_STANDALONE_WIN is needed for Unity 3
        [DllImport("EnchanterWrapper", SetLastError = true, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int EXECUTE(
            string argument, string solutionPath, string enchanterPath,
            long bufferInPtr, long bufferOutPtr, long resolvePtr,
            ref int status
        );
#else // UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        [DllImport("EnchanterWrapper", SetLastError = true, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int EXECUTE(
            string argument, string solutionPath, string enchanterPath,
            long bufferInPtr, long bufferOutPtr, long resolvePtr,
            ref int status, ref IntPtr coreCLRDelegate,
            ref IntPtr coreCLRHostHandle, ref uint coreCLRDomainId
        );
#endif // UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN

    }


    public class EnchanterEditor
    {
        const int magicUINumber = -5;
        //Enchanter.Enchanter enchanter = new Enchanter.Enchanter();

        public enum States
        {
            Loading,
            LoadingPart2,
            Configuration,
            Enchanting,
            EnchantingPaused,
            Result,
            Error,
            FailNoFileSelected,
        }

        public States state = States.Loading;

        static String ProjectPath
        {
            get
            {
                var assetPath = Application.dataPath;
                return assetPath.Substring(0, assetPath.Length - 6);
            }
        }

        static string _enchanterPath; // need to cache because Application.dataPath can only be called from main thread
        static String EnchanterPath
        {
            get
            {
                if (_enchanterPath == null)
                {
                    var assetPath = Application.dataPath;
                    var directories = System.IO.Directory.GetDirectories(assetPath, "AutoInputManager", System.IO.SearchOption.AllDirectories);
                    if (directories.Length == 0)
                        throw new Exception("\"AutoInputManager\" directory couldn't be found. Please do not rename it. It could be placed in a subfolder.");
                    var path = directories[0];
                    path = path.Replace('\\', '/');
                    _enchanterPath = path + "/";
                }
                return _enchanterPath;
            }
        }

        static bool Is64Bit
        {
            get
            {
                return IntPtr.Size == 8;
            }
        }

        static String ExecutablePath
        {
            get
            {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
                return EnchanterPath + @"Plugins/EnchanterExecutableProxy.dll1";
#else
                return EnchanterPath + @"Plugins/EnchanterExecutableApp.dll1";
#endif
            }
        }

        static String TempExecutablePath
        {
            get
            {
                return System.IO.Path.GetTempPath() + @"EnchanterExecutableApp.dll";
            }
        }

        static String GetSolution()
        {
            var dirInfo = new System.IO.DirectoryInfo(ProjectPath);
            var files = dirInfo.GetFiles("*.sln", System.IO.SearchOption.TopDirectoryOnly);
            // sort by write time descending
            Array.Sort(files, delegate (System.IO.FileInfo fi1, System.IO.FileInfo fi2)
                { return fi2.LastWriteTime.CompareTo(fi1.LastWriteTime); });

            if (files.Length > 0)
                return files[0].FullName;
            else
            {
                files = dirInfo.GetFiles("*.sln", System.IO.SearchOption.AllDirectories);
                // sort by write time descending
                Array.Sort(files, delegate (System.IO.FileInfo fi1, System.IO.FileInfo fi2)
                    { return fi2.LastWriteTime.CompareTo(fi1.LastWriteTime); });

                if (files.Length > 0)
                    return files[0].FullName;
                else
                    return null;
            }
        }

        public MySolution solution;
        public List<MyEnchanter> enchanters;
#if MY_CE_TREEVIEW_SUPPORT
        // storing model to keep it between simple and advanced window switches
        internal UnityEnchanterGUI.TreeModel<UnityEnchanterGUI.MyTreeElement> treeModel;

        UnityEnchanterGUI.MultiColumnWindow advancedWindow;
#endif // MY_CE_TREEVIEW_SUPPORT

        SimpleWindow simpleWindow;

        public IterateData iterateData;

        bool yo()
        {
            //string dotNetCoreVersion, dotNetCorePath;
            //GetDotNetCoreDetails(out dotNetCoreVersion, out dotNetCorePath);

            //ELogger.Log(ELogger.Level.ExtraDetail, "dotNetCoreVersion: " + dotNetCoreVersion);
            //ELogger.Log(ELogger.Level.ExtraDetail, "dotNetCorePath: " + dotNetCorePath);

            //var process = StartExecutable(Common.CommonStrings.InitializeArgument);
            var process = null as System.Diagnostics.Process;
            var thread = StartThread(Common.CommonStrings.InitializeArgument);

            // Synchronously read the standard output of the spawned process. 
            //System.IO.StreamReader reader = process.StandardOutput;
            //string output = reader.ReadToEnd();
            string output = IOI.Read(process);
            IOI.Read(process); // CommonStrings.Terminate

            if (IOI.IsInterrupted)
            {
                IOI.RestoreInterrupt();
                output = CommonStrings.ErrorStartingDotNet;
            }

            if (process != null) {
                process.WaitForExit();
                process.Close();
            } else if (thread != null) {
                thread.Join();
            }

            if (ProcessError(output))
                return false;

            // Write the redirected output to this application's window.
            ELogger.Log(ELogger.Level.ExtraDetail, "Init output " + output.Length);
            ELogger.Log(ELogger.Level.ExtraDetail, output);

            //var enchanterList = JsonUtility.FromJson<Enchanter.MyEnchanterList>(output);
            var enchanterList = jsonDeserialize<Common.MyEnchanterList>(output);
            enchanters = enchanterList.Enchanters;

            //Debug.Log("Press any key to exit.");
            //Console.ReadLine();

            return true;
        }

        // these two methods needed because unity resets static field values when window closes
        static void TryReadCoreCLRValuesFromFile()
        {
            var coreCLRText = string.Empty;
            if (System.IO.File.Exists(EnchanterPath + "EnchanterCoreCLR.txt"))
                coreCLRText = System.IO.File.ReadAllText(EnchanterPath + "EnchanterCoreCLR.txt");
            if (coreCLRText != null && coreCLRText.Contains(" "))
            {
                var coreCLRTextP = coreCLRText.Split(' ');
                if (coreCLRTextP.Length == 3)
                {
                    long l1, l2;
                    uint i1;
                    if (long.TryParse(coreCLRTextP[0], out l1) && long.TryParse(coreCLRTextP[1], out l2) && uint.TryParse(coreCLRTextP[2], out i1))
                    {
                        NativeMethods.coreCLRDelegate = new IntPtr(l1);
                        NativeMethods.coreCLRHostHandle = new IntPtr(l2);
                        NativeMethods.coreCLRDomainId = i1;
                    }
                }
            }
        }
        static void WriteCoreCLRValuesToFile()
        {
            var l1 = NativeMethods.coreCLRDelegate.ToInt64();
            var l2 = NativeMethods.coreCLRHostHandle.ToInt64();
            var i1 = NativeMethods.coreCLRDomainId.ToString();

            System.IO.File.WriteAllText(EnchanterPath + "EnchanterCoreCLR.txt", l1 + " " + l2 + " " + i1);
        }

        static long ptr1, ptr2;
        static string myarg1, myarg2, myarg3;
        static void ThreadStarter()
        {
            //AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            var rd = new ResolveEventHandler(CurrentDomain_AssemblyResolve);
            var rdh = GCHandle.Alloc(rd);

            var funcPtr = Marshal.GetFunctionPointerForDelegate(rd);
            var funcPtrLong = funcPtr.ToInt64();

            var newArg = ptr1.ToString() + CommonStrings.ArgumentSeperator + ptr2.ToString() + CommonStrings.ArgumentSeperator + myarg1 + CommonStrings.ArgumentSeperator + myarg2 + CommonStrings.ArgumentSeperator + myarg3;

            /*
            newArg += CommonStrings.ArgumentSeperator + LowLevelLogging.IsEnabled.ToString();
            if (LowLevelLogging.Path != null)
                newArg += CommonStrings.ArgumentSeperator + LowLevelLogging.Path;
            */
            newArg += CommonStrings.ArgumentSeperator + EnchanterOptions.IsLoggingAll.ToString();

            var dllPath = ExecutablePath;

            var tempRun = false;
#if !UNITY_EDITOR_WIN && !UNITY_STANDALONE_WIN
            tempRun = true;
#endif
            if (tempRun)
            {
                CopyBinariesToTempFolder(EnchanterPath);
                dllPath = TempExecutablePath;
            }

            int status = 1100;

#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            // fix coreclr path
            var envPath = Environment.GetEnvironmentVariable("PATH");
            //ELogger.Log(ELogger.Level.ExtraDetail, "env: " + envPath);
            if (!envPath.Contains("/usr/local/share/dotnet"))
                Environment.SetEnvironmentVariable("PATH", envPath + ":/usr/local/share/dotnet");
            //ELogger.Log(ELogger.Level.ExtraDetail, "env: " + Environment.GetEnvironmentVariable("PATH"));

            TryReadCoreCLRValuesFromFile();
#endif

            ELogger.Log(ELogger.Level.ExtraDetail, "newArg: " + newArg);

            ELogger.Log(ELogger.Level.ExtraDetail, "dllPath: " + dllPath);
            ELogger.Log(ELogger.Level.ExtraDetail, "EnchanterPath: " + EnchanterPath);
            ELogger.Log(ELogger.Level.ExtraDetail, "CoreCLR: " + NativeMethods.coreCLRDelegate + " _ " + NativeMethods.coreCLRHostHandle + " _ " + NativeMethods.coreCLRDomainId);
            //NativeMethods.coreCLRDelegate = IntPtr.Zero;
            int hr = 0;
            try
            {
                hr = NativeMethods.EXECUTE(dllPath, "aa2", /*EnchanterPath*/newArg, ptr1, ptr2, funcPtrLong, ref status
#if !UNITY_EDITOR_WIN && !UNITY_STANDALONE_WIN
                , ref NativeMethods.coreCLRDelegate, ref NativeMethods.coreCLRHostHandle, ref NativeMethods.coreCLRDomainId
#endif
                );
            }
            catch(Exception e)
            {
                ELogger.Log(ELogger.Level.Error, "EXECUTE_ERROR: " + e.ToString());
            }
            ELogger.Log(ELogger.Level.ExtraDetail, "EXECUTE_RESULT: " + hr);

            if (hr < 0)
            {
                var exception = Marshal.GetExceptionForHR(hr);
                ELogger.Log(ELogger.Level.Error, exception.ToString());
                ELogger.Log(ELogger.Level.Error, "STATUS1: " + status);
                IOI.Interrupt();
            }
            else if (status != 1200 && status != 2245 && status != -2232)
            {
                ELogger.Log(ELogger.Level.Error, "Error in native wrapper");
                ELogger.Log(ELogger.Level.Error, "STATUS2: " + status);
                IOI.Interrupt();
            }

#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            WriteCoreCLRValuesToFile();
#endif

            rdh.Free();
        }

        //**--these two are not functional
        private static System.Reflection.Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            return null;
        }
        private static System.Reflection.Assembly CurrentDomain_AssemblyResolve222(object sender, ResolveEventArgs args)
        {
            //try
            {
                string RMSAssemblyFolder = @"D:\Develop\Enchanter\UnityProject\Assets\AutoInputManager\Plugins\x86_64\";

                System.Reflection.Assembly MyAssembly = null;
                string strTempAssmbPath = string.Empty;

                var objExecutingAssemblies = System.Reflection.Assembly.GetExecutingAssembly();
                var arrReferencedAssmbNames = objExecutingAssemblies.GetReferencedAssemblies();

                var myAssemblyName = Array.Find<System.Reflection.AssemblyName>(arrReferencedAssmbNames, a => a.Name == args.Name);

                if (myAssemblyName != null)
                {
                    MyAssembly = System.Reflection.Assembly.LoadFrom(myAssemblyName.CodeBase);
                }
                else
                {
                    strTempAssmbPath = System.IO.Path.Combine(RMSAssemblyFolder, args.Name.Substring(0, args.Name.IndexOf(",")) + ".dll");

                    if (!string.IsNullOrEmpty(strTempAssmbPath))
                    {
                        if (System.IO.File.Exists(strTempAssmbPath))
                        {
                            //**--logger.Information("Assembly to load: {0} - File was found in: {1}", args.Name, strTempAssmbPath);

                            // Loads the assembly from the specified path.                  
                            MyAssembly = System.Reflection.Assembly.LoadFrom(strTempAssmbPath);
                        }
                    }
                }

                // Returns the loaded assembly.
                return MyAssembly;
            }
            /* catch (Exception exc)
             {
                 //**--logger.Error(exc);
                 return null;
             }*/
        }

        private static System.Threading.Thread StartThread(string firstArgument)
        {
            ELogger.Log(ELogger.Level.ExtraDetail, "StartThread0");

            var solution = GetSolution();
            if (solution == null)
            {
                GenerateSolutionFile();
                solution = GetSolution();
            }

            ELogger.Log(ELogger.Level.ExtraDetail, "StartThread1");


            //**--free memory
            var bufferIn = Marshal.AllocHGlobal(IOI.PtrSize);
            var bufferOut = Marshal.AllocHGlobal(IOI.PtrSize);
            Marshal.WriteByte(bufferIn, 0, 0);
            Marshal.WriteByte(bufferOut, 0, 0);

            ELogger.Log(ELogger.Level.ExtraDetail, "StartThread2");

            IOI.Initialize(bufferOut, bufferIn);

            ptr1 = bufferIn.ToInt64();
            ptr2 = bufferOut.ToInt64();

            ELogger.Log(ELogger.Level.ExtraDetail, "StartThread4");


            System.Threading.Thread thread = null;
            //try
            {
                myarg1 = firstArgument;
                myarg2 = solution;
                myarg3 = EnchanterPath;

                thread = new System.Threading.Thread(ThreadStarter);
                thread.Start();

                ELogger.Log(ELogger.Level.ExtraDetail, "StartThread5");

                //thread.Join(); // comment out for so()
            }
            //catch(Exception e)
            {

            }

            ELogger.Log(ELogger.Level.ExtraDetail, "StartThread6");

            return thread;
        }

        private static System.Diagnostics.Process StartExecutable(string firstArgument)
        {
            var solution = GetSolution();
            if (solution == null)
            {
                GenerateSolutionFile();
                solution = GetSolution();
            }
            
            var process = new System.Diagnostics.Process();
            process.StartInfo.FileName = ExecutablePath;
            process.StartInfo.Arguments = firstArgument + " \"" + solution + "\" \"" + EnchanterPath + "\"";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.StandardOutputEncoding = System.Text.Encoding.UTF8;
            process.Start();


            SynchronizeOptions(process);
            return process;
        }

        public string ErrorDetail;
        bool ProcessError(string error, string document = null)
        {
            if (error == CommonStrings.ErrorCompileFail)
            {
                state = States.Error;
                ELogger.Log(ELogger.Level.Error, "Compile failed...");
                ErrorDetail = "Compile failed. Please fix errors in the code and try again.";
                return true;
            }
            else if (error == CommonStrings.ErrorArgument)
            {
                state = States.Error;
                ELogger.Log(ELogger.Level.Error, "Argument passing error...");
                ErrorDetail = "Unexpected argument error. Please contact support.";
                return true;
            }
            else if (error == CommonStrings.ErrorArgument2)
            {
                state = States.Error;
                ELogger.Log(ELogger.Level.Error, "Argument passing error 2...");
                ErrorDetail = "Unexpected argument error 2. Please contact support.";
                return true;
            }
            else if (error == CommonStrings.ErrorMSWorkspace)
            {
                state = States.Error;
                ELogger.Log(ELogger.Level.Error, "MS Workspace error...");
                ErrorDetail = "Cannot initialize enchanter. Please install latest Visual Studio.";
                return true;
            }

            else if (error == CommonStrings.ErrorGettingEnchanters)
            {
                state = States.Error;
                ELogger.Log(ELogger.Level.Error, "Cannot retrieve enchanters...");
                ErrorDetail = "Cannot initialize enchanter. Please install latest .NET runtime.";
                return true;
            }
            else if (error == CommonStrings.ErrorSerializingEnchanters)
            {
                state = States.Error;
                ELogger.Log(ELogger.Level.Error, "Cannot serialize enchanters...");
                ErrorDetail = "Cannot initialize enchanter. Please validate all dll files.";
                return true;
            }
            else if (error == CommonStrings.ErrorGettingSolutionDetails)
            {
                state = States.Error;
                ELogger.Log(ELogger.Level.Error, "Cannot retrieve solution...");
                ErrorDetail = "Cannot initialize enchanter. Please install latest Visual Studio.";
                return true;
            }
            else if (error == CommonStrings.ErrorSerializingSolutionDetails)
            {
                state = States.Error;
                ELogger.Log(ELogger.Level.Error, "Cannot serialize solution...");
                ErrorDetail = "Cannot initialize enchanter. Please contact support.";
                return true;
            }

            else if (error == CommonStrings.ErrorCreatingManager)
            {
                state = States.Error;
                ELogger.Log(ELogger.Level.Error, "Cannot create manager...");
                ErrorDetail = "Cannot initialize manager. Please contact support.";
                return true;
            }
            else if (error == CommonStrings.ErrorCreatingEnchanter)
            {
                state = States.Error;
                ELogger.Log(ELogger.Level.Error, "Cannot create enchanter...");
                ErrorDetail = "Cannot initialize enchanter. Please contact support.";
                return true;
            }
            else if (error == CommonStrings.ErrorIOIBufferOverflow)
            {
                state = States.Error;
                ELogger.Log(ELogger.Level.Error, "Cannot write output buffer...");
                ErrorDetail = "Cannot execute enchanter. Please contact support.";
                return true;
            }
            else if (error == CommonStrings.ErrorStartingDotNet)
            {
                state = States.Error;
                ELogger.Log(ELogger.Level.Error, "Cannot start dotnet...");
                ErrorDetail = "Cannot start dotNet. Please contact support";
                return true;
            }
            

            // Document specific errors
            else if (error == CommonStrings.ErrorReadingInput)
            {
                ELogger.Log(ELogger.Level.Warning, "Error reading script info. Skipping " + document);
                return true;
            }
            else if (error == CommonStrings.ErrorDeserializingDocument)
            {
                ELogger.Log(ELogger.Level.Warning, "Error deserializing script info. Skipping " + document);
                return true;
            }
            else if (error == CommonStrings.ErrorEnchantingDocument)
            {
                ELogger.Log(ELogger.Level.Warning, "Error enchanting script. Skipping " + document);
                return true;
            }
            else if (error == CommonStrings.ErrorWritingDocument)
            {
                ELogger.Log(ELogger.Level.Warning, "Error writing script info. Skipping " + document);
                return true;
            }


            return false;
        }

        string ho_ExecuteProcess()
        {
            //var process = StartExecutable(Common.CommonStrings.SolutionArgument);
            var process = null as System.Diagnostics.Process;
            var thread = StartThread(Common.CommonStrings.SolutionArgument);

            // Synchronously read the standard output of the spawned process. 
            //System.IO.StreamReader reader = process.StandardOutput;
            //string output = reader.ReadToEnd();
            string output = IOI.Read(process);
            IOI.Read(process); // CommonStrings.Terminate

            if (IOI.IsInterrupted)
            {
                IOI.RestoreInterrupt();
                output = CommonStrings.ErrorStartingDotNet;
            }

            if (process != null)
            {
                process.WaitForExit();
                process.Close();
            }
            else if (thread != null)
                thread.Join();

            return output;
        }

        static void GenerateSolutionFile()
        {
            ELogger.Log(ELogger.Level.Standard, "Generating solution file...");
            EditorApplication.ExecuteMenuItem("Assets/Open C# Project");
        }

        // if version >= 5.5
        static bool IsUnityVersion5Dot5OrHigher
        {
            get
            {
                var version = Application.unityVersion;
                if (version.StartsWith("1.")
                    || version.StartsWith("2.")
                    || version.StartsWith("3.")
                    || version.StartsWith("4.")
                    || version.StartsWith("5.0")
                    || version.StartsWith("5.1")
                    || version.StartsWith("5.2")
                    || version.StartsWith("5.3")
                    || version.StartsWith("5.4"))
                    return false;
                else
                    return true;
            }
        }

        // if version >= 3.0
        static bool IsUnityVersion3rHigher
        {
            get
            {
                var version = Application.unityVersion;
                if (version.StartsWith("1.")
                    || version.StartsWith("2."))
                    return false;
                else
                    return true;
            }
        }


        // if version >= 5.6
        static bool IsUnityVersion5Dot6OrHigher
        {
            get
            {
                var version = Application.unityVersion;
                if (version.StartsWith("1.")
                    || version.StartsWith("2.")
                    || version.StartsWith("3.")
                    || version.StartsWith("4.")
                    || version.StartsWith("5.0")
                    || version.StartsWith("5.1")
                    || version.StartsWith("5.2")
                    || version.StartsWith("5.3")
                    || version.StartsWith("5.4")
                    || version.StartsWith("5.5"))
                    return false;
                else
                    return true;
            }
        }

        bool ho()
        {
            var output = ho_ExecuteProcess();


            // this error can be recovered
            // force create sln file
            if (output == CommonStrings.ErrorGettingSolutionDetails
                || output == CommonStrings.ErrorMSWorkspace
                || output == CommonStrings.ErrorCompileFail)
            {
                ELogger.Log(ELogger.Level.Standard, "Trying to recover from previous error...");
                GenerateSolutionFile();
                output = ho_ExecuteProcess();
            }


            if (ProcessError(output))
                return false;

            // Write the redirected output to this application's window.
            ELogger.Log(ELogger.Level.ExtraDetail, "Sol input " + output.Length);
            ELogger.Log(ELogger.Level.ExtraDetail, output);


            //var solution = JsonUtility.FromJson<Enchanter.MySolution>(output);
            solution = jsonDeserialize<MySolution>(output);

            var foreachVersionCheck = !IsUnityVersion5Dot5OrHigher;
            // create enchanters here, did not created with the solution to decrease json string size
            foreach (var project in solution.Projects)
            {
                foreach (var document in project.Documents)
                {
                    foreach (var enchanter in this.enchanters)
                    {
                        var newEnchanter = new MyEnchanter();
                        newEnchanter.Name = enchanter.Name;
                        document.Enchanters.Add(newEnchanter);

                        // disable foreach enchanter for unity version5.5 or higher (memory garbage creation bug fixed in this version)
                        if (!foreachVersionCheck && newEnchanter.Name == "ForEach")
                            newEnchanter.Count = -1;
                    }
                }
            }


            ELogger.Log(ELogger.Level.Detail, "Sol path: " + solution.path);

            //Debug.Log("Press any key to exit.");
            //Console.ReadLine();

            return true;
        }

        static System.Threading.Thread so_StartThread()
        {
            var thread = StartThread(Common.CommonStrings.EnchantArgument);
            return thread;
        }

        static System.Diagnostics.Process so_StartProcess()
        {
            //var process = StartExecutable(Common.CommonStrings.EnchantArgument);
            System.Diagnostics.Process process = null;

            /*
            process.OutputDataReceived += new System.Diagnostics.DataReceivedEventHandler(
                (s, e) =>
                {
                    //Console.WriteLine(e.Data);
                    Debug.Log("eee " + e.Data);
                }
            );*/

            /*
            process.Start();

            SynchronizeOptions(process);*/

            return process;
        }

        public class IterateData
        {
            public System.Diagnostics.Process process;
            public System.Threading.Thread thread;
            public System.IO.StreamWriter input;
            public System.IO.StreamReader output;
            public int indexOfMyDocuments;
            public List<MyDocument> myDocuments;

            public int GetProcessedEnchantmentCount
            {
                //**--optimize later
                get
                {
                    int count = 0;
                    foreach (var myDocument in myDocuments)
                    {
                        foreach (var enchanter in myDocument.Enchanters)
                        {
                            count += enchanter.Count;
                        }
                    }
                    return count;
                }
            }
        }

        bool IterateEnchantAux(IterateData data)
        {
            //for (; indexOfMyDocuments < myDocuments.Count; indexOfMyDocuments++)
            //var myDocument = myDocuments[0];
            {
                //try
                {

                    var myDocument = data.myDocuments[data.indexOfMyDocuments];
                    var inputJsonString = jsonSerialize(myDocument);
                    //input.Write(inputJsonString);
                    //input.Flush();

                    ELogger.Log(ELogger.Level.ExtraDetail, "Ench input (2) " + inputJsonString.Length);
                    ELogger.Log(ELogger.Level.ExtraDetail, inputJsonString);
                    //data.input.WriteLine(inputJsonString);

                    var temp = IOI.Read(data.output, data.input); // CommonStrings.EnchantIterate
                    ELogger.Log(ELogger.Level.ExtraDetail, "Temp (1)" + temp);
                    if (ProcessError(temp, myDocument.Name)) return true;
                    IOI.Write(inputJsonString, data.output, data.input);

                    //var outputJsonString = data.output.ReadLine();
                    var outputJsonString = IOI.Read(data.output, data.input);
                    ELogger.Log(ELogger.Level.Detail, "Ench result of " + myDocument.Name + " : (" + outputJsonString.Length + ")" + outputJsonString);

                    if (ProcessError(outputJsonString, myDocument.Name)) return true;

                    var enchantedMyDocument = jsonDeserialize<MyDocument>(outputJsonString);
                    data.myDocuments[data.indexOfMyDocuments] = enchantedMyDocument;
                    
                }
                /*catch(System.IO.IOException ioException)
                {
                    var myDocument = myDocuments[indexOfMyDocuments];
                    Debug.Log("error in doc " + myDocument.Name);

                    //process.Kill();
                    process.WaitForExit();
                    process.Start();
                }*/
            }

            return false;
        }

        int iterateCounter = magicUINumber;
        public bool IterateEnchant()
        {
            if (state != States.Enchanting) return false;

            if (iterateCounter < 0) { iterateCounter++; return true; }

            var data = iterateData;

            //var process = data.process;
            //var input = data.input;
            //var output = data.output;
            //var indexOfMyDocuments = data.indexOfMyDocuments;
            //var myDocuments = data.myDocuments;

            /*var hasError =*/ IterateEnchantAux(data);
            /*
            if (hasError)
            {
                var output = IOI.Read(data.output, data.input);
                if (output != CommonStrings.Terminate)
                    output = IOI.Read(data.output, data.input);
                if (output != CommonStrings.Terminate)
                    output = IOI.Read(data.output, data.input);
                if (output != CommonStrings.Terminate)
                    output = IOI.Read(data.output, data.input);

                ELogger.Log(ELogger.Level.ExtraDetail, "Error last output " + output);
            }*/

            //input.WriteLine(CommonStrings.EndEnchantString);

            iterateCounter = magicUINumber;
            data.indexOfMyDocuments++;

            if (data.indexOfMyDocuments == data.myDocuments.Count)
            {
                //data.input.WriteLine(CommonStrings.EndEnchantString);
                IOI.Read(data.output, data.input); // CommonStrings.EnchantIterate
                IOI.Write(CommonStrings.EndEnchantString, data.output, data.input);
                IOI.Read(data.output, data.input); // CommonStrings.Terminate
                state = States.Result;

                //**--close process, join thread
            }

            return true;
        }

        public void OnResult()
        {
            var total = iterateData.GetProcessedEnchantmentCount;
            string plural = total > 1 ? "s" : "";
            ELogger.Log(ELogger.Level.Standard, "Enchanter completed.");
            ELogger.Log(ELogger.Level.Standard, total + " enchantment" + plural + " applied.");
        }

        public void IterateEnchantForced()
        {
            iterateCounter = 0;
            IterateEnchant();
        }

        //**--remove later
        static void so(List<MyDocument> myDocuments)
        {
            var process = so_StartProcess();
            var input = process.StandardInput;
            var output = process.StandardOutput;

            for (var indexOfMyDocuments = 0; indexOfMyDocuments < myDocuments.Count; indexOfMyDocuments++)
            //var myDocument = myDocuments[0];
            {
                //try
                {

                    var myDocument = myDocuments[indexOfMyDocuments];
                    var inputJsonString = jsonSerialize(myDocument);
                    //input.Write(inputJsonString);
                    //input.Flush();

                    ELogger.Log(ELogger.Level.ExtraDetail, "so inputJsonString " + inputJsonString.Length);
                    ELogger.Log(ELogger.Level.ExtraDetail, inputJsonString);
                    input.WriteLine(inputJsonString);

                    var outputJsonString = output.ReadLine();
                    ELogger.Log(ELogger.Level.ExtraDetail, "Ench result of " + myDocument.Name + " : (" + outputJsonString.Length + ")" + outputJsonString);

                    /*
                    if (CommonStrings.IsErrorString(outputJsonString))
                    {
                        ProcessError(outputJsonString);
                    }*/
                }
                /*catch(System.IO.IOException ioException)
                {
                    var myDocument = myDocuments[indexOfMyDocuments];
                    Debug.Log("error in doc " + myDocument.Name);

                    //process.Kill();
                    process.WaitForExit();
                    process.Start();
                }*/
            }

            input.WriteLine(CommonStrings.EndEnchantString);
        }

        static void InitializeMyLogger()
        {
            if (EnchanterOptions.IsLoggingToFile)
                System.IO.File.WriteAllText(EnchanterPath + EnchanterOptions.LogFileName, string.Empty); // clear file
            /*else
                //System.IO.File.r
                ;//**--remove file
                */
        }

        static void InitializeOptions()
        {
            try
            {
                var optionsText = System.IO.File.ReadAllText(EnchanterPath + "EnchanterOptions.txt");
                //var options = JsonUtility.FromJson<Common.EnchanterOptions>(optionsText);
                var options = jsonDeserialize<EnchanterOptions>(optionsText);
                Common.EnchanterOptions.Instance = options;

                InitializeMyLogger();

                ELogger.Log(ELogger.Level.ExtraDetail, "Options initialized (1)");
            }
            catch (Exception e)
            {
                InitializeMyLogger();

                ELogger.Log(ELogger.Level.Warning, "Options couldn't be initialized");
                ELogger.Log(ELogger.Level.Warning, e.ToString());
            }

        }

        static void SynchronizeOptions(System.Diagnostics.Process process)
        {
            /*
            ELogger.Log(ELogger.Level.ExtraDetail, "Options synchronizing");

            var jsonOutput = JsonUtility.ToJson(EnchanterOptions.Instance);
            IOI.Read(process); // CommonStrings.SynchronizeOptions
            IOI.Write(jsonOutput, process);*/
        }

        [MenuItem("Tools/Auto Input Manager")]
        private static void NewMenuOption()
        {
            if (!Application.isEditor)
            {
                Debug.LogError("Auto Input Manager should only be used in the editor.");
                return;
            }
            if (!IsUnityVersion3rHigher)
            {
                Debug.LogError("Unity version 3.0 or higher is supported.");
                return;
            }

            var ee = new EnchanterEditor();

            InitializeOptions();

            // open simple window
            AdvancedWindow_MySimpleEvent(ee);
        }

        int initCounter = magicUINumber;
        public bool Initialize()
        {
            if (initCounter < 0) { initCounter++; return true; }
            if (initCounter > 0) return false;
            initCounter++;

            if (state == States.Loading)
            {
                string dotNetFWVersion = "-";
                string vcppVersion = "-";
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
                bool isDotNetFWInstalled = true, isVCppVersionInstalled = true;
                if (EnchanterOptions.IsCheckingRedistributables)
                {
                    int dotNetFWVersionInt, vcppVersionInt;
                    isDotNetFWInstalled = IsDotNetFramework471orHigherInstalled(out dotNetFWVersionInt);
                    isVCppVersionInstalled = IsVisualStudioVCppRuntimeInstalled(out vcppVersionInt);
                    dotNetFWVersion = dotNetFWVersionInt.ToString();
                    vcppVersion = vcppVersionInt.ToString();
                }
#endif
                ELogger.Log(ELogger.Level.Document, "Versions:" +
                    " AutoInputManager:" + CommonStrings.AutoInputManagerVersion +
                    " Unity:" + Application.unityVersion +
#if UNITY_5_3_OR_NEWER
                    " dotNet:" + Application.version +
#endif
                    " dotNetFW:" + dotNetFWVersion +
                    " vCpp:" + vcppVersion +
                    " OS:" + Environment.OSVersion.ToString() +
                    " 64Bit:" + Is64Bit
                    );
                ELogger.Log(ELogger.Level.Standard, "Initializing Enchanter...");

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
                if (!isDotNetFWInstalled || !isVCppVersionInstalled)
                    return false;
#endif

                if (!this.yo())
                    return true;

                initCounter = magicUINumber;
                state = States.LoadingPart2;
            }
            else if (state == States.LoadingPart2)
            {
                ELogger.Log(ELogger.Level.Standard, "Getting project details...");
                if (!this.ho())
                    return true;

                this.state = States.Configuration;

                try
                {
                    var enchantmentTypeCount = this.solution.GetSelectedEnchanterCount(this.enchanters);
                    var documentCount = this.solution.GetSelectedDocumentCount();
                    ELogger.Log(ELogger.Level.Document, "Initialized with " + enchantmentTypeCount + " enchantments and " + documentCount + " documents");
                }
                catch(Exception e)
                {
                    ELogger.Log(ELogger.Level.Warning, "Couldn't log analytics of initialization");
                    ELogger.Log(ELogger.Level.Warning, e.ToString());
                }
            }

            /*
            // Get existing open window or if none, make a new one:
            EnchanterWindow window = (EnchanterWindow)EditorWindow.GetWindow(typeof(EnchanterWindow));
            window.Show();
            */

            //this.state = States.Configuration;
            //AdvancedWindow_MySimpleEvent(ew);

            return true;
        }

        private static void SimpleWindow_ToAdvancedWindowEvent(EnchanterEditor ee)
        {
#if MY_CE_TREEVIEW_SUPPORT
            if (ee.simpleWindow != null)
                ee.simpleWindow.Close();
            //ew.advancedWindow.Show();

            var advancedWindow = EditorWindow.GetWindow<UnityEnchanterGUI.MultiColumnWindow>();
            advancedWindow.titleContent = new GUIContent("Auto Input Mngr"/*title*/);
            advancedWindow.myEnchanterEditor = ee;
            //advancedWindow.mySolution = ee.solution;
            //advancedWindow.myEnchanters = ee.enchanters;
            advancedWindow.MyEnchantEvent += AdvancedWindow_MyEnchantEvent;
            advancedWindow.MySimpleEvent += AdvancedWindow_MySimpleEvent;
            /*window.Focus();
            window.Repaint();
            return window;*/
            //advancedWindow.Show();
            ee.advancedWindow = advancedWindow;
#else //MY_CE_TREEVIEW_SUPPORT
            Debug.Log("Advanced/Table View is supported on Unity versions 5.6 and higher");
#endif // MY_CE_TREEVIEW_SUPPORT
        }

        private static void AdvancedWindow_MyEnchantEvent(EnchanterEditor ee)
        {
            AdvancedWindow_MySimpleEvent(ee);//enforce to simple window, to be removed later
            SimpleWindow_SimpleEnchantEvent(ee);
        }

        private static void AdvancedWindow_MySimpleEvent(EnchanterEditor ee)
        {
#if MY_CE_TREEVIEW_SUPPORT
            if (ee.advancedWindow != null)
                ee.advancedWindow.Close();
#endif // MY_CE_TREEVIEW_SUPPORT
            //ew.simpleWindow.Show();

            // "Auto Input Manager" last letter doesnt fit the tab title space :(
            var simpleWindow = EditorWindow.GetWindow<SimpleWindow>(false, "Auto Input Mngr"/*title*/);
            //simpleWindow.titleContent = new GUIContent("Auto Input Mngr"/*title*/);
            simpleWindow.enchanterEditor = ee;
            ee.simpleWindow = simpleWindow;
            //simpleWindow.DocumentCount = ew.solution.GetDocumentCount();
            //simpleWindow.EnchantmentTypeCount = ew.enchanters.Count;
            simpleWindow.ToAdvancedWindowEvent += SimpleWindow_ToAdvancedWindowEvent;
            simpleWindow.SimpleEnchantEvent += SimpleWindow_SimpleEnchantEvent;
            simpleWindow.Show();
        }

        private static void SimpleWindow_SimpleEnchantEvent(EnchanterEditor ee)
        {
            ELogger.Log(ELogger.Level.Standard, "Starting Enchanter...");

            var docList = Export(ee.solution);

            if (docList.Count == 0) // no file selected
            {
                ee.state = States.FailNoFileSelected;
                return;
            }

            ee.iterateData = new IterateData();
            //ee.iterateData.process = so_StartProcess();
            //ee.iterateData.input = ee.iterateData.process.StandardInput;
            //ee.iterateData.output = ee.iterateData.process.StandardOutput;
            ee.iterateData.thread = so_StartThread();
            ee.iterateData.indexOfMyDocuments = 0;
            ee.iterateData.myDocuments = docList;

            ee.state = States.Enchanting;
            //so(docList);
        }

        static List<MyDocument> Export(MySolution solution)
        {
            List<MyDocument> myDocs = new List<MyDocument>();

            foreach (var project in solution.Projects)
            {
                foreach (var document in project.Documents)
                {
                    var doc = new MyDocument();
                    doc.Name = document.Name;
                    doc.Folders = document.Folders;
                    doc.ProjectName = document.ProjectName;

                    foreach (var enchanter in document.Enchanters)
                    {
                        if (enchanter.Count >= 0)
                        {
                            var ench = new MyEnchanter();
                            ench.Name = enchanter.Name;
                            doc.Enchanters.Add(ench);
                        }
                    }

                    if (doc.Enchanters.Count > 0)
                        myDocs.Add(doc);
                }
            }

            return myDocs;
        }

        public static void MyLog(ELogger.Level level, string message)
        {
            if (level == ELogger.Level.Standard)
                Debug.Log(message);
            if (level == ELogger.Level.Error)
                Debug.LogError(message);
            if (level == ELogger.Level.Warning)
                Debug.LogWarning(message);

            if (level == ELogger.Level.Error || level == ELogger.Level.Warning)
                message = level.ToString().ToUpper() + " " + message;

            try
            {
                if (EnchanterOptions.IsLoggingAll ||
                        (EnchanterOptions.IsLoggingToFile && 
                        ((int)ELogger.Level.Standard >= (int)level || level == ELogger.Level.Document))
                    )
                    System.IO.File.AppendAllText(EnchanterPath + EnchanterOptions.LogFileName, message + Environment.NewLine);
            }
            catch
            {
            }
        }

        static string jsonSerialize(object o)
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(o);
            //return JsonUtility.ToJson(o);
        }

        static T jsonDeserialize<T>(string s)
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(s);
            //return JsonUtility.FromJson<T>(s);
        }

        static string ExecuteBashCommand(string command)
        {
            // according to: https://stackoverflow.com/a/15262019/637142
            // thans to this we will pass everything as one command
            command = command.Replace("\"","\"\"");

            var proc = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = "-c \""+ command + "\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            proc.Start();
            proc.WaitForExit();

            return proc.StandardOutput.ReadToEnd();
        }

        static void GetDotNetCoreDetails(out string version, out string path,
            out string sdkPath)
        {
            var info = ExecuteBashCommand("dotnet --info");
            ELogger.Log(ELogger.Level.ExtraDetail, "GetDotNetCoreDetails: " + info);
            var lines = info.Split(new string[]{Environment.NewLine}, StringSplitOptions.None);

            version = string.Empty;
            path = string.Empty;
            sdkPath = string.Empty;

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("Microsoft.NETCore.App"))
                {
                    var blocks = trimmed.Split(' ');
                    version = blocks[1];
                    path = blocks[2];
                    if (path[0] == '[' && path.Length > 2) // clear []
                        path = path.Substring(1, path.Length - 2);

                    //break; // dont break loop, use last line (for the last version)
                }

                if (trimmed.StartsWith("Base Path:"))
                {
                    sdkPath = trimmed.Substring(10);
                    sdkPath = sdkPath.Trim();
                }
            }

            ELogger.Log(ELogger.Level.ExtraDetail, "GetDotNetCoreDetails.Version: " + version);
            ELogger.Log(ELogger.Level.ExtraDetail, "GetDotNetCoreDetails.Path: " + path);
            ELogger.Log(ELogger.Level.ExtraDetail, "GetDotNetCoreDetails.SDK: " + sdkPath);
        }

        bool IsDotNetFramework471orHigherInstalled(out int releaseKey)
        {
#if NET_STANDARD_2_0
            //**--TODO, fix later
            releaseKey = -2;
            return true;
#else

            releaseKey = -1;
            const string subkey = @"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\";

            try
            {
                using (Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(subkey))
                {
                    if (key != null)
                    {
                        System.Object o = key.GetValue("Release");
                        if (o != null)
                        {
                            releaseKey = (int)o;

                            if (releaseKey >= 461308)
                                //return "4.7.1";
                                return true;
                            else
                            {
                                state = States.Error;
                                ELogger.Log(ELogger.Level.Error, ".NET Framework error 1...");
                                ErrorDetail = ".NET Framework 4.7.1 or higher couldn't be found (1). Please install it";

                                return false;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)  //just for demonstration...it's always best to handle specific exceptions
            {
                //react appropriately

                // permission error??
                ELogger.Log(ELogger.Level.Warning, ".NET Framework error 3... " + ex.Message);

                return true;
            }

            state = States.Error;
            ELogger.Log(ELogger.Level.Error, ".NET Framework error 2...");
            ErrorDetail = ".NET Framework 4.7.1 or higher couldn't be found (2). Please install it";

            return false;
#endif
        }

        bool IsVisualStudioVCppRuntimeInstalled(out int version)
        {
#if NET_STANDARD_2_0
            //**--TODO, fix later
            version = -2;
            return true;
#else
            version = -1;
            const string subkey = @"SOFTWARE\Wow6432Node\Microsoft\VisualStudio\14.0\VC\Runtimes\x64\";
            

            try
            {
                using (Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(subkey))
                {
                    if (key != null)
                    {
                        System.Object o = key.GetValue("Bld");
                        if (o != null)
                        {
                            version = (int)o;

                            if (version >= 26020)
                                return true;
                            else
                            {
                                state = States.Error;
                                ELogger.Log(ELogger.Level.Error, "Visual C++ Redistributable error 1...");
                                ErrorDetail = "Microsoft Visual C++ Redistributable for Visual Studio 2017 or higher couldn't be found (1). Please install it";

                                return false;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)  //just for demonstration...it's always best to handle specific exceptions
            {
                //react appropriately

                // permission error??
                ELogger.Log(ELogger.Level.Warning, "Visual C++ Redistributable error 2... " + ex.Message);
            }


            const string subkey32 = @"SOFTWARE\Wow6432Node\Microsoft\VisualStudio\14.0\VC\Runtimes\x86\";

            try
            {
                using (Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(subkey32))
                {
                    if (key != null)
                    {
                        System.Object o = key.GetValue("Bld");
                        if (o != null)
                        {
                            version = (int)o;

                            if (version >= 26020)
                                return true;
                            else
                            {
                                state = States.Error;
                                ELogger.Log(ELogger.Level.Error, "Visual C++ Redistributable error 3...");
                                ErrorDetail = "Microsoft Visual C++ Redistributable for Visual Studio 2017 or higher couldn't be found (3). Please install it";

                                return false;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)  //just for demonstration...it's always best to handle specific exceptions
            {
                //react appropriately

                // permission error??
                ELogger.Log(ELogger.Level.Warning, "Visual C++ Redistributable error 4... " + ex.Message);

                return true;
            }



            state = States.Error;
            ELogger.Log(ELogger.Level.Error, "Visual C++ Redistributable error 5...");
            ErrorDetail = "Microsoft Visual C++ Redistributable for Visual Studio 2017 or higher couldn't be found (5). Please install it";

            return false;
#endif
        }

        int Foo123()
        {
            // to disable warning CS0414
            return simpleWindow.DocumentCount;
        }

        static void CopyBinariesToTempFolder(string enchanterPath)
        {
            var tempDirectory = System.IO.Path.GetTempPath();
            var pluginsPath = enchanterPath + @"/Plugins";
            var pluginsSubPath = enchanterPath + @"/Plugins/lib/netstandard2.0";

            ELogger.Log(ELogger.Level.Detail, "CopyBinariesToTempFolder.tempDirectory: " + tempDirectory);

            //string dotnetCoreVersion, dotnetCorePath, dotnetCoreSDKPath;
            //GetDotNetCoreDetails(out dotnetCoreVersion, out dotnetCorePath, out dotnetCoreSDKPath);
            //var systemDirectories = new string[] { dotnetCoreSDKPath };

            var systemDirectories = new string[] { };

            //**--linux stalls on second copy sometimes, probably threads not exiting properly
            //if (System.IO.File.Exists(System.IO.Path.Combine(tempDirectory, "EnchanterExecutableApp.dll")))
            //return;


            ELogger.Log(ELogger.Level.ExtraDetail, "CopyBinariesToTempFolder2");

            var dirInfo = new System.IO.DirectoryInfo(pluginsPath);
            CopyBinariesToTempFolderAux(dirInfo, tempDirectory, systemDirectories);
            dirInfo = new System.IO.DirectoryInfo(pluginsSubPath);
            CopyBinariesToTempFolderAux(dirInfo, tempDirectory, systemDirectories);

            ELogger.Log(ELogger.Level.ExtraDetail, "CopyBinariesToTempFolder3");

            var specialDirectory = @"runtimes/unix/lib/netstandard1.3";
            var specialFile = specialDirectory + @"/System.Security.Cryptography.ProtectedData.dll";
            System.IO.Directory.CreateDirectory(System.IO.Path.Combine(tempDirectory, specialDirectory));
            var sf1 = System.IO.Path.Combine(System.IO.Path.Combine(enchanterPath, "Plugins"), specialFile + "1");
            System.IO.File.Copy(sf1, System.IO.Path.Combine(tempDirectory, specialFile), true);

            ELogger.Log(ELogger.Level.ExtraDetail, "CopyBinariesToTempFolder4");
        }

        private static void CopyBinariesToTempFolderAux(System.IO.DirectoryInfo dirInfo, string tempDirectory, string[] systemDirectories)
        {
            var files = dirInfo.GetFiles("*.dll", System.IO.SearchOption.TopDirectoryOnly);
            foreach (var file in files)
            {
                System.IO.File.Copy(file.FullName, System.IO.Path.Combine(tempDirectory, file.Name), true);
            }

            var files1 = dirInfo.GetFiles("*.dll1", System.IO.SearchOption.TopDirectoryOnly);
            foreach (var file in files1)
            {
                var isCopied = false;
                var filename = file.Name.Substring(0, file.Name.Length - 1);//remove trailing "1"

                foreach (var systemDirectory in systemDirectories)
                {
                    var systemFile = System.IO.Path.Combine(systemDirectory, filename);
                    if (System.IO.File.Exists(systemFile))
                    {
                        ELogger.Log(ELogger.Level.ExtraDetail, "Copying dll from system directory " + systemFile);
                        System.IO.File.Copy(systemFile, System.IO.Path.Combine(tempDirectory, filename), true);
                        isCopied = true;
                        break;
                    }
                    else
                        ELogger.Log(ELogger.Level.ExtraDetail, systemFile + " not found");
                }

                if (!isCopied)
                {
                    ELogger.Log(ELogger.Level.ExtraDetail, "Copying dll from asset directory " + file.FullName);
                    System.IO.File.Copy(file.FullName, System.IO.Path.Combine(tempDirectory, filename), true);

                }
            }
        }
    }

}
