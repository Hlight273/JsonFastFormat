using System.Diagnostics;
using Microsoft.Win32;

internal static class Program
{
    const string MenuKeyPath = @"Software\Classes\SystemFileAssociations\.json\shell\JsonFastFormatPreview";
    const string CommandSubKey = "command";

    static readonly string MenuText = "\u901a\u8fc7\u8bb0\u4e8b\u672c\u9884\u89c8\u683c\u5f0f\u5316\u540e\u7684json";

    [STAThread]
    static int Main(string[] args)
    {
        if (args.Length >= 2 && string.Equals(args[0], "--preview", StringComparison.OrdinalIgnoreCase))
        {
            return PreviewFormattedJson(args[1]);
        }

        if (args.Length == 1 && string.Equals(args[0], "--install", StringComparison.OrdinalIgnoreCase))
        {
            InstallMenu();
            return 0;
        }

        if (args.Length == 1 && string.Equals(args[0], "--uninstall", StringComparison.OrdinalIgnoreCase))
        {
            UninstallMenu();
            return 0;
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new ManagerForm());
        return 0;
    }

    static int PreviewFormattedJson(string jsonPath)
    {
        try
        {
            string inputPath = Path.GetFullPath(jsonPath);
            string appDir = AppContext.BaseDirectory;
            string formatterPath = Path.Combine(appDir, "jsonfmt.exe");

            if (!File.Exists(formatterPath))
            {
                MessageBox.Show($"Cannot find jsonfmt.exe:\n{formatterPath}", "JsonFastFormat", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 2;
            }

            string previewDir = Path.Combine(Path.GetTempPath(), "JsonFastFormatPreview");
            Directory.CreateDirectory(previewDir);
            CleanupOldPreviewFiles(previewDir);

            string baseName = Path.GetFileNameWithoutExtension(inputPath);
            string safeName = string.Join("_", baseName.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
            string timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            string previewPath = Path.Combine(previewDir, $"{safeName}.formatted.{timestamp}.json");

            var formatter = Process.Start(new ProcessStartInfo
            {
                FileName = formatterPath,
                UseShellExecute = false,
                CreateNoWindow = true,
                Arguments = $"\"{inputPath}\" \"{previewPath}\""
            });

            formatter?.WaitForExit();
            if (formatter is null || formatter.ExitCode != 0)
            {
                MessageBox.Show("JSON formatting failed. The file may not be valid JSON.", "JsonFastFormat", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return formatter?.ExitCode ?? 3;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = "notepad.exe",
                Arguments = $"\"{previewPath}\"",
                UseShellExecute = true
            });

            return 0;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "JsonFastFormat", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 1;
        }
    }

    static void CleanupOldPreviewFiles(string previewDir)
    {
        DateTime cutoff = DateTime.Now.AddDays(-7);

        foreach (string file in Directory.EnumerateFiles(previewDir, "*.json"))
        {
            try
            {
                if (File.GetLastWriteTime(file) < cutoff)
                {
                    File.Delete(file);
                }
            }
            catch
            {
                // Best-effort cleanup; preview should not fail because a temp file is locked.
            }
        }
    }

    sealed class ManagerForm : Form
    {
        readonly Label statusLabel = new();
        readonly Button installButton = new();
        readonly Button uninstallButton = new();

        public ManagerForm()
        {
            Text = "JsonFastFormat";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(460, 220);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            Font = new Font("Microsoft YaHei UI", 9F);

            string iconPath = Path.Combine(AppContext.BaseDirectory, "JsonFastFormat.ico");
            if (File.Exists(iconPath))
            {
                Icon = new Icon(iconPath);
            }

            var title = new Label
            {
                Text = "JSON \u53f3\u952e\u83dc\u5355\u5f00\u5173",
                Font = new Font("Microsoft YaHei UI", 14F, FontStyle.Bold),
                Location = new Point(24, 20),
                Size = new Size(390, 30)
            };

            statusLabel.Location = new Point(26, 64);
            statusLabel.Size = new Size(390, 28);

            var detail = new Label
            {
                Text = "\u83dc\u5355\u9879: " + MenuText,
                Location = new Point(26, 96),
                Size = new Size(400, 24)
            };

            installButton.Text = "\u542f\u7528\u53f3\u952e\u83dc\u5355";
            installButton.Location = new Point(28, 142);
            installButton.Size = new Size(125, 36);
            installButton.Click += (_, _) => RunAction(InstallMenu, "\u53f3\u952e\u83dc\u5355\u5df2\u542f\u7528\u3002");

            uninstallButton.Text = "\u5173\u95ed\u53f3\u952e\u83dc\u5355";
            uninstallButton.Location = new Point(166, 142);
            uninstallButton.Size = new Size(125, 36);
            uninstallButton.Click += (_, _) => RunAction(UninstallMenu, "\u53f3\u952e\u83dc\u5355\u5df2\u5173\u95ed\u3002");

            var closeButton = new Button
            {
                Text = "\u5173\u95ed",
                Location = new Point(304, 142),
                Size = new Size(92, 36)
            };
            closeButton.Click += (_, _) => Close();

            Controls.AddRange([title, statusLabel, detail, installButton, uninstallButton, closeButton]);
            RefreshStatus();
        }

        void RunAction(Action action, string message)
        {
            try
            {
                action();
                RefreshStatus();
                MessageBox.Show(message, "JsonFastFormat", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "JsonFastFormat", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        void RefreshStatus()
        {
            bool installed = IsInstalled();
            statusLabel.Text = installed
                ? "\u5f53\u524d\u72b6\u6001: \u5df2\u542f\u7528"
                : "\u5f53\u524d\u72b6\u6001: \u672a\u542f\u7528";
            statusLabel.ForeColor = installed ? Color.FromArgb(20, 120, 60) : Color.FromArgb(160, 70, 30);
            installButton.Enabled = !installed;
            uninstallButton.Enabled = installed;
        }
    }

    static bool IsInstalled()
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(MenuKeyPath);
        return key is not null;
    }

    static void InstallMenu()
    {
        string managerPath = Environment.ProcessPath ?? Application.ExecutablePath;
        using RegistryKey menuKey = Registry.CurrentUser.CreateSubKey(MenuKeyPath, writable: true)
            ?? throw new InvalidOperationException("Cannot create registry key.");
        menuKey.SetValue("MUIVerb", MenuText, RegistryValueKind.String);
        menuKey.SetValue("Icon", $"\"{managerPath}\",0", RegistryValueKind.String);

        using RegistryKey commandKey = menuKey.CreateSubKey(CommandSubKey, writable: true)
            ?? throw new InvalidOperationException("Cannot create registry command key.");
        commandKey.SetValue(null, $"\"{managerPath}\" --preview \"%1\"", RegistryValueKind.String);
    }

    static void UninstallMenu()
    {
        Registry.CurrentUser.DeleteSubKeyTree(MenuKeyPath, throwOnMissingSubKey: false);
    }
}
