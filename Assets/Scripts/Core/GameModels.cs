using System.Collections.Generic;

namespace NightCast.Core
{
    [System.Serializable]
    public struct RodData
    {
        public string nombre;
        public float accionPotencia;
        public float elasticidad;
        public float durabilidadMax;
        public float durabilidadActual;
        public float costeReparacion;
        public int precio;
    }

    [System.Serializable]
    public struct BaitData
    {
        public string nombre;
        public string modalidadCompatible;
        public int precio;
        public bool esConsumible;
    }

    [System.Serializable]
    public struct FishSpecies
    {
        public string nombre;
        public float pesoMin;
        public float pesoMax;
        public float fuerzaBase;
        public int precioBase;
        public bool esJefe;
        public string ceboPreferido;
    }

    [System.Serializable]
    public class PlayerState
    {
        public int dinero;
        public int rachaActual;
        public float multiplicadorRacha;
        public RodData equipoEquipadoRod;
        public BaitData equipoEquipadoBait;
        public List<string> inventario;
        public List<string> zonasDesbloqueadas;

        public PlayerState()
        {
            dinero = 0;
            rachaActual = 0;
            multiplicadorRacha = 1.0f;
            inventario = new List<string>();
            zonasDesbloqueadas = new List<string>();
        }
    }
}
