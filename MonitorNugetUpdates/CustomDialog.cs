using Microsoft.VisualStudio.Shell;
using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell.Interop;
using EnvDTE;
using System.Collections.Generic;

namespace MonitorNugetUpdates
{
    public class CustomDialog : Form
    {
        private readonly Label _messageLabel;
        private readonly Label _instructionLabel;

        public CustomDialog(string title, List<String> packagesList, List<String> latestVersionList)
        {
            Text = "Updates are available!";

            ListView listView = new ListView();
            listView.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            listView.View = View.Details;
            listView.Dock = DockStyle.Fill;
            ControlBox = true;
            MinimizeBox = false;
            MaximizeBox = false;
            
            
            //listView.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);

            listView.Columns.Add("Package", -2, HorizontalAlignment.Left);
            listView.Columns.Add("Latest Version", -2, HorizontalAlignment.Left);
            
            
            for (int i = 0; i < packagesList.Count; i++)
            {
                var package = new ListViewItem(new[] { packagesList[i], latestVersionList[i] });
                listView.Items.Add(package);
            }
            // Set the properties of the notification bubble
            this.BackColor = Color.White;
            this.TransparencyKey = Color.WhiteSmoke;
            this.StartPosition = FormStartPosition.Manual;
            this.TopMost = true;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            //this.AutoSize = true;


            //for (int i = 0; i <= listView.Columns.Count - 1; i++)
            //{
            //    listView.Columns[i].Width = -2;
            //}

            int maxFirstColumnWidth = 0;
            foreach (ListViewItem item in listView.Items)
            {
                int itemWidth = (int)CreateGraphics().MeasureString(item.SubItems[0].Text, listView.Font).Width;
                if (itemWidth > maxFirstColumnWidth)
                {
                    maxFirstColumnWidth = itemWidth;
                }
            }


            int maxSecondColumnWidth = 0;
            foreach (ListViewItem item in listView.Items)
            {
                int itemWidth = (int)CreateGraphics().MeasureString(item.SubItems[1].Text, listView.Font).Width;
                if (itemWidth > maxSecondColumnWidth)
                {
                    maxSecondColumnWidth = itemWidth;
                }
            }

            // Set the width of the second column to the maximum width
            listView.Columns[1].Width = maxSecondColumnWidth;

            //AutoSize = false;

            //// Set the form width to accommodate the ListView control
            //int width = listView.Columns[0].Width + listView.Columns[1].Width;
            //ClientSize = new Size(width, listView.Height);

            listView.Columns[1].AutoResize(ColumnHeaderAutoResizeStyle.HeaderSize);
            //listView.Columns[1].AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
            // Add the ListView control to the form
            Controls.Add(listView);
            //listView.Columns[1].Width = -1;
            // Auto-size the columns based on their content
            

            Label instructionLabel = new Label();
            instructionLabel.Text = "To install these packages, right click on your solution and go to manage nuget packages";
            instructionLabel.Dock = DockStyle.Bottom;
            instructionLabel.Padding = new Padding(0, 10, 0, 10);
            instructionLabel.Size = new Size(listView.ClientSize.Width, instructionLabel.PreferredHeight);
            instructionLabel.Height = 70;
            this.Controls.Add(instructionLabel);
            

            ClientSize = new Size(maxFirstColumnWidth + maxSecondColumnWidth + 45 + SystemInformation.VerticalScrollBarWidth, listView.Height + 50);
        }

        public static void ShowDialog(string title, List<String> packagesList, List<String> latestVersionList)
        {
            IntPtr ownerHandle = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;

// Set the owner of the dialog using the IWin32Window interface
            using (var dialog = new CustomDialog(title, packagesList, latestVersionList))
            {
                // Center the dialog on the bottom of the screen
                var workingArea = Screen.GetWorkingArea(dialog);

                // Set the position of the dialog
                dialog.StartPosition = FormStartPosition.Manual;
                dialog.Left = workingArea.Left;
                dialog.Top = workingArea.Bottom - dialog.Height- 30;

                dialog.ShowDialog(new Win32Window(ownerHandle));

            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            //ControlPaint.DrawBorder(e.Graphics, ClientRectangle, Color.Black, ButtonBorderStyle.Solid);
        }
    }

    public class Win32Window : System.Windows.Forms.IWin32Window
    {
        public IntPtr Handle { get; }

        public Win32Window(IntPtr handle)
        {
            Handle = handle;
        }
    }

}
