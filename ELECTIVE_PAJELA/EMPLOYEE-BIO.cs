using libzkfpcsharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Threading;

namespace ELECTIVE_PAJELA
{
    public partial class EMPLOYEE_BIO : Form
    {
        // ── Fingerprint SDK State ──
        private IntPtr mDevHandle = IntPtr.Zero;
        private IntPtr mDBHandle = IntPtr.Zero;
        private bool _deviceOpen;
        private bool _scanInProgress;

        private int _imgWidth;
        private int _imgHeight;
        private byte[] _imgBuffer;

        // Maps SDK fid to Database EmployeeID
        private Dictionary<int, string> _fidToEmployeeId = new Dictionary<int, string>();

        // Timer for clearing the UI after 5 seconds
        private System.Windows.Forms.Timer _uiClearTimer;
        private string connectionString = @"Data Source=LAPTOP-RF7MTOVT\SQLEXPRESS;Initial Catalog=EMPLOYEEE_REGDB;Integrated Security=True;TrustServerCertificate=True";

        public EMPLOYEE_BIO()
        {
            InitializeComponent();

            // UI initialization setup
            txtEmployeeID.Enabled = false;
            nameTxtBox.Enabled = false;
            departmentTxtBox.Enabled = false;
            timeINlbl.Text = "--:--:--";
            timeOUTlbl.Text = "--:--:--";

            // Initialize UI Clear Timer (5000 milliseconds = 5 seconds)
            _uiClearTimer = new System.Windows.Forms.Timer();
            _uiClearTimer.Interval = 5000;
            _uiClearTimer.Tick += UiClearTimer_Tick;

            // Wire up form load and close to manage sensor lifecycle
            this.Load += Attendance_Load;
            this.FormClosing += Attendance_FormClosing;
        }

        private void Attendance_Load(object sender, EventArgs e)
        {
            // Auto-start scanner initialization when form loads
            if (OpenScanner())
            {
                LoadFingerprintsIntoMemory();
                StartContinuousScan();
            }
        }

        private void Attendance_FormClosing(object sender, FormClosingEventArgs e)
        {
            _uiClearTimer.Stop();
            _uiClearTimer.Dispose();
            CloseScanner();
        }

        private void StartUiClearTimer()
        {
            _uiClearTimer.Stop();  // Reset the 5s window
            _uiClearTimer.Start();
        }

        private void UiClearTimer_Tick(object sender, EventArgs e)
        {
            _uiClearTimer.Stop(); // Only run once per trigger

            // Safely clear UI elements on the UI Thread
            this.Invoke((MethodInvoker)delegate
            {
                txtEmployeeID.Clear();
                nameTxtBox.Clear();
                departmentTxtBox.Clear();
                timeINlbl.Text = "--:--:--";
                timeOUTlbl.Text = "--:--:--";

                var oldImg = employeePicBox.Image;
                employeePicBox.Image = null;
                oldImg?.Dispose();
            });
        }

