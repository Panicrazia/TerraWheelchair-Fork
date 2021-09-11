﻿using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TerraWheelchair.Buffs;

namespace TerraWheelchair.Projectiles
{
	public class WheelchairProj : ModProjectile
	{
		// Vector2 localOldPosition;
		// Vector2 localOldVelocity;
		bool localHolding;
		bool oldLocalHolding;

		public int Holder { get => projectile.owner;  }
		public int AI_Target
		{
			get => (int)projectile.ai[0];
			set => projectile.ai[0] = value;
		}
		public float AI_Hopping
		{
			get => projectile.ai[1];
			set => projectile.ai[1] = value;
		}

		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Wheelchair");
			DisplayName.AddTranslation(Terraria.Localization.GameCulture.Chinese, "轮椅");
			// Main.projectileFrameCount[projectile.type] = 2;
			// projectileID.Sets.MustAlwaysDraw[projectile.type] = true;
			Main.projFrames[projectile.type] = 2;
			ProjectileID.Sets.NeedsUUID[projectile.type] = true;
		}

		public override void SetDefaults()
		{
			projectile.penetrate = -1;
			projectile.width = 40;
			projectile.height = 32;
			projectile.aiStyle = -1;
			projectile.friendly = true;
			projectile.Hitbox = new Rectangle(0, 0, 32, 32);
			AI_Hopping = 0;
			AI_Target = -1;
		}

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
			drawOffsetX = projectile.spriteDirection == 1 ? 0 : -4;
            return base.PreDraw(spriteBatch, lightColor);
        }

        public override void PostDraw(SpriteBatch spriteBatch, Color drawColor)
		{
			if (localHolding != oldLocalHolding)
			{
				if (localHolding)
					Main.PlaySound(SoundID.Item, -1, -1, 1, 1);
				else // if (Main.player[Holder].itemAnimation == 0)
				{
					Main.PlaySound(SoundID.Item, -1, -1, 1, 1, -100.1f);
				}
			}
			oldLocalHolding = localHolding;
			// CheckCollide(localOldVelocity, localOldPosition);
			// UpdatePlayerPosition();
		}

		public override void AI()
		{
			if (!projectile.active) return;
			Lighting.AddLight(projectile.Center, 0.3f, 0.3f, 0.3f);

			WheelchairPlayer owner = Main.player[Holder].GetModPlayer<WheelchairPlayer>();
			if (!owner.player.active)
			{
				// Main.NewText(String.Format("{0} {1}", Holder,))
				owner.player.ClearBuff(ModContent.BuffType<WheelchairBuff>());
				// projectile.Kill();
				return;
			}
			if (owner.IsLocalPlayer)
			{
				if (owner.player.HasBuff(ModContent.BuffType<WheelchairBuff>()))
				{
					projectile.timeLeft = 5;
					owner.wheelchairUUID = projectile.identity;
				}
			}
			else if (owner.wheelchairUUID != -1)
			{
				projectile.timeLeft = 5;
			}
			/* 
			foreach (projectile p in Main.projectile)
				if (p.active && p.type == projectile.type && p.netID != projectile.netID && (p.modprojectile as Wheelchairprojectile).AI_Holder == AI_Holder)
				{
					if (p.Distance(owner.player.Center) < projectile.Distance(owner.player.Center))
					{
						projectile.active = false;
						return;
					}
					else
					{
						p.active = false;
					}
				} */

			WheelchairPlayer target = CheckTarget();

			localHolding = false;
			projectile.velocity.X *= 0.99f;
			projectile.velocity.Y = projectile.velocity.Y + 0.4f + AI_Hopping;
			AI_Hopping *= 0.3f;

			if (projectile.velocity.Y > 16f)
			{
				projectile.velocity.Y = 16f;
			}
			if (target != null && target.player.whoAmI == owner.player.whoAmI)
			{
				// auto running mode
				projectile.spriteDirection = -owner.player.direction;
				projectile.velocity.X = (projectile.velocity.X * owner.player.direction > 0 ? projectile.velocity.X * 0.7f : 0) + 0.5f * owner.player.direction;
				owner.holdingWheelchair = false;
			}
			else
			{
				Vector2 ownerHand = owner.player.Center + new Vector2(owner.player.direction * 0f, 5);
				bool canHold = (owner.player.itemAnimation == 0 && owner.localHoldingChairItem && (projectile.Center - ownerHand).Length() < 42f);
				if (!owner.IsLocalPlayer) canHold = owner.holdingWheelchair;
				else owner.holdingWheelchair = canHold;
				if (canHold)
				{
					localHolding = true;
					owner.player.bodyFrame.Y = owner.player.bodyFrame.Height * 3;
					projectile.spriteDirection = -owner.player.direction;
					projectile.velocity = owner.player.velocity;
					Vector2 vec = (ownerHand + new Vector2(owner.player.direction * 26f, 0f) - projectile.Center);
					projectile.velocity = projectile.velocity * 0.8f + vec * 0.2f; // - owner.player.velocity;
					var force = projectile.velocity.Length();
					if (force > 14f)
						projectile.velocity *= 14f / force;
				}
			}

			if (target != null)
			{
				if (target.player.mount.Active)
				{
					projectile.active = false;
					return;
				}
			}
			/* projectile.frameCounter += ((projectile.oldVelocity.Y == 0 ? 1.1 : 1) * Math.Abs(projectile.velocity.X));
			if (projectile.frame.Height > 0)
				projectile.frame.Y = (projectile.frame.Y + (int)(projectile.frameCounter / 5f) * projectile.frame.Height) % (Main.projectileFrameCount[projectile.type] * projectile.frame.Height);
			projectile.frameCounter -= 5 * (int)(projectile.frameCounter / 5); */
			projectile.frameCounter += (int)(100 * ((projectile.oldVelocity.Y == 0 ? 1.1 : 1) * Math.Abs(projectile.velocity.X)) );
			projectile.frame = (projectile.frame + (int)(projectile.frameCounter / 500f)) % Main.projFrames[projectile.type];
			projectile.frameCounter -= 500 * (int)(projectile.frameCounter / 500f);
			projectile.rotation *= 0.9f;
			// projectile.direction = 1;

			projectile.netUpdate = owner.IsLocalPlayer;

			UpdatePlayerPosition();
			// localOldPosition = projectile.position;
			// localOldVelocity = projectile.velocity;
		}

        public override bool PreKill(int timeLeft)
        {
			WheelchairPlayer owner = Main.player[Holder].GetModPlayer<WheelchairPlayer>();
			owner.wheelchairUUID = -1;
			ReleaseTarget(CheckTarget());
			return base.PreKill(timeLeft);
        }


        public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough)
        {
			if (localHolding)
				fallThrough = true;
			else
				fallThrough = false;
            return base.TileCollideStyle(ref width, ref height, ref fallThrough);
        }
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
			WheelchairPlayer owner = Main.player[Holder].GetModPlayer<WheelchairPlayer>();
			if (projectile.velocity.Y == 0 && projectile.oldVelocity.Y != 0 && oldVelocity.Y > 4)
			{
				for (int i = 0; i < 15; i++)
					_ = Dust.NewDust(projectile.position + new Vector2(0, 25), projectile.width, 1, DustID.Fire);
				Main.PlaySound(SoundID.Tink, -1, -1, 1, 1, 0.3f);
				Main.PlaySound(SoundID.Item, -1, -1, 52, 0.5f, 100.3f);
			}
			// Main.NewText(String.Format("{0} {1}", projectile.velocity, projectile.oldVelocity));
			if (projectile.velocity.X == 0 && oldVelocity.X != 0 && projectile.oldVelocity.X != 0)
			{
				if (Math.Abs(AI_Hopping) <= 0.00001f)
				{
					AI_Hopping = projectile.velocity.Y == 0 ? -3.0f : -1.0f;
					projectile.rotation = projectile.spriteDirection * 0.5f;
					if (owner.player.whoAmI != AI_Target)
					{
						// not auto running
						if (!localHolding)
							projectile.velocity.X = -0.1f * oldVelocity.X;
						if (projectile.velocity.X != 0)
							projectile.spriteDirection = projectile.velocity.X < 0 ? 1 : -1;
					}
					else
					{
						// auto running
						AI_Hopping = -3.0f;
					}
					if (Math.Abs(projectile.oldVelocity.X) > 2f)
					{
						// high speed collision
						for (int i = 0; i < 5; i++)
							_ = Dust.NewDust(projectile.position + new Vector2(oldVelocity.X < 0 ? 0 : projectile.width - 3, 25), 3, 1, DustID.Fire);
						Main.PlaySound(SoundID.Tink, -1, -1, 1, 1, 0.3f);
						Main.PlaySound(SoundID.Item, -1, -1, 52, 0.5f, 100.3f);
						for (int i = 0; i < 10; i++)
							_ = Dust.NewDust(projectile.position, projectile.width, projectile.height, DustID.Stone);
					}
					else
					{
						projectile.rotation = projectile.spriteDirection * 0.3f;
						Main.PlaySound(SoundID.Tink, -1, -1, 1, 0.6f, 0.3f);
						Dust.NewDust(projectile.position + new Vector2(oldVelocity.X < 0 ? 0 : projectile.width - 3, 25), 3, 1, DustID.Fire);
						Dust.NewDust(projectile.position, projectile.width, projectile.height, DustID.Stone);
					}
				}
			}
			// UpdatePlayerPosition();
			return false;
		}

		private WheelchairPlayer CheckTarget()
		{
			WheelchairPlayer target = null;
			if (AI_Target != -1 && AI_Target < Main.player.Length)
				target = Main.player[AI_Target].GetModPlayer<WheelchairPlayer>();
			if (target != null)
				if (!target.player.active || target.player.dead || target.player.mount.Active || !target.UpdatePrescription() || target.onChairUUID != projectile.identity)
				{
					// target not valid anymore
					ReleaseTarget(target);
					target = null;
					AI_Target = -1;
				}
			// keep old target
			if (target != null)
				return target; 
			// find new target
			foreach (Player p in Main.player)
				if (p.active && !p.dead && !p.mount.Active && p.Distance(projectile.Center) < 50f)
				{
					if (p.GetModPlayer<WheelchairPlayer>().onChairUUID == -1 && p.GetModPlayer<WheelchairPlayer>().UpdatePrescription())
					{
						target = p.GetModPlayer<WheelchairPlayer>();
						target.onChairUUID = projectile.identity;
						AI_Target = p.whoAmI;
						return target;
					}
				}
			AI_Target = -1;
			return null;
		}

		private void ReleaseTarget(WheelchairPlayer target)
		{
			if (target == null)
				return;
			target.OffWheelchair();
		}

		private void UpdatePlayerPosition()
		{
			WheelchairPlayer target = CheckTarget();
			if (target == null)
			{
				return;
			}
			target.player.direction = -projectile.spriteDirection;

			target.player.position = projectile.position + projectile.velocity * 0.01f + new Vector2((-target.player.width + projectile.width) * 0.5f + target.player.direction * 5, projectile.height - target.player.height - 5f);
			target.player.velocity = new Vector2(0f, -1f);// projectile.velocity + new Vector2(0f, 0f);

			target.player.fullRotationOrigin = new Vector2(11, 22);
			target.player.fullRotation = projectile.rotation;

			if (Main.clientPlayer.whoAmI == target.player.whoAmI)
			{
				Main.SetCameraLerp(0.07f, 5);
			}
			if (projectile.velocity.Y <= 0f)
			{
				target.player.fallStart = target.player.fallStart2 = (int)(target.player.position.Y / 16f);
			}
		}
	}
}
