using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.IO;

namespace CodeGenerator
{
    public static class Open
    {
        public static void OnNotepad(string nameFile, string text)
        {
            File.WriteAllText(nameFile, text, Encoding.Unicode);
            string pt = Directory.GetCurrentDirectory() + "\\"+nameFile;
            Process.Start(pt);
        }
    }
}
