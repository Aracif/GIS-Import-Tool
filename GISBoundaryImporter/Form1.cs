using Microsoft.Data.SqlClient;
using System.Diagnostics;

namespace GISBoundaryImporter // Change this to match your project namespace if different
{
    public partial class Form1 : Form
    {
        private TextBox txtServerName;
        private TextBox txtTempDatabase;
        private TextBox txtTargetDatabase;
        private TextBox txtTenantId;
        private TextBox txtShapeFilePath;
        private TextBox txtEPSG;
        private TextBox txtLog;
        private Button btnBrowseShapeFile;
        private Button btnCheckGDAL;
        private Button btnStep1Import;
        private Button btnStep2Transfer;
        private Button btnTestGeography;
        private Button btnExportWKT;
        private ComboBox cboFixOption;
        private Label lblStatus;
        private Button btnTestDb;

        private ProgressBar progressBar;

        // Right-side Test Geography UI
        private Panel pnlTestGeoHost;
        private TestGeographyPanel? testGeoPanel;

// OSGeo4W selection + path UI
        private RadioButton rdoOsgeo4w;
        private TextBox txtOgr2OgrPath;
        private Button btnBrowseOgr;

// Internal mode + selected ogr2ogr path
        private enum GdalMode
        {
            Osgeo4w,
            Custom
        }

        private GdalMode _gdalMode = GdalMode.Osgeo4w;
        private string? _ogr2ogrPath;

        public Form1()
        {
            InitializeComponent();
            CheckGDALInstallation();
        }

        private void InitializeComponent()
        {
            this.Text = "GIS Boundary Import Tool for Parks & Rec";
            this.Size = new Size(800, 700);
            this.StartPosition = FormStartPosition.CenterScreen;

            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 16, Padding = new Padding(10)
            };

            
            // Headers
            var lblTitle = new Label
            {
                Text = "Parks & Rec GIS Boundary Import Tool",
                Font = new Font("Arial", 14, FontStyle.Bold),
                AutoSize = true,
                ForeColor = Color.DarkBlue
            };
            mainPanel.SetColumnSpan(lblTitle, 3);

            // Server Configuration
            mainPanel.Controls.Add(new Label { Text = "SQL Server Name:", AutoSize = true }, 0, 1);
            txtServerName = new TextBox { Width = 250, Text = Environment.MachineName + "\\MSSQLSERVER2022" };
            mainPanel.Controls.Add(txtServerName, 1, 1);

            mainPanel.Controls.Add(new Label { Text = "Temp Database:", AutoSize = true }, 0, 2);
            txtTempDatabase = new TextBox { Width = 250, Text = "temp_import" };
            mainPanel.Controls.Add(txtTempDatabase, 1, 2);

            mainPanel.Controls.Add(new Label { Text = "Target Database:", AutoSize = true }, 0, 3);
            txtTargetDatabase = new TextBox { Width = 250, Text = "parksrec" };
            mainPanel.Controls.Add(txtTargetDatabase, 1, 3);

            // Place Test DB Connection near DB inputs
            btnTestDb = new Button { Text = "Test DB Connection", Width = 150, Height = 30 };
            btnTestDb.Click += BtnTestDb_Click;
            mainPanel.Controls.Add(btnTestDb, 2, 3);

            mainPanel.Controls.Add(new Label { Text = "Tenant ID:", AutoSize = true }, 0, 4);
            txtTenantId = new TextBox { Width = 100 };
            mainPanel.Controls.Add(txtTenantId, 1, 4);

            // Shape File Configuration
            mainPanel.Controls.Add(new Label { Text = "Shape File Path:", AutoSize = true }, 0, 5);
            txtShapeFilePath = new TextBox { Width = 250 };
            mainPanel.Controls.Add(txtShapeFilePath, 1, 5);
            btnBrowseShapeFile = new Button { Text = "Browse...", Width = 80 };
            btnBrowseShapeFile.Click += BtnBrowseShapeFile_Click;
            mainPanel.Controls.Add(btnBrowseShapeFile, 2, 5);

            mainPanel.Controls.Add(new Label { Text = "EPSG Code:", AutoSize = true }, 0, 6);
            txtEPSG = new TextBox { Width = 100, Text = "4326" };
            mainPanel.Controls.Add(txtEPSG, 1, 6);

