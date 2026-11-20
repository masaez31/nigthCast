using UnityEngine;

namespace NightCast.Core
{
    public class FishAI
    {
        public enum FSMState
        {
            Idle,
            Bite,
            Fighting_Run,
            Fighting_Headshake,
            Fighting_Tired,
            Resolved
        }

        public enum Resolution
        {
            None,
            Caught,
            Escaped,
            LineBroken,
            RodBroken
        }

        public FSMState CurrentState { get; private set; }
        public Resolution CurrentResolution { get; private set; }
        public FishSpecies Species { get; private set; }

        private float _stateTimer;
        private float _stamina;

        public FishAI(FishSpecies species)
        {
            Species = species;

            // 10% RNG de "Picada Monstruosa" solo si no viene ya forzado un jefe
            if (!species.esJefe && Random.value <= 0.10f)
            {
                Species = new FishSpecies
                {
                    nombre = "El Gran Sargo Real",
                    pesoMin = 3.0f,
                    pesoMax = 5.5f,
                    fuerzaBase = 75.0f,
                    precioBase = 120,
                    esJefe = true,
                    ceboPreferido = species.ceboPreferido
                };
            }

            CurrentState = FSMState.Idle;
            CurrentResolution = Resolution.None;

            _stamina = Species.fuerzaBase * 6f;
            _stateTimer = Random.Range(2.0f, 4.5f);
        }

        public void AccelerateBite(float seconds)
        {
            if (CurrentState == FSMState.Idle)
            {
                _stateTimer -= seconds;
                if (_stateTimer < 0.25f) _stateTimer = 0.25f;
            }
        }

        public void Tick(float dt)
        {
            if (CurrentState == FSMState.Resolved) return;

            _stateTimer -= dt;

            switch (CurrentState)
            {
                case FSMState.Idle:
                    if (_stateTimer <= 0f)
                    {
                        CurrentState = FSMState.Bite;
                        // Ventana de hooking justa y jugable
                        _stateTimer = Random.Range(1.5f, 2.5f);
                    }
                    break;

                case FSMState.Bite:
                    if (_stateTimer <= 0f)
                    {
                        Resolve(Resolution.Escaped);
                    }
                    break;

                case FSMState.Fighting_Run:
                case FSMState.Fighting_Headshake:
                case FSMState.Fighting_Tired:
                    ProcessFightingState();
                    break;
            }
        }

        private void ProcessFightingState()
        {
            if (_stateTimer <= 0f)
            {
                if (_stamina <= 0f)
                {
                    CurrentState = FSMState.Fighting_Tired;
                    _stateTimer = 4.0f;
                    // Recupera un pequeno respiro tras descansar para que el combate siga vivo
                    _stamina = Species.fuerzaBase * 1.5f;
                }
                else
                {
                    float roll = Random.value;
                    if (roll < 0.45f)
                    {
                        CurrentState = FSMState.Fighting_Run;
                        _stateTimer = Random.Range(1.5f, 3.0f);
                    }
                    else if (roll < 0.80f)
                    {
                        CurrentState = FSMState.Fighting_Headshake;
                        _stateTimer = Random.Range(1.5f, 2.5f);
                    }
                    else
                    {
                        CurrentState = FSMState.Fighting_Tired;
                        _stateTimer = Random.Range(2.0f, 3.0f);
                    }
                }
            }
        }

        public bool TryHook()
        {
            if (CurrentState == FSMState.Bite)
            {
                CurrentState = FSMState.Fighting_Run;
                _stateTimer = 2.0f;
                return true;
            }

            Resolve(Resolution.Escaped);
            return false;
        }

        public void DrainStamina(float amount)
        {
            if (CurrentState == FSMState.Fighting_Run || CurrentState == FSMState.Fighting_Headshake)
            {
                _stamina -= amount;
                if (_stamina < 0f) _stamina = 0f;
            }
        }

        public void Resolve(Resolution resolution)
        {
            CurrentState = FSMState.Resolved;
            CurrentResolution = resolution;
        }
    }
}
