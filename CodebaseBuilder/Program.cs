using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace CodebaseBuilder
{
    static class Program
    {
        static string i386Path;
        static string WorkingPath;

        static List<string> Paths;

        static void Main(string[] args)
        {
            Paths = new List<string>();
            i386Path = args[0];
            WorkingPath = Environment.CurrentDirectory;

            if (!Directory.Exists(i386Path))
            {
                Console.WriteLine("Incorrect Path.");
                return;
            }

            DirectoryInfo i386Dir = new DirectoryInfo(i386Path);

            FileInfo[] processFiles = i386Dir.GetFiles("*.ex_");
            ExtractFiles(processFiles);
            processFiles = i386Dir.GetFiles("*.dl_");
            ExtractFiles(processFiles);
            processFiles = i386Dir.GetFiles("*.sy_");
            ExtractFiles(processFiles);
            processFiles = i386Dir.GetFiles("*.oc_");
            ExtractFiles(processFiles);


            processFiles = i386Dir.GetFiles("*.bin");
            ProcessFiles(processFiles);
            processFiles = i386Dir.GetFiles("*.dll");
            ProcessFiles(processFiles);
            processFiles = i386Dir.GetFiles("*.exe");
            ProcessFiles(processFiles);

            List<string> SortedUniquePaths = Paths.Distinct().OrderBy(s => s).ToList();

            TextWriter tw = new StreamWriter("path.txt", false, Encoding.UTF8);

            foreach (string path in SortedUniquePaths)
            {
                tw.WriteLine(path);
            }

            tw.Flush();
            tw.Close();

            Console.WriteLine("Path File Written");

            Console.ReadKey();
        }

        static void ExtractFiles(FileInfo[] extractableFiles)
        {
            foreach (FileInfo f in extractableFiles)
            {
                string TempPath = Path.Combine(WorkingPath, f.Name.Substring(0, f.Name.Length - f.Extension.Length) + ".bin");
                Process p = new Process();
                p.StartInfo.FileName = @"C:\Windows\System32\expand.exe";
                p.StartInfo.Arguments = string.Format("\"{0}\" \"{1}\"", f.FullName, TempPath);
                p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

                p.Start();

                while (!p.HasExited)
                {
                    Thread.Sleep(10);
                    continue;
                }

                ProcessPath(TempPath, true);

                Console.WriteLine("Indexed {0}", f.Name);
            }
        }

        static void ProcessFiles(FileInfo[] processFiles)
        {
            foreach (FileInfo f in processFiles)
            {
                ProcessPath(f.FullName, false);
            }
        }

        static void ProcessPath(string TempPath, bool DeleteAfterUse)
        {
            FileInfo tf = new FileInfo(TempPath);

            if (!tf.Exists)
            {
                return;
            }

            FileStream fStr = tf.Open(FileMode.Open, FileAccess.Read);
            TextReader tr = new StreamReader(fStr) as TextReader;
            string Content = tr.ReadToEnd();

            Regex rx = new Regex("[Dd]:\\\\[\\\\A-Za-z0-9\\.]+");
            MatchCollection mc = rx.Matches(Content);

            foreach (Match m in mc)
            {
                string path = m.Value.ToLower();
                string postPath = "";
                postPath = Regex.Replace(path, "[A-Za-z0-9\\.]+\\\\\\.\\.\\\\", "\\");
                postPath = postPath.Replace("\\\\", "\\");
                while (path != postPath)
                {
                    path = postPath;
                    postPath = Regex.Replace(path, "[A-Za-z0-9\\.]+\\\\\\.\\.\\\\", "\\");
                    postPath = postPath.Replace("\\\\", "\\");
                }
                Paths.Add(postPath);
            }

            tr.Close();
            fStr.Close();
            if (DeleteAfterUse)
            {
                tf.Delete();
            }
        }
    }
}
