using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using TerrariaApi.Server;
using TShockAPI;

namespace SkeletronPhase2Plugin
{
    [ApiVersion(2, 1)]
    public class SkeletronPhase2Plugin : TerrariaPlugin
    {
        public override string Name => "Skeletron Phase 2 Extra Arm";
        public override string Author => "Nhan Ly";
        public override string Description => "Skeletron Phase 2 mọc thêm tay";
        public override Version Version => new Version(1, 0, 0);

        private bool isSkeletronPhase2Active = false;

        public SkeletronPhase2Plugin(Main game) : base(game) { }

        public override void Initialize()
        {
            ServerApi.Hooks.GameUpdate.Register(this, OnGameUpdate);
        }

        private void OnGameUpdate(EventArgs args)
        {
            bool skeletronAlive = false;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];

                if (npc.active && npc.type == NPCID.SkeletronHead)
                {
                    skeletronAlive = true;
                    float healthRatio = (float)npc.life / npc.lifeMax;

                    if (healthRatio <= 0.5f && !isSkeletronPhase2Active)
                    {
                        isSkeletronPhase2Active = true;
                        SpawnExtraHand(npc);
                        TSPlayer.All.SendTextMessage("Skeletron đã mọc thêm tay và tiến vào Phase 2!", Color.Red);
                    }
                }
            }

            if (!skeletronAlive && isSkeletronPhase2Active)
            {
                isSkeletronPhase2Active = false;
            }
        }

        private void SpawnExtraHand(NPC headNpc)
        {
            int armIndex = NPC.NewNPC(
                headNpc.GetSource_FromAI(),
                (int)headNpc.Center.X,
                (int)headNpc.Center.Y,
                NPCID.SkeletronHand
            );

            if (armIndex < Main.maxNPCs)
            {
                Main.npc[armIndex].ai[1] = headNpc.whoAmI;
                TSPlayer.All.SendData(PacketTypes.NpcUpdate, "", armIndex);
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
