using JumpKing.MiscSystems.Achievements;
using JumpKing.Player;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
namespace TAS {
	public class FrameState {
		public Vector2 Position;
		public Vector2 Velocity;
		public Vector2 LastVelocity;
		public int Time;
		public float JumpTime;
		public int LastScreen;
		public int TimeStamp;
		public SpriteEffects Direction;
		public bool InWater;
		public bool Knocked;
		public bool OnGround;
		public bool OnIce;
		public bool OnSnow;
		public bool WindEnabled;
		public FrameState(PlayerEntity player) {
			SetValues(player);
		}
		public void SetValues(PlayerEntity player) {
			if (player != null) {
				BodyComp body = player.m_body;
				InWater = body._is_in_water;
				Knocked = body._knocked;
				OnGround = body._is_on_ground;
				OnIce = body._is_on_ice;
				OnSnow = body._is_on_snow;
				WindEnabled = body.m_wind_enabled;
				Position = body.position;
				Velocity = body.velocity;
				LastVelocity = body._last_velocity;
				LastScreen = body.m_last_screen;
				Direction = player.m_flip;
				TimeStamp = player.m_time_stamp;
				JumpTime = player.m_jump.m_timer;
			}
			if (AchievementManager.instance != null) {
				Time = AchievementManager.instance.m_all_time_stats._ticks;
			}
		}
		public void UpdateBody(PlayerEntity player) {
			BodyComp body = player.m_body;
			body._is_in_water = InWater;
			body._knocked = Knocked;
			body._is_on_ground = OnGround;
			body._is_on_ice = OnIce;
			body._is_on_snow = OnSnow;
			body.m_wind_enabled = WindEnabled;
			body.position = Position;
			body.velocity = Velocity;
			body._last_velocity = LastVelocity;
			body.m_last_screen = LastScreen;
			player.m_flip = Direction;
			player.m_time_stamp = TimeStamp;
			player.m_jump.m_timer = JumpTime;
			AchievementManager.instance.m_all_time_stats._ticks = Time;
		}
	}
}