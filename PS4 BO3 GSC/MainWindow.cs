using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;
using MetroFramework.Forms;
using libdebug;
using TreyarchCompiler;
using System.Windows.Forms.VisualStyles;
using System.Globalization;
using System.Diagnostics;
using System.Reflection;
using T89CompilerLib;
using TreyarchCompiler.Utilities;
using TreyarchCompiler.Interface;
using TreyarchCompiler.Enums;
using TreyarchCompiler.Games;

namespace PS4_BO3_GSC
{
    public partial class MainWindow : MetroForm
    {

        public MainWindow()
        {
            InitializeComponent();
        }

        private void MainWindow_Load(object sender, EventArgs e)
        {

        }

        private void T7browseGscFolderButton_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                DialogResult result = fbd.ShowDialog();
                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    T7gscProjectFolderTextBox.Text = fbd.SelectedPath;
                }
            }
        }

        private void T7browseOutputPathButton_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Compiled GSC Files (*.gscc)|*.gscc";
            saveFileDialog.RestoreDirectory = true;
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                T7compiledGscFileOutputTextBox.Text = saveFileDialog.FileName;
            }
        }

        private void T7compileGscProjectButton_Click(object sender, EventArgs e)
        {
            if (T7gscProjectFolderTextBox.Text == "" || T7compiledGscOutputLabel.Text == "")
            {
                MessageBox.Show(this, "Please select a gsc project folder and a location to save the compiled GSC file.", "Fill Out All Fields", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            List<string> conditionalSymbols = new List<string>();

            if (File.Exists("gsc.conf"))
            {
                foreach (string line in File.ReadAllLines("gsc.conf"))
                {
                    if (line.Trim().StartsWith("#")) continue;
                    var split = line.Trim().Split('=');
                    if (split.Length < 2) continue;
                    switch (split[0].ToLower().Trim())
                    {
                        case "symbols":
                            foreach (string token in split[1].Trim().Split(','))
                            {
                                conditionalSymbols.Add(token);
                            }
                            break;
                    }
                }
            }
            string source = "";
            CompiledCode code;
            List<SourceTokenDef> sourceTokens = new List<SourceTokenDef>();
            StringBuilder sb = new StringBuilder();
            int currentLineCount = 0;
            int currentCharCount = 0;
            foreach (string file in Directory.EnumerateFiles(T7gscProjectFolderTextBox.Text, "*.gsc", SearchOption.AllDirectories).Where(x => x.EndsWith(".gsc", StringComparison.CurrentCultureIgnoreCase)))
            {
                var CurrentSource = new SourceTokenDef();
                CurrentSource.FilePath = file.Replace(T7gscProjectFolderTextBox.Text, "").Substring(1).Replace("\\", "/");
                CurrentSource.LineStart = currentLineCount;
                CurrentSource.CharStart = currentCharCount;
                foreach (var line in File.ReadAllLines(file))
                {
                    CurrentSource.LineMappings[currentLineCount] = (currentCharCount, currentCharCount + line.Length + 1);
                    sb.Append(line);
                    sb.Append("\n");
                    currentLineCount += 1;
                    currentCharCount += line.Length + 1;
                }
                CurrentSource.LineEnd = currentLineCount;
                CurrentSource.CharEnd = currentCharCount;
                sourceTokens.Add(CurrentSource);
                sb.Append("\n");
            }
            source = sb.ToString();
            var ppc = new ConditionalBlocks();
            conditionalSymbols.Add("BO3");
            ppc.LoadConditionalTokens(conditionalSymbols);

            try
            {
                source = ppc.ParseSource(source);
            }
            catch (CBSyntaxException error)
            {
                int errorCharPos = error.ErrorPosition;
                int numLineBreaks = 0;
                foreach (var stok in sourceTokens)
                {
                    do
                    {
                        if (errorCharPos < stok.CharStart || errorCharPos > stok.CharEnd)
                        {
                            break;
                        }
                        errorCharPos -= numLineBreaks;
                        foreach (var line in stok.LineMappings)
                        {
                            var constraints = line.Value;
                            if (errorCharPos < constraints.CStart || errorCharPos > constraints.CEnd)
                            {
                                continue;
                            }
                            MessageBox.Show(this, $"There was an error compiling your GSC Project\n{error.Message} in scripts/{stok.FilePath} at line {line.Key - stok.LineStart}, position {errorCharPos - constraints.CStart}", "Compiler Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                    }
                    while (false);
                    numLineBreaks++;
                }
                MessageBox.Show(this, "There was an error compiling your GSC Project.", "Compiler Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            code = Compiler.Compile(false, source);
            if (code.Error != null && code.Error.Length > 0)
            {
                MessageBox.Show(this, "There was an error compiling your GSC Project.", "Compiler Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            File.WriteAllBytes(T7compiledGscFileOutputTextBox.Text, code.CompiledScript);
            MessageBox.Show(this, $"Your compiled gsc file has been exported to {T7compiledGscFileOutputTextBox.Text}! Enjoy :)", "Compile Success!", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void T8browseGscFolderButton_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                DialogResult result = fbd.ShowDialog();
                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    T8gscProjectFolderTextBox.Text = fbd.SelectedPath;
                }
            }
        }

        private void T8browseOutputPathButton_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Compiled GSC Files (*.gscc)|*.gscc";
            saveFileDialog.RestoreDirectory = true;
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                T8compiledGscFileOutputTextBox.Text = saveFileDialog.FileName;
            }

        }

        private void T8compileGscProjectButton_Click(object sender, EventArgs e)
        {
            if (T8gscProjectFolderTextBox.Text == "" || T8compiledGscOutputLabel.Text == "")
            {
                MessageBox.Show(this, "Please select a GSC project folder and a location to save the compiled GSC file.", "Fill Out All Fields", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            List<string> conditionalSymbols = new List<string>();

            // Load symbols from configuration
            if (File.Exists("gsc.conf"))
            {
                foreach (string line in File.ReadAllLines("gsc.conf"))
                {
                    if (line.Trim().StartsWith("#")) continue;
                    var split = line.Trim().Split('=');
                    if (split.Length < 2) continue;
                    switch (split[0].ToLower().Trim())
                    {
                        case "symbols":
                            foreach (string token in split[1].Trim().Split(','))
                            {
                                conditionalSymbols.Add(token);
                            }
                            break;
                    }
                }
            }
            string source = "";
            CompiledCode code;
            List<SourceTokenDef> SourceTokens = new List<SourceTokenDef>();
            StringBuilder sb = new StringBuilder();
            int CurrentLineCount = 0;
            int CurrentCharCount = 0;
            foreach (string f in Directory.GetFiles(T8gscProjectFolderTextBox.Text, "*.gsc", SearchOption.AllDirectories))
            {
                var CurrentSource = new SourceTokenDef();
                CurrentSource.FilePath = f.Replace(T8gscProjectFolderTextBox.Text, "").Substring(1).Replace("\\", "/");
                CurrentSource.LineStart = CurrentLineCount;
                CurrentSource.CharStart = CurrentCharCount;
                foreach (var line in File.ReadAllLines(f))
                {
                    CurrentSource.LineMappings[CurrentLineCount] = (CurrentCharCount, CurrentCharCount + line.Length + 1);
                    sb.Append(line);
                    sb.Append("\n");
                    CurrentLineCount += 1;
                    CurrentCharCount += line.Length + 1; // + \n
                }
                CurrentSource.LineEnd = CurrentLineCount;
                CurrentSource.CharEnd = CurrentCharCount;
                SourceTokens.Add(CurrentSource);
                sb.Append("\n");
            }
            source = sb.ToString();
            var ppc = new ConditionalBlocks();
            conditionalSymbols.Add("BO4");
            ppc.LoadConditionalTokens(conditionalSymbols);
            try
            {
                source = ppc.ParseSource(source);
            }
            catch (CBSyntaxException ex) // Rename the local variable
            {
                int errorCharPos = ex.ErrorPosition;
                int numLineBreaks = 0;
                foreach (var stok in SourceTokens)
                {
                    do
                    {
                        if (errorCharPos < stok.CharStart || errorCharPos > stok.CharEnd)
                        {
                            break;
                        }
                        errorCharPos -= numLineBreaks;
                        foreach (var line in stok.LineMappings)
                        {
                            var constraints = line.Value;
                            if (errorCharPos < constraints.CStart || errorCharPos > constraints.CEnd)
                            {
                                continue;
                            }
                            MessageBox.Show(this, $"{ex.Message} in scripts/{stok.FilePath} at line {line.Key - stok.LineStart}, position {errorCharPos - constraints.CStart}");
                        }
                    }
                    while (false);
                    numLineBreaks++;
                }
                MessageBox.Show(ex.Message);
            }

            code = Compiler.Compile(false, source);
            if (code.Error != null && code.Error.Length > 0)
            {
                if (code.Error.LastIndexOf("line=") < 0)
                {
                    MessageBox.Show(code.Error);
                }
                int iStart = code.Error.LastIndexOf("line=") + "line=".Length;
                int iLength = code.Error.LastIndexOf("]") - iStart;
                int line = int.Parse(code.Error.Substring(iStart, iLength));
                // Console.WriteLine(code.Error + " :: " + line);
                foreach (var stok in SourceTokens)
                {
                    do
                    {
                        if (stok.LineStart <= line && stok.LineEnd >= line)
                        {
                            MessageBox.Show(this, $"Syntax error in scripts/{stok.FilePath} around line {line - stok.LineStart + 1}");
                        }
                    }
                    while (false);
                    line--; // acccount for linebreaks appended to each file
                }
                MessageBox.Show(code.Error);
            }

            if (code.StubbedScript != null)
            {
                File.WriteAllBytes($"compiled.stub.gscc", code.StubScriptData);
            }

            string cpath = $"compiled.{("gscc")}";
            File.WriteAllBytes(cpath, code.CompiledScript);
            string hpath = "hashes.txt";
            StringBuilder hashes = new StringBuilder();
            foreach (var kvp in code.HashMap)
            {
                hashes.AppendLine($"0x{kvp.Key:X}, {kvp.Value}");
            }
            File.WriteAllText(hpath, hashes.ToString());

            if (code.OpcodeEmissions != null)
            {
                byte[] opsRaw = new byte[code.OpcodeEmissions.Count * 4];
                for (int i = 0; i < code.OpcodeEmissions.Count; i++)
                {
                    BitConverter.GetBytes(code.OpcodeEmissions[i]).CopyTo(opsRaw, i * 4);
                }
                File.WriteAllBytes("compiled.omap", opsRaw);
            }
        }
    }
}
