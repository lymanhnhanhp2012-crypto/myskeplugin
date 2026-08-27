using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using TerrariaID;
using TerrariaApi.Server;
using TShockAPI;

namespace SkeletronPhase2Plugin
{
    [ApiVersion(2, 1)]
    public class SkeletronPhase2Plugin : TerrariaPlugin
    {
        public override string Name => "Skeletron Phase 2 Extra Arm & Skulls";
        public override Version Version => new Version(1, 0, 0);

        private bool isSkeletronPhase2Active = false;

        private class CustomSkull
        {
            public int ProjectileIndex;
            public int LifeTimeTicks;
        }

        private List<CustomSkull> activeSkulls = new List<CustomSkull>();

        public SkeletronPhase2Plugin(Main game) : base(game) { }

        public override void Initialize()
        {
            ServerApi.Hooks.GameUpdate.Register(this, OnGameUpdate);
        }

        private void OnGameUpdate(EventArgs args)
        {
            // 1. KIỂM TRA PHASE 2 CỦA SKELETRON
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];

                if (npc.active && npc.type == NPCID.SkeletronHead)
                {
                    float healthRatio = (float)npc.life / npc.lifeMax;

                    if (healthRatio <= 0.5f && !isSkeletronPhase2Active)
                    {
                        isSkeletronPhase2Active = true;
                        
                        // --- HÀM TẠO THÊM 1 CÁNH TAY ---
                        SpawnExtraHand(npc);

                        // --- PHÓNG ĐẦU LÂU 360 ĐỘ ---
                        CastHomingSkullBurst(npc, projectileCount: 16, damage: 45, speed: 7f);

                        TSPlayer.All.SendTextMessage("Skeletron đã mọc thêm tay và tiến vào Phase 2!", Color.Red);
                    }
                }
            }

            // 2. XỬ LÝ ĐẠN ĐẦU LÂU ĐUỔI 10S
            UpdateHomingSkulls();
        }

        // --- TẠO THÊM CÁNH TAY CHO SKELETRON ---
        private void SpawnExtraHand(NPC headNpc)
        {
            // Tạo NPC cánh tay (SkeletronHand) tại vị trí của Đầu Skeletron
            int armIndex = NPC.NewNPC(
                headNpc.GetSource_FromAI(),
                (int)headNpc.Center.X,
                (int)headNpc.Center.Y,
                NPCID.SkeletronHand
            );

            if (armIndex < Main.maxNPCs)
            {
                NPC arm = Main.npc[armIndex];
                
                // Gán ai[1] là ID của Skeletron Head để cánh tay biết dính vào đầu nào
                arm.ai[1] = headNpc.whoAmI;
                
                // Đồng bộ NPC Cánh tay mới vừa tạo cho toàn bộ Server
                TSPlayer.All.SendData(PacketTypes.NpcUpdate, "", armIndex);
            }
        }

        // --- PHÓNG ĐẦU LÂU 360 ĐỘ ---
        private void CastHomingSkullBurst(NPC npc, int projectileCount, int damage, float speed)
        {
            float angleStep = MathHelper.TwoPi / projectileCount;

            for (int i = 0; i < projectileCount; i++)
            {
                float angle = i * angleStep;
                Vector2 velocity = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * speed;

                int projIndex = Projectile.NewProjectile(
                    npc.GetSource_FromAI(),
                    npc.Center,
                    velocity,
                    ProjectileID.Skull,
                    damage,
                    1f,
                    Main.myPlayer
                );

                TSPlayer.All.SendData(PacketTypes.ProjectileNew, "", projIndex);

                activeSkulls.Add(new CustomSkull
                {
                    ProjectileIndex = projIndex,
                    LifeTimeTicks = 0
                });
            }
        }

        // --- CẬP NHẬT ĐẦU LÂU ĐUỔI 10S ---
        private void UpdateHomingSkulls()
        {
            for (int i = activeSkulls.Count - 1; i >= 0; i--)
            {
                CustomSkull skullData = activeSkulls[i];
                Projectile proj = Main.projectile[skullData.ProjectileIndex];

                if (!proj.active || proj.type != ProjectileID.Skull)
                {
                    activeSkulls.RemoveAt(i);
                    continue;
                }

                skullData.LifeTimeTicks++;

                // Hết 10 giây (600 ticks) -> Xóa đạn
                if (skullData.LifeTimeTicks >= 600)
                {
                    proj.Kill();
                    TSPlayer.All.SendData(PacketTypes.ProjectileDestroy, "", skullData.ProjectileIndex);
                    activeSkulls.RemoveAt(i);
                    continue;
                }

                // Đuổi theo Player gần nhất
                Player target = TSPlayer.FindClosestPlayer(proj.Center, 2000, 2000);
                if (target != null && target.active && !target.dead)
                {
                    Vector2 targetDirection = target.Center - proj.Center;
                    targetDirection.Normalize();

                    float speed = 7f;
                    float turnInertia = 20f;

                    proj.velocity = (proj.velocity * (turnInertia - 1) + targetDirection * speed) / turnInertia;

                    TSPlayer.All.SendData(PacketTypes.ProjectileNew, "", skullData.ProjectileIndex);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.GameUpdate.Deregister(this, OnGameUpdate);
            }
            base.Dispose(disposing);
        }
    }
}