        private bool OpenScanner()
        {
            if (_deviceOpen) return true;

            int ret = zkfp2.Init();
            if (ret != zkfp.ZKFP_ERR_OK)
            {
                MessageBox.Show("Initialize Engine Failed.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            mDevHandle = zkfp2.OpenDevice(0);
            if (mDevHandle == IntPtr.Zero)
            {
                zkfp2.Terminate();
                MessageBox.Show("Failed to open device.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            mDBHandle = zkfp2.DBInit();
            if (mDBHandle == IntPtr.Zero)
            {
                zkfp2.CloseDevice(mDevHandle);
                zkfp2.Terminate();
                mDevHandle = IntPtr.Zero;
                MessageBox.Show("Failed to initialize matching engine.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            // Allocate image parameters
            byte[] paramBuf = new byte[4];
            int paramLen = 4;
            zkfp2.GetParameters(mDevHandle, 1, paramBuf, ref paramLen);
            _imgWidth = BitConverter.ToInt32(paramBuf, 0);

            paramLen = 4;
            zkfp2.GetParameters(mDevHandle, 2, paramBuf, ref paramLen);
            _imgHeight = BitConverter.ToInt32(paramBuf, 0);

            if (_imgWidth <= 0 || _imgHeight <= 0)
            {
                CloseScanner();
                MessageBox.Show("Scanner dimensions invalid.", "Sensor Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            _imgBuffer = new byte[_imgWidth * _imgHeight];
            _deviceOpen = true;
            return true;
        }

        private void CloseScanner()
        {
            _scanInProgress = false;

            if (mDBHandle != IntPtr.Zero)
            {
                zkfp2.DBFree(mDBHandle);
                mDBHandle = IntPtr.Zero;
            }

            if (mDevHandle != IntPtr.Zero)
            {
                zkfp2.CloseDevice(mDevHandle);
                mDevHandle = IntPtr.Zero;
            }

            if (_deviceOpen)
            {
                zkfp2.Terminate();
                _deviceOpen = false;
            }
        }

        private void LoadFingerprintsIntoMemory()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    // ✅ FIX: Changed column name from 'FingerprintTemplate' to 'picpath'
                    //         which matches the actual column defined in your database schema.
                    string query = "SELECT EmployeeID, picpath FROM Employees WHERE picpath IS NOT NULL";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        int fid = 1;
                        _fidToEmployeeId.Clear();

                        while (reader.Read())
                        {
                            string empId = reader.GetString(0);

                            // ✅ FIX: Read from 'picpath' instead of 'FingerprintTemplate'
                            byte[] template = (byte[])reader["picpath"];

                            // Add template to ZKTeco in-memory matching DB
                            zkfp2.DBAdd(mDBHandle, fid, template);
                            _fidToEmployeeId[fid] = empId;
                            fid++;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load templates into sensor memory:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void StartContinuousScan()
        {
            if (!_deviceOpen || _imgBuffer == null) return;

            _scanInProgress = true;
            BackgroundWorker worker = new BackgroundWorker();

            worker.DoWork += (_, args) =>
            {
                byte[] templateBuffer = new byte[2048 * 10];

                while (_deviceOpen && _scanInProgress)
                {
                    int templateLength = templateBuffer.Length;
                    int captureResult = zkfp2.AcquireFingerprint(mDevHandle, _imgBuffer, templateBuffer, ref templateLength);

                    if (captureResult == zkfp.ZKFP_ERR_OK)
                    {
                        byte[] finalTemplate = new byte[templateLength];
                        Array.Copy(templateBuffer, finalTemplate, templateLength);
                        args.Result = finalTemplate;
                        return; // Got a fingerprint, exit loop
                    }

                    Thread.Sleep(200); // Polling delay
                }

                args.Result = null; // Loop exited by manual stop
            };

            worker.RunWorkerCompleted += (_, args) =>
            {
                if (!_scanInProgress) return;

                if (args.Error == null && args.Result is byte[] templateBuffer)
                {
                    MatchFingerprint(templateBuffer);
                }

                // Keep scanner alive: restart scan after short delay
                if (_scanInProgress && _deviceOpen)
                {
                    System.Threading.Tasks.Task.Delay(1500).ContinueWith(t =>
                    {
                        if (!this.IsDisposed && this.IsHandleCreated)
                        {
                            this.Invoke((MethodInvoker)delegate { StartContinuousScan(); });
                        }
                    });
                }
            };

            worker.RunWorkerAsync();
        }

        private void MatchFingerprint(byte[] incomingTemplate)
        {
            int matchedFid = 0, score = 0;
            int ret = zkfp2.DBIdentify(mDBHandle, incomingTemplate, ref matchedFid, ref score);

            if (ret == zkfp.ZKFP_ERR_OK && _fidToEmployeeId.TryGetValue(matchedFid, out string empId))
            {
                // ✅ FIX: ProcessAttendanceLogic updates UI labels, so it must run on the UI thread
                this.Invoke((MethodInvoker)delegate
                {
                    using (SqlConnection conn = new SqlConnection(connectionString))
                    {
                        conn.Open();
                        ProcessAttendanceLogic(empId, conn);
                    }
                });
            }
            else
            {
                this.Invoke((MethodInvoker)delegate
                {
                    Form autoCloseMsg = new Form()
                    {
                        Text = "Failed",
                        Size = new Size(300, 120),
                        FormBorderStyle = FormBorderStyle.FixedDialog,
                        StartPosition = FormStartPosition.CenterScreen,
                        MaximizeBox = false,
                        MinimizeBox = false
                    };

                    autoCloseMsg.Controls.Add(new Label()
                    {
                        Text = "Fingerprint not recognized.",
                        Dock = DockStyle.Fill,
                        TextAlign = ContentAlignment.MiddleCenter,
                        Font = new Font("Segoe UI", 10, FontStyle.Regular)
                    });

                    System.Windows.Forms.Timer closeTimer = new System.Windows.Forms.Timer() { Interval = 3000 };
                    closeTimer.Tick += (s, args) =>
                    {
                        closeTimer.Stop();
                        closeTimer.Dispose(); // ✅ FIX: Dispose timer to prevent memory leak
                        autoCloseMsg.Close();
                    };

                    closeTimer.Start();
                    autoCloseMsg.Show();
                });
            }
        }

        private void EMPLOYEE_BIO_Load(object sender, EventArgs e)
        {
            // Reserved for designer-generated load logic
        }

        private void ProcessAttendanceLogic(string empID, SqlConnection conn)
        {
            string check = @"SELECT TOP 1 LogID, TimeIn, TimeOut 
                             FROM Attendance 
                             WHERE EmployeeID = @id AND LogDate = CAST(GETDATE() AS DATE) 
                             ORDER BY LogID DESC";

            DataTable dt = new DataTable();

            using (SqlCommand cmd = new SqlCommand(check, conn))
            {
                cmd.Parameters.AddWithValue("@id", empID);
                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                {
                    da.Fill(dt);
                }
            }

            if (dt.Rows.Count == 0 || dt.Rows[0]["TimeOut"] != DBNull.Value)
            {
                // Action: TIME IN
                string ins = @"INSERT INTO Attendance (EmployeeID, TimeIn, LogDate) 
                               VALUES (@id, GETDATE(), CAST(GETDATE() AS DATE))";

                using (SqlCommand insCmd = new SqlCommand(ins, conn))
                {
                    insCmd.Parameters.AddWithValue("@id", empID);
                    insCmd.ExecuteNonQuery();
                }

                timeINlbl.Text = DateTime.Now.ToString("hh:mm tt");
                timeOUTlbl.Text = "--:--";
                StartUiClearTimer(); // ✅ Start auto-clear after showing time in
                MessageBox.Show($"Time In Successful for Employee ID: {empID}");
            }
            else
            {
                // Action: TIME OUT
                int logId = Convert.ToInt32(dt.Rows[0]["LogID"]);
                string upd = @"UPDATE Attendance SET TimeOut = GETDATE() WHERE LogID = @logid";

                using (SqlCommand updCmd = new SqlCommand(upd, conn))
                {
                    updCmd.Parameters.AddWithValue("@logid", logId);
                    updCmd.ExecuteNonQuery();
                }

                timeINlbl.Text = Convert.ToDateTime(dt.Rows[0]["TimeIn"]).ToString("hh:mm tt");
                timeOUTlbl.Text = DateTime.Now.ToString("hh:mm tt");
                StartUiClearTimer(); // ✅ Start auto-clear after showing time out
                MessageBox.Show($"Time Out Successful for Employee ID: {empID}");
            }
        }

        private MemoryStream RawToBitmapStream(byte[] buffer, int width, int height)
        {
            Bitmap bmp = new Bitmap(width, height, PixelFormat.Format8bppIndexed);
            ColorPalette cp = bmp.Palette;
            for (int i = 0; i < 256; i++) cp.Entries[i] = Color.FromArgb(i, i, i);
            bmp.Palette = cp;

            BitmapData data = bmp.LockBits(new Rectangle(0, 0, width, height),
                ImageLockMode.WriteOnly, bmp.PixelFormat);
            Marshal.Copy(buffer, 0, data.Scan0, width * height);
            bmp.UnlockBits(data);

            MemoryStream ms = new MemoryStream();
            bmp.Save(ms, ImageFormat.Bmp);
            ms.Position = 0;
            return ms;
        }
    }

}  

