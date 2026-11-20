using UnityEngine;

namespace NightCast.Core
{
    public class FishingPhysics
    {
        public enum TensionState
        {
            Optimal,
            Degrading,
            LineBroken,
            RodBroken
        }

        private float _timeInCritical = 0f;

        /// <summary>
        /// Calcula la tension actual del sedal segun la formula del GDD, evitando valores negativos.
        /// </summary>
        public float CalculateTension(float fuerzaPez, float coeficienteFreno, float fuerzaRecogidaJugador, float elasticidadCana)
        {
            float raw = (fuerzaPez * coeficienteFreno) + fuerzaRecogidaJugador - elasticidadCana;
            return Mathf.Max(0f, raw);
        }

        /// <summary>
        /// Evalua umbrales de tension:
        /// 0-75% zona optima (cansa al pez)
        /// 76-99% desgaste acelerado de durabilidad
        /// >=100% durante >1.0s rompe el sedal (pierde pez, cebo y racha)
        /// freno=100% y tension>=150% parte la cana (requiere reparacion)
        /// </summary>
        public TensionState EvaluateTension(float tensionActual, float accionPotenciaCana, float coeficienteFreno, float dt)
        {
            if (accionPotenciaCana <= 0f) accionPotenciaCana = 1f;

            if (coeficienteFreno >= 1.0f && tensionActual >= accionPotenciaCana * 1.5f)
            {
                return TensionState.RodBroken;
            }

            float percentage = tensionActual / accionPotenciaCana;

            if (percentage >= 1.0f)
            {
                _timeInCritical += dt;
                if (_timeInCritical > 1.0f)
                {
                    return TensionState.LineBroken;
                }
                return TensionState.Degrading;
            }
            else
            {
                _timeInCritical = 0f;

                if (percentage >= 0.76f)
                {
                    return TensionState.Degrading;
                }
                return TensionState.Optimal;
            }
        }

        public void ResetTensionState()
        {
            _timeInCritical = 0f;
        }
    }
}
