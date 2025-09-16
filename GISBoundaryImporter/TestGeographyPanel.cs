using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace GISBoundaryImporter
{
    public class TestGeographyPanel : UserControl
    {
        private TextBox txtInsideLat;
        private TextBox txtInsideLon;
        private TextBox txtOutsideLat;
        private TextBox txtOutsideLon;
        private TextBox txtInsidePair;
        private TextBox txtOutsidePair;
        private Button btnPasteInsidePair;
        private Button btnPasteOutsidePair;
        private CheckBox chkSingleField;
        private Button btnRunTest;
        private Button btnHide;
        private ListBox lstHistory;
        private Button btnUseSelected;
        private Label lblHeader;
        private Label lblResult;

        // Boundary drawing
        private PictureBox picBoundary;
        private List<List<PointF>> _polygons = new List<List<PointF>>();
        private string? _noBoundaryMessage = "No boundary loaded.";

        public event EventHandler<TestRequestedEventArgs>? TestRequested;
        public event EventHandler? HideRequested;

        public TestGeographyPanel()
        {
            BuildUi();
        }

        private void BuildUi()
        {
            this.Dock = DockStyle.Fill;

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 20,
                Padding = new Padding(8),
                AutoSize = true
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            lblHeader = new Label
            {
                Text = "Test Geography",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                AutoSize = true,
                ForeColor = Color.DarkBlue,
                Margin = new Padding(0, 0, 0, 6)
            };
            layout.Controls.Add(lblHeader, 0, 0);
            layout.SetColumnSpan(lblHeader, 2);

            // Inside
            layout.Controls.Add(new Label { Text = "Inside Latitude:", AutoSize = true }, 0, 1);
            txtInsideLat = new TextBox { Width = 120, Text = "40.7580" };
            layout.Controls.Add(txtInsideLat, 1, 1);

            layout.Controls.Add(new Label { Text = "Inside Longitude:", AutoSize = true }, 0, 2);
            txtInsideLon = new TextBox { Width = 120, Text = "-73.9855" };
            layout.Controls.Add(txtInsideLon, 1, 2);

            // Outside
            layout.Controls.Add(new Label { Text = "Outside Latitude:", AutoSize = true }, 0, 3);
            txtOutsideLat = new TextBox { Width = 120, Text = "40.6892" };
            layout.Controls.Add(txtOutsideLat, 1, 3);

            layout.Controls.Add(new Label { Text = "Outside Longitude:", AutoSize = true }, 0, 4);
            txtOutsideLon = new TextBox { Width = 120, Text = "-74.0445" };
            layout.Controls.Add(txtOutsideLon, 1, 4);

            btnRunTest = new Button { Text = "Run Test", AutoSize = true, Margin = new Padding(0, 6, 6, 6) };
            btnRunTest.Click += (s, e) => OnRunTest();
            layout.Controls.Add(btnRunTest, 1, 5);

            // Option: single-field lat,lon inputs
            chkSingleField = new CheckBox { Text = "Use single input (lat,lon)", AutoSize = true };
            chkSingleField.CheckedChanged += (s, e) => ToggleInputMode();
            layout.Controls.Add(chkSingleField, 0, 6);
            layout.SetColumnSpan(chkSingleField, 2);

            // Inside/Outside combined inputs
            layout.Controls.Add(new Label { Text = "Inside (lat,lon):", AutoSize = true }, 0, 7);
            txtInsidePair = new TextBox { Width = 180, PlaceholderText = "40.7580,-73.9855" };
            btnPasteInsidePair = new Button { Text = "Paste", AutoSize = true, Margin = new Padding(4, 0, 0, 0) };
            btnPasteInsidePair.Click += (s, e) =>
            {
                try { txtInsidePair.Text = (Clipboard.GetText() ?? string.Empty).Trim(); }
                catch { /* ignore clipboard exceptions */ }
            };
            var insidePairPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Margin = new Padding(0) };
            insidePairPanel.Controls.Add(txtInsidePair);
            insidePairPanel.Controls.Add(btnPasteInsidePair);
            layout.Controls.Add(insidePairPanel, 1, 7);

            layout.Controls.Add(new Label { Text = "Outside (lat,lon):", AutoSize = true }, 0, 8);
            txtOutsidePair = new TextBox { Width = 180, PlaceholderText = "40.6892,-74.0445" };
            btnPasteOutsidePair = new Button { Text = "Paste", AutoSize = true, Margin = new Padding(4, 0, 0, 0) };
            btnPasteOutsidePair.Click += (s, e) =>
            {
                try { txtOutsidePair.Text = (Clipboard.GetText() ?? string.Empty).Trim(); }
                catch { /* ignore clipboard exceptions */ }
            };
            var outsidePairPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Margin = new Padding(0) };
            outsidePairPanel.Controls.Add(txtOutsidePair);
            outsidePairPanel.Controls.Add(btnPasteOutsidePair);
            layout.Controls.Add(outsidePairPanel, 1, 8);

            // Hide button
            btnHide = new Button { Text = "Hide Panel", AutoSize = true };
            btnHide.Click += (s, e) => HideRequested?.Invoke(this, EventArgs.Empty);
            layout.Controls.Add(btnHide, 0, 9);

            lblResult = new Label { AutoSize = true, ForeColor = Color.Black, Margin = new Padding(0, 8, 0, 8) };
            layout.Controls.Add(lblResult, 0, 10);
            layout.SetColumnSpan(lblResult, 2);

            layout.Controls.Add(new Label { Text = "History:", AutoSize = true, Margin = new Padding(0, 6, 0, 0) }, 0, 11);
            lstHistory = new ListBox { Dock = DockStyle.Fill, Height = 150 };
            // Enable owner-draw to allow coloring successful items
            lstHistory.DrawMode = DrawMode.OwnerDrawFixed;
            lstHistory.DrawItem += LstHistory_DrawItem;
            layout.Controls.Add(lstHistory, 0, 12);
            layout.SetColumnSpan(lstHistory, 2);

            btnUseSelected = new Button { Text = "Use Selected", AutoSize = true };
            btnUseSelected.Click += (s, e) => UseSelectedHistory();
            layout.Controls.Add(btnUseSelected, 1, 13);

            // Boundary preview
            layout.Controls.Add(new Label { Text = "Boundary Preview:", AutoSize = true, Margin = new Padding(0, 6, 0, 0) }, 0, 14);
            picBoundary = new PictureBox { Dock = DockStyle.Fill, Height = 180, BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            picBoundary.Paint += PicBoundary_Paint;
            layout.Controls.Add(picBoundary, 0, 15);
            layout.SetColumnSpan(picBoundary, 2);

            this.Controls.Add(layout);

            // Initialize input mode (default: separate fields enabled)
            chkSingleField.Checked = false;
            ToggleInputMode();
        }

        private void PicBoundary_Paint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            var rect = picBoundary.ClientRectangle;
            g.Clear(picBoundary.BackColor);

            if (_polygons == null || _polygons.Count == 0)
            {
                using var br = new SolidBrush(Color.Gray);
                var text = _noBoundaryMessage ?? "No boundary loaded.";
                g.DrawString(text, this.Font, br, new PointF(6, 6));
                return;
            }

            // Compute bounds
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            foreach (var poly in _polygons)
            {
                foreach (var p in poly)
                {
                    if (p.X < minX) minX = p.X;
                    if (p.X > maxX) maxX = p.X;
                    if (p.Y < minY) minY = p.Y;
                    if (p.Y > maxY) maxY = p.Y;
                }
            }

            float pad = 10f;
            float availW = Math.Max(1f, rect.Width - 2 * pad);
            float availH = Math.Max(1f, rect.Height - 2 * pad);
            float srcW = Math.Max(1e-6f, maxX - minX);
            float srcH = Math.Max(1e-6f, maxY - minY);
            float scale = Math.Min(availW / srcW, availH / srcH);

            // Map function (lon->X, lat->Y with Y inverted)
            PointF Map(PointF pt)
            {
                float x = pad + (pt.X - minX) * scale;
                float y = pad + (maxY - pt.Y) * scale; // invert Y to have north up
                return new PointF(x, y);
            }

            using var pen = new Pen(Color.RoyalBlue, 2f);
            using var fill = new SolidBrush(Color.FromArgb(40, Color.RoyalBlue));

            foreach (var poly in _polygons)
            {
                if (poly.Count >= 3)
                {
                    var pts = poly.Select(Map).ToArray();
                    g.FillPolygon(fill, pts);
                    g.DrawPolygon(pen, pts);
                }
            }
        }

        public void SetBoundaryWkt(string? wkt)
        {
            _polygons.Clear();
            if (string.IsNullOrWhiteSpace(wkt))
            {
                _noBoundaryMessage = "No boundary found for this tenant.";
                picBoundary?.Invalidate();
                return;
            }

            // Normalize common variants like "SRID=4326;MULTIPOLYGON(...)" and handle EMPTY
            wkt = wkt.Trim();
            if (wkt.StartsWith("SRID=", StringComparison.OrdinalIgnoreCase))
            {
                int idx = wkt.IndexOf(';');
                if (idx >= 0 && idx + 1 < wkt.Length)
                    wkt = wkt.Substring(idx + 1).Trim();
            }
            if (wkt.EndsWith("EMPTY", StringComparison.OrdinalIgnoreCase))
            {
                _noBoundaryMessage = "Boundary is EMPTY.";
                picBoundary?.Invalidate();
                return;
            }

            try
            {
                var parsed = ParseWktPolygons(wkt);
                if (parsed.Count == 0)
                {
                    _noBoundaryMessage = "Boundary WKT parsed to 0 polygons.";
                }
                _polygons = parsed;
            }
            catch (Exception ex)
            {
                _noBoundaryMessage = "Failed to parse WKT: " + ex.Message;
            }

            picBoundary?.Invalidate();
        }

        private static List<List<PointF>> ParseWktPolygons(string wkt)
        {
            // Basic support for POLYGON and MULTIPOLYGON. We parse only outer rings.
            wkt = wkt.Trim();
            var result = new List<List<PointF>>();

            if (wkt.StartsWith("POLYGON", StringComparison.OrdinalIgnoreCase))
            {
                var rings = ExtractRings(wkt);
                if (rings.Count > 0)
                    result.Add(ParseRing(rings[0]));
            }
            else if (wkt.StartsWith("MULTIPOLYGON", StringComparison.OrdinalIgnoreCase))
            {
                var polys = ExtractPolygonsFromMulti(wkt);
                foreach (var polyText in polys)
                {
                    var rings = ExtractRings("POLYGON " + polyText);
                    if (rings.Count > 0)
                        result.Add(ParseRing(rings[0]));
                }
            }
            else if (wkt.StartsWith("GEOMETRYCOLLECTION", StringComparison.OrdinalIgnoreCase))
            {
                // naive: try to find contained POLYGONs
                var matches = Regex.Matches(wkt, "POLYGON\\s*\\(\\((?:[^()]*|\\([^()]*\\))*\\)\\)", RegexOptions.IgnoreCase);
                foreach (Match m in matches)
                {
                    var rings = ExtractRings(m.Value);
                    if (rings.Count > 0)
                        result.Add(ParseRing(rings[0]));
                }
            }

            return result;
        }

        private static List<string> ExtractRings(string polygonWkt)
        {
            // expects input starting with POLYGON and containing ((outer),(hole1),...))
            int start = polygonWkt.IndexOf("((", StringComparison.Ordinal);
            int end = polygonWkt.LastIndexOf("))", StringComparison.Ordinal);
            if (start < 0 || end <= start) return new List<string>();
            string inner = polygonWkt.Substring(start + 2, end - (start + 2)); // content between the double parentheses

            // Split rings by "),("
            var rings = inner.Split(new[] { "),(" }, StringSplitOptions.None)
                             .Select(r => r.Trim(' ', '(', ')'))
                             .Where(r => r.Length > 0)
                             .ToList();
            return rings;
        }

        private static List<string> ExtractPolygonsFromMulti(string multiWkt)
        {
            // input starts with MULTIPOLYGON
            int kw = multiWkt.IndexOf("MULTIPOLYGON", StringComparison.OrdinalIgnoreCase);
            int start = multiWkt.IndexOf('(', kw >= 0 ? kw : 0);
            int end = multiWkt.LastIndexOf(')');
            if (start < 0 || end <= start) return new List<string>();
            string inner = multiWkt.Substring(start + 1, end - start - 1).Trim(); // content inside MULTIPOLYGON(...)

            var polys = new List<string>();
            int depth = 0;
            int segStart = 0;
            for (int i = 0; i < inner.Length; i++)
            {
                char c = inner[i];
                if (c == '(') depth++;
                else if (c == ')') depth--;
                else if (c == ',' && depth == 0)
                {
                    // split between polygons
                    string seg = inner.Substring(segStart, i - segStart).Trim();
                    if (seg.Length > 0) polys.Add(seg);
                    segStart = i + 1;
                }
            }
            // add last segment
            string last = inner.Substring(segStart).Trim();
            if (last.Length > 0) polys.Add(last);

            // Ensure each returned segment remains in ((...)) form
            return polys;
        }

        private static List<PointF> ParseRing(string ringText)
        {
            // Coordinates are typically in "lon lat" order for WKT from SQL Server geography
            var pts = new List<PointF>();
            var parts = ringText.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                var coords = part.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (coords.Length >= 2 &&
                    float.TryParse(coords[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float lon) &&
                    float.TryParse(coords[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float lat))
                {
                    pts.Add(new PointF(lon, lat));
                }
            }
            return pts;
        }

        private void ToggleInputMode()
        {
            bool single = chkSingleField.Checked;

            // When using single input, disable double inputs
            txtInsideLat.Enabled = !single;
            txtInsideLon.Enabled = !single;
            txtOutsideLat.Enabled = !single;
            txtOutsideLon.Enabled = !single;

            // Enable/disable single input and paste buttons accordingly
            txtInsidePair.Enabled = single;
            txtOutsidePair.Enabled = single;
            if (btnPasteInsidePair != null) btnPasteInsidePair.Enabled = single;
            if (btnPasteOutsidePair != null) btnPasteOutsidePair.Enabled = single;
        }

        private void OnRunTest()
        {
            if (!TryGetInputs(out var inside, out var outside, out string? error))
            {
                SetResult($"Invalid input: {error}", isError: true);
                return;
            }

            // Raise event for host form to execute DB logic
            TestRequested?.Invoke(this, new TestRequestedEventArgs(inside.Value, outside.Value));

            // Add to history as a simple string representation
            var item = new CoordinatePair(inside.Value, outside.Value);
            lstHistory.Items.Insert(0, item);
        }

        private void UseSelectedHistory()
        {
            if (lstHistory.SelectedItem is CoordinatePair cp)
            {
                txtInsideLat.Text = cp.Inside.lat.ToString();
                txtInsideLon.Text = cp.Inside.lon.ToString();
                txtOutsideLat.Text = cp.Outside.lat.ToString();
                txtOutsideLon.Text = cp.Outside.lon.ToString();

                // Also update single-input pair boxes
                if (txtInsidePair != null) txtInsidePair.Text = $"{cp.Inside.lat},{cp.Inside.lon}";
                if (txtOutsidePair != null) txtOutsidePair.Text = $"{cp.Outside.lat},{cp.Outside.lon}";
            }
        }

        // Mark the most recently added history item as success/failure
        public void MarkLastHistorySuccess(bool success)
        {
            if (lstHistory.Items.Count > 0 && lstHistory.Items[0] is CoordinatePair cp)
            {
                cp.Success = success;
                // Refresh to redraw colors
                lstHistory.Invalidate();
            }
        }

        // Owner-draw to color successful items green
        private void LstHistory_DrawItem(object? sender, DrawItemEventArgs e)
        {
            e.DrawBackground();
            if (e.Index >= 0 && e.Index < lstHistory.Items.Count)
            {
                var item = lstHistory.Items[e.Index];
                string text = item?.ToString() ?? string.Empty;
                Color foreColor = e.ForeColor;

                if (item is CoordinatePair cp && cp.Success == true)
                {
                    foreColor = Color.Green;
                }

                using (var br = new SolidBrush(foreColor))
                {
                    e.Graphics.DrawString(text, e.Font, br, e.Bounds);
                }
            }
            e.DrawFocusRectangle();
        }

        private bool TryGetInputs(out (double lat, double lon)? inside, out (double lat, double lon)? outside, out string? error)
        {
            inside = null; outside = null; error = null;

            // If the user chose single-field mode and provided values, try parsing those first
            if (chkSingleField != null && chkSingleField.Checked && (!string.IsNullOrWhiteSpace(txtInsidePair?.Text) || !string.IsNullOrWhiteSpace(txtOutsidePair?.Text)))
            {
                var inPairOk = TryParsePair(txtInsidePair?.Text, out var inPair, out var inErr);
                var outPairOk = TryParsePair(txtOutsidePair?.Text, out var outPair, out var outErr);

                if (!inPairOk || !outPairOk)
                {
                    error = string.Join("; ", new[] { inErr, outErr }.Where(s => !string.IsNullOrEmpty(s)));
                    return false;
                }

                inside = inPair;
                outside = outPair;
                return true;
            }

            // Fallback to separate fields
            bool ok = true;
            string? err = null;

            if (!double.TryParse(txtInsideLat.Text.Trim(), out var inLat) || inLat < -90 || inLat > 90)
            { err = "Inside latitude must be a number between -90 and 90"; ok = false; }
            if (!double.TryParse(txtInsideLon.Text.Trim(), out var inLon) || inLon < -180 || inLon > 180)
            { err = err == null ? "Inside longitude must be between -180 and 180" : err + "; Inside longitude must be between -180 and 180"; ok = false; }

            if (!double.TryParse(txtOutsideLat.Text.Trim(), out var outLat) || outLat < -90 || outLat > 90)
            { err = err == null ? "Outside latitude must be between -90 and 90" : err + "; Outside latitude must be between -90 and 90"; ok = false; }
            if (!double.TryParse(txtOutsideLon.Text.Trim(), out var outLon) || outLon < -180 || outLon > 180)
            { err = err == null ? "Outside longitude must be between -180 and 180" : err + "; Outside longitude must be between -180 and 180"; ok = false; }

            if (ok)
            {
                inside = (inLat, inLon);
                outside = (outLat, outLon);
                return true;
            }
            else
            {
                error = err;
                return false;
            }
        }

        private static bool TryParsePair(string? text, out (double lat, double lon) pair, out string? error)
        {
            pair = default;
            error = null;
            if (string.IsNullOrWhiteSpace(text))
            {
                error = "Coordinate pair is required (format: lat,lon)";
                return false;
            }

            var parts = text.Split(',');
            if (parts.Length != 2)
            {
                error = "Invalid format. Use: latitude,longitude";
                return false;
            }

            if (!double.TryParse(parts[0].Trim(), out var lat) || lat < -90 || lat > 90)
            {
                error = "Latitude must be a number between -90 and 90";
                return false;
            }
            if (!double.TryParse(parts[1].Trim(), out var lon) || lon < -180 || lon > 180)
            {
                error = "Longitude must be a number between -180 and 180";
                return false;
            }

            pair = (lat, lon);
            return true;
        }

        public void SetResult(string text, bool isError = false)
        {
            lblResult.ForeColor = isError ? Color.Red : Color.DarkGreen;
            lblResult.Text = text;
        }

        public record CoordinatePair((double lat, double lon) Inside, (double lat, double lon) Outside)
        {
            public bool? Success { get; set; }
            public override string ToString()
            {
                return $"Inside: {Inside.lat},{Inside.lon} | Outside: {Outside.lat},{Outside.lon}";
            }
        }
    }

    public class TestRequestedEventArgs : EventArgs
    {
        public (double lat, double lon) Inside { get; }
        public (double lat, double lon) Outside { get; }
        public TestRequestedEventArgs((double lat, double lon) inside, (double lat, double lon) outside)
        {
            Inside = inside;
            Outside = outside;
        }
    }
}
