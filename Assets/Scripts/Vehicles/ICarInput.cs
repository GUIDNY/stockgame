namespace TurboLoop.Vehicles
{
    /// <summary>Anything that can drive a car: the keyboard or an AI driver.</summary>
    public interface ICarInput
    {
        /// <summary>-1 (brake / reverse) .. 1 (full throttle).</summary>
        float Throttle { get; }
        /// <summary>-1 (left) .. 1 (right).</summary>
        float Steer { get; }
        bool Handbrake { get; }
    }
}
