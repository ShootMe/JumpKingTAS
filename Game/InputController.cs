using JumpKing.Controller;
using System.Collections.Generic;
using System.IO;
namespace TAS {
	public class InputController {
		private List<InputRecord> inputs = new List<InputRecord>();
		private int inputIndex, frameToNext;
		private string filePath;
		private List<InputRecord> fastForwards = new List<InputRecord>();
		public InputController(string filePath) {
			this.filePath = filePath;
		}

		public bool CanPlayback { get { return inputIndex < inputs.Count; } }
		public bool HasFastForward { get { return fastForwards.Count > 0; } }
		public int FastForwardSpeed { get { return fastForwards.Count == 0 ? 1 : fastForwards[0].Frames == 0 ? 400 : fastForwards[0].Frames; } }
		public int CurrentFrame { get; set; }
		public int CurrentInputFrame { get { return CurrentFrame - frameToNext + Current.Frames; } }
		public InputRecord Current { get; set; }
		public InputRecord Previous {
			get {
				if (frameToNext != 0 && inputIndex - 1 >= 0 && inputs.Count > 0) {
					return inputs[inputIndex - 1];
				}
				return null;
			}
		}
		public InputRecord Next {
			get {
				if (frameToNext != 0 && inputIndex + 1 < inputs.Count) {
					return inputs[inputIndex + 1];
				}
				return null;
			}
		}
		public bool HasInput(Actions action) {
			InputRecord input = Current;
			return input.HasActions(action);
		}
		public bool HasInputPressed(Actions action) {
			InputRecord input = Current;

			return input.HasActions(action) && CurrentInputFrame == 1;
		}
		public bool HasInputReleased(Actions action) {
			InputRecord current = Current;
			InputRecord previous = Previous;

			return !current.HasActions(action) && previous != null && previous.HasActions(action) && CurrentInputFrame == 1;
		}
		public override string ToString() {
			if (frameToNext == 0 && Current != null) {
				return Current.ToString() + "(" + CurrentFrame.ToString() + ")";
			} else if (inputIndex < inputs.Count && Current != null) {
				int inputFrames = Current.Frames;
				int startFrame = frameToNext - inputFrames;
				return Current.ToString() + "(" + (CurrentFrame - startFrame).ToString() + " / " + inputFrames + " : " + CurrentFrame + ")";
			}
			return string.Empty;
		}
		public string NextInput() {
			if (frameToNext != 0 && inputIndex + 1 < inputs.Count) {
				return inputs[inputIndex + 1].ToString();
			}
			return string.Empty;
		}
		public void InitializePlayback() {
			int trycount = 5;
			while (!ReadFile() && trycount >= 0) {
				System.Threading.Thread.Sleep(50);
				trycount--;
			}

			CurrentFrame = 0;
			inputIndex = 0;
			if (inputs.Count > 0) {
				Current = inputs[0];
				frameToNext = Current.Frames;
			} else {
				Current = new InputRecord();
				frameToNext = 1;
			}
		}
		public void ReloadPlayback() {
			int playedBackFrames = CurrentFrame;
			InitializePlayback();
			CurrentFrame = playedBackFrames;

			while (CurrentFrame >= frameToNext) {
				if (inputIndex + 1 >= inputs.Count) {
					inputIndex++;
					return;
				}
				if (Current.FastForward) {
					fastForwards.RemoveAt(0);
				}
				Current = inputs[++inputIndex];
				frameToNext += Current.Frames;
			}
		}
		public void PlaybackPlayer() {
			if (inputIndex < inputs.Count && !Manager.IsLoading()) {
				if (CurrentFrame >= frameToNext) {
					if (inputIndex + 1 >= inputs.Count) {
						inputIndex++;
						return;
					}
					if (Current.FastForward) {
						fastForwards.RemoveAt(0);
					}
					Current = inputs[++inputIndex];
					frameToNext += Current.Frames;
				}

				CurrentFrame++;
			}
		}
		public PadState GetPadState() {
			if (Current == null) {
				return default(PadState);
			} else {
				return new PadState() {
					up = Current.HasActions(Actions.Up),
					down = Current.HasActions(Actions.Down),
					left = Current.HasActions(Actions.Left),
					right = Current.HasActions(Actions.Right),
					jump = Current.HasActions(Actions.Jump),
					confirm = Current.HasActions(Actions.Jump),
					cancel = Current.HasActions(Actions.Cancel),
					pause = Current.HasActions(Actions.Pause)
				};
			}
		}
		public PadState GetPressed() {
			InputRecord previous = Previous;
			if (previous == null) {
				return GetPadState();
			} else {
				return new PadState() {
					up = !previous.HasActions(Actions.Up) && Current.HasActions(Actions.Up),
					down = !previous.HasActions(Actions.Down) && Current.HasActions(Actions.Down),
					left = !previous.HasActions(Actions.Left) && Current.HasActions(Actions.Left),
					right = !previous.HasActions(Actions.Right) && Current.HasActions(Actions.Right),
					jump = !previous.HasActions(Actions.Jump) && Current.HasActions(Actions.Jump),
					confirm = !previous.HasActions(Actions.Jump) && Current.HasActions(Actions.Jump),
					cancel = !previous.HasActions(Actions.Cancel) && Current.HasActions(Actions.Cancel),
					pause = !previous.HasActions(Actions.Pause) && Current.HasActions(Actions.Pause)
				};
			}
		}
		private bool ReadFile() {
			try {
				inputs.Clear();
				fastForwards.Clear();
				if (!File.Exists(filePath)) { return false; }

				int lines = 0;
				using (StreamReader sr = new StreamReader(filePath)) {
					while (!sr.EndOfStream) {
						string line = sr.ReadLine();

						if (line.IndexOf("Read", System.StringComparison.OrdinalIgnoreCase) == 0 && line.Length > 5) {
							lines++;
							ReadFile(line.Substring(5), lines);
							lines--;
						}

						InputRecord input = new InputRecord(++lines, line);
						if (input.FastForward) {
							fastForwards.Add(input);

							if (inputs.Count > 0) {
								inputs[inputs.Count - 1].ForceBreak = input.ForceBreak;
								inputs[inputs.Count - 1].FastForward = true;
							}
						} else if (input.Frames != 0) {
							inputs.Add(input);
						}
					}
				}
				return true;
			} catch {
				return false;
			}
		}
		private void ReadFile(string extraFile, int lines) {
			int index = extraFile.IndexOf(',');
			string filePath = index > 0 ? extraFile.Substring(0, index) : extraFile;
			if (!File.Exists(filePath)) {
				string[] files = Directory.GetFiles(Directory.GetCurrentDirectory(), $"{filePath}*.tas");
				filePath = (files.GetValue(0)).ToString();
				if (!File.Exists(filePath)) { return; }
			}
			int skipLines = 0;
			int lineLen = int.MaxValue;
			if (index > 0) {
				int indexLen = extraFile.IndexOf(',', index + 1);
				if (indexLen > 0) {
					string startLine = extraFile.Substring(index + 1, indexLen - index - 1);
					string endLine = extraFile.Substring(indexLen + 1);
					if (!int.TryParse(startLine, out skipLines)) {
						skipLines = GetLine(startLine, filePath);
					}
					if (!int.TryParse(endLine, out lineLen)) {
						lineLen = GetLine(endLine, filePath);
					}
				} else {
					string startLine = extraFile.Substring(index + 1);
					if (!int.TryParse(startLine, out skipLines)) {
						skipLines = GetLine(startLine, filePath);
					}
				}
			}

			int subLine = 0;
			using (StreamReader sr = new StreamReader(filePath)) {
				while (!sr.EndOfStream) {
					string line = sr.ReadLine();

					subLine++;
					if (subLine <= skipLines) { continue; }
					if (subLine > lineLen) { break; }

					if (line.IndexOf("Read", System.StringComparison.OrdinalIgnoreCase) == 0 && line.Length > 5) {
						ReadFile(line.Substring(5), lines);
					}

					InputRecord input = new InputRecord(lines, line);
					if (input.FastForward) {
						fastForwards.Add(input);

						if (inputs.Count > 0) {
							inputs[inputs.Count - 1].ForceBreak = input.ForceBreak;
							inputs[inputs.Count - 1].FastForward = true;
						}
					} else if (input.Frames != 0) {
						inputs.Add(input);
					}
				}
			}
		}
		private int GetLine(string label, string path) {
			int curLine = 0;
			using (StreamReader sr = new StreamReader(path)) {
				while (!sr.EndOfStream) {
					curLine++;
					string line = sr.ReadLine();
					if (line.StartsWith("#" + label)) {
						return curLine;
					}
				}
				return int.MaxValue;
			}
		}
	}
}