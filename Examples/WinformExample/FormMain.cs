using Gma.System.MouseKeyHook;
using Loamen.KeyMouseHook;
using Loamen.KeyMouseHook.Native;
using Loamen.KeyMouseHook.Simulators;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinformExample
{
    public partial class FormMain : Form
    {
        [DllImport("user32.dll")]
        static extern short VkKeyScan(char ch);

        private readonly KeyMouseFactory eventHookFactory = new KeyMouseFactory(Hook.GlobalEvents());
        private readonly KeyboardWatcher keyboardWatcher;
        private readonly MouseWatcher mouseWatcher;
        private List<MacroEvent> _macroEvents;
        private List<string> lines;
        //MacroEvent
        private Hotkey hotkey;
        private bool isRecording = false;
        private bool isPlaying = false;
        private bool isNew = false;
        private string path;
        private int hotkeyRecordId;
        private int hotkeyPlaybackId;

        public FormMain()
        {
            InitializeComponent();

            keyboardWatcher = eventHookFactory.GetKeyboardWatcher();
            keyboardWatcher.OnKeyboardInput += (s, e) =>
            {
                if (e.KeyMouseEventType == MacroEventType.KeyPress)
                {
                    var keyEvent = (KeyPressEventArgs)e.EventArgs;
                    //Keys key = (Keys)Enum.Parse(typeof(Keys), ((int)keyEvent.KeyChar).ToString());
                    //var j = System.Windows.Input.KeyInterop.KeyFromVirtualKey(Convert.ToInt32(keyEvent.KeyChar));
                    //var mappedChar = VkKeyScan(keyEvent.KeyChar);
                    Log(string.Format("Key {0}\t\t{1}\n", keyEvent.KeyChar, e.KeyMouseEventType));
                }
                else
                {
                    var keyEvent = (KeyEventArgs)e.EventArgs;
                    Log(string.Format("Key {0}\t\t{1}\n", keyEvent.KeyCode, e.KeyMouseEventType));
                    if ((keyEvent.KeyData == (Keys.Alt | Keys.Scroll)) || keyEvent.KeyData == Keys.Scroll)
                        return;
                }

                if (_macroEvents != null)
                {
                    _macroEvents.Add(e);
                    //MacroEvent macroEvent = new MacroEvent(MacroEventType.KeyDown,new EventArgs(),1000);

                    //macroEvent.EventArgs = e.EventArgs;
                    //macroEvent.KeyMouseEventType = e.KeyMouseEventType;
                    //macroEvent.TimeSinceLastEvent = e.TimeSinceLastEvent;
                    if (e.KeyMouseEventType == MacroEventType.KeyDown)
                    {
                        var keyEvent = (KeyEventArgs)e.EventArgs;
                        lines.Add(keyEvent.KeyCode.ToString());
                        lines.Add(keyEvent.KeyValue.ToString());
                        lines.Add(e.TimeSinceLastEvent.ToString());
                    }
                }   
            };

            mouseWatcher = eventHookFactory.GetMouseWatcher();
            mouseWatcher.OnMouseInput += (s, e) =>
            {
                if (_macroEvents != null)
                    _macroEvents.Add(e);

                switch (e.KeyMouseEventType)
                {
                    case MacroEventType.MouseMove:
                        var mouseEvent = (MouseEventArgs)e.EventArgs;
                        LogMouseLocation(mouseEvent.X, mouseEvent.Y);
                        break;
                    case MacroEventType.MouseWheel:
                        mouseEvent = (MouseEventArgs)e.EventArgs;
                        LogMouseWheel(mouseEvent.Delta);
                        break;
                    case MacroEventType.MouseClick:
                    case MacroEventType.MouseDown:
                    case MacroEventType.MouseUp:
                        mouseEvent = (MouseEventArgs)e.EventArgs;
                        Log(string.Format("Mouse {0}\t\t{1}\n", mouseEvent.Button, e.KeyMouseEventType));
                        break;
                    case MacroEventType.MouseDownExt:
                        MouseEventExtArgs downExtEvent = (MouseEventExtArgs)e.EventArgs;
                        if (downExtEvent.Button != MouseButtons.Right)
                        {
                            Log(string.Format("Mouse Down \t {0}\n", downExtEvent.Button));
                            return;
                        }
                        Log(string.Format("Mouse Down \t {0} Suppressed\n", downExtEvent.Button));
                        downExtEvent.Handled = true;
                        break;
                    case MacroEventType.MouseWheelExt:
                        MouseEventExtArgs wheelEvent = (MouseEventExtArgs)e.EventArgs;
                        labelWheel.Text = string.Format("Wheel={0:000}", wheelEvent.Delta);
                        Log("Mouse Wheel Move Suppressed.\n");
                        wheelEvent.Handled = true;
                        break;
                    case MacroEventType.MouseDragStarted:
                        Log("MouseDragStarted\n");
                        break;
                    case MacroEventType.MouseDragFinished:
                        Log("MouseDragFinished\n");
                        break;
                    case MacroEventType.MouseDoubleClick:
                        mouseEvent = (MouseEventArgs)e.EventArgs;
                        Log(string.Format("Mouse {0}\t\t{1}\n", mouseEvent.Button, e.KeyMouseEventType));
                        break;
                    default:
                        break;
                }
            };
        }

        private void Log(string text)
        {
            if (IsDisposed) return;
            textBoxLog.AppendText(DateTime.Now.ToString("HH:mm:ss:")  + text);
            textBoxLog.ScrollToCaret();
        }

        private void LogMouseWheel(int Delta)
        {
            if (IsDisposed) return;
            labelWheel.Text = string.Format("Wheel={0:000}", Delta);
        }
        private void LogMouseLocation(int X, int Y)
        {
            if (IsDisposed) return;
            labelMousePosition.Text = string.Format("x={0:0000}; y={1:0000}", X, Y);
        }

        public void StartWatch(IKeyboardMouseEvents events = null)
        {
            Thread.Sleep(1000);
            _macroEvents = new List<MacroEvent>();
            keyboardWatcher.Start(events);
            mouseWatcher.Start(events);
        }

        public void StopWatch()
        {
            keyboardWatcher.Stop();
            mouseWatcher.Stop();
        }

        private void FormMain_Load(object sender, EventArgs e)
        {
            this.Location = new Point(Screen.PrimaryScreen.WorkingArea.Width - this.Width - 10, this.Height / 2);

            InitHotkey();
        }

        private void InitHotkey()
        {
            hotkey = new Hotkey(this.Handle);
            hotkey.OnHotkey += Hotkey_OnHotkey;
            this.hotkeyRecordId = hotkey.RegisterHotkey(Keys.Scroll, Hotkey.KeyFlags.NONE);
            this.hotkeyPlaybackId = hotkey.RegisterHotkey(Keys.Scroll, Hotkey.KeyFlags.MOD_ALT);

            #region Combination
            //var record = Combination.TriggeredBy(Keys.F10).With(Keys.Control);
            //var playback = Combination.TriggeredBy(Keys.F12).With(Keys.Control);

            //var assignment = new Dictionary<Combination, Action>
            //{
            //    {record, ()=>{this.Record(); Debug.WriteLine("Control+F10"); } },
            //    {playback,  ()=>{this.Playback(); Debug.WriteLine("Control+F12"); }}
            //};

            //Hook.GlobalEvents().OnCombination(assignment);
            #endregion
        }

        private void Hotkey_OnHotkey(int HotKeyID)
        {
            if (HotKeyID == hotkeyRecordId)
            {
                if (isPlaying) return;
                this.Record();
            }
            else if (HotKeyID == hotkeyPlaybackId)
                this.Playback();
        }

        private void FormMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (eventHookFactory != null)
                eventHookFactory.Dispose();
        }

        private void btnRecord_Click(object sender, EventArgs e)
        {
            Record();
        }

        private void btnPlayback_Click(object sender, EventArgs e)
        {
            Playback();
        }

        private void btnClearLog_Click(object sender, EventArgs e)
        {
            textBoxLog.Clear();
        }

        private void radioNone_CheckedChanged(object sender, EventArgs e)
        {
            if (((RadioButton)sender).Checked)
            {
                StopWatch();
                isRecording = false;
                btnRecord.Text = "Record";
                if (_macroEvents != null && _macroEvents.Count > 0)
                {
                    btnPlayback.Enabled = true;
                }
            }
        }

        private void checkBoxSuppressMouse_CheckedChanged(object sender, EventArgs e)
        {
            if (eventHookFactory.KeyboardMouseEvents == null) return;

            mouseWatcher.SupressMouse(((CheckBox)sender).Checked, MacroEventType.MouseDown);
        }

        private void checkBoxSupressMouseWheel_CheckedChanged(object sender, EventArgs e)
        {
            if (eventHookFactory.KeyboardMouseEvents == null) return;

            mouseWatcher.SupressMouse(((CheckBox)sender).Checked, MacroEventType.MouseWheel);
        }

        private void Record()
        {
            if (!isRecording)
            {
                if (radioApplication.Checked)
                    StartWatch(Hook.AppEvents());
                else if (radioGlobal.Checked)
                    StartWatch(Hook.GlobalEvents());
                isRecording = true;
                btnRecord.Text = "停止(ScrLk)";
            }
            else
            {
                StopWatch();
                isRecording = false;
                btnRecord.Text = "记录(ScrLk)";
                if (_macroEvents != null && _macroEvents.Count > 0)
                {
                    btnPlayback.Enabled = true;
                }
            }
        }
        
        private void Playback()
        {
            this.isPlaying = true;
            btnPlayback.Enabled = false;
            var sim = new InputSimulator();
            sim.OnPlayback += OnPlayback;
            sim.PlayBack(_macroEvents);
            btnPlayback.Enabled = true;
            var timer = new System.Threading.Timer(new TimerCallback(SetPlaying), false, 10000, 2000);
        }
        
        private void OnPlayback(object sender, MacroEvent e)
        {
            switch (e.KeyMouseEventType)
            {
                case MacroEventType.MouseMove:
                    var mouseEvent = (MouseEventArgs)e.EventArgs;
                    LogMouseLocation(mouseEvent.X, mouseEvent.Y);
                    break;
                case MacroEventType.MouseWheel:
                    mouseEvent = (MouseEventArgs)e.EventArgs;
                    LogMouseWheel(mouseEvent.Delta);
                    break;
                case MacroEventType.MouseClick:
                case MacroEventType.MouseDown:
                case MacroEventType.MouseUp:
                    mouseEvent = (MouseEventArgs)e.EventArgs;
                    Log(string.Format("Mouse {0}\t\t{1}\t\tSimulator\n", mouseEvent.Button, e.KeyMouseEventType));
                    break;
                case MacroEventType.MouseDownExt:
                    MouseEventExtArgs downExtEvent = (MouseEventExtArgs)e.EventArgs;
                    if (downExtEvent.Button != MouseButtons.Right)
                    {
                        Log(string.Format("Mouse Down \t {0}\t\t\tSimulator\n", downExtEvent.Button));
                        return;
                    }
                    Log(string.Format("Mouse Down \t {0} Suppressed.\t\tSimulator\n", downExtEvent.Button));
                    downExtEvent.Handled = true;
                    break;
                case MacroEventType.MouseWheelExt:
                    MouseEventExtArgs wheelEvent = (MouseEventExtArgs)e.EventArgs;
                    labelWheel.Text = string.Format("Wheel={0:000}", wheelEvent.Delta);
                    Log("Mouse Wheel Move Suppressed.\t\tSimulator\n");
                    wheelEvent.Handled = true;
                    break;
                case MacroEventType.MouseDragStarted:
                    Log("MouseDragStarted\t\tSimulator\n");
                    break;
                case MacroEventType.MouseDragFinished:
                    Log("MouseDragFinished\t\tSimulator\n");
                    break;
                case MacroEventType.MouseDoubleClick:
                    mouseEvent = (MouseEventArgs)e.EventArgs;
                    Log(string.Format("Mouse {0}\t\t{1}\t\tSimulator\n", mouseEvent.Button, e.KeyMouseEventType));
                    break;
                case MacroEventType.KeyPress:
                    var keyEvent = (KeyPressEventArgs)e.EventArgs;
                    Keys key = (Keys)Enum.Parse(typeof(Keys), ((int)Char.ToUpper(keyEvent.KeyChar)).ToString());
                    Log(string.Format("Key {0}\t\t{1}\t\tSimulator\n", key, e.KeyMouseEventType));
                    break;
                case MacroEventType.KeyDown:
                case MacroEventType.KeyUp:
                    var kEvent = (KeyEventArgs)e.EventArgs;
                    Log(string.Format("Key {0}\t\t{1}\t\tSimulator\n", kEvent.KeyCode, e.KeyMouseEventType));
                    break;
                default:
                    break;
            }
        }

        private void SetPlaying(object state)
        {
            isPlaying = (bool)state;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            
            
            if (!isNew)
            {
                lines = new List<string>();
                if (radioApplication.Checked)
                    StartWatch(Hook.AppEvents());
                else if (radioGlobal.Checked)
                    StartWatch(Hook.GlobalEvents());
                isNew = true;
                button1.Text = "保存";
                path = newfile();
            }
            else
            {
                StopWatch();
                isRecording = false;
                button1.Text = "记录(C+F)";
                if (_macroEvents != null && _macroEvents.Count > 0)
                {
                    
                    btnPlayback.Enabled = true;
                    string[] temp=new string[lines.Count+1];
                    lines.CopyTo(temp);
                    writefile(path, temp);
                }
            }
        }
        private string newfile()
        {
           
            string path = System.IO.Directory.GetCurrentDirectory();
            var file=System.IO.Directory.CreateDirectory(path);
            string fileName = "dongsile.txt";
            string pathString = System.IO.Path.Combine(path, fileName);
            
            return pathString;
            
        }
        private void writefile(string pathString,string[] lines)
        {
            if (!System.IO.File.Exists(pathString))
            {
                //System.IO.File.Create(pathString);
                FileStream _file = new FileStream(pathString, FileMode.Create, FileAccess.ReadWrite);
                using (System.IO.StreamWriter file =
                 new System.IO.StreamWriter(_file))
                {
                    foreach (string line in lines)
                    {
                        file.WriteLine(line);
                     
                    }
                }
            }

            else
            {
                Console.WriteLine("文件已经存在，删掉重试");
                MessageBox.Show("文件已经存在，删掉重试");
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            _macroEvents = new List<MacroEvent>();
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = "c://";
            openFileDialog.Filter = "文本文件|*.txt|所有文件|*.*";
            openFileDialog.RestoreDirectory = true;
            openFileDialog.FilterIndex = 1;
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string fName = openFileDialog.FileName;
                FileStream fs = new FileStream(fName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                int counter = 0;
                string line;

                // Read the file and display it line by line.  
                System.IO.StreamReader file =
                    new System.IO.StreamReader(fs);
                while ((line = file.ReadLine()) != null)
                {
                    System.Console.WriteLine(line);
                    lines.Add(line);
                    counter++;
                }
                for(int i=0;i<lines.Count-1;i=i+3)
                {
                    MacroEvent temp = new MacroEvent(MacroEventType.KeyDown,new EventArgs(), Convert.ToInt32(lines[i+2]));
                    var tempenvent = new KeyEventArgs((Keys)Enum.Parse(typeof(Keys), lines[i]));
                    //tempenvent.KeyValue = Convert.ToInt32(lines[i + 1]);
                    temp.EventArgs = tempenvent;
                    _macroEvents.Add(temp);
                }
                
                while (true)
                {
                    Playback();
                    //if (s.KeyCode == Keys.Enter)
                      //  break;
                }
                file.Close();
                System.Console.WriteLine("There were {0} lines.", counter);
            }


        }
        private void btnDemo_Click(object sender, EventArgs e)
        {
            Random ran = new Random();
            
            var sim = new InputSimulator();
            sim.Keyboard
               .KeyPress(VirtualKeyCode.TAB)
               .Sleep(500+ ran.Next(100))
               .KeyPress(VirtualKeyCode.TAB)
               .Sleep(500)
               .KeyPress(VirtualKeyCode.VK_2)
               .Sleep(400 + ran.Next(100))
               .KeyPress(VirtualKeyCode.VK_1)
               .Sleep(2800 + ran.Next(100))
               .KeyPress(VirtualKeyCode.VK_3)
               .Sleep(100)
               .KeyPress(VirtualKeyCode.VK_A)
               .Sleep(500)
               .KeyPress(VirtualKeyCode.VK_A)
               .Sleep(200)
               .KeyPress(VirtualKeyCode.VK_A)
               .Sleep(500)
               .KeyPress(VirtualKeyCode.VK_A)
               .Sleep(200)
               .KeyPress(VirtualKeyCode.VK_A)
               .Sleep(500)
               .KeyPress(VirtualKeyCode.VK_A)
               .Sleep(200)
               .KeyPress(VirtualKeyCode.VK_A)
               .Sleep(500)
               .KeyPress(VirtualKeyCode.VK_A)
               .Sleep(800)
               .KeyPress(VirtualKeyCode.VK_4)
               .Sleep(1800 + ran.Next(100))
               .KeyPress(VirtualKeyCode.VK_4)
               .Sleep(1200 + ran.Next(100))
               .KeyPress(VirtualKeyCode.VK_5)
               .Sleep(3500)
               .KeyPress(VirtualKeyCode.VK_6)
               .Sleep(1500 + ran.Next(100))
               .KeyPress(VirtualKeyCode.VK_7)
               .Sleep(1000 + ran.Next(100))
               .KeyPress(VirtualKeyCode.VK_7)
               .Sleep(500 + ran.Next(100))
               .KeyPress(VirtualKeyCode.VK_K)
               .Sleep(1000 + ran.Next(100))
               .KeyPress(VirtualKeyCode.VK_K)
               .Sleep(1000 + ran.Next(100))
               .KeyPress(VirtualKeyCode.VK_8)
               .Sleep(700 + ran.Next(100))
               .KeyPress(VirtualKeyCode.SPACE)
               
               .KeyPress(VirtualKeyCode.VK_8)
               .Sleep(1000 + ran.Next(100))
               .KeyPress(VirtualKeyCode.SPACE)
               
               .KeyPress(VirtualKeyCode.VK_1)
               .Sleep(2800 + ran.Next(100))
               .KeyPress(VirtualKeyCode.VK_3)
               .Sleep(100)
               .KeyPress(VirtualKeyCode.VK_D)
               .Sleep(500)
               .KeyPress(VirtualKeyCode.VK_D)
               .Sleep(200)
               .KeyPress(VirtualKeyCode.VK_D)
               .Sleep(500)
               .KeyPress(VirtualKeyCode.VK_D)
               .Sleep(200)
               .KeyPress(VirtualKeyCode.VK_D)
               .Sleep(500)
               .KeyPress(VirtualKeyCode.VK_D)
               .Sleep(200)
               .KeyPress(VirtualKeyCode.VK_D)
               .Sleep(500)
               .KeyPress(VirtualKeyCode.VK_D)
               .Sleep(800 + ran.Next(100))
               .KeyPress(VirtualKeyCode.VK_4)
               .Sleep(1800)
               .KeyPress(VirtualKeyCode.VK_4)
               .Sleep(1200 + ran.Next(100))
               .KeyPress(VirtualKeyCode.VK_6)
               .Sleep(2800 + ran.Next(100))
               .KeyPress(VirtualKeyCode.VK_7)
               .Sleep(1000 + ran.Next(100))
               .KeyPress(VirtualKeyCode.VK_7)
               .Sleep(1000 + ran.Next(100))
               
               .KeyPress(VirtualKeyCode.VK_K)
               .Sleep(1000 + ran.Next(100))
               .KeyPress(VirtualKeyCode.VK_K)
               .Sleep(1000 + ran.Next(100))
               .KeyPress(VirtualKeyCode.VK_8)
               .Sleep(1000 + ran.Next(100))
               .KeyPress(VirtualKeyCode.SPACE)
               .Sleep(1000)
               .KeyPress(VirtualKeyCode.VK_8)
               .Sleep(1000 + ran.Next(100))
               .KeyPress(VirtualKeyCode.SPACE)
               .Sleep(1000)

               ;


              // .TextEntry("notepad")
              

           
        }
    }
}