            mainPanel.Controls.Add(new Label { Text = "Fix Option:", AutoSize = true }, 0, 7);
            cboFixOption = new ComboBox { Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
            cboFixOption.Items.AddRange(new[] { "None", "MakeValid()", "ReorientObject()" });
            cboFixOption.SelectedIndex = 0;
            mainPanel.Controls.Add(cboFixOption, 1, 7);

            // Grouped action areas with uniform button styling
            Size uniformButtonSize = new Size(160, 32);
            Padding uniformButtonMargin = new Padding(6, 6, 6, 6);

            // Steps Group
            var gbSteps = new GroupBox { Text = "Steps", Dock = DockStyle.Top, AutoSize = true };
            var stepsPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                WrapContents = true,
                Padding = new Padding(8),
                Margin = new Padding(0)
            };
            btnStep1Import = new Button { Text = "Step 1: Import Shape" };
            btnStep1Import.Size = uniformButtonSize;
            btnStep1Import.Margin = uniformButtonMargin;
            btnStep1Import.FlatStyle = FlatStyle.System;
            btnStep1Import.Click += BtnStep1Import_Click;
            stepsPanel.Controls.Add(btnStep1Import);

            btnStep2Transfer = new Button { Text = "Step 2: Transfer to Tenant" };
            btnStep2Transfer.Size = uniformButtonSize;
            btnStep2Transfer.Margin = uniformButtonMargin;
            btnStep2Transfer.FlatStyle = FlatStyle.System;
            btnStep2Transfer.Click += BtnStep2Transfer_Click;
            stepsPanel.Controls.Add(btnStep2Transfer);

            gbSteps.Controls.Add(stepsPanel);
            mainPanel.Controls.Add(gbSteps, 0, 10);
            mainPanel.SetColumnSpan(gbSteps, 3);

            // Tools Group
            var gbTools = new GroupBox { Text = "Tools", Dock = DockStyle.Top, AutoSize = true };
            var toolsPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                WrapContents = true,
                Padding = new Padding(8),
                Margin = new Padding(0)
            };

            btnCheckGDAL = new Button { Text = "Check GDAL" };
            btnCheckGDAL.Size = uniformButtonSize;
            btnCheckGDAL.Margin = uniformButtonMargin;
            btnCheckGDAL.FlatStyle = FlatStyle.System;
            btnCheckGDAL.Click += BtnCheckGDAL_Click;
            toolsPanel.Controls.Add(btnCheckGDAL);

            btnTestGeography = new Button { Text = "Test Geography" };
            btnTestGeography.Size = uniformButtonSize;
            btnTestGeography.Margin = uniformButtonMargin;
            btnTestGeography.FlatStyle = FlatStyle.System;
            btnTestGeography.Click += BtnTestGeography_Click;
            toolsPanel.Controls.Add(btnTestGeography);

            btnExportWKT = new Button { Text = "Export to WKT" };
            btnExportWKT.Size = uniformButtonSize;
            btnExportWKT.Margin = uniformButtonMargin;
            btnExportWKT.FlatStyle = FlatStyle.System;
            btnExportWKT.Click += BtnExportWKT_Click;
            toolsPanel.Controls.Add(btnExportWKT);

            gbTools.Controls.Add(toolsPanel);
            mainPanel.Controls.Add(gbTools, 0, 11);
            mainPanel.SetColumnSpan(gbTools, 3);

            // Status
            lblStatus = new Label { Text = "Ready", AutoSize = true, ForeColor = Color.Green };
            mainPanel.Controls.Add(lblStatus, 0, 12);
            mainPanel.SetColumnSpan(lblStatus, 3);

            progressBar = new ProgressBar { Width = 750, Height = 20 };
            mainPanel.Controls.Add(progressBar, 0, 13);
            mainPanel.SetColumnSpan(progressBar, 3);

