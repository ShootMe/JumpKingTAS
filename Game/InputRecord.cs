using System;
using System.Text;
namespace TAS {
	[Flags]
	public enum Actions {
		None,
		Left = 1,
		Right = 2,
		Up = 4,
		Down = 8,
		Jump = 16,
		Pause = 32,
		Cancel = 64,
		Reset = 128,
		State = 256
	}
	public class InputRecord {
		public int Line { get; set; }
		public int Frames { get; set; }
		public Actions Actions { get; set; }
		public int PosX { get; set; }
		public int PosY { get; set; }
		public int Direction { get; set; }
		public bool FastForward { get; set; }
		public bool ForceBreak { get; set; }
		public InputRecord() { }
		public InputRecord(int number, string line) {
			Line = number;

			int index = 0;
			Frames = ReadFrames(line, ref index);
			if (Frames == 0) {
				line = line.Trim();
				if (line.StartsWith("***")) {
					FastForward = true;
					index = 3;

					if (line.Length >= 4 && line[3] == '!') {
						ForceBreak = true;
						index = 4;
					}

					Frames = ReadFrames(line, ref index);
				} else if (line.StartsWith("@")) {
					index++;
					int x = ReadFrames(line, ref index);
					index++;
					int y = ReadFrames(line, ref index);
					index++;
					int d = ReadFrames(line, ref index);
					Actions |= Actions.State;
					PosX = x;
					PosY = y;
					Direction = d == 1 ? 1 : 0;
					Frames = 1;
				}
				return;
			}

			while (index < line.Length) {
				char c = line[index];

				switch (char.ToUpper(c)) {
					case 'L': Actions ^= Actions.Left; break;
					case 'R': Actions ^= Actions.Right; break;
					case 'U': Actions ^= Actions.Up; break;
					case 'D': Actions ^= Actions.Down; break;
					case 'J': Actions ^= Actions.Jump; break;
					case 'P': Actions ^= Actions.Pause; break;
					case 'C': Actions ^= Actions.Cancel; break;
					case 'X': Actions ^= Actions.Reset; break;
				}

				index++;
			}
		}
		private int ReadFrames(string line, ref int start) {
			bool foundFrames = false;
			int frames = 0;
			bool negative = false;
			while (start < line.Length) {
				char c = line[start];

				if (!foundFrames) {
					if (char.IsDigit(c)) {
						foundFrames = true;
						frames = c ^ 0x30;
					} else if (c == '-') {
						negative = true;
					} else if (c != ' ') {
						return negative ? -frames : frames;
					}
				} else if (char.IsDigit(c)) {
					if (frames < 999999) {
						frames = frames * 10 + (c ^ 0x30);
					} else {
						frames = 999999;
					}
				} else if (c != ' ') {
					return negative ? -frames : frames;
				}

				start++;
			}

			return negative ? -frames : frames;
		}
		public bool HasActions(Actions actions) {
			return (Actions & actions) != 0;
		}
		public override string ToString() {
			return Frames == 0 ? string.Empty : (!HasActions(Actions.State) ? Frames.ToString().PadLeft(4, ' ') : string.Empty) + ActionsToString();
		}
		public string ActionsToString() {
			StringBuilder sb = new StringBuilder();
			if (HasActions(Actions.Left)) { sb.Append(",L"); }
			if (HasActions(Actions.Right)) { sb.Append(",R"); }
			if (HasActions(Actions.Up)) { sb.Append(",U"); }
			if (HasActions(Actions.Down)) { sb.Append(",D"); }
			if (HasActions(Actions.Jump)) { sb.Append(",J"); }
			if (HasActions(Actions.Pause)) { sb.Append(",P"); }
			if (HasActions(Actions.Cancel)) { sb.Append(",C"); }
			if (HasActions(Actions.Reset)) { sb.Append(",X"); }
			if (HasActions(Actions.State)) { sb.Append($"@{PosX},{PosY},{Direction}"); }
			return sb.ToString();
		}
		public override bool Equals(object obj) {
			return obj is InputRecord && ((InputRecord)obj) == this;
		}
		public override int GetHashCode() {
			return Frames ^ (int)Actions;
		}
		public static bool operator ==(InputRecord one, InputRecord two) {
			bool oneNull = (object)one == null;
			bool twoNull = (object)two == null;
			if (oneNull != twoNull) {
				return false;
			} else if (oneNull && twoNull) {
				return true;
			}
			return one.Actions == two.Actions && one.PosX == two.PosX && one.PosY == two.PosY;
		}
		public static bool operator !=(InputRecord one, InputRecord two) {
			bool oneNull = (object)one == null;
			bool twoNull = (object)two == null;
			if (oneNull != twoNull) {
				return true;
			} else if (oneNull && twoNull) {
				return false;
			}
			return one.Actions != two.Actions || one.PosX != two.PosX || one.PosY != two.PosY;
		}
	}
}