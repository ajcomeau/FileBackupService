namespace FileBackupService
{
    partial class BackupService
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.backupTimer = new System.Timers.Timer();
            this.fswModel = new System.IO.FileSystemWatcher();
            ((System.ComponentModel.ISupportInitialize)(this.backupTimer)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.fswModel)).BeginInit();
            // 
            // backupTimer
            // 
            this.backupTimer.Enabled = true;
            this.backupTimer.Interval = 5000D;
            this.backupTimer.Elapsed += new System.Timers.ElapsedEventHandler(this.backupTimer_Elapsed);
            // 
            // fswModel
            // 
            this.fswModel.EnableRaisingEvents = true;
            this.fswModel.Changed += new System.IO.FileSystemEventHandler(this.fswModel_Changed);
            this.fswModel.Created += new System.IO.FileSystemEventHandler(this.fswModel_Created);
            this.fswModel.Deleted += new System.IO.FileSystemEventHandler(this.fswModel_Deleted);
            this.fswModel.Renamed += new System.IO.RenamedEventHandler(this.fswModel_Renamed);
            // 
            // BackupService
            // 
            this.ServiceName = "Service1";
            ((System.ComponentModel.ISupportInitialize)(this.backupTimer)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.fswModel)).EndInit();

        }

        #endregion

        private System.Timers.Timer backupTimer;
        private System.IO.FileSystemWatcher fswModel;
    }
}
