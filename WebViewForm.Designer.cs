using System.Windows.Forms;

namespace AppleNotesPortable
{
    partial class WebViewForm
    {
        private Microsoft.Web.WebView2.WinForms.WebView2 webView21;
        private Panel contentHost;

        private void InitializeComponent()
        {
            this.webView21 = new Microsoft.Web.WebView2.WinForms.WebView2();
            this.contentHost = new System.Windows.Forms.Panel();
            ((System.ComponentModel.ISupportInitialize)(this.webView21)).BeginInit();
            this.contentHost.SuspendLayout();
            this.SuspendLayout();
            // 
            // contentHost  (fills the window and CLIPS the web view, so iCloud's
            // top toolbar is pushed off the top edge: hidden and unclickable)
            // 
            this.contentHost.Dock = System.Windows.Forms.DockStyle.Fill;
            this.contentHost.Name = "contentHost";
            this.contentHost.Controls.Add(this.webView21);
            // 
            // webView21  (real bounds are set in WebViewForm_Load from the .ini
            // ClipHeight; anchored so the clip survives a window resize)
            // 
            this.webView21.Anchor = ((System.Windows.Forms.AnchorStyles)(
                System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Bottom
                | System.Windows.Forms.AnchorStyles.Left
                | System.Windows.Forms.AnchorStyles.Right));
            this.webView21.Location = new System.Drawing.Point(0, 0);
            this.webView21.Name = "webView21";
            this.webView21.Size = new System.Drawing.Size(450, 700);
            this.webView21.TabIndex = 0;
            this.webView21.ZoomFactor = 1D;
            // 
            // WebViewForm
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(450, 700);
            this.Controls.Add(this.contentHost);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            // Load the window icon from an embedded resource (see .csproj).
            string resourceName = "AppleNotesPortable.Resources.Apple_Notes_icon.ico";
            System.Reflection.Assembly executingAssembly = System.Reflection.Assembly.GetExecutingAssembly();
            using (System.IO.Stream iconStream = executingAssembly.GetManifestResourceStream(resourceName))
            {
                if (iconStream != null)
                {
                    this.Icon = new System.Drawing.Icon(iconStream);
                }
            }

            this.Name = "WebViewForm";
            this.Text = "Apple Notes";
            this.Load += new System.EventHandler(this.WebViewForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.webView21)).EndInit();
            this.contentHost.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
