using JumpKing;
using JumpKing.Controller;
using JumpKing.GameManager;
using JumpKing.MiscSystems.Achievements;
using JumpKing.Player;
using JumpKing.SaveThread;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
namespace TAS {
	[Flags]
	public enum State {
		None = 0,
		Enable = 1,
		FrameStep = 2,
		Disable = 4,
		StepBack = 8
	}
	public class Manager {
		public static bool Running;
		private static InputController controller;
		public static State state, nextState;
		public static string CurrentStatus, PlayerStatus;
		public static int FrameStepCooldown, FrameLoops;
		private static bool frameStepWasDpadUp, frameStepWasDpadDown, frameStepWasLeftStick;
		private static CultureInfo enUS;
		private static KeyboardState kbState;
		private static GamePadState padState;
		private static List<FrameState> saves;
		static Manager() {
			FrameLoops = 1;
			enUS = CultureInfo.CreateSpecificCulture("en-US");
			controller = new InputController("JumpKing.tas");
			long lookHere = 0x123456789abcdef1;
			CurrentStatus = lookHere.ToString();
			saves = new List<FrameState>();
		}
		private static bool IsKeyDown(Keys key) {
			return kbState.IsKeyDown(key);
		}
		public static bool IsLoading() {
			return false;
		}
		public static PadState GetPadState() {
			return controller.GetPadState();
		}
		public static PadState GetPressed() {
			return controller.GetPressed();
		}
		private static void UpdatePlayerInfo() {
			PlayerEntity player = GameLoop.m_player;
			if (player != null) {
				SaveState save = player.GetSaveState();

				StringBuilder sb = new StringBuilder();
				sb.AppendLine($"Pos: {save.position.X.ToString("0.00", enUS)},{save.position.Y.ToString("0.00", enUS)}");
				sb.AppendLine($"Vel: {save.velocity.X.ToString("0.00", enUS)},{save.velocity.Y.ToString("0.00", enUS)}");
				sb.AppendLine($"Dir: {save.direction} Time: {AchievementManager.instance.GetCurrentStats().timeSpan.TotalSeconds.ToString("0.000")}");
				if (save.is_on_ground) {
					sb.AppendLine("Ground");
				}
				PlayerStatus = sb.ToString().TrimEnd();
			} else {
				PlayerStatus = null;
			}
		}
		public static void UpdateInputs() {
			UpdatePlayerInfo();
			kbState = Keyboard.GetState();
			padState = GamePad.GetState(0);
			HandleFrameRates();
			CheckControls();
			FrameStepping();

			if (HasFlag(state, State.Enable)) {
				Running = true;

				if (!HasFlag(state, State.FrameStep)) {
					bool fastForward = controller.HasFastForward;
					controller.PlaybackPlayer();

					if (fastForward && (!controller.HasFastForward || controller.Current.ForceBreak && controller.CurrentInputFrame == controller.Current.Frames)) {
						nextState |= State.FrameStep;
						FrameLoops = 1;
					}

					if (controller.Current != null) {
						if (controller.Current.HasActions(Actions.Reset)) {
							AchievementManager.instance.m_all_time_stats.attempts = 1;
							AchievementManager.instance.m_all_time_stats.falls = 0;
							AchievementManager.instance.m_all_time_stats.jumps = 0;
							AchievementManager.instance.m_all_time_stats.session = 0;
							AchievementManager.instance.m_all_time_stats._ticks = 0;
							AchievementManager.instance.m_snapshot.attempts = 1;
							AchievementManager.instance.m_snapshot.falls = 0;
							AchievementManager.instance.m_snapshot.jumps = 0;
							AchievementManager.instance.m_snapshot.session = 0;
							AchievementManager.instance.m_snapshot._ticks = 2;
						} else if (controller.Current.HasActions(Actions.State) && GameLoop.m_player != null) {
							GameLoop.m_player.ApplySaveState(new SaveState() {
								direction = controller.Current.Direction == 1 ? 1 : -1,
								position = new Vector2((float)controller.Current.PosX / 100f, (float)controller.Current.PosY / 100f),
								velocity = new Vector2(0, 0.26f)
							});
							Camera.UpdateCamera(GameLoop.m_player.m_body.GetHitbox().Center);
						}
					}

					if (saves.Count < controller.CurrentFrame) {
						saves.Add(new FrameState(GameLoop.m_player));
					} else {
						saves[controller.CurrentFrame - 1].SetValues(GameLoop.m_player);
					}

					if (!controller.CanPlayback) {
						DisableRun();
					}
				}

				string status = controller.Current.Line + "[" + controller.ToString() + "]";
				CurrentStatus = status;
			} else {
				Running = false;
				CurrentStatus = null;
			}
		}
		private static void HandleFrameRates() {
			if (HasFlag(state, State.Enable) && !HasFlag(state, State.FrameStep) && !HasFlag(nextState, State.FrameStep)) {
				if (controller.HasFastForward) {
					FrameLoops = controller.FastForwardSpeed;
					return;
				}

				float rightStickX = padState.ThumbSticks.Right.X;
				if (IsKeyDown(Keys.RightShift) && IsKeyDown(Keys.RightControl)) {
					rightStickX = 1f;
				}

				if (rightStickX <= 0.2) {
					FrameLoops = 1;
				} else if (rightStickX <= 0.3) {
					FrameLoops = 2;
				} else if (rightStickX <= 0.4) {
					FrameLoops = 3;
				} else if (rightStickX <= 0.5) {
					FrameLoops = 4;
				} else if (rightStickX <= 0.6) {
					FrameLoops = 5;
				} else if (rightStickX <= 0.7) {
					FrameLoops = 6;
				} else if (rightStickX <= 0.8) {
					FrameLoops = 7;
				} else if (rightStickX <= 0.9) {
					FrameLoops = 8;
				} else {
					FrameLoops = 9;
				}
			} else {
				FrameLoops = 1;
			}
		}
		private static void FrameStepping() {
			bool dpadUp = padState.DPad.Up == ButtonState.Pressed || (IsKeyDown(Keys.OemCloseBrackets) && !IsKeyDown(Keys.RightControl));
			bool dpadDown = padState.DPad.Down == ButtonState.Pressed || (IsKeyDown(Keys.OemOpenBrackets) && !IsKeyDown(Keys.RightControl));
			bool leftStick = padState.Buttons.LeftStick == ButtonState.Pressed;

			if (HasFlag(state, State.Enable)) {
				if (HasFlag(nextState, State.FrameStep)) {
					state |= State.FrameStep;
					nextState &= ~State.FrameStep;
				}

				if (!dpadUp && frameStepWasDpadUp) {
					if (!HasFlag(state, State.FrameStep)) {
						state |= State.FrameStep;
						nextState &= ~State.FrameStep;
					} else {
						state &= ~State.FrameStep;
						nextState |= State.FrameStep;
						controller.ReloadPlayback();
					}
					FrameStepCooldown = 60;
				} else if (!dpadDown && frameStepWasDpadDown) {
					StepBack();
				} else if (!leftStick && frameStepWasLeftStick) {
					state &= ~State.FrameStep;
					nextState &= ~State.FrameStep;
					controller.ReloadPlayback();
				} else if (HasFlag(state, State.FrameStep) && (padState.ThumbSticks.Right.X > 0.1 || (IsKeyDown(Keys.RightShift) && IsKeyDown(Keys.RightControl)))) {
					float rStick = padState.ThumbSticks.Right.X;
					if (rStick < 0.1f) {
						rStick = 0.5f;
					}
					FrameStepCooldown -= (int)((rStick - 0.1) * 80f);
					if (FrameStepCooldown <= 0) {
						FrameStepCooldown = 60;
						state &= ~State.FrameStep;
						nextState |= State.FrameStep;
						controller.ReloadPlayback();
					}
				} else if (HasFlag(state, State.FrameStep) && padState.ThumbSticks.Right.X < -0.1) {
					float rStick = padState.ThumbSticks.Right.X;
					FrameStepCooldown += (int)((rStick + 0.1) * 80f);
					if (FrameStepCooldown <= 0) {
						FrameStepCooldown = 60;
						StepBack();
					}
				}
			}

			frameStepWasDpadUp = dpadUp;
			frameStepWasDpadDown = dpadDown;
			frameStepWasLeftStick = leftStick;
		}
		private static void StepBack() {
			if (controller.CurrentFrame > 2 && controller.CanPlayback) {
				state &= ~State.FrameStep;
				nextState |= State.FrameStep;
				controller.ReloadPlayback();
				controller.CurrentFrame -= 2;
				controller.ReloadPlayback();
				if (GameLoop.m_player != null && controller.CurrentFrame >= 0 && controller.CurrentFrame < saves.Count) {
					saves[controller.CurrentFrame].UpdateBody(GameLoop.m_player);
					Camera.UpdateCamera(GameLoop.m_player.m_body.GetHitbox().Center);
				}
			}
		}
		private static void CheckControls() {
			bool openBracket = IsKeyDown(Keys.RightControl) && IsKeyDown(Keys.OemOpenBrackets);
			bool rightStick = padState.Buttons.RightStick == ButtonState.Pressed || openBracket;

			if (rightStick) {
				if (!HasFlag(state, State.Enable)) {
					nextState |= State.Enable;
				} else {
					nextState |= State.Disable;
				}
			} else if (HasFlag(nextState, State.Enable)) {
				EnableRun();
			} else if (HasFlag(nextState, State.Disable)) {
				DisableRun();
			}
		}
		private static void DisableRun() {
			Running = false;
			state = State.None;
			nextState = State.None;
			saves.Clear();
		}
		private static void EnableRun() {
			nextState &= ~State.Enable;
			UpdateVariables(false);
		}
		private static void UpdateVariables(bool recording) {
			state |= State.Enable;
			state &= ~State.FrameStep;
			controller.InitializePlayback();
			Running = true;
		}
		private static bool HasFlag(State state, State flag) {
			return (state & flag) == flag;
		}
	}
}
