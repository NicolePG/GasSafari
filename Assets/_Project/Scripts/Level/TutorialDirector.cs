using System.Collections;
using System.Collections.Generic;
using GasSafari.Audio;
using GasSafari.Characters;
using GasSafari.Core;
using GasSafari.Raft;
using GasSafari.Team;
using GasSafari.UI;
using UnityEngine;
using UnityEngine.Playables;

namespace GasSafari.Level
{
    // T-0 (docs/04 T-0, docs/07 §8): lleva las lecciones en orden, muestra el cartel de cada una al entrar en su zona,
    // suena "checkpoint" al cumplirla y empieza la siguiente. La lección del Súper Pedo llena el medidor al 100 % en 1 s
    // al entrar en su zona y, al lanzarlo, reproduce la toma de Timeline que sigue a la balsa por encima del muro.
    public class TutorialDirector : MonoBehaviour
    {
        [SerializeField] private TutorialLesson[] lessons = System.Array.Empty<TutorialLesson>();
        [SerializeField] private TutorialCard card;
        [Tooltip("Toma de 2 s del salto del muro (lección 10)")] [SerializeField] private PlayableDirector superFartShot;
        [Tooltip("Cámara de la toma: sigue a la balsa")] [SerializeField] private Cinemachine.CinemachineVirtualCamera shotCamera;
        [Tooltip("s que tarda en llenarse el medidor en la lección 10 (docs/04)")] [SerializeField] private float meterFillTime = 1f;
        [Tooltip("s tras un Súper Pedo fallido antes de volver a llenar el medidor")] [SerializeField] private float refillDelay = 2f;

        private RaftController raft;
        private TeamMeter meter;
        private PingSystem pings;
        private Coroutine filling;
        private float lastSuperFart = -10f;
        private bool shotPlayed;

        public int CurrentIndex { get; private set; }
        public TutorialLesson Current => CurrentIndex < lessons.Length ? lessons[CurrentIndex] : null;
        public IReadOnlyList<TutorialLesson> Lessons => lessons;
        public bool IsFinished => CurrentIndex >= lessons.Length;
        public TutorialCard Card => card;

        public void Configure(TutorialLesson[] levelLessons, TutorialCard tutorialCard, PlayableDirector shot, Cinemachine.CinemachineVirtualCamera camera)
        {
            lessons = levelLessons;
            card = tutorialCard;
            superFartShot = shot;
            shotCamera = camera;
        }

        private void OnEnable()
        {
            for (int i = 0; i < lessons.Length; i++)
            {
                lessons[i].Entered += OnLessonEntered;
                lessons[i].Completed += OnLessonCompleted;
            }
            GameEvents.OnSuperFart += OnSuperFart;
            GameEvents.OnRespawn += OnRespawn;
        }

        private void OnDisable()
        {
            for (int i = 0; i < lessons.Length; i++)
            {
                lessons[i].Entered -= OnLessonEntered;
                lessons[i].Completed -= OnLessonCompleted;
            }
            GameEvents.OnSuperFart -= OnSuperFart;
            GameEvents.OnRespawn -= OnRespawn;
            if (pings != null)
                pings.CountdownTick -= OnCountdown;
        }

        // La balsa la crea RaftSpawner al empezar el nivel; las pruebas pueden atar una balsa propia.
        public void Bind(RaftController levelRaft, IReadOnlyList<PlayerAvatar> crew)
        {
            raft = levelRaft;
            meter = raft.GetComponent<TeamMeter>();
            if (pings != null)
                pings.CountdownTick -= OnCountdown;
            pings = raft.GetComponent<PingSystem>();
            if (pings != null)
                pings.CountdownTick += OnCountdown;
            for (int i = 0; i < lessons.Length; i++)
                lessons[i].Bind(crew, raft);
            if (shotCamera != null)
            {
                shotCamera.Follow = raft.transform;
                shotCamera.LookAt = raft.transform;
            }
            CurrentIndex = 0;
            BeginCurrent(true);
        }

        private void Update()
        {
            if (raft == null)
            {
                var manager = LevelManager.Current;
                if (manager != null && manager.Raft != null)
                    Bind(manager.Raft, manager.Avatars);
                return;
            }
            KeepMeterReady();
        }

        private void BeginCurrent(bool showCard)
        {
            var lesson = Current;
            if (lesson == null)
                return;
            lesson.Begin();
            if (showCard)
                ShowCard(lesson);
        }

        private void ShowCard(TutorialLesson lesson)
        {
            if (card != null)
                card.Show(lesson.Step, Services.IsReady && Services.Session != null ? Services.Session.Players : null);
        }

        private void OnLessonEntered(TutorialLesson lesson)
        {
            int index = System.Array.IndexOf(lessons, lesson);
            // Una lección saltada por otro camino (p. ej. romper la pared de globos de un golpe) cuenta como cumplida.
            while (index > CurrentIndex && Current != null)
                Current.Complete();
            if (lesson != Current)
                return;
            ShowCard(lesson);
            if (lesson.Step.fillTeamMeter)
                FillMeter();
        }

        private void OnLessonCompleted(TutorialLesson lesson)
        {
            if (lesson != Current)
                return;
            if (card != null)
                card.Hide();
            if (Services.IsReady)
                Services.Audio.Play(SoundId.Checkpoint, raft != null ? raft.transform.position : transform.position);
            CurrentIndex++;
            // La siguiente empieza ya; su cartel sale al entrar en su zona (o enseguida si la balsa ya está dentro).
            var next = Current;
            BeginCurrent(next != null && next.RaftInZone);
        }

        private void OnCountdown(int value)
        {
            var lesson = Current;
            if (lesson != null)
                lesson.OnCountdown(value);
        }

        private void OnRespawn()
        {
            var lesson = Current;
            if (lesson != null)
                lesson.ResetProgress();
        }

        // ---------------------------------------------------------------- lección 10
        private void FillMeter()
        {
            if (meter == null || meter.IsFull || filling != null)
                return;
            filling = StartCoroutine(FillRoutine());
        }

        private IEnumerator FillRoutine()
        {
            float from = meter.Value;
            float t = 0f;
            while (t < meterFillTime)
            {
                t += Time.deltaTime;
                meter.Set(Mathf.Lerp(from, meter.Max, t / meterFillTime));
                yield return null;
            }
            meter.Set(meter.Max);
            filling = null;
        }

        // Si el salto falla y la balsa vuelve al agua dentro de la zona, el medidor se llena otra vez.
        private void KeepMeterReady()
        {
            var lesson = Current;
            if (lesson == null || !lesson.Step.fillTeamMeter || !lesson.RaftInZone || raft.Buoyancy.IsAirborne)
                return;
            if (Time.time - lastSuperFart >= refillDelay)
                FillMeter();
        }

        private void OnSuperFart(float power)
        {
            lastSuperFart = Time.time;
            if (shotPlayed || superFartShot == null)
                return;
            // OnSuperFart llega también a la lección, que puede completarse antes que esto: se mira la lección del muro.
            for (int i = 0; i < lessons.Length; i++)
            {
                if (lessons[i].Step.condition != TutorialCondition.SuperFart)
                    continue;
                if (lessons[i].IsActive || (lessons[i].IsComplete && CurrentIndex == i + 1))
                {
                    shotPlayed = true;
                    superFartShot.time = 0;
                    superFartShot.Play();
                }
                return;
            }
        }
    }
}
