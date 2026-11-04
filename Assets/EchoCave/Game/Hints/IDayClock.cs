namespace EchoCave.Game
{
    // The calendar, as the hint rewards see it: a day number. Behind an interface so the tests can move time.
    public interface IDayClock
    {
        // A number that grows by one for each calendar day, from 1. The same all day.
        int Today { get; }
    }
}
