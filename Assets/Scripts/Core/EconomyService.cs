using UnityEngine;

namespace NightCast.Core
{
    public class EconomyService
    {
        public void SellCapture(PlayerState player, FishSpecies fish, float weight)
        {
            float finalPrice = fish.precioBase * weight * player.multiplicadorRacha;
            player.dinero += Mathf.RoundToInt(finalPrice);
            
            player.rachaActual++;
            player.multiplicadorRacha = Mathf.Clamp(1.0f + (player.rachaActual * 0.1f), 1.0f, 5.0f);
        }

        public void HandleEscapeOrBreak(PlayerState player)
        {
            player.rachaActual = 0;
            player.multiplicadorRacha = 1.0f;
        }

        public bool BuyBait(PlayerState player, BaitData bait)
        {
            if (player.dinero >= bait.precio)
            {
                player.dinero -= bait.precio;
                player.inventario.Add(bait.nombre);
                return true;
            }
            return false;
        }

        public bool BuyRod(PlayerState player, RodData rod)
        {
            if (player.dinero >= rod.precio)
            {
                player.dinero -= rod.precio;
                player.inventario.Add(rod.nombre);
                return true;
            }
            return false;
        }

        public bool RepairRod(PlayerState player, ref RodData rod)
        {
            if (rod.durabilidadActual >= rod.durabilidadMax) return false;

            float lostDurability = rod.durabilidadMax - rod.durabilidadActual;
            float damageRatio = lostDurability / rod.durabilidadMax;
            int repairCost = Mathf.RoundToInt(rod.costeReparacion * damageRatio);

            if (repairCost < 1 && lostDurability > 0)
            {
                repairCost = 1;
            }

            if (player.dinero >= repairCost)
            {
                player.dinero -= repairCost;
                rod.durabilidadActual = rod.durabilidadMax;
                return true;
            }
            return false;
        }
    }
}
