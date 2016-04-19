using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace CodebaseBuilder {
	static class Program {
		static string SearchPath;
		static string WorkingPath;
		static string Scope = "[D-Fd-f]";

		static List<string> Paths;

		static void Main(string[] args) {
			Paths = new List<string>();
			SearchPath = args[0];
			WorkingPath = Environment.CurrentDirectory;

			if (!Directory.Exists(SearchPath)) {
				Console.WriteLine("Incorrect Path.");
				return;
			}

			ProcessDirectoriesRecursively(new DirectoryInfo(SearchPath));

			List<string> SortedUniquePaths = Paths.Distinct().OrderBy(s => s).ToList();

			TextWriter tw = new StreamWriter("path.txt", false, Encoding.UTF8);

			foreach (string path in SortedUniquePaths) {
				tw.WriteLine(path);
			}

			tw.Flush();
			tw.Close();

			Console.WriteLine("Path File Written");
			Console.ReadKey();
		}

		static void ProcessDirectoriesRecursively(DirectoryInfo directory) {
			Console.WriteLine("Traversing " + directory.Name);

			// Gather and process packaged files 
			FileInfo[] processFiles = directory.GetFiles("*.ex_");
			ExtractFiles(processFiles);
			processFiles = directory.GetFiles("*.dl_");
			ExtractFiles(processFiles);
			processFiles = directory.GetFiles("*.sy_");
			ExtractFiles(processFiles);
			processFiles = directory.GetFiles("*.oc_");
			ExtractFiles(processFiles);

			// Gather and process unpackaged files
			processFiles = directory.GetFiles("*.bin");
			ProcessFiles(processFiles);
			processFiles = directory.GetFiles("*.dll");
			ProcessFiles(processFiles);
			processFiles = directory.GetFiles("*.exe");
			ProcessFiles(processFiles);
			processFiles = directory.GetFiles("*.scr");
			ProcessFiles(processFiles);

			foreach (DirectoryInfo dirInfo in directory.GetDirectories()) {
				ProcessDirectoriesRecursively(dirInfo);
			}
		}

		static void ExtractFiles(FileInfo[] extractableFiles) {
			foreach (FileInfo f in extractableFiles) {
				string TempPath = Path.Combine(WorkingPath, f.Name.Substring(0, f.Name.Length - f.Extension.Length) + ".bin");
				Process p = new Process();
				p.StartInfo.FileName = @"C:\Windows\System32\expand.exe";
				p.StartInfo.Arguments = string.Format("\"{0}\" \"{1}\"", f.FullName, TempPath);
				p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

				p.Start();

				while (!p.HasExited) {
					Thread.Sleep(10);
					continue;
				}

				ProcessPath(TempPath, true);

				Console.WriteLine("Indexed {0}", f.Name);
			}
		}

		static void ProcessFiles(FileInfo[] processFiles) {
			foreach (FileInfo f in processFiles) {
				ProcessPath(f.FullName, false);
			}
		}

		static void ProcessPath(string TempPath, bool DeleteAfterUse) {
			FileInfo tf = new FileInfo(TempPath);

			if (!tf.Exists) {
				return;
			}

			FileStream fStr = tf.Open(FileMode.Open, FileAccess.Read);
			TextReader tr = new StreamReader(fStr) as TextReader;
			string Content = tr.ReadToEnd();

			// Match any path in content
			Regex rx = new Regex(Scope + @":\\[A-Za-z0-9.\\]+");
			MatchCollection mc = rx.Matches(Content);

			// Remove trailing "\..\" slashes
			foreach (Match m in mc) {
				string path = m.Value.ToLower();
				string postPath = "";
				postPath = Regex.Replace(path, "[A-Za-z0-9\\.]+\\\\\\.\\.\\\\", "\\");
				postPath = postPath.Replace("\\\\", "\\");
				while (path != postPath) // Repeat until no more slashes can be replaced 
				{
					path = postPath;
					postPath = Regex.Replace(path, "[A-Za-z0-9\\.]+\\\\\\.\\.\\\\", "\\");
					postPath = postPath.Replace("\\\\", "\\");
				}
				Paths.Add(postPath);
			}

			tr.Close();
			fStr.Close();

			// Delete any extracted files
			if (DeleteAfterUse) {
				tf.Delete();
			}
		}
	}
}
