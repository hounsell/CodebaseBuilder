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

            FileInfo[] extractableFiles = new DirectoryInfo(i386Path).GetFiles("*.ex_");
            ProcessFiles(extractableFiles);

            extractableFiles = new DirectoryInfo(i386Path).GetFiles("*.dl_");
            ProcessFiles(extractableFiles);

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

        static void ProcessFiles(FileInfo[] extractableFiles)
        {
            foreach (FileInfo f in extractableFiles)
            {
                string TempPath = Path.Combine(WorkingPath, f.Name.Substring(0, f.Name.Length - f.Extension.Length) + ".bin");
                Process p = new Process();
                p.StartInfo.FileName = @"C:\Windows\System32\expand.exe";
                p.StartInfo.Arguments = string.Format("\"{0}\" \"{1}\"", f.FullName, TempPath);

                p.Start();

                while (!p.HasExited)
                {
                    Thread.Sleep(10);
                    continue;
                }

                FileInfo tf = new FileInfo(TempPath);
                FileStream fStr = tf.Open(FileMode.Open);
                TextReader tr = new StreamReader(fStr) as TextReader;
                string Content = tr.ReadToEnd();

                Regex rx = new Regex("d:\\\\[\\\\A-Za-z0-9\\.]+");
                MatchCollection mc = rx.Matches(Content);

                foreach (Match m in mc)
                {
                    Paths.Add(m.Value);
                }

                tr.Close();
                fStr.Close();
                tf.Delete();

                Console.WriteLine("Indexed {0}", f.Name);
            }
        }
    }
}
