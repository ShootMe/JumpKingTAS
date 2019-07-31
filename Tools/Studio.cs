﻿using TASStudio.Controls;
using TASStudio.Entities;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;
namespace TASStudio {
	public partial class Studio : Form {
		private static string titleBarText = "Studio v" + Assembly.GetExecutingAssembly().GetName().Version.ToString(3);
		private const string RegKey = "HKEY_CURRENT_USER\\SOFTWARE\\JumpKingStudio\\Form";
		[STAThread]
		public static void Main() {
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new Studio());
		}

		private List<InputRecord> Lines = new List<InputRecord>();
		private GameMemory memory = new GameMemory();
		private int totalFrames = 0, currentFrame = 0;
		private bool updating = false;
		private DateTime lastChanged = DateTime.MinValue;
		public Studio() {
			InitializeComponent();
			Text = titleBarText;

			InputRecord.Delimiter = (char)RegRead("delim", (int)',');
			Lines.Add(new InputRecord(""));
			EnableStudio(false);

			DesktopLocation = new Point(RegRead("x", DesktopLocation.X), RegRead("y", DesktopLocation.Y));
			Size = new Size(RegRead("w", Size.Width), RegRead("h", Size.Height));
		}
		private void TASStudio_FormClosed(object sender, FormClosedEventArgs e) {
			RegWrite("delim", (int)InputRecord.Delimiter);
			RegWrite("x", DesktopLocation.X); RegWrite("y", DesktopLocation.Y);
			RegWrite("w", Size.Width); RegWrite("h", Size.Height);
		}
		private void Studio_Shown(object sender, EventArgs e) {
			Thread updateThread = new Thread(UpdateLoop);
			updateThread.IsBackground = true;
			updateThread.Start();
		}
		private void Studio_KeyDown(object sender, KeyEventArgs e) {
			try {
				if (e.Modifiers == (Keys.Shift | Keys.Control) && e.KeyCode == Keys.S) {
					tasText.SaveNewFile();
				} else if (e.Modifiers == Keys.Control && e.KeyCode == Keys.S) {
					tasText.SaveFile();
				} else if (e.Modifiers == Keys.Control && e.KeyCode == Keys.O) {
					tasText.OpenFile();
				} else if (e.Modifiers == Keys.Control && e.KeyCode == Keys.K) {
					CommentText();
				}
			} catch (Exception ex) {
				MessageBox.Show(this, ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}
		private DialogResult ShowInputDialog(string title, ref string input) {
			Size size = new Size(200, 70);
			DialogResult result = DialogResult.Cancel;

			using (Form inputBox = new Form()) {
				inputBox.FormBorderStyle = FormBorderStyle.FixedDialog;
				inputBox.ClientSize = size;
				inputBox.Text = title;
				inputBox.StartPosition = FormStartPosition.CenterParent;
				inputBox.MinimizeBox = false;
				inputBox.MaximizeBox = false;

				TextBox textBox = new TextBox();
				textBox.Size = new Size(size.Width - 10, 23);
				textBox.Location = new Point(5, 5);
				textBox.Font = tasText.Font;
				textBox.Text = input;
				textBox.MaxLength = 1;
				inputBox.Controls.Add(textBox);

				Button okButton = new Button();
				okButton.DialogResult = DialogResult.OK;
				okButton.Name = "okButton";
				okButton.Size = new Size(75, 23);
				okButton.Text = "&OK";
				okButton.Location = new Point(size.Width - 80 - 80, 39);
				inputBox.Controls.Add(okButton);

				Button cancelButton = new Button();
				cancelButton.DialogResult = DialogResult.Cancel;
				cancelButton.Name = "cancelButton";
				cancelButton.Size = new Size(75, 23);
				cancelButton.Text = "&Cancel";
				cancelButton.Location = new Point(size.Width - 80, 39);
				inputBox.Controls.Add(cancelButton);

				inputBox.AcceptButton = okButton;
				inputBox.CancelButton = cancelButton;

				result = inputBox.ShowDialog(this);
				input = textBox.Text;
			}
			return result;
		}
		private void UpdateLoop() {
			bool lastHooked = false;
			while (true) {
				try {
					bool hooked = memory.HookProcess();
					if (lastHooked != hooked) {
						lastHooked = hooked;
						this.Invoke((Action)delegate () { EnableStudio(hooked); });
					}
					if (lastChanged.AddSeconds(0.6) < DateTime.Now) {
						lastChanged = DateTime.Now;
						this.Invoke((Action)delegate () {
							if ((!string.IsNullOrEmpty(tasText.LastFileName) || !string.IsNullOrEmpty(tasText.SaveToFileName)) && tasText.IsChanged) {
								tasText.SaveFile();
							}
						});
					}
					if (hooked) {
						UpdateValues();
					}

					Thread.Sleep(14);
				} catch { }
			}
		}
		public void EnableStudio(bool hooked) {
			if (hooked) {
				string fileName = Path.Combine(Path.GetDirectoryName(memory.Program.MainModule.FileName), "JumpKing.tas");
				if (!File.Exists(fileName)) { File.WriteAllText(fileName, string.Empty); }

				if (string.IsNullOrEmpty(tasText.LastFileName)) {
					if (string.IsNullOrEmpty(tasText.SaveToFileName)) {
						tasText.OpenBindingFile(fileName, Encoding.ASCII);
					}
					tasText.LastFileName = fileName;
				}
				tasText.SaveToFileName = fileName;
				if (tasText.LastFileName != tasText.SaveToFileName) {
					tasText.SaveFile(true);
				}
				tasText.Focus();
			} else {
				lblStatus.Text = "Searching...";
				tasText.Height += statusBar.Height - 22;
				statusBar.Height = 22;
				currentFrame = 0;
				if (tasText.CurrentLine >= 0) {
					tasText.CurrentLine = -1;
				}
			}
		}
		public void UpdateValues() {
			if (this.InvokeRequired) {
				this.Invoke((Action)UpdateValues);
			} else {
				string tas = memory.TASOutput();
				if (!string.IsNullOrEmpty(tas)) {
					int index = tas.IndexOf('[');
					string num = tas.Substring(0, index);
					int temp = 0;
					if (int.TryParse(num, out temp)) {
						temp--;
						if (tasText.CurrentLine != temp) {
							tasText.CurrentLine = temp;
						}
					}

					index = tas.IndexOf(':');
					int pIndex = tas.IndexOf(')', index);
					if (pIndex >= 0) {
						num = tas.Substring(index + 2, tas.IndexOf(')', index) - index - 2);
					}
					if (int.TryParse(num, out temp)) {
						currentFrame = temp;
					}

					index = tas.IndexOf('(');
					int index2 = tas.IndexOf(' ', index);
					if (index2 >= 0) {
						num = tas.Substring(index + 1, index2 - index - 1);
						if (tasText.CurrentLineText != num) {
							tasText.CurrentLineText = num;
						}
					}
				} else {
					currentFrame = 0;
					if (tasText.CurrentLine >= 0) {
						tasText.CurrentLine = -1;
					}
				}

				UpdateStatusBar();
			}
		}
		private void tasText_LineRemoved(object sender, LineRemovedEventArgs e) {
			int count = e.Count;
			while (count-- > 0) {
				InputRecord input = Lines[e.Index];
				totalFrames -= input.Frames;
				Lines.RemoveAt(e.Index);
			}

			UpdateStatusBar();
		}
		private void tasText_LineInserted(object sender, LineInsertedEventArgs e) {
			RichText tas = (RichText)sender;
			int count = e.Count;
			while (count-- > 0) {
				InputRecord input = new InputRecord(tas.GetLineText(e.Index + count));
				Lines.Insert(e.Index, input);
				totalFrames += input.Frames;
			}

			UpdateStatusBar();
		}
		private void UpdateStatusBar() {
			if (memory.IsHooked) {
				lblStatus.Text = "(" + (currentFrame > 0 ? currentFrame + "/" : "") + totalFrames + ") " + memory.TASPlayerOutput();
			} else {
				lblStatus.Text = "(" + totalFrames + ")\r\nSearching...";
			}
			string text = lblStatus.Text;
			int totalLines = 0;
			int index = 0;
			while ((index = text.IndexOf('\n', index) + 1) > 0) {
				totalLines++;
			}
			if (text.LastIndexOf('\n') + 1 < text.Length) {
				totalLines++;
			}
			totalLines = totalLines * 18;
			totalLines = totalLines < 22 ? 22 : totalLines;
			if (statusBar.Height - totalLines != 0) {
				tasText.Height += statusBar.Height - totalLines;
				statusBar.Height = totalLines;
			}
		}
		private void tasText_TextChanged(object sender, TextChangedEventArgs e) {
			lastChanged = DateTime.Now;
			UpdateLines((RichText)sender, e.ChangedRange);
		}
		private void CommentText() {
			Range range = tasText.Selection;

			int start = range.Start.iLine;
			int end = range.End.iLine;
			if (start > end) {
				int temp = start;
				start = end;
				end = temp;
			}

			tasText.Selection = new Range(tasText, 0, start, tasText[end].Count, end);
			string text = tasText.SelectedText;

			int i = 0;
			bool startLine = true;
			StringBuilder sb = new StringBuilder(text.Length + end - start);
			while (i < text.Length) {
				char c = text[i++];
				if (startLine) {
					if (c != '#') {
						sb.Append('#').Append(c);
					}
					startLine = false;
				} else if (c == '\n') {
					sb.AppendLine();
					startLine = true;
				} else if (c != '\r') {
					sb.Append(c);
				}
			}

			tasText.SelectedText = sb.ToString();
			tasText.Selection = new Range(tasText, 0, start, tasText[end].Count, end);
		}
		private void UpdateLines(RichText tas, Range range) {
			if (updating) { return; }
			updating = true;

			int start = range.Start.iLine;
			int end = range.End.iLine;
			if (start > end) {
				int temp = start;
				start = end;
				end = temp;
			}
			int originalStart = start;

			bool modified = false;
			StringBuilder sb = new StringBuilder();
			Place place = new Place(0, end);
			while (start <= end) {
				InputRecord old = Lines.Count > start ? Lines[start] : null;
				string text = tas[start++].Text;
				InputRecord input = new InputRecord(text);
				if (old != null) {
					totalFrames -= old.Frames;

					string line = input.ToString();
					if (text != line) {
						if (old.Frames == 0 && input.Frames == 0 && old.ZeroPadding == input.ZeroPadding && old.Equals(input) && line.Length >= text.Length) {
							line = string.Empty;
						}

						Range oldRange = tas.Selection;
						if (!string.IsNullOrEmpty(line)) {
							int index = oldRange.Start.iChar + line.Length - text.Length;
							if (index < 0) { index = 0; }
							if (index > 4) { index = 4; }
							if (old.Frames == input.Frames && old.ZeroPadding == input.ZeroPadding) { index = 4; }

							place = new Place(index, start - 1);
						}
						modified = true;
					} else {
						place = new Place(4, start - 1);
					}

					text = line;
					Lines[start - 1] = input;
				} else {
					place = new Place(text.Length, start - 1);
				}

				if (start <= end) {
					sb.AppendLine(text);
				} else {
					sb.Append(text);
				}

				totalFrames += input.Frames;
			}

			if (modified) {
				tas.Selection = new Range(tas, 0, originalStart, tas[end].Count, end);
				tas.SelectedText = sb.ToString();
				tas.Selection = new Range(tas, place.iChar, end, place.iChar, end);
				Text = titleBarText + " ***";
			}
			UpdateStatusBar();

			updating = false;
		}
		private void tasText_NoChanges(object sender, EventArgs e) {
			Text = titleBarText;
		}
		private void tasText_FileOpening(object sender, EventArgs e) {
			Lines.Clear();
			totalFrames = 0;
			UpdateStatusBar();
		}
		private void tasText_LineNeeded(object sender, LineNeededEventArgs e) {
			InputRecord record = new InputRecord(e.SourceLineText);
			e.DisplayedLineText = record.ToString();
		}
		private void tasText_FileOpened(object sender, EventArgs e) {
			try {
				tasText.SaveFile(true);
			} catch { }
		}
		private int RegRead(string name, int def) {
			object o = null;
			try {
				o = Registry.GetValue(RegKey, name, null);
			} catch { }

			if (o is int) {
				return (int)o;
			}

			return def;
		}
		private void RegWrite(string name, int val) {
			try {
				Registry.SetValue(RegKey, name, val);
			} catch { }
		}
	}
}