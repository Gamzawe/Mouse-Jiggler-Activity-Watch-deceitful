using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;

namespace LogiOptions
{
    public class ConfigurationForm : Form
    {
        private TrackBar _speedSlider;
        private CheckBox _enableCheckbox;
        private Button _saveButton;
        private Button _aboutButton;
        private TextBox _projectNameBox;
        private NumericUpDown _timeoutDown;

        public ConfigurationForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Logitech Options Configuration Utility";
            this.Size = new Size(450, 350);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;

            var tabs = new TabControl()
            {
                Dock = DockStyle.Top,
                Height = 220
            };

            // ---- General Settings Tab ----
            var tabGeneral = new TabPage("General Settings");
            
            var speedLabel = new Label() { Text = "Test execution speed:", Location = new Point(20, 30), AutoSize = true };
            _speedSlider = new TrackBar() { Location = new Point(140, 20), Width = 200, Minimum = 1, Maximum = 10, Value = 5, TickStyle = TickStyle.None };
            _enableCheckbox = new CheckBox() { Text = "Enable automated test execution", Location = new Point(20, 70), AutoSize = true, Checked = true };
            
            var infoLabel = new Label()
            {
                Text = "Note: This utility performs one-shot test cycles on demand. Background monitoring is disabled.",
                Location = new Point(20, 110), Width = 380, Height = 40, ForeColor = Color.DarkSlateGray, Font = new Font("Segoe UI", 8, FontStyle.Italic)
            };

            tabGeneral.Controls.Add(speedLabel);
            tabGeneral.Controls.Add(_speedSlider);
            tabGeneral.Controls.Add(_enableCheckbox);
            tabGeneral.Controls.Add(infoLabel);

            // ---- Test Setup Tab ----
            var tabSetup = new TabPage("Test Setup");
            
            var projectLabel = new Label() { Text = "Project name:", Location = new Point(20, 30), AutoSize = true };
            _projectNameBox = new TextBox() { Text = "QA Validation", Location = new Point(140, 27), Width = 200 };
            
            var timeoutLabel = new Label() { Text = "Test timeout (sec):", Location = new Point(20, 70), AutoSize = true };
            _timeoutDown = new NumericUpDown() { Value = 30, Location = new Point(140, 68), Width = 60 };

            var verboseLabel = new Label()
            {
                Text = "Configuration for on-demand QA validation cycles.",
                Location = new Point(20, 110), Width = 380, Height = 40, ForeColor = Color.DarkSlateGray, Font = new Font("Segoe UI", 8, FontStyle.Italic)
            };

            tabSetup.Controls.Add(projectLabel);
            tabSetup.Controls.Add(_projectNameBox);
            tabSetup.Controls.Add(timeoutLabel);
            tabSetup.Controls.Add(_timeoutDown);
            tabSetup.Controls.Add(verboseLabel);

            tabs.TabPages.Add(tabGeneral);
            tabs.TabPages.Add(tabSetup);

            _saveButton = new Button() { Text = "Save", Location = new Point(330, 250), Size = new Size(80, 30) };
            _saveButton.Click += OnSaveClick;

            _aboutButton = new Button() { Text = "About", Location = new Point(20, 250), Size = new Size(80, 30) };
            _aboutButton.Click += OnAboutClick;

            this.Controls.Add(tabs);
            this.Controls.Add(_saveButton);
            this.Controls.Add(_aboutButton);

            this.Load += OnFormLoad;
        }

        private void OnAboutClick(object sender, EventArgs e)
        {
            string aboutText = "Logitech Options QA Utility v10.5.2\n\n" +
                               "Executes single-cycle UI test scenarios with hardware-level timing.\n" +
                               "Optimization for on-demand validation (no background services).\n\n" +
                               "Support: http://support.logitech.com/qa-automation";
            
            MessageBox.Show(aboutText, "About Logitech Options", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void OnFormLoad(object sender, EventArgs e)
        {
            try
            {
                using (var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Logitech\Options"))
                {
                    key.SetValue("TelemetryAsked", 1, RegistryValueKind.DWord);
                }
            }
            catch { }
        }

        private void OnSaveClick(object sender, EventArgs e)
        {
            try
            {
                using (var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Logitech\Options\Settings"))
                {
                    key.SetValue("Enabled", _enableCheckbox.Checked ? 1 : 0, RegistryValueKind.DWord);
                    key.SetValue("ProjectName", _projectNameBox.Text);
                    key.SetValue("TestTimeout", (int)_timeoutDown.Value, RegistryValueKind.DWord);
                }
                MessageBox.Show("Configuration updated successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