            // Log output
            txtLog = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Width = 750,
                Height = 200,
                Font = new Font("Consolas", 9)
            };
            mainPanel.Controls.Add(txtLog, 0, 14);
            mainPanel.SetColumnSpan(txtLog, 3);

            mainPanel.Controls.Add(lblTitle, 0, 0);

            // ===== NEW ROW: OGR2OGR / OSGeo4W selection =====

            // Row A: OSGeo4W-only indicator
            mainPanel.Controls.Add(new Label { Text = "OGR Source:", AutoSize = true }, 0, 8);
            var radioPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true };
            rdoOsgeo4w = new RadioButton { Text = "OSGeo4W", AutoSize = true, Checked = true, Enabled = false };
            radioPanel.Controls.Add(rdoOsgeo4w);
            mainPanel.Controls.Add(radioPanel, 1, 8);

            // Row B: path + Browse…
            mainPanel.Controls.Add(new Label { Text = "ogr2ogr.exe:", AutoSize = true }, 0, 9);
            var ogrPanel = new TableLayoutPanel { ColumnCount = 2, Dock = DockStyle.Fill };
            ogrPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            ogrPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            txtOgr2OgrPath = new TextBox { ReadOnly = true, Dock = DockStyle.Fill };
            btnBrowseOgr = new Button { Text = "Browse…", AutoSize = true };
            btnBrowseOgr.Click += (s, e) =>
            {
                using (var ofd = new OpenFileDialog
                       {
                           Filter = "ogr2ogr.exe|ogr2ogr.exe|Executables|*.exe", Title = "Select ogr2ogr.exe"
                       })
                {
                    if (ofd.ShowDialog(this) == DialogResult.OK)
                    {
                        _ogr2ogrPath = ofd.FileName;
                        txtOgr2OgrPath.Text = _ogr2ogrPath;
                        _gdalMode = GdalMode.Osgeo4w;
                        rdoOsgeo4w.Checked = true;
                    }
                }
            };
            ogrPanel.Controls.Add(txtOgr2OgrPath, 0, 0);
            ogrPanel.Controls.Add(btnBrowseOgr, 1, 0);
            mainPanel.Controls.Add(ogrPanel, 1, 9);

            // Try to auto-detect ogr2ogr and populate the UI
            EnsureOgr2OgrSelected();
            txtOgr2OgrPath.Text = _ogr2ogrPath ?? "(not found)";
            rdoOsgeo4w.Checked = true;
            


            // Create right-side host for Test Geography panel (hidden by default)
            pnlTestGeoHost = new Panel { Dock = DockStyle.Right, Width = 300, BorderStyle = BorderStyle.FixedSingle, Visible = false };
            testGeoPanel = new TestGeographyPanel();
            // Wire event for test requests
            testGeoPanel.TestRequested += async (s, args) => await HandleTestRequestedAsync(args);
            testGeoPanel.HideRequested += (s, e) =>
            {
                pnlTestGeoHost.Visible = false;
                LogMessage("=== Test Geography Panel Hidden ===");
                lblStatus.Text = "Ready";
                lblStatus.ForeColor = Color.Green;
            };
            pnlTestGeoHost.Controls.Add(testGeoPanel);

            // Add panels to form (add host first so mainPanel fills remaining space)
            this.Controls.Add(pnlTestGeoHost);
            this.Controls.Add(mainPanel);
        }
        
        
        private async void BtnTestDb_Click(object? sender, EventArgs e)
        {
            lblStatus.Text = "Testing SQL connections...";
            lblStatus.ForeColor = Color.DarkGoldenrod;

            // Test Temp database
            var (okTemp, msgTemp) = await DbConnectionHelper.TestConnectionAsync(
                txtServerName.Text.Trim(),
                txtTempDatabase.Text.Trim(),
                integratedSecurity: true,
                encrypt: true,
                trustServerCertificate: true,
                timeoutSeconds: 5
            );
            LogMessage("TEMP DB: " + msgTemp);

            // Test Target database
            var (okTarget, msgTarget) = await DbConnectionHelper.TestConnectionAsync(
                txtServerName.Text.Trim(),
                txtTargetDatabase.Text.Trim(),
                integratedSecurity: true,
                encrypt: true,
                trustServerCertificate: true,
                timeoutSeconds: 5
            );
            LogMessage("TARGET DB: " + msgTarget);

            if (okTemp && okTarget)
            {
                lblStatus.Text = "SQL connection(s) OK";
                lblStatus.ForeColor = Color.Green;
            }
            else
            {
                lblStatus.Text = "SQL connection failed";
                lblStatus.ForeColor = Color.Red;
            }
        }

        private void CheckGDALInstallation()
        {
            string[] candidates = { @"C:\\OSGeo4W64\\bin\\ogr2ogr.exe", @"C:\\OSGeo4W\\bin\\ogr2ogr.exe" };
            string? found = null;
            foreach (var c in candidates)
            {
                if (File.Exists(c)) { found = c; break; }
            }

            if (found != null)
            {
                LogMessage("✓ OSGeo4W ogr2ogr found at: " + found);
                lblStatus.Text = "OSGeo4W Installed";
                lblStatus.ForeColor = Color.Green;
            }
            else
            {
                LogMessage("✗ OSGeo4W not found. Please install OSGeo4W and ensure ogr2ogr.exe is available.");
                lblStatus.Text = "OSGeo4W Not Found - Please Install";
                lblStatus.ForeColor = Color.Red;
            }
        }

        private string? AutoDetectOgr2Ogr(out GdalMode mode)
        {
            var candidates = new[]
            {
                // OSGeo4W standard paths
                @"C:\\OSGeo4W64\\bin\\ogr2ogr.exe",
                @"C:\\OSGeo4W\\bin\\ogr2ogr.exe"
            };

            foreach (var p in candidates)
            {
                if (File.Exists(p))
                {
                    mode = GdalMode.Osgeo4w;
                    return p;
                }
            }

            mode = GdalMode.Custom;
            return null;
        }

        private void EnsureOgr2OgrSelected()
        {
            if (string.IsNullOrWhiteSpace(_ogr2ogrPath) || !File.Exists(_ogr2ogrPath))
            {
                _ogr2ogrPath = AutoDetectOgr2Ogr(out _gdalMode);
            }
        }

        /// <summary>
        /// Build the ProcessStartInfo for ogr2ogr with the correct env vars.
        /// In OSGeo4W mode we set PATH/GDAL_DATA/PROJ_LIB so it finds grids / EPSG.
        /// </summary>
