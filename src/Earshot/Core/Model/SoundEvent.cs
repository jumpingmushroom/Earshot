namespace Earshot.Core.Model
{
    /// <summary>One sound as the model sees it: plain data, filled in by the game-side adapter.</summary>
    public sealed class SoundEvent
    {
        /// <summary>Prefab name without "(Clone)"; "creature:<prefab>" for creature loops, "raid:<event>" for raids.</summary>
        public string PrefabName;
        public string PrimaryToken = "";
        public string SecondaryToken = "";
        public VanillaType VanillaType;
        /// <summary>Character.m_name of the creature within 2 m of the sound, e.g. "$enemy_greydwarf"; null if none.</summary>
        public string CreatureToken;
        /// <summary>Piece.m_name of the piece the sound is parented under; null if none.</summary>
        public string ObjectToken;
        /// <summary>Instance id of that creature or piece, for ×N; 0 when unknown (counts as one source).</summary>
        public int SourceId;
        public bool IsLoop;
        public float X;
        public float Z;
        /// <summary>Metres from the audio listener.</summary>
        public float Distance;
        public float MaxDistance;
        /// <summary>0..1 at the listener, before the player's volume sliders.</summary>
        public float Loudness;
        public bool OnScreen;
    }
}
