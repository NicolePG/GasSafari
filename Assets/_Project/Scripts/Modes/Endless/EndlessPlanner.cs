using System;
using System.Collections.Generic;

namespace GasSafari.Modes
{
    // Lo que el planificador necesita saber de un tramo (EndlessChunkData). Dificultad 0 = solo para la salida.
    public struct ChunkSpec
    {
        public string Id;
        public int World;
        public int Difficulty;
        public float Length;
        // Giro neto en grados de la variante normal (positivo = a la derecha). Con giro hay variante espejada.
        public float Turn;
    }

    // Un tramo elegido: cuál, si va espejado, dónde empieza y con qué dificultad se pidió.
    public struct ChunkPick
    {
        public int Spec;
        public bool Mirrored;
        public float Start;
        public int Tier;

        public override string ToString() => Spec + (Mirrored ? "L" : "R") + "@" + Start.ToString("0", System.Globalization.CultureInfo.InvariantCulture);
    }

    // Elige la secuencia de tramos de Infinito y del Desafío Diario (docs/06 §1) con un System.Random propio: la misma
    // semilla da siempre el mismo recorrido. La dificultad pedida sube 1 cada 'difficultyStep' m (1 a 5); el rumbo se
    // mantiene dentro de ±maxHeading eligiendo la variante del giro, así el río nunca se vuelve sobre sí mismo.
    public sealed class EndlessPlanner
    {
        public const int MAX_TIER = 5;
        private const int WEIGHT_TIER = 3;
        private const int WEIGHT_BELOW = 1;

        private readonly IReadOnlyList<ChunkSpec> specs;
        private readonly Random rng;
        private readonly float difficultyStep;
        private readonly float maxHeading;
        private readonly List<int> candidates = new List<int>();
        private readonly List<int> weights = new List<int>();
        private int last = -1;

        public float Distance { get; private set; }
        public float Heading { get; private set; }
        public int Count { get; private set; }

        public EndlessPlanner(IReadOnlyList<ChunkSpec> chunkSpecs, int seed, float step, float headingLimit)
        {
            specs = chunkSpecs;
            rng = new Random(seed);
            difficultyStep = Math.Max(1f, step);
            maxHeading = headingLimit;
        }

        public int TierAt(float distance) => Math.Min(MAX_TIER, 1 + (int)(distance / difficultyStep));

        public ChunkPick Next()
        {
            int tier = TierAt(Distance);
            int index = Count == 0 ? FindStart() : Choose(tier);
            var spec = specs[index];
            bool mirrored = false;
            if (spec.Turn != 0f)
            {
                mirrored = rng.Next(2) == 1;
                float heading = Heading + (mirrored ? -spec.Turn : spec.Turn);
                if (Math.Abs(heading) > maxHeading)
                    mirrored = !mirrored;
            }
            var pick = new ChunkPick { Spec = index, Mirrored = mirrored, Start = Distance, Tier = tier };
            Heading += mirrored ? -spec.Turn : spec.Turn;
            Distance += spec.Length;
            Count++;
            last = index;
            return pick;
        }

        // Recorrido completo hasta 'length' m (Desafío Diario).
        public static List<ChunkPick> Plan(IReadOnlyList<ChunkSpec> chunkSpecs, int seed, float length, float step, float headingLimit)
        {
            var planner = new EndlessPlanner(chunkSpecs, seed, step, headingLimit);
            var picks = new List<ChunkPick>();
            while (planner.Distance < length)
                picks.Add(planner.Next());
            return picks;
        }

        private int FindStart()
        {
            for (int i = 0; i < specs.Count; i++)
            {
                if (specs[i].Difficulty == 0)
                    return i;
            }
            return 0;
        }

        // Tramos de la dificultad pedida (peso 3) o de una menos (peso 1), sin repetir el anterior.
        private int Choose(int tier)
        {
            candidates.Clear();
            weights.Clear();
            int total = 0;
            for (int i = 0; i < specs.Count; i++)
            {
                int d = specs[i].Difficulty;
                if (d < 1 || i == last || (d != tier && d != tier - 1))
                    continue;
                int w = d == tier ? WEIGHT_TIER : WEIGHT_BELOW;
                candidates.Add(i);
                weights.Add(w);
                total += w;
            }
            if (candidates.Count == 0)
                return Fallback(tier);
            int roll = rng.Next(total);
            for (int i = 0; i < candidates.Count; i++)
            {
                roll -= weights[i];
                if (roll < 0)
                    return candidates[i];
            }
            return candidates[candidates.Count - 1];
        }

        // Sin tramos de esa dificultad: el más difícil que no pase de ella.
        private int Fallback(int tier)
        {
            int best = -1;
            for (int i = 0; i < specs.Count; i++)
            {
                int d = specs[i].Difficulty;
                if (d < 1 || d > tier || i == last)
                    continue;
                if (best < 0 || d > specs[best].Difficulty)
                    best = i;
            }
            return best >= 0 ? best : FindStart();
        }
    }
}
