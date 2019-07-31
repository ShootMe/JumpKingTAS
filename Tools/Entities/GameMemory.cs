using System;
using System.Diagnostics;
namespace TASStudio.Entities {
	//.load C:\Windows\Microsoft.NET\Framework\v4.0.30319\SOS.dll
	public class GameMemory {
		private static ProgramPointer TAS = new ProgramPointer(AutoDeref.Single, new ProgramSignature(PointerVersion.Steam, "C745F8F1DEBC9AC745FC785634128D4DF8", 24));
		public Process Program { get; set; }
		public bool IsHooked { get; set; } = false;
		private DateTime lastHooked;

		public GameMemory() {
			lastHooked = DateTime.MinValue;
		}

		public string TASOutput() {
			return TAS.Read(Program, 0x0, 0x0);
		}
		public string TASPlayerOutput() {
			return TAS.Read(Program, 0x4, 0x0);
		}
		public bool HookProcess() {
			IsHooked = Program != null && !Program.HasExited;
			if (!IsHooked && DateTime.Now > lastHooked.AddSeconds(1)) {
				lastHooked = DateTime.Now;
				Process[] processes = Process.GetProcessesByName("JumpKing");
				Program = processes != null && processes.Length > 0 ? processes[0] : null;

				if (Program != null && !Program.HasExited) {
					MemoryReader.Update64Bit(Program);
					IsHooked = true;
				}
			}

			return IsHooked;
		}
		public void Dispose() {
			if (Program != null) {
				Program.Dispose();
			}
		}
	}
}