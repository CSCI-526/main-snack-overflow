public static class GameMode {
    public static bool TutorialRequested { get; private set; }
    public static void SetTutorial(bool on) => TutorialRequested = on;
}