private ProcessStartInfo BuildOgr2OgrPsi(string arguments)
        {
            var path = _ogr2ogrPath;
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                // Fallbacks if not set
                path = AutoDetectOgr2Ogr(out _gdalMode) ?? @"C:\\OSGeo4W64\\bin\\ogr2ogr.exe";
            }

            var psi = new ProcessStartInfo
            {
                FileName = path,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            if (_gdalMode == GdalMode.Osgeo4w)
            {
                // Typically: C:\OSGeo4W\bin\ogr2ogr.exe
                var binDir = Path.GetDirectoryName(path)!; // ...\OSGeo4W\bin
                var root = Directory.GetParent(binDir)!.FullName; // ...\OSGeo4W
                var share = Path.Combine(root, "share");

                // PATH for dependent DLLs
                psi.EnvironmentVariables["PATH"] = (Environment.GetEnvironmentVariable("PATH") ?? "") + ";" + binDir;

                // Data dirs
                psi.EnvironmentVariables["GDAL_DATA"] = Path.Combine(share, "gdal");
                psi.EnvironmentVariables["PROJ_LIB"] = Path.Combine(share, "proj");

                // ** START OF CHANGE **
                // Set GDAL_DRIVER_PATH to find plugins like ogr_MSSQLSpatial.dll.
                // Newer OSGeo4W installations place this in a deeper path.
                var modernPluginPath = Path.Combine(root, "apps", "gdal", "lib", "gdalplugins");
                var legacyPluginPath = Path.Combine(binDir, "gdalplugins");

                if (Directory.Exists(modernPluginPath))
                {
                    psi.EnvironmentVariables["GDAL_DRIVER_PATH"] = modernPluginPath;
                }
                else if (Directory.Exists(legacyPluginPath))
                {
                    psi.EnvironmentVariables["GDAL_DRIVER_PATH"] = legacyPluginPath;
                }
                // ** END OF CHANGE **
            }

            return psi;
        }

        private void BtnBrowseShapeFile_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = "Shapefile XML (*.shp.xml)|*.shp.xml|All files (*.*)|*.*";
                ofd.Title = "Select the Shapefile XML (.shp.xml)";

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    txtShapeFilePath.Text = ofd.FileName;
                    // Pass the selected XML file directly to the detection method
                    DetectEPSG(ofd.FileName);
                }
            }
        }

        private void DetectEPSG(string xmlFilePath)
        {
            try
            {
                if (!xmlFilePath.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                {
                    LogMessage("EPSG detection requires a .shp.xml file.");
                    return;
                }

                string content = File.ReadAllText(xmlFilePath);
                int epsgIndex = content.IndexOf("EPSG", StringComparison.OrdinalIgnoreCase);
                if (epsgIndex > 0)
                {
                    // Try to extract the EPSG code
                    string substring = content.Substring(epsgIndex, Math.Min(50, content.Length - epsgIndex));
                    var numbers = System.Text.RegularExpressions.Regex.Match(substring, @"\d{4,5}");
                    if (numbers.Success)
                    {
                        txtEPSG.Text = numbers.Value;
                        LogMessage($"Detected EPSG: {numbers.Value} from {Path.GetFileName(xmlFilePath)}");
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Could not auto-detect EPSG: {ex.Message}");
            }
        }
        private void BtnCheckGDAL_Click(object? sender, EventArgs e)
        {
            try
            {
                EnsureOgr2OgrSelected();
                if (string.IsNullOrWhiteSpace(_ogr2ogrPath) || !File.Exists(_ogr2ogrPath))
                {
                    lblStatus.Text = "ogr2ogr.exe not found";
                    lblStatus.ForeColor = Color.Red;
                    LogMessage("ogr2ogr.exe not found. Browse to it or install OSGeo4W.");
                    return;
                }

                txtOgr2OgrPath.Text = _ogr2ogrPath;
                lblStatus.Text = "ogr2ogr found";
                lblStatus.ForeColor = Color.Green;

                // Show version
                var psi = BuildOgr2OgrPsi("--version");
                using var p = new Process { StartInfo = psi };
                p.Start();
                var output = p.StandardOutput.ReadToEnd();
                var error = p.StandardError.ReadToEnd();
                p.WaitForExit();

                if (!string.IsNullOrEmpty(output)) LogMessage(output.Trim());
                if (!string.IsNullOrEmpty(error)) LogMessage("ERR: " + error.Trim());

                // Optional: list available formats and confirm MSSQLSpatial exists
                var psiFmt = BuildOgr2OgrPsi("--formats");
                using var pf = new Process { StartInfo = psiFmt };
                pf.Start();
                var formats = pf.StandardOutput.ReadToEnd();
                pf.WaitForExit();

                if (formats.IndexOf("MSSQLSpatial", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    LogMessage("MSSQLSpatial driver detected ✔");
                }
                else
                {
                    LogMessage(
                        "⚠ MSSQLSpatial driver NOT listed. If using OSGeo4W, add the GDAL MSSQL driver via the OSGeo4W installer.");
                    // Driver notes: https://gdal.org/.../mssqlspatial.html and community threads.
                }
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Error checking GDAL";
                lblStatus.ForeColor = Color.Red;
                LogMessage("Exception: " + ex.Message);
            }
        }

private void BtnStep1Import_Click(object? sender, EventArgs e)
{
    try
    {
        EnsureTempDatabase();

        if (!ValidateInputsForImport()) return;


        progressBar.Value = 10;
        EnsureOgr2OgrSelected();

        // The text box contains the path to the .shp.xml file.
        // The ogr2ogr command needs the path to the directory containing the shape files.
        var shpXmlPath = txtShapeFilePath.Text;
        var shapeFileDirectory = Path.GetDirectoryName(shpXmlPath);

        var epsg = txtEPSG.Text.Trim();

        // If the source EPSG is 4326, use -a_srs; otherwise, convert it.
        string srsArgs = string.IsNullOrWhiteSpace(epsg) || epsg == "4326"
            ? "-a_srs \"EPSG:4326\""
            : $"-s_srs \"EPSG:{epsg}\" -t_srs \"EPSG:4326\"";

        // Destination: MSSQLSpatial connection string.
        string dsn =
            $"MSSQL:server={txtServerName.Text};database={txtTempDatabase.Text};trusted_connection=yes;Driver={{ODBC Driver 17 for SQL Server}}";

        // ** FIXED: Removed invalid BCP layer creation option **
        // Layer/table name and creation options.
        // BCP is controlled via --config option, not -lco
        string tableName = "ogr_import";
        string lco =
            "-lco \"GEOM_TYPE=geography\" -lco \"GEOM_NAME=ogr_geometry\" -lco \"SPATIAL_INDEX=No\" -lco \"PRECISION=No\" -lco \"LAUNDER=No\"";
        
        // Explicitly disable BCP using the configuration option
        string bcpConfig = "--config MSSQLSPATIAL_USE_BCP FALSE";

        string arguments =
            $"-overwrite -f MSSQLSpatial \"{dsn}\" \"{shapeFileDirectory}\" -nln \"{tableName}\" {lco} {srsArgs} {bcpConfig} -progress";

        LogMessage($"Running: ogr2ogr {arguments}");
        progressBar.Value = 40;

        var psi = BuildOgr2OgrPsi(arguments);
        using var process = new Process { StartInfo = psi };
        process.Start();
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (!string.IsNullOrEmpty(output)) LogMessage("Output: " + output);
        if (!string.IsNullOrEmpty(error)) LogMessage("Error: " + error);

        if (process.ExitCode == 0)
        {
            progressBar.Value = 100;
            lblStatus.Text = "Import successful!";
            lblStatus.ForeColor = Color.Green;
            LogMessage("✓ Shapefile imported to temp DB");
        }
        else
        {
            lblStatus.Text = "ogr2ogr failed";
            lblStatus.ForeColor = Color.Red;
            LogMessage($"ogr2ogr exit code: {process.ExitCode}");
        }
    }
    catch (Exception ex)
    {
        lblStatus.Text = "Import error";
        lblStatus.ForeColor = Color.Red;
        LogMessage("Exception: " + ex.Message);
    }
}private void DetectImportedTable()
        {
            try
            {
                using (var conn = new SqlConnection(
                           $"Server={txtServerName.Text};Database={txtTargetDatabase.Text};" +
                           $"Integrated Security=SSPI;Encrypt=True;TrustServerCertificate=True;"))
                {
                    conn.Open();
                    var cmd = new SqlCommand(
                        "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'", conn);
                    using (var reader = cmd.ExecuteReader())
                    {
                        var tables = new List<string>();
                        while (reader.Read())
                        {
                            tables.Add(reader.GetString(0));
                        }

                        if (tables.Count > 0)
                        {
                            LogMessage($"Found tables: {string.Join(", ", tables)}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Could not detect tables: {ex.Message}");
            }
        }

        private void BtnStep2Transfer_Click(object sender, EventArgs e)
        {
            if (!ValidateInputs()) return;

            progressBar.Value = 0;
            lblStatus.Text = "Transferring to tenant table...";
            LogMessage("\n=== Starting Step 2: Transfer to Tenant Table ===");

            try
            {
                // Prompt for source table name
                string sourceTable = Microsoft.VisualBasic.Interaction.InputBox(
                    "Enter the source table name (without dbo. prefix):", "Source Table", "ogr_import");


                if (string.IsNullOrEmpty(sourceTable))
                {
                    LogMessage("No source table specified.");
                    return;
                }

                string fixAction = "";
                switch (cboFixOption.SelectedIndex)
                {
                    case 0:
                        fixAction = "@g";
                        break;
                    case 1:
                        fixAction = "@g.MakeValid()";
                        break;
                    case 2:
                        fixAction = "@g.ReorientObject()";
                        break;
                }

                string sql = $@"
                    DECLARE @g geography;
                    SET @g = (SELECT ogr_geometry FROM [{txtTempDatabase.Text}].dbo.[{sourceTable}] WHERE ogr_fid = 1);
                    UPDATE [{txtTargetDatabase.Text}].dbo.tenant SET tenant_boundary = {fixAction} WHERE tenant_id = {txtTenantId.Text};";

                LogMessage($"Executing SQL: {sql}");
                progressBar.Value = 50;

                using (var conn = new SqlConnection(
                           $"Server={txtServerName.Text};Database={txtTargetDatabase.Text};" +
                           $"Integrated Security=SSPI;Encrypt=True;TrustServerCertificate=True;"))
                {
                    conn.Open();
                    var cmd = new SqlCommand(sql, conn);
                    int rowsAffected = cmd.ExecuteNonQuery();

                    progressBar.Value = 100;
                    lblStatus.Text = "Transfer successful!";
                    lblStatus.ForeColor = Color.Green;
                    LogMessage($"✓ Updated {rowsAffected} row(s) in tenant table");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error during transfer: {ex.Message}");
                lblStatus.Text = "Transfer error";
                lblStatus.ForeColor = Color.Red;

                // Suggest fixes based on error message
                if (ex.Message.Contains("MakeValid"))
                {
                    LogMessage("Suggestion: Try selecting 'MakeValid()' option and retry.");
                    cboFixOption.SelectedIndex = 1;
                }
                else if (ex.Message.Contains("hemisphere") || ex.Message.Contains("ReorientObject"))
                {
                    LogMessage("Suggestion: Try selecting 'ReorientObject()' option and retry.");
                    cboFixOption.SelectedIndex = 2;
                }
            }
        }

private void BtnTestGeography_Click(object sender, EventArgs e)
{
    // Toggle the right-side Test Geography panel
    if (pnlTestGeoHost != null)
    {
        pnlTestGeoHost.Visible = !pnlTestGeoHost.Visible;
        if (pnlTestGeoHost.Visible)
        {
            LogMessage("\n=== Test Geography Panel Opened ===");
            lblStatus.Text = "Enter coordinates and click Run Test";
            lblStatus.ForeColor = Color.Blue;
        }
        else
        {
            LogMessage("=== Test Geography Panel Hidden ===");
            lblStatus.Text = "Ready";
            lblStatus.ForeColor = Color.Green;
        }
    }
}

private (double lat, double lon)? ParseCoordinates(string coordString)
{
    try
    {
        var parts = coordString.Trim().Split(',');
        if (parts.Length != 2) return null;
        
        double lat = double.Parse(parts[0].Trim());
        double lon = double.Parse(parts[1].Trim());
        
        // Basic validation
        if (lat < -90 || lat > 90 || lon < -180 || lon > 180)
            return null;
            
        return (lat, lon);
    }
    catch
    {
        return null;
    }
}

private async Task<bool> TestPointInBoundary(SqlConnection conn, double latitude, double longitude)
{
    string sql = $@"
        DECLARE @point geography;
        SET @point = geography::Point(@lat, @lon, 4326);
        
        SELECT CASE 
            WHEN tenant_boundary.STIntersects(@point) = 1 THEN 1
            ELSE 0
        END as IsInside
        FROM [{txtTargetDatabase.Text}].dbo.tenant
        WHERE tenant_id = @tenantId";

    using (var cmd = new SqlCommand(sql, conn))
    {
        cmd.Parameters.AddWithValue("@lat", latitude);
        cmd.Parameters.AddWithValue("@lon", longitude);
        cmd.Parameters.AddWithValue("@tenantId", int.Parse(txtTenantId.Text));
        
        var result = await cmd.ExecuteScalarAsync();
        return result != null && result != DBNull.Value && Convert.ToBoolean(result);
    }
}
        private void BtnExportWKT_Click(object sender, EventArgs e)
        {
            if (!ValidateInputs()) return;

            LogMessage("\n=== Exporting to Well Known Text (WKT) ===");

            try
            {
                string sql = $@"
                    SELECT tenant_boundary.STAsText() AS WKT
                    FROM [{txtTargetDatabase.Text}].dbo.tenant
                    WHERE tenant_id = {txtTenantId.Text}";

                using (var conn = new SqlConnection(
                           $"Server={txtServerName.Text};Database={txtTargetDatabase.Text};" +
                           $"Integrated Security=SSPI;Encrypt=True;TrustServerCertificate=True;"))
                {
                    conn.Open();
                    var cmd = new SqlCommand(sql, conn);
                    var result = cmd.ExecuteScalar();

                    if (result != null && result != DBNull.Value)
                    {
                        string wkt = result.ToString();

                        // Save to file
                        var saveDialog = new SaveFileDialog
                        {
                            Filter = "Text files (*.txt)|*.txt|SQL files (*.sql)|*.sql",
                            FileName = $"tenant_{txtTenantId.Text}_boundary.sql"
                        };

                        if (saveDialog.ShowDialog() == DialogResult.OK)
                        {
                            string updateSql =
                                $"UPDATE tenant SET tenant_boundary = '{wkt}' WHERE tenant_id = {txtTenantId.Text};";
                            File.WriteAllText(saveDialog.FileName, updateSql);
                            LogMessage($"✓ WKT exported to: {saveDialog.FileName}");
                            LogMessage("You can now run this SQL on your production server.");
                        }
                    }
                    else
                    {
                        LogMessage("✗ No geography data found for this tenant ID.");
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error exporting WKT: {ex.Message}");
            }
        }

        private bool ValidateInputs()
        {
            if (string.IsNullOrWhiteSpace(txtServerName.Text))
            {
                MessageBox.Show("Please enter SQL Server name", "Validation Error", MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtTenantId.Text))
            {
                MessageBox.Show("Please enter Tenant ID", "Validation Error", MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }

private bool ValidateInputsForImport()
{
    // reset status line
    lblStatus.Text = "";
    lblStatus.ForeColor = System.Drawing.Color.Black;

    var errors = new List<string>();

    // 1) Basic text fields
    string server = txtServerName.Text?.Trim() ?? "";
    string tempDb = txtTempDatabase.Text?.Trim() ?? "";
    string shpXml = txtShapeFilePath.Text?.Trim() ?? ""; // Renamed for clarity
    string epsg = txtEPSG.Text?.Trim() ?? "";

    if (string.IsNullOrWhiteSpace(server)) errors.Add("SQL Server name is required.");
    if (string.IsNullOrWhiteSpace(tempDb)) errors.Add("Temp database name is required.");

    // 2) Shapefile XML existence (.shp.xml required)
    if (string.IsNullOrWhiteSpace(shpXml))
    {
        errors.Add("Path to the .shp.xml file is required.");
    }
    else
    {
        if (!File.Exists(shpXml))
            errors.Add($".shp.xml file not found: {shpXml}");
        else if (!shpXml.EndsWith(".shp.xml", StringComparison.OrdinalIgnoreCase))
            errors.Add("Selected file must be a .shp.xml file.");
        else
        {
            // Also check that the corresponding .shp file exists
            var correspondingShp = shpXml.Replace(".shp.xml", ".shp");
            if (!File.Exists(correspondingShp))
            {
                errors.Add($"Required companion .shp file not found: {Path.GetFileName(correspondingShp)}");
            }
        }
    }


    // 3) EPSG format (optional but if provided, must be numeric)
    if (!string.IsNullOrWhiteSpace(epsg) && !System.Text.RegularExpressions.Regex.IsMatch(epsg, @"^\d{3,6}$"))
    {
        errors.Add("EPSG must be numeric (e.g., 4326). Leave blank to default to 4326.");
    }

    // 4) ogr2ogr presence (use your autodetect if blank)
    if (string.IsNullOrWhiteSpace(_ogr2ogrPath) || !File.Exists(_ogr2ogrPath))
    {
        EnsureOgr2OgrSelected();
    }

    if (string.IsNullOrWhiteSpace(_ogr2ogrPath) || !File.Exists(_ogr2ogrPath))
    {
        errors.Add("ogr2ogr.exe not found. Use Browse… or install OSGeo4W.");
    }

    // Early exit if we already have errors
    if (errors.Count > 0)
    {
        foreach (var e in errors) LogMessage("✖ " + e);
        lblStatus.Text = "Please fix the highlighted issues.";
        lblStatus.ForeColor = System.Drawing.Color.Red;
        return false;
    }

    // 5) Quick SQL connectivity check (5s timeout)
    try
    {
        // 5) Quick SQL connectivity check (5s timeout)
        var (ok, msg) = DbConnectionHelper.TestConnection(
            server,
            tempDb,
            integratedSecurity: true,
            encrypt: true,
            trustServerCertificate: true,
            timeoutSeconds: 5
        );
        if (!ok)
        {
            LogMessage("✖ " + msg);
            lblStatus.Text = "SQL connection failed.";
            lblStatus.ForeColor = System.Drawing.Color.Red;
            return false;
        }
        LogMessage("✓ " + msg.Split('\n')[0]); // brief line

    }
    catch (Exception ex)
    {
        LogMessage("✖ SQL connection failed: " + ex.Message);
        lblStatus.Text = "SQL connection failed.";
        lblStatus.ForeColor = System.Drawing.Color.Red;
        return false;
    }

    // 6) If user selected OSGeo4W, sanity-set per-process env (in case PATH not set globally)
    if (_gdalMode == GdalMode.Osgeo4w && File.Exists(_ogr2ogrPath))
    {
        // BuildOgr2OgrPsi() will set PATH/GDAL_DATA/PROJ_LIB when launching,
        // but we log a helpful note here for visibility.
        LogMessage(
            "Using OSGeo4W ogr2ogr; data dirs will be resolved from OSGeo4W’s share\\gdal and share\\proj at launch.");
    }

    return true;
}        
        private void EnsureTenantBoundaryColumn()
        {
            using var conn = new SqlConnection($"Server={txtServerName.Text};Database={txtTargetDatabase.Text};" +
                                               $"Integrated Security=SSPI;Encrypt=True;TrustServerCertificate=True;");
            conn.Open();
            var sql = $@"
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('{txtTargetDatabase.Text}.dbo.tenant')
      AND name = 'tenant_boundary')
BEGIN
    ALTER TABLE dbo.tenant ADD tenant_boundary geography NULL;
END";
            new SqlCommand(sql, conn).ExecuteNonQuery();
        }

        
        private void EnsureTempDatabase()
        {
            using var conn = new SqlConnection($"Server={txtServerName.Text};Database={txtTargetDatabase.Text};" +
                                               $"Integrated Security=SSPI;Encrypt=True;TrustServerCertificate=True;");
            conn.Open();
            var dbName = txtTempDatabase.Text;
            var sql = $@"
IF DB_ID('{dbName}') IS NULL
BEGIN
    DECLARE @sql nvarchar(max) = 'CREATE DATABASE [{dbName}]';
    EXEC(@sql);
END";
            new SqlCommand(sql, conn).ExecuteNonQuery();
        }

        
        private void LogMessage(string message)
        {
            txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\r\n");
            txtLog.SelectionStart = txtLog.Text.Length;
            txtLog.ScrollToCaret();
        }

        private async Task HandleTestRequestedAsync(TestRequestedEventArgs args)
        {
            try
            {
                if (!ValidateInputs())
                {
                    testGeoPanel?.SetResult("Please fill required inputs (Server, Tenant ID) first.", isError: true);
                    return;
                }

                LogMessage($"\n=== Testing Geography ===");
                lblStatus.Text = "Testing boundary...";
                lblStatus.ForeColor = Color.Blue;

                using (var conn = new SqlConnection(
                           $"Server={txtServerName.Text};Database={txtTargetDatabase.Text};" +
                           $"Integrated Security=SSPI;Encrypt=True;TrustServerCertificate=True;"))
                {
                    await conn.OpenAsync();

                    // Test inside
                    bool insideResult = await TestPointInBoundary(conn, args.Inside.lat, args.Inside.lon);
                    LogMessage($"INSIDE point {args.Inside.lat},{args.Inside.lon}: {(insideResult ? "✓ Inside (CORRECT)" : "✗ Outside (INCORRECT)")}");

                    // Test outside
                    bool outsideResult = await TestPointInBoundary(conn, args.Outside.lat, args.Outside.lon);
                    LogMessage($"OUTSIDE point {args.Outside.lat},{args.Outside.lon}: {(outsideResult ? "✗ Inside (INCORRECT)" : "✓ Outside (CORRECT)")}");

                    string panelSummary;
                    if (insideResult && !outsideResult)
                    {
                        panelSummary = "Boundary test PASSED";
                        lblStatus.Text = panelSummary;
                        lblStatus.ForeColor = Color.Green;
                        testGeoPanel?.SetResult(panelSummary);
                        testGeoPanel?.MarkLastHistorySuccess(true);
                    }
                    else if (!insideResult && outsideResult)
                    {
                        panelSummary = "Boundary INVERTED - try ReorientObject()";
                        lblStatus.Text = panelSummary;
                        lblStatus.ForeColor = Color.Orange;
                        cboFixOption.SelectedIndex = 2;
                        testGeoPanel?.SetResult(panelSummary, isError: true);
                    }
                    else
                    {
                        panelSummary = "Boundary test FAILED - check data/coordinates";
                        lblStatus.Text = panelSummary;
                        lblStatus.ForeColor = Color.Red;
                        testGeoPanel?.SetResult(panelSummary, isError: true);
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error testing geography: {ex.Message}");
                lblStatus.Text = "Test error";
                lblStatus.ForeColor = Color.Red;
                testGeoPanel?.SetResult($"Error: {ex.Message}", isError: true);
            }
        }
    }
}